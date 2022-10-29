using System.Runtime.InteropServices;
using HarmonyLib;
using Il2CppInterop.Common;
using Il2CppInterop.Runtime;
using Il2CppSystem;
using Unity.Burst;
using Unity.Burst.LowLevel;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using ArgumentException = System.ArgumentException;
using Array = System.Array;
using Hash128 = UnityEngine.Hash128;
using IntPtr = System.IntPtr;
using InvalidOperationException = System.InvalidOperationException;
using Unsafe = System.Runtime.CompilerServices.Unsafe;

namespace CoreLib.Util.Extensions
{
    /// <summary>
    /// Extensions to allow using ECS components, which do not have AOT compiled variants. Also includes working with modded components
    /// </summary>
    public static class ECSExtensions
    {
        #region Public
        
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
        public static unsafe void AddNewComponentTypes(params Il2CppSystem.Type[] types)
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
        public static Type[] GetComponentTypes(ObjectID objectID)
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
            return Array.Empty<Type>();
        }

        /// <summary>
        /// List all <see cref="Il2CppSystem.Type"/> that are on the entity
        /// </summary>
        /// <param name="entityManager">World EntityManager</param>
        /// <param name="entity">Target Entity</param>
        public static Type[] GetComponentTypes(EntityManager entityManager, Entity entity)
        {
            NativeArray<ComponentType> typesArray = entityManager.GetComponentTypes(entity);
            Type[] types = new Type[typesArray.Length];

            for (var i = 0; i < typesArray.Length; i++)
            {
                types[i] = TypeManager.GetType(typesArray[i].TypeIndex);
            }

            return types;
        }

        /// <summary>
        /// Get Component Data of type.
        /// This method will work on any type, including mod created ones
        /// </summary>
        /// <param name="entityManager">World EntityManager</param>
        /// <param name="entity">Target Entity</param>
        /// <typeparam name="T">Component Type</typeparam>
        public static unsafe T GetModComponentData<T>(this EntityManager entityManager, Entity entity)
        {
            int typeIndex = GetModTypeIndex<T>();
            var dataAccess = entityManager.GetCheckedEntityDataAccessFix();
            if (!dataAccess->IsInExclusiveTransaction)
            {
                (&dataAccess->m_DependencyManager)->CompleteWriteDependency(typeIndex);
            }

            byte* ret = GetComponentDataWithTypeRO(GetEntityComponentStore(dataAccess), entity, typeIndex);

            return Marshal.PtrToStructure<T>((IntPtr)ret);
        }

        /// <summary>
        /// Set Component Data of type.
        /// This method will work on any type, including mod created ones
        /// </summary>
        /// <param name="entity">Target Entity</param>
        /// <param name="entityManager">World EntityManager</param>
        /// <param name="component">Component Data</param>
        /// <typeparam name="T">Component Type</typeparam>
        public static unsafe void SetModComponentData<T>(this EntityManager entityManager, Entity entity, T component)
        {
            int typeIndex = GetModTypeIndex<T>();
            var dataAccess = entityManager.GetCheckedEntityDataAccessFix();
            var componentStore = GetEntityComponentStore(dataAccess);

            if (!dataAccess->IsInExclusiveTransaction)
            {
                (&dataAccess->m_DependencyManager)->CompleteReadAndWriteDependency(typeIndex);
            }

            byte* writePtr = GetComponentDataWithTypeRW(componentStore, entity, typeIndex, componentStore->m_GlobalSystemVersion);
            Marshal.StructureToPtr(component, (IntPtr)writePtr, false);
        }

        public static bool AddModComponentData<T>(this EntityManager entityManager, Entity entity, T component)
        {
            ComponentType componentType = ComponentType.FromTypeIndex(GetModTypeIndex<T>());
            bool result = AddModComponent<T>(entityManager, entity);
            if (!componentType.IsZeroSized)
                SetModComponentData(entityManager, entity, component);

            return result;
        }
        
