using System;
using System.Collections;
using System.Linq;
using CoreLib.Submodule.EquipmentSlot.System;
using CoreLib.Util;
using CoreLib.Util.Extension;
using HarmonyLib;
using UnityEngine;

namespace CoreLib.Submodule.EquipmentSlot.Patch
{
    public static class MemoryManager_Patch
    {
        /*
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

                var poolBlock = ScriptableObject.CreateInstance<PooledObjectDataBlock>();
                poolBlock.prefab = slot.slotPrefab;

                var paramsBlock = ScriptableData.GetDataBlocks<PoolParameterDataBlock>().First(block => block.name.Contains("4_16_1024"));
                poolBlock.poolParams = paramsBlock;

                poolBlock.MakeAddress();
                
                CoreLibDataBlockLoader.Instance.AddDataBlock(poolBlock);
                
                EquipmentSlotModule.log.LogInfo($"Registering {slot.slotType} equipment slot prefab for pooling");
            }
        }*/

    }
}