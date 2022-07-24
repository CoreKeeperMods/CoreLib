using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using CoreLib.Submodules.CustomEntity.Patches;
using CoreLib.Submodules.Localization;
using UnhollowerRuntimeLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CoreLib.Submodules.CustomEntity;

/// <summary>
/// This module provides means to add new content such as item.
/// Currently does not support adding blocks, NPCs and other non item entities!
/// </summary>
[CoreLibSubmodule(Dependencies = new[]{typeof(LocalizationModule)})]
public static class CustomEntityModule
{
    #region PublicInterface

    /// <summary>
    /// Return true if the submodule is loaded.
    /// </summary>
    public static bool Loaded
    {
        get => _loaded;
        internal set => _loaded = value;
    }

    /// <summary>
    /// Registers mod resources for loading
    /// </summary>
    /// <param name="resource"></param>
    public static void AddResource(ResourceData resource)
    {
        ThrowIfNotLoaded();
        modResources.Add(resource);
    }

    /// <summary>
    /// Load asset from mod asset bundles
    /// </summary>
    /// <param name="assetPath">path to the asset</param>
    public static Object LoadAsset(string assetPath)
    {
        ThrowIfNotLoaded();
        foreach (ResourceData resource in modResources)
        {
            if (!assetPath.ToLower().Contains(resource.keyWord.ToLower()) || !resource.HasAssetBundle()) continue;

            if (resource.bundle.Contains(assetPath.WithExtension(".prefab")))
            {
                Object prefab = resource.bundle.LoadAsset<GameObject>(assetPath.WithExtension(".prefab"));
                CoreLibPlugin.Logger.LogDebug($"Loading registered asset {assetPath}: {(prefab != null ? "Success" : "Failure")}");
                return prefab;
            }

            foreach (string extension in spriteFileExtensions)
            {
                if (!resource.bundle.Contains(assetPath.WithExtension(extension))) continue;

                Object sprite = resource.bundle.LoadAsset<Object>(assetPath.WithExtension(extension));

                CoreLibPlugin.Logger.LogDebug($"Loading registered asset {assetPath}: {(sprite != null ? "Success" : "Failure")}");

                return sprite;
            }

            foreach (string extension in audioClipFileExtensions)
            {
                if (!resource.bundle.Contains(assetPath.WithExtension(extension))) continue;

                Object audioClip = resource.bundle.LoadAsset<Object>(assetPath.WithExtension(extension));
                CoreLibPlugin.Logger.LogDebug($"Loading registered asset {assetPath}: {(audioClip != null ? "Success" : "Failure")}");
                return audioClip;
            }
        }

        CoreLibPlugin.Logger.LogWarning($"Failed to find asset '{assetPath}' in mod assets!");
        return Resources.Load(assetPath);
    }

    /// <summary>
    /// Get item index from UNIQUE item id
    /// </summary>
    /// <param name="itemID">UNIQUE string item ID</param>
    public static int GetItemIndex(string itemID)
    {
        if (modIDs.ContainsKey(itemID))
        {
            return modIDs[itemID];
        }

        return -1;
    }

    /// <summary>
    /// Add new Entity. Currently only supports adding new items.
    /// </summary>
    /// <param name="itemId">UNIQUE item id</param>
    /// <param name="path">path to your prefab in asset bundle</param>
    /// <returns>Added item integer index. If adding failed returns -1</returns>
    /// <exception cref="InvalidOperationException">Throws if called too late</exception>
    public static ObjectID AddEntity(string itemId, string path)
    {
        ThrowIfNotLoaded();
        if (hasInjected)
        {
            throw new InvalidOperationException($"AddEntity called too late. Entity injection already done. Prefab path: {path}");
        }

        Object gameObject = LoadAsset(path);
        if (gameObject == null)
        {
            CoreLibPlugin.Logger.LogInfo($"Failed to add entity, path: {path}");
            return ObjectID.None;
        }

        EntityMonoBehaviourData data = gameObject.Cast<GameObject>().GetComponent<EntityMonoBehaviourData>();

        if (!ValidateEntity(data))
        {
            CoreLibPlugin.Logger.LogError(
                "Trying to add a entity with EntityMonoBehavior! This is not supported yet! Please don't try to bypass this, it will not work!");
            return ObjectID.None;
        }

        int itemIndex = NextFreeId();
        itemIndex = modItemIDs.Bind("Items", itemId, itemIndex).Value;

        takenIDs.Add(itemIndex);
        modIDs.Add(itemId, itemIndex);

        data.objectInfo.objectID = (ObjectID)itemIndex;

        entitiesToAdd.Add(data);
        CoreLibPlugin.Logger.LogInfo($"Added entity {data.objectInfo.objectID}, path: {path}!");
        return (ObjectID) itemIndex;
    }

