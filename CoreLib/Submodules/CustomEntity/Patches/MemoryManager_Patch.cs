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
            foreach (EntityMonoBehaviourData data in CustomEntityModule.entitiesToAdd.Values)
            {
                CoreLibPlugin.Logger.LogInfo($"Checking prefab {data.objectInfo.objectID.ToString()}");
                EntityPrefabOverride prefabOverride = data.GetComponent<EntityPrefabOverride>();
                if (prefabOverride != null)
                {
                    ObjectID entityId = prefabOverride.sourceEntity.Value;
                    if (entityId != ObjectID.None)
                    {
                        CoreLibPlugin.Logger.LogInfo("Skipping prefab!");
                        continue;
                    }
                }
                
                foreach (PrefabInfo prefabInfo in data.objectInfo.prefabInfos)
                {
                    if (prefabInfo.prefab == null) continue;
                    

                    PoolablePrefabBank.PoolablePrefab prefab = new PoolablePrefabBank.PoolablePrefab
                    {
                        prefab = prefabInfo.prefab.gameObject,
                        initialSize = 16,
                        maxSize = 1024
                    };

                    CoreLibPlugin.Logger.LogInfo("Adding!");
                    __instance.poolablePrefabBank.poolInitializers.Add(prefab);
                }
            }
            CoreLibPlugin.Logger.LogDebug($"Done!");
        }

        [HarmonyPatch(typeof(MemoryManager), nameof(MemoryManager.Init))]
        [HarmonyPostfix]
        public static void AfterMemoryInit(MemoryManager __instance)
        {
            CoreLibPlugin.Logger.LogDebug($"MemoryManager Done!");
        }
    }
}