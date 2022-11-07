using System;
using Unity.Collections;
using Unity.Entities;
using Type = Il2CppSystem.Type;

namespace CoreLib.Util
{
    public class ModComponents
    {
        /// <summary>
        /// This function allows for unregistered component types to be added to the TypeManager allowing for their use
        /// across the ECS apis _after_ TypeManager.Initialize() may have been called. Importantly, this function must
        /// be called from the main thread and will create a synchronization point across all worlds. If a type which
        /// is already registered with the TypeManager is passed in, this function will throw.
        /// </summary>
        /// <remarks>Types with [WriteGroup] attributes will be accepted for registration however their
        /// write group information will be ignored.</remarks>
        /// <param name="types"></param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ArgumentException"></exception>
        internal static unsafe void AddNewComponentTypes(params Il2CppSystem.Type[] types)
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
            TypeManager.SharedEntityOffsetInfo.Ref.GetData() = TypeManager.s_EntityOffsetList.GetMListData()->Ptr;
            TypeManager.SharedBlobAssetRefOffset.Ref.GetData() = TypeManager.s_BlobAssetRefOffsetList.GetMListData()->Ptr;
            TypeManager.SharedWriteGroup.Ref.GetData() = TypeManager.s_WriteGroupList.GetMListData()->Ptr;

            // Since the ptrs may have changed we need to ensure all entity component stores are using the correct ones
            foreach (var w in World.All)
            {
                var access = w.EntityManager.GetCheckedEntityDataAccess();
                var ecs = access->EntityComponentStore;
                ecs->InitializeTypeManagerPointers();
            }
        }
        
        /// <summary>
        /// Get Component Data for entity
        /// </summary>
        /// <param name="objectID">Entity ObjectID</param>
        /// <typeparam name="T">Component Type</typeparam>
        public static T GetPugComponentData<T>(ObjectID objectID)
        {
            return GetPugComponentData<T>(new ObjectDataCD {objectID = objectID});
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
                World world = World.DefaultGameObjectInjectionWorld;
                Entity entity = PugDatabase.objectPrefabEntityLookup[objectData];
                return world.EntityManager.GetModComponentData<T>(entity);
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
            SetPugComponentData(new ObjectDataCD {objectID = objectID}, component);
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
                World world = World.DefaultGameObjectInjectionWorld;
                Entity entity = PugDatabase.objectPrefabEntityLookup[objectData];
                world.EntityManager.SetModComponentData(entity, component);
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
            return HasPugComponentData<T>(new ObjectDataCD {objectID = objectID});
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
                World world = World.DefaultGameObjectInjectionWorld;
                Entity entity = PugDatabase.objectPrefabEntityLookup[objectData];
                return world.EntityManager.HasModComponent<T>(entity);
            }

            CoreLibPlugin.Logger.LogWarning($"No prefab in PugDatabase with objectID: {objectData.objectID}");
            return default;
        }

        /// <summary>
        /// List all <see cref="Il2CppSystem.Type"/> that are on the entity
        /// </summary>
        /// <param name="objectID">Entity ObjectID</param>
        public static Il2CppSystem.Type[] GetComponentTypes(ObjectID objectID)
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
                World world = World.DefaultGameObjectInjectionWorld;
                Entity entity = PugDatabase.objectPrefabEntityLookup[objectData];

                return GetComponentTypes(world.EntityManager, entity);
            }
            
            CoreLibPlugin.Logger.LogWarning($"No prefab in PugDatabase with objectID: {objectData.objectID}");
            return Array.Empty<Il2CppSystem.Type>();
        }

        /// <summary>
        /// List all <see cref="Il2CppSystem.Type"/> that are on the entity
        /// </summary>
        /// <param name="entityManager">World EntityManager</param>
        /// <param name="entity">Target Entity</param>
        public static Il2CppSystem.Type[] GetComponentTypes(EntityManager entityManager, Entity entity)
        {
            NativeArray<ComponentType> typesArray = entityManager.GetComponentTypes(entity);
            Il2CppSystem.Type[] types = new Type[typesArray.Length];

            for (var i = 0; i < typesArray.Length; i++)
            {
                types[i] = TypeManager.GetType(typesArray[i].TypeIndex);
            }

            return types;
        }

        public static ComponentType ReadOnly<T>()
        {
            int typeIndex = ECSExtensions.GetModTypeIndex<T>();
            ComponentType componentType = ComponentType.FromTypeIndex(typeIndex);
            componentType.AccessModeType = ComponentType.AccessMode.ReadOnly;
            return componentType;
        }
        
        public static ComponentType ReadWrite<T>()
        {
            int typeIndex = ECSExtensions.GetModTypeIndex<T>();
            ComponentType componentType = ComponentType.FromTypeIndex(typeIndex);
            componentType.AccessModeType = ComponentType.AccessMode.ReadWrite;
            return componentType;
        }
        
        public static ComponentType Exclude<T>()
        {
            int typeIndex = ECSExtensions.GetModTypeIndex<T>();
            ComponentType componentType = ComponentType.FromTypeIndex(typeIndex);
            componentType.AccessModeType = ComponentType.AccessMode.Exclude;
            return componentType;
        }
        
    }
}