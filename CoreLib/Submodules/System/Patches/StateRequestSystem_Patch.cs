using System;
using System.Runtime.CompilerServices;
using CoreLib.Submodules.ModComponent;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Unity.Collections;
using Unity.Entities;

namespace CoreLib.Submodules.ModSystem.Patches
{
    public static class StateRequestSystem_Patch
    {
        internal static ModComponentDataFromEntity<StateInfoCD> stateInfoGroup;

        internal static EntityQuery entityQuery;
        internal static IntPtr updateJobFieldPtr;

        private static unsafe ref StateRequestSystem.UpdateJob GetJob(StateRequestSystem system)
        {
            IntPtr fieldPtr = IL2CPP.Il2CppObjectBaseToPtrNotNull(system) + (int) IL2CPP.il2cpp_field_get_offset(updateJobFieldPtr);
            return ref Unsafe.AsRef<StateRequestSystem.UpdateJob>((void*)fieldPtr);
        }
        
        [HarmonyPatch(typeof(StateRequestSystem), nameof(StateRequestSystem.OnCreate))]
        [HarmonyPostfix]
        public static void OnCreate(StateRequestSystem __instance)
        {
            stateInfoGroup = new ModComponentDataFromEntity<StateInfoCD>(__instance.EntityManager);
            entityQuery = __instance.EntityManager.CreateEntityQuery(ComponentModule.ReadOnly<StateInfoCD>());
            updateJobFieldPtr = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<StateRequestSystem>.NativeClassPtr, nameof (StateRequestSystem._updateJob));

            foreach (IStateRequester stateRequester in SystemModule.stateRequesters)
            {
                stateRequester.OnCreate(__instance.World);
            }
        }

        [HarmonyPatch(typeof(StateRequestSystem), nameof(StateRequestSystem.OnUpdate))]
        [HarmonyPostfix]
        public static void Test(StateRequestSystem __instance)
        {
            __instance.Dependency.Complete();
            
            ref StateRequestSystem.UpdateJob job = ref GetJob(__instance);
            
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
            NativeArray<Entity> entities = entityQuery.ToEntityArray(Allocator.Temp);


            foreach (Entity entity in entities)
            {
                StateInfoCD stateInfoCd = stateInfoGroup[entity];
                bool hasChanged = false;
                
                foreach (IStateRequester stateRequester in SystemModule.stateRequesters)
                {
                    hasChanged |= stateRequester.OnUpdate(
                        entity,
                        ecb,
                        ref job.Data,
                        ref job.Containers,
                        ref stateInfoCd);
                }
                
                if (hasChanged)
                    ecb.SetModComponent(entity, stateInfoCd);
            }
                
            ecb.Playback(__instance.EntityManager);
        }
    }
}