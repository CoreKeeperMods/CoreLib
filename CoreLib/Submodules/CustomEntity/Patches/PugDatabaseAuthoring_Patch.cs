using CoreLib;
using CoreLib.Submodules.CustomEntity;
using HarmonyLib;
using Il2CppSystem.Collections.Generic;
using UnityEngine;

namespace CoreLib.Submodules.CustomEntity.Patches;

public static class PugDatabaseAuthoring_Patch
{
    [HarmonyPatch(typeof(PugDatabaseAuthoring), nameof(PugDatabaseAuthoring.DeclareReferencedPrefabs))]
    [HarmonyPostfix]
    public static void InitMaterials(PugDatabaseAuthoring __instance, List<GameObject> referencedPrefabs) 
    {
        if (!CustomEntityModule.hasInjected)
        {
            CoreLibPlugin.Logger.LogInfo($"Start crawling prefabs!");

            PrefabCrawler.CheckPrefabs(__instance);

            CoreLibPlugin.Logger.LogInfo($"Finished crawling prefabs, found {PrefabCrawler.materials.Count} materials!");
            CustomEntityModule.hasInjected = true;
        }

        foreach (var prefabs in CustomEntityModule.entitiesToAdd.Values)
        {
            if (!ApplyOverrides(prefabs)) continue;

            foreach (EntityMonoBehaviourData data in prefabs)
            {
                __instance.prefabList.Add(data);
                referencedPrefabs.Add(data.gameObject);
            }
        }

        CoreLibPlugin.Logger.LogInfo($"Added {CustomEntityModule.entitiesToAdd.Count} entities!");

        foreach (EntityMonoBehaviourData entity in __instance.prefabList)
        {
            if (CustomEntityModule.entityModifyFunctions.ContainsKey(entity.objectInfo.objectID))
            {
                CustomEntityModule.entityModifyFunctions[entity.objectInfo.objectID]?.Invoke(entity);
            }
        }
        CustomEntityModule.entityModifyFunctions.Clear();
        
        CoreLibPlugin.Logger.LogInfo("Finished modifying entities!");

    }

    private static bool ApplyOverrides(System.Collections.Generic.List<EntityMonoBehaviourData> prefabs)
    {
        foreach (EntityMonoBehaviourData data in prefabs)
        {
            EntityPrefabOverride prefabOverride = data.GetComponent<EntityPrefabOverride>();
            if (prefabOverride != null)
            {
                ObjectID entityId = prefabOverride.sourceEntity.Value;
                if (PrefabCrawler.entityPrefabs.ContainsKey(entityId))
                {
                    CoreLibPlugin.Logger.LogInfo($"Overriding prefab for {data.objectInfo.objectID.ToString()} to {entityId.ToString()} prefab!");
                    data.objectInfo.prefabInfos._items[0].prefab = PrefabCrawler.entityPrefabs[entityId];
                    Object.Destroy(prefabOverride);
                }
                else
                {
                    CoreLibPlugin.Logger.LogWarning(
                        $"Failed to add entity {data.objectInfo.objectID.ToString()} prefab, because override prefab {entityId.ToString()} was not found!");
                    return false;
                }
            }
        }
        return true;
    }
}