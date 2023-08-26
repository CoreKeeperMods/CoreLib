using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CoreLib.Submodules.DropTables.Patches;
using LootList = System.Collections.Generic.List<LootInfo>;

namespace CoreLib.Submodules.DropTables
{
    [CoreLibSubmodule]
    public static class DropTablesModule
    {
        #region Public Interface

        /// <summary>
        /// Return true if the submodule is loaded.
        /// </summary>
        public static bool Loaded
        {
            get => _loaded;
            internal set => _loaded = value;
        }

        public static bool HasLootTableID(string lootTableId)
        {
            return customLootTableIdMap.ContainsKey(lootTableId);
        }
    
        public static LootTableID GetLootTableID(string lootTableId)
        {
            ThrowIfNotLoaded();
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
            ThrowIfNotLoaded();
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
            ThrowIfNotLoaded();
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
            ThrowIfNotLoaded();
            DropTableModificationData data = GetModificationData(tableID);
            List<ObjectID> removeInfos = data.removeDrops;
            if (!removeInfos.Contains(item))
            {
                removeInfos.Add(item);
            }
        }

        #endregion

        #region Private Implementation

        private static bool _loaded;

        [CoreLibSubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks()
        {
            CoreLibMod.harmony.PatchAll(typeof(LootTableBank_Patch));
        }
        
        internal static void ThrowIfNotLoaded()
        {
            if (!Loaded)
            {
                Type submoduleType = MethodBase.GetCurrentMethod().DeclaringType;
                string message = $"{submoduleType.Name} is not loaded. Please use [{nameof(CoreLibSubmoduleDependency)}(nameof({submoduleType.Name})]";
                throw new InvalidOperationException(message);
            }
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