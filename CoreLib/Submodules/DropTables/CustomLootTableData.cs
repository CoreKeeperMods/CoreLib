using LootList = Il2CppSystem.Collections.Generic.List<LootInfo>;

namespace CoreLib.Submodules.DropTables
{
    public struct CustomLootTableData
    {
        public LootTableID tableId;
        public int minUniqueDrops;
        public int maxUniqueDrops;

        public CustomLootTableData(LootTableID tableId, int minUniqueDrops, int maxUniqueDrops)
        {
            this.tableId = tableId;
            this.minUniqueDrops = minUniqueDrops;
            this.maxUniqueDrops = maxUniqueDrops;
        }

        public LootTable GetTable()
        {
            return new LootTable()
            {
                id = tableId,
                minUniqueDrops = minUniqueDrops,
                maxUniqueDrops = maxUniqueDrops,
                lootInfos = new LootList(),
                guaranteedLootInfos = new LootList()
            };
        }
    }
}