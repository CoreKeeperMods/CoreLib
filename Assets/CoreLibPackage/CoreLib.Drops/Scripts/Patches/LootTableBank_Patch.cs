using System;
using HarmonyLib;
using LootList = System.Collections.Generic.List<LootInfo>;

namespace CoreLib.Drops.Patches
{
    public static class LootTableBank_Patch
    {
        private static bool needsToEditLoot = true;
        
        [HarmonyPatch(typeof(LootTableConverter), nameof(LootTableConverter.Convert))]
        [HarmonyPrefix]
        private static void AddCustomLootTables()
        {
            if (!needsToEditLoot) return;
            
            var lootList = Manager.mod.LootTable;

            CoreLibMod.Log.LogInfo("Adding new loot!");
            foreach (CustomLootTableData tableData in DropTablesModule.customLootTables)
            {
                lootList.Add(tableData.GetTable());
            }

            foreach (LootTable lootTable in lootList)
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

            needsToEditLoot = false;
        }
    }
}