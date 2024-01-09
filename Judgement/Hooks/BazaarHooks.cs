using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace Judgement
{
    public class BazaarHooks
    {
        private BasicPickupDropTable dtEquip = Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/Common/dtEquipment.asset").WaitForCompletion();
        private BasicPickupDropTable dtWhite = Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/Common/dtTier1Item.asset").WaitForCompletion();
        private BasicPickupDropTable dtGreen = Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/Common/dtTier2Item.asset").WaitForCompletion();
        private BasicPickupDropTable dtRed = Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/Common/dtTier3Item.asset").WaitForCompletion();

        private GameObject potentialPickup = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/OptionPickup/OptionPickup.prefab").WaitForCompletion();
        private GameObject portalPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/PortalArena/PortalArena.prefab").WaitForCompletion();
        private GameObject rerollEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/LunarRecycler/LunarRerollEffect.prefab").WaitForCompletion();
        private GameObject woodShrine = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/ShrineHealing/ShrineHealing.prefab").WaitForCompletion();
        private GameObject shrineUseEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/VFX/ShrineUseEffect.prefab").WaitForCompletion();
        private GameObject blueFire = PrefabAPI.InstantiateClone(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/LunarRecycler/LunarRecycler.prefab").WaitForCompletion().transform.GetChild(2).gameObject, "BlueFireNux");

        private SpawnCard lockBox = Addressables.LoadAssetAsync<SpawnCard>("RoR2/Junk/TreasureCache/iscLockbox.asset").WaitForCompletion();
        private SpawnCard lockBoxVoid = Addressables.LoadAssetAsync<SpawnCard>("RoR2/DLC1/TreasureCacheVoid/iscLockboxVoid.asset").WaitForCompletion();
        private SpawnCard freeChest = Addressables.LoadAssetAsync<SpawnCard>("RoR2/DLC1/FreeChest/iscFreeChest.asset").WaitForCompletion();
        private SpawnCard greenPrinter = Addressables.LoadAssetAsync<SpawnCard>("RoR2/Base/DuplicatorLarge/iscDuplicatorLarge.asset").WaitForCompletion();
        private SpawnCard voidChest = Addressables.LoadAssetAsync<SpawnCard>("RoR2/DLC1/VoidChest/iscVoidChest.asset").WaitForCompletion();

        public BazaarHooks()
        {
            blueFire.AddComponent<NetworkIdentity>();
            On.RoR2.BazaarController.Start += BazaarController_Start;
            On.RoR2.PurchaseInteraction.OnInteractionBegin += PurchaseInteraction_OnInteractionBegin;
        }

        private void BazaarController_Start(On.RoR2.BazaarController.orig_Start orig, BazaarController self)
        {
            orig(self);
            if (Run.instance && Run.instance.name.Contains("Judgement"))
            {
                Run.instance.stageClearCount += 1;
                GameMode.JudgementRun judgementRun = Run.instance.gameObject.GetComponent<GameMode.JudgementRun>();
                GameObject holder = GameObject.Find("HOLDER: Store");
                if (holder)
                {
                    judgementRun.shouldGoBazaar = false;
                    GameObject portal = GameObject.Instantiate(portalPrefab, new Vector3(-128.6f, -25.4f, -14.4f), Quaternion.Euler(0, 90, 0));
                    NetworkServer.Spawn(portal);
                    if (judgementRun.availableHeals != 0)
                    {
                        GameObject shrine = GameObject.Instantiate(woodShrine, new Vector3(-112.0027f, -24f, -4.5843f), Quaternion.Euler(0, 180, 0));
                        shrine.GetComponent<PurchaseInteraction>().costType = CostTypeIndex.None;
                        shrine.GetComponent<PurchaseInteraction>().Networkcost = 0;
                        shrine.GetComponent<PurchaseInteraction>().Networkavailable = true;
                        shrine.GetComponent<PurchaseInteraction>().contextToken = "Full Heal Shrine (Limited Uses)";
                        shrine.GetComponent<PurchaseInteraction>().displayNameToken = "Full Heal Shrine (Limited Uses)";
                        NetworkServer.Spawn(shrine);
                    }
                    holder.transform.GetChild(2).gameObject.SetActive(false); // disable seershop
                    holder.transform.GetChild(3).gameObject.SetActive(false); // disable cauldrons
                    GameObject kickout = SceneInfo.instance.transform.Find("KickOutOfShop").gameObject;
                    if ((bool)kickout)
                    {
                        kickout.gameObject.SetActive(true);
                        kickout.transform.GetChild(8).gameObject.SetActive(false);
                    }
                    holder.transform.GetChild(0).GetChild(2).Rotate(0, 0, 50);

                    if (judgementRun.currentWave == 4 || judgementRun.currentWave == 8)
                    {
                        for (int i = 0; i < Run.instance.participatingPlayerCount; i++)
                        {
                            DirectorCore instance = DirectorCore.instance;
                            SpawnCard spawnCard = voidChest;
                            DirectorPlacementRule placementRule = new DirectorPlacementRule();
                            placementRule.placementMode = DirectorPlacementRule.PlacementMode.Direct;
                            placementRule.position = new Vector3(-90f, -25f, -11.5f);
                            Xoroshiro128Plus rng = judgementRun.bazaarRng;
                            DirectorSpawnRequest directorSpawnRequest = new DirectorSpawnRequest(spawnCard, placementRule, rng);
                            instance.TrySpawnObject(directorSpawnRequest);
                        }
                    }

                    int num = 0;
                    foreach (CharacterMaster readOnlyInstances in CharacterMaster.readOnlyInstancesList)
                    {
                        if (readOnlyInstances.inventory.GetItemCount(DLC1Content.Items.RegeneratingScrap) > 0)
                            ++num;
                    }
                    if (num > 0)
                    {
                        DirectorCore instance = DirectorCore.instance;
                        SpawnCard spawnCard = greenPrinter;
                        DirectorPlacementRule placementRule = new DirectorPlacementRule();
                        placementRule.placementMode = DirectorPlacementRule.PlacementMode.Direct;
                        placementRule.position = new Vector3(-108.7849f, -27f, -46.7452f);
                        Xoroshiro128Plus rng = judgementRun.bazaarRng;
                        DirectorSpawnRequest directorSpawnRequest = new DirectorSpawnRequest(spawnCard, placementRule, rng);
                        instance.TrySpawnObject(directorSpawnRequest);
                    }
                    int num2 = 0;
                    foreach (CharacterMaster readOnlyInstances in CharacterMaster.readOnlyInstancesList)
                    {
                        if (readOnlyInstances.inventory.GetItemCount(RoR2Content.Items.TreasureCache) > 0)
                            ++num2;
                    }
                    Vector3[] lockboxPositions = new Vector3[] { new Vector3(-103.7627f, -24.5f, -4.7243f), new Vector3(-101.7627f, -24.5f, -4.7243f), new Vector3(-105.7627f, -24.5f, -4.7243f), new Vector3(-107.7627f, -24.5f, -4.7243f) };
                    for (int i = 0; i < num2; i++)
                    {
                        DirectorCore instance = DirectorCore.instance;
                        SpawnCard spawnCard = lockBox;
                        DirectorPlacementRule placementRule = new DirectorPlacementRule();
                        placementRule.placementMode = DirectorPlacementRule.PlacementMode.Direct;
                        placementRule.position = lockboxPositions[i];
                        Xoroshiro128Plus rng = judgementRun.bazaarRng;
                        DirectorSpawnRequest directorSpawnRequest = new DirectorSpawnRequest(spawnCard, placementRule, rng);
                        instance.TrySpawnObject(directorSpawnRequest);
                    }
                    int num3 = 0;
                    foreach (CharacterMaster readOnlyInstances in CharacterMaster.readOnlyInstancesList)
                    {
                        if (readOnlyInstances.inventory.GetItemCount(DLC1Content.Items.TreasureCacheVoid) > 0)
                            ++num3;
                    }
                    Vector3[] lockboxVoidPositions = new Vector3[] { new Vector3(-89.5709f, -23.5f, -6.589f), new Vector3(-87.5709f, -23.5f, -6f), new Vector3(-85.5709f, -23.5f, -5.589f), new Vector3(-83.5709f, -23.5f, -5f) };
                    for (int i = 0; i < num3; i++)
                    {
                        DirectorCore instance = DirectorCore.instance;
                        SpawnCard spawnCard = lockBoxVoid;
                        DirectorPlacementRule placementRule = new DirectorPlacementRule();
                        placementRule.placementMode = DirectorPlacementRule.PlacementMode.Direct;
                        placementRule.position = lockboxVoidPositions[i];
                        Xoroshiro128Plus rng = judgementRun.bazaarRng;
                        DirectorSpawnRequest directorSpawnRequest = new DirectorSpawnRequest(spawnCard, placementRule, rng);
                        instance.TrySpawnObject(directorSpawnRequest);
                    }
                    int num4 = 0;
                    Vector3[] freeChestPositions = new Vector3[] { new Vector3(-122.9354f, -26f, -29.2073f), new Vector3(-123.8197f, -25.1055f, -22.2822f), new Vector3(-117.0709f, -24.2098f, -32.8076f), new Vector3(-110.2542f, -24.979f, -37.4319f) };
                    foreach (CharacterMaster readOnlyInstances in CharacterMaster.readOnlyInstancesList)
                    {
                        if (readOnlyInstances.inventory.GetItemCount(DLC1Content.Items.FreeChest) > 0)
                            ++num4;
                    }
                    for (int i = 0; i < num4; i++)
                    {
                        DirectorCore instance = DirectorCore.instance;
                        SpawnCard spawnCard = freeChest;
                        DirectorPlacementRule placementRule = new DirectorPlacementRule();
                        placementRule.placementMode = DirectorPlacementRule.PlacementMode.Direct;
                        placementRule.position = freeChestPositions[i];
                        Xoroshiro128Plus rng = judgementRun.bazaarRng;
                        DirectorSpawnRequest directorSpawnRequest = new DirectorSpawnRequest(spawnCard, placementRule, rng);
                        instance.TrySpawnObject(directorSpawnRequest);
                    };
                    for (int i = 0; i < holder.transform.GetChild(0).GetChild(2).childCount; i++)
                    {
                        Transform child = holder.transform.GetChild(0).GetChild(2).GetChild(i);
                        if (judgementRun.currentWave != 0 && judgementRun.currentWave != 4 && i == 2)
                        {
                            GameObject.Destroy(child.gameObject);
                            continue;
                        }
                        GameObject.Destroy(child.gameObject.GetComponent<RoR2.Hologram.HologramProjector>());
                        child.transform.GetChild(0).GetChild(0).GetChild(3).gameObject.SetActive(false);
                        GameObject fire = GameObject.Instantiate(blueFire, child.transform.GetChild(0).GetChild(0));
                        fire.transform.localPosition = new Vector3(0, 1.5f, 0);
                        NetworkServer.Spawn(fire);
                        child.gameObject.GetComponent<PurchaseInteraction>().costType = CostTypeIndex.None;
                    }
                    for (int i = 0; i < Run.instance.participatingPlayerCount - 1; i++)
                    {
                        GameObject newTable = GameObject.Instantiate(holder.transform.GetChild(0).GetChild(2), holder.transform.GetChild(0)).gameObject;
                        newTable.transform.localPosition = new Vector3(2.83f, -8.51f, -3.39f);
                        newTable.transform.localRotation = Quaternion.Euler(270, 330.3174f, 0);
                        NetworkServer.Spawn(newTable);
                    }
                    // getchild(0) lunar shop
                    // getchild(0)(2) table, disable all children
                    // item positions 
                    // new Vector3(-73.9124f, -24.0468f, -37.9145f)  new Vector3(-77.4559f, -24.0468f, -37.4419f)  new Vector3(-80.6413f, -24.0468f, -42.1104f) new Vector3(-79.2328f, -24.0468f, -45.2478f) |middle new Vector3(-80.0593f, -24.0468f, -39.2219f)|
                }
            }
        }

        private void PurchaseInteraction_OnInteractionBegin(On.RoR2.PurchaseInteraction.orig_OnInteractionBegin orig, PurchaseInteraction self, Interactor activator)
        {
            GameMode.JudgementRun judgementRun = Run.instance.gameObject.GetComponent<GameMode.JudgementRun>();
            if (Run.instance && Run.instance.name.Contains("Judgement") && self.name == "VoidChest(Clone)")
            {
                CharacterBody body = activator.GetComponent<CharacterBody>();

                if (judgementRun.persistentCurse.TryGetValue(body.master.netId, out int _))
                    judgementRun.persistentCurse[body.master.netId] += 20;
                else
                    judgementRun.persistentCurse.Add(body.master.netId, 20);

                for (int i = 0; i < 20; i++)
                    body.AddBuff(RoR2Content.Buffs.PermanentCurse);

            }
            if (Run.instance && Run.instance.name.Contains("Judgement") && self.name == "ShrineHealing(Clone)")
            {
                if (judgementRun.availableHeals == 0)
                    return;
                judgementRun.availableHeals -= 1;
                Chat.SendBroadcastChat(new Chat.SimpleChatMessage() { baseToken = "You have " + judgementRun.availableHeals + " heal left." });
                HealthComponent healthComponent = activator.GetComponent<CharacterBody>().healthComponent;
                healthComponent.health = healthComponent.fullHealth;
                EffectManager.SpawnEffect(shrineUseEffect, new EffectData()
                {
                    origin = self.transform.position,
                    rotation = Quaternion.identity,
                    scale = 1f,
                    color = Color.green
                }, true);
                return;
            }
            if (Run.instance && Run.instance.name.Contains("Judgement") && self.name == "DuplicatorLarge(Clone)")
            {
                int count = activator.GetComponent<CharacterBody>().inventory.GetItemCount(DLC1Content.Items.RegeneratingScrap);
                if (count == 0)
                    return;
            }
            if (Run.instance && Run.instance.name.Contains("Judgement") && (self.name == "LunarShopTerminal" || self.name == "LunarShopTerminal (1)"))
            {
                ShopTerminalBehavior shopTerminalBehavior = self.GetComponent<ShopTerminalBehavior>();
                self.GetComponent<PurchaseInteraction>().available = false;
                Vector3 velocity = shopTerminalBehavior.transform.TransformVector(shopTerminalBehavior.dropVelocity);
                judgementRun.purchaseCounter += 1;
                Util.PlaySound("Play_UI_tripleChestShutter", self.gameObject);
                EffectManager.SpawnEffect(rerollEffect, new EffectData()
                {
                    origin = self.gameObject.transform.GetChild(2).gameObject.transform.position,
                }, true);
                self.gameObject.transform.GetChild(0).GetChild(0).GetChild(4).gameObject.SetActive(false);
                if ((bool)shopTerminalBehavior.animator)
                {
                    int layerIndex = shopTerminalBehavior.animator.GetLayerIndex("Body");
                    shopTerminalBehavior.animator.PlayInFixedTime("Open", layerIndex);
                }
                Vector3 position = self.gameObject.transform.GetChild(0).GetChild(0).GetChild(2).gameObject.transform.position;

                if (judgementRun.currentWave == 0)
                {
                    switch (judgementRun.purchaseCounter)
                    {
                        case 1:
                            PickupDropletController.CreatePickupDroplet(new GenericPickupController.CreatePickupInfo()
                            {
                                pickupIndex = PickupCatalog.FindPickupIndex(ItemTier.Tier1),
                                pickerOptions = PickupPickerController.GenerateOptionsFromDropTable(3, dtWhite, judgementRun.bazaarRng),
                                rotation = Quaternion.identity,
                                prefabOverride = potentialPickup
                            }, position, velocity);
                            break;
                        case 2:
                            PickupDropletController.CreatePickupDroplet(new GenericPickupController.CreatePickupInfo()
                            {
                                pickupIndex = PickupCatalog.FindPickupIndex(ItemTier.Tier1),
                                pickerOptions = PickupPickerController.GenerateOptionsFromDropTable(3, dtWhite, judgementRun.bazaarRng),
                                rotation = Quaternion.identity,
                                prefabOverride = potentialPickup
                            }, position, velocity);
                            break;
                        case 3:
                            PickupDropletController.CreatePickupDroplet(new GenericPickupController.CreatePickupInfo()
                            {
                                pickupIndex = PickupCatalog.FindPickupIndex(ItemTier.Tier1),
                                pickerOptions = PickupPickerController.GenerateOptionsFromDropTable(3, dtWhite, judgementRun.bazaarRng),
                                rotation = Quaternion.identity,
                                prefabOverride = potentialPickup
                            }, position, velocity);
                            break;
                        case 4:
                            PickupDropletController.CreatePickupDroplet(new GenericPickupController.CreatePickupInfo()
                            {
                                pickupIndex = PickupCatalog.FindPickupIndex(ItemTier.Tier2),
                                pickerOptions = PickupPickerController.GenerateOptionsFromDropTable(3, dtGreen, judgementRun.bazaarRng),
                                rotation = Quaternion.identity,
                                prefabOverride = potentialPickup
                            }, position, velocity);
                            break;
                        case 5:
                            PickupDropletController.CreatePickupDroplet(new GenericPickupController.CreatePickupInfo()
                            {
                                pickupIndex = PickupCatalog.FindPickupIndex(EquipmentCatalog.FindEquipmentIndex("DroneBackup")),
                                pickerOptions = PickupPickerController.GenerateOptionsFromDropTable(3, dtEquip, judgementRun.bazaarRng),
                                rotation = Quaternion.identity,
                                prefabOverride = potentialPickup
                            }, position, velocity);
                            if (Run.instance.participatingPlayerCount > 1)
                                judgementRun.purchaseCounter = 0;
                            break;
                    }
                }
                else
                {
                    switch (judgementRun.purchaseCounter)
                    {
                        case 1:
                            PickupDropletController.CreatePickupDroplet(new GenericPickupController.CreatePickupInfo()
                            {
                                pickupIndex = PickupCatalog.FindPickupIndex(ItemTier.Tier1),
                                pickerOptions = PickupPickerController.GenerateOptionsFromDropTable(3, dtWhite, judgementRun.bazaarRng),
                                rotation = Quaternion.identity,
                                prefabOverride = potentialPickup
                            }, position, velocity);
                            break;
                        case 2:
                            PickupDropletController.CreatePickupDroplet(new GenericPickupController.CreatePickupInfo()
                            {
                                pickupIndex = PickupCatalog.FindPickupIndex(ItemTier.Tier2),
                                pickerOptions = PickupPickerController.GenerateOptionsFromDropTable(3, dtGreen, judgementRun.bazaarRng),
                                rotation = Quaternion.identity,
                                prefabOverride = potentialPickup
                            }, position, velocity);
                            break;
                        case 3:
                            PickupDropletController.CreatePickupDroplet(new GenericPickupController.CreatePickupInfo()
                            {
                                pickupIndex = PickupCatalog.FindPickupIndex(ItemTier.Tier1),
                                pickerOptions = PickupPickerController.GenerateOptionsFromDropTable(3, dtWhite, judgementRun.bazaarRng),
                                rotation = Quaternion.identity,
                                prefabOverride = potentialPickup
                            }, position, velocity);
                            break;
                        case 4:
                            if (judgementRun.currentWave == 4 || judgementRun.currentWave == 8)
                            {
                                PickupDropletController.CreatePickupDroplet(new GenericPickupController.CreatePickupInfo()
                                {
                                    pickupIndex = PickupCatalog.FindPickupIndex(ItemTier.Tier3),
                                    pickerOptions = PickupPickerController.GenerateOptionsFromDropTable(3, dtRed, judgementRun.bazaarRng),
                                    rotation = Quaternion.identity,
                                    prefabOverride = potentialPickup
                                }, position, velocity);
                            }
                            else
                            {
                                PickupDropletController.CreatePickupDroplet(new GenericPickupController.CreatePickupInfo()
                                {
                                    pickupIndex = PickupCatalog.FindPickupIndex(ItemTier.Tier2),
                                    pickerOptions = PickupPickerController.GenerateOptionsFromDropTable(3, dtGreen, judgementRun.bazaarRng),
                                    rotation = Quaternion.identity,
                                    prefabOverride = potentialPickup
                                }, position, velocity);
                                if (Run.instance.participatingPlayerCount > 1)
                                    judgementRun.purchaseCounter = 0;
                            }
                            break;
                        case 5:
                            if (judgementRun.currentWave == 4)
                            {
                                PickupDropletController.CreatePickupDroplet(new GenericPickupController.CreatePickupInfo()
                                {
                                    pickupIndex = PickupCatalog.FindPickupIndex(EquipmentCatalog.FindEquipmentIndex("DroneBackup")),
                                    pickerOptions = PickupPickerController.GenerateOptionsFromDropTable(3, dtEquip, judgementRun.bazaarRng),
                                    rotation = Quaternion.identity,
                                    prefabOverride = potentialPickup
                                }, position, velocity);
                                if (Run.instance.participatingPlayerCount > 1)
                                    judgementRun.purchaseCounter = 0;
                            }
                            break;
                    }
                }
            }
            else
                orig(self, activator);
        }
    }
}