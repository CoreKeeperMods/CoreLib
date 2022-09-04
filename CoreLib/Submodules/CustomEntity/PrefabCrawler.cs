using System.Collections.Generic;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace CoreLib.Submodules.CustomEntity;

public static class PrefabCrawler
{
    public static Dictionary<string, Material> materials = new Dictionary<string, Material>();
    private static bool materialsReady;
    public static Dictionary<ObjectID, EntityMonoBehaviour> entityPrefabs = new Dictionary<ObjectID, EntityMonoBehaviour>();
    private static bool entityPrefabsReady;

    public static void SetupPrefabIDMap(Il2CppSystem.Collections.Generic.List<EntityMonoBehaviourData> prefabList)
    {
        if (entityPrefabsReady) return;
            
        foreach (EntityMonoBehaviourData data in prefabList)
        {
            if (!entityPrefabs.ContainsKey(data.objectInfo.objectID))
            {
                PrefabInfo info = data.objectInfo.prefabInfos._items[0];
                if (info == null) continue;
                entityPrefabs.Add(data.objectInfo.objectID, info.prefab);
            }
        }

        entityPrefabsReady = true;
    }

    public static void FindMaterials(Il2CppSystem.Collections.Generic.List<PoolablePrefabBank.PoolablePrefab> prefabs)
    {
        if (materialsReady) return;
            
        foreach (PoolablePrefabBank.PoolablePrefab prefab in prefabs)
        {
            GameObject prefabRoot = prefab.prefab;
            CheckPrefab(prefabRoot);
        }

        materialsReady = true;
    }

    private static void CheckPrefab(GameObject prefab)
    {
        Il2CppArrayBase<SpriteRenderer> renderers = prefab.GetComponentsInChildren<SpriteRenderer>();

        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer == null) continue;

            if (renderer.sharedMaterial != null && !materials.ContainsKey(renderer.sharedMaterial.name))
            {
                materials.Add(renderer.sharedMaterial.name, renderer.sharedMaterial);
            }
        }
    }
}