using System;
using CoreLib.Components;
using HarmonyLib;
using UnityEngine;

namespace CoreLib.Submodules.CustomEntity.Patches;

[HarmonyPatch]
public static class MemoryManager_Patch
{
    [HarmonyPatch(typeof(MemoryManager), nameof(MemoryManager.Init))]
    [HarmonyPrefix]
    public static void OnMemoryInit(MemoryManager __instance)
    {
        PrefabCrawler.FindMaterials(__instance.poolablePrefabBank.poolInitializers);
        CoreLibPlugin.Logger.LogInfo($"Finished crawling prefabs, found {PrefabCrawler.materials.Count} materials!");

        int count = 0;
            
        foreach (var prefabs in CustomEntityModule.entitiesToAdd.Values)
        {
            if (prefabs.Count <= 0) continue;
            if (HasPrefabOverrides(prefabs)) continue;

            EntityMonoBehaviourData entity = prefabs[0];

            foreach (PrefabInfo prefabInfo in entity.objectInfo.prefabInfos)
            {
                if (prefabInfo.prefab == null) continue;

                ApplyPrefabModAuthorings(entity, prefabInfo.prefab.gameObject);
                
                PoolablePrefabBank.PoolablePrefab prefab = new PoolablePrefabBank.PoolablePrefab
                {
                    prefab = prefabInfo.prefab.gameObject,
                    initialSize = 16,
                    maxSize = 1024
                };

                __instance.poolablePrefabBank.poolInitializers.Add(prefab);
                count++;
            }
        }

        CoreLibPlugin.Logger.LogDebug($"Added {count} custom prefab pools!");
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

    private static void ApplyPrefabModAuthorings(EntityMonoBehaviourData entityData, GameObject gameObject)
    {
        var customAuthorings = gameObject.GetComponentsInChildren<ModCDAuthoringBase>();
        foreach (ModCDAuthoringBase customAuthoring in customAuthorings)
        {
            try
            {
                customAuthoring.Apply(entityData);
            }
            catch (Exception e)
            {
                CoreLibPlugin.Logger.LogWarning($"Failed to apply {customAuthoring.GetIl2CppType().FullName} on {customAuthoring.name}:\n{e}");
            }
        }
    }
}