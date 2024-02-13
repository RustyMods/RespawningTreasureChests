using System.Collections.Generic;
using HarmonyLib;
using RespawningTreasureChests.LootPatches;
using UnityEngine;
using static RespawningTreasureChests.RespawningTreasureChestsPlugin;

namespace RespawningTreasureChests.ContainerPatches;

public static class Patches
{
    private static readonly int ContainerTime = "ContainerTimer".GetStableHashCode();
    private static readonly int RespawnContainer = "RespawnContainer".GetStableHashCode();

    private static readonly Dictionary<string, string> ConfigChestData = new ()
    {
        {"TreasureChest_meadows", _TreasureChestMeadowsConfigEntry.Value },
        {"TreasureChest_meadows_buried", _TreasureChestMeadowsBuriedConfigEntry.Value}, 
        {"TreasureChest_fcrypt", _TreasureChestFCryptConfigEntry.Value},
        {"TreasureChest_trollcave", _TreasureChestTrollCaveConfigEntry.Value},
        {"TreasureChest_forestcrypt", _TreasureChestForestCryptConfigEntry.Value},
        {"TreasureChest_blackforest", _TreasureChestBlackForestConfigEntry.Value},
        {"TreasureChest_swamp", _TreasureChestSwampConfigEntry.Value},
        {"TreasureChest_sunkencrypt", _TreasureChestSunkenCryptConfigEntry.Value},
        {"TreasureChest_mountains", _TreasureChestMountainsConfigEntry.Value},
        {"TreasureChest_mountaincave", _TreasureChestMountainCaveConfigEntry.Value},
        {"TreasureChest_heath", _TreasureChestHeathConfigEntry.Value},
        {"TreasureChest_plainsstone", _TreasureChestPlainsStoneConfigEntry.Value},
        {"TreasureChest_dvergertower", _TreasureChestDvergerTowerConfigEntry.Value},
        {"TreasureChest_dvergertown", _TreasureChestDvergerTownConfigEntry.Value},
    };
    public static List<TreasureChestData.ChestData> YMLChestData = new();

    [HarmonyPatch(typeof(Container), nameof(Container.OnContainerChanged))]
    private static class ContainerOnChangedPatch
    {
        private static void Postfix(Container __instance)
        {
            if (_RespawningChestConfigEntry.Value is Toggle.Off) return;
            if (!__instance) return;
            if (!__instance.m_nview.IsValid()) return;
            if (__instance.m_piece.IsPlacedByPlayer()) return;
            
            bool flag = __instance.m_nview.GetZDO().GetBool(RespawnContainer);

            switch (_RespawnTypeConfigEntry.Value)
            {
                case Respawn.OnRepeat:
                    if (flag) break;
                    if (_RespawningMessagesConfigEntry.Value is Toggle.On)
                    {
                        RespawningTreasureChestsLogger.LogInfo($"{__instance.name} changed, setting time");
                    }
                    __instance.m_nview.GetZDO().Set(ContainerTime, (long)ZNet.instance.GetTimeSeconds());
                    __instance.m_nview.GetZDO().Set(RespawnContainer, true);
                    break;
            }
        }
    }
    
