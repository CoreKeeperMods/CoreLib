using System.Collections;
using CoreLib.Submodule.EquipmentSlot.System;
using HarmonyLib;

namespace CoreLib.Submodule.EquipmentSlot.Patch
{
    public static class MemoryManager_Patch
    {
        
        [HarmonyPatch(typeof(MemoryManager), nameof(MemoryManager.Init))]
        [HarmonyPostfix]
        public static void OnInit(MemoryManager __instance)
        {
            Manager.RunAfterInitComplete(PoolEquipmentSlots(__instance));
        }

        private static IEnumerator PoolEquipmentSlots(MemoryManager memoryManager)
        {
            yield return null;

            foreach (var slot in EquipmentSlotModule.slots.Values)
            {
                if (slot.slotPrefab == null) continue;
                if (!slot.createPool) continue;
                
                //TODO what to do here??
                //memoryManager.CreateModdedPrefabPool(slot.slotPrefab, initialSize: 4);
                EquipmentSlotModule.log.LogInfo($"Registering {slot.slotType} equipment slot prefab for pooling");
            }
        }

    }
}