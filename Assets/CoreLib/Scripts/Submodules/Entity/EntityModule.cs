using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CoreLib.Submodules.Localization;
using CoreLib.Submodules.ModEntity.Atributes;
using CoreLib.Submodules.ModEntity.Interfaces;
using CoreLib.Submodules.ModEntity.Patches;
using CoreLib.Util;
using CoreLib.Util.Extensions;
using HarmonyLib;
using UnityEngine;

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
    
    /*
    /// <summary>
    /// Add custom workbench with specified sprite. It is automatically added to main mod workbench
    /// </summary>
    /// <param name="itemId">UNIQUE entity Id</param>
    /// <param name="spritePath">path to your sprite in asset bundle</param>
    /// <param name="variantsPath">path to variants texture</param>
    public static ObjectID AddModWorkbench(string itemId, string spritePath, string variantsPath)
    {
        return AddModWorkbench(itemId, spritePath, variantsPath, null, true);
    }

    /// <summary>
    /// Add custom workbench with specified sprite.
    /// </summary>
    /// <param name="itemId">UNIQUE entity Id</param>
    /// <param name="spritePath">path to your sprite in asset bundle</param>
    /// <param name="variantsPath">path to variants texture</param>
    public static ObjectID AddModWorkbench(string itemId, string spritePath, string variantsPath, bool bindToRootWorkbench)
    {
        return AddModWorkbench(itemId, spritePath, variantsPath, null, bindToRootWorkbench);
    }

    /// <summary>
    /// Add custom workbench with specified sprite. It is automatically added to main mod workbench
    /// </summary>
    /// <param name="itemId">UNIQUE entity Id</param>
    /// <param name="spritePath">path to your sprite in asset bundle</param>
    /// <param name="recipe">workbench craft recipe</param>
    /// <param name="variantsPath">path to variants texture</param>
    public static ObjectID AddModWorkbench(string itemId, string spritePath, string variantsPath, List<CraftingData> recipe)
    {
        return AddModWorkbench(itemId, spritePath, variantsPath, recipe, true);
    }

    /// <summary>
    /// Add custom workbench with specified sprite.
    /// </summary>
    /// <param name="itemId">UNIQUE entity Id</param>
    /// <param name="spritePath">path to your sprite in asset bundle</param>
    /// <param name="recipe">workbench craft recipe</param>
    /// <param name="variantsPath">path to variants texture</param>
    public static ObjectID AddModWorkbench(string itemId, string spritePath, string variantsPath, List<CraftingData> recipe, bool bindToRootWorkbench)
    {
        ThrowIfNotLoaded();
        ThrowIfTooLate(nameof(AddModWorkbench));
        ObjectID workbenchId = AddWorkbench(itemId, spritePath, variantsPath, recipe, true);
        if (bindToRootWorkbench)
        {
            ObjectID root = TryAddRootWorkbench();
            AddWorkbenchItem(root, workbenchId);
        }

        return workbenchId;
    }
    
    /// <summary>
    /// Add custom workbench with specified sprite.
    /// </summary>
    /// <param name="itemId">UNIQUE entity Id</param>
    /// <param name="spritePath">path to your sprite in asset bundle</param>
    /// <param name="recipe">workbench craft recipe</param>
    /// <param name="variantsPath">path to variants texture</param>
    public static ObjectID AddModWorkbench(string itemId, string bigIconPath, string smallIconPath, string variantsPath, List<CraftingData> recipe, bool bindToRootWorkbench)
    {
        ThrowIfNotLoaded();
        ThrowIfTooLate(nameof(AddModWorkbench));
        ObjectID workbenchId = AddWorkbench(itemId, bigIconPath, smallIconPath,variantsPath, recipe, true);
        if (bindToRootWorkbench)
        {
            ObjectID root = TryAddRootWorkbench();
            AddWorkbenchItem(root, workbenchId);
        }

        return workbenchId;
    }

    internal static ObjectID AddModWorkbench(string itemId, Sprite bigIconPath, Sprite smallIconPath, Texture2D variantsTexture, List<CraftingData> recipe, bool bindToRootWorkbench)
    {
        ThrowIfNotLoaded();
        ThrowIfTooLate(nameof(AddModWorkbench));
        ObjectID workbenchId = AddWorkbench(itemId, bigIconPath, smallIconPath, variantsTexture, recipe, true);
        if (bindToRootWorkbench)
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
            EntityMonoBehaviourData workbenchEntity = modWorkbenchesChain[workBenchId].Last();
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
    */

    /// <summary>
    /// Add new entity.
    /// </summary>
    /// <param name="itemId">UNIQUE entity id</param>
    /// <param name="prefabPath">path to your prefab in asset bundle</param>
    /// <returns>Added objectID. If adding failed returns <see cref="ObjectID.None"/></returns>
    /// <exception cref="InvalidOperationException">Throws if called too late</exception>
    [Obsolete("Use Mod SDK instead")]
    public static ObjectID AddEntity(string itemId, string prefabPath)
    {
        return ObjectID.None;
    }

    /// <summary>
    /// Add new entity with variations. Each prefab must have variation field set.
    /// </summary>
    /// <param name="itemId">UNIQUE entity id</param>
    /// <param name="prefabsPaths">paths to your prefabs in asset bundle</param>
    /// <returns>Added objectID. If adding failed returns <see cref="ObjectID.None"/></returns>
    /// <exception cref="InvalidOperationException">Throws if called too late</exception>
    [Obsolete("Use Mod SDK instead")]
    public static ObjectID AddEntityWithVariations(string itemId, string[] prefabsPaths)
    {
        return ObjectID.None;
    }

    [Obsolete("Use Mod SDK instead")]
    public static ObjectID AddEntityWithVariations(string itemId, List<EntityMonoBehaviourData> entities)
    {
        return ObjectID.None;
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

    internal static Dictionary<ObjectID, List<EntityMonoBehaviourData>> entitiesToAdd = new Dictionary<ObjectID, List<EntityMonoBehaviourData>>();
    internal static Dictionary<ObjectID, Action<MonoBehaviour>> entityModifyFunctions = new Dictionary<ObjectID, Action<MonoBehaviour>>();
    internal static Dictionary<string, Action<MonoBehaviour>> modEntityModifyFunctions = new Dictionary<string, Action<MonoBehaviour>>();
    internal static Dictionary<Type, Action<MonoBehaviour>> prefabModifyFunctions = new Dictionary<Type, Action<MonoBehaviour>>();
    
    internal static HashSet<Type> loadedPrefabTypes = new HashSet<Type>();
    internal static Dictionary<ObjectID, List<EntityMonoBehaviourData>> modWorkbenchesChain = new Dictionary<ObjectID, List<EntityMonoBehaviourData>>();
    internal static Dictionary<ObjectID, Texture2D> workbenchTextures = new Dictionary<ObjectID, Texture2D>();

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
        modEntityIDs = new IdBindConfigFile($"CoreLib/CoreLib.ModEntityID.cfg", modEntityIdRangeStart, modEntityIdRangeEnd);
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
            CraftingAuthoring craftingCdAuthoring = entity.GetComponent<CraftingAuthoring>();
            craftingCdAuthoring.canCraftObjects.Add(new CraftingAuthoring.CraftableObject() { objectID = rootWorkbench.Value, amount = 1 });
        }
    }

    [PrefabModification(typeof(SimpleCraftingBuilding))]
    private static void EditSimpleCraftingBuilding(MonoBehaviour entityMono)
    {
        SimpleCraftingBuilding craftingBuilding = (SimpleCraftingBuilding)entityMono;

        foreach (var pair in workbenchTextures)
        {
            var textureList = new List<Texture2D>(1);
            textureList.Add(pair.Value);
            var reskinInfo = new EntityMonoBehaviour.ReskinInfo()
            {
                objectIDToUseReskinOn = pair.Key,
                variation = 0,
                textures = textureList
            };
            
            foreach (var reskinOption in craftingBuilding.reskinOptions)
            {
                reskinOption.reskins.Add(reskinInfo);
            }
        }
    }
    

    private static void CloneWorkbench(EntityMonoBehaviourData oldWorkbench)
    {
        string prevId = GetObjectStringId(oldWorkbench.objectInfo.objectID);
        string newId = IncrementID(prevId);

       /* ObjectID newWorkbench = AddWorkbench(newId, "Assets/CoreLib/Textures/modWorkbench","Assets/CoreLib/Textures/modWorkbenchVariants", null, false);

        if (GetMainEntity(newWorkbench, out EntityMonoBehaviourData entity))
        {
            AddAdditionalWorkbench(oldWorkbench.objectInfo.objectID, entity);
            CraftingAuthoring crafting = oldWorkbench.gameObject.GetComponent<CraftingAuthoring>();
            crafting.includeCraftedObjectsFromBuildings.Add(entity.gameObject.GetComponent<CraftingAuthoring>());
        }*/
    }

    private static void AddAdditionalWorkbench(ObjectID root, EntityMonoBehaviourData entity)
    {
        if (modWorkbenchesChain.TryGetValue(root, out var value))
        {
            value.Add(entity);
        }
        else
        {
            modWorkbenchesChain.Add(root, new List<EntityMonoBehaviourData>()
            {
                entity
            });
        }
    }
    
    /*
    private static ObjectID TryAddRootWorkbench()
    {
        if (rootWorkbench == null)
        {
            ObjectID workbench = AddWorkbench(RootWorkbench, "Assets/CoreLib/Textures/modWorkbench", "Assets/CoreLib/Textures/modWorkbenchVariants", null, true);
            rootWorkbench = workbench;
            LocalizationModule.AddEntityLocalization(workbench, $"Root Workbench", "This workbench contains all modded workbenches!");
        }
        return rootWorkbench.Value;
    }
    
    private static ObjectID AddWorkbench(string itemId, string bigIconPath, string smallIconPath, string variantsPath, List<CraftingData> recipe, bool isPrimary)
    {
        Sprite bigIcon = ResourcesModule.LoadAsset<Sprite>(bigIconPath);
        Sprite smallIcon = ResourcesModule.LoadAsset<Sprite>(smallIconPath);
        Texture2D skinsTexture = ResourcesModule.LoadAsset<Texture2D>(variantsPath);

        return AddWorkbench(itemId, bigIcon, smallIcon, skinsTexture, recipe, isPrimary);
    }

    private static ObjectID AddWorkbench(string itemId, string spritePath, string variantsPath, List<CraftingData> recipe, bool isPrimary)
    {
        Sprite[] sprites = ResourcesModule.LoadSprites(spritePath).OrderSprites();
        if (sprites == null || sprites.Length != 2)
        {
            CoreLibMod.Log.LogError($"Failed to add workbench! Provided sprite must be in 'Multiple' mode and have two sprites!");
            return ObjectID.None;
        }

        Texture2D skinsTexture = ResourcesModule.LoadAsset<Texture2D>(variantsPath);

        return AddWorkbench(itemId, sprites[0], sprites[1], skinsTexture, recipe, isPrimary);
    }
    
    private static ObjectID AddWorkbench(string itemId, Sprite bigIcon, Sprite smallIcon, Texture2D variantsTexture, List<CraftingData> recipe, bool isPrimary)
    {
        ObjectID id = AddEntity(itemId, "TemplateWorkbench");
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
                }).ToList();
            }

            CraftingAuthoring comp = entity.gameObject.AddComponent<CraftingAuthoring>();
            comp.craftingType = CraftingType.Simple;
            comp.canCraftObjects = new List<CraftingAuthoring.CraftableObject>(4);
            comp.includeCraftedObjectsFromBuildings = new List<CraftingAuthoring>();
            
            workbenchTextures.Add(id, variantsTexture);
            
            if(isPrimary)
                AddAdditionalWorkbench(id, entity);
        }

        return id;
    }*/

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