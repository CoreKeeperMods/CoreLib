using System.Runtime.InteropServices;
using CoreLib.Util.Jobs;
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
using Hash128 = UnityEngine.Hash128;
using IntPtr = System.IntPtr;
using InvalidOperationException = System.InvalidOperationException;
using Unsafe = System.Runtime.CompilerServices.Unsafe;

namespace CoreLib.Util
{
    /// <summary>
    /// Extensions to allow using ECS components, which do not have AOT compiled variants. Also includes working with modded components
    /// </summary>
    public static class ECSExtensions
    {
        #region Public
        
        /// <summary>
        /// Gets the run-time type information required to access an array of component data in a chunk.
        /// </summary>
        /// <param name="isReadOnly">Whether the component data is only read, not written. Access components as
        /// read-only whenever possible.</param>
        /// <typeparam name="T">A struct that implements <see cref="IComponentData"/>.</typeparam>
        /// <returns>An object representing the type information required to safely access component data stored in a
        /// chunk.</returns>
        /// <remarks>Pass an <see cref="ComponentTypeHandle{T}"/> instance to a job that has access to chunk data,
        /// such as an <see cref="IJobChunk"/> job, to access that type of component inside the job.</remarks>
        public static unsafe ModComponentTypeHandle<T> GetModComponentTypeHandle<T>(this ComponentSystemBase system, bool isReadOnly = false) where T : struct
        {
            SystemState* state =system.CheckedState();
            return state->GetModComponentTypeHandle<T>(isReadOnly);
        }
        
        /// <summary>
        /// Gets the run-time type information required to access an array of component data in a chunk.
        /// </summary>
        /// <param name="isReadOnly">Whether the component data is only read, not written. Access components as
        /// read-only whenever possible.</param>
        /// <typeparam name="T">A struct that implements <see cref="IComponentData"/>.</typeparam>
        /// <returns>An object representing the type information required to safely access component data stored in a
        /// chunk.</returns>
        /// <remarks>Pass an <see cref="ComponentTypeHandle{T}"/> instance to a job that has access to chunk data,
        /// such as an <see cref="IJobChunk"/> job, to access that type of component inside the job.</remarks>
        public static unsafe ModComponentTypeHandle<T> GetModComponentTypeHandle<T>(this SystemState system, bool isReadOnly = false) where T : struct
        {
            system.AddReaderWriter(isReadOnly ? ModComponents.ReadOnly<T>() : ModComponents.ReadWrite<T>());
            return system.EntityManager.GetModComponentTypeHandle<T>(isReadOnly);
        }
        
        /// <summary>
        /// Gets the dynamic type object required to access a chunk component of type T.
        /// </summary>
        /// <remarks>
        /// To access a component stored in a chunk, you must have the type registry information for the component.
        /// This function provides that information. Use the returned <see cref="ComponentTypeHandle{T}"/>
        /// object with the functions of an <see cref="ArchetypeChunk"/> object to get information about the components
        /// in that chunk and to access the component values.
        /// </remarks>
        /// <param name="isReadOnly">Specify whether the access to the component through this object is read only
        /// or read and write. For managed components isReadonly will always be treated as false.</param>
        /// <typeparam name="T">The compile-time type of the component.</typeparam>
        /// <returns>The run-time type information of the component.</returns>

        public static unsafe ModComponentTypeHandle<T> GetModComponentTypeHandle<T>(this EntityManager entityManager, bool isReadOnly)
        {
            return new ModComponentTypeHandle<T>(isReadOnly, entityManager.GlobalSystemVersion);
        }
        
