using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using RoR2.UI;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;
using EntityStates.Missions.BrotherEncounter;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace Judgement
{
    public class GameMode
    {
        public static GameObject judgementRunPrefab;
        public static GameObject extraGameModeMenu;
        private int currentWave = 0;
        private bool shouldDropRewards = false;
        private bool shouldGoBazaar = true;
        private bool isFirstStage = true;
        private Vector3 safeWardPos = Vector3.zero;
        private Dictionary<CharacterMaster, float> persistentHP = new();
        private SceneDef voidPlains = Addressables.LoadAssetAsync<SceneDef>("RoR2/DLC1/itgolemplains/itgolemplains.asset").WaitForCompletion();
        private SceneDef voidAqueduct = Addressables.LoadAssetAsync<SceneDef>("RoR2/DLC1/itgoolake/itgoolake.asset").WaitForCompletion();
        private SceneDef voidAphelian = Addressables.LoadAssetAsync<SceneDef>("RoR2/DLC1/itancientloft/itancientloft.asset").WaitForCompletion();
        private SceneDef voidRPD = Addressables.LoadAssetAsync<SceneDef>("RoR2/DLC1/itfrozenwall/itfrozenwall.asset").WaitForCompletion();
        private SceneDef voidAbyssal = Addressables.LoadAssetAsync<SceneDef>("RoR2/DLC1/itdampcave/itdampcave.asset").WaitForCompletion();
        private SceneDef voidMeadow = Addressables.LoadAssetAsync<SceneDef>("RoR2/DLC1/itskymeadow/itskymeadow.asset").WaitForCompletion();
        private SceneDef voidMoon = Addressables.LoadAssetAsync<SceneDef>("RoR2/DLC1/itmoon/itmoon.asset").WaitForCompletion();

        private GameObject potentialPickup = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/OptionPickup/OptionPickup.prefab").WaitForCompletion();

        private BasicPickupDropTable dtEquip = Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/Common/dtEquipment.asset").WaitForCompletion();
        private BasicPickupDropTable dtWhite = Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/Common/dtTier1Item.asset").WaitForCompletion();
        private BasicPickupDropTable dtGreen = Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/Common/dtTier2Item.asset").WaitForCompletion();
        private BasicPickupDropTable dtRed = Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/Common/dtTier3Item.asset").WaitForCompletion();

        private static GameEndingDef judgementRunEnding = Addressables.LoadAssetAsync<GameEndingDef>("RoR2/Base/WeeklyRun/PrismaticTrialEnding.asset").WaitForCompletion();

        private static readonly GameObject voidChest = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/VoidChest/VoidChest.prefab").WaitForCompletion();
        private static readonly GameObject portalPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/PortalArena/PortalArena.prefab").WaitForCompletion();

        private static readonly PostProcessProfile ppProfile = Addressables.LoadAssetAsync<PostProcessProfile>("RoR2/Base/title/ppSceneEclipseStandard.asset").WaitForCompletion();
        private static readonly Material spaceStarsMat = Addressables.LoadAssetAsync<Material>("RoR2/Base/eclipseworld/matEclipseStarsSpheres.mat").WaitForCompletion();
        private static readonly Material altSkyboxMat = Addressables.LoadAssetAsync<Material>("RoR2/Base/artifactworld/matAWSkySphere.mat").WaitForCompletion();

        public GameMode()
        {

            judgementRunPrefab = PrefabAPI.InstantiateClone(new GameObject("xJudgementRun"), "xJudgementRun");
            judgementRunPrefab.AddComponent<NetworkIdentity>();
            PrefabAPI.RegisterNetworkPrefab(judgementRunPrefab);

            InfiniteTowerRun component2 = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/GameModes/InfiniteTowerRun/InfiniteTowerRun.prefab").WaitForCompletion().GetComponent<InfiniteTowerRun>();

            JudgementRun judgementRun = judgementRunPrefab.AddComponent<JudgementRun>();
            judgementRun.nameToken = "Judgement Trial";
            judgementRun.userPickable = true;
            judgementRun.startingSceneGroup = component2.startingSceneGroup;
            judgementRun.gameOverPrefab = component2.gameOverPrefab;
            judgementRun.lobbyBackgroundPrefab = component2.lobbyBackgroundPrefab;
            judgementRun.uiPrefab = component2.uiPrefab;

            judgementRun.defaultWavePrefab = component2.defaultWavePrefab;
            judgementRun.waveCategories = component2.waveCategories;
            foreach (InfiniteTowerWaveCategory cat in judgementRun.waveCategories)
            {
                if (cat.name == "BossWaveCategory")
                {
                    cat.availabilityPeriod = 2;
                    List<InfiniteTowerWaveCategory.WeightedWave> weightedWaves = new();
                    foreach (InfiniteTowerWaveCategory.WeightedWave item in cat.wavePrefabs)
                    {
                        if (!item.wavePrefab.name.Contains("Brother") && !item.wavePrefab.name.Contains("Lunar"))
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
            IL.RoR2.SceneDirector.PopulateScene += RemoveExtraLoot;
            On.RoR2.Run.Start += Run_Start;
            On.RoR2.Stage.Start += Stage_Start;
            On.RoR2.BazaarController.Start += BazaarController_Start;
            On.RoR2.MusicController.PickCurrentTrack += MusicController_PickCurrentTrack;
            On.EntityStates.Missions.BrotherEncounter.BossDeath.OnEnter += BossDeathOnEnter;
            On.RoR2.UI.LanguageTextMeshController.Start += LanguageTextMeshController_Start;
            On.RoR2.GameModeCatalog.SetGameModes += GameModeCatalog_SetGameModes;
            On.RoR2.InfiniteTowerRun.OverrideRuleChoices += InfiniteTowerRun_OverrideRuleChoices;
            On.RoR2.CharacterMaster.OnBodyStart += CharacterMaster_OnBodyStart;
            On.RoR2.SceneExitController.Begin += SceneExitController_Begin;
            On.RoR2.Run.PickNextStageScene += Run_PickNextStageScene;
            On.RoR2.InfiniteTowerRun.SpawnSafeWard += InfiniteTowerRun_SpawnSafeWard;
            On.RoR2.InfiniteTowerRun.MoveSafeWard += InfiniteTowerRun_MoveSafeWard;
            On.RoR2.InfiniteTowerWaveController.DropRewards += InfiniteTowerWaveController_DropRewards;
            On.RoR2.InfiniteTowerWaveController.OnEnable += InfiniteTowerWaveController_OnEnable;
            On.RoR2.InfiniteTowerBossWaveController.PreStartClient += InfiniteTowerBossWaveController_PreStartClient;
            On.RoR2.InfiniteTowerRun.RecalculateDifficultyCoefficentInternal += InfiniteTowerRun_RecalculateDifficultyCoefficentInternal;
        }
        /*
                public void SavePersistentHP()
                {
                    if (!NetworkServer.active)
                        return;
                    foreach (PlayerCharacterMasterController instance in PlayerCharacterMasterController.instances)
                    {
                        CharacterBody body = instance.master?.GetBody();
                        if (body != null && body.isPlayerControlled)
                        {
                            if (body.healthComponent)
                                this.persistentHP[instance.master] = body.healthComponent.health;
                        }
                    }
                }
        */
        private void RemoveExtraLoot(ILContext il)
        {
            ILCursor ilCursor = new(il);

            static int ItemFunction(int itemCount)
            {
                if (Run.instance && Run.instance.name.Contains("Judgement") && SceneManager.GetActiveScene().name != "bazaar")
                    return 0;
                return itemCount;
            }

            if (ilCursor.TryGotoNext(0, x => ILPatternMatchingExt.MatchLdsfld(x, typeof(RoR2Content.Items), "TreasureCache")))
            {
                ilCursor.Index += 2;
                ilCursor.EmitDelegate(ItemFunction);
            }
            else
                Debug.LogWarning("Judgement: TreasureCache IL hook failed");

            ilCursor.Index = 0;

            if (ilCursor.TryGotoNext(0, x => ILPatternMatchingExt.MatchLdsfld(x, typeof(DLC1Content.Items), "FreeChest")))
            {
                ilCursor.Index += 2;
                ilCursor.EmitDelegate(ItemFunction);
            }
            else
                Debug.LogWarning("Judgement: FreeChest IL hook failed");
        }

        public void LoadPersistentHP(CharacterBody body)
        {
            if (!NetworkServer.active || body.master == null || !this.persistentHP.TryGetValue(body.master, out float hp))
                return;
            if (body.healthComponent)
                body.healthComponent.health = hp;
            this.persistentHP.Remove(body.master);
        }
        // bazaar spawnpos -81.5 -24.8 -16.6
        // portal spawnpos -128.6 -25.4 -14.4
        // key/shorm1 -112.0027 -23.7788 -4.5843
        // key/shorm2 -103.7627 -23.8988 -4.7243

        private void BazaarController_Start(On.RoR2.BazaarController.orig_Start orig, BazaarController self)
        {
            orig(self);
            if (Run.instance && Run.instance.name.Contains("Judgement"))
            {
                Run.instance.stageClearCount += 1;
                GameObject holder = GameObject.Find("HOLDER: Store");
                if (holder)
                {
                    shouldGoBazaar = false;
                    GameObject portal = GameObject.Instantiate(portalPrefab, new Vector3(-128.6f, -25.4f, -14.4f), Quaternion.Euler(0, 90, 0));
                    NetworkServer.Spawn(portal);
                    holder.transform.GetChild(2).gameObject.SetActive(false); // disable seershop
                    holder.transform.GetChild(3).gameObject.SetActive(false); // disable cauldrons
                    GameObject kickout = SceneInfo.instance.transform.Find("KickOutOfShop").gameObject;
                    if ((bool)kickout)
                    {
                        kickout.gameObject.SetActive(true);
                        kickout.transform.GetChild(8).gameObject.SetActive(false);
                    }
                    foreach (Transform child in holder.transform.GetChild(0).GetChild(2))
                        GameObject.Destroy(child.gameObject);

                    // getchild(0) lunar shop
                    // getchild(0)(2) table, disable all children
                    // item positions 
                    // new Vector3(-73.9124f, -24.0468f, -37.9145f)  new Vector3(-77.4559f, -24.0468f, -37.4419f)  new Vector3(-80.6413f, -24.0468f, -42.1104f) new Vector3(-79.2328f, -24.0468f, -45.2478f) |middle new Vector3(-80.0593f, -24.0468f, -39.2219f)|
                    if (this.currentWave == 0)
                    {
                        PickupDropletController.CreatePickupDroplet(new GenericPickupController.CreatePickupInfo()
                        {
                            pickupIndex = PickupCatalog.FindPickupIndex(ItemTier.Tier1),
                            pickerOptions = PickupPickerController.GenerateOptionsFromDropTable(3, dtWhite, self.rng),
                            rotation = Quaternion.identity,
                            prefabOverride = potentialPickup
                        }, new Vector3(-73.9124f, -24f, -37.9145f), Vector3.zero);
                        PickupDropletController.CreatePickupDroplet(new GenericPickupController.CreatePickupInfo()
                        {
                            pickupIndex = PickupCatalog.FindPickupIndex(ItemTier.Tier1),
                            pickerOptions = PickupPickerController.GenerateOptionsFromDropTable(3, dtWhite, self.rng),
                            rotation = Quaternion.identity,
                            prefabOverride = potentialPickup
                        }, new Vector3(-77.4559f, -24f, -37.4419f), Vector3.zero);
                        PickupDropletController.CreatePickupDroplet(new GenericPickupController.CreatePickupInfo()
                        {
                            pickupIndex = PickupCatalog.FindPickupIndex(ItemTier.Tier3),
                            pickerOptions = PickupPickerController.GenerateOptionsFromDropTable(3, dtRed, self.rng),
                            rotation = Quaternion.identity,
                            prefabOverride = potentialPickup
                        }, new Vector3(-80.6413f, -24f, -42.1104f), Vector3.zero);
                        PickupDropletController.CreatePickupDroplet(new GenericPickupController.CreatePickupInfo()
                        {
                            pickupIndex = PickupCatalog.FindPickupIndex(EquipmentIndex.None),
                            pickerOptions = PickupPickerController.GenerateOptionsFromDropTable(3, dtEquip, self.rng),
                            rotation = Quaternion.identity,
                            prefabOverride = potentialPickup
                        }, new Vector3(-79.2328f, -24f, -45.2478f), Vector3.zero);
                    }
                    else
                    {
                        if (this.currentWave == 4)
                        {
                            PickupDropletController.CreatePickupDroplet(new GenericPickupController.CreatePickupInfo()
                            {
                                pickupIndex = PickupCatalog.FindPickupIndex(EquipmentIndex.None),
                                pickerOptions = PickupPickerController.GenerateOptionsFromDropTable(3, dtEquip, self.rng),
                                rotation = Quaternion.identity,
                                prefabOverride = potentialPickup
                            }, new Vector3(-80.0593f, -24f, -39.2219f), Vector3.zero);
                            PickupDropletController.CreatePickupDroplet(new GenericPickupController.CreatePickupInfo()
                            {
                                pickupIndex = PickupCatalog.FindPickupIndex(ItemTier.Tier3),
                                pickerOptions = PickupPickerController.GenerateOptionsFromDropTable(3, dtRed, self.rng),
                                rotation = Quaternion.identity,
                                prefabOverride = potentialPickup
                            }, new Vector3(-73.9124f, -24f, -37.9145f), Vector3.zero);
                        }
                        if (this.currentWave != 4)
                        {
                            PickupDropletController.CreatePickupDroplet(new GenericPickupController.CreatePickupInfo()
                            {
                                pickupIndex = PickupCatalog.FindPickupIndex(ItemTier.Tier2),
                                pickerOptions = PickupPickerController.GenerateOptionsFromDropTable(3, dtGreen, self.rng),
                                rotation = Quaternion.identity,
                                prefabOverride = potentialPickup
                            }, new Vector3(-73.9124f, -24f, -37.9145f), Vector3.zero);
                        }
                        PickupDropletController.CreatePickupDroplet(new GenericPickupController.CreatePickupInfo()
                        {
                            pickupIndex = PickupCatalog.FindPickupIndex(ItemTier.Tier1),
                            pickerOptions = PickupPickerController.GenerateOptionsFromDropTable(3, dtWhite, self.rng),
                            rotation = Quaternion.identity,
                            prefabOverride = potentialPickup
                        }, new Vector3(-77.4559f, -24f, -37.4419f), Vector3.zero);
                        PickupDropletController.CreatePickupDroplet(new GenericPickupController.CreatePickupInfo()
                        {
                            pickupIndex = PickupCatalog.FindPickupIndex(ItemTier.Tier2),
                            pickerOptions = PickupPickerController.GenerateOptionsFromDropTable(3, dtGreen, self.rng),
                            rotation = Quaternion.identity,
                            prefabOverride = potentialPickup
                        }, new Vector3(-80.6413f, -24f, -42.1104f), Vector3.zero);
                        PickupDropletController.CreatePickupDroplet(new GenericPickupController.CreatePickupInfo()
                        {
                            pickupIndex = PickupCatalog.FindPickupIndex(ItemTier.Tier1),
                            pickerOptions = PickupPickerController.GenerateOptionsFromDropTable(3, dtWhite, self.rng),
                            rotation = Quaternion.identity,
                            prefabOverride = potentialPickup
                        }, new Vector3(-79.2328f, -24f, -45.2478f), Vector3.zero);
                    }
                    // Form/Keybox
                    int num2 = 0;
                    foreach (CharacterMaster readOnlyInstances in CharacterMaster.readOnlyInstancesList)
                    {
                        if (readOnlyInstances.inventory.GetItemCount(RoR2Content.Items.TreasureCache) > 0)
                            ++num2;
                    }
                    for (int index = 0; index < num2; ++index)
                    {
                        DirectorCore instance = DirectorCore.instance;
                        SpawnCard spawnCard = LegacyResourcesAPI.Load<SpawnCard>("SpawnCards/InteractableSpawnCard/iscLockbox");
                        DirectorPlacementRule placementRule = new DirectorPlacementRule();
                        placementRule.placementMode = DirectorPlacementRule.PlacementMode.Direct;
                        placementRule.position = new Vector3(-103.7627f, -23.8988f, -4.7243f);
                        Xoroshiro128Plus rng = self.rng;
                        DirectorSpawnRequest directorSpawnRequest = new DirectorSpawnRequest(spawnCard, placementRule, rng);
                        instance.TrySpawnObject(directorSpawnRequest);
                    }
                    int num4 = 0;
                    foreach (CharacterMaster readOnlyInstances in CharacterMaster.readOnlyInstancesList)
                    {
                        if (readOnlyInstances.inventory.GetItemCount(DLC1Content.Items.FreeChest) > 0)
                            ++num4;
                    }
                    for (int index = 0; index < num4; ++index)
                    {
                        DirectorCore instance = DirectorCore.instance;
                        SpawnCard spawnCard = LegacyResourcesAPI.Load<SpawnCard>("SpawnCards/InteractableSpawnCard/iscFreeChest");
                        DirectorPlacementRule placementRule = new DirectorPlacementRule();
                        placementRule.placementMode = DirectorPlacementRule.PlacementMode.Direct;
                        placementRule.position = new Vector3(-112.0027f, -23.7788f, -4.5843f);
                        Xoroshiro128Plus rng = self.rng;
                        DirectorSpawnRequest directorSpawnRequest = new DirectorSpawnRequest(spawnCard, placementRule, rng);
                        instance.TrySpawnObject(directorSpawnRequest);
                    }
                }
            }
        }

        private void InfiniteTowerRun_RecalculateDifficultyCoefficentInternal(On.RoR2.InfiniteTowerRun.orig_RecalculateDifficultyCoefficentInternal orig, InfiniteTowerRun self)
        {
            if (Run.instance && Run.instance.name.Contains("Judgement"))
            {
                DifficultyDef difficultyDef = DifficultyCatalog.GetDifficultyDef(self.selectedDifficulty);
                float num1 = 1.5f * (float)self.waveIndex;
                float num2 = 0.0506f * (difficultyDef.scalingValue * 2f);
                float num3 = Mathf.Pow(1.02f, (float)self.waveIndex);
                self.difficultyCoefficient = (float)(1.0 + (double)num2 * (double)num1) * num3;
                self.compensatedDifficultyCoefficient = self.difficultyCoefficient;
                self.ambientLevel = Mathf.Min((float)(((double)self.difficultyCoefficient - 1.0) / 0.33000001311302185 + 1.0), 9999f);
                int ambientLevelFloor = self.ambientLevelFloor;
                self.ambientLevelFloor = Mathf.FloorToInt(self.ambientLevel);
                if (ambientLevelFloor == self.ambientLevelFloor || ambientLevelFloor == 0 || self.ambientLevelFloor <= ambientLevelFloor)
                    return;
                self.OnAmbientLevelUp();
            }
            else
                orig(self);
        }

        private void Run_Start(On.RoR2.Run.orig_Start orig, Run self)
        {
            orig(self);
            if (self.name.Contains("Judgement"))
            {
                currentWave = 0;
                shouldDropRewards = false;
                isFirstStage = true;
                shouldGoBazaar = true;
            }
        }

        private static void MusicController_PickCurrentTrack(On.RoR2.MusicController.orig_PickCurrentTrack orig, MusicController self, ref MusicTrackDef newTrack)
        {
            orig(self, ref newTrack);
            if (Run.instance && Run.instance.name.Contains("Judgement") && newTrack.cachedName != "muSong25")
                newTrack = MusicTrackCatalog.FindMusicTrackDef("muSong23");
        }

        private void Stage_Start(On.RoR2.Stage.orig_Start orig, Stage self)
        {
            orig(self);
            if (Run.instance && Run.instance.name.Contains("Judgement"))
            {
                float ambientIntensity = 0.85f;
                if (SceneManager.GetActiveScene().name == "itfrozenwall")
                    ambientIntensity = 0.5f;
                GameObject skybox = GameObject.Find("Weather, VoidArena");
                if (skybox)
                {
                    PostProcessVolume ppv = skybox.transform.GetChild(0).gameObject.GetComponent<PostProcessVolume>();
                    SetAmbientLight amb = skybox.transform.GetChild(0).gameObject.GetComponent<SetAmbientLight>();
                    ppv.profile = ppProfile;
                    amb.ambientIntensity = ambientIntensity;
                    amb.ApplyLighting();
                    skybox.transform.GetChild(2).GetComponent<Light>().intensity = 1f;
                    MeshRenderer skyRenderer = skybox.transform.GetChild(1).GetChild(0).GetChild(2).gameObject.GetComponent<MeshRenderer>();
                    skyRenderer.sharedMaterials = new Material[] { altSkyboxMat, spaceStarsMat };
                }
                else
                {
                    skybox = GameObject.Find("Weather, InfiniteTower");
                    if (skybox)
                    {
                        PostProcessVolume ppv = skybox.transform.GetChild(0).gameObject.GetComponent<PostProcessVolume>();
                        SetAmbientLight amb = skybox.transform.GetChild(0).gameObject.GetComponent<SetAmbientLight>();
                        ppv.profile = ppProfile;
                        amb.ambientIntensity = ambientIntensity;
                        amb.ApplyLighting();
                        skybox.transform.GetChild(2).GetComponent<Light>().intensity = 1f;
                        MeshRenderer skyRenderer = skybox.transform.GetChild(1).GetChild(0).GetChild(2).gameObject.GetComponent<MeshRenderer>();
                        skyRenderer.sharedMaterials = new Material[] { altSkyboxMat, spaceStarsMat };
                    }
                }
            }
        }

        private void Run_PickNextStageScene(On.RoR2.Run.orig_PickNextStageScene orig, Run self, WeightedSelection<SceneDef> choices)
        {
            if (Run.instance.stageClearCount == 0 && this.isFirstStage && self.name.Contains("Judgement"))
            {
                this.isFirstStage = false;
                SceneDef sceneDef = SceneCatalog.FindSceneDef("bazaar");
                self.nextStageScene = sceneDef;
            }
            else
                orig(self, choices);
        }

        private void InfiniteTowerRun_SpawnSafeWard(On.RoR2.InfiniteTowerRun.orig_SpawnSafeWard orig, InfiniteTowerRun self, InteractableSpawnCard spawnCard, DirectorPlacementRule placementRule)
        {
            if (Run.instance && Run.instance.name.Contains("Judgement"))
            {
                if (SceneManager.GetActiveScene().name == "bazaar")
                {
                    if (self.fogDamageController)
                        GameObject.Destroy(self.fogDamageController.gameObject);
                    return;
                }
                shouldGoBazaar = true;
                if (currentWave == 10)
                {
                    GameObject.Destroy(self.fogDamageController.gameObject);
                    return;
                }
                currentWave += 2;
            }
            orig(self, spawnCard, placementRule);
        }

        private void InfiniteTowerRun_MoveSafeWard(On.RoR2.InfiniteTowerRun.orig_MoveSafeWard orig, InfiniteTowerRun self)
        {
            if (Run.instance && Run.instance.name.Contains("Judgement"))
                return;
            orig(self);
        }

        private void InfiniteTowerWaveController_OnEnable(On.RoR2.InfiniteTowerWaveController.orig_OnEnable orig, InfiniteTowerWaveController self)
        {
            if (Run.instance && Run.instance.name.Contains("Judgement"))
                self.linearCreditsPerWave *= 1.25f;
            orig(self);
        }

        private void InfiniteTowerWaveController_DropRewards(On.RoR2.InfiniteTowerWaveController.orig_DropRewards orig, InfiniteTowerWaveController self)
        {
            if (Run.instance && Run.instance.name.Contains("Judgement"))
            {
                if (this.currentWave != 10)
                    return;
                if (!this.shouldDropRewards)
                {
                    this.shouldDropRewards = true;
                    return;
                }
                Vector3 dropPos = this.safeWardPos;
                double angle = 360 / 4;
                Vector3 velocity = Quaternion.AngleAxis(UnityEngine.Random.Range(0, 360), Vector3.up) * (Vector3.up * 40f + Vector3.forward * 5f);
                Vector3 up = Vector3.up;
                Quaternion quaternion = Quaternion.AngleAxis((float)angle, up);
                Vector3 position = dropPos + new Vector3(0, 5f, 0);
                for (int i = 0; i < 2; i++)
                {
                    if (UnityEngine.Random.value < 0.1f)
                    {
                        PickupDropletController.CreatePickupDroplet(new GenericPickupController.CreatePickupInfo()
                        {
                            pickupIndex = PickupCatalog.FindPickupIndex(ItemTier.Tier3),
                            pickerOptions = PickupPickerController.GenerateOptionsFromDropTable(3, dtRed, Run.instance.runRNG),
                            rotation = Quaternion.identity,
                            prefabOverride = potentialPickup
                        }, position, velocity);
                        velocity = quaternion * velocity;
                    }
                    else
                    {
                        PickupDropletController.CreatePickupDroplet(new GenericPickupController.CreatePickupInfo()
                        {
                            pickupIndex = PickupCatalog.FindPickupIndex(ItemTier.Tier2),
                            pickerOptions = PickupPickerController.GenerateOptionsFromDropTable(3, dtGreen, Run.instance.runRNG),
                            rotation = Quaternion.identity,
                            prefabOverride = potentialPickup
                        }, position, velocity);
                        velocity = quaternion * velocity;
                    }
                    PickupDropletController.CreatePickupDroplet(new GenericPickupController.CreatePickupInfo()
                    {
                        pickupIndex = PickupCatalog.FindPickupIndex(ItemTier.Tier1),
                        pickerOptions = PickupPickerController.GenerateOptionsFromDropTable(3, dtWhite, Run.instance.runRNG),
                        rotation = Quaternion.identity,
                        prefabOverride = potentialPickup
                    }, position, velocity);
                    velocity = quaternion * velocity;
                }
            }
            else
                orig(self);
        }

        private void InfiniteTowerBossWaveController_PreStartClient(On.RoR2.InfiniteTowerBossWaveController.orig_PreStartClient orig, InfiniteTowerBossWaveController self)
        {
            if (Run.instance && Run.instance.name.Contains("Judgement"))
                self.guaranteeInitialChampion = true;
            orig(self);
        }

        private void SceneExitController_Begin(On.RoR2.SceneExitController.orig_Begin orig, SceneExitController self)
        {
            if (Run.instance && Run.instance.name.Contains("Judgement"))
            {
                // SavePersistentHP();
                if (this.currentWave == 0)
                    Run.instance.nextStageScene = voidPlains;
                if (this.currentWave == 2)
                {
                    if (UnityEngine.Random.value < 0.5f)
                        Run.instance.nextStageScene = voidAqueduct;
                    else
                        Run.instance.nextStageScene = voidAphelian;
                }
                if (this.currentWave == 4)
                    Run.instance.nextStageScene = voidRPD;
                if (this.currentWave == 6)
                    Run.instance.nextStageScene = voidAbyssal;
                if (this.currentWave == 8)
                    Run.instance.nextStageScene = voidMeadow;
                if (this.currentWave == 10)
                {
                    SceneDef sceneDef = SceneCatalog.FindSceneDef("moon2");
                    Run.instance.nextStageScene = sceneDef;
                }
                if (this.shouldGoBazaar)
                {
                    SceneDef sceneDef = SceneCatalog.FindSceneDef("bazaar");
                    Run.instance.nextStageScene = sceneDef;
                }
            }
            orig(self);
        }

        private void CharacterMaster_OnBodyStart(
          On.RoR2.CharacterMaster.orig_OnBodyStart orig,
          CharacterMaster self,
          CharacterBody body)
        {
            orig(self, body);
            if (!NetworkServer.active || Run.instance.gameModeIndex != GameModeCatalog.FindGameModeIndex("xJudgementRun"))
                return;
            if (body.isPlayerControlled)
            {
                body.baseRegen = 0f;
                body.levelRegen = 0f;
                // if (!body.HasBuff(RoR2Content.Buffs.Immune))
                // LoadPersistentHP(body);
                if (SceneManager.GetActiveScene().name == "bazaar" && body.characterMotor)
                    body.characterMotor.Motor.SetPositionAndRotation(new Vector3(-81.5f, -24.8f, -16.6f), Quaternion.Euler(0, 90, 0));
                if (SceneManager.GetActiveScene().name == "moon2" && body.characterMotor && !body.HasBuff(RoR2Content.Buffs.Immune))
                    body.characterMotor.Motor.SetPositionAndRotation(new Vector3(100, 500, 100), Quaternion.identity);
            }
        }
        private void BossDeathOnEnter(On.EntityStates.Missions.BrotherEncounter.BossDeath.orig_OnEnter orig, BossDeath self)
        {
            orig(self);
            if (Run.instance && Run.instance.name.Contains("Judgement"))
                Run.instance.BeginGameOver(judgementRunEnding);
        }

        private void GameModeCatalog_SetGameModes(
          On.RoR2.GameModeCatalog.orig_SetGameModes orig,
          Run[] newGameModePrefabComponents)
        {
            Array.Sort<Run>(newGameModePrefabComponents, (Comparison<Run>)((a, b) => string.CompareOrdinal(a.name, b.name)));
            orig(newGameModePrefabComponents);
        }

        private void InfiniteTowerRun_OverrideRuleChoices(
          On.RoR2.InfiniteTowerRun.orig_OverrideRuleChoices orig,
          InfiniteTowerRun self,
          RuleChoiceMask mustInclude,
          RuleChoiceMask mustExclude,
          ulong runSeed)
        {
            if ((bool)(UnityEngine.Object)PreGameController.instance && PreGameController.instance.gameModeIndex == GameModeCatalog.FindGameModeIndex("xJudgementRun"))
            {
                self.ForceChoice(mustInclude, mustExclude, "Difficulty.Hard");
                foreach (ArtifactDef artifactDef in ArtifactCatalog.artifactDefs)
                {
                    RuleChoiceDef choice5 = RuleCatalog.FindRuleDef("Artifacts." + artifactDef.cachedName)?.FindChoice("Off");
                    if (choice5 != null)
                        self.ForceChoice(mustInclude, mustExclude, choice5);
                }
            }
            else orig(self, mustInclude, mustExclude, runSeed);
        }

        private void LanguageTextMeshController_Start(
          On.RoR2.UI.LanguageTextMeshController.orig_Start orig,
          LanguageTextMeshController self)
        {
            orig(self);
            if (!(self.token == "TITLE_ECLIPSE") || !(bool)(UnityEngine.Object)self.GetComponent<HGButton>())
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
                this.hgButton.onClick.AddListener((UnityAction)(() =>
                {
                    int num = (int)Util.PlaySound("Play_UI_menuClick", RoR2Application.instance.gameObject);
                    RoR2.Console.instance.SubmitCmd((NetworkUser)null, "transition_command \"gamemode xJudgementRun; host 0; \"");
                }));
            }
        }

        public class JudgementRunButtonAdder : MonoBehaviour
        {
            public void Start()
            {
                GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.transform.Find("GenericMenuButton (Eclipse)").gameObject, this.transform);
                gameObject.AddComponent<JudgementRunButton>();
                gameObject.GetComponent<LanguageTextMeshController>().token = "Judgement";
                gameObject.GetComponent<HGButton>().hoverToken = "Defeat all that stand before you to reach the final throne.";
            }
        }

        public class JudgementRun : InfiniteTowerRun
        {



        }
    }
}