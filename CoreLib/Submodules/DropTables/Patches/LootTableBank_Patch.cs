using System;
using HarmonyLib;
using List = Il2CppSystem.Collections.Generic.List<LootInfo>;

namespace CoreLib.Submodules.DropTables.Patches;

public static class LootTableBank_Patch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(LootTableBank), "InitLoot")]
    public static void GetLootTableBank(LootTable lootTable, List lootInfos, int minUniqueDrops, int maxUniqueDrops, List guaranteedLootInfos)
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