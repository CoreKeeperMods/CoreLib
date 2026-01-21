// ========================================================
// Project: Core Library Mod (Core Keeper)
// File: EntityModule.cs
// Author: Minepatcher, Limoka
// Created: 2025-12-02
// Description: Provides functionalities for managing custom entities, prefabs, and modifications within the game.
// ========================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CoreLib.Submodule.Entity.Attribute;
using CoreLib.Submodule.Entity.Component;
using CoreLib.Submodule.Entity.Interface;
using CoreLib.Submodule.Entity.Patch;
using CoreLib.Submodule.Localization;
using CoreLib.Util.Extension;
using HarmonyLib;
using I2.Loc;
using PugMod;
using QFSW.QC.Utilities;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using Object = UnityEngine.Object;
using Logger = CoreLib.Util.Logger;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Entity
{
    /// Represents a module for managing custom entities, prefabs, and modifications within the framework.
    /// This class provides functionality for registering and managing entities, customization, and pooling,
    /// as well as applying modifications and managing dependencies.
    public class EntityModule : BaseSubmodule
    {
        #region Fields

        public const string NAME = "Core Library - Entity";

        /// <summary>Module-scoped logger instance for EntityModule.</summary>
        internal static Logger log = new(NAME);

        /// <summary>Convenience accessor for the loaded instance of this module.</summary>
        internal static EntityModule Instance => CoreLibMod.GetModuleInstance<EntityModule>();

        /// <summary>Module dependencies required before this module can initialize.</summary>
        internal override Type[] Dependencies => new[] { typeof(LocalizationModule) };

        /// <summary>List of prefabs that should be enabled for pooling.</summary>
        internal static List<PoolablePrefabBank.PoolablePrefab> poolablePrefabs = new();

        /// <summary>List of Modded Entities.</summary>
        internal static List<SupportsCoreLib> moddedEntities = new();

        /// <summary>List of Entity Modification Attributes.</summary>
        private static readonly List<EntityModifyAttribute> _entityModifyAttributes = new();

        /// <summary>List of Prefab Modification Attributes.</summary>
        private static readonly List<PrefabModifyAttribute> _prefabModifyAttributes = new();

        /// <summary>Definitions for workbenches added by mods.</summary>
        internal static List<WorkbenchDefinition> modWorkbenches = new();

        /// <summary>Chain of root workbench authoring for composite workbench behavior.</summary>
        internal static List<GameObject> rootWorkbenchesChain = new();

        /// <summary>The root workbench definition loaded by the module.</summary>
        internal static WorkbenchDefinition rootWorkbenchDefinition;

        /// <summary>Registered dynamic item handlers for runtime item logic.</summary>
        internal static List<IDynamicItemHandler> dynamicItemHandlers = new();

        /// <summary>Flag indicating whether entity injection has already been completed.</summary>
        internal static bool hasInjected;

        #endregion

        #region Custom Classes

        private class EntityModifyAttribute
        {
            public EntityModificationAttribute entityAttribute;
            public ModifyAction entityModifyAction;
        }

        private class PrefabModifyAttribute
        {
            public Type prefabType;
            public Action<MonoBehaviour> prefabModifyAction;
        }

        #endregion

        #region Delegates

        /// Delegate representing an entity modification routine that operates on:
        /// the ECS <see cref="Unity.Entities.Entity"/>, its associated <see cref="GameObject"/>,
        /// and the <see cref="EntityManager"/> controlling its ECS state.
        /// <param name="arg1">The ECS entity being modified.</param>
        /// <param name="arg2">The corresponding authoring <see cref="GameObject"/>.</param>
        /// <param name="arg3">The <see cref="EntityManager"/> managing the entity state.</param>
        public delegate void ModifyAction(Unity.Entities.Entity arg1, GameObject arg2, EntityManager arg3);

        #endregion

        #region BaseSubmodule Implementation

        /// Applies framework-level Harmony patches required for entity behavior,
        /// conversion, memory management, and player handling.
        internal override void SetHooks() => CoreLibMod.Patch(typeof(EntityModulePatches));

        /// Performs module load operations.  
        /// Currently, triggers a refresh of module asset bundles.
        internal override void Load()
        {
            base.Load();

            var entityModificationList = API.Reflection.AllTypes()
                .Where(type => type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(m => m.HasAttribute<EntityModificationAttribute>()).ToList().Count > 0).ToList();
            var prefabModificationList = API.Reflection.AllTypes()
                .Where(type => type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(m => m.HasAttribute<PrefabModificationAttribute>()).ToList().Count > 0).ToList();
            rootWorkbenchDefinition = Mod.Assets.OfType<WorkbenchDefinition>().FirstOrDefault(def => def.itemID == "CoreLib:RootModWorkbench");
            var supportsPoolingCoreLib = Mod.Assets.OfType<GameObject>().Where(go => go.TryGetComponent(out SupportsPooling _))
                .Select(go => go.GetComponent<SupportsPooling>().GetPoolablePrefab()).ToList();
            poolablePrefabs.AddRange(supportsPoolingCoreLib);

            foreach (var mod in DependentMods)
            {
                var moddedEntityList = mod.Assets.OfType<GameObject>().Where(go => go.TryGetComponent(out SupportsCoreLib _))
                    .Select(go => go.GetComponent<SupportsCoreLib>()).ToList();
                moddedEntities.AddRange(moddedEntityList);
                var supportsPoolingList = mod.Assets.OfType<GameObject>().Where(go => go.TryGetComponent(out SupportsPooling _))
                    .Select(go => go.GetComponent<SupportsPooling>().GetPoolablePrefab()).ToList();
                poolablePrefabs.AddRange(supportsPoolingList);
                var workbenchList = mod.Assets.OfType<WorkbenchDefinition>().ToList();
                modWorkbenches.AddRange(workbenchList);
                log.LogInfo(
                    $"Mod: {mod.Metadata.name} Found: {moddedEntityList.Count} Modded Entities, {supportsPoolingList.Count} Poolable Prefabs, {workbenchList.Count} Workbenches");
            }

            entityModificationList.ForEach(RegisterEntityModifications_Internal);
            prefabModificationList.ForEach(RegisterPrefabModifications_Internal);

            API.Authoring.OnObjectTypeAdded += OnObjectTypeAdded;
        }

        /// Executes final initialization steps after all dependencies have loaded.  
        /// Sets up object ID binding ranges, loads the root workbench, and registers
        /// built-in entity and prefab modifications.
        internal override void PostLoad()
        {
            InitializeWorkbenchDefinitions();
            InitializeModdedEntities();
        }

        #endregion

        #region Private Methods

        private static void InitializeWorkbenchDefinitions()
        {
            if (modWorkbenches.Count <= 0) return;
            foreach (var workbench in modWorkbenches)
            {
                AddWorkbenchDefinition(workbench);
            }
        }

        private static void InitializeModdedEntities()
        {
            if (moddedEntities.Count <= 0) return;
            moddedEntities = moddedEntities.OrderBy(obj => obj.ModID).ThenBy(obj => obj.EntityName).ToList();
            var sortedMods = moddedEntities.Where(ent => ent.bindToRootWorkbench).Select(ent => ent.ModID).Distinct().ToList();
            if (sortedMods.Count <= 0) return;
            AddRootWorkbenches(sortedMods);
            foreach (var entity in moddedEntities)
                API.Authoring.RegisterAuthoringGameObject(entity.gameObject);
        }

        /// Creates and registers the root workbench. This includes creating the first
        /// workbench entity, linking it to existing workbenches, and generating localization.
        /// <param name="sortedMods"></param>
        private static void AddRootWorkbenches(List<string> sortedMods)
        {
            GameObject currentWorkbench = null;
            foreach (string mod in sortedMods)
            {
                var modEntities = moddedEntities.Where(ent => mod == ent.ModID && ent.bindToRootWorkbench)
                    .Select(ent => ent.GetCraftingObject()).ToList();
                int modulusCount = 6 - modEntities.Count % 6;
                modEntities = modEntities.Concat(Enumerable.Range(0, modulusCount)
                        .Select(_ => new InventoryItemAuthoring.CraftingObject { objectName = "None", amount = 0 }))
                    .ToList();
                while (modEntities.Count > 0)
                {
                    if (rootWorkbenchesChain.Count == 0 ||
                        currentWorkbench?.GetComponent<ModCraftingAuthoring>().canCraftObjects.Count >= 18)
                    {
                        rootWorkbenchDefinition.itemID = IncrementID(rootWorkbenchDefinition.itemID);
                        log.LogInfo($"Create new Root Workbench for Mod: {mod} - {rootWorkbenchDefinition.itemID}");
                        currentWorkbench = AddWorkbenchDefinition(rootWorkbenchDefinition);
                        rootWorkbenchesChain.Add(currentWorkbench);
                        LocalizationModule.AddEntityLocalization(rootWorkbenchDefinition.itemID,
                            "Root Workbench", "This workbench contains all modded workbenches!");
                        if (rootWorkbenchesChain.Count > 1)
                        {
                            rootWorkbenchesChain.First().GetComponent<ModCraftingAuthoring>()
                                .includeCraftedObjectsFromBuildings.Add(rootWorkbenchDefinition.itemID);
                        }
                    }

                    if (currentWorkbench is null) continue;
                    if (!currentWorkbench.TryGetComponent(out ModCraftingAuthoring craftingAuthoring)) continue;
                    var craftingObjects = craftingAuthoring.canCraftObjects;
                    var skipCraftingObjects = modEntities.Take(18 - craftingObjects.Count).ToList();
                    craftingObjects.AddRange(skipCraftingObjects);
                    modEntities = modEntities.Skip(skipCraftingObjects.Count).ToList();
                }
            }

            foreach (var workbench in rootWorkbenchesChain)
            {
                var titleSettings = workbench.GetComponent<ModCraftingUISetting>();
                var canCraftObjects = workbench.GetComponent<ModCraftingAuthoring>().canCraftObjects;
                for (int i = 0; i < canCraftObjects.Count; i += 6)
                {
                    string modID = canCraftObjects[i].objectName.Split(":").FirstOrDefault();
                    Debug.Log($"Processing mod ID: {modID}");
                    LocalizedString strLoc = string.IsNullOrEmpty(modID) ? "CoreLib/Items" : $"Mods/{modID}";
                    Debug.Log($"Adding title for mod ID: {strLoc.mTerm}");
                    titleSettings.titles.Add(strLoc);
                }
            }
        }

        /// Creates a new workbench using the given definition, sets up icons, recipes,
        /// related building connections, asset skins, and ensures the prefab is pooled.
        /// <param name="workbenchDefinition">The definition describing the new workbench.</param>
        private static GameObject AddWorkbenchDefinition(WorkbenchDefinition workbenchDefinition)
        {
            var typeName = workbenchDefinition.workbenchType == WorkbenchType.Wide ? "WideWorkbench" : "Workbench";
            
            var newEntityPrefab = LoadPrefab(workbenchDefinition.itemID, $"Template{typeName}Entity");
            if (newEntityPrefab is null) return null;
            
            if (newEntityPrefab.TryGetComponent(out TemplateObject templateObject))
            {
                templateObject.Convert();
                Object.DestroyImmediate(templateObject);
            }

            if (newEntityPrefab.TryGetComponent(out ObjectAuthoring authoring))
                authoring.objectName = workbenchDefinition.itemID;

            if (newEntityPrefab.TryGetComponent(out InventoryItemAuthoring itemAuthoring))
            {
                itemAuthoring.icon = workbenchDefinition.bigIcon;
                itemAuthoring.smallIcon = workbenchDefinition.smallIcon;
                itemAuthoring.requiredObjectsToCraft = workbenchDefinition.recipe;
            }

            if (newEntityPrefab.TryGetComponent(out GhostAuthoringComponent ghost))
                ghost.SetValue("prefabId", workbenchDefinition.itemID.GetGuid());

            var modCraftingAuthoring = newEntityPrefab.AddComponent<ModCraftingAuthoring>();
            modCraftingAuthoring.craftingType = CraftingType.Simple;
            modCraftingAuthoring.canCraftObjects.AddRange(workbenchDefinition.canCraft);
            modCraftingAuthoring.includeCraftedObjectsFromBuildings.AddRange(workbenchDefinition.relatedWorkbenches);

            var modRefreshCraftingBuildingTitles = newEntityPrefab.AddComponent<ModRefreshCraftingBuildingTitles>();
            modRefreshCraftingBuildingTitles.refreshBuildingTitles = workbenchDefinition.refreshRelatedWorkbenchTitles;

            var modCraftingUISetting = newEntityPrefab.AddComponent<ModCraftingUISetting>();
            workbenchDefinition.SetupNewTitles();
            modCraftingUISetting.titles = workbenchDefinition.titles;
            modCraftingUISetting.craftingUIBackgroundVariation = workbenchDefinition.skin;

            var modReskinCondition = newEntityPrefab.AddComponent<ModReskinCondition>();
            modReskinCondition.season = Season.None;
            modReskinCondition.reskin = new List<SpriteSkinFromEntityAndSeason.SkinAndGradientMap>
            {
                new() { skinRef = workbenchDefinition.assetRef },
                new() { skinRef = workbenchDefinition.assetRef }
            };

            var supportsCoreLib = newEntityPrefab.AddComponent<SupportsCoreLib>();
            supportsCoreLib.bindToRootWorkbench = workbenchDefinition.bindToRootWorkbench;

            if (!moddedEntities.Contains(supportsCoreLib))
                moddedEntities.Add(supportsCoreLib);
            log.LogInfo($"Created new Workbench: {workbenchDefinition.itemID}");
            return newEntityPrefab;
        }

        /// Generates a new item ID by applying or updating a numerical suffix in the "$$" format.
        /// Prevents ID collisions when adding multiple autogenerated workbenches.
        /// <param name="prevId">The original ID.</param>
        /// <returns>The incremented ID.</returns>
        private static string IncrementID(string prevId)
        {
            string[] idParts = prevId.Split(new[] { "$$" }, StringSplitOptions.None);
            int currentIndex = 0;

            if (idParts.Length >= 2 && int.TryParse(idParts[1], out int result))
                currentIndex = ++result;

            return $"{idParts[0]}$${currentIndex}";
        }

        /// Loads a prefab from the asset system and prepares it for CoreLib entity authoring.
        /// <param name="itemId">The logical identifier of the new entity.</param>
        /// <param name="prefabName">Resource path to the prefab inside the asset bundle.</param>
        /// <returns>An initialized <see cref="ObjectAuthoring"/> component.</returns>
        private static GameObject LoadPrefab(string itemId, string prefabName)
        {
            var prefab = Mod.Assets.OfType<GameObject>().ToList().Find(x => x.name == prefabName);
            return CopyPrefab(itemId, prefab);
        }

        /// Clones an existing prefab, configures authoring components, assigns proper names,
        /// initializes ghost components, and returns the resulting <see cref="ObjectAuthoring"/>.
        /// <param name="itemId">The item ID associated with the entity.</param>
        /// <param name="prefab">The prefab GameObject to copy and initialize.</param>
        /// <returns>An authoring component for the cloned prefab.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the cloned prefab does not contain a valid <see cref="ObjectAuthoring"/>.
        /// </exception>
        private static GameObject CopyPrefab(string itemId, GameObject prefab)
        {
            string prefabName;
            if (prefab.TryGetComponent(out EntityMonoBehaviourData _))
            {
                throw new InvalidOperationException(
                    $"[{NAME}] Error loading prefab for '{itemId}' - Core Lib does not support using EntityMonoBehaviourData!"
                );
            }

            if (prefab.TryGetComponent(out TemplateObject _) || prefab.TryGetComponent(out ObjectAuthoring _))
            {
                prefabName = $"{itemId}_Entity";
            }
            else
            {
                prefabName = $"{itemId}";
            }

            var newPrefab = Object.Instantiate(prefab);
            newPrefab.name = prefabName;
            newPrefab.hideFlags = HideFlags.HideAndDontSave;

            return newPrefab;
        }

        #endregion

        #region Modification Methods

        private static void RegisterEntityModifications_Internal(Type type)
        {
            int result = API.Experimental.RegisterAttributeFunction<EntityModificationAttribute, ModifyAction>(type, (action, attribute) =>
            {
                if (attribute.target == ObjectID.None && string.IsNullOrEmpty(attribute.modTarget))
                {
                    log.LogWarning($"Entity modify method '{action.Method.FullDescription()}' does not have a target set!");
                    return false;
                }

                _entityModifyAttributes.Add(new EntityModifyAttribute { entityAttribute = attribute, entityModifyAction = action });
                return true;
            });
            log.LogInfo($"Registered {result} entity modifiers in type {type.FullName}!");
        }

        private static void RegisterPrefabModifications_Internal(Type type)
        {
            int result = API.Experimental.RegisterAttributeFunction<PrefabModificationAttribute, Action<MonoBehaviour>>(type, (action, attribute) =>
            {
                if (attribute.targetType == null)
                {
                    log.LogWarning($"Attribute on method '{action.Method.FullDescription()}' has no type info!");
                    return false;
                }

                _prefabModifyAttributes.Add(new PrefabModifyAttribute { prefabType = attribute.targetType, prefabModifyAction = action });
                return true;
            });

            log.LogInfo($"Registered {result} prefab modifiers in type {type.FullName}!");
        }

        /// Combines mod-scoped entity modification delegates into the main
        /// <see cref="_entityModifyAttributes"/> collection.
        /// Converts string-based mod targets into <see cref="ObjectID"/> values.
        private static void CombineModifyDelegates()
        {
            foreach (var pair in _entityModifyAttributes.Where(pair => pair.entityAttribute.target == ObjectID.None))
            {
                pair.entityAttribute.target = API.Authoring.GetObjectID(pair.entityAttribute.modTarget);
                if (pair.entityAttribute.target != ObjectID.None) continue;
                log.LogWarning($"Failed to resolve mod entity target: {pair.entityAttribute}!");
            }
        }

        /// Called when the game registers a new object type.  
        /// Applies all accumulated entity modification delegates (both mod-scoped and global).
        /// <param name="entity">The ECS entity representing the object instance.</param>
        /// <param name="authoring">The associated authoring <see cref="GameObject"/>.</param>
        /// <param name="entityManager">The ECS <see cref="EntityManager"/> that manages the entity.</param>
        private static void OnObjectTypeAdded(Unity.Entities.Entity entity, GameObject authoring, EntityManager entityManager)
        {
            if (_entityModifyAttributes.Count <= 0) return;
            CombineModifyDelegates();
            var id = authoring.GetEntityObjectID();
            if (id == ObjectID.None) return;
            try
            {
                _entityModifyAttributes.Where(pair => pair.entityAttribute.target == id).ToList()
                    .ForEach(action => action.entityModifyAction.Invoke(entity, authoring, entityManager));
            }
            catch (Exception e)
            {
                log.LogError($"Exception while executing mod modify function for {id}:\n{e}");
            }
        }

        /// Applies modifications to the player’s entity when a workbench-related modification
        /// is triggered. Ensures that the last root workbench becomes craftable.
        /// <param name="entity">The player ECS entity.</param>
        /// <param name="authoring">The authoring GameObject associated with the player.</param>
        /// <param name="entityManager">The manager controlling ECS state.</param>
        [EntityModification(ObjectID.Player)]
        private static void EditPlayer(Unity.Entities.Entity entity, GameObject authoring, EntityManager entityManager)
        {
            if (rootWorkbenchesChain.Count <= 0) return;
            var rootWorkbenchID = rootWorkbenchesChain.First().GetEntityObjectID();
            var crafting = authoring.GetComponent<CraftingAuthoring>().canCraftObjects;
            if (crafting.FindIndex(x => x.objectID == rootWorkbenchID) == -1)
                crafting.Add(new CraftingAuthoring.CraftableObject { objectID = rootWorkbenchID, amount = 1 });
        }

        /// Applies prefab modifications registered via <see cref="_prefabModifyAttributes"/>.
        /// Iterates all graphical object banks and invokes modification functions for matching types.
        /// <param name="prefabBank">The memory manager containing pooled prefabs.</param>
        internal static void ApplyPrefabModifications(PooledGraphicalObjectBank prefabBank)
        {
            if (_prefabModifyAttributes.Count <= 0) return;

            foreach (var prefab in prefabBank)
            {
                if (!prefab.prefab.TryGetComponent(out EntityMonoBehaviourData mono)) continue;
                var type = mono.GetType();
                var prefabTypes = _prefabModifyAttributes.Where(pair => pair.prefabType == type).ToList();
                if (prefabTypes.Count > 0) continue;
                try
                {
                    prefabTypes.ForEach(action => action.prefabModifyAction.Invoke(mono));
                }
                catch (Exception e)
                {
                    log.LogError($"Error while executing prefab modification for type {type.FullName}!\n{e}");
                }
            }

            log.LogInfo("Finished Modifying Prefabs!");
        }

        #endregion

        #region Public Methods

        /// Registers a new dynamic item handler of the specified type if it is not yet registered.
        /// <typeparam name="T">The type of the dynamic item handler to register. Must implement <see cref="IDynamicItemHandler"/> and have a parameterless constructor.</typeparam>
        public static void RegisterDynamicItemHandler<T>() where T : IDynamicItemHandler, new()
        {
            var handler = Activator.CreateInstance<T>();
            if (dynamicItemHandlers.Contains(handler))
                log.LogWarning($"Failed to register dynamic handler {typeof(T).FullName}, because it is already registered!");
            else
                dynamicItemHandlers.Add(handler);
        }

        #endregion
    }
}