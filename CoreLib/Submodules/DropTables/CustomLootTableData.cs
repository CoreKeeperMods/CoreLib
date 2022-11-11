using LootList = Il2CppSystem.Collections.Generic.List<LootInfo>;

namespace CoreLib.Submodules.DropTables
{
    public struct CustomLootTableData
    {
        public AreaLevel biomeLevel;
        public LootTableID tableId;
        public int minUniqueDrops;
        public int maxUniqueDrops;

        public CustomLootTableData(AreaLevel biomeLevel, LootTableID tableId, int minUniqueDrops, int maxUniqueDrops)
        {
            this.biomeLevel = biomeLevel;
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