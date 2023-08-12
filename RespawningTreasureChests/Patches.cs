using HarmonyLib;
using System;
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
            private static void HandleRespawn(Container containerInstance, RespawningTreasureChestsPlugin.Respawn respawnType)
            {
                if (containerInstance.m_inventory.m_inventory.Count == 0 || respawnType != RespawningTreasureChestsPlugin.Respawn.OnEmpty)
                {
                    var currentTime = DateTime.Now;
                    containerInstance.m_nview.GetZDO().Set(Hash.chestRespawn, currentTime.Ticks);
                }
            }

            [HarmonyPatch(nameof(Container.OnContainerChanged)), HarmonyPostfix]
            static void SetRespawn(Container __instance)
            {
                var respawnToggle = RespawningTreasureChestsPlugin._RespawningChestConfigEntry.Value;
                if (respawnToggle == RespawningTreasureChestsPlugin.Toggle.Off) return;

                var respawnType = RespawningTreasureChestsPlugin._RespawnTypeConfigEntry.Value;
                var prefabNames = RespawningTreasureChestsPlugin._PrefabNameContainsConfigEntry.Value.Split(',');

                foreach (var name in prefabNames)
                {
                    if (__instance.name.Contains(name) && __instance.m_nview?.IsValid() == true)
                    {
                        HandleRespawn(__instance, respawnType);
                    }
                }
            }

            [HarmonyPatch(nameof(Container.CheckForChanges)), HarmonyPostfix]
            static void RespawnItems(Container __instance)
            {
                var respawnToggle = RespawningTreasureChestsPlugin._RespawningChestConfigEntry.Value;
                if (respawnToggle == RespawningTreasureChestsPlugin.Toggle.Off) return;

                var respawnMessages = RespawningTreasureChestsPlugin._RespawningMessagesConfigEntry.Value;
                var respawnType = RespawningTreasureChestsPlugin._RespawnTypeConfigEntry.Value;
                var prefabNames = RespawningTreasureChestsPlugin._PrefabNameContainsConfigEntry.Value.Split(',');
                var respawnDuration = TimeSpan.FromMinutes(RespawningTreasureChestsPlugin._RespawnTimeConfigEntry.Value);

                foreach (var name in prefabNames)
                {
                    if (__instance.name.Contains(name) && __instance.m_nview?.IsValid() == true)
                    {
                        var storedTimeTicks = __instance.m_nview.GetZDO().GetLong(Hash.chestRespawn, 0);
                        var storedTime = new DateTime(storedTimeTicks);

                        if (storedTime != DateTime.MaxValue && DateTime.Now >= storedTime + respawnDuration)
                        {
                            __instance.m_nview.GetZDO().Set(ZDOVars.s_addedDefaultItems, false);
                            if (respawnType == RespawningTreasureChestsPlugin.Respawn.OnEmpty)
                            {
                                __instance.m_nview.GetZDO().Set(Hash.chestRespawn, DateTime.MaxValue.Ticks);
                                __instance.AddDefaultItems();
                            }
                            else
                            {
                                __instance.m_nview.GetZDO().Set(Hash.chestRespawn, DateTime.Now.Ticks);
                                __instance.m_inventory.RemoveAll();
                                __instance.AddDefaultItems();
                            }
                            __instance.m_nview.GetZDO().Set(ZDOVars.s_addedDefaultItems,  true);
                            if (respawnMessages == RespawningTreasureChestsPlugin.Toggle.On)
                            {
                                RespawningTreasureChestsPlugin.RespawningTreasureChestsLogger.Log(LogLevel.Message, $"{__instance.name} items respawned");
                            }
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
                var dataMapping = new Dictionary<string, string>
                {
                    {"TreasureChest_meadows", RespawningTreasureChestsPlugin._TreasureChestMeadowsConfigEntry.Value },
                    {"TreasureChest_meadows_buried", RespawningTreasureChestsPlugin._TreasureChestMeadowsBuriedConfigEntry.Value}, 
                    {"TreasureChest_fcrypt", RespawningTreasureChestsPlugin._TreasureChestFCryptConfigEntry.Value},
                    {"TreasureChest_trollcave", RespawningTreasureChestsPlugin._TreasureChestTrollCaveConfigEntry.Value},
                    {"TreasureChest_forestcrypt", RespawningTreasureChestsPlugin._TreasureChestForestCryptConfigEntry.Value},
                    {"TreasureChest_blackforest", RespawningTreasureChestsPlugin._TreasureChestBlackForestConfigEntry.Value},
                    {"TreasureChest_swamp", RespawningTreasureChestsPlugin._TreasureChestSwampConfigEntry.Value},
                    {"TreasureChest_sunkencrypt", RespawningTreasureChestsPlugin._TreasureChestSunkenCryptConfigEntry.Value},
                    {"TreasureChest_mountains", RespawningTreasureChestsPlugin._TreasureChestMountainsConfigEntry.Value},
                    {"TreasureChest_mountaincave", RespawningTreasureChestsPlugin._TreasureChestMountainCaveConfigEntry.Value},
                    {"TreasureChest_heath", RespawningTreasureChestsPlugin._TreasureChestHeathConfigEntry.Value},
                    {"TreasureChest_plainsstone", RespawningTreasureChestsPlugin._TreasureChestPlainsStoneConfigEntry.Value},
                    {"TreasureChest_dvergertower", RespawningTreasureChestsPlugin._TreasureChestDvergerTowerConfigEntry.Value},
                    {"TreasureChest_dvergertown", RespawningTreasureChestsPlugin._TreasureChestDvergerTownConfigEntry.Value},
                };
                
                foreach (var entry in dataMapping)
                {
                    SetItemData(__instance, entry.Key, entry.Value);
                }
                
            }

            private static void SetItemData(ZNetScene scene, string prefabName, string itemData)
            {
                var obj = scene.GetPrefab(prefabName);
                if (!obj) return;
                
                var drops = ParseItemData(scene, itemData);
                var containerScript = obj.GetComponent<Container>();
                containerScript.m_defaultItems.m_drops = drops;
            }
            
            private static List<DropTable.DropData> ParseItemData(ZNetScene scene, string itemData)
            {
                var drops = new List<DropTable.DropData>();

                foreach (var data in itemData.Split(':'))
                {
                    var parts = data.Split(',');
                    if (parts.Length < 4) continue;

                    var item = scene.GetPrefab(parts[0]);
                    if (!item) continue;

                    drops.Add(new DropTable.DropData
                    {
                        m_item = item,
                        m_stackMin = int.TryParse(parts[1], out int stackMin) ? stackMin : 0,
                        m_stackMax = int.TryParse(parts[2], out int stackMax) ? stackMax : 0,
                        m_weight = float.TryParse(parts[3], out float weight) ? weight : 0f
                    });
                }

                return drops;
            }
        }
    }
}
