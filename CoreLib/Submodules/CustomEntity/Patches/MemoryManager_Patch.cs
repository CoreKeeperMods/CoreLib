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
            foreach (var prefabs in CustomEntityModule.entitiesToAdd.Values)
            {
                if (prefabs.Count <= 0) continue;
                if (HasPrefabOverrides(prefabs)) continue;

                EntityMonoBehaviourData entity = prefabs[0];

                foreach (PrefabInfo prefabInfo in entity.objectInfo.prefabInfos)
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

        private static bool HasPrefabOverrides(System.Collections.Generic.List<EntityMonoBehaviourData> data)
        {
            foreach (EntityMonoBehaviourData entity in data)
            {
                EntityPrefabOverride prefabOverride = entity.GetComponent<EntityPrefabOverride>();
                if (prefabOverride != null)
                {
                    ObjectID entityId = prefabOverride.sourceEntity.Value;
                    if (entityId != ObjectID.None)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}