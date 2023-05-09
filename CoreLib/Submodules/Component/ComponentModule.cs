using System.Collections.Generic;
using System.Reflection;
using CoreLib.Components;
using CoreLib.Submodules.ModComponent.Patches;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using Il2CppSystem;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using ArgumentException = System.ArgumentException;
using Array = System.Array;
using IntPtr = System.IntPtr;
using InvalidOperationException = System.InvalidOperationException;

namespace CoreLib.Submodules.ModComponent
{
    [CoreLibSubmodule]
    public static class ComponentModule
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

        public static T GetModComponentData<T>(Entity entity, World world)
        {
            if (!world.EntityManager.HasModComponent<T>(entity))
            {
                CoreLibPlugin.Logger.LogWarning($"Tried to get component {typeof(T).FullName} from entity which does not have it!");
                return default;
            }
            return world.EntityManager.GetModComponentData<T>(entity);
        }
        
        public static void SetModComponentData<T>(Entity entity, World world, T componentData)
        {
            if (!world.EntityManager.HasModComponent<T>(entity))
            {
                CoreLibPlugin.Logger.LogWarning($"Tried to set component {typeof(T).FullName} to entity which does not have it!");
                return;
            }
            world.EntityManager.SetModComponentData(entity, componentData);
        }
        

        /// <summary>
        /// Get Component Data for entity
        /// </summary>
        /// <param name="objectID">Entity ObjectID</param>
        /// <typeparam name="T">Component Type</typeparam>
        public static T GetPugComponentData<T>(ObjectID objectID)
        {
            return GetPugComponentData<T>(new ObjectDataCD { objectID = objectID });
        }

        /// <summary>
        /// Get Component Data for entity
        /// </summary>
        /// <param name="objectData">Entity ObjectDataCD</param>
        /// <typeparam name="T">Component Type</typeparam>
        public static T GetPugComponentData<T>(ObjectDataCD objectData)
        {
            PugDatabase.InitObjectPrefabEntityLookup();
            objectData.variation = 0;
            objectData.amount = 1;

            if (PugDatabase.objectPrefabEntityLookup.ContainsKey(objectData))
            {
                Entity entity = PugDatabase.objectPrefabEntityLookup[objectData];
                return PugDatabase.world.EntityManager.GetModComponentData<T>(entity);
            }

            CoreLibPlugin.Logger.LogWarning($"No prefab in PugDatabase with objectID: {objectData.objectID}");
            return default;
        }

        /// <summary>
        /// Get Component Data for entity
        /// </summary>
        /// <param name="objectID">Entity ObjectID</param>
        /// <param name="component">Component Data</param>
        /// <typeparam name="T">Component Type</typeparam>
        public static void SetPugComponentData<T>(ObjectID objectID, T component)
        {
            SetPugComponentData(new ObjectDataCD { objectID = objectID }, component);
        }

        /// <summary>
        /// Set Component Data for entity
        /// </summary>
        /// <param name="objectData">Entity ObjectDataCD</param>
        /// <param name="component">Component Data</param>
        /// <typeparam name="T">Component Type</typeparam>
        public static void SetPugComponentData<T>(ObjectDataCD objectData, T component)
        {
            PugDatabase.InitObjectPrefabEntityLookup();
            objectData.variation = 0;
            objectData.amount = 1;

            if (PugDatabase.objectPrefabEntityLookup.ContainsKey(objectData))
            {
                Entity entity = PugDatabase.objectPrefabEntityLookup[objectData];
                PugDatabase.world.EntityManager.SetModComponentData(entity, component);
            }
            else
            {
                CoreLibPlugin.Logger.LogWarning($"No prefab in PugDatabase with objectID: {objectData.objectID}");
            }
        }

        /// <summary>
        /// Does Entity have component
        /// </summary>
        /// <param name="objectID">entity ObjectID</param>
        /// <typeparam name="T">Component Type</typeparam>
        public static bool HasPugComponentData<T>(ObjectID objectID)
        {
            return HasPugComponentData<T>(new ObjectDataCD { objectID = objectID });
        }

        /// <summary>
        /// Does Entity have component
        /// </summary>
        /// <param name="objectData">entity ObjectDataCD</param>
        /// <typeparam name="T">Component Type</typeparam>
        public static bool HasPugComponentData<T>(ObjectDataCD objectData)
        {
            PugDatabase.InitObjectPrefabEntityLookup();
            objectData.variation = 0;
            objectData.amount = 1;

            if (PugDatabase.objectPrefabEntityLookup.ContainsKey(objectData))
            {
                Entity entity = PugDatabase.objectPrefabEntityLookup[objectData];
                return PugDatabase.world.EntityManager.HasModComponent<T>(entity);
            }

            CoreLibPlugin.Logger.LogWarning($"No prefab in PugDatabase with objectID: {objectData.objectID}");
            return default;
        }

        /// <summary>
        /// List all <see cref="Il2CppSystem.Type"/> that are on the entity
        /// </summary>
        /// <param name="objectID">Entity ObjectID</param>
        public static Type[] GetPugComponentTypes(ObjectID objectID)
        {
            PugDatabase.InitObjectPrefabEntityLookup();
            ObjectDataCD objectData = new ObjectDataCD
            {
                objectID = objectID,
                variation = 0,
                amount = 1
            };

            if (PugDatabase.objectPrefabEntityLookup.ContainsKey(objectData))
            {
                Entity entity = PugDatabase.objectPrefabEntityLookup[objectData];

                return PugDatabase.world.EntityManager.GetModComponentTypes(entity);
            }

            CoreLibPlugin.Logger.LogWarning($"No prefab in PugDatabase with objectID: {objectData.objectID}");
            return Array.Empty<Type>();
        }

