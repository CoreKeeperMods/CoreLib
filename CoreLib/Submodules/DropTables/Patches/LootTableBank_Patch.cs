using System;
using HarmonyLib;
using LootList = Il2CppSystem.Collections.Generic.List<LootInfo>;

namespace CoreLib.Submodules.DropTables.Patches;

public static class LootTableBank_Patch
{
    [HarmonyPatch(typeof(LootTableBank), nameof(LootTableBank.OnAfterDeserialize))]
    [HarmonyPrefix]
    private static void AddCustomLootTables(LootTableBank __instance)
    {
        foreach (CustomLootTableData tableData in DropTablesModule.customLootTables)
        {
            LootTable lootTable = tableData.GetTable();

            foreach (BiomeLootTables biomeLootTables in __instance.biomeLootTables)
            {
                if (biomeLootTables.biomeLevel == tableData.biomeLevel)
                {
                    biomeLootTables.lootTables.Add(lootTable);
                    break;
                }
            }
        }
    }
    
    
    [HarmonyPatch(typeof(LootTableBank), nameof(LootTableBank.InitLoot))]
    [HarmonyPrefix]
    public static void GetLootTableBank(LootTable lootTable, LootList lootInfos, int minUniqueDrops, int maxUniqueDrops, LootList guaranteedLootInfos)
    {
        try
        {
            if (DropTablesModule.dropTableModification.ContainsKey(lootTable.id))
            {
                DropTableModificationData modificationData = DropTablesModule.dropTableModification[lootTable.id];
                if (lootInfos != null && guaranteedLootInfos != null)
                {
                    DropTablesModule.RemoveDrops(lootInfos, guaranteedLootInfos, modificationData);
                    DropTablesModule.EditDrops(lootTable, lootInfos, guaranteedLootInfos, modificationData);
                    DropTablesModule.AddDrops(lootTable, lootInfos, guaranteedLootInfos, modificationData);
                }
            }
        }
        catch (Exception e)
        {
            CoreLibPlugin.Logger.LogWarning($"Failed to update loot tables:\n{e}");
        }
    }
}