using System;
using HarmonyLib;
using LootList = System.Collections.Generic.List<LootInfo>;

namespace CoreLib.Drops.Patches
{
    public static class LootTableBank_Patch
    {
        [HarmonyPatch(typeof(LootTableBank), nameof(LootTableBank.OnAfterDeserialize))]
        [HarmonyPrefix]
        private static void AddCustomLootTables(LootTableBank __instance)
        {
            CoreLibMod.Log.LogInfo("Adding new loot!");
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

            foreach (BiomeLootTables biomeLootTable in __instance.biomeLootTables)
            {
                foreach (LootTable lootTable in biomeLootTable.lootTables)
                {
                    try
                    {
                        if (DropTablesModule.dropTableModification.ContainsKey(lootTable.id))
                        {
                            DropTableModificationData modificationData = DropTablesModule.dropTableModification[lootTable.id];
                            if (lootTable.lootInfos != null && lootTable.guaranteedLootInfos != null)
                            {
                                DropTablesModule.RemoveDrops(lootTable.lootInfos, lootTable.guaranteedLootInfos, modificationData);
                                DropTablesModule.EditDrops(lootTable, lootTable.lootInfos, lootTable.guaranteedLootInfos, modificationData);
                                DropTablesModule.AddDrops(lootTable, lootTable.lootInfos, lootTable.guaranteedLootInfos, modificationData);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        CoreLibMod.Log.LogWarning($"Failed to update loot tables:\n{e}");
                    }
                }
            }
        }
    }
}