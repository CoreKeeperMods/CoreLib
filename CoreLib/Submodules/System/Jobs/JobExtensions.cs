using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace CoreLib.Submodules.ModSystem.Jobs
{
    public static class JobExtensions
    {
        public interface IModJob
        {
            void Execute();
        }
        
        [Obsolete("Method is not ready for use!")]
        public static unsafe JobHandle Schedule<T>(this ref T jobData, JobHandle dependsOn = default) where T : unmanaged, IModJob
        {
            void* ptr = Unsafe.AsPointer(ref jobData);
            JobsUtility.JobScheduleParameters parameters = new JobsUtility.JobScheduleParameters(ptr, JobStruct<T>.jobReflectionData, dependsOn, ScheduleMode.Single);
            return JobsUtility.Schedule(ref parameters);
        }
        
        [StructLayout(LayoutKind.Sequential, Size = 1)]
        [SuppressMessage("ReSharper", "StaticMemberInGenericType")]
        internal struct JobStruct<T> where T : unmanaged, IModJob
        {
            public static readonly IntPtr jobReflectionData;

            static JobStruct()
            {
                var @delegate = DelegateSupport.ConvertDelegate<JobDelegate>(Execute);
                //  var @delegate = new JobDelegate();
                jobReflectionData = JobsUtility.CreateJobReflectionData(Il2CppType.Of<T>(), @delegate);
            }
        
            public static unsafe void Execute(
                IntPtr data,
                IntPtr additionalPtr,
                IntPtr bufferRangePatchData,
                IntPtr ranges,
                int jobIndex)
            {
                ref T obj = ref Unsafe.AsRef<T>((void*)data);
                obj.Execute();
            }
        }
    
        public class JobDelegate : Il2CppSystem.Delegate
        {
            public JobDelegate(IntPtr pointer) : base(pointer) { }

            public JobDelegate(Object target, IntPtr method) : base(ClassInjector.DerivedConstructorPointer<JobDelegate>())
            {
                ClassInjector.DerivedConstructorBody(this);
            }

            public unsafe void Invoke(IntPtr ptr1, IntPtr ptr2, IntPtr ptr3, IntPtr ptr4, int value)
            {
                CoreLibPlugin.Logger.LogWarning("Invoke method is not implemented!");
            }
        }
        
        
    }
}