using System;
using System.Runtime.CompilerServices;
using CoreLib.Util.Extensions;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;

// Code taken from Unity engine source code, licensed under the Unity Companion License

namespace CoreLib.Submodules.ModComponent
{
    public static class EntityQueryExtensions
    {

        /// <summary>
        /// Creates a NativeArray containing the components of type T for the selected entities.
        /// </summary>
        /// <param name="allocator">The type of memory to allocate.</param>
        /// <typeparam name="T">The component type.</typeparam>
        /// <returns>An array containing the specified component for all the entities selected
        /// by the EntityQuery.</returns>
        /// <remarks>This version of the function blocks on all registered jobs that access any of the query components.</remarks>
        /// <exception cref="InvalidOperationException">Thrown if <typeparamref name="T"/> is not part of the query.</exception>

        [Obsolete("Not tested, may produce invalid results!")]
        public static unsafe ModNativeArray<T> ToModComponentDataArray<T>(this EntityQuery query, Allocator allocator) where T : unmanaged
        {
            return ToComponentDataArrayImpl<T>(query.__impl, allocator, query);
        }

        private static unsafe ModNativeArray<T> ToComponentDataArrayImpl<T>(EntityQueryImpl* impl, Allocator allocator, EntityQuery outer)
            where T : unmanaged
        {
            var componentType = new ModComponentTypeHandle<T>(true, impl->_Access->EntityComponentStore->GlobalSystemVersion);

            impl->CalculateChunkAndEntityCount(out int entityCount, out int chunkCount);

            ModNativeArray<T> res;

            /*in cases of sparse entities spread over many archetypes, the cache lines read from chunks will exceed
             the actual memory of the entities read. In cases like these, a jobified path is the better approach */
            if (math.max(chunkCount * 64, entityCount * Unsafe.SizeOf<T>()) <= EntityQueryImpl.kImmediateMemoryThreshold)
            {
                // The synchronous path needs to wait for jobs accessing any query components to complete.
                // Since we're only reading component values, we could potentially only wait on a subset of these jobs
                // (specifically, the ones writing to query components).
                impl->CompleteDependency();

                res = CreateComponentDataArray(allocator, componentType, entityCount, outer);
            }
            else
            {
                res = CreateComponentDataArrayAsyncComplete(allocator, componentType, entityCount, outer, impl->GetDependency());
            }

            return res;
        }

        /// <summary>
        /// Creates a NativeArray with the value of a single component for all entities matching the provided query.
        /// This function will not sync the needed types in the EntityQueryFilter so they have to be synced manually before calling this function.
        /// </summary>
        /// <param name="allocator">Allocator to use for the array.</param>
        /// <param name="typeHandle">Type handle for the component whose values should be extracted.</param>
        /// <param name="entityCount">Number of entities that match the query. Used as the output array size.</param>
        /// <param name="entityQuery">Entities that match this query will be included in the output.</param>
        /// <returns>NativeArray of all the chunks in the matchingArchetypes list.</returns>
        private static unsafe ModNativeArray<T> CreateComponentDataArray<T>(
            Allocator allocator,
            ModComponentTypeHandle<T> typeHandle,
            int entityCount,
            EntityQuery entityQuery)
            where T : unmanaged
        {
            var cache = entityQuery.__impl->_QueryData->GetMatchingChunkCache();
            var matchingArchetypes = entityQuery.__impl->_QueryData->MatchingArchetypes;

            var componentData = CreateNativeArray<T>(entityCount, allocator, NativeArrayOptions.UninitializedMemory);
            if (!entityQuery.HasFilter())
            {
                ChunkIterationUtility.GatherComponentData((byte*)componentData.GetUnsafePtr(), typeHandle.m_TypeIndex, ref cache);
            }
            else
            {
                var filter = entityQuery.__impl->_Filter;
                ChunkIterationUtility.GatherComponentDataWithFilter((byte*)componentData.GetUnsafePtr(), typeHandle.m_TypeIndex, ref cache, ref matchingArchetypes, ref filter);
            }

            return componentData;
        }
        
        private static unsafe ModNativeArray<T> CreateComponentDataArrayAsyncComplete<T>(
            Allocator allocator,
            ModComponentTypeHandle<T> typeHandle,
            int entityCount,
            EntityQuery entityQuery,
            JobHandle dependsOn)
            where T : unmanaged
        {
            var componentData = CreateNativeArray<T>(entityCount, allocator, NativeArrayOptions.UninitializedMemory);

            var job = new GatherComponentDataJob
            {
                ComponentData = (byte*)componentData.GetUnsafePtr(),
                TypeIndex = typeHandle.m_TypeIndex
            };
            var jobHandle = JobEntityBatchIndexExtensions.ScheduleParallel(job, entityQuery, dependsOn);
            jobHandle.Complete();

            return componentData;
        }
        
        /// <summary>
        /// Create a NativeArray, using a provided AllocatorHandle.
        /// </summary>
        /// <param name="length">The number of elements to allocate.</param>
        /// <param name="allocator">The AllocatorHandle to use.</param>
        /// <param name="options">Options for allocation, such as whether to clear the memory.</param>
        /// <returns>Returns the NativeArray that was created.</returns>
        public static ModNativeArray<T> CreateNativeArray<T>(int length, AllocatorManager.AllocatorHandle allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
            where T : unmanaged
        {
            ModNativeArray<T> container;
            if(!AllocatorManager.IsCustomAllocator(allocator))
            {
                container = new ModNativeArray<T>(length, allocator.ToAllocator, options);
            }
            else
            {
                container = new ModNativeArray<T>();
                container.Initialize(length, allocator, options);
            }
            return container;
        }
        
        private static unsafe void Initialize<T>(ref this ModNativeArray<T> array,
            int length,
            AllocatorManager.AllocatorHandle allocator,
            NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
            where T : unmanaged
        {
            AllocatorManager.AllocatorHandle handle = allocator;
            array.m_Buffer = handle.AllocateStruct<T>(length);
            array.m_Length = length;
            array.m_AllocatorLabel = Allocator.None;
            if (options == NativeArrayOptions.ClearMemory)
            {
                UnsafeUtility.MemClear(array.m_Buffer, array.m_Length * UnsafeUtility.SizeOf<T>());
            }
        }
        
        private static unsafe void* AllocateStruct<U>(ref this AllocatorManager.AllocatorHandle t, int items) where U : struct
        {
            return Allocate(ref t, UnsafeUtility.SizeOf<U>(), UnsafeUtility.AlignOf<U>(), items);
        }
        
        private static unsafe void* Allocate(ref this AllocatorManager.AllocatorHandle t, int sizeOf, int alignOf, int items)
        {
            return (void*)AllocateBlock(ref t, sizeOf, alignOf, items).Range.Pointer;
        }
        
        private static AllocatorManager.Block AllocateBlock(ref this AllocatorManager.AllocatorHandle t, int sizeOf, int alignOf, int items)
        {
            AllocatorManager.Block block = default;
            block.Range.Pointer = IntPtr.Zero;
            block.Range.Items = items;
            block.Range.Allocator = t.Handle;
            block.BytesPerItem = sizeOf;
            // Make the alignment multiple of cacheline size
            block.Alignment = math.max(JobsUtility.CacheLineSize, alignOf);

            t.Try(ref block);
            return block;
        }
    }
}