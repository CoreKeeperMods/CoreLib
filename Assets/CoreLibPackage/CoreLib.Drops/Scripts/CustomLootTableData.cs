using LootList = System.Collections.Generic.List<LootInfo>;

namespace CoreLib.Drops
{
    /// <summary>
    /// Represents the configuration data for a custom loot table.
    /// </summary>
    /// <remarks>
    /// This struct is used to define the properties and behavior of a loot table, including its ID,
    /// the range of unique drops it can generate, and whether duplicate drops are allowed.
    /// </remarks>
    public struct CustomLootTableData
    {
        /// <summary>
        /// Represents the ID of the loot table associated with this instance of custom loot table data.
        /// This variable is used to uniquely identify a specific loot table within the system.
        /// </summary>
        public LootTableID tableId;

        /// <summary>
        /// Specifies the minimum number of unique loot drops that can be selected from the loot table.
        /// </summary>
        /// <remarks>
        /// This value ensures that at least the specified number of unique loot items will be dropped,
        /// depending on the configuration of the loot table and other parameters such as
        /// <see cref="dontAllowDuplicates"/> and <c>maxUniqueDrops</c>.
        /// </remarks>
        public int minUniqueDrops;

        /// <summary>
        /// Represents the maximum number of unique items that can be dropped
        /// from a custom loot table.
        /// </summary>
        public int maxUniqueDrops;

        /// <summary>
        /// Determines whether duplicate items are allowed in the loot table.
        /// </summary>
        /// <remarks>
        /// When set to <c>true</c>, the loot table will exclude duplicate items from being selected.
        /// When set to <c>false</c>, duplicate items may be included in the loot selection process.
        /// </remarks>
        public bool dontAllowDuplicates;

        /// CustomLootTableData is a data structure used to define a custom loot table within the CoreLib.Drops namespace.
        /// It encapsulates the configuration needed for a custom loot table such as the table's unique identifier,
        /// the minimum and maximum number of unique items that can be dropped, and whether duplicate items are allowed.
        public CustomLootTableData(LootTableID tableId, int minUniqueDrops, int maxUniqueDrops, bool dontAllowDuplicates)
        {
            this.tableId = tableId;
            this.minUniqueDrops = minUniqueDrops;
            this.maxUniqueDrops = maxUniqueDrops;
            this.dontAllowDuplicates = dontAllowDuplicates;
        }

        /// Generates and returns a new instance of a LootTable based on the current CustomLootTableData.
        /// The method initializes a new LootTable object with its properties populated using the
        /// associated values from the fields of the CustomLootTableData instance.
        /// <returns>
        /// A LootTable object containing the table ID, minimum unique drops, maximum unique
        /// drops, duplicate allowance, and lists of loot information.
        /// </returns>
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