using System;
using System.Runtime.CompilerServices;
using CoreLib.Submodules.ModComponent;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Unity.Collections;
using Unity.Entities;

// ReSharper disable ForCanBeConvertedToForeach

namespace CoreLib.Submodules.ModSystem.Patches
{
    public static class StateRequestSystem_Patch
    {
        internal static EntityQuery entityQuery;
        internal static IntPtr updateJobFieldPtr;
        internal static ModComponentDataFromEntity<StateInfoCD> stateInfoGroup;

        private static unsafe ref StateRequestSystem.UpdateJob GetJob(StateRequestSystem system)
        {
            IntPtr fieldPtr = IL2CPP.Il2CppObjectBaseToPtrNotNull(system) + (int)IL2CPP.il2cpp_field_get_offset(updateJobFieldPtr);
            return ref Unsafe.AsRef<StateRequestSystem.UpdateJob>((void*)fieldPtr);
        }

        [HarmonyPatch(typeof(StateRequestSystem), nameof(StateRequestSystem.OnCreate))]
        [HarmonyPostfix]
        public static void OnCreate(StateRequestSystem __instance)
        {
            entityQuery = __instance.EntityManager.CreateEntityQuery(ComponentModule.ReadOnly<StateInfoCD>());
            updateJobFieldPtr = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<StateRequestSystem>.NativeClassPtr, nameof(StateRequestSystem._updateJob));
            stateInfoGroup = new ModComponentDataFromEntity<StateInfoCD>(__instance.EntityManager);

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
        public static void OnUpdate(StateRequestSystem __instance)
        {
            if (SystemModule.stateRequesters.Count == 0) return;
            
            __instance.Dependency.Complete();

            ref StateRequestSystem.UpdateJob job = ref GetJob(__instance);

            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
            NativeArray<Entity> entities = entityQuery.ToEntityArray(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                StateInfoCD stateInfoCd = stateInfoGroup[entity];
                bool hasChanged = false;

                foreach (IStateRequester stateRequester in SystemModule.stateRequesters)
                {
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
                }

                if (hasChanged)
                    ecb.SetModComponent(entity, stateInfoCd);
            }

            ecb.Playback(__instance.EntityManager);

            entities.Dispose();
            ecb.Dispose();
        }
    }
}