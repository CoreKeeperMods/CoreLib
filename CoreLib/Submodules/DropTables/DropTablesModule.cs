using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CoreLib.Submodules.DropTables.Patches;
using CoreLib.Submodules.Localization.Patches;
using CoreLib.Util.Extensions;
using List = Il2CppSystem.Collections.Generic.List<LootInfo>;

namespace CoreLib.Submodules.DropTables;

[CoreLibSubmodule]
public static class DropTablesModule
{
    /// <summary>
    /// Return true if the submodule is loaded.
    /// </summary>
    public static bool Loaded
    {
        get => _loaded;
        internal set => _loaded = value;
    }

    private static bool _loaded;


    [CoreLibSubmoduleInit(Stage = InitStage.SetHooks)]
    internal static void SetHooks()
    {
        CoreLibPlugin.harmony.PatchAll(typeof(LootTableBank_Patch));
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

        CoreLibPlugin.Logger.LogWarning($"Trying to add new item {info.item} to drop table {tableID}, which is already added!");
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

        CoreLibPlugin.Logger.LogWarning($"Trying to edit item {info.item} in drop table {tableID}, but another mod is already editing it!");
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
    
    internal static void RemoveDrops(List lootInfos, List guaranteedLootInfos, DropTableModificationData modificationData)
    {
        foreach (ObjectID objectID in modificationData.removeDrops)
        {
            lootInfos.RemoveAll(loot => loot.objectID == objectID);
            guaranteedLootInfos.RemoveAll(loot => loot.objectID == objectID);
        }
    }
    
    internal static void EditDrops(LootTable lootTable, List lootInfos, List guaranteedLootInfos, DropTableModificationData modificationData)
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
                CoreLibPlugin.Logger.LogWarning($"Failed to edit droptable {lootTable.id}, item {dropTableInfo.item}, because such item was not found!");
            }
        }
    }

    internal static void AddDrops(LootTable lootTable, List lootInfos, List guaranteedLootInfos, DropTableModificationData modificationData)
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
                CoreLibPlugin.Logger.LogWarning($"Failed to add item {dropTableInfo.item} to droptable {lootTable.id}, because it already exists!");
            }

            hasDrop = guaranteedLootInfos.Exists(info => info.objectID == dropTableInfo.item);
            if (!hasDrop)
            {
                guaranteedLootInfos.Add(dropTableInfo.GetLootInfo());
            }
            else
            {
                CoreLibPlugin.Logger.LogWarning(
                    $"Failed to add item {dropTableInfo.item} to droptable (guaranteed) {lootTable.id}, because it already exists!");
            }
        }
    }

}