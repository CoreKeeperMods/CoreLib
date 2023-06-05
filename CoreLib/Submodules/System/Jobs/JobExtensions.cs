using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unsafe = System.Runtime.CompilerServices.Unsafe;

namespace CoreLib.Submodules.ModSystem.Jobs
{
    public static unsafe class JobExtensions
    {
        
        [StructLayout(LayoutKind.Explicit)]
        internal struct JobReflectionData
        {
            [FieldOffset(0)]
            public CppGCHandle handle1;
            [FieldOffset(24)]
            public CppGCHandle handle2;
            [FieldOffset(48)]
            public CppGCHandle handle3;

            [FieldOffset(72)]
            public IntPtr method1;
            [FieldOffset(80)]
            public IntPtr method2;
            [FieldOffset(88)]
            public IntPtr method3;


            [FieldOffset(0x78)]
            public long someFlag;
            [FieldOffset(0x80)]
            public IntPtr burstDispatchInfo;
            [FieldOffset(0x88)]
            public IntPtr wrapperClass;
            [FieldOffset(0x90)]
            public IntPtr userJobClass;
            [FieldOffset(0x98)]
            public IntPtr domain;
            [FieldOffset(0xA0)]
            public JobHandle jobHandle;
            
            [FieldOffset(0xB0)]
            public IntPtr somethingAllocated;
            [FieldOffset(0x10C)]
            public IntPtr somethingCommited;
            
            [FieldOffset(0x160)]
            public IntPtr somethingAllocated_2;
            [FieldOffset(0x168)]
            public long element_size;
            [FieldOffset(0x170)]
            public IntPtr somethingAllocated_3;
            [FieldOffset(0x178)]
            public IntPtr somethingAllocated_4;
            [FieldOffset(0x180)]
            public IntPtr profileMarker_1;
            [FieldOffset(0x188)]
            public IntPtr profileMarker;
            [FieldOffset(0x190)]
            public IntPtr profileMarker_2;
            [FieldOffset(0x198)]
            public IntPtr somethingAllocated_1;
        }
        
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct CppGCHandle
        {
            public long m_Handle;
            public int m_BlockId;
            public int m_HandleInBlock;
            public int* m_PtrToReferences;
        }

        public interface IModJob
        {
            void Execute();
        }

        public interface IModParallelJob
        {
            void Execute(int index);
        }

        public static JobHandle ModSchedule<T>(this ref T jobData, JobHandle dependsOn = default) where T : unmanaged, IModJob
        {
            SystemModule.ThrowIfNotLoaded();
            IntPtr ptr = JobToPtr(ref jobData);
            
            JobsUtility.JobScheduleParameters parameters =
                CreateParams(ptr, JobStruct<T>.jobReflectionDataPtr, dependsOn, ScheduleMode.Single);
            return JobsUtility.Schedule(ref parameters);
        }
        
        public static void Run<T>(this T jobData) where T : unmanaged, IModJob
        {
            SystemModule.ThrowIfNotLoaded();
            IntPtr ptr = JobToPtr(ref jobData);
            JobsUtility.JobScheduleParameters jobScheduleParameters = CreateParams(ptr, JobStruct<T>.jobReflectionDataPtr, default, ScheduleMode.Run);
            JobsUtility.Schedule(ref jobScheduleParameters);
        }

        public static JobHandle ModSchedule<T>(this ref T jobData, int arrayLength, int innerloopBatchCount, JobHandle dependsOn = default)
            where T : unmanaged, IModParallelJob
        {
            SystemModule.ThrowIfNotLoaded();
            IntPtr ptr = JobToPtr(ref jobData);
            JobsUtility.JobScheduleParameters jobScheduleParameters = CreateParams(ptr,
                ParallelForJobStruct<T>.jobReflectionDataPtr, dependsOn, ScheduleMode.Batched);
            return JobsUtility.ScheduleParallelFor(ref jobScheduleParameters, arrayLength, innerloopBatchCount);
        }
        