        /// <summary>
        /// Provides a native array interface to components stored in this chunk.
        /// </summary>
        /// <remarks>The native array returned by this method references existing data, not a copy.</remarks>
        /// <param name="chunkComponentTypeHandle">An object containing type and job safety information. Create this
        /// object by calling <see cref="ComponentSystemBase.GetComponentTypeHandle{T}(bool)"/>immediately
        /// before scheduling a job. Pass the object to a job using a public field you define as part of the job struct.</param>
        /// <typeparam name="T">The data type of the component.</typeparam>
        /// <exception cref="ArgumentException">If you call this function on a "tag" component type (which is an empty
        /// component with no fields).</exception>
        /// <returns>A native array containing the components in the chunk.</returns>
        public static unsafe NativeArrayData GetNativeArray<T>(this ArchetypeChunk chunk, ModComponentTypeHandle<T> chunkComponentTypeHandle)
            where T : struct
        {
            Chunk* chunks = (Chunk*)chunk.m_Chunk;
            Archetype* archetype = (Archetype*)chunks->Archetype;
            
            var typeIndexInArchetype = ChunkDataUtility.GetIndexInTypeArray(archetype, chunkComponentTypeHandle.m_TypeIndex);
            if (typeIndexInArchetype == -1)
            {
                return new NativeArrayData();
            }

            byte* ptr = (chunkComponentTypeHandle.IsReadOnly)
                ? ChunkDataUtility.GetComponentDataRO(chunks, 0, typeIndexInArchetype)
                : ChunkDataUtility.GetComponentDataRW(chunks, 0, typeIndexInArchetype, chunkComponentTypeHandle.GlobalSystemVersion);

            var length = chunk.Count;
            var batchStartOffset = chunk.m_BatchStartEntityIndex * ((ushort*)archetype->SizeOfs)[typeIndexInArchetype];
            var result = new NativeArrayData()
            {
                pointer = (IntPtr) ptr + batchStartOffset,
                length = length
            };

            return result;
        }

        public struct NativeArrayData
        {
            public IntPtr pointer;
            public int length;
            public Allocator allocatorLabel;
            
            public static unsafe NativeArrayData ToNativeArray<T>(NativeArray<T> array) where T : new()
            {
                return new NativeArrayData()
                {
                    pointer = (IntPtr)array.m_Buffer,
                    length = array.m_Length,
                    allocatorLabel = array.m_AllocatorLabel
                };
            }
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
            var dataAccess = entityManager.GetCheckedEntityDataAccess();
            if (!dataAccess->IsInExclusiveTransaction)
            {
                (&dataAccess->m_DependencyManager)->CompleteWriteDependency(typeIndex);
            }

            byte* ret = dataAccess->EntityComponentStore->GetComponentDataWithTypeRO(entity, typeIndex);

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
            var dataAccess = entityManager.GetCheckedEntityDataAccess();
            var componentStore = dataAccess->EntityComponentStore;

            if (!dataAccess->IsInExclusiveTransaction)
            {
                (&dataAccess->m_DependencyManager)->CompleteReadAndWriteDependency(typeIndex);
            }

            byte* writePtr = componentStore->GetComponentDataWithTypeRW(entity, typeIndex, componentStore->m_GlobalSystemVersion);
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
            var dataAccess = entityManager.GetCheckedEntityDataAccess();
            var componentStore = dataAccess->EntityComponentStore;
            
            CoreLibPlugin.Logger.LogInfo($"DataAccess: {(IntPtr)dataAccess}, ComponentStore: {(IntPtr)componentStore}");
            
            if (dataAccess->HasComponent(entity, componentType))
                return false;
            
            if (!componentStore->Exists(entity))
                throw new InvalidOperationException("The entity does not exist");

            if (!dataAccess->IsInExclusiveTransaction)
                dataAccess->BeforeStructuralChange();

            var changes = componentStore->BeginArchetypeChangeTracking();

            bool result = StructuralChange.AddComponentEntity(componentStore, &entity, componentType.TypeIndex);
            
            componentStore->EndArchetypeChangeTracking(changes, &dataAccess->m_EntityQueryManager);
            componentStore->InvalidateChunkListCacheForChangedArchetypes();
            dataAccess->PlaybackManagedChangesMono();

            return result;
        }

        public static unsafe bool HasModComponent<T>(this EntityManager entityManager, Entity entity)
        {
            ComponentType componentType = ComponentType.FromTypeIndex(GetModTypeIndex<T>());
            var dataAccess = entityManager.GetCheckedEntityDataAccess();

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
        /*
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
        }*/

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