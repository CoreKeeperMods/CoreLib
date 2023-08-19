using System.Collections.Generic;
using PugTilemap.Quads;
using UnityEngine;

namespace CoreLib.Submodules
{
    public static class PrefabCrawler
    {
        public static Dictionary<string, Material> materials = new Dictionary<string, Material>();
        private static bool materialsReady;
        public static Dictionary<ObjectID, GameObject> entityPrefabs = new Dictionary<ObjectID, GameObject>();
        private static bool entityPrefabsReady;

        public static void SetupPrefabIDMap(List<MonoBehaviour> prefabList)
        {
            if (entityPrefabsReady) return;

            foreach (MonoBehaviour monoBeh in prefabList)
            {
                var data = monoBeh.GetComponent<IEntityMonoBehaviourData>();
                if (!entityPrefabs.ContainsKey(data.ObjectInfo.objectID))
                {
                    PrefabInfo info = data.ObjectInfo.prefabInfos[0];
                    if (info == null) continue;
                    entityPrefabs.Add(data.ObjectInfo.objectID, info.prefab.gameObject);
                }
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
            var renderers = prefab.GetComponentsInChildren<Renderer>();

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