using System.Collections.Generic;
using System.Linq;
using CoreLib.Drops.Patches;
using CoreLib.Util.Extensions;
using LootList = System.Collections.Generic.List<LootInfo>;

namespace CoreLib.Drops
{
    //TODO test the DropTablesModule
    public class DropTablesModule : BaseSubmodule
    {
        #region Public Interface

        internal override GameVersion Build => new GameVersion(0, 0, 0, 0, "");
        internal static DropTablesModule Instance => CoreLibMod.GetModuleInstance<DropTablesModule>();

        public static bool HasLootTableID(string lootTableId)
        {
            return customLootTableIdMap.ContainsKey(lootTableId);
        }
    
        public static LootTableID GetLootTableID(string lootTableId)
        {
            Instance.ThrowIfNotLoaded();
            if (customLootTableIdMap.ContainsKey(lootTableId))
            {
                return customLootTableIdMap[lootTableId];
            }
        
            CoreLibMod.Log.LogWarning($"Requesting ID for loot table {lootTableId}, which is not registered!");
            return LootTableID.Empty;
        }

        public static LootTableID AddLootTable(string lootTableId, AreaLevel biome)
        {
            return AddLootTable(lootTableId, biome, 1, 1);
        }
    
        public static LootTableID AddLootTable(string lootTableId, AreaLevel biome, int minUnqiueDrops, int maxUniqueDrops)
        {
            if (customLootTableIdMap.ContainsKey(lootTableId))
            {
                CoreLibMod.Log.LogWarning($"Failed to add new loot table with id {lootTableId}, because table with this ID is already registered!");
                return LootTableID.Empty;
            }

            int lootTableIndex = lastCustomLootTableId;
            LootTableID lootTable = (LootTableID)lootTableIndex;
            lastCustomLootTableId++;
            customLootTables.Add(new CustomLootTableData(biome, lootTable, minUnqiueDrops, maxUniqueDrops));
            customLootTableIdMap.Add(lootTableId, lootTable);
            return lootTable;
        }

        public static void AddNewDrop(LootTableID tableID, DropTableInfo info)
        {
            Instance.ThrowIfNotLoaded();
            DropTableModificationData data = GetModificationData(tableID);

            List<DropTableInfo> addInfos = data.addDrops;
            if (addInfos.All(tableInfo => tableInfo.item != info.item))
            {
                addInfos.Add(info);
                return;
            }

            CoreLibMod.Log.LogWarning($"Trying to add new item {info.item} to drop table {tableID}, which is already added!");
        }

        public static void EditDrop(LootTableID tableID, DropTableInfo info)
        {
            Instance.ThrowIfNotLoaded();
            DropTableModificationData data = GetModificationData(tableID);

            List<DropTableInfo> editInfos = data.editDrops;
            if (editInfos.All(tableInfo => tableInfo.item != info.item))
            {
                editInfos.Add(info);
                return;
            }

            CoreLibMod.Log.LogWarning($"Trying to edit item {info.item} in drop table {tableID}, but another mod is already editing it!");
        }

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

        internal override void SetHooks()
        {
            CoreLibMod.Patch(typeof(LootTableBank_Patch));
        }

        internal static Dictionary<LootTableID, DropTableModificationData> dropTableModification = new Dictionary<LootTableID, DropTableModificationData>();
        internal static Dictionary<string, LootTableID> customLootTableIdMap = new Dictionary<string, LootTableID>();
        internal static List<CustomLootTableData> customLootTables = new List<CustomLootTableData>();

        internal static int lastCustomLootTableId = 2000;
    
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

        internal static void RemoveDrops(LootList lootInfos, LootList guaranteedLootInfos, DropTableModificationData modificationData)
        {
            foreach (ObjectID objectID in modificationData.removeDrops)
            {
                lootInfos.RemoveAll(loot => loot.objectID == objectID);
                guaranteedLootInfos.RemoveAll(loot => loot.objectID == objectID);
            }
        }

        internal static void EditDrops(LootTable lootTable, LootList lootInfos, LootList guaranteedLootInfos, DropTableModificationData modificationData)
        {
            foreach (DropTableInfo dropTableInfo in modificationData.editDrops)
            {
                bool editedAnything = false;
                foreach (LootInfo lootInfo in lootInfos)
                {
                    if (lootInfo.objectID == dropTableInfo.item)
                    {
                        dropTableInfo.SetLootInfo(lootInfo);
                        editedAnything = true;
                        break;
                    }
                }

                foreach (LootInfo lootInfo in guaranteedLootInfos)
                {
                    if (lootInfo.objectID == dropTableInfo.item)
                    {
                        dropTableInfo.SetLootInfo(lootInfo);
                        editedAnything = true;
                        break;
                    }
                }

                if (!editedAnything)
                {
                    CoreLibMod.Log.LogWarning($"Failed to edit droptable {lootTable.id}, item {dropTableInfo.item}, because such item was not found!");
                }
            }
        }

        internal static void AddDrops(LootTable lootTable, LootList lootInfos, LootList guaranteedLootInfos, DropTableModificationData modificationData)
        {
            foreach (DropTableInfo dropTableInfo in modificationData.addDrops)
            {
                bool hasDrop = lootInfos.Exists(info => info.objectID == dropTableInfo.item);
                if (!hasDrop)
                {
                    lootInfos.Add(dropTableInfo.GetLootInfo());
                }
                else
                {
                    CoreLibMod.Log.LogWarning($"Failed to add item {dropTableInfo.item} to droptable {lootTable.id}, because it already exists!");
                }

                hasDrop = guaranteedLootInfos.Exists(info => info.objectID == dropTableInfo.item);
                if (!hasDrop)
                {
                    guaranteedLootInfos.Add(dropTableInfo.GetLootInfo());
                }
                else
                {
                    CoreLibMod.Log.LogWarning(
                        $"Failed to add item {dropTableInfo.item} to droptable (guaranteed) {lootTable.id}, because it already exists!");
                }
            }
        }

        #endregion
    }
}