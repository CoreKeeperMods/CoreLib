using System.Collections.Generic;
using System.Linq;
using CoreLib.Submodule.LootDrop.Patch;
using CoreLib.Util;
using PugMod;
using LootList = System.Collections.Generic.List<LootInfo>;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.LootDrop
{
    public class LootDropModule : BaseSubmodule
    {
        #region Public Interface
        
        public const string NAME = "Core Library - Loot Drop";
        
        internal static Logger log = new(NAME);
        
        /// Provides a singleton instance of the <see cref="LootDropModule"/> class.
        /// This property fetches the current instance of the module, ensuring it is properly loaded and accessible.
        /// The instance is used to perform operations such as adding, editing, or removing drop tables and their associated loot data.
        /// <remarks>
        /// The instance is retrieved internally using <c>CoreLibMod.GetModuleInstance&lt;DropTablesModule&gt;()</c>.
        /// Ensure that the module has been correctly initialized before attempting to access this property.
        /// Throws an exception if accessed while the module is not loaded.
        /// </remarks>
        internal static LootDropModule Instance => CoreLibMod.GetModuleInstance<LootDropModule>();

        /// Checks whether the specified loot table ID exists in the custom loot table mapping.
        /// <param name="lootTableId">The unique identifier of the loot table to check.</param>
        /// <returns>True if the specified loot table ID exists; otherwise, false.</returns>
        public static bool HasLootTableID(string lootTableId)
        {
            return customLootTableIdMap.ContainsKey(lootTableId);
        }

        /// Retrieves the LootTableID associated with the specified loot table identifier.
        /// <param name="lootTableId">The unique identifier of the loot table to retrieve.</param>
        /// <returns>The LootTableID associated with the given loot table identifier if it exists; otherwise, an empty LootTableID.</returns>
        public static LootTableID GetLootTableID(string lootTableId)
        {
            Instance.ThrowIfNotLoaded();
            if (customLootTableIdMap.ContainsKey(lootTableId))
            {
                return customLootTableIdMap[lootTableId];
            }

            log.LogWarning($"Requesting ID for loot table {lootTableId}, which is not registered!");
            return LootTableID.Empty;
        }

        /// Adds a new loot table to the system with default parameters.
        /// <param name="lootTableId">The unique identifier for the loot table to be added.</param>
        /// <returns>The identifier of the newly created loot table.</returns>
        public static LootTableID AddLootTable(string lootTableId)
        {
            return AddLootTable(lootTableId, 1, 1, false);
        }

        /// Adds a new loot table with the specified parameters.
        /// <param name="lootTableId">The identifier for the new loot table to be added.</param>
        /// <param name="minUnqiueDrops">The minimum number of unique drops allowed in the loot table.</param>
        /// <param name="maxUniqueDrops">The maximum number of unique drops allowed in the loot table.</param>
        /// <param name="dontAllowDuplicates">Specifies whether duplicate drops are allowed in the loot table.</param>
        /// <return>Returns the unique identifier representing the newly created loot table. Returns an empty identifier if the loot table ID is already registered.</return>
        public static LootTableID AddLootTable(string lootTableId, int minUnqiueDrops, int maxUniqueDrops, bool dontAllowDuplicates)
        {
            if (customLootTableIdMap.ContainsKey(lootTableId))
            {
                log.LogWarning($"Failed to add new loot table with id {lootTableId}, because table with this ID is already registered!");
                return LootTableID.Empty;
            }

            int lootTableIndex = lastCustomLootTableId;
            LootTableID lootTable = (LootTableID)lootTableIndex;
            lastCustomLootTableId++;
            customLootTables.Add(new CustomLootTableData(lootTable, minUnqiueDrops, maxUniqueDrops, dontAllowDuplicates));
            customLootTableIdMap.Add(lootTableId, lootTable);
            return lootTable;
        }

        /// Adds a new drop item to a specified loot table, ensuring it does not already exist in the table.
        /// <param name="tableID">The identifier of the loot table to which the drop item should be added.</param>
        /// <param name="info">The details of the drop item, including its item name, to be added to the loot table.</param>
        public static void AddNewDrop(LootTableID tableID, DropTableInfo info)
        {
            Instance.ThrowIfNotLoaded();
            DropTableModificationData data = GetModificationData(tableID);

            List<DropTableInfo> addInfos = data.addDrops;
            if (addInfos.All(tableInfo => tableInfo.itemName != info.itemName))
            {
                addInfos.Add(info);
                return;
            }

            log.LogWarning($"Trying to add new item {info.itemName} to drop table {tableID}, which is already added!");
        }

        /// Edits the specified drop item in a loot table, ensuring that duplicate modifications from other sources are not applied.
        /// <param name="tableID">The identifier of the loot table in which the drop item is to be edited.</param>
        /// <param name="info">The information about the drop item to be edited, including its item name.</param>
        public static void EditDrop(LootTableID tableID, DropTableInfo info)
        {
            Instance.ThrowIfNotLoaded();
            DropTableModificationData data = GetModificationData(tableID);

            List<DropTableInfo> editInfos = data.editDrops;
            if (editInfos.All(tableInfo => tableInfo.itemName != info.itemName))
            {
                editInfos.Add(info);
                return;
            }

            log.LogWarning($"Trying to edit item {info.itemName} in drop table {tableID}, but another mod is already editing it!");
        }

        /// Removes a specified item from the list of drops in the given loot table.
        /// <param name="tableID">The unique identifier of the loot table from which the drop should be removed.</param>
        /// <param name="item">The identifier of the item to remove from the loot table's drops.</param>
        public static void RemoveDrop(LootTableID tableID, ObjectID item)
        {
            Instance.ThrowIfNotLoaded();
            DropTableModificationData data = GetModificationData(tableID);
            List<ObjectID> removeInfos = data.removeDrops;
            if (!removeInfos.Contains(item))
            {
                removeInfos.Add(item);
            }
        }

        #endregion

        #region Private Implementation

        /// Sets up the necessary hooks for the DropTablesModule by applying patches to relevant classes or methods
        /// to modify or extend functionality related to loot tables.
        /// This method is expected to be called during module initialization to enable custom behavior or integrations.
        internal override void SetHooks()
        {
            CoreLibMod.Patch(typeof(LootTableBankPatch));
        }

        /// Represents a mapping of loot table identifiers to their corresponding modification data.
        /// This dictionary is primarily used to store and manage any updates, additions, or removals made to drop tables
        /// within the <see cref="LootDropModule"/>. Each entry contains the unique loot table identifier as the key
        /// and its respective <see cref="DropTableModificationData"/> as the value.
        /// <remarks>
        /// This mapping plays a crucial role in tracking changes to drop tables, facilitating the application of those
        /// modifications during gameplay or data persistence. Entries in this dictionary are updated when loot tables
        /// are edited, items are added or removed, or any other modification occurs.
        /// Access to this dictionary should be done with caution, as unintended changes may disrupt the consistency of
        /// the drop table system.
        /// </remarks>
        internal static Dictionary<LootTableID, DropTableModificationData> dropTableModification = new Dictionary<LootTableID, DropTableModificationData>();

        /// A dictionary used to associate custom loot table string identifiers with their corresponding
        /// <see cref="LootTableID"/> values.
        /// <remarks>
        /// This internal dictionary plays a key role in the management of custom loot tables within the system.
        /// It enables efficient registration, retrieval, and lookup of loot table identifiers, allowing for dynamic
        /// modifications and enhancements of the loot table functionality. The key represents a unique string identifier
        /// for each loot table, while the value is the mapped <see cref="LootTableID"/> used internally.
        /// </remarks>
        internal static Dictionary<string, LootTableID> customLootTableIdMap = new Dictionary<string, LootTableID>();

        /// Represents a collection of custom loot table data used within the drop tables system.
        /// This list stores all user-defined or dynamically generated loot tables, allowing for
        /// the addition and modification of custom loot entries in the game's loot system.
        /// <remarks>
        /// Entries added to this list are primarily utilized to extend or override the default loot tables
        /// provided by the base game. Modifications to this list can affect loot distribution and behavior
        /// when interacting with the game's loot system.
        /// </remarks>
        internal static List<CustomLootTableData> customLootTables = new List<CustomLootTableData>();

        /// Represents the last assigned custom loot table ID within the <see cref="LootDropModule"/> class.
        /// This variable is used to track and incrementally generate unique IDs for new custom loot tables
        /// added through the module.
        /// <remarks>
        /// The initial value is set to 2000 and is incremented each time a new custom loot table is added.
        /// This ensures that each custom loot table has a unique identifier.
        /// Modifications to this variable should only occur internally within the module to preserve ID integrity.
        /// </remarks>
        internal static int lastCustomLootTableId = 2000;

        /// Retrieves the modification data for the specified loot table ID. If no existing
        /// modification data is found, a new instance is created and stored for the given ID.
        /// <param name="tableID">The unique identifier of the loot table for which modification data is required.</param>
        /// <returns>The modification data associated with the specified loot table ID.</returns>
        private static DropTableModificationData GetModificationData(LootTableID tableID)
        {
            if (dropTableModification.ContainsKey(tableID))
            {
                return dropTableModification[tableID];
            }

            DropTableModificationData data = new DropTableModificationData();
            dropTableModification.Add(tableID, data);
            return data;
        }

        /// Removes specified drops from the provided loot lists based on the given modification data.
        /// <param name="lootInfos">The list of general loot items to process.</param>
        /// <param name="guaranteedLootInfos">The list of guaranteed loot items to process.</param>
        /// <param name="modificationData">The data containing the ObjectIDs of the drops to be removed.</param>
        internal static void RemoveDrops(LootList lootInfos, LootList guaranteedLootInfos, DropTableModificationData modificationData)
        {
            foreach (ObjectID objectID in modificationData.removeDrops)
            {
                lootInfos.RemoveAll(loot => loot.objectID == objectID);
                guaranteedLootInfos.RemoveAll(loot => loot.objectID == objectID);
            }
        }

        /// Edits existing drop items within a given loot table based on modification data.
        /// <param name="lootTable">The loot table containing the drops to be edited.</param>
        /// <param name="lootInfos">The list of loot items linked to the loot table to be checked and modified.</param>
        /// <param name="guaranteedLootInfos">The list of guaranteed loot items linked to the loot table to be checked and modified.</param>
        /// <param name="modificationData">The modification data detailing the drops to edit, including the items and their updated details.</param>
        internal static void EditDrops(LootTable lootTable, LootList lootInfos, LootList guaranteedLootInfos, DropTableModificationData modificationData)
        {
            foreach (DropTableInfo dropTableInfo in modificationData.editDrops)
            {
                bool editedAnything = false;
                var itemID = API.Authoring.GetObjectID(dropTableInfo.itemName);
                foreach (LootInfo lootInfo in lootInfos)
                {
                    if (lootInfo.objectID == itemID)
                    {
                        dropTableInfo.SetLootInfo(lootInfo);
                        editedAnything = true;
                        break;
                    }
                }

                foreach (LootInfo lootInfo in guaranteedLootInfos)
                {
                    if (lootInfo.objectID == itemID)
                    {
                        dropTableInfo.SetLootInfo(lootInfo);
                        editedAnything = true;
                        break;
                    }
                }

                if (!editedAnything)
                {
                    log.LogWarning($"Failed to edit droptable {lootTable.id}, item {dropTableInfo.itemName}, because such item was not found!");
                }
            }
        }

        /// Adds items from the modification data to the specified loot table's lists of loot items and guaranteed loot items.
        /// <param name="lootTable">The loot table to modify by adding new items.</param>
        /// <param name="lootInfos">The list of loot items associated with the loot table.</param>
        /// <param name="guaranteedLootInfos">The list of guaranteed loot items associated with the loot table.</param>
        /// <param name="modificationData">The modification data that contains the items to be added to the loot table.</param>
        internal static void AddDrops(LootTable lootTable, LootList lootInfos, LootList guaranteedLootInfos, DropTableModificationData modificationData)
        {
            foreach (DropTableInfo dropTableInfo in modificationData.addDrops)
            {
                var itemID = API.Authoring.GetObjectID(dropTableInfo.itemName);
                bool hasDrop = lootInfos.Exists(info => info.objectID == itemID);
                if (!hasDrop)
                {
                    lootInfos.Add(dropTableInfo.GetLootInfo());
                }
                else
                {
                    log.LogWarning($"Failed to add item {dropTableInfo.itemName} to droptable {lootTable.id}, because it already exists!");
                }

                hasDrop = guaranteedLootInfos.Exists(info => info.objectID == itemID);
                if (!hasDrop)
                {
                    guaranteedLootInfos.Add(dropTableInfo.GetLootInfo());
                }
                else
                {
                    log.LogWarning(
                        $"Failed to add item {dropTableInfo.itemName} to droptable (guaranteed) {lootTable.id}, because it already exists!");
                }
            }
        }

        #endregion
    }
}