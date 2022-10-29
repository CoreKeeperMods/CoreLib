using System;
using System.Runtime.InteropServices;
using HarmonyLib;
using Il2CppInterop.Common;
using Il2CppInterop.Runtime;
using Unity.Burst;
using Unity.Burst.LowLevel;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unsafe = System.Runtime.CompilerServices.Unsafe;

namespace CoreLib.Util.Extensions
{
    public static class ECSExtensions
    {
        /// <summary>
        /// Get Component Data of type.
        /// Experimental method to bypass lack of AOT compiled method.
        /// </summary>
        /// <typeparam name="T">Component Type</typeparam>
        public static unsafe T GetComponentDataRaw<T>(this EntityManager entityManager, Entity entity)
        {
            int typeIndex = GetTypeIndex<T>();
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
        /// Experimental method to bypass lack of AOT compiled method.
        /// </summary>
        /// <param name="component">data to write</param>
        /// <typeparam name="T">Component Type</typeparam>
        public static unsafe void SetComponentDataRaw<T>(this EntityManager entityManager, Entity entity, T component)
        {
            int typeIndex = GetTypeIndex<T>();
            var dataAccess = entityManager.GetCheckedEntityDataAccessFix();
            var componentStore = GetEntityComponentStore(dataAccess);

            if (!dataAccess->IsInExclusiveTransaction)
            {
                (&dataAccess->m_DependencyManager)->CompleteReadAndWriteDependency(typeIndex);
            }

            byte* writePtr = GetComponentDataWithTypeRW(componentStore, entity, typeIndex, componentStore->m_GlobalSystemVersion);
            Marshal.StructureToPtr(component, (IntPtr)writePtr, false);
        }

        public static unsafe bool AddComponentDataRaw<T>(this EntityManager entityManager, Entity entity, T component)
        {
            ComponentType componentType = ComponentType.FromTypeIndex(GetTypeIndex<T>());
            bool result = AddComponentRaw<T>(entityManager, entity);
            if (!componentType.IsZeroSized)
                SetComponentDataRaw(entityManager, entity, component);

            return result;
        }
        
        public static unsafe bool AddComponentRaw<T>(this EntityManager entityManager, Entity entity)
        {
            ComponentType componentType = ComponentType.FromTypeIndex(GetTypeIndex<T>());
            var dataAccess = entityManager.GetCheckedEntityDataAccessFix();
            var componentStore = GetEntityComponentStore(dataAccess);
            
            CoreLibPlugin.Logger.LogInfo($"Adding: index: {componentType.TypeIndex}, dataAccess: {(IntPtr)dataAccess}, componentStore: {(IntPtr)componentStore}, entity {(IntPtr)(&entity)}");

            if (dataAccess->HasComponent(entity, componentType))
                return false;
            
            if (!componentStore->Exists(entity))
                throw new InvalidOperationException("The entity does not exist");
            
            AssertCanAddComponent(componentStore, GetArchetype(componentStore, entity), componentType);
            
            if (!dataAccess->IsInExclusiveTransaction)
                dataAccess->BeforeStructuralChange();

            var changes = componentStore->BeginArchetypeChangeTracking();
            
            bool result = AddComponentEntity(componentStore, &entity, componentType.TypeIndex);
            
            CoreLibPlugin.Logger.LogInfo($"Adding component, result: {result}");
            EndArchetypeChangeTracking(componentStore, changes, &dataAccess->m_EntityQueryManager);
            componentStore->InvalidateChunkListCacheForChangedArchetypes();
            dataAccess->PlaybackManagedChangesMono();

            return result;
        }

        public static unsafe bool HasComponentRaw<T>(this EntityManager entityManager, Entity entity)
        {
            ComponentType componentType = ComponentType.FromTypeIndex(GetTypeIndex<T>());
            var dataAccess = entityManager.GetCheckedEntityDataAccessFix();

            return dataAccess->HasComponent(entity, componentType);
        }
        
        public static unsafe Archetype* GetArchetype(EntityComponentStore* store, Entity entity)
        {
            return ((Archetype**)store->m_ArchetypeByEntity)[entity.Index];
        }
        
        public static unsafe void AssertCanAddComponent(EntityComponentStore* store, Archetype* archetype, ComponentType componentType)
        {
            var componentTypeInfo = ((TypeManager.TypeInfo*)store->m_TypeInfos)[componentType.TypeIndex & 0x00FFFFFF];
            var componentInstanceSize = CollectionHelper.Align(componentTypeInfo.SizeInChunk, 64);
            var archetypeInstanceSize = archetype->InstanceSizeWithOverhead + componentInstanceSize;
            var chunkDataSize = Chunk.GetChunkBufferSize();
            if (archetypeInstanceSize > chunkDataSize)
                throw new InvalidOperationException("Entity archetype component data is too large. Previous archetype size per instance {archetype->InstanceSizeWithOverhead}  bytes. Attempting to add component size {componentInstanceSize} bytes. Maximum chunk size {chunkDataSize}.");
        }

        public static unsafe EntityDataAccess* GetCheckedEntityDataAccessFix(this EntityManager entityManager)
        {
            IntPtr method = (IntPtr)Il2CppInteropUtils
                .GetIl2CppMethodInfoPointerFieldForGeneratedMethod(typeof(EntityManager).GetMethod("GetCheckedEntityDataAccess")).GetValue(null);
            
            
            
            IntPtr intPtr2 = IntPtr.Zero;
            IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(method, (IntPtr)Unsafe.AsPointer(ref entityManager), null, ref intPtr2);
            Il2CppException.RaiseExceptionIfNecessary(intPtr2);
            
            return (EntityDataAccess*)intPtr;
        }

        public static unsafe EntityComponentStore* GetEntityComponentStore(EntityDataAccess* dataAccess)
        {
            EntityComponentStore* ptr = &dataAccess->m_EntityComponentStore;
            return ptr;
        }

        public static unsafe bool AddComponentEntity(EntityComponentStore* entityComponentStore, Entity* entity, int typeIndex)
        {
            IntPtr method = (IntPtr)Il2CppInteropUtils
                .GetIl2CppMethodInfoPointerFieldForGeneratedMethod(typeof(StructuralChange).GetMethod("AddComponentEntity")).GetValue(null);
            
            IntPtr* ptr = stackalloc IntPtr[3 * sizeof(IntPtr)];
            ptr[0] = (IntPtr)entityComponentStore;
            ptr[1] = (IntPtr)entity;
            ptr[2] = (IntPtr)Unsafe.AsPointer(ref typeIndex);
            
            IntPtr intPtr2 = IntPtr.Zero;
            IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(method, IntPtr.Zero, (void**)ptr, ref intPtr2);
            Il2CppException.RaiseExceptionIfNecessary(intPtr2);
            return *(bool*)IL2CPP.il2cpp_object_unbox(intPtr);
        }
        
        public static unsafe void EndArchetypeChangeTracking(EntityComponentStore* entityComponentStore, EntityComponentStore.ArchetypeChanges changes, EntityQueryManager* queries)
        {
            IntPtr method = (IntPtr)Il2CppInteropUtils
                .GetIl2CppMethodInfoPointerFieldForGeneratedMethod(typeof(EntityComponentStore).GetMethod("EndArchetypeChangeTracking")).GetValue(null);
            
            IntPtr* ptr = stackalloc IntPtr[2 * sizeof(IntPtr)];
            ptr[0] = (IntPtr)Unsafe.AsPointer(ref changes);
            ptr[1] = (IntPtr)queries;
            IntPtr intPtr2 = IntPtr.Zero;
            IL2CPP.il2cpp_runtime_invoke(method, (IntPtr)entityComponentStore, (void**)ptr, ref intPtr2);
            Il2CppException.RaiseExceptionIfNecessary(intPtr2);
        }
        
        public static unsafe byte* GetComponentDataWithTypeRW(EntityComponentStore* entityComponentStore, Entity entity, int typeIndex, uint globalVersion)
        {
            IntPtr method = (IntPtr)Il2CppInteropUtils
                .GetIl2CppMethodInfoPointerFieldForGeneratedMethod(typeof(EntityComponentStore).GetMethod("GetComponentDataWithTypeRW", AccessTools.all, new[]{typeof(Entity), typeof(int), typeof(uint)})).GetValue(null);
            
            IntPtr* ptr = stackalloc IntPtr[3 * sizeof(IntPtr)];
            ptr[0] = (IntPtr)Unsafe.AsPointer(ref entity);
            ptr[1] = (IntPtr)Unsafe.AsPointer(ref typeIndex);
            ptr[2] = (IntPtr)Unsafe.AsPointer(ref globalVersion);
            IntPtr intPtr2 = IntPtr.Zero;
            IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(method, (IntPtr)entityComponentStore, (void**)ptr, ref intPtr2);
            Il2CppException.RaiseExceptionIfNecessary(intPtr2);

            return (byte*)intPtr;
        }
        
        public static unsafe byte* GetComponentDataWithTypeRO(EntityComponentStore* entityComponentStore, Entity entity, int typeIndex)
        {
            IntPtr method = (IntPtr)Il2CppInteropUtils
                .GetIl2CppMethodInfoPointerFieldForGeneratedMethod(typeof(EntityComponentStore).GetMethod("GetComponentDataWithTypeRO", AccessTools.all, new[]{typeof(Entity), typeof(int)})).GetValue(null);

            
            IntPtr* ptr = stackalloc IntPtr[2 * sizeof(IntPtr)];
            ptr[0] = (IntPtr)Unsafe.AsPointer(ref entity);
            ptr[1] = (IntPtr)Unsafe.AsPointer(ref typeIndex);
            
            IntPtr intPtr2 = IntPtr.Zero;
            IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(method, (IntPtr)entityComponentStore, (void**)ptr, ref intPtr2);
            Il2CppException.RaiseExceptionIfNecessary(intPtr2);

            return  (byte*)intPtr;
        }

        public static unsafe int GetTypeIndex<T>()
        {
            var index = SharedTypeIndex<T>.Ref.Data;

            if (index <= 0)
            {
                throw new ArgumentException($"Failed to get type index for {typeof(T).FullName}");
            }

            return index;
        }

        internal sealed class SharedTypeIndex<TComponent>
        {
            public static readonly CustomSharedStatic<int> Ref = CustomSharedStatic<int>.GetOrCreate<TypeManager.TypeManagerKeyContext, TComponent>();
        }

        public static unsafe void* GetOrCreateSharedStaticInternal(long getHashCode64, long getSubHashCode64, uint sizeOf, uint alignment)
        {
            if (sizeOf == 0) throw new ArgumentException("sizeOf must be > 0", nameof(sizeOf));
            var hash128 = new UnityEngine.Hash128((ulong)getHashCode64, (ulong)getSubHashCode64);
            var result = GetOrCreateSharedMemory(ref hash128, sizeOf, alignment);
            if (result == null)
                throw new InvalidOperationException(
                    "Unable to create a SharedStatic for this key. It is likely that the same key was used to allocate a shared memory with a smaller size while the new size requested is bigger");
            return result;
        }

        public static unsafe void* GetOrCreateSharedMemory(ref UnityEngine.Hash128 key, uint size_of, uint alignment)
        {
            IntPtr* ptr = stackalloc IntPtr[3 * sizeof(IntPtr)];
            ptr[0] = (IntPtr)Unsafe.AsPointer(ref key);
            ptr[1] = (IntPtr)Unsafe.AsPointer(ref size_of);
            ptr[2] = (IntPtr)Unsafe.AsPointer(ref alignment);
            IntPtr intPtr2 = IntPtr.Zero;

            IntPtr method = (IntPtr)Il2CppInteropUtils
                .GetIl2CppMethodInfoPointerFieldForGeneratedMethod(typeof(BurstCompilerService).GetMethod("GetOrCreateSharedMemory")).GetValue(null);

            IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(method, (IntPtr)0, (void**)ptr, ref intPtr2);
            Il2CppException.RaiseExceptionIfNecessary(intPtr2);

            return (void*)intPtr;
        }


        public static long GetHashCode64<T>()
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
            public readonly void* _buffer;

            private CustomSharedStatic(void* buffer)
            {
                _buffer = buffer;
            }

            /// <summary>
            /// Get a writable reference to the shared data.
            /// </summary>
            public ref T Data => ref Unsafe.AsRef<T>(_buffer);

            /// <summary>
            /// Get a direct unsafe pointer to the shared data.
            /// </summary>
            public void* UnsafeDataPointer => _buffer;

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

#if !NET_DOTS
            /// <summary>
            /// Creates a shared static data for the specified context (reflection based, only usable from C#, but not from HPC#)
            /// </summary>
            /// <param name="contextType">A type class that uniquely identifies the this shared data</param>
            /// <param name="alignment">Optional alignment</param>
            /// <returns>A shared static for the specified context</returns>
            public static CustomSharedStatic<T> GetOrCreate(Il2CppSystem.Type contextType, uint alignment = 0)
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
            public static CustomSharedStatic<T> GetOrCreate(Il2CppSystem.Type contextType, Il2CppSystem.Type subContextType, uint alignment = 0)
            {
                return new CustomSharedStatic<T>(GetOrCreateSharedStaticInternal(
                    BurstRuntime.GetHashCode64(contextType), BurstRuntime.GetHashCode64(subContextType),
                    (uint)UnsafeUtility.SizeOf<T>(), alignment == 0 ? (uint)4 : alignment));
            }
#endif
        }
    }
}