        public static ComponentType ReadOnly<T>()
        {
            int typeIndex = GetModTypeIndex<T>();
            ComponentType componentType = ComponentType.FromTypeIndex(typeIndex);
            componentType.AccessModeType = ComponentType.AccessMode.ReadOnly;
            return componentType;
        }

        public static ComponentType ReadWrite<T>()
        {
            int typeIndex = GetModTypeIndex<T>();
            ComponentType componentType = ComponentType.FromTypeIndex(typeIndex);
            componentType.AccessModeType = ComponentType.AccessMode.ReadWrite;
            return componentType;
        }

        public static ComponentType Exclude<T>()
        {
            int typeIndex = GetModTypeIndex<T>();
            ComponentType componentType = ComponentType.FromTypeIndex(typeIndex);
            componentType.AccessModeType = ComponentType.AccessMode.Exclude;
            return componentType;
        }
        
        public static int GetModTypeIndex<T>()
        {
            var index = SharedTypeIndex<T>.Ref.Data;

            if (index <= 0)
            {
                throw new ArgumentException($"Failed to get type index for {typeof(T).FullName}");
            }

            return index;
        }

        public static void RegisterECSComponent<T>()
        {
            ThrowIfNotLoaded();
            RegisterECSComponent(typeof(T));
        }

        public static void RegisterECSComponent(System.Type componentType)
        {
            ThrowIfNotLoaded();
            if (!ClassInjector.IsTypeRegisteredInIl2Cpp(componentType))
                ClassInjector.RegisterTypeInIl2Cpp(componentType);

            Type il2CppType = Il2CppType.From(componentType);

            if (!customComponentsTypes.Contains(il2CppType))
            {
                CoreLibPlugin.Logger.LogDebug($"Registering ECS component {componentType.FullName}");
                customComponentsTypes.Add(il2CppType);
            }
        }

        #endregion

        #region PrivateImplementation

        private static bool _loaded;

        internal static List<Type> customComponentsTypes = new List<Type>();

        internal static void ThrowIfNotLoaded()
        {
            if (!Loaded)
            {
                System.Type submoduleType = MethodBase.GetCurrentMethod().DeclaringType;
                string message = $"{submoduleType.Name} is not loaded. Please use [{nameof(CoreLibSubmoduleDependency)}(nameof({submoduleType.Name})]";
                throw new InvalidOperationException(message);
            }
        }


        [CoreLibSubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks()
        {
            CoreLibPlugin.harmony.PatchAll(typeof(TypeManager_Patch));
            CoreLibPlugin.harmony.PatchAll(typeof(GameObjectConversionMappingSystem_Patch));
        }

        [CoreLibSubmoduleInit(Stage = InitStage.PostLoad)]
        internal static void Load()
        {
            ClassInjector.RegisterTypeInIl2Cpp<ModCDAuthoringBase>();
        }

        /// <summary>
        /// This function allows for unregistered component types to be added to the TypeManager allowing for their use
        /// across the ECS apis _after_ TypeManager.Initialize() may have been called. Importantly, this function must
        /// be called from the main thread and will create a synchronization point across all worlds. If a type which
        /// is already registered with the TypeManager is passed in, this function will throw.
        /// </summary>
        /// <remarks>Types with [WriteGroup] attributes will be accepted for registration however their
        /// write group information will be ignored.</remarks>
        /// <param name="types"></param>
        /// <exception cref="System.InvalidOperationException"></exception>
        /// <exception cref="System.ArgumentException"></exception>
        internal static unsafe void AddNewComponentTypes(params Type[] types)
        {
            // We might invalidate the SharedStatics ptr so we must synchronize all jobs that might be using those ptrs
            foreach (var world in World.All)
                world.EntityManager.BeforeStructuralChange();

            // Is this a new type, or are we replacing an existing one?
            foreach (var type in types)
            {
                if (TypeManager.s_ManagedTypeToIndex.ContainsKey(type))
                    continue;

                var typeInfo = TypeManager.BuildComponentType(type);
                TypeManager.AddTypeInfoToTables(type, typeInfo, type.FullName);
            }

            // We may have added enough types to cause the underlying containers to resize so re-fetch their ptrs
            TypeManager.SharedEntityOffsetInfo.Ref.GetData() = new IntPtr(TypeManager.s_EntityOffsetList.GetUnsafePtr());
            TypeManager.SharedBlobAssetRefOffset.Ref.GetData() = new IntPtr(TypeManager.s_BlobAssetRefOffsetList.GetUnsafePtr());
            TypeManager.SharedWeakAssetRefOffset.Ref.GetData() = new IntPtr(TypeManager.s_WeakAssetRefOffsetList.GetUnsafePtr());
            TypeManager.SharedWriteGroup.Ref.GetData() = new IntPtr(TypeManager.s_WriteGroupList.GetUnsafePtr());

            // Since the ptrs may have changed we need to ensure all entity component stores are using the correct ones
            foreach (var w in World.All)
            {
                var access = w.EntityManager.GetCheckedEntityDataAccess();
                var ecs = access->EntityComponentStore;
                ecs->InitializeTypeManagerPointers();
            }
        }

        #endregion
    }
}