        public static unsafe bool AddModComponent<T>(this EntityManager entityManager, Entity entity)
        {
            ComponentType componentType = ComponentType.FromTypeIndex(GetModTypeIndex<T>());
            var dataAccess = entityManager.GetCheckedEntityDataAccessFix();
            var componentStore = GetEntityComponentStore(dataAccess);
            
            if (dataAccess->HasComponent(entity, componentType))
                return false;
            
            if (!componentStore->Exists(entity))
                throw new InvalidOperationException("The entity does not exist");
            
            AssertCanAddComponent(componentStore, GetArchetype(componentStore, entity), componentType);
            
            if (!dataAccess->IsInExclusiveTransaction)
                dataAccess->BeforeStructuralChange();

            var changes = componentStore->BeginArchetypeChangeTracking();
            
            bool result = AddComponentEntity(componentStore, &entity, componentType.TypeIndex);
            
            EndArchetypeChangeTracking(componentStore, changes, &dataAccess->m_EntityQueryManager);
            componentStore->InvalidateChunkListCacheForChangedArchetypes();
            dataAccess->PlaybackManagedChangesMono();

            return result;
        }

        public static unsafe bool HasModComponent<T>(this EntityManager entityManager, Entity entity)
        {
            ComponentType componentType = ComponentType.FromTypeIndex(GetModTypeIndex<T>());
            var dataAccess = entityManager.GetCheckedEntityDataAccessFix();

            return dataAccess->HasComponent(entity, componentType);
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
        
        #endregion

        #region PrivateImplementation

        private static readonly IntPtr GetCheckedEntityDataAccessFixMethodPtr;
        private static readonly IntPtr AddComponentEntityMethodPtr;
        private static readonly IntPtr EndArchetypeChangeTrackingMethodPtr;
        private static readonly IntPtr GetComponentDataWithTypeRWMethodPtr;
        private static readonly IntPtr GetComponentDataWithTypeROMethodPtr;
        private static readonly IntPtr GetOrCreateSharedMemoryMethodPtr;

        static ECSExtensions()
        {
            GetCheckedEntityDataAccessFixMethodPtr = (IntPtr)Il2CppInteropUtils
                .GetIl2CppMethodInfoPointerFieldForGeneratedMethod(typeof(EntityManager).GetMethod("GetCheckedEntityDataAccess")).GetValue(null);

            AddComponentEntityMethodPtr = (IntPtr)Il2CppInteropUtils
                .GetIl2CppMethodInfoPointerFieldForGeneratedMethod(typeof(StructuralChange).GetMethod("AddComponentEntity")).GetValue(null);

            EndArchetypeChangeTrackingMethodPtr = (IntPtr)Il2CppInteropUtils
                .GetIl2CppMethodInfoPointerFieldForGeneratedMethod(typeof(EntityComponentStore).GetMethod("EndArchetypeChangeTracking")).GetValue(null);

            GetComponentDataWithTypeRWMethodPtr = (IntPtr)Il2CppInteropUtils
                .GetIl2CppMethodInfoPointerFieldForGeneratedMethod(typeof(EntityComponentStore).GetMethod("GetComponentDataWithTypeRW", AccessTools.all, new[]{typeof(Entity), typeof(int), typeof(uint)})).GetValue(null);

            GetComponentDataWithTypeROMethodPtr = (IntPtr)Il2CppInteropUtils
                .GetIl2CppMethodInfoPointerFieldForGeneratedMethod(typeof(EntityComponentStore).GetMethod("GetComponentDataWithTypeRO", AccessTools.all, new[]{typeof(Entity), typeof(int)})).GetValue(null);

            GetOrCreateSharedMemoryMethodPtr = (IntPtr)Il2CppInteropUtils
                .GetIl2CppMethodInfoPointerFieldForGeneratedMethod(typeof(BurstCompilerService).GetMethod("GetOrCreateSharedMemory")).GetValue(null);

        }
        
        internal static unsafe ref T GetData<T>(this SharedStatic<T> shared) where T : new() 
        {
            IntPtr field = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<SharedStatic<T>>.NativeClassPtr, "_buffer");
            
            IntPtr intPtr = IL2CPP.Il2CppObjectBaseToPtrNotNull(shared) + (int)IL2CPP.il2cpp_field_get_offset(field);
            IntPtr intPtr2 = *(IntPtr*)intPtr;
            
            return ref System.Runtime.CompilerServices.Unsafe.AsRef<T>((void*)intPtr2);
        }

