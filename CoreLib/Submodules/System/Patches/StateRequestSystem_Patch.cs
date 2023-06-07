using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CoreLib.Submodules.ModSystem.Jobs;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Reflection;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using CommandBufferExtensions = CoreLib.Submodules.ModComponent.CommandBufferExtensions;
using ComponentModule = CoreLib.Submodules.ModComponent.ComponentModule;
using Type = Il2CppSystem.Type;

// ReSharper disable ForCanBeConvertedToForeach

namespace CoreLib.Submodules.ModSystem.Patches
{
    public static class StateRequestSystem_Patch
    {
        internal static IntPtr updateJobFieldPtr;
        internal static IntPtr getNativeArrayPtr;

        internal static EntityQuery entityQuery;

        private static unsafe ref StateRequestSystem.UpdateJob GetJob(StateRequestSystem system)
        {
            IntPtr fieldPtr = IL2CPP.Il2CppObjectBaseToPtrNotNull(system) + (int)IL2CPP.il2cpp_field_get_offset(updateJobFieldPtr);
            return ref Unsafe.AsRef<StateRequestSystem.UpdateJob>((void*)fieldPtr);
        }

        private static unsafe StateRequestSystem.UpdateJob* GetJobPtr(StateRequestSystem system)
        {
            IntPtr fieldPtr = IL2CPP.Il2CppObjectBaseToPtrNotNull(system) + (int)IL2CPP.il2cpp_field_get_offset(updateJobFieldPtr);
            return (StateRequestSystem.UpdateJob*)fieldPtr;
        }

        private sealed class MethodInfoStoreGeneric_GetNativeArray<T>
        {
            internal static IntPtr Pointer = IL2CPP.il2cpp_method_get_from_reflection(
                IL2CPP.Il2CppObjectBaseToPtrNotNull(
                    new MethodInfo(IL2CPP.il2cpp_method_get_object(
                            getNativeArrayPtr,
                            Il2CppClassPointerStore<ArchetypeChunk>.NativeClassPtr))
                        .MakeGenericMethod(new Il2CppReferenceArray<Type>(new[]
                        {
                            Type.internal_from_handle(IL2CPP.il2cpp_class_get_type(Il2CppClassPointerStore<T>.NativeClassPtr))
                        }))));
        }

        private static unsafe NativeArray<T> GetNativeArrayB<T>(ref ArchetypeChunk chunk, ComponentTypeHandle<T> chunkComponentTypeHandle)
            where T : unmanaged
        {
            IntPtr* numPtr = stackalloc IntPtr[1];
            numPtr[0] = (IntPtr)Unsafe.AsPointer(ref chunkComponentTypeHandle);
            IntPtr exc = IntPtr.Zero;
            IntPtr thisPtr = (IntPtr)Unsafe.AsPointer(ref chunk);

            IntPtr num = IL2CPP.il2cpp_runtime_invoke(MethodInfoStoreGeneric_GetNativeArray<T>.Pointer, thisPtr, (void**)numPtr, ref exc);
            Il2CppException.RaiseExceptionIfNecessary(exc);
            return *(NativeArray<T>*)IL2CPP.il2cpp_object_unbox(num);
        }

        [HarmonyPatch(typeof(StateRequestSystem), nameof(StateRequestSystem.OnCreate))]
        [HarmonyPostfix]
        public static void OnCreate(StateRequestSystem __instance)
        {
            entityQuery = __instance.EntityManager.CreateEntityQuery(ComponentModule.ReadOnly<StateInfoCD>());
            updateJobFieldPtr = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<StateRequestSystem>.NativeClassPtr, nameof(StateRequestSystem._updateJob));
            getNativeArrayPtr = (IntPtr)typeof(ArchetypeChunk)
                .GetField("NativeMethodInfoPtr_GetNativeArray_Public_NativeArray_1_T_ComponentTypeHandle_1_T_0", AccessTools.all)
                .GetValue(null);

            SystemModule.stateRequesters.Sort((x, y) => x.priority.CompareTo(y.priority));

            foreach (IStateRequester stateRequester in SystemModule.stateRequesters)
            {
                try
                {
                    stateRequester.OnCreate(__instance.World);
                }
                catch (Exception e)
                {
                    CoreLibPlugin.Logger.LogError($"Error while executing {stateRequester.GetType().FullName} State Requester OnCreate():\n{e}");
                }
            }
        }

        [HarmonyPatch(typeof(StateRequestSystem), nameof(StateRequestSystem.OnUpdate))]
        [HarmonyPostfix]
        public static unsafe void OnUpdate(StateRequestSystem __instance)
        {
            if (SystemModule.stateRequesters.Count == 0) return;

            StateRequestSystem.UpdateJob* jobPtr = GetJobPtr(__instance);

            EntityCommandBuffer ecb = __instance.ecbSystem.CreateCommandBuffer();
            NativeArray<ArchetypeChunk> chunkArray = entityQuery.CreateArchetypeChunkArrayAsync(Allocator.TempJob, out JobHandle queryJob);

            JobHandle jobHandle = JobHandle.CombineDependencies(__instance.Dependency, queryJob);

            ModStateRequesterJob stateRequesterJob = new ModStateRequesterJob()
            {
                ecb = ecb,
                chunkArray = chunkArray,
                updateJob = jobPtr
            };

            JobHandle modStatesHandle = stateRequesterJob.ModSchedule(chunkArray.Length, 1, jobHandle);
            __instance.ecbSystem.AddJobHandleForProducer(modStatesHandle);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public unsafe struct ModStateRequesterJob : IModParallelJob
        {
            public EntityCommandBuffer ecb;
            [Il2CppAttribute(typeof(DeallocateOnJobCompletionAttribute))]
            public NativeArray<ArchetypeChunk> chunkArray;
            public StateRequestSystem.UpdateJob* updateJob;

            public void Execute(int index)
            {
                ArchetypeChunk chunk = chunkArray[index];
                if (chunk.Count == 0) return;
                
                UpdateForChunk(ref chunk);
            }

            private void UpdateForChunk(ref ArchetypeChunk chunk)
            {
                ref var job = ref Unsafe.AsRef<StateRequestSystem.UpdateJob>(updateJob);
                NativeArray<Entity> entities = chunk.GetNativeArray(job.Entity);
                NativeArray<StateInfoCD> stateInfos = GetNativeArrayB(ref chunk, job.StateInfo);

                foreach (IStateRequester stateRequester in SystemModule.stateRequesters)
                {
                    if (!stateRequester.ShouldUpdate(entities[0], ref job.Data, ref job.Containers)) continue;

                    for (int j = 0; j < entities.Length; j++)
                    {
                        Entity entity = entities[j];
                        StateInfoCD stateInfoCd = stateInfos[j];
                        bool hasChanged = false;

                        try
                        {
                            hasChanged |= stateRequester.OnUpdate(
                                entity,
                                ecb,
                                ref job.Data,
                                ref job.Containers,
                                ref stateInfoCd);
                        }
                        catch (Exception e)
                        {
                            CoreLibPlugin.Logger.LogError($"Error while executing {stateRequester.GetType().FullName} State Requester OnUpdate():\n{e}");
                        }


                        if (hasChanged)
                        {
                            CoreLibPlugin.Logger.LogInfo("Data has changed!");
                            CommandBufferExtensions.SetModComponent(ecb, entity, stateInfoCd);
                        }
                    }
                }
            }
        }
    }
}