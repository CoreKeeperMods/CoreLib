using System;
using System.Collections.Generic;
using System.Linq;
using CoreLib.Data;
using CoreLib.Submodule.Entity.Atributes;
using CoreLib.Submodule.Entity.Components;
using CoreLib.Submodule.Entity.Interfaces;
using CoreLib.Submodule.Entity.Patches;
using CoreLib.Submodule.Localization;
using CoreLib.Submodule.Resource;
using CoreLib.Util;
using CoreLib.Util.Extensions;
using HarmonyLib;
using Pug.Sprite;
using PugMod;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using Object = UnityEngine.Object;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Entity
{
    /// <summary>
    /// Represents a module for managing custom entities, prefabs, and modifications within the framework.
    /// This class provides functionality for registering and managing entities, customization, and pooling,
    /// as well as applying modifications and managing dependencies.
    /// </summary>
    public class EntityModule : BaseSubmodule
    {
        
        #region PublicInterface
        
        public new const string Name = "Core Lib Entity";
        
        /// <summary>
        /// A static event that triggers when material swapping is ready to be applied.
        /// This event is utilized by modules to respond to changes in material configuration
        /// or execute necessary updates related to material swaps.
        /// </summary>
        public static event Action MaterialSwapReady;

        /// <summary>
        /// Adds the specified GameObject to the authoring list for processing.
        /// </summary>
        /// <param name="gameObject">The GameObject to be added to the authoring list.</param>
        public static void AddToAuthoringList(GameObject gameObject)
        {
            ModAuthoringTargets.Add(gameObject);
        }

        /// <summary>
        /// Enables pooling for the specified GameObject, adding it to the list of poolable prefabs.
        /// </summary>
        /// <param name="gameObject">The GameObject to enable pooling for.</param>
        public static void EnablePooling(GameObject gameObject)
        {
            PoolablePrefabs.Add(gameObject);
        }

        /// <summary>
        /// Registers entity modification functions for a specified mod by its ID.
        /// </summary>
        /// <param name="modId">The unique identifier of the mod whose entities are to be modified.</param>
        public static void RegisterEntityModifications(long modId)
        {
            Instance.ThrowIfNotLoaded();
            ThrowIfTooLate(nameof(RegisterEntityModifications));

            RegisterEntityModifications_Internal(modId);
        }

        /// <summary>
        /// Registers modifications to a prefab associated with the specified mod ID.
        /// </summary>
        /// <param name="modId">The unique identifier of the mod to register modifications for.</param>
        public static void RegisterPrefabModifications(long modId)
        {
            Instance.ThrowIfNotLoaded();
            ThrowIfTooLate(nameof(RegisterPrefabModifications));

            RegisterPrefabModifications_Internal(modId);
        }

        /// <summary>
        /// Registers entity modification methods defined within the specified type.
        /// </summary>
        /// <param name="type">The type containing the entity modifications to register.</param>
        public static void RegisterEntityModifications(Type type)
        {
            Instance.ThrowIfNotLoaded();
            ThrowIfTooLate(nameof(RegisterEntityModifications));

            RegisterEntityModificationsInType_Internal(type);
        }

        /// <summary>
        /// Registers specific modifications to apply to prefabs based on the provided type.
        /// </summary>
        /// <param name="type">The type defining the modifications for the prefabs.</param>
        public static void RegisterPrefabModifications(Type type)
        {
            Instance.ThrowIfNotLoaded();
            ThrowIfTooLate(nameof(RegisterPrefabModifications));

            RegisterPrefabModificationsInType_Internal(type);
        }

        /// <summary>
        /// Retrieves the ObjectType corresponding to the specified type name, adding it to the collection if not already present.
        /// </summary>
        /// <param name="typeName">The name of the type to retrieve or define as an ObjectType.</param>
        /// <returns>The ObjectType corresponding to the specified type name.</returns>
        public static ObjectType GetObjectType(string typeName)
        {
            Instance.ThrowIfNotLoaded();

            int index = ObjectTypeIDs.HasIndex(typeName) ? ObjectTypeIDs.GetIndex(typeName) : ObjectTypeIDs.GetNextId(typeName);
            return (ObjectType)index;
        }

        /// <summary>
        /// Adds a Mod Workbench to the entity module for processing.
        /// </summary>
        /// <param name="workbenchDefinition">The definition of the Workbench to be added.</param>
        public static void AddModWorkbench(WorkbenchDefinition workbenchDefinition)
        {
            Instance.ThrowIfNotLoaded();
            ThrowIfTooLate(nameof(AddModWorkbench));
            AddWorkbench(workbenchDefinition);
            if (workbenchDefinition.bindToRootWorkbench)
            {
                AddRootWorkbenchItem(workbenchDefinition.itemId);
            }
        }

        /// <summary>
        /// Adds a new entity with the specified item ID and prefab path.
        /// </summary>
        /// <param name="itemId">The unique identifier for the entity to be added.</param>
        /// <param name="prefabPath">The path to the prefab in the asset bundle.</param>
        /// <returns>The ID of the added object if successful, or <see cref="ObjectID.None"/> if the addition failed.</returns>
        /// <exception cref="InvalidOperationException">Thrown if this method is called too late in the initialization process.</exception>
        public static void AddEntity(string itemId, string prefabPath)
        {
            AddEntityWithVariations(itemId, new[] { prefabPath });
        }

        /// <summary>
        /// Adds a new entity to the system.
        /// </summary>
        /// <param name="itemId">The unique identifier for the entity to be added.</param>
        /// <param name="prefab">The prefab associated with the entity.</param>
        /// <returns>The ObjectID of the added entity. Returns <see cref="ObjectID.None"/> if the addition fails.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the method is called too late in the processing sequence.</exception>
        public static void AddEntity(string itemId, ObjectAuthoring prefab)
        {
            AddEntityWithVariations(itemId, new List<ObjectAuthoring> { prefab });
        }

        /// <summary>
        /// Adds a new entity with multiple variations. Each prefab in the provided paths must include a variation field set.
        /// </summary>
        /// <param name="itemId">The unique identifier for the entity.</param>
        /// <param name="prefabsPaths">An array of paths to the prefabs included in the entity's variations, located within the asset bundle.</param>
        public static void AddEntityWithVariations(string itemId, string[] prefabsPaths)
        {
            Instance.ThrowIfNotLoaded();
            ThrowIfTooLate(nameof(AddEntityWithVariations));

            if (prefabsPaths.Length == 0)
            {
                Log.LogError($"Failed to add entity {itemId}: prefabsPaths has no paths!");
                return;
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
                    Log.LogError($"Failed to add entity {itemId}, prefab {prefabPath} is missing!");
                    return;
                }
            }
            
            AddEntityWithVariations(itemId, entities);
        }

        /// <summary>
        /// Adds an entity with multiple variations to the system for registration and processing.
        /// </summary>
        /// <param name="itemId">The unique identifier for the entity being added.</param>
        /// <param name="prefabs">A list of entity prefabs to be registered as variations of the specified entity.</param>
        public static void AddEntityWithVariations(string itemId, List<ObjectAuthoring> prefabs)
        {
            foreach (ObjectAuthoring prefab in prefabs)
            {
                prefab.objectName = itemId;
                API.Authoring.RegisterAuthoringGameObject(prefab.gameObject);
                AddToAuthoringList(prefab.gameObject);
            }

            ModdedEntities.Add(itemId, prefabs);
        }

        /// <summary>
        /// Adds a custom player customization texture sheet to the customization table.
        /// </summary>
        /// <typeparam name="T">The type of the customization skin, derived from <see cref="SkinBase"/>.</typeparam>
        /// <param name="skin">The customization skin containing texture sheet information.</param>
        /// <returns>The index of the newly added skin. Returns 0 if the addition fails.</returns>
        public static byte AddPlayerCustomization<T>(T skin)
            where T : SkinBase
        {
            Instance.ThrowIfNotLoaded();
            InitCustomizationTable();

            try
            {
                var list = (List<T>)GetSkinList<T>();
                var sortedList = (List<T>)GetSortedSkinList<T>();

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
                Log.LogError($"Failed to add player customization of type {typeof(T).FullName}, because there is no such customization table!");
            }

            return 0;
        }

        /// <summary>
        /// Registers a new dynamic item handler of the specified type if it is not already registered.
        /// </summary>
        /// <typeparam name="T">The type of the dynamic item handler to register. Must implement <see cref="IDynamicItemHandler"/> and have a parameterless constructor.</typeparam>
        /// <remarks>
        /// If a handler of the specified type is already registered, a warning will be logged, and the handler will not be registered again.
        /// </remarks>
        public static void RegisterDynamicItemHandler<T>()
            where T : IDynamicItemHandler, new()
        {
            if (DynamicItemHandlers.Any(handler => handler.GetType() == typeof(T)))
            {
                Log.LogWarning($"Failed to register dynamic handler {typeof(T).FullName}, because it is already registered!");
                return;
            }

            T handler = Activator.CreateInstance<T>();
            DynamicItemHandlers.Add(handler);
        }

        #endregion

        #region PrivateImplementation

        /// <summary>
        /// Represents a delegate that defines a modification action applied to an Entity in combination with
        /// a GameObject and an EntityManager. This delegate facilitates modifications to the specified
        /// Entity and its associated Unity GameObject using the provided EntityManager instance.
        /// </summary>
        /// <param name="arg1">The Entity to be modified.</param>
        /// <param name="arg2">The GameObject associated with the Entity.</param>
        /// <param name="arg3">An instance of EntityManager used for managing the Entity and its state.</param>
        internal delegate void ModifyAction(Unity.Entities.Entity arg1, GameObject arg2, EntityManager arg3);

        /// <summary>
        /// A static property that provides access to the singleton instance of the <see cref="EntityModule"/> class.
        /// The instance is used to manage functionality related to entity modifications, prefab interactions,
        /// and other module-specific behaviors within the framework.
        /// </summary>
        internal static EntityModule Instance => CoreLibMod.GetModuleInstance<EntityModule>();

        /// <summary>
        /// A collection of module dependencies that must be loaded before this module.
        /// Identifies the prerequisite modules required for the proper functionality
        /// of the current module.
        /// </summary>
        internal override Type[] Dependencies => new[] { typeof(LocalizationModule), typeof(ResourcesModule) };

        /// <summary>
        /// A static collection that holds a list of GameObjects designated for authoring or modification
        /// during the modding process. This list is primarily used to track and apply changes to entities
        /// before they are finalized or integrated into the game.
        /// </summary>
        internal static List<GameObject> ModAuthoringTargets = new();

        /// <summary>
        /// A static list of GameObject instances intended for pooling purposes.
        /// This collection holds prefabs that can be reused or instantiated through
        /// pooling systems, optimizing memory and performance for frequently used entities.
        /// </summary>
        internal static List<GameObject> PoolablePrefabs = new();

        /// <summary>
        /// A static dictionary that serves as a centralized repository for modded entities and their associated variations.
        /// The key represents the unique identifier of an entity, and the value is a list of its variations, allowing for modular
        /// and customizable entity management.
        /// </summary>
        internal static Dictionary<string, List<ObjectAuthoring>> ModdedEntities = new();

        /// <summary>
        /// A dictionary that maps object identifiers (<see cref="ObjectID"/>) to associated modification actions.
        /// These modification actions are delegates that enable custom behavior or adjustments to entities,
        /// allowing dynamic and context-specific operations during runtime.
        /// This is internally utilized by the entity subsystem to apply modifications to objects
        /// during their creation or initialization.
        /// </summary>
        internal static Dictionary<ObjectID, ModifyAction> EntityModifyFunctions = new();

        /// <summary>
        /// A static dictionary used to associate string identifiers with modification actions
        /// that can be applied to entities and their corresponding game objects. This dictionary
        /// is designed to facilitate the dynamic extension of entity behaviors or properties
        /// at runtime by mapping specific logic into modifiable targets.
        /// </summary>
        internal static Dictionary<string, ModifyAction> ModEntityModifyFunctions = new();

        /// <summary>
        /// A dictionary that maps `Type` objects to `<see cref="MonoBehaviour"/>` delegates for modifying prefabs.
        /// This is used to store and apply custom modification actions to prefabs of specific types
        /// during runtime or initialization within the entity module system.
        /// </summary>
        internal static Dictionary<Type, Action<MonoBehaviour>> PrefabModifyFunctions = new();

        /// <summary>
        /// A static collection of modifiable workbench definitions utilized by the system.
        /// This list serves as a centralized repository for managing and accessing all
        /// registered workbench types, enabling dynamic crafting functionalities and related behavior.
        /// </summary>
        internal static List<WorkbenchDefinition> ModWorkbenches = new();

        /// <summary>
        /// A static list representing a chain of root workbench entities within the system.
        /// It is used to manage and track the hierarchical order of workbench definitions.
        /// This list is frequently accessed or modified to update crafting-related associations.
        /// </summary>
        internal static List<ObjectAuthoring> RootWorkbenchesChain = new();

        /// <summary>
        /// Represents the root workbench definition utilized as the primary container
        /// for organizing and managing all modded workbenches within the system.
        /// This definition plays a central role in initializing, maintaining, and
        /// localizing the root workbench, ensuring proper linkage with modded entities
        /// and facilitating related operations.
        /// </summary>
        internal static WorkbenchDefinition RootWorkbenchDefinition;

        /// <summary>
        /// A static list that holds registered dynamic item handlers implementing the <see cref="IDynamicItemHandler"/> interface.
        /// This collection is used within the module to invoke specific handler logic related to dynamic item processing,
        /// enabling customized runtime behavior for modifiable entities.
        /// </summary>
        internal static List<IDynamicItemHandler> DynamicItemHandlers = new();

        /// <summary>
        /// Represents a static instance of the IdBind class that is responsible for managing object type identifiers.
        /// This variable provides functionality for maintaining, indexing, and assigning unique IDs for object types
        /// within the context of the EntityModule.
        /// </summary>
        internal static IdBind ObjectTypeIDs;

        /// <summary>
        /// A collection that maintains a set of busy or in-use unique identifiers.
        /// It is used to track IDs that are currently engaged in specific operations,
        /// preventing conflicts or redundant processing within the module's workflow.
        /// </summary>
        internal static HashSet<int> BusyIDsSet = new();

        /// <summary>
        /// Represents the player's customization data table, containing configurations
        /// and collections of skins or other customization options for the player character.
        /// This table is utilized to load and manage player-specific visual customizations
        /// such as body, clothing, and armor skins.
        /// </summary>
        internal static PlayerCustomizationTable CustomizationTable;

        /// <summary>
        /// A constant integer value representing the starting point of the range for
        /// modded object type IDs. This is used to ensure that custom mod object type
        /// IDs have a dedicated and distinct range, avoiding conflicts with other identifiers.
        /// </summary>
        internal const int ModObjectTypeIdRangeStart = 33000;

        /// <summary>
        /// Represents the exclusive upper boundary for the range of object type IDs
        /// reserved for modded entities within the system. This value is typically used
        /// to ensure that object type IDs allocated for mod entities remain within
        /// a defined, valid range.
        /// </summary>
        internal const int ModObjectTypeIdRangeEnd = ushort.MaxValue;

        /// <summary>
        /// A static flag indicating whether the injection of modified entities and associated data
        /// into the game engine or memory system has been completed. This flag is utilized to
        /// prevent further modifications or injections after the initial process is finalized.
        /// </summary>
        internal static bool HasInjected;

        #region Initialization

        /// <summary>
        /// Applies necessary hooks and patches related to the EntityModule for core functionality.
        /// </summary>
        internal override void SetHooks()
        {
            CoreLibMod.Patch(typeof(MemoryManager_Patch));
            CoreLibMod.Patch(typeof(PlayerController_Patch));
            CoreLibMod.Patch(typeof(ColorReplacer_Patch));
            CoreLibMod.Patch(typeof(GraphicalObjectConversion_Patch));
        }

        /// <summary>
        /// Loads the entity module and triggers the refresh of module bundles.
        /// </summary>
        internal override void Load()
        {
            ResourcesModule.RefreshModuleBundles();
        }

        /// <summary>
        /// Initializes and processes post-load operations for the EntityModule.
        /// This includes setting up object type identifiers, loading required assets,
        /// registering entity and prefab modifications, and subscribing to object type addition events.
        /// </summary>
        internal override void PostLoad()
        {
            ObjectTypeIDs = new IdBind(ModObjectTypeIdRangeStart, ModObjectTypeIdRangeEnd);
            RootWorkbenchDefinition = ResourcesModule.LoadAsset<WorkbenchDefinition>("Assets/CoreLibPackage/CoreLib.Entity/RootWorkbench");

            RegisterEntityModifications(typeof(EntityModule));
            RegisterPrefabModifications(typeof(EntityModule));

            API.Authoring.OnObjectTypeAdded += OnObjectTypeAdded;
        }

        /// <summary>
        /// Throws an exception if the method is called after entity injection has already been completed.
        /// </summary>
        /// <param name="methodName">The name of the method attempting to execute.</param>
        internal static void ThrowIfTooLate(string methodName)
        {
            if (HasInjected)
            {
                throw new InvalidOperationException($"{nameof(EntityModule)}.{methodName}() method called too late! Entity injection is already done.");
            }
        }

        /// <summary>
        /// Initializes the player customization table by loading the configuration
        /// data and populating it with relevant skin identifiers.
        /// </summary>
        /// <remarks>
        /// This method ensures that the player customization table is only
        /// initialized once. It attempts to load the "PlayerCustomizationTable" resource
        /// and processes the breast armor skins to add their IDs to the tracking set.
        /// </remarks>
        private static void InitCustomizationTable()
        {
            if (CustomizationTable != null) return;
            CustomizationTable = Resources.Load<PlayerCustomizationTable>($"PlayerCustomizationTable");
            foreach (var armorSkin in CustomizationTable.breastArmorSkins)
            {
                BusyIDsSet.Add(armorSkin.id);
            }
        }

        #endregion

        #region Workbenches

        /// <summary>
        /// Modifies the specified entity associated with the player based on the provided GameObject and EntityManager.
        /// </summary>
        /// <param name="entity">The player entity to be modified.</param>
        /// <param name="authoring">The GameObject providing authoring data for the modification.</param>
        /// <param name="entityManager">The EntityManager instance managing the entities and their associated components.</param>
        [EntityModification(ObjectID.Player)]
        private static void EditPlayer(Unity.Entities.Entity entity, GameObject authoring, EntityManager entityManager)
        {
            if (RootWorkbenchesChain.Count <= 0) return;
            var lastRootWorkbenchId = API.Authoring.GetObjectID(RootWorkbenchesChain.Last().objectName);

            var canCraftBuffer = entityManager.GetBuffer<CanCraftObjectsBuffer>(entity);
            var lastItem = canCraftBuffer[^1];

            if (lastItem.objectID == ObjectID.None)
            {
                lastItem.objectID = lastRootWorkbenchId;
                lastItem.amount = 1;
                canCraftBuffer[^1] = lastItem;
            }
            else
            {
                canCraftBuffer.Add(new CanCraftObjectsBuffer
                {
                    objectID = lastRootWorkbenchId,
                    amount = 1,
                    entityAmountToConsume = 0
                });
            }
        }

        /// <summary>
        /// Initializes the root workbench by adding it to the root workbenches chain and setting up its localization and connections.
        /// </summary>
        private static void AddRootWorkbench()
        {
            RootWorkbenchDefinition.itemId = IncrementID(RootWorkbenchDefinition.itemId);

            AddWorkbench(RootWorkbenchDefinition);
            if (GetMainEntity(RootWorkbenchDefinition.itemId, out ObjectAuthoring entity))
            {
                if (RootWorkbenchesChain.Count > 0)
                {
                    var oldWorkbench = RootWorkbenchesChain.Last();
                    var crafting = oldWorkbench.gameObject.GetComponent<ModCraftingAuthoring>();
                    crafting.includeCraftedObjectsFromBuildings.Add(RootWorkbenchDefinition.itemId);
                }

                RootWorkbenchesChain.Add(entity);
            }

            LocalizationModule.AddEntityLocalization(RootWorkbenchDefinition.itemId, $"Root Workbench {RootWorkbenchesChain.Count}",
                "This workbench contains all modded workbenches!");
        }

        /// <summary>
        /// Adds the specified entity ID as an item to the root workbench.
        /// </summary>
        /// <param name="entityId">The unique identifier of the entity to be added as a crafting item in the root workbench.</param>
        private static void AddRootWorkbenchItem(string entityId)
        {
            Instance.ThrowIfNotLoaded();
            ThrowIfTooLate(nameof(AddModWorkbench));
            
            if (RootWorkbenchesChain.Count == 0) 
                AddRootWorkbench();
            
            while (true)
            {
                ObjectAuthoring workbenchEntity = RootWorkbenchesChain.Last();
                ModCraftingAuthoring craftingCdAuthoring = workbenchEntity.gameObject.GetComponent<ModCraftingAuthoring>();

                Log.LogInfo($"Adding item {entityId} to root workbench");

                if (craftingCdAuthoring.canCraftObjects.Count < 18)
                {
                    craftingCdAuthoring.canCraftObjects.Add(new InventoryItemAuthoring.CraftingObject { objectName = entityId, amount = 1 });
                    return;
                }

                AddRootWorkbench();
            }
        }

        /// <summary>
        /// Adds the specified WorkbenchDefinition to the game as a new workbench.
        /// </summary>
        /// <param name="workbenchDefinition">The WorkbenchDefinition to be added, containing configuration and identifiers for the new workbench.</param>
        private static void AddWorkbench(WorkbenchDefinition workbenchDefinition)
        {
            AddEntity(workbenchDefinition.itemId, "Assets/CoreLibPackage/CoreLib.Entity/Prefab/TemplateWorkbench");
            if (GetMainEntity(workbenchDefinition.itemId, out ObjectAuthoring entity))
            {
                var itemAuthoring = entity.GetComponent<InventoryItemAuthoring>();

                itemAuthoring.icon = workbenchDefinition.bigIcon;
                itemAuthoring.smallIcon = workbenchDefinition.smallIcon;
                itemAuthoring.requiredObjectsToCraft = workbenchDefinition.recipe;

                ModCraftingAuthoring comp = entity.gameObject.AddComponent<ModCraftingAuthoring>();
                comp.craftingType = CraftingType.Simple;
                comp.canCraftObjects = workbenchDefinition.canCraft;
                comp.includeCraftedObjectsFromBuildings = workbenchDefinition.relatedWorkbenches;
                
                SpriteAsset targetSkin = RootWorkbenchDefinition.assetSkin.targetAsset;
                if (workbenchDefinition.assetSkin.targetAsset != targetSkin)
                {
                    workbenchDefinition.assetSkin.SetValue("m_targetAsset", targetSkin);
                    Log.LogInfo($"Changed {workbenchDefinition.itemId} target asset to root workbench asset");
                }
                ModWorkbenches.Add(workbenchDefinition);
                
                if (!PoolablePrefabs.Contains(entity.graphicalPrefab))
                    EnablePooling(entity.graphicalPrefab);
            }
        }

        /// <summary>
        /// Increments the identifier by appending or modifying an index component within the ID format.
        /// </summary>
        /// <param name="prevId">The previous identifier to be incremented or processed.</param>
        /// <returns>A new identifier string with the incremented or updated index component.</returns>
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

        /// <summary>
        /// Applies all modification authoring to the targeted GameObjects in the mod authoring list.
        /// This includes resolving and applying prefab modifications and graphical changes to associated objects.
        /// Invokes the MaterialSwapReady event prior to processing the GameObjects.
        /// </summary>
        internal static void ApplyAllModAuthoring()
        {
            MaterialSwapReady?.Invoke();
            foreach (var gameObject in ModAuthoringTargets)
            {
                var objectAuthoring = gameObject.GetComponent<ObjectAuthoring>();
                var entityData = gameObject.GetComponent<EntityMonoBehaviourData>();

                MonoBehaviour dataMonoBehaviour = objectAuthoring != null ? objectAuthoring : entityData;
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

        /// <summary>
        /// Retrieves the primary entity associated with the specified object ID if it exists.
        /// </summary>
        /// <param name="objectID">The unique identifier of the object to retrieve the entity for.</param>
        /// <param name="entity">The output parameter that will contain the primary entity associated with the specified object ID if found; otherwise, null.</param>
        /// <returns>True if the main entity was found for the specified object ID; otherwise, false.</returns>
        internal static bool GetMainEntity(string objectID, out ObjectAuthoring entity)
        {
            if (ModdedEntities.TryGetValue(objectID, out var moddedEntity))
            {
                entity = moddedEntity[0];
                return true;
            }

            entity = null;
            return false;
        }

        /// <summary>
        /// Retrieves an entity with the specified object ID and variation, if it exists.
        /// </summary>
        /// <param name="objectID">The unique identifier of the object to retrieve.</param>
        /// <param name="variation">The variation of the object to retrieve.</param>
        /// <param name="entity">When this method returns, contains the matching ObjectAuthoring instance if found; otherwise, null.</param>
        /// <returns>True if an entity with the specified object ID and variation is found; otherwise, false.</returns>
        internal static bool GetEntity(string objectID, int variation, out ObjectAuthoring entity)
        {
            if (ModdedEntities.TryGetValue(objectID, out var entities))
            {
                foreach (var entityData in entities)
                {
                    if (entityData.variation != variation) continue;
                    entity = entityData;
                    return true;
                }
            }

            entity = null;
            return false;
        }

        /// <summary>
        /// Loads a prefab based on the given item ID and prefab path, and prepares it for further processing.
        /// </summary>
        /// <param name="itemId">The unique identifier for the item associated with the prefab.</param>
        /// <param name="prefabPath">The resource path of the prefab to be loaded.</param>
        /// <returns>An instance of <c>ObjectAuthoring</c> representing the loaded and processed prefab.</returns>
        internal static ObjectAuthoring LoadPrefab(string itemId, string prefabPath)
        {
            GameObject prefab = ResourcesModule.LoadAsset<GameObject>(prefabPath);

            return CopyPrefab(itemId, prefab);
        }

        /// <summary>
        /// Creates a copy of the given prefab, configures it for authoring, and returns the associated ObjectAuthoring component.
        /// </summary>
        /// <param name="itemId">The unique identifier for the item associated with the prefab.</param>
        /// <param name="prefab">The GameObject to be copied and configured as a prefab.</param>
        /// <returns>The ObjectAuthoring component of the newly created prefab.</returns>
        private static ObjectAuthoring CopyPrefab(string itemId, GameObject prefab)
        {
            GameObject newPrefab = Object.Instantiate(prefab);
            newPrefab.hideFlags = HideFlags.HideAndDontSave;

            var objectAuthoring = newPrefab.GetComponent<ObjectAuthoring>();
            var templateObject = newPrefab.GetComponent<TemplateObject>();
            if (templateObject != null)
            {
                objectAuthoring = templateObject.Convert();
            }

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
                ghost.SetValue("prefabId", itemId.GetGUID());
            }

            return objectAuthoring;
        }

        #endregion

        #region Modification

        /// <summary>
        /// Registers entity modifications defined in the specified mod.
        /// </summary>
        /// <param name="modId">The unique identifier of the mod whose entity modifications are to be registered.</param>
        private static void RegisterEntityModifications_Internal(long modId)
        {
            IEnumerable<Type> types = API.Reflection.GetTypes(modId).Where(ModAPIExtensions.HasAttributeChecked<EntityModificationAttribute>);

            foreach (Type type in types)
            {
                RegisterEntityModificationsInType_Internal(type);
            }
        }
        
        /// <summary>
        /// Registers entity modification attributes and their associated actions within the specified type.
        /// </summary>
        /// <param name="type">The type to scan for entity modification attributes and the corresponding modifications to register.</param>
        private static void RegisterEntityModificationsInType_Internal(Type type)
        {
            int result = API.Experimental.RegisterAttributeFunction<EntityModificationAttribute, ModifyAction>(type, (action, attribute) =>
            {
                if (!string.IsNullOrEmpty(attribute.ModTarget))
                {
                    ModEntityModifyFunctions.AddDelegate(attribute.ModTarget, action);
                }
                else
                {
                    if (attribute.Target == ObjectID.None)
                    {
                        Log.LogWarning($"Entity modify method '{action.Method.FullDescription()}' does not have a target set!");
                        return false;
                    }

                    //TODO add delegate wrapping
                    EntityModifyFunctions.AddDelegate(attribute.Target, action);
                }

                return true;
            });
            Log.LogInfo($"Registered {result} entity modifiers in type {type.FullName}!");
        }

        /// <summary>
        /// Registers modifications to prefabs for the specified mod by processing
        /// all types in the mod that are annotated with <see cref="PrefabModificationAttribute" />.
        /// </summary>
        /// <param name="modId">The unique identifier of the mod whose prefab modifications are to be registered.</param>
        private static void RegisterPrefabModifications_Internal(long modId)
        {
            IEnumerable<Type> types = API.Reflection.GetTypes(modId).Where(ModAPIExtensions.HasAttributeChecked<PrefabModificationAttribute>);

            foreach (Type type in types)
            {
                RegisterPrefabModificationsInType_Internal(type);
            }
        }

        /// <summary>
        /// Registers modifications for prefabs in the specified type based on attributes.
        /// </summary>
        /// <param name="type">The type containing methods or classes marked with PrefabModificationAttribute to process.</param>
        private static void RegisterPrefabModificationsInType_Internal(Type type)
        {
            int result = API.Experimental.RegisterAttributeFunction<PrefabModificationAttribute, Action<MonoBehaviour>>(type, (action, attribute) =>
            {
                if (attribute.TargetType == null)
                {
                    Log.LogWarning($"Attribute on method '{action.Method.FullDescription()}' has no type info!");
                    return false;
                }

                PrefabModifyFunctions.Add(attribute.TargetType, action);
                return true;
            });
            Log.LogInfo($"Registered {result} prefab modifiers in type {type.FullName}!");
        }

        /// <summary>
        /// Combines modification delegates from mod-specific entities into the main entity modification functions dictionary.
        /// </summary>
        /// <remarks>
        /// This method processes and transfers delegates from the <c>modEntityModifyFunctions</c> dictionary to the
        /// <c>entityModifyFunctions</c> dictionary, mapping them to appropriate object IDs.
        /// Delegates from mods that cannot be resolved to a valid object ID are logged as warnings, and their addition is skipped.
        /// After transferring, the <c>modEntityModifyFunctions</c> dictionary is cleared.
        /// </remarks>
        private static void CombineModifyDelegates()
        {
            if (ModEntityModifyFunctions.Count == 0) return;

            foreach (var pair in ModEntityModifyFunctions)
            {
                ObjectID objectID = API.Authoring.GetObjectID(pair.Key);
                if (objectID == ObjectID.None)
                {
                    Log.LogWarning($"Failed to resolve mod entity target: {pair.Key}!");
                    continue;
                }

                EntityModifyFunctions.AddDelegate(objectID, pair.Value);
            }

            ModEntityModifyFunctions.Clear();
        }

        /// <summary>
        /// Handles the addition of a new object type represented by the specified entity and authoring GameObject,
        /// applying any relevant modification functions if available.
        /// </summary>
        /// <param name="entity">The entity associated with the new object type.</param>
        /// <param name="authoring">The authoring GameObject linked to the entity.</param>
        /// <param name="entityManager">The EntityManager instance responsible for managing entities.</param>
        private static void OnObjectTypeAdded(Unity.Entities.Entity entity, GameObject authoring, EntityManager entityManager)
        {
            CombineModifyDelegates();

            if (EntityModifyFunctions.Count == 0) return;

            ObjectID objectID = authoring.GetEntityObjectID();

            if (EntityModifyFunctions.ContainsKey(objectID))
            {
                try
                {
                    EntityModifyFunctions[objectID]?.Invoke(entity, authoring, entityManager);
                }
                catch (Exception e)
                {
                    Log.LogError($"Exception while executing mod modify function for {objectID}:\n{e}");
                }
                
            }
        }

        /// <summary>
        /// Applies modifications to prefabs based on the defined modification functions for specific MonoBehaviour types.
        /// </summary>
        /// <param name="memoryManager">The MemoryManager instance containing the poolable prefab banks to be processed.</param>
        internal static void ApplyPrefabModifications(MemoryManager memoryManager)
        {
            if (PrefabModifyFunctions.Count == 0) return;

            foreach (var prefabBank in memoryManager.poolablePrefabBanks)
            {
                if (prefabBank is not PooledGraphicalObjectBank) continue;
                
                foreach (var prefab in prefabBank)
                {
                    EntityMonoBehaviour prefabMono = prefab.prefab.GetComponent<EntityMonoBehaviour>();
                    if (prefabMono == null) continue;

                    Type type = prefabMono.GetType();
                    if (PrefabModifyFunctions.ContainsKey(type))
                    {
                        try
                        {
                            PrefabModifyFunctions[type]?.Invoke(prefabMono);
                        }
                        catch (Exception e)
                        {
                            Log.LogError($"Error while executing prefab modification for type {type.FullName}!\n{e}");
                        }
                    }
                }
            }

            Log.LogInfo("Finished Modifying Prefabs!");
        }

        #endregion

        /// <summary>
        /// Retrieves a list of skins of the specified type from the PlayerCustomizationTable.
        /// </summary>
        /// <typeparam name="T">The type of skin to retrieve. Must inherit from <see cref="SkinBase"/>.</typeparam>
        /// <returns>A list of skins of the specified type.</returns>
        /// <exception cref="ArgumentException">Thrown if the specified skin type is unsupported or unknown.</exception>
        private static object GetSkinList<T>()
            where T : SkinBase
        {
            return typeof(T).GetNameChecked() switch
            {
                "BodySkin" => CustomizationTable.bodySkins,
                "HairSkin" => CustomizationTable.hairSkins,
                "EyesSkin" => CustomizationTable.eyeSkins,
                "ShirtSkin" => CustomizationTable.shirtSkins,
                "PantsSkin" => CustomizationTable.pantsSkins,
                "HelmSkin" => CustomizationTable.helmSkins,
                "BreastArmorSkin" => CustomizationTable.breastArmorSkins,
                "PantsArmorSkin" => CustomizationTable.pantsArmorSkins,
                _ => throw new ArgumentException($"Unknows skin type: {typeof(T).GetNameChecked()}")
            };
        }

        /// <summary>
        /// Retrieves a sorted list of skins of the specified type from the customization table.
        /// </summary>
        /// <typeparam name="T">The type of skin to retrieve, derived from SkinBase.</typeparam>
        /// <returns>A sorted list of skins of the specified type.</returns>
        private static object GetSortedSkinList<T>()
            where T : SkinBase
        {
            return typeof(T).GetNameChecked() switch
            {
                "BodySkin" => CustomizationTable.bodySkinsSorted,
                "HairSkin" => CustomizationTable.hairSkinsSorted,
                "EyesSkin" => CustomizationTable.eyeSkinsSorted,
                "ShirtSkin" => CustomizationTable.shirtSkinsSorted,
                "PantsSkin" => CustomizationTable.pantsSkinsSorted,
                "HelmSkin" => CustomizationTable.helmSkinsSorted,
                "BreastArmorSkin" => CustomizationTable.breastArmorSkinsSorted,
                "PantsArmorSkin" => CustomizationTable.pantsArmorSkinsSorted,
                _ => throw new ArgumentException($"Unknows skin type: {typeof(T).GetNameChecked()}")
            };
        }
        
        #endregion
    }
}