        internal static unsafe UnsafeList* GetMListData<T>(this NativeList<T> list) where T : new()
        {
            IntPtr field = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<NativeList<T>>.NativeClassPtr, "m_ListData");
            
            IntPtr intPtr = IL2CPP.Il2CppObjectBaseToPtrNotNull(list) + (int)IL2CPP.il2cpp_field_get_offset(field);
            IntPtr intPtr2 = *(IntPtr*)intPtr;
            return (UnsafeList*)intPtr2;
        }
        
        internal static unsafe Archetype* GetArchetype(EntityComponentStore* store, Entity entity)
        {
            return ((Archetype**)store->m_ArchetypeByEntity)[entity.Index];
        }
        
        internal static unsafe void AssertCanAddComponent(EntityComponentStore* store, Archetype* archetype, ComponentType componentType)
        {
            var componentTypeInfo = ((TypeManager.TypeInfo*)store->m_TypeInfos)[componentType.TypeIndex & 0x00FFFFFF];
            var componentInstanceSize = CollectionHelper.Align(componentTypeInfo.SizeInChunk, 64);
            var archetypeInstanceSize = archetype->InstanceSizeWithOverhead + componentInstanceSize;
            var chunkDataSize = Chunk.GetChunkBufferSize();
            if (archetypeInstanceSize > chunkDataSize)
                throw new InvalidOperationException("Entity archetype component data is too large. Previous archetype size per instance {archetype->InstanceSizeWithOverhead}  bytes. Attempting to add component size {componentInstanceSize} bytes. Maximum chunk size {chunkDataSize}.");
        }

        internal static unsafe EntityDataAccess* GetCheckedEntityDataAccessFix(this EntityManager entityManager)
        {
            IntPtr intPtr2 = IntPtr.Zero;
            IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(GetCheckedEntityDataAccessFixMethodPtr, (IntPtr)Unsafe.AsPointer(ref entityManager), null, ref intPtr2);
            Il2CppException.RaiseExceptionIfNecessary(intPtr2);
            
            return (EntityDataAccess*)intPtr;
        }

        internal static unsafe EntityComponentStore* GetEntityComponentStore(EntityDataAccess* dataAccess)
        {
            EntityComponentStore* ptr = &dataAccess->m_EntityComponentStore;
            return ptr;
        }

        internal static unsafe bool AddComponentEntity(EntityComponentStore* entityComponentStore, Entity* entity, int typeIndex)
        {
            IntPtr* ptr = stackalloc IntPtr[3 * sizeof(IntPtr)];
            ptr[0] = (IntPtr)entityComponentStore;
            ptr[1] = (IntPtr)entity;
            ptr[2] = (IntPtr)Unsafe.AsPointer(ref typeIndex);
            
            IntPtr intPtr2 = IntPtr.Zero;
            IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(AddComponentEntityMethodPtr, IntPtr.Zero, (void**)ptr, ref intPtr2);
            Il2CppException.RaiseExceptionIfNecessary(intPtr2);
            return *(bool*)IL2CPP.il2cpp_object_unbox(intPtr);
        }
        
        internal static unsafe void EndArchetypeChangeTracking(EntityComponentStore* entityComponentStore, EntityComponentStore.ArchetypeChanges changes, EntityQueryManager* queries)
        {
            IntPtr* ptr = stackalloc IntPtr[2 * sizeof(IntPtr)];
            ptr[0] = (IntPtr)Unsafe.AsPointer(ref changes);
            ptr[1] = (IntPtr)queries;
            IntPtr intPtr2 = IntPtr.Zero;
            IL2CPP.il2cpp_runtime_invoke(EndArchetypeChangeTrackingMethodPtr, (IntPtr)entityComponentStore, (void**)ptr, ref intPtr2);
            Il2CppException.RaiseExceptionIfNecessary(intPtr2);
        }
        
        internal static unsafe byte* GetComponentDataWithTypeRW(EntityComponentStore* entityComponentStore, Entity entity, int typeIndex, uint globalVersion)
        {
            IntPtr* ptr = stackalloc IntPtr[3 * sizeof(IntPtr)];
            ptr[0] = (IntPtr)Unsafe.AsPointer(ref entity);
            ptr[1] = (IntPtr)Unsafe.AsPointer(ref typeIndex);
            ptr[2] = (IntPtr)Unsafe.AsPointer(ref globalVersion);
            IntPtr intPtr2 = IntPtr.Zero;
            IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(GetComponentDataWithTypeRWMethodPtr, (IntPtr)entityComponentStore, (void**)ptr, ref intPtr2);
            Il2CppException.RaiseExceptionIfNecessary(intPtr2);

            return (byte*)intPtr;
        }
        
        internal static unsafe byte* GetComponentDataWithTypeRO(EntityComponentStore* entityComponentStore, Entity entity, int typeIndex)
        {
            IntPtr* ptr = stackalloc IntPtr[2 * sizeof(IntPtr)];
            ptr[0] = (IntPtr)Unsafe.AsPointer(ref entity);
            ptr[1] = (IntPtr)Unsafe.AsPointer(ref typeIndex);
            
            IntPtr intPtr2 = IntPtr.Zero;
            IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(GetComponentDataWithTypeROMethodPtr, (IntPtr)entityComponentStore, (void**)ptr, ref intPtr2);
            Il2CppException.RaiseExceptionIfNecessary(intPtr2);

            return  (byte*)intPtr;
        }

        internal static class SharedTypeIndex<TComponent>
        {
            public static readonly CustomSharedStatic<int> Ref = CustomSharedStatic<int>.GetOrCreate<TypeManager.TypeManagerKeyContext, TComponent>();
        }

        internal static unsafe void* GetOrCreateSharedStaticInternal(long getHashCode64, long getSubHashCode64, uint sizeOf, uint alignment)
        {
            if (sizeOf == 0) throw new ArgumentException("sizeOf must be > 0", nameof(sizeOf));
            var hash128 = new Hash128((ulong)getHashCode64, (ulong)getSubHashCode64);
            var result = GetOrCreateSharedMemory(ref hash128, sizeOf, alignment);
            if (result == null)
                throw new InvalidOperationException(
                    "Unable to create a SharedStatic for this key. It is likely that the same key was used to allocate a shared memory with a smaller size while the new size requested is bigger");
            return result;
        }

        internal static unsafe void* GetOrCreateSharedMemory(ref Hash128 key, uint sizeOf, uint alignment)
        {
            IntPtr* ptr = stackalloc IntPtr[3 * sizeof(IntPtr)];
            ptr[0] = (IntPtr)Unsafe.AsPointer(ref key);
            ptr[1] = (IntPtr)Unsafe.AsPointer(ref sizeOf);
            ptr[2] = (IntPtr)Unsafe.AsPointer(ref alignment);
            IntPtr intPtr2 = IntPtr.Zero;
            
            IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(GetOrCreateSharedMemoryMethodPtr, (IntPtr)0, (void**)ptr, ref intPtr2);
            Il2CppException.RaiseExceptionIfNecessary(intPtr2);

            return (void*)intPtr;
        }


        internal static long GetHashCode64<T>()
        {
            // DOTS Runtime IL2CPP Builds do not use C#'s lazy static initialization order (it uses a C like order, aka random)
            // As such we cannot rely on static init for caching types since any static constructor calling this function
            // may return uninitialized/default-initialized memory
            return BurstRuntime.HashStringWithFNV1A64(Il2CppType.Of<T>().AssemblyQualifiedName);
        }

        /// <summary>
        /// A structure that allows to share mutable static data between C# and HPC#.
        /// </summary>
        /// <typeparam name="T">Type of the data to share (must not contain any reference types)</typeparam>
        public readonly unsafe struct CustomSharedStatic<T> where T : struct
        {
            public readonly void* buffer;

            private CustomSharedStatic(void* buffer)
            {
                this.buffer = buffer;
            }

            /// <summary>
            /// Get a writable reference to the shared data.
            /// </summary>
            public ref T Data => ref Unsafe.AsRef<T>(buffer);

            /// <summary>
            /// Get a direct unsafe pointer to the shared data.
            /// </summary>
            public void* UnsafeDataPointer => buffer;

            /// <summary>
            /// Creates a shared static data for the specified context (usable from both C# and HPC#)
            /// </summary>
            /// <typeparam name="TContext">A type class that uniquely identifies the this shared data.</typeparam>
            /// <param name="alignment">Optional alignment</param>
            /// <returns>A shared static for the specified context</returns>
            public static CustomSharedStatic<T> GetOrCreate<TContext>(uint alignment = 0)
            {
                return new CustomSharedStatic<T>(GetOrCreateSharedStaticInternal(
                    GetHashCode64<TContext>(), 0, (uint)UnsafeUtility.SizeOf<T>(), alignment == 0 ? 4 : alignment));
            }

            /// <summary>
            /// Creates a shared static data for the specified context and sub-context (usable from both C# and HPC#)
            /// </summary>
            /// <typeparam name="TContext">A type class that uniquely identifies the this shared data.</typeparam>
            /// <typeparam name="TSubContext">A type class that uniquely identifies this shared data within a sub-context of the primary context</typeparam>
            /// <param name="alignment">Optional alignment</param>
            /// <returns>A shared static for the specified context</returns>
            public static CustomSharedStatic<T> GetOrCreate<TContext, TSubContext>(uint alignment = 0)
            {
                return new CustomSharedStatic<T>(GetOrCreateSharedStaticInternal(
                    GetHashCode64<TContext>(), GetHashCode64<TSubContext>(),
                    (uint)UnsafeUtility.SizeOf<T>(), alignment == 0 ? 4 : alignment));
            }

            /// <summary>
            /// Creates a shared static data for the specified context (reflection based, only usable from C#, but not from HPC#)
            /// </summary>
            /// <param name="contextType">A type class that uniquely identifies the this shared data</param>
            /// <param name="alignment">Optional alignment</param>
            /// <returns>A shared static for the specified context</returns>
            public static CustomSharedStatic<T> GetOrCreate(Type contextType, uint alignment = 0)
            {
                return new CustomSharedStatic<T>(GetOrCreateSharedStaticInternal(
                    BurstRuntime.GetHashCode64(contextType), 0, (uint)UnsafeUtility.SizeOf<T>(), alignment == 0 ? 4 : alignment));
            }

            /// <summary>
            /// Creates a shared static data for the specified context and sub-context (usable from both C# and HPC#)
            /// </summary>
            /// <param name="contextType">A type class that uniquely identifies the this shared data</param>
            /// <param name="subContextType">A type class that uniquely identifies this shared data within a sub-context of the primary context</param>
            /// <param name="alignment">Optional alignment</param>
            /// <returns>A shared static for the specified context</returns>
            public static CustomSharedStatic<T> GetOrCreate(Type contextType, Type subContextType, uint alignment = 0)
            {
                return new CustomSharedStatic<T>(GetOrCreateSharedStaticInternal(
                    BurstRuntime.GetHashCode64(contextType), BurstRuntime.GetHashCode64(subContextType),
                    (uint)UnsafeUtility.SizeOf<T>(), alignment == 0 ? 4 : alignment));
            }
        }
        #endregion
    }
}