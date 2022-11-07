using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace CoreLib.Util.Jobs
{
    public class IJobChunkUtil
    {
        internal struct JobChunkWrapper<T> where T : struct
        {
            internal T JobData;
            
            internal ECSExtensions.NativeArrayData PrefilterData;

            internal int IsParallel;
        }
        
        internal static unsafe JobHandle ScheduleInternal<T>(ref T jobData, EntityQuery query, JobHandle dependsOn, ScheduleMode mode, bool isParallel = true)
            where T : struct
        {
            var unfilteredChunkCount = query.CalculateChunkCountWithoutFiltering();
            var impl = query._GetImpl();

            var prefilterHandle = ChunkIterationUtility.PreparePrefilteredChunkListsAsync(unfilteredChunkCount,

                ((EntityQueryData*)impl->_QueryData)->MatchingArchetypes, impl->_Filter, dependsOn, mode,
                out NativeArray<byte> prefilterData,
                out void* deferredCountData,
                out var useFiltering);

            JobChunkWrapper<T> jobChunkWrapper = new JobChunkWrapper<T>
            {
                JobData = jobData,
                PrefilterData = ECSExtensions.NativeArrayData.ToNativeArray(prefilterData),
                IsParallel = isParallel ? 1 : 0
            };

            var scheduleParams = new JobsUtility.JobScheduleParameters(
                Unsafe.AsPointer(ref jobChunkWrapper),
                isParallel ? JobChunkExtensions.JobChunkProducer<T>.InitializeParallel() : JobChunkExtensions.JobChunkProducer<T>.InitializeSingle(),
                prefilterHandle,
                mode);

            try
            {
                if (!isParallel)
                {
                    return JobsUtility.Schedule(ref scheduleParams);
                }
                else
                {
                    if (useFiltering)
                        return JobsUtility.ScheduleParallelForDeferArraySize(ref scheduleParams, 1, deferredCountData, null);
                    else
                        return JobsUtility.ScheduleParallelFor(ref scheduleParams, unfilteredChunkCount, 1);
                }
            }
            catch (InvalidOperationException e)
            {
                prefilterHandle.Complete();
                prefilterData.Dispose();
                throw;
            }
        }
    }
}