using System;
using System.Collections.Generic;
using CoreLib.Util;
using UnityEngine;

namespace CoreLib.Submodules.Equipment.Patches
{
    public static class MemoryManager_IndirectPatch
    {
        public static void InjectNewEquipmentSlots(MemoryManager __instance)
        {
            if (!EquipmentSlotModule.Loaded) return;
            
            foreach (KeyValuePair<Type, GameObject> kv in EquipmentSlotModule.slotPrefabs)
            {
                MonoBehaviourUtils.ApplyPrefabModAuthorings(null, kv.Value);
                
                __instance.poolablePrefabBank.poolInitializers.Add(new PoolablePrefabBank.PoolablePrefab()
                {
                    prefab = kv.Value,
                    initialSize = 16,
                    maxSize = 16
                });

                CoreLibPlugin.Logger.LogInfo($"Added EquipmentSlot of type {kv.Key.FullName}");
            }
        }
    }
}