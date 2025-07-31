using System.Collections.Generic;
using PugTilemap.Quads;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodules.ModEntity
{
    public static class PrefabCrawler
    {
        public static Dictionary<string, Material> materials = new Dictionary<string, Material>();
        private static bool materialsReady;
        public static Dictionary<ObjectID, GameObject> entityPrefabs = new Dictionary<ObjectID, GameObject>();
        private static bool entityPrefabsReady;

        public static void SetupPrefabIDMap(List<MonoBehaviour> prefabList)
        {
            if (prefabList == null)
            {
                CoreLibMod.Log.LogWarning($"Failed to setup ID to prefab map: '{nameof(prefabList)}' is null!");
                return;
            }
            if (entityPrefabsReady) return;

            foreach (MonoBehaviour entity in prefabList)
            {
                var entityMonoBehaviorData = entity.GetComponent<EntityMonoBehaviourData>();
                if (entityMonoBehaviorData == null) continue;
                if (entityPrefabs.ContainsKey(entityMonoBehaviorData.objectInfo.objectID)) continue;
                PrefabInfo info = entityMonoBehaviorData.ObjectInfo.prefabInfos[0];
                if (info == null || info.prefab == null) continue;
                entityPrefabs.Add(entityMonoBehaviorData.ObjectInfo.objectID, info.prefab.gameObject);
            }

            entityPrefabsReady = true;
        }

        public static void FindMaterialsInTilesetLayers(PugMapTileset tileset)
        {
            TryAddMaterial(tileset.tilesetMaterial);
            foreach (QuadGenerator layer in tileset.layers)
            {
                TryAddMaterial(layer.customMaterial);
                TryAddMaterial(layer.overrideMaterial);
            }
        }
        
        public static void FindMaterials(List<PoolablePrefabBank.PoolablePrefab> prefabs)
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
            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>();

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null) continue;

                TryAddMaterial(renderer.sharedMaterial);
            }

        }

        private static void TryAddMaterial(Material material)
        {
            if (material != null && !materials.ContainsKey(material.name))
            {
                materials.Add(material.name, material);
            }
        }
    }
}