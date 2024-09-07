using LootList = System.Collections.Generic.List<LootInfo>;

namespace CoreLib.Drops
{
    public struct CustomLootTableData
    {
        public LootTableID tableId;
        public int minUniqueDrops;
        public int maxUniqueDrops;
        public bool dontAllowDuplicates;

        public CustomLootTableData(LootTableID tableId, int minUniqueDrops, int maxUniqueDrops, bool dontAllowDuplicates)
        {
            this.tableId = tableId;
            this.minUniqueDrops = minUniqueDrops;
            this.maxUniqueDrops = maxUniqueDrops;
            this.dontAllowDuplicates = dontAllowDuplicates;
        }

        public LootTable GetTable()
        {
            return new LootTable()
            {
                id = tableId,
                minUniqueDrops = minUniqueDrops,
                maxUniqueDrops = maxUniqueDrops,
                dontAllowDuplicates = dontAllowDuplicates,
                lootInfos = new LootList(),
                guaranteedLootInfos = new LootList()
            };
        }
    }
}