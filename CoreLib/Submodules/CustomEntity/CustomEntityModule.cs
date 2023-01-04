using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using CoreLib.Components;
using CoreLib.Submodules.Common.Patches;
using CoreLib.Submodules.CustomEntity.Atributes;
using CoreLib.Submodules.CustomEntity.Interfaces;
using CoreLib.Submodules.CustomEntity.Patches;
using CoreLib.Submodules.Localization;
using CoreLib.Submodules.ModResources;
using CoreLib.Util;
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
/// This module provides means to add new content
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

        return (ObjectID)modEntityIDs.GetIndex(itemID);
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

    public static ObjectType GetObjectType(string typeName)
    {
        ThrowIfNotLoaded();

        int index = objectTypeIDs.HasIndex(typeName) ? 
            objectTypeIDs.GetIndex(typeName) : 
            objectTypeIDs.GetNextId(typeName);
        return (ObjectType)index;
    }

    public static string[] GetAllModdedItems()
    {
        ThrowIfNotLoaded();
        return modEntityIDs.modIDs.Keys.ToArray();
    }

    /// <summary>
    /// Add custom workbench with specified sprite. It is automatically added to main mod workbench
    /// </summary>
    /// <param name="itemId">UNIQUE entity Id</param>
    /// <param name="spritePath">path to your sprite in asset bundle</param>
    public static ObjectID AddModWorkbench(string itemId, string spritePath)
    {
        return AddModWorkbench(itemId, spritePath, null, true);
    }

    /// <summary>
    /// Add custom workbench with specified sprite.
    /// </summary>
    /// <param name="itemId">UNIQUE entity Id</param>
    /// <param name="spritePath">path to your sprite in asset bundle</param>
    public static ObjectID AddModWorkbench(string itemId, string spritePath, bool bindToRootWorkbench)
    {
        return AddModWorkbench(itemId, spritePath, null, bindToRootWorkbench);
    }

    /// <summary>
    /// Add custom workbench with specified sprite. It is automatically added to main mod workbench
    /// </summary>
    /// <param name="itemId">UNIQUE entity Id</param>
    /// <param name="spritePath">path to your sprite in asset bundle</param>
    /// <param name="recipe">workbench craft recipe</param>
    public static ObjectID AddModWorkbench(string itemId, string spritePath, List<CraftingData> recipe)
    {
        return AddModWorkbench(itemId, spritePath, recipe, true);
    }

    /// <summary>
    /// Add custom workbench with specified sprite.
    /// </summary>
    /// <param name="itemId">UNIQUE entity Id</param>
    /// <param name="spritePath">path to your sprite in asset bundle</param>
    /// <param name="recipe">workbench craft recipe</param>
    public static ObjectID AddModWorkbench(string itemId, string spritePath, List<CraftingData> recipe, bool bindToRootWorkbench)
    {
        ThrowIfNotLoaded();
        ThrowIfTooLate(nameof(AddModWorkbench));
        ObjectID workbenchId = AddWorkbench(itemId, spritePath, recipe);
        if (bindToRootWorkbench)
        {
            if (rootWorkbenches.Count == 0)
                AddRootWorkbench();
            AddWorkbenchItem(rootWorkbenches.Last(), workbenchId);
        }

        return workbenchId;
    }
    
    /// <summary>
    /// Add custom workbench with specified sprite.
    /// </summary>
    /// <param name="itemId">UNIQUE entity Id</param>
    /// <param name="spritePath">path to your sprite in asset bundle</param>
    /// <param name="recipe">workbench craft recipe</param>
    public static ObjectID AddModWorkbench(string itemId, string bigIconPath, string smallIconPath, List<CraftingData> recipe, bool bindToRootWorkbench)
    {
        ThrowIfNotLoaded();
        ThrowIfTooLate(nameof(AddModWorkbench));
        ObjectID workbenchId = AddWorkbench(itemId, bigIconPath, smallIconPath, recipe);
        if (bindToRootWorkbench)
        {
            if (rootWorkbenches.Count == 0)
                AddRootWorkbench();
            AddWorkbenchItem(rootWorkbenches.Last(), workbenchId);
        }
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
                MonoBehaviourUtils.CallAlloc(entity);
                entities.Add(entity);
            }
            catch (ArgumentException)
            {
                CoreLibPlugin.Logger.LogError($"Failed to add entity {itemId}, prefab {prefabPath} is missing!");
                return ObjectID.None;
            }
        }

        entities.Sort((a, b) => a.objectInfo.variation.CompareTo(b.objectInfo.variation));

        return AddEntityWithVariations(itemId, entities);
    }

    public static ObjectID AddEntityWithVariations(string itemId, List<EntityMonoBehaviourData> entities)
    {
        ThrowIfNotLoaded();
        ThrowIfTooLate(nameof(AddEntityWithVariations));
        
        if (entities.Count == 0)
        {
            CoreLibPlugin.Logger.LogError($"Failed to add entity {itemId}: entities has no entities!");
            return ObjectID.None;
        }
        
        int itemIndex = modEntityIDs.GetNextId(itemId);
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
    [Obsolete("Use LocalizationModule.AddEntityLocalization() instead")]
    public static void AddEntityLocalization(ObjectID obj, string enName, string enDesc, string cnName = "", string cnDesc = "")
    {
        LocalizationModule.AddEntityLocalization(obj, enName, enDesc, cnName, cnDesc);
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
            Il2CppSystem.Reflection.FieldInfo property = Il2CppType.Of<PlayerCustomizationTable>().GetFieldOfType<Il2CppSystem.Collections.Generic.List<T>>();

            Il2CppSystem.Collections.Generic.List<T> list = property.GetValue(customizationTable).Cast<Il2CppSystem.Collections.Generic.List<T>>();
            if (list.Count < 255)
            {
                byte skinIndex = (byte)list.Count;
                list.Add(skin);
                return skinIndex;
            }
        }
        catch (InvalidOperationException)
        {
            CoreLibPlugin.Logger.LogError($"Failed to add player customization of type {typeof(T).FullName}, because there is no such customization table!");
        }

        return 0;
    }

    public static void RegisterDynamicItemHandler<T>()
    where T : IDynamicItemHandler, new()
    {
        if (dynamicItemHandlers.Any(handler => handler.GetType() == typeof(T)))
        {
            CoreLibPlugin.Logger.LogWarning($"Failed to register dynamic handler {typeof(T).FullName}, because it is already registered!");
            return;
        }
        
        T handler = Activator.CreateInstance<T>();
        dynamicItemHandlers.Add(handler);
    }
    
    #endregion

    #region PrivateImplementation

    private static bool _loaded;

    internal static Dictionary<ObjectID, List<EntityMonoBehaviourData>> entitiesToAdd = new Dictionary<ObjectID, List<EntityMonoBehaviourData>>();
    internal static Dictionary<ObjectID, Action<EntityMonoBehaviourData>> entityModifyFunctions = new Dictionary<ObjectID, Action<EntityMonoBehaviourData>>();
    internal static Dictionary<string, Action<EntityMonoBehaviourData>> modEntityModifyFunctions = new Dictionary<string, Action<EntityMonoBehaviourData>>();

    internal static Dictionary<Tileset, GCHandleObject<MapWorkshopTilesetBank.Tileset>> customTilesets =
        new Dictionary<Tileset, GCHandleObject<MapWorkshopTilesetBank.Tileset>>();

    internal static Dictionary<string, PugMapTileset> tilesetLayers = new Dictionary<string, PugMapTileset>();
    internal static MapWorkshopTilesetBank.Tileset missingTileset;

    internal static List<IDynamicItemHandler> dynamicItemHandlers = new List<IDynamicItemHandler>();

    internal static IdBindConfigFile modEntityIDs;
    internal static IdBindConfigFile tilesetIDs;
    internal static IdBind objectTypeIDs;

    internal static List<ObjectID> rootWorkbenches = new List<ObjectID>();

    internal static PlayerCustomizationTable customizationTable;

    public const int modEntityIdRangeStart = 33000;
    public const int modEntityIdRangeEnd = ushort.MaxValue;

    public const int modTilesetIdRangeStart = 100;
    public const int modTilesetIdRangeEnd = 200;
    
    public const int modObjectTypeIdRangeStart = 33000;
    public const int modObjectTypeIdRangeEnd = ushort.MaxValue;

    internal static bool hasInjected;
    internal static bool hasConverted;

    public const string RootWorkbench = "CoreLib:RootModWorkbench";

    [CoreLibSubmoduleInit(Stage = InitStage.SetHooks)]
    internal static void SetHooks()
    {
        MemoryManager_Patch.TryPatch();
        CoreLibPlugin.harmony.PatchAll(typeof(PugDatabaseAuthoring_Patch));
        CoreLibPlugin.harmony.PatchAll(typeof(TilesetTypeUtility_Patch));
        CoreLibPlugin.harmony.PatchAll(typeof(GameObjectConversionMappingSystem_Patch));
        CoreLibPlugin.harmony.PatchAll(typeof(PlayerController_Patch));
        CoreLibPlugin.harmony.PatchAll(typeof(ColorReplacer_Patch));
    }

    [CoreLibSubmoduleInit(Stage = InitStage.PostLoad)]
    internal static void Load()
    {
        BepInPlugin metadata = MetadataHelper.GetMetadata(typeof(CoreLibPlugin));
        modEntityIDs = new IdBindConfigFile($"{Paths.ConfigPath}/CoreLib/CoreLib.ModEntityID.cfg", metadata, modEntityIdRangeStart, modEntityIdRangeEnd);
        tilesetIDs = new IdBindConfigFile($"{Paths.ConfigPath}/CoreLib/CoreLib.TilesetID.cfg", metadata, modTilesetIdRangeStart, modTilesetIdRangeEnd);
        objectTypeIDs = new IdBind(modObjectTypeIdRangeStart, modObjectTypeIdRangeEnd);

        ClassInjector.RegisterTypeInIl2Cpp<EntityPrefabOverride>();
        ClassInjector.RegisterTypeInIl2Cpp<RuntimeMaterial>();
        ClassInjector.RegisterTypeInIl2Cpp<RuntimeMaterialV2>();
        ClassInjector.RegisterTypeInIl2Cpp<ModEntityMonoBehavior>();
        ClassInjector.RegisterTypeInIl2Cpp<ModProjectile>();
        ClassInjector.RegisterTypeInIl2Cpp<ModCDAuthoringBase>();
        ClassInjector.RegisterTypeInIl2Cpp<ModTileCDAuthoring>();
        ClassInjector.RegisterTypeInIl2Cpp<ModEquipmentSkinCDAuthoring>();
        ClassInjector.RegisterTypeInIl2Cpp<ModDropsLootFromTableCDAuthoring>();
        ClassInjector.RegisterTypeInIl2Cpp<ModRangeWeaponCDAuthoring>();
        ClassInjector.RegisterTypeInIl2Cpp<ModObjectTypeAuthoring>();
        RegisterModifications(typeof(CustomEntityModule));

        InitTilesets();
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
        if (rootWorkbenches.Count > 0)
        {
            CraftingCDAuthoring craftingCdAuthoring = entity.GetComponent<CraftingCDAuthoring>();
            craftingCdAuthoring.canCraftObjects.Add(new CraftableObject() { objectID = rootWorkbenches.First(), amount = 1 });
        }
    }

    private static ObjectID AddRootWorkbench()
    {
        ObjectID workbench = AddWorkbench(RootWorkbench, "Assets/CoreLib/Textures/modWorkbench", null);
        rootWorkbenches.Add(workbench);
        LocalizationModule.AddEntityLocalization(workbench, $"Root Workbench {rootWorkbenches.Count}", "This workbench contains all modded workbenches!");
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
    
    private static ObjectID AddWorkbench(string itemId, string bigIconPath, string smallIconPath, List<CraftingData> recipe)
    {
        Sprite bigIcon = ResourcesModule.LoadAsset<Sprite>(bigIconPath);
        Sprite smallIcon = ResourcesModule.LoadAsset<Sprite>(smallIconPath);

        return AddWorkbench(itemId, bigIcon, smallIcon, recipe);
    }

    private static ObjectID AddWorkbench(string itemId, string spritePath, List<CraftingData> recipe)
    {
        Sprite[] sprites = ResourcesModule.LoadSprites(spritePath).OrderSprites();
        if (sprites == null || sprites.Length != 2)
        {
            CoreLibPlugin.Logger.LogError($"Failed to add workbench! Provided sprite must be in 'Multiple' mode and have two sprites!");
            return ObjectID.None;
        }

        return AddWorkbench(itemId, sprites[0], sprites[1], recipe);
    }

    private static ObjectID AddWorkbench(string itemId, Sprite bigIcon, Sprite smallIcon, List<CraftingData> recipe)
    {
        ObjectID id = AddEntity(itemId, "Assets/CoreLib/Objects/TemplateWorkbench");
        if (GetMainEntity(id, out EntityMonoBehaviourData entity))
        {
            entity.objectInfo.icon = bigIcon;
            entity.objectInfo.smallIcon = smallIcon;
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

    internal static EntityMonoBehaviourData LoadPrefab(string itemId, string prefabPath)
    {
        GameObject prefab = ResourcesModule.LoadAsset<GameObject>(prefabPath);

        GameObject newPrefab = Object.Instantiate(prefab);
        ResourcesModule.Retain(newPrefab);

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
            var attributes = method.GetCustomAttributes<EntityModificationAttribute>();

            var parameters = method.GetParameters();
            if (method.ReturnParameter.ParameterType == typeof(void) &&
                parameters.Length == 1 &&
                parameters[0].ParameterType == typeof(EntityMonoBehaviourData))
            {
                Action<EntityMonoBehaviourData> modifyDelegate = method.CreateDelegate<Action<EntityMonoBehaviourData>>();

                foreach (EntityModificationAttribute attribute in attributes)
                {
                    if (!string.IsNullOrEmpty(attribute.modTarget))
                    {
                        modEntityModifyFunctions.AddDelegate(attribute.modTarget, modifyDelegate);
                    }
                    else
                    {
                        entityModifyFunctions.AddDelegate(attribute.target, modifyDelegate);
                    }

                    modifiersCount++;
                }
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