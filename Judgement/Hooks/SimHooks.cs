using RoR2;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Judgement
{
    public class SimHooks
    {
        public SimHooks()
        {
            On.RoR2.InfiniteTowerRun.OverrideRuleChoices += InfiniteTowerRun_OverrideRuleChoices;
            On.RoR2.InfiniteTowerRun.SpawnSafeWard += InfiniteTowerRun_SpawnSafeWard;
            On.RoR2.InfiniteTowerRun.MoveSafeWard += InfiniteTowerRun_MoveSafeWard;
            On.RoR2.InfiniteTowerRun.RecalculateDifficultyCoefficentInternal += InfiniteTowerRun_RecalculateDifficultyCoefficentInternal;
            On.RoR2.InfiniteTowerWaveController.DropRewards += InfiniteTowerWaveController_DropRewards;
            On.RoR2.InfiniteTowerWaveController.OnEnable += InfiniteTowerWaveController_OnEnable;
            On.RoR2.InfiniteTowerBossWaveController.PreStartClient += InfiniteTowerBossWaveController_PreStartClient;
        }

        private void InfiniteTowerWaveController_OnEnable(On.RoR2.InfiniteTowerWaveController.orig_OnEnable orig, InfiniteTowerWaveController self)
        {
            if (Run.instance && Run.instance.name.Contains("Judgement"))
            {
                if (self is InfiniteTowerBossWaveController)
                    self.baseCredits = 400;
                else
                    self.baseCredits = 125;
                // 159 500
            }
            orig(self);
        }

        private void InfiniteTowerRun_MoveSafeWard(On.RoR2.InfiniteTowerRun.orig_MoveSafeWard orig, InfiniteTowerRun self)
        {
            if (Run.instance && Run.instance.name.Contains("Judgement"))
                return;
            orig(self);
        }

        private void InfiniteTowerWaveController_DropRewards(On.RoR2.InfiniteTowerWaveController.orig_DropRewards orig, InfiniteTowerWaveController self)
        {
            if (Run.instance && Run.instance.name.Contains("Judgement"))
                return;
            orig(self);
        }

        private void InfiniteTowerBossWaveController_PreStartClient(On.RoR2.InfiniteTowerBossWaveController.orig_PreStartClient orig, InfiniteTowerBossWaveController self)
        {
            if (Run.instance && Run.instance.name.Contains("Judgement"))
                self.guaranteeInitialChampion = true;
            orig(self);
        }

        private void InfiniteTowerRun_OverrideRuleChoices(
          On.RoR2.InfiniteTowerRun.orig_OverrideRuleChoices orig,
          InfiniteTowerRun self,
          RuleChoiceMask mustInclude,
          RuleChoiceMask mustExclude,
          ulong runSeed)
        {
            if ((bool)PreGameController.instance && PreGameController.instance.gameModeIndex == GameModeCatalog.FindGameModeIndex("xJudgementRun"))
            {
                string[] itemBlacklist = new string[] {
                    "HealWhileSafe",
                    "HealingPotion",
                    "Medkit",
                    "Tooth",
                    "Seed",
                    "TPHealingNova",
                    "BarrierOnOverHeal",
                    "ExtraLife",
                    "ExtraLifeVoid",
                    "IncreaseHealing",
                    "NovaOnHeal",
                    "Plant",
                    "RepeatHeal",
                    "Mushroom",
                    "MushroomVoid",
                };
                string[] equipmentBlacklist = new string[] {
                    "Fruit",
                    "LifestealOnHit",
                    "PassiveHealing",
                    "VendingMachine"
                };

                foreach (string item in itemBlacklist)
                {
                    RuleChoiceDef choice = RuleCatalog.FindRuleDef("Items." + item)?.FindChoice("Off");
                    if (choice != null)
                        self.ForceChoice(mustInclude, mustExclude, choice);
                }
                foreach (string equipment in equipmentBlacklist)
                {
                    RuleChoiceDef choice = RuleCatalog.FindRuleDef("Equipment." + equipment)?.FindChoice("Off");
                    if (choice != null)
                        self.ForceChoice(mustInclude, mustExclude, choice);
                }
            }
            else orig(self, mustInclude, mustExclude, runSeed);
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
                GameMode.JudgementRun judgementRun = Run.instance.gameObject.GetComponent<GameMode.JudgementRun>();
                judgementRun.shouldGoBazaar = true;
                judgementRun.isFirstStage = false;
                judgementRun.purchaseCounter = 0;
                if (judgementRun.currentWave == 10)
                {
                    GameObject.Destroy(self.fogDamageController.gameObject);
                    GameObject director = GameObject.Find("Director");
                    if (director)
                    {
                        foreach (CombatDirector cd in director.GetComponents<CombatDirector>())
                            GameObject.Destroy(cd);
                    }
                    return;
                }
                judgementRun.currentWave += 2;
            }
            orig(self, spawnCard, placementRule);
        }
    }
}