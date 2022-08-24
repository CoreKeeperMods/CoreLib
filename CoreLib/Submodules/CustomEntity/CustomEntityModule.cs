using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
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
using JetBrains.Annotations;
using Unity.NetCode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CoreLib.Submodules.CustomEntity;

/// <summary>
/// This module provides means to add new content such as item.
/// Currently does not support adding blocks, NPCs and other non item entities!
/// </summary>
[CoreLibSubmodule(Dependencies = new[] { typeof(LocalizationModule), typeof(ResourcesModule) })]
[EntityModification]
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

    public static void RegisterModifications(Assembly assembly)
    {
        ThrowIfNotLoaded();
        ThrowIfTooLate();

        RegisterModifications_Internal(assembly);
    }


    /// <summary>
    /// Get item index from UNIQUE item id
    /// </summary>
    /// <param name="itemID">UNIQUE string item ID</param>
    public static ObjectID GetItemIndex(string itemID)
    {
        ThrowIfNotLoaded();
        if (modIDs.ContainsKey(itemID))
        {
            return (ObjectID)modIDs[itemID];
        }

        return ObjectID.None;
    }

    public static ObjectID AddModWorkbench(string itemId, string spritePath)
    {
        return AddModWorkbench(itemId, spritePath, null);
    }

    public static ObjectID AddModWorkbench(string itemId, string spritePath, List<CraftingData> recipe)
    {
        ThrowIfNotLoaded();
        ThrowIfTooLate();
        ObjectID workbenchId = AddWorkbench(itemId, spritePath, recipe);
        AddWorkbenchItem(rootWorkbenches.Last(), workbenchId);
        return workbenchId;
    }
    

    public static void AddWorkbenchItem(ObjectID workBenchId, ObjectID itemId)
    {
        ThrowIfNotLoaded();
        ThrowIfTooLate();
        if (entitiesToAdd.ContainsKey(workBenchId))
        {
            EntityMonoBehaviourData entity = entitiesToAdd[workBenchId];
            CraftingCDAuthoring craftingCdAuthoring = entity.gameObject.GetComponent<CraftingCDAuthoring>();
            bool isRootWorkbench = IsRootWorkbench(workBenchId);
            
            CoreLibPlugin.Logger.LogDebug($"Adding item {itemId.ToString()} to workbench {workBenchId.ToString()}");

            if (craftingCdAuthoring.canCraftObjects.Count < (isRootWorkbench ? 17 : 18))
            {
                craftingCdAuthoring.canCraftObjects.Add(new CraftableObject() { objectID = itemId, amount = 1 });
                return;
            }

            if (isRootWorkbench)
            {
                ObjectID newWorkbench = AddRootWorkbench();
                craftingCdAuthoring.canCraftObjects.Insert(0, new CraftableObject() { objectID = newWorkbench, amount = 1 });
                AddWorkbenchItem(newWorkbench, itemId);
                return;
            }

            CoreLibPlugin.Logger.LogWarning($"Workbench {workBenchId.ToString()} has ran out of slots!");
            return;
        }
        
        CoreLibPlugin.Logger.LogError($"Failed to add workbench item! Found no entities in the list with ID: {workBenchId}.");
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
        ThrowIfTooLate();

        Object gameObject = ResourcesModule.LoadAsset(path);
        if (gameObject == null)
        {
            CoreLibPlugin.Logger.LogInfo($"Failed to add entity, path: {path}");
            return ObjectID.None;
        }

        GameObject prefab = gameObject.Cast<GameObject>();
        GameObject newPrefab = Object.Instantiate(prefab);
        newPrefab.name = $"{itemId}_Prefab";
        newPrefab.hideFlags = HideFlags.HideAndDontSave;

        GhostAuthoringComponent ghost = newPrefab.GetComponent<GhostAuthoringComponent>();
        if (ghost != null)
        {
            ghost.Name = itemId;
            ghost.prefabId = Guid.NewGuid().ToString("N");
        }

        EntityMonoBehaviourData entity = newPrefab.GetComponent<EntityMonoBehaviourData>();

        int itemIndex = NextFreeId();
        itemIndex = modItemIDs.Bind("Items", itemId, itemIndex).Value;

        takenIDs.Add(itemIndex);
        modIDs.Add(itemId, itemIndex);

        entity.objectInfo.objectID = (ObjectID)itemIndex;

        entitiesToAdd.Add((ObjectID)itemIndex, entity);
        CoreLibPlugin.Logger.LogDebug($"Added entity {entity.objectInfo.objectID}, path: {path}!");
        return (ObjectID)itemIndex;
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
    /// Set entity with id EquipmentSkinCD's skin 
    /// </summary>
    /// <param name="id">Target Entity ID</param>
    /// <param name="skinId">new skin Index</param>
    public static void SetEquipmentSkin(ObjectID id, byte skinId)
    {
        ThrowIfNotLoaded();
        ThrowIfTooLate();

        if (entitiesToAdd.ContainsKey(id))
        {
            EntityMonoBehaviourData entity = entitiesToAdd[id];
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
            CoreLibPlugin.Logger.LogError($"Failed to set equipment skin! Found no entities in the list with ID: {id}.");
        }
    }

    #endregion

    #region PrivateImplementation

    private static bool _loaded;

    internal static Dictionary<ObjectID, EntityMonoBehaviourData> entitiesToAdd = new Dictionary<ObjectID, EntityMonoBehaviourData>();
    internal static Dictionary<ObjectID, Action<EntityMonoBehaviourData>> entityModifyFunctions = new Dictionary<ObjectID, Action<EntityMonoBehaviourData>>();

    internal static ConfigFile modItemIDs;
    internal static HashSet<int> takenIDs = new HashSet<int>();
    internal static Dictionary<string, int> modIDs = new Dictionary<string, int>();

    internal static List<ObjectID> rootWorkbenches = new List<ObjectID>();

    internal static PlayerCustomizationTable customizationTable;

    internal const Il2CppSystem.Reflection.BindingFlags all = Il2CppSystem.Reflection.BindingFlags.Instance | Il2CppSystem.Reflection.BindingFlags.Static | Il2CppSystem.Reflection.BindingFlags.Public | Il2CppSystem.Reflection.BindingFlags.NonPublic | Il2CppSystem.Reflection.BindingFlags.GetField | Il2CppSystem.Reflection.BindingFlags.SetField | Il2CppSystem.Reflection.BindingFlags.GetProperty | Il2CppSystem.Reflection.BindingFlags.SetProperty;


    public const int modIdRangeStart = 33000;
    public const int modIdRangeEnd = 65535;

    internal static int firstUnusedId = modIdRangeStart;

    internal static bool hasInjected;

    public const string RootWorkbench = "CoreLib:RootModWorkbench";
    
    [CoreLibSubmoduleInit(Stage = InitStage.SetHooks)]
    internal static void SetHooks()
    {
        CoreLibPlugin.harmony.PatchAll(typeof(MemoryManager_Patch));
        CoreLibPlugin.harmony.PatchAll(typeof(PugDatabaseAuthoring_Patch));
        CoreLibPlugin.harmony.PatchAll(typeof(Loading_Patch));
    }

    [CoreLibSubmoduleInit(Stage = InitStage.PostLoad)]
    internal static void Load()
    {
        BepInPlugin metadata = MetadataHelper.GetMetadata(typeof(CoreLibPlugin));
        modItemIDs = new ConfigFile($"{Paths.ConfigPath}/CoreLib/CoreLib.ModItemID.cfg", true, metadata);

        ClassInjector.RegisterTypeInIl2Cpp<EntityPrefabOverride>();
        ClassInjector.RegisterTypeInIl2Cpp<RuntimeMaterial>();
        RegisterModifications(Assembly.GetExecutingAssembly());

        AddRootWorkbench();
    }

    [EntityModification(ObjectID.Player)]
    private static void EditPlayer(EntityMonoBehaviourData entity)
    {
        CraftingCDAuthoring craftingCdAuthoring = entity.GetComponent<CraftingCDAuthoring>();
        craftingCdAuthoring.canCraftObjects.Add(new CraftableObject(){objectID = rootWorkbenches.First(), amount = 1});
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

    internal static void ThrowIfTooLate()
    {
        if (hasInjected)
        {
            throw new InvalidOperationException($"{nameof(CustomEntityModule)} method called too late. Entity injection is already done.");
        }
    }

    private static bool IsRootWorkbench(ObjectID objectID)
    {
        return rootWorkbenches.Contains(objectID);
    }

    private static int option = 0;
    private static string[] prefabs =
    {
        "Assets/CoreLib/Objects/TemplateWorkbench",
        "Assets/CoreLib/Objects/TemplateWorkbench-test"
    };
    
    private static ObjectID AddWorkbench(string itemId, string spritePath, List<CraftingData> recipe)
    {
        Sprite[] sprites = ResourcesModule.LoadSprites(spritePath).OrderSprites();
        if (sprites == null || sprites.Length != 2)
        {
            CoreLibPlugin.Logger.LogError($"Failed to add workbench! Provided sprite must be in 'Multiple' mode and have two sprites!");
            return ObjectID.None;
        }

        string prefab = prefabs[option];
        //option++;
      //  option %= 2;

        ObjectID id = AddEntity(itemId, prefab);
        if (entitiesToAdd.ContainsKey(id))
        {
            EntityMonoBehaviourData entity = entitiesToAdd[id];
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
            
            CoreLibPlugin.Logger.LogInfo($"Modifying crafting data for {entity.objectInfo.objectID}");
            
            CraftingCDAuthoring comp = entity.gameObject.AddComponent<CraftingCDAuthoring>();
            comp.craftingType = CraftingType.Simple;
            comp.canCraftObjects = new Il2CppSystem.Collections.Generic.List<CraftableObject>(4);

        }

        return id;
    }
    
    private static void RegisterModifications_Internal(Assembly assembly)
    {
        bool HasAttribute(MemberInfo type)
        {
            return type.GetCustomAttribute<EntityModificationAttribute>() != null;
        }

        IEnumerable<Type> types = assembly.GetTypes().Where(HasAttribute);
        int modifiersCount = 0;

        foreach (Type type in types)
        {
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
                    modifiersCount++;
                    if (entityModifyFunctions.ContainsKey(attribute.target))
                    {
                        entityModifyFunctions[attribute.target] += modifyDelegate;
                    }
                    else
                    {
                        entityModifyFunctions.Add(attribute.target, modifyDelegate);
                    }
                }
                else
                {
                    CoreLibPlugin.Logger.LogWarning(
                        $"Failed to add modify method '{method.FullDescription()}', because method signature is incorrect. Should be void ({nameof(EntityMonoBehaviourData)})!");
                }
            }
        }
        CoreLibPlugin.Logger.LogInfo($"Registered {modifiersCount} entity modifiers from {assembly.FullName}!");
    }

    private static bool IsIdFree(int id)
    {
        if (id is < modIdRangeStart or >= modIdRangeEnd)
        {
            return false;
        }

        if (modItemIDs.GetOrphanedEntries().Any(pair =>
            {
                if (int.TryParse(pair.Value, out int value))
                {
                    return value == id;
                }

                return false;
            }))
        {
            return false;
        }

        if (modItemIDs.Any(pair => { return (int)pair.Value.BoxedValue == id; }))
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

    #endregion
}