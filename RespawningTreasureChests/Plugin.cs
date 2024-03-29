﻿using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;
using ServerSync;

namespace RespawningTreasureChests
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class RespawningTreasureChestsPlugin : BaseUnityPlugin
    {
        internal const string ModName = "RespawningTreasureChests";
        internal const string ModVersion = "1.0.6";
        internal const string Author = "RustyMods";
        private const string ModGUID = Author + "." + ModName;
        private static string ConfigFileName = ModGUID + ".cfg";
        private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
        internal static string ConnectionError = "";
        private readonly Harmony _harmony = new(ModGUID);

        public static readonly ManualLogSource RespawningTreasureChestsLogger =
            BepInEx.Logging.Logger.CreateLogSource(ModName);

        public static readonly ConfigSync ConfigSync = new(ModGUID)
            { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };
        
        public enum Toggle
        {
            On = 1,
            Off = 0
        }

        public enum Respawn
        {
            OnEmpty = 1,
            OnRepeat = 0
        }

        public enum ConfigType
        {
            useConfig = 1,
            useYaml = 0
        }

        public void Awake()
        {
            _serverConfigLocked = config("1 - General", "Lock Configuration", Toggle.On,
                "If on, the configuration is locked and can be changed by server admins only.");
            _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);
            
            #region Respawning Configurations
            _RespawningChestConfigEntry = config(
                "2 - Respawn Configurations",
                "Chests Respawn Toggle",
                Toggle.On,
                "If on, chests respawn.");
            _RespawnTypeConfigEntry = config(
                "2 - Respawn Configurations",
                "Respawn Trigger",
                Respawn.OnEmpty,
                "Toggle for type of trigger to start respawning timer");
            _RespawningMessagesConfigEntry = config(
                "2 - Respawn Configurations",
                "Message log",
                Toggle.Off,
                "If you would like a messages to appear when chests item respawn"
            );
            _RespawnTimeConfigEntry = config(
                "2 - Respawn Configurations",
                "Respawn Time in minutes",
                60,
                "Timer in minutes when treasure chest emptied.");
            _PluginConfigTypeConfigEntry = config("2 - Respawn Configurations", "Config Input Type",
                ConfigType.useConfig,
                "For more control, use Yaml file generated in config folder and set this to use yaml");
            #endregion
            #region Treasure Chest Configurations
            _TreasureChestMeadowsConfigEntry = config(
                "3 - Treasure Chest Configurations",
                "Treasure Chest Meadows",
                "Feathers,1,3,1:Coins,5,15,1:Amber,1,1,1:ArrowFlint,10,20,1:Torch,1,1,1:Flint,2,4,1",
                "List of item drops");
            _TreasureChestMeadowsBuriedConfigEntry = config(
                "3 - Treasure Chest Configurations",
                "Treasure Chest Meadows Buried",
                "ArrowFire,10,15,1:Coins,20,50,1:Ruby,1,3,1:AmberPearl,1,2,1:SilverNecklace,1,1,1",
                "List of item drops");
            _TreasureChestFCryptConfigEntry = config(
                "3 - Treasure Chest Configurations",
                "Treasure Chest F Crypt",
                "Feathers,5,15,1:ArrowFlint,5,10,1:Coins,5,50,1:Amber,1,3,1",
                "List of item drops");
            _TreasureChestTrollCaveConfigEntry = config(
                "3 - Treasure Chest Configurations",
                "Treasure Chest Troll Cave",
                "Wood,10,30,1:Stone,10,30,1:Ruby,1,2,1:Coins,20,50,1:DeerHide,2,4,1:BoneFragments,10,15,1:LeatherScraps,3,5,1",
                "List of item drops");
            _TreasureChestForestCryptConfigEntry = config(
                "3 - Treasure Chest Configurations",
                "Treasure Chest Forest Crypt",
                "Feathers,1,10,1:ArrowFlint,5,10,1:Ruby,1,2,1:Amber,1,3,1",
                "List of item drops");
            _TreasureChestBlackForestConfigEntry = config(
                "3 - Treasure Chest Configurations",
                "Treasure Chest Black Forest Drops",
                "Feathers,2,4,1:ArrowFlint,5,10,1:Coins,5,30,1:Amber,1,2,1", 
                "List of item drops");
            _TreasureChestSwampConfigEntry = config(
                "3 - Treasure Chest Configurations",
                "Treasure Chest Swamp",
                "WitheredBone,1,1,0.5:ArrowIron,10,15,1:ArrowPoison,10,15,1:Coins,20,60,1:Amber,1,5,1:AmberPearl,1,3,1:Ruby,1,3,1:Chain,1,1,1:ElderBark,20,30,1",
                "List of item drops");
            _TreasureChestSunkenCryptConfigEntry = config(
                "3 - Treasure Chest Configurations",
                "Treasure Chest Sunken Crypt",
                "WitheredBone,1,1,0.5:ArrowIron,10,15,1:ArrowPoison,10,15,1:Coins,20,60,1:Amber,1,5,1:AmberPearl,1,3,1:Ruby,1,3,1:Chain,1,3,1:ElderBark,20,30,1:IronScrap,10,20,2",
                "List of item drops");
            _TreasureChestMountainsConfigEntry = config(
                "3 - Treasure Chest Configurations",
                "Treasure Chest Mountains",
                "OnionSeeds,3,9,1:Amber,1,6,1:Coins,46,96,1:AmberPearl,2,5,1:Ruby,1,2,1:Obsidian,5,10,1:ArrowFrost,5,10,1",
                "List of item drops");
            _TreasureChestMountainCaveConfigEntry = config(
                "3 - Treasure Chest Configurations",
                "Treasure Chest Mountain Cave",
                "Obsidian,3,9,0.5:SilverNecklace,1,1,0.1:Ruby,1,2,0.5:Coins,15,35,1:Coins,15,35,1:Obsidian,1,5,1",
                "List of item drops");
            _TreasureChestHeathConfigEntry = config(
                "3 - Treasure Chest Configurations",
                "Treasure Chest Heath",
                "Barley,2,4,0.5:BlackMetalScrap,2,5,1:Needle,2,5,1:Coins,10,40,1:SharpeningStone,1,1,0.1",
                "List of item drops");
            _TreasureChestPlainsStoneConfigEntry = config(
                "3 - Treasure Chest Configurations",
                "Treasure Chest Plains Stone",
                "Feathers,5,10,1:ArrowObsidian,5,10,1:SilverNecklace,1,1,0.5:Coins,66,99,1:GoblinTotem,1,1,0.1",
                "List of item drops");
            _TreasureChestDvergerTowerConfigEntry = config(
                "3 - Treasure Chest Configurations",
                "Treasure Chest Dvergr Tower",
                "Coins,12,54,1:Softtissue,1,8,1:DvergrNeedle,1,3,1",
                "List of item drops");
            _TreasureChestDvergerTownConfigEntry = config(
                "3 - Treasure Chest Configurations",
                "Treasure Chest Dvergr Town",
                "MeadHealthMinor,1,2,1:MeadStaminaMinor,1,2,1:MeadPoisonResist,1,2,1:Sausages,3,6,1:Tankard_dvergr,1,1,0.1:Coins,33,66,1:Coins,33,66,1",
                "List of item drops");
            #endregion
            
            Assembly assembly = Assembly.GetExecutingAssembly();
            _harmony.PatchAll(assembly);
            SetupWatcher();
        }
        private void OnDestroy()
        {
            Config.Save();
        }

        private void SetupWatcher()
        {
            FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) return;
            try
            {
                RespawningTreasureChestsLogger.LogDebug("ReadConfigValues called");
                Config.Reload();
            }
            catch
            {
                RespawningTreasureChestsLogger.LogError($"There was an issue loading your {ConfigFileName}");
                RespawningTreasureChestsLogger.LogError("Please check your config entries for spelling and format!");
            }
        }


        #region ConfigOptions

        private static ConfigEntry<Toggle> _serverConfigLocked = null!;

        public static ConfigEntry<Toggle> _RespawningChestConfigEntry = null!;
        public static ConfigEntry<int> _RespawnTimeConfigEntry = null!;
        public static ConfigEntry<Respawn> _RespawnTypeConfigEntry = null!;
        public static ConfigEntry<Toggle> _RespawningMessagesConfigEntry = null!;

        public static ConfigEntry<string> _TreasureChestMeadowsConfigEntry = null!;
        public static ConfigEntry<string> _TreasureChestMeadowsBuriedConfigEntry = null!;
        public static ConfigEntry<string> _TreasureChestFCryptConfigEntry = null!;
        public static ConfigEntry<string> _TreasureChestTrollCaveConfigEntry = null!;
        public static ConfigEntry<string> _TreasureChestForestCryptConfigEntry = null!;
        public static ConfigEntry<string> _TreasureChestBlackForestConfigEntry = null!;
        public static ConfigEntry<string> _TreasureChestSwampConfigEntry = null!;
        public static ConfigEntry<string> _TreasureChestSunkenCryptConfigEntry = null!;
        public static ConfigEntry<string> _TreasureChestMountainsConfigEntry = null!;
        public static ConfigEntry<string> _TreasureChestMountainCaveConfigEntry = null!;
        public static ConfigEntry<string> _TreasureChestHeathConfigEntry = null!;
        public static ConfigEntry<string> _TreasureChestPlainsStoneConfigEntry = null!;
        public static ConfigEntry<string> _TreasureChestDvergerTowerConfigEntry = null!;
        public static ConfigEntry<string> _TreasureChestDvergerTownConfigEntry = null!;

        public static ConfigEntry<ConfigType> _PluginConfigTypeConfigEntry = null!;

        private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
            bool synchronizedSetting = true)
        {
            ConfigDescription extendedDescription =
                new(
                    description.Description +
                    (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                    description.AcceptableValues, description.Tags);
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);
            //var configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        private ConfigEntry<T> config<T>(string group, string name, T value, string description,
            bool synchronizedSetting = true)
        {
            return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        }

        private class ConfigurationManagerAttributes
        {
            [UsedImplicitly] public int? Order;
            [UsedImplicitly] public bool? Browsable;
            [UsedImplicitly] public string? Category;
            [UsedImplicitly] public Action<ConfigEntryBase>? CustomDrawer;
        }

        class AcceptableShortcuts : AcceptableValueBase
        {
            public AcceptableShortcuts() : base(typeof(KeyboardShortcut))
            {
            }

            public override object Clamp(object value) => value;
            public override bool IsValid(object value) => true;

            public override string ToDescriptionString() =>
                "# Acceptable values: " + string.Join(", ", KeyboardShortcut.AllKeyCodes);
        }

        #endregion
    }
}