        public static void ModRun<T>(this ref T jobData, int arrayLength) where T : unmanaged, IModParallelJob
        {
            SystemModule.ThrowIfNotLoaded();
            IntPtr ptr = JobToPtr(ref jobData);
            JobsUtility.JobScheduleParameters jobScheduleParameters = CreateParams(ptr,
                ParallelForJobStruct<T>.jobReflectionDataPtr, default, ScheduleMode.Run);
            JobsUtility.ScheduleParallelFor(ref jobScheduleParameters, arrayLength, arrayLength);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static JobsUtility.JobScheduleParameters CreateParams(IntPtr i_jobData, IntPtr i_reflectionData, JobHandle i_dependency, ScheduleMode i_scheduleMode)
        {
            return new JobsUtility.JobScheduleParameters()
            {
                Dependency = i_dependency,
                JobDataPtr = i_jobData,
                ReflectionData = i_reflectionData,
                ScheduleMode = (int)i_scheduleMode,
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IntPtr JobToPtr<T>(ref T jobData) where T : struct
        {
            return (IntPtr)Unsafe.AsPointer(ref jobData);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref T PtrToJob<T>(IntPtr ptr) where T : struct
        {
            return ref Unsafe.AsRef<T>((void*)ptr);
        }

        private delegate byte d_GetWorkStealingRange(IntPtr ranges, int jobIndex, IntPtr beginIndex, IntPtr endIndex);

        private static readonly d_GetWorkStealingRange i_GetWorkStealingRange =
            IL2CPP.ResolveICall<d_GetWorkStealingRange>("Unity.Jobs.LowLevel.Unsafe.JobsUtility::GetWorkStealingRange");

        private static bool GetWorkStealingRange(ref JobRanges ranges, int jobIndex, out int beginIndex, out int endIndex)
        {
            beginIndex = 0;
            endIndex = 0;
            IntPtr rangesPtr = (IntPtr)Unsafe.AsPointer(ref ranges);
            IntPtr beginPtr = (IntPtr)Unsafe.AsPointer(ref beginIndex);
            IntPtr endPtr = (IntPtr)Unsafe.AsPointer(ref endIndex);

            return i_GetWorkStealingRange.Invoke(rangesPtr, jobIndex, beginPtr, endPtr) == 1;
        }

        [StructLayout(LayoutKind.Sequential, Size = 1)]
        [SuppressMessage("ReSharper", "StaticMemberInGenericType")]
        internal struct ParallelForJobStruct<T> where T : unmanaged, IModParallelJob
        {
            public static readonly IntPtr jobReflectionDataPtr;
            public static ref JobReflectionData jobReflectionData => ref Unsafe.AsRef<JobReflectionData>((void*)jobReflectionDataPtr);

            static ParallelForJobStruct()
            {
                jobReflectionDataPtr = JobsUtility.CreateJobReflectionData(Il2CppType.Of<T>(), new JobDelegate(Execute));
                jobReflectionData.element_size = sizeof(T);
            }
            
            public static void Execute(
                IntPtr data,
                IntPtr additionalPtr,
                IntPtr bufferRangePatchData,
                IntPtr ranges,
                int jobIndex)
            {
                ref T job = ref PtrToJob<T>(data);
                ref JobRanges rangesObj = ref Unsafe.AsRef<JobRanges>((void*)ranges);

                while (true)
                {
                    if (!GetWorkStealingRange(ref rangesObj, jobIndex, out int beginIndex, out int endIndex))
                    {
                        break;
                    }

                    for (int i = beginIndex; i < endIndex; i++)
                    {
                        job.Execute(i);
                    }
                }
            }
        }

        [StructLayout(LayoutKind.Sequential, Size = 1)]
        [SuppressMessage("ReSharper", "StaticMemberInGenericType")]
        internal struct JobStruct<T> where T : unmanaged, IModJob
        {
            public static readonly IntPtr jobReflectionDataPtr;
            public static ref JobReflectionData jobReflectionData => ref Unsafe.AsRef<JobReflectionData>((void*)jobReflectionDataPtr);

            static JobStruct()
            {
                jobReflectionDataPtr = JobsUtility.CreateJobReflectionData(Il2CppType.Of<T>(), new JobDelegate(Execute));
                jobReflectionData.element_size = sizeof(T);
            }

            public static void Execute(
                IntPtr data,
                IntPtr additionalPtr,
                IntPtr bufferRangePatchData,
                IntPtr ranges,
                int jobIndex)
            {
                ref T job = ref PtrToJob<T>(data);
                job.Execute();
            }
        }
        
        internal delegate void ManagedDelegate(IntPtr data, IntPtr additionalPtr, IntPtr bufferRangePatchData, IntPtr ranges, int jobIndex);

        internal class JobDelegate : Il2CppSystem.Delegate
        {
            public ManagedDelegate managedDelegate;

            public JobDelegate(IntPtr ptr) : base(ptr)
            {
            }
            
            public JobDelegate(ManagedDelegate managedDelegate) : base(ClassInjector.DerivedConstructorPointer<JobDelegate>())
            {
                ClassInjector.DerivedConstructorBody(this);
                this.managedDelegate = managedDelegate;
            }

            public void Invoke(IntPtr data, IntPtr additionalPtr, IntPtr bufferRangePatchData, IntPtr ranges, int jobIndex)
            {
                managedDelegate?.Invoke(data, additionalPtr, bufferRangePatchData, ranges, jobIndex);
            }
        }
    }
}