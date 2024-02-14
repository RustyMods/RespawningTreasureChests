using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using HarmonyLib;
using ServerSync;
using UnityEngine;
using YamlDotNet.Serialization;
using static RespawningTreasureChests.RespawningTreasureChestsPlugin;

namespace RespawningTreasureChests.LootPatches;

public static class TreasureChestData
{
    private static readonly CustomSyncedValue<string> SyncedTreasureChestData = new(RespawningTreasureChestsPlugin.ConfigSync, "TreasureChestData", "");
    
    private static readonly string outputFilePath = Paths.ConfigPath + Path.DirectorySeparatorChar + "TreasureChestData.yml";
    [Serializable]
    public class ChestData
    {
        public string target_prefab_name = null!;
        public string name = null!;
        public float health;
        public int width;
        public int height;
        public int drop_min;
        public int drop_max;
        public float drop_chance;
        public bool one_of_each;
        public List<DefaultItems> default_items = new List<DefaultItems>();
    }
    [Serializable]
    public class DefaultItems
    {
        public string item_name = null!;
        public int stack_min;
        public int stack_max;
        public float weight = 1f;
    }

    private static void OnTreasureDataFileChange(object sender, FileSystemEventArgs e)
    {
        if (!ZNet.instance.IsServer()) return;
        IDeserializer deserializer = new DeserializerBuilder().Build();
        string data = File.ReadAllText(outputFilePath);
        List<ChestData> ChestData = deserializer.Deserialize<List<ChestData>>(data);
        ContainerPatches.Patches.YMLChestData = ChestData;
        SyncedTreasureChestData.Value = data;
    }

    private static void OnServerSyncedDataChange()
    {
        IDeserializer deserializer = new DeserializerBuilder().Build();
        if (_RespawningMessagesConfigEntry.Value is Toggle.On)
        {
            RespawningTreasureChestsLogger.LogInfo("Client: Server treasure chest data changed, updating");
        }
        ContainerPatches.Patches.YMLChestData = deserializer.Deserialize<List<ChestData>>(SyncedTreasureChestData.Value);
    }

    private static void InitYaml()
    {
        if (_RespawningMessagesConfigEntry.Value is Toggle.On)
        {
            RespawningTreasureChestsLogger.LogInfo("Initializing yaml configurations");
        }
        ContainerPatches.Patches.YMLChestData = GetChestData();
        FileSystemWatcher SyncedYamlWatcher = new FileSystemWatcher(Paths.ConfigPath)
        {
            Filter = "TreasureChestData.yml",
            EnableRaisingEvents = true,
            IncludeSubdirectories = true,
            SynchronizingObject = ThreadingHelper.SynchronizingObject,
            NotifyFilter = NotifyFilters.LastWrite,
        };
        SyncedYamlWatcher.Created += OnTreasureDataFileChange;
        SyncedYamlWatcher.Changed += OnTreasureDataFileChange;

        SyncedTreasureChestData.ValueChanged += OnServerSyncedDataChange;
        
        TerminalCommand.InitTerminalCommands();
    }

    private static List<ChestData> GetTreasureChests()
    {
        HashSet<string> PrefabNames = new();
        List<ChestData> TreasureChestData = new List<ChestData>();
        GameObject[] array = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject obj in array)
        {
            if (!obj.name.StartsWith("TreasureChest_")) continue;
            if (obj.name.EndsWith("(Clone)")) continue;
            if (!obj.TryGetComponent(out Container container)) continue;
            if (!obj.TryGetComponent(out WearNTear wearNTear)) continue;
            if (PrefabNames.Contains(obj.name)) continue;
            ChestData data = new ChestData()
            {
                target_prefab_name = obj.name,
                name = container.m_name,
                health = wearNTear.m_health,
                width = container.m_width,
                height = container.m_height,
                default_items = new List<DefaultItems>()
            };

            if (container.m_defaultItems != null)
            {
                data.drop_min = container.m_defaultItems.m_dropMin;
                data.drop_max = container.m_defaultItems.m_dropMax;
                data.one_of_each = container.m_defaultItems.m_oneOfEach;
                data.drop_chance = container.m_defaultItems.m_dropChance;
                
                foreach (DropTable.DropData item in container.m_defaultItems.m_drops)
                {
                    DefaultItems itemData = new DefaultItems()
                    {
                        item_name = item.m_item.name,
                        stack_min = item.m_stackMin,
                        stack_max = item.m_stackMax,
                        weight = item.m_weight
                    };
                    data.default_items.Add(itemData);
                }
            }

            TreasureChestData.Add(data);
            PrefabNames.Add(data.target_prefab_name);
        }

        return TreasureChestData;
    }
    
    private static List<ChestData> GetChestData()
    {
        if (File.Exists(outputFilePath))
        {
            string data = File.ReadAllText(outputFilePath);
            Deserializer deserializer = new Deserializer();
            List<ChestData> fileData = deserializer.Deserialize<List<ChestData>>(data);
            if (fileData.Count == 0) return CollectAndSaveTreasureChestData();
            return fileData;
        }

        if (_RespawningMessagesConfigEntry.Value is Toggle.On)
        { 
            RespawningTreasureChestsLogger.LogInfo("Missing Treasure Chest YAML, generating...");
        }
        return CollectAndSaveTreasureChestData();
    }

    public static List<ChestData> CollectAndSaveTreasureChestData()
    {
        List<ChestData> data = GetTreasureChests();
        Serializer serializer = new Serializer();
        string yamlData = serializer.Serialize(data);
        File.WriteAllText(outputFilePath, yamlData);
        if (_RespawningMessagesConfigEntry.Value is Toggle.On)
        {
            RespawningTreasureChestsLogger.LogInfo("Treasure Chest data collected and saved to " + outputFilePath);
        }
        return data;
    }
    
    [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.Start))]
    private static class ZoneSystemStartPatch
    {
        private static void Postfix() => InitYaml();
    }
}