using System;
using CoreLib;
using CoreLib.Submodules.CustomEntity;
using HarmonyLib;
using Il2CppSystem.Collections.Generic;
using UnityEngine;

namespace CoreLib.Submodules.CustomEntity.Patches
{
    [HarmonyPatch]
    public static class MemoryManager_Patch
    {
        [HarmonyPatch(typeof(MemoryManager), nameof(MemoryManager.Init))]
        [HarmonyPrefix]
        public static void OnMemoryInit(MemoryManager __instance)
        {
            foreach (EntityMonoBehaviourData data in CustomEntityModule.entitiesToAdd)
            {
                foreach (PrefabInfo prefabInfo in data.objectInfo.prefabInfos)
                {
                    if (prefabInfo.prefab == null) continue;

                    CoreLibPlugin.Logger.LogDebug($"About to add the prefab {data.objectInfo.objectID}!");
                    PoolablePrefabBank.PoolablePrefab prefab = new PoolablePrefabBank.PoolablePrefab
                    {
                        prefab = prefabInfo.prefab.gameObject,
                        initialSize = 16,
                        maxSize = 1024
                    };
                    __instance.poolablePrefabBank.poolInitializers.Add(prefab);
                }
            }
        }
    }
}