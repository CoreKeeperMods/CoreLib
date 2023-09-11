using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CoreLib.Scripts.Util.Extensions;
using CoreLib.Submodules.Localization;
using CoreLib.Submodules.ModEntity.Atributes;
using CoreLib.Submodules.ModEntity.Interfaces;
using CoreLib.Submodules.ModEntity.Patches;
using CoreLib.Submodules.ModResources;
using CoreLib.Util;
using CoreLib.Util.Extensions;
using HarmonyLib;
using PugMod;
using Unity.NetCode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CoreLib.Submodules.ModEntity
{
    public static class EntityModule
    {
        internal static List<GameObject> modAuthoringTargets = new List<GameObject>();
        internal static List<GameObject> poolablePrefabs = new List<GameObject>();

        public static event Action MaterialSwapReady; 

        public static void AddToAuthoringList(GameObject gameObject)
        {
            modAuthoringTargets.Add(gameObject);
        }
        
        public static void EnablePooling(GameObject gameObject)
        {
            poolablePrefabs.Add(gameObject);
        }

        internal static void ApplyAll()
        {
            MaterialSwapReady?.Invoke();
            foreach (GameObject gameObject in modAuthoringTargets)
            {
                var objectAuthoring = gameObject.GetComponent<ObjectAuthoring>();
                var entityData = gameObject.GetComponent<EntityMonoBehaviourData>();

                MonoBehaviour dataMonoBehaviour = objectAuthoring != null ? (MonoBehaviour)objectAuthoring : (MonoBehaviour)entityData;
                MonoBehaviourUtils.ApplyPrefabModAuthorings(dataMonoBehaviour, gameObject);
                if (objectAuthoring != null && objectAuthoring.graphicalPrefab != null)
                {
                    MonoBehaviourUtils.ApplyPrefabModAuthorings(dataMonoBehaviour, objectAuthoring.graphicalPrefab);
                }
                else if (entityData != null && entityData.objectInfo.prefabInfos[0].prefab != null)
                {
                    MonoBehaviourUtils.ApplyPrefabModAuthorings(dataMonoBehaviour, entityData.objectInfo.prefabInfos[0].prefab.gameObject);
                }
            }
        }
        
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
    public static void RegisterEntityModifications(Assembly assembly)
    {
        ThrowIfNotLoaded();
        ThrowIfTooLate(nameof(RegisterEntityModifications));

        RegisterEntityModifications_Internal(assembly);
    }
    
    public static void RegisterPrefabModifications(Assembly assembly)
    {
        ThrowIfNotLoaded();
        ThrowIfTooLate(nameof(RegisterPrefabModifications));

        RegisterPrefabModifications_Internal(assembly);
    }

    /// <summary>
    /// Register you entity modifications methods.
    /// </summary>
    /// <param name="type">Type to analyze</param>
    public static void RegisterEntityModifications(Type type)
    {
        ThrowIfNotLoaded();
        ThrowIfTooLate(nameof(RegisterEntityModifications));

        RegisterEntityModificationsInType_Internal(type);
    }
    
    public static void RegisterPrefabModifications(Type type)
    {
        ThrowIfNotLoaded();
        ThrowIfTooLate(nameof(RegisterPrefabModifications));

        RegisterPrefabModificationsInType_Internal(type);
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
    
    public static string GetObjectStringId(ObjectID itemID)
    {
        ThrowIfNotLoaded();

        return modEntityIDs.GetStringID((int)itemID);
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
        return modEntityIDs.ModIDs.Keys.ToArray();
    }

    public static ObjectID AddModWorkbench(WorkbenchDefinition workbenchDefinition)
    {
        ThrowIfNotLoaded();
        ThrowIfTooLate(nameof(AddModWorkbench));
        ObjectID workbenchId = AddWorkbench(workbenchDefinition, true);
        if (workbenchDefinition.bindToRootWorkbench)
        {
            ObjectID root = TryAddRootWorkbench();
            AddWorkbenchItem(root, workbenchId);
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
        if (modWorkbenchesChain.ContainsKey(workBenchId))
        {
            ObjectAuthoring workbenchEntity = modWorkbenchesChain[workBenchId].Last();
            CraftingAuthoring craftingCdAuthoring = workbenchEntity.gameObject.GetComponent<CraftingAuthoring>();

            CoreLibMod.Log.LogDebug($"Adding item {entityId.ToString()} to workbench {workBenchId.ToString()}");

            if (craftingCdAuthoring.canCraftObjects.Count < 18)
            {
                craftingCdAuthoring.canCraftObjects.Add(new CraftingAuthoring.CraftableObject() { objectID = entityId, amount = 1 });
                return;
            }
            CloneWorkbench(workbenchEntity);
            AddWorkbenchItem(workBenchId, entityId);
        }

        CoreLibMod.Log.LogError($"Failed to add workbench item! Found no entities in the list with ID: {workBenchId}.");
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
    /// Add new entity.
    /// </summary>
    /// <param name="itemId">UNIQUE entity id</param>
    /// <param name="prefabPath">path to your prefab in asset bundle</param>
    /// <returns>Added objectID. If adding failed returns <see cref="ObjectID.None"/></returns>
    /// <exception cref="InvalidOperationException">Throws if called too late</exception>
    public static ObjectID AddEntity(string itemId, ObjectAuthoring prefab)
    {
        return AddEntityWithVariations(itemId, new List<ObjectAuthoring> {prefab});
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
            CoreLibMod.Log.LogError($"Failed to add entity {itemId}: prefabsPaths has no paths!");
            return ObjectID.None;
        }

        List<ObjectAuthoring> entities = new List<ObjectAuthoring>(prefabsPaths.Length);
        
        foreach (string prefabPath in prefabsPaths)
        {
            try
            {
                ObjectAuthoring entity = LoadPrefab(itemId, prefabPath);
                entities.Add(entity);
            }
            catch (ArgumentException)
            {
                CoreLibMod.Log.LogError($"Failed to add entity {itemId}, prefab {prefabPath} is missing!");
                return ObjectID.None;
            }
        }

        return AddEntityWithVariations(itemId, entities);
    }

    public static ObjectID AddEntityWithVariations(string itemId, List<ObjectAuthoring> prefabs)
    {
        int itemIndex = modEntityIDs.GetNextId(itemId);
        ObjectID objectID = (ObjectID)itemIndex;

        foreach (ObjectAuthoring prefab in prefabs)
        {
            prefab.objectID = itemIndex;
            API.Authoring.RegisterAuthoringGameObject(prefab.gameObject);
        }

        moddedEntities.Add(objectID, prefabs);
        CoreLibMod.Log.LogDebug($"Assigned entity {itemId} objectID: {objectID}!");
        return objectID;
    }

    /// <summary>
    /// Add custom customization texture sheet
    /// </summary>
    /// <param name="skin">Class with texture sheet information</param>
    /// <returns>New skin index. '0' if failed.</returns>
    public static byte AddPlayerCustomization<T>(T skin)
        where T : SkinBase
    {
        ThrowIfNotLoaded();
        InitCustomizationTable();

        try
        {
            var properties = typeof(PlayerCustomizationTable).GetFieldsOfType<List<T>>();
            var listProperty = properties.First(info => !info.Name.Contains("Sorted"));
            var sorttedListProperty = properties.First(info => info.Name.Contains("Sorted"));

            var list = (List<T>)listProperty.GetValue(customizationTable);
            var sortedList = (List<T>)sorttedListProperty.GetValue(customizationTable);
            
            if (list.Count < 255)
            {
                byte skinIndex = (byte)list.Count;
                skin.id = skinIndex;
                list.Add(skin);
                sortedList.Add(skin);
                return skinIndex;
            }
        }
        catch (InvalidOperationException)
        {
            CoreLibMod.Log.LogError($"Failed to add player customization of type {typeof(T).FullName}, because there is no such customization table!");
        }

        return 0;
    }

    public static void RegisterDynamicItemHandler<T>()
        where T : IDynamicItemHandler, new()
    {
        if (dynamicItemHandlers.Any(handler => handler.GetType() == typeof(T)))
        {
            CoreLibMod.Log.LogWarning($"Failed to register dynamic handler {typeof(T).FullName}, because it is already registered!");
            return;
        }
        
        T handler = Activator.CreateInstance<T>();
        dynamicItemHandlers.Add(handler);
    }
    
    
    #endregion

    #region PrivateImplementation

    private static bool _loaded;

    internal static Dictionary<ObjectID, List<ObjectAuthoring>> moddedEntities = new Dictionary<ObjectID, List<ObjectAuthoring>>();
    internal static Dictionary<ObjectID, Action<MonoBehaviour>> entityModifyFunctions = new Dictionary<ObjectID, Action<MonoBehaviour>>();
    internal static Dictionary<string, Action<MonoBehaviour>> modEntityModifyFunctions = new Dictionary<string, Action<MonoBehaviour>>();
    internal static Dictionary<Type, Action<MonoBehaviour>> prefabModifyFunctions = new Dictionary<Type, Action<MonoBehaviour>>();
    
    internal static Dictionary<ObjectID, List<ObjectAuthoring>> modWorkbenchesChain  = new Dictionary<ObjectID, List<ObjectAuthoring>>();
    internal static Dictionary<ObjectID, WorkbenchDefinition> modWorkbenches = new Dictionary<ObjectID, WorkbenchDefinition>();

    internal static List<IDynamicItemHandler> dynamicItemHandlers = new List<IDynamicItemHandler>();

    internal static IdBindConfigFile modEntityIDs;
    internal static IdBind objectTypeIDs;

    internal static ObjectID? rootWorkbench;
    
    internal static HashSet<int> busyIDsSet = new HashSet<int>();

    internal static PlayerCustomizationTable customizationTable;

    public const int modEntityIdRangeStart = 33000;
    public const int modEntityIdRangeEnd = ushort.MaxValue;
    
    
    public const int modObjectTypeIdRangeStart = 33000;
    public const int modObjectTypeIdRangeEnd = ushort.MaxValue;

    internal static bool hasInjected;
    internal static bool hasConverted;

    public const string RootWorkbench = "CoreLib:RootModWorkbench";

    #region Initialization

    [CoreLibSubmoduleInit(Stage = InitStage.SetHooks)]
    internal static void SetHooks()
    {
        CoreLibMod.harmony.PatchAll(typeof(MemoryManager_Patch));
        CoreLibMod.harmony.PatchAll(typeof(PlayerController_Patch));
        CoreLibMod.harmony.PatchAll(typeof(ColorReplacer_Patch));
    }

    [CoreLibSubmoduleInit(Stage = InitStage.PostLoad)]
    internal static void Load()
    {
        modEntityIDs = new IdBindConfigFile("CoreLib","CoreLib.ModEntityID", modEntityIdRangeStart, modEntityIdRangeEnd);
        objectTypeIDs = new IdBind(modObjectTypeIdRangeStart, modObjectTypeIdRangeEnd);
        
        RegisterEntityModifications(typeof(EntityModule));
        RegisterPrefabModifications(typeof(EntityModule));
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
            throw new InvalidOperationException($"{nameof(EntityModule)}.{methodName}() method called too late! Entity injection is already done.");
        }
    }

    private static void InitCustomizationTable()
    {
        if (customizationTable == null)
        {
            customizationTable = Resources.Load<PlayerCustomizationTable>("PlayerCustomizationTable");
            foreach (BreastArmorSkin armorSkin in customizationTable.breastArmorSkins)
            {
                busyIDsSet.Add(armorSkin.id);
            }
        }
    }
    
    #endregion

    #region Workbenches

    [EntityModification(ObjectID.Player)]
    private static void EditPlayer(MonoBehaviour entity)
    {
        if (rootWorkbench != null)
        {
            var rootWorkbenchId = rootWorkbench.Value;
            var lastRootWorkbenchId  = modWorkbenchesChain[rootWorkbenchId].Last().objectID;
            CraftingAuthoring craftingCdAuthoring = entity.GetComponent<CraftingAuthoring>();
            craftingCdAuthoring.canCraftObjects.Add(new CraftingAuthoring.CraftableObject() { objectID = (ObjectID)lastRootWorkbenchId, amount = 1 });
        }
    }

    [PrefabModification(typeof(SimpleCraftingBuilding))]
    private static void EditSimpleCraftingBuilding(MonoBehaviour entityMono)
    {
        SimpleCraftingBuilding craftingBuilding = (SimpleCraftingBuilding)entityMono;

        foreach (var pair in modWorkbenches)
        {
            var reskinInfo = new EntityMonoBehaviour.ReskinInfo()
            {
                objectIDToUseReskinOn = pair.Key,
                variation = 0,
                textures =
                {
                    pair.Value.skinsTexture
                }
            };
            
            foreach (var reskinOption in craftingBuilding.reskinOptions)
            {
                reskinOption.reskins.Add(reskinInfo);
            }
        }
    }

    private static void CloneWorkbench(ObjectAuthoring oldWorkbench)
    {
        ObjectID oldId = (ObjectID)oldWorkbench.objectID;
        if (modWorkbenches.ContainsKey(oldId))
        {
            var oldDefinition = modWorkbenches[oldId];
            string newId = IncrementID(oldDefinition.itemId);

            var newDefinition = Object.Instantiate(oldDefinition);
            newDefinition.itemId = newId;
            var newObjectId = AddModWorkbench(newDefinition);
            if (GetMainEntity(newObjectId, out ObjectAuthoring entity))
            {
                CraftingAuthoring crafting = oldWorkbench.gameObject.GetComponent<CraftingAuthoring>();
                crafting.includeCraftedObjectsFromBuildings.Add(entity.gameObject.GetComponent<CraftingAuthoring>());
            }
        }
    }
    
    
    private static ObjectID TryAddRootWorkbench()
    {
        if (rootWorkbench == null)
        {
            WorkbenchDefinition definition = ResourcesModule.LoadAsset<WorkbenchDefinition>("Assets/CoreLib/Resources/RootWorkbench");

            ObjectID workbench = AddWorkbench(definition, true);
            rootWorkbench = workbench;
            LocalizationModule.AddEntityLocalization(workbench, $"Root Workbench", "This workbench contains all modded workbenches!");
        }
        return rootWorkbench.Value;
    }

    private static ObjectID AddWorkbench(WorkbenchDefinition workbenchDefinition, bool isPrimary)
    {
        ObjectID id = AddEntity(workbenchDefinition.itemId, "Assets/CoreLib/Resources/Tileset/MissingTileset");
        if (GetMainEntity(id, out ObjectAuthoring entity))
        {
            var itemAuthoring = entity.GetComponent<InventoryItemAuthoring>();
            
            itemAuthoring.icon = workbenchDefinition.bigIcon;
            itemAuthoring.smallIcon = workbenchDefinition.smallIcon;
            itemAuthoring.requiredObjectsToCraft = workbenchDefinition.recipe;

            CraftingAuthoring comp = entity.gameObject.AddComponent<CraftingAuthoring>();
            comp.craftingType = CraftingType.Simple;
            comp.canCraftObjects = workbenchDefinition.canCraft;
            comp.includeCraftedObjectsFromBuildings = new List<CraftingAuthoring>();
            
            modWorkbenches.Add(id, workbenchDefinition);
        }

        return id;
    }

    private static string IncrementID(string prevId)
    {
        string[] idParts = prevId.Split(new[] { "$$" }, StringSplitOptions.None);
        int currentIndex = 0;
        
        if (idParts.Length >= 2 && int.TryParse(idParts[1], out int result))
            currentIndex = result;

        return $"{idParts[0]}$${currentIndex}";
    }
    
    #endregion

    #region Entities
    
    internal static bool GetMainEntity(ObjectID objectID, out ObjectAuthoring entity)
    {
        if (moddedEntities.ContainsKey(objectID))
        {
            entity = moddedEntities[objectID][0];
            return true;
        }

        entity = null;
        return false;
    }

    internal static bool GetEntity(ObjectID objectID, int variation, out ObjectAuthoring entity)
    {
        if (moddedEntities.ContainsKey(objectID))
        {
            var entities = moddedEntities[objectID];
            foreach (ObjectAuthoring entityData in entities)
            {
                if (entityData.variation == variation)
                {
                    entity = entityData;
                    return true;
                }
            }
        }

        entity = null;
        return false;
    }
    
    internal static ObjectAuthoring LoadPrefab(string itemId, string prefabPath)
    {
        GameObject prefab = ResourcesModule.LoadAsset<GameObject>(prefabPath);
        
        return CopyPrefab(itemId, prefab);
    }

    private static ObjectAuthoring CopyPrefab(string itemId, GameObject prefab)
    {
        GameObject newPrefab = Object.Instantiate(prefab);
        newPrefab.hideFlags = HideFlags.HideAndDontSave;

        var objectAuthoring = newPrefab.GetComponent<ObjectAuthoring>();
        if (objectAuthoring == null)
        {
            throw new InvalidOperationException(
                $"Error loading prefab for '{itemId}', no ObjectAuthoring found! " +
                "Core Lib does not support using EntityMonoBehaviourData!");
        }

        string fullItemId = $"{itemId}_{objectAuthoring.variation}";

        newPrefab.name = $"{fullItemId}_Prefab";

        GhostAuthoringComponent ghost = newPrefab.GetComponent<GhostAuthoringComponent>();
        if (ghost != null)
        {
            ghost.Name = itemId;
            ghost.prefabId = fullItemId.GetGUID();
        }

        return objectAuthoring;
    }

    private static void RegisterEntityModifications_Internal(Assembly assembly)
    {
        IEnumerable<Type> types = assembly.GetTypes().Where(Reflection.HasAttribute<EntityModificationAttribute>);

        foreach (Type type in types)
        {
            RegisterEntityModificationsInType_Internal(type);
        }
    }
    
    private static void RegisterPrefabModifications_Internal(Assembly assembly)
    {
        IEnumerable<Type> types = assembly.GetTypes().Where(Reflection.HasAttribute<PrefabModificationAttribute>);

        foreach (Type type in types)
        {
            RegisterPrefabModificationsInType_Internal(type);
        }
    }
    
    #endregion

    private static void RegisterEntityModificationsInType_Internal(Type type)
    {
        int modifiersCount = 0;

        IEnumerable<MethodInfo> methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic).Where(Reflection.HasAttribute<EntityModificationAttribute>);
        foreach (MethodInfo method in methods)
        {
            if (!Reflection.IsAction<MonoBehaviour>(method))
            {
                CoreLibMod.Log.LogError(
                    $"Failed to add modify method '{method.FullDescription()}', because method signature is incorrect. Should be void ({nameof(MonoBehaviour)})!");
                continue;
            }

            var attributes = method.GetCustomAttributes<EntityModificationAttribute>();
            Action<MonoBehaviour> modifyDelegate = (Action<MonoBehaviour>)method.CreateDelegate(typeof(Action<MonoBehaviour>));

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

        CoreLibMod.Log.LogInfo($"Registered {modifiersCount} entity modifiers in type {type.FullName}!");
    }
    
    private static void RegisterPrefabModificationsInType_Internal(Type type)
    {
        int modifiersCount = 0;

        IEnumerable<MethodInfo> methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic).Where(Reflection.HasAttribute<PrefabModificationAttribute>);
        foreach (MethodInfo method in methods)
        {
            if (!Reflection.IsAction<MonoBehaviour>(method))
            {
                CoreLibMod.Log.LogWarning(
                    $"Failed to add prefab modify method '{method.FullDescription()}', because method signature is incorrect. Should be void ({nameof(MonoBehaviour)})!");
                continue;
            }

            var attributes = method.GetCustomAttributes<PrefabModificationAttribute>();
            Action<MonoBehaviour> modifyDelegate = (Action<MonoBehaviour>)method.CreateDelegate(typeof(Action<MonoBehaviour>));
            
            foreach (PrefabModificationAttribute attribute in attributes)
            {
                if (attribute.targetType == null)
                {
                    CoreLibMod.Log.LogWarning($"Attribute on method '{method.FullDescription()}' has no type info!");
                    continue;
                }

                Type targetType = attribute.targetType;
                if (targetType == null)
                {
                    CoreLibMod.Log.LogWarning($"Type '{attribute.targetType.FullName}' is not registered in Il2Cpp!");
                    continue;
                }
                    
                prefabModifyFunctions.Add(targetType, modifyDelegate);
                modifiersCount++;
            }
        }

        CoreLibMod.Log.LogInfo($"Registered {modifiersCount} prefab modifiers in type {type.FullName}!");
    }

    #endregion
    }
}