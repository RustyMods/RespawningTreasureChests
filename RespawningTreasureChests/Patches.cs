using HarmonyLib;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx.Logging;

namespace RespawningTreasureChests
{
    public static class Hash
    { 
        public static readonly int chestRespawn = "ChestRespawn".GetStableHashCode();
    }
    public static class Patches
    {
        [HarmonyPatch(typeof(Container))]
        public class ContainerPatches
        {
            [HarmonyPatch(nameof(Container.OnContainerChanged)), HarmonyPostfix]
            static void SetRespawn(Container __instance)
            {
                var respawnToggle = RespawningTreasureChestsPlugin._RespawningChestConfigEntry.Value;
                if (respawnToggle == RespawningTreasureChestsPlugin.Toggle.Off) return;

                var respawnType = RespawningTreasureChestsPlugin._RespawnTypeConfigEntry.Value;
                string prefabNameContains = RespawningTreasureChestsPlugin._PrefabNameContainsConfigEntry.Value;
                
                string[] prefabNames = prefabNameContains.Split(',');
                foreach (string name in prefabNames)
                {
                    if (__instance.name.Contains(name))
                    {
                        var view = __instance.m_nview;
                        if (view == null || !view.IsValid()) return;

                        if (respawnType == RespawningTreasureChestsPlugin.Respawn.OnEmpty)
                        {
                            if (__instance.m_inventory.m_inventory.Count == 0)
                            {
                                DateTime currentTime = DateTime.Now;
                                view.GetZDO().Set(Hash.chestRespawn, currentTime.Ticks);
                            }
                        }
                        else
                        {
                            DateTime currentTime = DateTime.Now;
                            view.GetZDO().Set(Hash.chestRespawn, currentTime.Ticks);
                        }
                    }
                }
            }

