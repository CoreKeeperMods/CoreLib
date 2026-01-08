using System;
using HarmonyLib;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.LootDrop.Patch
{
    /// <summary>
    /// The LootTableBank_Patch class serves as a static patching utility within the loot table system of the CoreLib module.
    /// This class is intended to provide runtime modifications or extensions to the behavior of loot table logic.
    /// It is utilized within the DropTablesModule to apply Harmony patches to specific components related to loot management,
    /// enabling custom behaviors or augmentations.
    /// </summary>
    public static class LootTableBankPatch
    {
        /// <summary>
        /// A boolean flag determining if loot tables need to be edited or modified.
        /// When set to <c>true</c>, custom loot tables will be added and modifications
        /// to existing loot tables will be applied. After these operations are performed,
        /// this variable is set to <c>false</c> to prevent redundant re-editing.
        /// </summary>
        private static bool needsToEditLoot = true;

        /// Adds custom loot tables to the game's existing loot system and applies modifications as needed.
        /// This method intercepts the loot table conversion process using a Harmony prefix patch. It performs
        /// the following actions:
        /// 1. Adds new custom loot tables defined in the `DropTablesModule.customLootTables` to the game's loot table list.
        /// 2. Logs information about the addition of new loot for debugging or auditing purposes.
        /// 3. Iterates through all existing loot tables, applying modifications based on predefined data within
        /// `DropTablesModule.dropTableModification`.
        /// If modifications are required for a specific loot table:
        /// - Removes pre-existing loot data as specified in the modification criteria.
        /// - Modifies existing loot entries in accordance with the defined rules.
        /// - Appends new loot items to the relevant loot tables if specified.
        /// Captures and logs exceptions to ensure any errors do not disrupt the overall functionality of the loot system.
        /// Provides safe error handling and ensures modifications occur reliably.
        /// This process is executed only once per game session by setting the internal flag `needsToEditLoot` to false.
        /// Note:
        /// - Interacts with `DropTablesModule` to retrieve and handle custom loot and modification specifications.
        /// - Utilizes `CoreLibMod.Log` for logging updates on the modification process.
        /// - Designed to seamlessly extend the loot table system via a Harmony library patch.
        [HarmonyPatch(typeof(LootTableConverter), nameof(LootTableConverter.Convert))]
        [HarmonyPrefix]
        private static void AddCustomLootTables()
        {
            if (!needsToEditLoot) return;

            var lootList = Manager.mod.LootTable;

            LootDropModule.Log.LogInfo("Adding new loot!");
            foreach (CustomLootTableData tableData in LootDropModule.CustomLootTables)
            {
                lootList.Add(tableData.GetTable());
            }

            foreach (LootTable lootTable in lootList)
            {
                try
                {
                    if (LootDropModule.DropTableModification.ContainsKey(lootTable.id))
                    {
                        DropTableModificationData modificationData =
                            LootDropModule.DropTableModification[lootTable.id];
                        if (lootTable.lootInfos != null && lootTable.guaranteedLootInfos != null)
                        {
                            LootDropModule.RemoveDrops(lootTable.lootInfos, lootTable.guaranteedLootInfos,
                                modificationData);
                            LootDropModule.EditDrops(lootTable, lootTable.lootInfos, lootTable.guaranteedLootInfos,
                                modificationData);
                            LootDropModule.AddDrops(lootTable, lootTable.lootInfos, lootTable.guaranteedLootInfos,
                                modificationData);
                        }
                    }
                }
                catch (Exception e)
                {
                    LootDropModule.Log.LogWarning($"Failed to update loot tables:\n{e}");
                }
            }

            needsToEditLoot = false;
        }
    }
}