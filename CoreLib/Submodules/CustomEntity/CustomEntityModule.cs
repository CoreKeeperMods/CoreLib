using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using CoreLib.Submodules.CustomEntity.Atributes;
using CoreLib.Submodules.CustomEntity.Patches;
using CoreLib.Submodules.Localization;
using CoreLib.Submodules.ModResources;
using CoreLib.Util.Extensions;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using PugTilemap;
using PugTilemap.Quads;
using PugTilemap.Workshop;
using Unity.NetCode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CoreLib.Submodules.CustomEntity;

/// <summary>
/// This module provides means to add new content such as item.
/// Currently does not support adding blocks, NPCs and other non item entities!
/// </summary>
[CoreLibSubmodule(Dependencies = new[] { typeof(LocalizationModule), typeof(ResourcesModule) })]
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
    /// <see cref="ResourcesModule"/>
    /// </summary>
    /// <param name="resource"></param>
    [Obsolete("Use ResourcesModule.AddResource() instead")]
    public static void AddResource(ResourceData resource)
    {
        ThrowIfNotLoaded();
        ResourcesModule.AddResource(resource);
    }

    /// <summary>
    /// Register you entity modifications methods.
    /// </summary>
    /// <param name="assembly">Assembly to analyze</param>
    public static void RegisterModifications(Assembly assembly)
    {
        ThrowIfNotLoaded();
        ThrowIfTooLate(nameof(RegisterModifications));

        RegisterModifications_Internal(assembly);
    }

    /// <summary>
    /// Register you entity modifications methods.
    /// </summary>
    /// <param name="type">Type to analyze</param>
    public static void RegisterModifications(Type type)
    {
        ThrowIfNotLoaded();
        ThrowIfTooLate(nameof(RegisterModifications));

        RegisterModificationsInType_Internal(type);
    }


    /// <summary>
    /// Get objectID from UNIQUE entity id
    /// </summary>
    /// <param name="itemID">UNIQUE string entity ID</param>
    public static ObjectID GetObjectId(string itemID)
    {
        ThrowIfNotLoaded();

        return (ObjectID)modItemIDs.GetIndex(itemID);
    }

    /// <summary>
    /// Get Tileset from UNIQUE tilset id
    /// </summary>
    /// <param name="itemID">UNIQUE string tilset ID</param>
    public static Tileset GetTilesetId(string itemID)
    {
        ThrowIfNotLoaded();

        return (Tileset)tilesetIDs.GetIndex(itemID);
    }

    /// <summary>
    /// Add custom workbench with specified sprite. It is automatically added to main mod workbench
    /// </summary>
    /// <param name="itemId">UNIQUE entity Id</param>
    /// <param name="spritePath">path to your sprite in asset bundle</param>
    public static ObjectID AddModWorkbench(string itemId, string spritePath)
    {
        return AddModWorkbench(itemId, spritePath, null);
    }

    /// <summary>
    /// Add custom workbench with specified sprite. It is automatically added to main mod workbench
    /// </summary>
    /// <param name="itemId">UNIQUE entity Id</param>
    /// <param name="spritePath">path to your sprite in asset bundle</param>
    /// <param name="recipe">workbench craft recipe</param>
    public static ObjectID AddModWorkbench(string itemId, string spritePath, List<CraftingData> recipe)
    {
        ThrowIfNotLoaded();
        ThrowIfTooLate(nameof(AddModWorkbench));
        ObjectID workbenchId = AddWorkbench(itemId, spritePath, recipe);
        AddWorkbenchItem(rootWorkbenches.Last(), workbenchId);
        return workbenchId;
    }


    /// <summary>
    /// Add entity to workbench. This will allow player to craft it
    /// </summary>
    /// <param name="workBenchId">Target workbench id</param>
    /// <param name="entityId">entity Id</param>
    public static void AddWorkbenchItem(ObjectID workBenchId, ObjectID entityId)
    {
        ThrowIfNotLoaded();
        ThrowIfTooLate(nameof(AddModWorkbench));
        if (GetMainEntity(workBenchId, out EntityMonoBehaviourData entity))
        {
            CraftingCDAuthoring craftingCdAuthoring = entity.gameObject.GetComponent<CraftingCDAuthoring>();
            bool isRootWorkbench = IsRootWorkbench(workBenchId);

            CoreLibPlugin.Logger.LogDebug($"Adding item {entityId.ToString()} to workbench {workBenchId.ToString()}");

            if (craftingCdAuthoring.canCraftObjects.Count < (isRootWorkbench ? 17 : 18))
            {
                craftingCdAuthoring.canCraftObjects.Add(new CraftableObject() { objectID = entityId, amount = 1 });
                return;
            }

            if (isRootWorkbench)
            {
                ObjectID newWorkbench = AddRootWorkbench();
                craftingCdAuthoring.canCraftObjects.Insert(0, new CraftableObject() { objectID = newWorkbench, amount = 1 });
                AddWorkbenchItem(newWorkbench, entityId);
                return;
            }

            CoreLibPlugin.Logger.LogWarning($"Workbench {workBenchId.ToString()} has ran out of slots!");
            return;
        }

        CoreLibPlugin.Logger.LogError($"Failed to add workbench item! Found no entities in the list with ID: {workBenchId}.");
    }


    /// <summary>
    /// Add new entity.
    /// </summary>
    /// <param name="itemId">UNIQUE entity id</param>
    /// <param name="prefabPath">path to your prefab in asset bundle</param>
    /// <returns>Added objectID. If adding failed returns <see cref="ObjectID.None"/></returns>
    /// <exception cref="InvalidOperationException">Throws if called too late</exception>
    public static ObjectID AddEntity(string itemId, string prefabPath)
    {
        return AddEntityWithVariations(itemId, new[] { prefabPath });
    }

    /// <summary>
    /// Add new entity with variations. Each prefab must have variation field set.
    /// </summary>
    /// <param name="itemId">UNIQUE entity id</param>
    /// <param name="prefabsPaths">paths to your prefabs in asset bundle</param>
    /// <returns>Added objectID. If adding failed returns <see cref="ObjectID.None"/></returns>
    /// <exception cref="InvalidOperationException">Throws if called too late</exception>
    public static ObjectID AddEntityWithVariations(string itemId, string[] prefabsPaths)
    {
        ThrowIfNotLoaded();
        ThrowIfTooLate(nameof(AddEntityWithVariations));

        if (prefabsPaths.Length == 0)
        {
            CoreLibPlugin.Logger.LogError($"Failed to add entity {itemId}: prefabsPaths has no paths!");
            return ObjectID.None;
        }

        List<EntityMonoBehaviourData> entities = new List<EntityMonoBehaviourData>(prefabsPaths.Length);

        foreach (string prefabPath in prefabsPaths)
        {
            try
            {
                EntityMonoBehaviourData entity = LoadPrefab(itemId, prefabPath);
                entities.Add(entity);
            }
            catch (ArgumentException)
            {
                CoreLibPlugin.Logger.LogError($"Failed to add entity {itemId}, prefab {prefabPath} is missing!");
                return ObjectID.None;
            }
        }

        entities.Sort((a, b) => a.objectInfo.variation.CompareTo(b.objectInfo.variation));

        int itemIndex = modItemIDs.GetNextId(itemId);
        ObjectID objectID = (ObjectID)itemIndex;


        foreach (EntityMonoBehaviourData entity in entities)
        {
            entity.objectInfo.objectID = objectID;
        }

        entitiesToAdd.Add(objectID, entities);
        CoreLibPlugin.Logger.LogDebug($"Added entity {itemId} as objectID: {objectID}!");
        return objectID;
    }

    /// <summary>
    /// Add one or more custom tilesets. Prefab must be <see cref="MapWorkshopTilesetBank"/> with fields 'friendlyName' set to tileset ids
    /// </summary>
    /// <param name="tilesetPath">path to your prefab in asset bundle</param>
    /// <exception cref="ArgumentException">If provided prefab was not found</exception>
    /// <exception cref="InvalidOperationException">Throws if called too late</exception>
    public static void AddCustomTileset(string tilesetPath)
    {
        ThrowIfNotLoaded();
        ThrowIfTooLate(nameof(AddCustomTileset));

        MapWorkshopTilesetBank tilesetBank = ResourcesModule.LoadAsset<MapWorkshopTilesetBank>(tilesetPath);
        if (tilesetBank == null)
        {
            throw new ArgumentException($"Not found MapWorkshopTilesetBank asset at path: {tilesetPath}");
        }

        foreach (MapWorkshopTilesetBank.Tileset tileset in tilesetBank.tilesets)
        {
            try
            {
                int itemIndex = tilesetIDs.GetNextId(tileset.friendlyName);
                Tileset tilesetID = (Tileset)itemIndex;

                if (tilesetLayers.ContainsKey(tileset.layers.name))
                {
                    tileset.layers = tilesetLayers[tileset.layers.name];
                    CoreLibPlugin.Logger.LogDebug($"Replacing tileset {tileset.friendlyName} layers config with default layers {tileset.layers.name}");
                }

                customTilesets.Add(tilesetID, tileset);
                CoreLibPlugin.Logger.LogDebug($"Added tileset {tileset.friendlyName} as TilesetID: {tilesetID}!");
            }
            catch (Exception e)
            {
                CoreLibPlugin.Logger.LogWarning($"Failed to add tileset {tileset.friendlyName}:\n{e}");
            }
        }
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

    /// <summary>
    /// Add custom customization texture sheet
    /// </summary>
    /// <param name="skin">Class with texture sheet information</param>
    /// <returns>New skin index. '0' if failed.</returns>
    public static byte AddPlayerCustomization<T>(T skin)
        where T : Il2CppObjectBase
    {
        ThrowIfNotLoaded();
        if (customizationTable == null)
        {
            customizationTable = Resources.Load<PlayerCustomizationTable>("PlayerCustomizationTable");
        }

        try
        {
            Il2CppSystem.Reflection.FieldInfo property = Il2CppType.Of<PlayerCustomizationTable>().GetFields(all)
                .First(info =>
                {
                    Il2CppReferenceArray<Il2CppSystem.Type> args = info.FieldType.GetGenericArguments();
                    if (args != null && args.Count > 0)
                    {
                        Il2CppSystem.Type listType = args.Single();
                        return listType.Equals(Il2CppType.Of<T>());
                    }

                    return false;
                });

            Il2CppSystem.Collections.Generic.List<T> list = property.GetValue(customizationTable).Cast<Il2CppSystem.Collections.Generic.List<T>>();
            if (list.Count < 255)
            {
                byte skinId = (byte)list.Count;
                list.Add(skin);
                return skinId;
            }
        }
        catch (InvalidOperationException)
        {
            CoreLibPlugin.Logger.LogError($"Failed to add player customization of type {typeof(T).FullName}, because there is no such customization table!");
        }

        return 0;
    }

    /// <summary>
    /// Set entity <see cref="EquipmentSkinCDAuthoring"/> skin 
    /// </summary>
    /// <param name="id">Target Entity ID</param>
    /// <param name="skinId">new skin Index</param>
    public static void SetEquipmentSkin(ObjectID id, byte skinId)
    {
        ThrowIfNotLoaded();
        ThrowIfTooLate(nameof(SetEquipmentSkin));

        if (GetMainEntity(id, out EntityMonoBehaviourData entity))
        {
            EquipmentSkinCDAuthoring skinCdAuthoring = entity.gameObject.GetComponent<EquipmentSkinCDAuthoring>();
            if (skinCdAuthoring != null)
            {
                skinCdAuthoring.skin = skinId;
            }
            else
            {
                skinCdAuthoring = entity.gameObject.AddComponent<EquipmentSkinCDAuthoring>();
                skinCdAuthoring.skin = skinId;
            }
        }
        else
        {
            CoreLibPlugin.Logger.LogError($"Failed to set equipment skin! Found no registered entities with ID: {id}.");
        }
    }

    /// <summary>
    /// Set entity <see cref="TileCDAuthoring"/> component tileset variable.
    /// </summary>
    /// <param name="id">Target entity id</param>
    /// <param name="tileset">new tileset</param>
    public static void SetTileset(ObjectID id, Tileset tileset)
    {
        ThrowIfNotLoaded();
        ThrowIfTooLate(nameof(SetTileset));

        if (GetMainEntity(id, out EntityMonoBehaviourData entity))
        {
            TileCDAuthoring tileCdAuthoring = entity.gameObject.GetComponent<TileCDAuthoring>();
            if (tileCdAuthoring != null)
            {
                tileCdAuthoring.tileset = tileset;
            }
            else
            {
                tileCdAuthoring = entity.gameObject.AddComponent<TileCDAuthoring>();
                tileCdAuthoring.tileset = tileset;
            }
        }
        else
        {
            CoreLibPlugin.Logger.LogError($"Failed to set tileset! Found no registered entities with ID: {id}.");
        }
    }

    #endregion

    #region PrivateImplementation

    private static bool _loaded;

    internal static Dictionary<ObjectID, List<EntityMonoBehaviourData>> entitiesToAdd = new Dictionary<ObjectID, List<EntityMonoBehaviourData>>();
    internal static Dictionary<ObjectID, Action<EntityMonoBehaviourData>> entityModifyFunctions = new Dictionary<ObjectID, Action<EntityMonoBehaviourData>>();

    internal static Dictionary<Tileset, GCHandleObject<MapWorkshopTilesetBank.Tileset>> customTilesets =
        new Dictionary<Tileset, GCHandleObject<MapWorkshopTilesetBank.Tileset>>();

    internal static Dictionary<string, PugMapTileset> tilesetLayers = new Dictionary<string, PugMapTileset>();
    internal static MapWorkshopTilesetBank.Tileset missingTileset;

    internal static IdBindConfigFile modItemIDs;
    internal static IdBindConfigFile tilesetIDs;

    internal static List<ObjectID> rootWorkbenches = new List<ObjectID>();

    internal static PlayerCustomizationTable customizationTable;

    internal const Il2CppSystem.Reflection.BindingFlags all = Il2CppSystem.Reflection.BindingFlags.Instance | Il2CppSystem.Reflection.BindingFlags.Static |
                                                              Il2CppSystem.Reflection.BindingFlags.Public | Il2CppSystem.Reflection.BindingFlags.NonPublic |
                                                              Il2CppSystem.Reflection.BindingFlags.GetField | Il2CppSystem.Reflection.BindingFlags.SetField |
                                                              Il2CppSystem.Reflection.BindingFlags.GetProperty |
                                                              Il2CppSystem.Reflection.BindingFlags.SetProperty;


    public const int modEntityIdRangeStart = 33000;
    public const int modEntityIdRangeEnd = 65535;

    public const int modTilesetIdRangeStart = 100;
    public const int modTilesetIdRangeEnd = 200;

    internal static bool hasInjected;

    public const string RootWorkbench = "CoreLib:RootModWorkbench";

    [CoreLibSubmoduleInit(Stage = InitStage.SetHooks)]
    internal static void SetHooks()
    {
        CoreLibPlugin.harmony.PatchAll(typeof(MemoryManager_Patch));
        CoreLibPlugin.harmony.PatchAll(typeof(PugDatabaseAuthoring_Patch));
        CoreLibPlugin.harmony.PatchAll(typeof(Loading_Patch));
        CoreLibPlugin.harmony.PatchAll(typeof(TilesetTypeUtility_Patch));
    }

    [CoreLibSubmoduleInit(Stage = InitStage.PostLoad)]
    internal static void Load()
    {
        BepInPlugin metadata = MetadataHelper.GetMetadata(typeof(CoreLibPlugin));
        modItemIDs = new IdBindConfigFile($"{Paths.ConfigPath}/CoreLib/CoreLib.ModItemID.cfg", metadata, modEntityIdRangeStart, modEntityIdRangeEnd);
        tilesetIDs = new IdBindConfigFile($"{Paths.ConfigPath}/CoreLib/CoreLib.TilesetID.cfg", metadata, modTilesetIdRangeStart, modTilesetIdRangeEnd);

        ClassInjector.RegisterTypeInIl2Cpp<EntityPrefabOverride>();
        ClassInjector.RegisterTypeInIl2Cpp<RuntimeMaterial>();
        ClassInjector.RegisterTypeInIl2Cpp<ModEntityMonoBehavior>();
        ClassInjector.RegisterTypeInIl2Cpp<ModCDAuthoringBase>();
        ClassInjector.RegisterTypeInIl2Cpp<ModTileCDAuthoring>();
        RegisterModifications(typeof(CustomEntityModule));

        InitTilesets();
        AddRootWorkbench();
    }

    private static void InitTilesets()
    {
        MapWorkshopTilesetBank vanillaBank = TilesetTypeUtility.tilesetBank;
        if (vanillaBank != null)
        {
            foreach (MapWorkshopTilesetBank.Tileset tileset in vanillaBank.tilesets)
            {
                string layersName = tileset.layers.name;
                if (!tilesetLayers.ContainsKey(layersName))
                {
                    tilesetLayers.Add(layersName, tileset.layers);
                }
            }
        }
        else
        {
            CoreLibPlugin.Logger.LogError("Failed to get default tileset layers!");
        }

        MapWorkshopTilesetBank tilesetBank = ResourcesModule.LoadAsset<MapWorkshopTilesetBank>("Assets/CoreLib/Tileset/MissingTileset");

        missingTileset = tilesetBank.tilesets._items[0];

        if (tilesetLayers.ContainsKey(missingTileset.layers.name))
        {
            missingTileset.layers = tilesetLayers[missingTileset.layers.name];
        }
    }

    [EntityModification(ObjectID.Player)]
    private static void EditPlayer(EntityMonoBehaviourData entity)
    {
        CraftingCDAuthoring craftingCdAuthoring = entity.GetComponent<CraftingCDAuthoring>();
        craftingCdAuthoring.canCraftObjects.Add(new CraftableObject() { objectID = rootWorkbenches.First(), amount = 1 });
    }

    private static ObjectID AddRootWorkbench()
    {
        ObjectID workbench = AddWorkbench(RootWorkbench, "Assets/CoreLib/Textures/modWorkbench", null);
        rootWorkbenches.Add(workbench);
        AddEntityLocalization(workbench, $"Root Workbench {rootWorkbenches.Count}", "This workbench contains all modded workbenches!");
        return workbench;
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

    internal static void ThrowIfTooLate(string methodName)
    {
        if (hasInjected)
        {
            throw new InvalidOperationException($"{nameof(CustomEntityModule)}.{methodName}() method called too late! Entity injection is already done.");
        }
    }

    private static bool IsRootWorkbench(ObjectID objectID)
    {
        return rootWorkbenches.Contains(objectID);
    }

    internal static bool GetMainEntity(ObjectID objectID, out EntityMonoBehaviourData entity)
    {
        if (entitiesToAdd.ContainsKey(objectID))
        {
            entity = entitiesToAdd[objectID][0];
            return true;
        }

        entity = null;
        return false;
    }

    internal static bool GetEntity(ObjectID objectID, int variation, out EntityMonoBehaviourData entity)
    {
        if (entitiesToAdd.ContainsKey(objectID))
        {
            var entities = entitiesToAdd[objectID];
            foreach (EntityMonoBehaviourData entityData in entities)
            {
                if (entityData.objectInfo.variation == variation)
                {
                    entity = entityData;
                    return true;
                }
            }
        }

        entity = null;
        return false;
    }


    private static ObjectID AddWorkbench(string itemId, string spritePath, List<CraftingData> recipe)
    {
        Sprite[] sprites = ResourcesModule.LoadSprites(spritePath).OrderSprites();
        if (sprites == null || sprites.Length != 2)
        {
            CoreLibPlugin.Logger.LogError($"Failed to add workbench! Provided sprite must be in 'Multiple' mode and have two sprites!");
            return ObjectID.None;
        }

        ObjectID id = AddEntity(itemId, "Assets/CoreLib/Objects/TemplateWorkbench");
        if (GetMainEntity(id, out EntityMonoBehaviourData entity))
        {
            entity.objectInfo.icon = sprites[0];
            entity.objectInfo.smallIcon = sprites[1];
            if (recipe != null)
            {
                entity.objectInfo.requiredObjectsToCraft = recipe.Select(data =>
                {
                    return new CraftingObject()
                    {
                        objectID = data.objectID,
                        amount = data.amount
                    };
                }).ToIl2CppList();
            }

            CraftingCDAuthoring comp = entity.gameObject.AddComponent<CraftingCDAuthoring>();
            comp.craftingType = CraftingType.Simple;
            comp.canCraftObjects = new Il2CppSystem.Collections.Generic.List<CraftableObject>(4);
        }

        return id;
    }

    private static EntityMonoBehaviourData LoadPrefab(string itemId, string prefabPath)
    {
        Object gameObject = ResourcesModule.LoadAsset(prefabPath);
        if (gameObject == null)
        {
            throw new ArgumentException($"Found no prefab at path: {prefabPath}");
        }

        GameObject prefab = gameObject.TryCast<GameObject>();
        if (prefab == null)
        {
            throw new ArgumentException($"Object at path: {prefabPath} is not a Prefab!");
        }

        GameObject newPrefab = Object.Instantiate(prefab);

        EntityMonoBehaviourData entityData = newPrefab.GetComponent<EntityMonoBehaviourData>();

        string fullItemId = $"{itemId}_{entityData.objectInfo.variation}";

        newPrefab.name = $"{fullItemId}_Prefab";
        newPrefab.hideFlags = HideFlags.HideAndDontSave;

        GhostAuthoringComponent ghost = newPrefab.GetComponent<GhostAuthoringComponent>();
        if (ghost != null)
        {
            ghost.Name = itemId;
            ghost.prefabId = fullItemId.GetGUID();
        }

        foreach (PrefabInfo prefabInfo in entityData.objectInfo.prefabInfos)
        {
            if (prefabInfo.prefab == null) continue;

            ModEntityMonoBehavior behavior = prefabInfo.prefab.TryCast<ModEntityMonoBehavior>();
            if (behavior != null)
            {
                behavior.Allocate();
            }
        }

        foreach (ModCDAuthoringBase gcAllocMonoBehavior in newPrefab.GetComponents<ModCDAuthoringBase>())
        {
            gcAllocMonoBehavior.Allocate();
        }

        return entityData;
    }

    private static void RegisterModifications_Internal(Assembly assembly)
    {
        IEnumerable<Type> types = assembly.GetTypes().Where(HasAttribute);

        foreach (Type type in types)
        {
            RegisterModificationsInType_Internal(type);
        }
    }

    private static void RegisterModificationsInType_Internal(Type type)
    {
        int modifiersCount = 0;

        IEnumerable<MethodInfo> methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic).Where(HasAttribute);
        foreach (MethodInfo method in methods)
        {
            EntityModificationAttribute attribute = method.GetCustomAttribute<EntityModificationAttribute>();
            attribute.ResolveTarget();
            if (attribute.target == ObjectID.None)
            {
                CoreLibPlugin.Logger.LogWarning($"Failed to add modify method '{method.FullDescription()}', because target object ID is not set!");
                continue;
            }

            var parameters = method.GetParameters();
            if (method.ReturnParameter.ParameterType == typeof(void) &&
                parameters.Length == 1 &&
                parameters[0].ParameterType == typeof(EntityMonoBehaviourData))
            {
                Action<EntityMonoBehaviourData> modifyDelegate = method.CreateDelegate<Action<EntityMonoBehaviourData>>();
                if (entityModifyFunctions.ContainsKey(attribute.target))
                {
                    entityModifyFunctions[attribute.target] += modifyDelegate;
                }
                else
                {
                    entityModifyFunctions.Add(attribute.target, modifyDelegate);
                }

                modifiersCount++;
            }
            else
            {
                CoreLibPlugin.Logger.LogWarning(
                    $"Failed to add modify method '{method.FullDescription()}', because method signature is incorrect. Should be void ({nameof(EntityMonoBehaviourData)})!");
            }
        }

        CoreLibPlugin.Logger.LogInfo($"Registered {modifiersCount} entity modifiers in type {type.FullName}!");
    }

    private static bool HasAttribute(MemberInfo type)
    {
        return type.GetCustomAttribute<EntityModificationAttribute>() != null;
    }

    #endregion
}