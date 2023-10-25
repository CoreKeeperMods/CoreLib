using LootList = System.Collections.Generic.List<LootInfo>;

namespace CoreLib.Drops
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