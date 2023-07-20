using System;
using System.Collections.Generic;
using CoreLib.Util;
using HarmonyLib;
using UnityEngine;

namespace CoreLib.Submodules.Equipment.Patches
{
    [HarmonyPatch]
    public static class MemoryManager_Patch
    {
        [HarmonyPatch(typeof(MemoryManager), nameof(MemoryManager.Init))]
        [HarmonyPrefix]
        public static void InjectNewEquipmentSlots(MemoryManager __instance)
        {
            Logger.LogInfo("MemoryManager::Init");
            
            PrefabCrawler.FindMaterials(__instance.poolablePrefabBank.poolInitializers);
            EntityModule.EntityModule.ApplyAll();
            
            foreach (KeyValuePair<Type, GameObject> kv in EquipmentSlotModule.slotPrefabs)
            {
                MonoBehaviourUtils.ApplyPrefabModAuthorings(null, kv.Value);
                
                __instance.poolablePrefabBank.poolInitializers.Add(new PoolablePrefabBank.PoolablePrefab()
                {
                    prefab = kv.Value,
                    initialSize = 16,
                    maxSize = 16
                });

                Logger.LogInfo($"Added EquipmentSlot of type {kv.Key.FullName}");
            }
        }
    }
}