namespace RespawningTreasureChests.LootPatches;

public static class TerminalCommand
{
    public static void InitTerminalCommands()
    {
        Terminal.ConsoleCommand GenerateYmlFile = new("write_treasure_chest_yml",
            "Writes to file a new yml file to use with treasure chest tweaks",
            args =>
            {
                TreasureChestData.CollectAndSaveTreasureChestData();
            });
    }
}