    [HarmonyPatch(typeof(Container), nameof(Container.CheckForChanges))]
    private static class CheckForChangesPatch
    {
        private static void Postfix(Container __instance)
        {
            if (_RespawningChestConfigEntry.Value is Toggle.Off) return;
            if (!__instance) return;
            if (!__instance.m_nview.IsValid() || !ZNet.instance) return;
            if (__instance.m_piece.IsPlacedByPlayer()) return;

            bool flag = __instance.m_nview.GetZDO().GetBool(RespawnContainer);

            switch (_RespawnTypeConfigEntry.Value)
            {
                case Respawn.OnEmpty:
                    if (__instance.m_inventory.m_inventory.Count == 0 && !flag)
                    {
                        if (_RespawningMessagesConfigEntry.Value is Toggle.On)
                        {
                            RespawningTreasureChestsLogger.LogInfo($"{__instance.name} emptied, setting time");
                        }
                        __instance.m_nview.GetZDO().Set(ContainerTime, (long)ZNet.instance.GetTimeSeconds());
                        __instance.m_nview.GetZDO().Set(RespawnContainer, true);
                    }
                    break;
            }

            if (!flag) return;
            long lastEmptied = __instance.m_nview.GetZDO().GetLong(ContainerTime);
            if (lastEmptied + (_RespawnTimeConfigEntry.Value * 60) >= (long)ZNet.instance.GetTimeSeconds()) return;
            if (__instance.m_inventory.m_inventory.Count > 0) __instance.m_inventory.RemoveAll();
            
            switch (_PluginConfigTypeConfigEntry.Value)
            {
                case ConfigType.useYaml:
                    TreasureChestData.ChestData YmlData = YMLChestData.Find(x => x.target_prefab_name == __instance.name.Replace("(Clone)",""));
                    if (YmlData == null) break;
                    SetYmlChestData(YmlData, __instance);
                    break;
                case ConfigType.useConfig:
                    if (!ConfigChestData.TryGetValue(__instance.name.Replace("(Clone)",""), out string ConfigData)) break;
                    SetConfigChestData(ConfigData, __instance);
                    break;
            }
            
            __instance.AddDefaultItems();
            __instance.m_nview.GetZDO().Set(ContainerTime, (long)ZNet.instance.GetTimeSeconds());
            __instance.m_nview.GetZDO().Set(RespawnContainer, false);
            if (_RespawningMessagesConfigEntry.Value is Toggle.On)
            {
                RespawningTreasureChestsLogger.LogInfo($"{__instance.name}: Items respawned");
            }
        }
    }
    
    private static void SetYmlChestData(TreasureChestData.ChestData data, Container container)
    {
        RespawningTreasureChestsLogger.LogDebug($"Setting values for: {container.name}");
        if (!container.TryGetComponent(out WearNTear wearNTear)) return;
        container.m_name = data.name;
        wearNTear.m_health = data.health;
        container.m_width = data.width;
        container.m_height = data.height;
        container.m_defaultItems.m_dropMin = data.drop_min;
        container.m_defaultItems.m_dropMax = data.drop_max;
        container.m_defaultItems.m_dropChance = data.drop_chance;
        container.m_defaultItems.m_oneOfEach = data.one_of_each;

        List<DropTable.DropData> drops = new();
        foreach (TreasureChestData.DefaultItems drop in data.default_items)
        {
            GameObject item = ZNetScene.instance.GetPrefab(drop.item_name);
            if (!item)
            {
                if (_RespawningMessagesConfigEntry.Value is Toggle.On)
                {
                    RespawningTreasureChestsLogger.LogWarning($"Failed to modify {data.target_prefab_name}, {drop.item_name} cannot be found.");
                }
                continue;
            }
            drops.Add(new DropTable.DropData()
            {
                m_item = item,
                m_stackMin = drop.stack_min,
                m_stackMax = drop.stack_max,
                m_weight = drop.weight
            });
        }

        container.m_defaultItems.m_drops = drops;
    }

    private static void SetConfigChestData(string data, Container container)
    {
        RespawningTreasureChestsLogger.LogDebug($"Setting values for: {container.name}");
        List<DropTable.DropData> drops = new();
        foreach (string item in data.Split(':'))
        {
            string[] parts = item.Split(',');
            if (parts.Length != 4)
            {
                RespawningTreasureChestsLogger.LogDebug($"Wrong config format: {container.name}");
                continue;
            }

            GameObject prefab = ZNetScene.instance.GetPrefab(parts[0]);
            if (!prefab)
            {
                if (_RespawningMessagesConfigEntry.Value is Toggle.On)
                {
                    RespawningTreasureChestsLogger.LogWarning($"Failed to find item: {parts[0]}");
                }
                continue;
            }
            drops.Add(new DropTable.DropData
            {
                m_item = prefab,
                m_stackMin = int.TryParse(parts[1], out int stackMin) ? stackMin : 0,
                m_stackMax = int.TryParse(parts[2], out int stackMax) ? stackMax : 0,
                m_weight = float.TryParse(parts[3], out float weight) ? weight : 1f
            });
        }
        container.m_defaultItems.m_drops = drops;
    }
}