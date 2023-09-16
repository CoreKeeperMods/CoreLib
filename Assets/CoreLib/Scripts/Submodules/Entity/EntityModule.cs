using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using QFSW.QC;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CoreLib.Submodules.ModEntity
{
    //TODO test the EntityModule
    [CommandPrefix("mod.")]
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
        /// <param name="assembly">Assembly to analyze</param>
        public static void RegisterEntityModifications(Assembly assembly)
        {
            Instance.ThrowIfNotLoaded();
            ThrowIfTooLate(nameof(RegisterEntityModifications));

            RegisterEntityModifications_Internal(assembly);
        }

        public static void RegisterPrefabModifications(Assembly assembly)
        {
            Instance.ThrowIfNotLoaded();
            ThrowIfTooLate(nameof(RegisterPrefabModifications));

            RegisterPrefabModifications_Internal(assembly);
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

        /// <summary>
        /// Get objectID from UNIQUE entity id
        /// </summary>
        /// <param name="itemID">UNIQUE string entity ID</param>
        public static ObjectID GetObjectId(string itemID)
        {
            Instance.ThrowIfNotLoaded();

            return (ObjectID)modEntityIDs.GetIndex(itemID);
        }

        public static string GetObjectStringId(ObjectID itemID)
        {
            Instance.ThrowIfNotLoaded();

            return modEntityIDs.GetStringID((int)itemID);
        }

        public static ObjectType GetObjectType(string typeName)
        {
            Instance.ThrowIfNotLoaded();

            int index = objectTypeIDs.HasIndex(typeName) ? objectTypeIDs.GetIndex(typeName) : objectTypeIDs.GetNextId(typeName);
            return (ObjectType)index;
        }

        public static string[] GetAllModdedItems()
        {
            Instance.ThrowIfNotLoaded();
            return modEntityIDs.ModIDs.Keys.ToArray();
        }

        public static ObjectID AddModWorkbench(WorkbenchDefinition workbenchDefinition)
        {
            Instance.ThrowIfNotLoaded();
            ThrowIfTooLate(nameof(AddModWorkbench));
            ObjectID workbenchId = AddWorkbench(workbenchDefinition);
            if (workbenchDefinition.bindToRootWorkbench)
            {
                AddRootWorkbenchItem(workbenchId);
            }

            return workbenchId;
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
            return AddEntityWithVariations(itemId, new List<ObjectAuthoring> { prefab });
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
            Instance.ThrowIfNotLoaded();
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
                AddToAuthoringList(prefab.gameObject);
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
            Instance.ThrowIfNotLoaded();
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

        internal delegate void ModifyAction(Entity arg1, GameObject arg2, EntityManager arg3);

        internal override GameVersion Build => new GameVersion(0, 0, 0, 0, "");
        internal static EntityModule Instance => CoreLibMod.GetModuleInstance<EntityModule>();

        internal static List<GameObject> modAuthoringTargets = new List<GameObject>();
        internal static List<GameObject> poolablePrefabs = new List<GameObject>();

        internal static Dictionary<ObjectID, List<ObjectAuthoring>> moddedEntities = new Dictionary<ObjectID, List<ObjectAuthoring>>();
        internal static Dictionary<ObjectID, ModifyAction> entityModifyFunctions = new Dictionary<ObjectID, ModifyAction>();
        internal static Dictionary<string, ModifyAction> modEntityModifyFunctions = new Dictionary<string, ModifyAction>();
        internal static Dictionary<Type, Action<MonoBehaviour>> prefabModifyFunctions = new Dictionary<Type, Action<MonoBehaviour>>();

        internal static Dictionary<ObjectID, WorkbenchDefinition> modWorkbenches = new Dictionary<ObjectID, WorkbenchDefinition>();
        internal static List<ObjectAuthoring> rootWorkbenchesChain = new List<ObjectAuthoring>();
        internal static WorkbenchDefinition rootWorkbenchDefinition;

        internal static List<IDynamicItemHandler> dynamicItemHandlers = new List<IDynamicItemHandler>();

        internal static IdBindConfigFile modEntityIDs;
        internal static IdBind objectTypeIDs;

        internal static HashSet<int> busyIDsSet = new HashSet<int>();

        internal static PlayerCustomizationTable customizationTable;

        internal const int modEntityIdRangeStart = 33000;
        internal const int modEntityIdRangeEnd = ushort.MaxValue;


        internal const int modObjectTypeIdRangeStart = 33000;
        internal const int modObjectTypeIdRangeEnd = ushort.MaxValue;

        internal static bool hasInjected;

        #region Initialization

        internal override void SetHooks()
        {
            CoreLibMod.harmony.PatchAll(typeof(MemoryManager_Patch));
            CoreLibMod.harmony.PatchAll(typeof(PlayerController_Patch));
            CoreLibMod.harmony.PatchAll(typeof(ColorReplacer_Patch));
        }

        internal override void PostLoad()
        {
            modEntityIDs = new IdBindConfigFile("CoreLib", "CoreLib.ModEntityID", modEntityIdRangeStart, modEntityIdRangeEnd);
            objectTypeIDs = new IdBind(modObjectTypeIdRangeStart, modObjectTypeIdRangeEnd);
            rootWorkbenchDefinition = ResourcesModule.LoadAsset<WorkbenchDefinition>("Assets/CoreLib/Resources/RootWorkbench");

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

        [Command("spawn", "Spawns any object by integer ID")]
        public void Spawn(string itemId, int amount)
        {
            PlayerController player = Manager.main.player;
            if (player == null)
            {
                return;
            }

            var results = modEntityIDs.ModIDs.Keys.Where(id => id.Contains(itemId, StringComparison.InvariantCultureIgnoreCase)).ToList();
            if (results.Count > 1)
            {
                CoreLibMod.Log.LogWarning($"Ambiguous id! Found {results.Count} matches!");
                return;
            }

            var objectID = GetObjectId(results.First());
            if (objectID == ObjectID.None)
            {
                CoreLibMod.Log.LogWarning($"Unknown item: {itemId}");
                return;
            }

            ObjectInfo objectInfo = PugDatabase.GetObjectInfo(objectID, 0);

            if (objectInfo.objectType != ObjectType.Creature && objectInfo.objectType != ObjectType.NonObtainable)
            {
                Vector3 b = new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), 0f, UnityEngine.Random.Range(-0.5f, 0.5f));
                player.playerCommandSystem.CreateAndDropEntity(objectID, player.transform.position + b, amount, player.entity, 0);
            }
        }

        #endregion

        #region Workbenches

        [EntityModification(ObjectID.Player)]
        private static void EditPlayer(Entity entity, GameObject authoring, EntityManager entityManager)
        {
            if (rootWorkbenchesChain.Count > 0)
            {
                var lastRootWorkbenchId = rootWorkbenchesChain.Last().objectID;

                var canCraftBuffer = entityManager.GetBuffer<CanCraftObjectsBuffer>(entity);
                canCraftBuffer.Add(new CanCraftObjectsBuffer
                {
                    objectID = (ObjectID)lastRootWorkbenchId,
                    amount = 1,
                    entityAmountToConsume = 0
                });
            }
        }

        [PrefabModification(typeof(SimpleCraftingBuilding))]
        private static void EditSimpleCraftingBuilding(MonoBehaviour entityMono)
        {
            SimpleCraftingBuilding craftingBuilding = (SimpleCraftingBuilding)entityMono;

            foreach (var pair in modWorkbenches)
            {
                var skinsTexture = pair.Value.skinsTexture;
                if (skinsTexture == null)
                {
                    CoreLibMod.Log.LogWarning($"Failed to add {pair.Key} workbench skinsTexture because it's null!");
                    continue;
                }

                var reskinInfo = new EntityMonoBehaviour.ReskinInfo
                {
                    objectIDToUseReskinOn = pair.Key,
                    worksForAnyVariation = true,
                    textures = new List<Texture2D>(1) { skinsTexture }
                };

                foreach (var reskinOption in craftingBuilding.reskinOptions)
                {
                    reskinOption.reskins.Add(reskinInfo);
                }
            }
        }

        private static void AddRootWorkbench()
        {
            rootWorkbenchDefinition.itemId = IncrementID(rootWorkbenchDefinition.itemId);

            ObjectID workbench = AddWorkbench(rootWorkbenchDefinition);
            if (GetMainEntity(workbench, out ObjectAuthoring entity))
            {
                if (rootWorkbenchesChain.Count > 0)
                {
                    var oldWorkbench = rootWorkbenchesChain.Last();
                    CraftingAuthoring crafting = oldWorkbench.gameObject.GetComponent<CraftingAuthoring>();
                    crafting.includeCraftedObjectsFromBuildings.Add(entity.gameObject.GetComponent<CraftingAuthoring>());
                }

                rootWorkbenchesChain.Add(entity);
            }

            LocalizationModule.AddEntityLocalization(workbench, $"Root Workbench {rootWorkbenchesChain.Count}",
                "This workbench contains all modded workbenches!");
        }

        private static void AddRootWorkbenchItem(ObjectID entityId)
        {
            Instance.ThrowIfNotLoaded();
            ThrowIfTooLate(nameof(AddModWorkbench));
            
            if (rootWorkbenchesChain.Count == 0) 
                AddRootWorkbench();
            
            while (true)
            {
                ObjectAuthoring workbenchEntity = rootWorkbenchesChain.Last();
                CraftingAuthoring craftingCdAuthoring = workbenchEntity.gameObject.GetComponent<CraftingAuthoring>();

                CoreLibMod.Log.LogDebug($"Adding item {entityId.ToString()} to root workbench");

                if (craftingCdAuthoring.canCraftObjects.Count < 18)
                {
                    craftingCdAuthoring.canCraftObjects.Add(new CraftingAuthoring.CraftableObject { objectID = entityId, amount = 1 });
                    return;
                }

                AddRootWorkbench();
            }
        }

        private static ObjectID AddWorkbench(WorkbenchDefinition workbenchDefinition)
        {
            ObjectID id = AddEntity(workbenchDefinition.itemId, "Assets/CoreLib/Prefab/Objects/TemplateWorkbench");
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

        #endregion

        #region Modification

        private static void RegisterEntityModifications_Internal(Assembly assembly)
        {
            IEnumerable<Type> types = assembly.GetTypes().Where(ReflectionUtil.HasAttribute<EntityModificationAttribute>);

            foreach (Type type in types)
            {
                RegisterEntityModificationsInType_Internal(type);
            }
        }

        private static void RegisterEntityModificationsInType_Internal(Type type)
        {
            int result = ReflectionUtil.RegisterAttributeFunction<EntityModificationAttribute, ModifyAction>(type, (action, attribute) =>
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

                    entityModifyFunctions.AddDelegate(attribute.target, action);
                }

                return true;
            });
            CoreLibMod.Log.LogInfo($"Registered {result} entity modifiers in type {type.FullName}!");
        }

        private static void RegisterPrefabModifications_Internal(Assembly assembly)
        {
            IEnumerable<Type> types = assembly.GetTypes().Where(ReflectionUtil.HasAttribute<PrefabModificationAttribute>);

            foreach (Type type in types)
            {
                RegisterPrefabModificationsInType_Internal(type);
            }
        }

        private static void RegisterPrefabModificationsInType_Internal(Type type)
        {
            int result = ReflectionUtil.RegisterAttributeFunction<PrefabModificationAttribute, Action<MonoBehaviour>>(type, (action, attribute) =>
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
                ObjectID objectID = GetObjectId(pair.Key);
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
                entityModifyFunctions[objectID]?.Invoke(entity, authoring, entityManager);
            }
        }

        internal static void ApplyPrefabModifications(MemoryManager memoryManager)
        {
            if (prefabModifyFunctions.Count == 0) return;

            foreach (var prefab in memoryManager.poolablePrefabBank.poolInitializers)
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

            CoreLibMod.Log.LogInfo("Finished Modifying Prefabs!");
        }

        #endregion

        #endregion
    }
}