    /// <summary>
    /// Add I2 terms for entity name and description
    /// </summary>
    /// <param name="enName">Object name in English</param>
    /// <param name="enDesc">Object description in English</param>
    /// <param name="cnName">Object name in Chinese</param>
    /// <param name="cnDesc">Object description in Chinese</param>
    public static void AddEntityLocalization(ObjectID obj, string enName, string enDesc, string cnName = "", string cnDesc = "")
    {
        if (obj == ObjectID.None) return;
        
        LocalizationModule.AddTerm($"Items/{(int)obj}", enName, cnName);
        LocalizationModule.AddTerm($"Items/{(int)obj}Desc", enDesc, cnDesc);
    } 

    #endregion

    #region PrivateImplementation

    private static bool _loaded;

    internal static List<ResourceData> modResources = new List<ResourceData>();
    internal static List<EntityMonoBehaviourData> entitiesToAdd = new List<EntityMonoBehaviourData>();

    internal static ConfigFile modItemIDs;
    internal static HashSet<int> takenIDs = new HashSet<int>();
    internal static Dictionary<string, int> modIDs = new Dictionary<string, int>();

    public const int modIdRangeStart = 12000;
    public const int modIdRangeEnd = 13000;

    internal static int firstUnusedId = modIdRangeStart;

    internal static string[] spriteFileExtensions = { ".jpg", ".png", ".tif" };
    internal static string[] audioClipFileExtensions = { ".mp3", ".ogg", ".waw", ".aif" };

    internal static bool hasInjected;

    [CoreLibSubmoduleInit(Stage = InitStage.SetHooks)]
    internal static void SetHooks()
    {
        CoreLibPlugin.harmony.PatchAll(typeof(MemoryManager_Patch));
        CoreLibPlugin.harmony.PatchAll(typeof(PugDatabaseAuthoring_Patch));
    }

    [CoreLibSubmoduleInit(Stage = InitStage.PostLoad)]
    internal static void Load()
    {
        BepInPlugin metadata = MetadataHelper.GetMetadata(typeof(CoreLibPlugin));
        modItemIDs = new ConfigFile($"{Paths.ConfigPath}/CoreLib/CoreLib.ModItemID.cfg", true, metadata);
        
        ClassInjector.RegisterTypeInIl2Cpp<RuntimeMaterial>();
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

    private static bool IsIdFree(int id)
    {
        if (id is < modIdRangeStart or >= modIdRangeEnd)
        {
            return false;
        }

        return !takenIDs.Contains(id);
    }

    private static int NextFreeId()
    {
        if (IsIdFree(firstUnusedId))
        {
            int id = firstUnusedId;
            firstUnusedId++;
            return id;
        }
        else
        {
            while (!IsIdFree(firstUnusedId))
            {
                firstUnusedId++;
                if (firstUnusedId >= modIdRangeEnd)
                {
                    throw new InvalidOperationException("Reached last mod range id! Report this to CoreLib developers!");
                }
            }

            int id = firstUnusedId;
            firstUnusedId++;
            return id;
        }
    }

    private static bool ValidateEntity(EntityMonoBehaviourData data)
    {
        bool isValid = true;
        foreach (PrefabInfo prefabInfo in data.objectInfo.prefabInfos)
        {
            if (prefabInfo.prefab != null)
            {
                isValid = false;
                break;
            }
        }

        if (data.objectInfo.objectType is ObjectType.Creature or ObjectType.PlaceablePrefab or ObjectType.PlayerType)
        {
            isValid = false;
        }

        return isValid;
    }

    private static string WithExtension(this string path, string extension)
    {
        if (path.EndsWith(extension))
        {
            return path;
        }

        return path + extension;
    } 

    #endregion
}