using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using RoR2.UI;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace Judgement
{
    public class GameMode
    {
        public static GameObject judgementRunPrefab;
        public static GameObject extraGameModeMenu;
        private GameObject simClone = PrefabAPI.InstantiateClone(Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/GameModes/InfiniteTowerRun/InfiniteTowerRun.prefab").WaitForCompletion(), "SimCloneNux");

        public GameMode()
        {
            judgementRunPrefab = PrefabAPI.InstantiateClone(new GameObject("xJudgementRun"), "xJudgementRun");
            judgementRunPrefab.AddComponent<NetworkIdentity>();
            PrefabAPI.RegisterNetworkPrefab(judgementRunPrefab);

            InfiniteTowerRun component2 = simClone.GetComponent<InfiniteTowerRun>();

            JudgementRun judgementRun = judgementRunPrefab.AddComponent<JudgementRun>();
            judgementRun.nameToken = "Judgement";
            judgementRun.userPickable = true;
            judgementRun.startingSceneGroup = component2.startingSceneGroup;
            judgementRun.gameOverPrefab = component2.gameOverPrefab;
            judgementRun.lobbyBackgroundPrefab = component2.lobbyBackgroundPrefab;
            judgementRun.uiPrefab = component2.uiPrefab;

            judgementRun.defaultWavePrefab = component2.defaultWavePrefab;
            InfiniteTowerWaveCategory[] waveCategories = new InfiniteTowerWaveCategory[component2.waveCategories.Length];
            Array.Copy(component2.waveCategories, waveCategories, component2.waveCategories.Length);
            judgementRun.waveCategories = waveCategories;
            foreach (InfiniteTowerWaveCategory cat in judgementRun.waveCategories)
            {
                if (cat.name == "BossWaveCategory")
                {
                    cat.availabilityPeriod = 2;
                    List<InfiniteTowerWaveCategory.WeightedWave> weightedWaves = new();
                    foreach (InfiniteTowerWaveCategory.WeightedWave item in cat.wavePrefabs)
                    {
                        if (!item.wavePrefab.name.Contains("Brother") && !item.wavePrefab.name.Contains("Lunar") && !item.wavePrefab.name.Contains("Void"))
                            weightedWaves.Add(item);
                    }
                    cat.wavePrefabs = weightedWaves.ToArray();
                }
            }
            judgementRun.defaultWaveEnemyIndicatorPrefab = component2.defaultWaveEnemyIndicatorPrefab;
            judgementRun.enemyItemPattern = component2.enemyItemPattern;
            judgementRun.enemyItemPeriod = 100;
            judgementRun.enemyInventory = judgementRunPrefab.AddComponent<Inventory>();
            judgementRun.stageTransitionPeriod = 2;
            judgementRun.stageTransitionPortalCard = component2.stageTransitionPortalCard;
            judgementRun.stageTransitionPortalMaxDistance = component2.stageTransitionPortalMaxDistance;
            judgementRun.stageTransitionChatToken = component2.stageTransitionChatToken;
            judgementRun.fogDamagePrefab = component2.fogDamagePrefab;
            judgementRun.spawnMaxRadius = component2.spawnMaxRadius;
            judgementRun.initialSafeWardCard = component2.initialSafeWardCard;
            judgementRun.safeWardCard = component2.safeWardCard;
            judgementRun.playerRespawnEffectPrefab = component2.playerRespawnEffectPrefab;
            judgementRun.interactableCredits = 0;
            judgementRun.blacklistedTags = component2.blacklistedTags;
            judgementRun.blacklistedItems = component2.blacklistedItems;

            judgementRunPrefab.AddComponent<TeamManager>();
            judgementRunPrefab.AddComponent<NetworkRuleBook>();
            judgementRunPrefab.AddComponent<TeamFilter>();
            judgementRunPrefab.AddComponent<EnemyInfoPanelInventoryProvider>();
            judgementRunPrefab.AddComponent<DirectorCore>();
            judgementRunPrefab.AddComponent<ExpansionRequirementComponent>();
            judgementRunPrefab.AddComponent<RunArtifactManager>();
            judgementRunPrefab.AddComponent<RunCameraManager>();

            ContentAddition.AddGameMode(judgementRunPrefab);

            On.RoR2.GameModeCatalog.SetGameModes += GameModeCatalog_SetGameModes;
            On.RoR2.UI.LanguageTextMeshController.Start += LanguageTextMeshController_Start;
        }

        private void GameModeCatalog_SetGameModes(
          On.RoR2.GameModeCatalog.orig_SetGameModes orig,
          Run[] newGameModePrefabComponents)
        {
            Array.Sort(newGameModePrefabComponents, (a, b) => string.CompareOrdinal(a.name, b.name));
            orig(newGameModePrefabComponents);
        }

        private void LanguageTextMeshController_Start(
          On.RoR2.UI.LanguageTextMeshController.orig_Start orig,
          LanguageTextMeshController self)
        {
            orig(self);
            if (!(self.token == "TITLE_ECLIPSE") || !(bool)self.GetComponent<HGButton>())
                return;
            self.transform.parent.gameObject.AddComponent<JudgementRunButtonAdder>();
        }

        public class JudgementRunButton : MonoBehaviour
        {
            public HGButton hgButton;

            public void Start()
            {
                this.hgButton = this.GetComponent<HGButton>();
                this.hgButton.onClick = new Button.ButtonClickedEvent();
                this.hgButton.onClick.AddListener(() =>
                {
                    int num = (int)Util.PlaySound("Play_UI_menuClick", RoR2Application.instance.gameObject);
                    RoR2.Console.instance.SubmitCmd(null, "transition_command \"gamemode xJudgementRun; host 0; \"");
                });
            }
        }

        public class JudgementRunButtonAdder : MonoBehaviour
        {
            public void Start()
            {
                GameObject gameObject = Instantiate(this.transform.Find("GenericMenuButton (Eclipse)").gameObject, this.transform);
                gameObject.AddComponent<JudgementRunButton>();
                gameObject.GetComponent<LanguageTextMeshController>().token = "Judgement";
                gameObject.GetComponent<HGButton>().hoverToken = "Defeat all that stand before you to reach the final throne.";
            }
        }

        public class JudgementRun : InfiniteTowerRun
        {
            public int currentWave = 0;
            public int availableHeals = 2;
            public int purchaseCounter = 0;
            public bool shouldGoBazaar = true;
            public bool isFirstStage = true;
            public Vector3 safeWardPos = Vector3.zero;
            public Xoroshiro128Plus bazaarRng;
            public Dictionary<NetworkInstanceId, float> persistentHP = new();
            public Dictionary<NetworkInstanceId, int> persistentCurse = new();
        }
    }
}