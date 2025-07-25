﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using CoreLib.Data;
using CoreLib.Localization;
using CoreLib.Submodules.ModEntity.Atributes;
using CoreLib.Submodules.ModEntity.Components;
using CoreLib.Submodules.ModEntity.Interfaces;
using CoreLib.Submodules.ModEntity.Patches;
using CoreLib.ModResources;
using CoreLib.Util;
using CoreLib.Util.Extensions;
using HarmonyLib;
using Pug.Sprite;
using PugMod;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using Object = UnityEngine.Object;

[assembly:InternalsVisibleTo("CoreLib.Audio")]
[assembly:InternalsVisibleTo("CoreLib.Commands")]
[assembly:InternalsVisibleTo("CoreLib.Drops")]
[assembly:InternalsVisibleTo("CoreLib.Editor")]
[assembly:InternalsVisibleTo("CoreLib.Equipment")]
[assembly:InternalsVisibleTo("CoreLib.JsonLoader")]
[assembly:InternalsVisibleTo("CoreLib.Localization")]
[assembly:InternalsVisibleTo("CoreLib.ModderTools")]
[assembly:InternalsVisibleTo("CoreLib.RewiredExtension")]
[assembly:InternalsVisibleTo("CoreLib.Tilesets")]

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodules.ModEntity
{
    public class EntityModule : BaseSubmodule
    {
        #region PublicInterface
        
        public static event Action MaterialSwapReady;

        public static void AddToAuthoringList(GameObject gameObject)
        {
            modAuthoringTargets.Add(gameObject);
        }

        public static void EnablePooling(GameObject gameObject)
        {
            poolablePrefabs.Add(gameObject);
        }

        /// <summary>
        /// Register you entity modifications methods.
        /// </summary>
        /// <param name="modId">Mod to analyze</param>
        public static void RegisterEntityModifications(long modId)
        {
            Instance.ThrowIfNotLoaded();
            ThrowIfTooLate(nameof(RegisterEntityModifications));

            RegisterEntityModifications_Internal(modId);
        }

        public static void RegisterPrefabModifications(long modId)
        {
            Instance.ThrowIfNotLoaded();
            ThrowIfTooLate(nameof(RegisterPrefabModifications));

            RegisterPrefabModifications_Internal(modId);
        }

        /// <summary>
        /// Register you entity modifications methods.
        /// </summary>
        /// <param name="type">Type to analyze</param>
        public static void RegisterEntityModifications(Type type)
        {
            Instance.ThrowIfNotLoaded();
            ThrowIfTooLate(nameof(RegisterEntityModifications));

            RegisterEntityModificationsInType_Internal(type);
        }

        public static void RegisterPrefabModifications(Type type)
        {
            Instance.ThrowIfNotLoaded();
            ThrowIfTooLate(nameof(RegisterPrefabModifications));

            RegisterPrefabModificationsInType_Internal(type);
        }

        public static ObjectType GetObjectType(string typeName)
        {
            Instance.ThrowIfNotLoaded();

            int index = objectTypeIDs.HasIndex(typeName) ? objectTypeIDs.GetIndex(typeName) : objectTypeIDs.GetNextId(typeName);
            return (ObjectType)index;
        }

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
        /// Add new entity.
        /// </summary>
        /// <param name="itemId">UNIQUE entity id</param>
        /// <param name="prefabPath">path to your prefab in asset bundle</param>
        /// <returns>Added objectID. If adding failed returns <see cref="ObjectID.None"/></returns>
        /// <exception cref="InvalidOperationException">Throws if called too late</exception>
        public static void AddEntity(string itemId, string prefabPath)
        {
             AddEntityWithVariations(itemId, new[] { prefabPath });
        }

        /// <summary>
        /// Add new entity.
        /// </summary>
        /// <param name="itemId">UNIQUE entity id</param>
        /// <param name="prefabPath">path to your prefab in asset bundle</param>
        /// <returns>Added objectID. If adding failed returns <see cref="ObjectID.None"/></returns>
        /// <exception cref="InvalidOperationException">Throws if called too late</exception>
        public static void AddEntity(string itemId, ObjectAuthoring prefab)
        { 
            AddEntityWithVariations(itemId, new List<ObjectAuthoring> { prefab });
        }

        /// <summary>
        /// Add new entity with variations. Each prefab must have variation field set.
        /// </summary>
        /// <param name="itemId">UNIQUE entity id</param>
        /// <param name="prefabsPaths">paths to your prefabs in asset bundle</param>
        /// <returns>Added objectID. If adding failed returns <see cref="ObjectID.None"/></returns>
        /// <exception cref="InvalidOperationException">Throws if called too late</exception>
        public static void AddEntityWithVariations(string itemId, string[] prefabsPaths)
        {
            Instance.ThrowIfNotLoaded();
            ThrowIfTooLate(nameof(AddEntityWithVariations));

            if (prefabsPaths.Length == 0)
            {
                CoreLibMod.Log.LogError($"Failed to add entity {itemId}: prefabsPaths has no paths!");
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
                    CoreLibMod.Log.LogError($"Failed to add entity {itemId}, prefab {prefabPath} is missing!");
                    return;
                }
            }
            
            AddEntityWithVariations(itemId, entities);
        }

        public static void AddEntityWithVariations(string itemId, List<ObjectAuthoring> prefabs)
        {
            foreach (ObjectAuthoring prefab in prefabs)
            {
                prefab.objectName = itemId;
                API.Authoring.RegisterAuthoringGameObject(prefab.gameObject);
                AddToAuthoringList(prefab.gameObject);
            }

            moddedEntities.Add(itemId, prefabs);
        }

        /// <summary>
        /// Add custom customization texture sheet
        /// </summary>
        /// <param name="skin">Class with texture sheet information</param>
        /// <returns>New skin index. '0' if failed.</returns>
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

        internal delegate void ModifyAction(Entity arg1, GameObject arg2, EntityManager arg3);

        internal override GameVersion Build => new GameVersion(1, 1, 0, "ab78");
        internal override string Version => "4.0.9";    
        internal static EntityModule Instance => CoreLibMod.GetModuleInstance<EntityModule>();
        
        internal override Type[] Dependencies => new[] { typeof(LocalizationModule), typeof(ResourcesModule) };

        internal static List<GameObject> modAuthoringTargets = new List<GameObject>();
        internal static List<GameObject> poolablePrefabs = new List<GameObject>();

        internal static Dictionary<string, List<ObjectAuthoring>> moddedEntities = new Dictionary<string, List<ObjectAuthoring>>();
        
        internal static Dictionary<ObjectID, ModifyAction> entityModifyFunctions = new Dictionary<ObjectID, ModifyAction>();
        internal static Dictionary<string, ModifyAction> modEntityModifyFunctions = new Dictionary<string, ModifyAction>();
        internal static Dictionary<Type, Action<MonoBehaviour>> prefabModifyFunctions = new Dictionary<Type, Action<MonoBehaviour>>();

        internal static List<WorkbenchDefinition> modWorkbenches = new List<WorkbenchDefinition>();
        internal static List<ObjectAuthoring> rootWorkbenchesChain = new List<ObjectAuthoring>();
        internal static WorkbenchDefinition rootWorkbenchDefinition;

        internal static List<IDynamicItemHandler> dynamicItemHandlers = new List<IDynamicItemHandler>();

        internal static IdBind objectTypeIDs;

        internal static HashSet<int> busyIDsSet = new HashSet<int>();

        internal static PlayerCustomizationTable customizationTable;
        
        internal const int modObjectTypeIdRangeStart = 33000;
        internal const int modObjectTypeIdRangeEnd = ushort.MaxValue;

        internal static bool hasInjected;

        #region Initialization

        internal override void SetHooks()
        {
            CoreLibMod.Patch(typeof(MemoryManager_Patch));
            CoreLibMod.Patch(typeof(PlayerController_Patch));
            CoreLibMod.Patch(typeof(ColorReplacer_Patch));
            CoreLibMod.Patch(typeof(GraphicalObjectConversion_Patch));
        }

        internal override void Load()
        {
            ResourcesModule.RefreshModuleBundles();
        }

        internal override void PostLoad()
        {
            objectTypeIDs = new IdBind(modObjectTypeIdRangeStart, modObjectTypeIdRangeEnd);
            rootWorkbenchDefinition = ResourcesModule.LoadAsset<WorkbenchDefinition>("Assets/CoreLibPackage/CoreLib.Entity/RootWorkbench");

            RegisterEntityModifications(typeof(EntityModule));
            RegisterPrefabModifications(typeof(EntityModule));

            API.Authoring.OnObjectTypeAdded += OnObjectTypeAdded;
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
        private static void EditPlayer(Entity entity, GameObject authoring, EntityManager entityManager)
        {
            if (rootWorkbenchesChain.Count > 0)
            {
                var lastRootWorkbenchId = API.Authoring.GetObjectID(rootWorkbenchesChain.Last().objectName);

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
        }

        private static void AddRootWorkbench()
        {
            rootWorkbenchDefinition.itemId = IncrementID(rootWorkbenchDefinition.itemId);

            AddWorkbench(rootWorkbenchDefinition);
            if (GetMainEntity(rootWorkbenchDefinition.itemId, out ObjectAuthoring entity))
            {
                if (rootWorkbenchesChain.Count > 0)
                {
                    var oldWorkbench = rootWorkbenchesChain.Last();
                    ModCraftingAuthoring crafting = oldWorkbench.gameObject.GetComponent<ModCraftingAuthoring>();
                    crafting.includeCraftedObjectsFromBuildings.Add(rootWorkbenchDefinition.itemId);
                }

                rootWorkbenchesChain.Add(entity);
            }

            LocalizationModule.AddEntityLocalization(rootWorkbenchDefinition.itemId, $"Root Workbench {rootWorkbenchesChain.Count}",
                "This workbench contains all modded workbenches!");
        }

        private static void AddRootWorkbenchItem(string entityId)
        {
            Instance.ThrowIfNotLoaded();
            ThrowIfTooLate(nameof(AddModWorkbench));
            
            if (rootWorkbenchesChain.Count == 0) 
                AddRootWorkbench();
            
            while (true)
            {
                ObjectAuthoring workbenchEntity = rootWorkbenchesChain.Last();
                ModCraftingAuthoring craftingCdAuthoring = workbenchEntity.gameObject.GetComponent<ModCraftingAuthoring>();

                CoreLibMod.Log.LogDebug($"Adding item {entityId} to root workbench");

                if (craftingCdAuthoring.canCraftObjects.Count < 18)
                {
                    craftingCdAuthoring.canCraftObjects.Add(new InventoryItemAuthoring.CraftingObject { objectName = entityId, amount = 1 });
                    return;
                }

                AddRootWorkbench();
            }
        }

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
                
                SpriteAsset targetSkin = rootWorkbenchDefinition.assetSkin.targetAsset;
                if (workbenchDefinition.assetSkin.targetAsset != targetSkin)
                {
                    workbenchDefinition.assetSkin.SetValue("m_targetAsset", targetSkin);
                    CoreLibMod.Log.LogInfo($"Changed {workbenchDefinition.itemId} target asset to root workbench asset");
                }
                modWorkbenches.Add(workbenchDefinition);
                
                if (!poolablePrefabs.Contains(entity.graphicalPrefab))
                    EnablePooling(entity.graphicalPrefab);
            }
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

        internal static void ApplyAllModAuthorings()
        {
            MaterialSwapReady?.Invoke();
            foreach (GameObject gameObject in modAuthoringTargets)
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

        internal static bool GetMainEntity(string objectID, out ObjectAuthoring entity)
        {
            if (moddedEntities.ContainsKey(objectID))
            {
                entity = moddedEntities[objectID][0];
                return true;
            }

            entity = null;
            return false;
        }

        internal static bool GetEntity(string objectID, int variation, out ObjectAuthoring entity)
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

        private static void RegisterEntityModifications_Internal(long modId)
        {
            IEnumerable<Type> types = API.Reflection.GetTypes(modId).Where(ModAPIExtensions.HasAttributeChecked<EntityModificationAttribute>);

            foreach (Type type in types)
            {
                RegisterEntityModificationsInType_Internal(type);
            }
        }

        
        
        private static void RegisterEntityModificationsInType_Internal(Type type)
        {
            int result = API.Experimental.RegisterAttributeFunction<EntityModificationAttribute, ModifyAction>(type, (action, attribute) =>
            {
                if (!string.IsNullOrEmpty(attribute.modTarget))
                {
                    modEntityModifyFunctions.AddDelegate(attribute.modTarget, action);
                }
                else
                {
                    if (attribute.target == ObjectID.None)
                    {
                        CoreLibMod.Log.LogWarning($"Entity modify method '{action.Method.FullDescription()}' does not have a target set!");
                        return false;
                    }

                    //TODO add delegate wrapping
                    entityModifyFunctions.AddDelegate(attribute.target, action);
                }

                return true;
            });
            CoreLibMod.Log.LogInfo($"Registered {result} entity modifiers in type {type.FullName}!");
        }

        private static void RegisterPrefabModifications_Internal(long modId)
        {
            IEnumerable<Type> types = API.Reflection.GetTypes(modId).Where(ModAPIExtensions.HasAttributeChecked<PrefabModificationAttribute>);

            foreach (Type type in types)
            {
                RegisterPrefabModificationsInType_Internal(type);
            }
        }

        private static void RegisterPrefabModificationsInType_Internal(Type type)
        {
            int result = API.Experimental.RegisterAttributeFunction<PrefabModificationAttribute, Action<MonoBehaviour>>(type, (action, attribute) =>
            {
                if (attribute.targetType == null)
                {
                    CoreLibMod.Log.LogWarning($"Attribute on method '{action.Method.FullDescription()}' has no type info!");
                    return false;
                }

                prefabModifyFunctions.Add(attribute.targetType, action);
                return true;
            });
            CoreLibMod.Log.LogInfo($"Registered {result} prefab modifiers in type {type.FullName}!");
        }

        private static void CombineModifyDelegates()
        {
            if (modEntityModifyFunctions.Count == 0) return;

            foreach (var pair in modEntityModifyFunctions)
            {
                ObjectID objectID = API.Authoring.GetObjectID(pair.Key);
                if (objectID == ObjectID.None)
                {
                    CoreLibMod.Log.LogWarning($"Failed to resolve mod entity target: {pair.Key}!");
                    continue;
                }

                entityModifyFunctions.AddDelegate(objectID, pair.Value);
            }

            modEntityModifyFunctions.Clear();
        }

        private static void OnObjectTypeAdded(Entity entity, GameObject authoring, EntityManager entityManager)
        {
            CombineModifyDelegates();

            if (entityModifyFunctions.Count == 0) return;

            ObjectID objectID = authoring.GetEntityObjectID();

            if (entityModifyFunctions.ContainsKey(objectID))
            {
                try
                {
                    entityModifyFunctions[objectID]?.Invoke(entity, authoring, entityManager);
                }
                catch (Exception e)
                {
                    CoreLibMod.Log.LogError($"Exception while executing mod modify function for {objectID}:\n{e}");
                }
                
            }
        }

        internal static void ApplyPrefabModifications(MemoryManager memoryManager)
        {
            if (prefabModifyFunctions.Count == 0) return;

            foreach (var prefabBank in memoryManager.poolablePrefabBanks)
            {
                if (prefabBank is not PooledGraphicalObjectBank) continue;
                
                foreach (var prefab in prefabBank)
                {
                    EntityMonoBehaviour prefabMono = prefab.prefab.GetComponent<EntityMonoBehaviour>();
                    if (prefabMono == null) continue;

                    Type type = prefabMono.GetType();
                    if (prefabModifyFunctions.ContainsKey(type))
                    {
                        try
                        {
                            prefabModifyFunctions[type]?.Invoke(prefabMono);
                        }
                        catch (Exception e)
                        {
                            CoreLibMod.Log.LogError($"Error while executing prefab modification for type {type.FullName}!\n{e}");
                        }
                    }
                }
            }

            CoreLibMod.Log.LogInfo("Finished Modifying Prefabs!");
        }

        #endregion

         private static object GetSkinList<T>()
            where T : SkinBase
        {
            switch (typeof(T).GetNameChecked())
            {
                case "BodySkin":
                    return customizationTable.bodySkins;
                case "HairSkin":
                    return customizationTable.hairSkins;
                case "EyesSkin":
                    return customizationTable.eyeSkins;
                case "ShirtSkin":
                    return customizationTable.shirtSkins;
                case "PantsSkin":
                    return customizationTable.pantsSkins;
                case "HelmSkin":
                    return customizationTable.helmSkins;
                case "BreastArmorSkin":
                    return customizationTable.breastArmorSkins;
                case "PantsArmorSkin":
                    return customizationTable.pantsArmorSkins;
            }
            
            throw new ArgumentException($"Unknows skin type: {typeof(T).GetNameChecked()}");
        }
        
        private static object GetSortedSkinList<T>()
            where T : SkinBase
        {
            switch (typeof(T).GetNameChecked())
            {
                case "BodySkin":
                    return customizationTable.bodySkinsSorted;
                case "HairSkin":
                    return customizationTable.hairSkinsSorted;
                case "EyesSkin":
                    return customizationTable.eyeSkinsSorted;
                case "ShirtSkin":
                    return customizationTable.shirtSkinsSorted;
                case "PantsSkin":
                    return customizationTable.pantsSkinsSorted;
                case "HelmSkin":
                    return customizationTable.helmSkinsSorted;
                case "BreastArmorSkin":
                    return customizationTable.breastArmorSkinsSorted;
                case "PantsArmorSkin":
                    return customizationTable.pantsArmorSkinsSorted;
            }
            
            throw new ArgumentException($"Unknows skin type: {typeof(T).GetNameChecked()}");
        }
        
        #endregion
    }
}