            [HarmonyPatch(nameof(Container.CheckForChanges)), HarmonyPostfix]
            static void RespawnItems(Container __instance)
            {
                var respawnToggle = RespawningTreasureChestsPlugin._RespawningChestConfigEntry.Value;
                if (respawnToggle == RespawningTreasureChestsPlugin.Toggle.Off) return;
                
                var respawnType = RespawningTreasureChestsPlugin._RespawnTypeConfigEntry.Value;
                string prefabNameContains = RespawningTreasureChestsPlugin._PrefabNameContainsConfigEntry.Value;
                int respawnValue = RespawningTreasureChestsPlugin._RespawnTimeConfigEntry.Value;
                
                string[] prefabNames = prefabNameContains.Split(',');
                foreach (string name in prefabNames)
                {
                    if (__instance.name.Contains(name))
                    {
                        var view = __instance.m_nview;
                        if (!view || !view.IsValid()) return;
                        
                        long data = view.GetZDO().GetLong(Hash.chestRespawn, 0);
                        DateTime dataTime = new DateTime(data);
                        // Check if timer is set to max
                        if (dataTime.Ticks == DateTime.MaxValue.Ticks) return;
                        
                        if (DateTime.Now >= dataTime.AddMinutes(respawnValue))
                        {
                            if (respawnType == RespawningTreasureChestsPlugin.Respawn.OnEmpty)
                            {
                                __instance.m_nview.GetZDO().Set(Hash.chestRespawn, DateTime.MaxValue.Ticks);
                            }
                            else
                            {
                                __instance.m_nview.GetZDO().Set(Hash.chestRespawn, DateTime.Now.Ticks);
                            }
                            __instance.m_nview.GetZDO().Set(ZDOVars.s_addedDefaultItems, false);
                            __instance.AddDefaultItems();
                            __instance.m_nview.GetZDO().Set(ZDOVars.s_addedDefaultItems,  true);
                            RespawningTreasureChestsPlugin.RespawningTreasureChestsLogger.Log(LogLevel.Message, $"{__instance.name} items respawned");
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
        public class TreasureChestPatches
        {
            public static void Postfix(ZNetScene __instance)
            {
                string MeadowsData = RespawningTreasureChestsPlugin._TreasureChestMeadowsConfigEntry.Value;
                string MeadowsBuriedData = RespawningTreasureChestsPlugin._TreasureChestMeadowsBuriedConfigEntry.Value;
                string FCryptData = RespawningTreasureChestsPlugin._TreasureChestFCryptConfigEntry.Value;
                string TrollCaveData = RespawningTreasureChestsPlugin._TreasureChestTrollCaveConfigEntry.Value;
                string ForestCryptData = RespawningTreasureChestsPlugin._TreasureChestForestCryptConfigEntry.Value;
                string BlackForestData = RespawningTreasureChestsPlugin._TreasureChestBlackForestConfigEntry.Value;
                string SwampData = RespawningTreasureChestsPlugin._TreasureChestSwampConfigEntry.Value;
                string SunkenCryptData = RespawningTreasureChestsPlugin._TreasureChestSunkenCryptConfigEntry.Value;
                string MountainsData = RespawningTreasureChestsPlugin._TreasureChestMountainsConfigEntry.Value;
                string MountainCaveData = RespawningTreasureChestsPlugin._TreasureChestMountainCaveConfigEntry.Value;
                string HeathData = RespawningTreasureChestsPlugin._TreasureChestHeathConfigEntry.Value;
                string PlainsStoneData = RespawningTreasureChestsPlugin._TreasureChestPlainsStoneConfigEntry.Value;
                string DvergerTowerData = RespawningTreasureChestsPlugin._TreasureChestDvergerTowerConfigEntry.Value;
                string DvergerTownData = RespawningTreasureChestsPlugin._TreasureChestDvergerTownConfigEntry.Value;
                
                SetItemData(__instance, "TreasureChest_meadows", MeadowsData);
                SetItemData(__instance, "TreasureChest_meadows_buried", MeadowsBuriedData);
                SetItemData(__instance, "TreasureChest_fCrypt", FCryptData);
                SetItemData(__instance, "TreasureChest_trollcave", TrollCaveData);
                SetItemData(__instance, "TreasureChest_forestcrypt", ForestCryptData);
                SetItemData(__instance, "TreasureChest_blackforest", BlackForestData);
                SetItemData(__instance, "TreasureChest_swamp", SwampData);
                SetItemData(__instance, "TreasureChest_sunkencrypt", SunkenCryptData);
                SetItemData(__instance, "TreasureChest_mountains", MountainsData);
                SetItemData(__instance, "TreasureChest_mountaincave", MountainCaveData);
                SetItemData(__instance, "TreasureChest_heath", HeathData);
                SetItemData(__instance, "TreasureChest_plains_stone", PlainsStoneData);
                SetItemData(__instance, "TreasureChest_dvergrtower", DvergerTowerData);
                SetItemData(__instance, "TreasureChest_dvergrtown", DvergerTownData);
            }

            private static void SetItemData(ZNetScene scene, string prefabName, string itemData)
            {
                GameObject obj = scene.GetPrefab(prefabName);
                if (!obj) return;
                
                List<DropTable.DropData> drops = new List<DropTable.DropData>();
                foreach (string data in itemData.Split(':'))
                {
                    if (data.Length == 4)
                    {
                        string itemName = data.Split(',')[0];
                        string stackMin = data.Split(',')[1];
                        string stackMax = data.Split(',')[2];
                        string weight = data.Split(',')[3];
                        GameObject item = scene.GetPrefab(itemName);
                        if (!item) return;
                        drops.Add(new DropTable.DropData()
                        {
                            m_item = item,
                            m_stackMin = int.Parse(stackMin),
                            m_stackMax = int.Parse(stackMax),
                            m_weight = float.Parse(weight)
                        });
                    }
                }
                Container containerScript = obj.GetComponent<Container>();
                containerScript.m_defaultItems.m_drops = drops;
            }
        }
    }
}
