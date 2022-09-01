using System.Collections.Generic;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace CoreLib.Submodules.CustomEntity
{
    public static class PrefabCrawler
    {
        public static Dictionary<string, Material> materials = new Dictionary<string, Material>();
        public static Dictionary<ObjectID, EntityMonoBehaviour> entityPrefabs = new Dictionary<ObjectID, EntityMonoBehaviour>();

        public static bool isReady { get; private set; }

        public static void CheckPrefabs(PugDatabaseAuthoring pugDatabaseAuthoring)
        {
            Il2CppSystem.Collections.Generic.List<EntityMonoBehaviourData> prefabs = pugDatabaseAuthoring.prefabList;

            if (prefabs == null)
            {
                CoreLibPlugin.Logger.LogWarning($"Entities is null!");
                return;
            }

            foreach (EntityMonoBehaviourData entity in prefabs)
            {
                foreach (PrefabInfo prefabInfo in entity.objectInfo.prefabInfos)
                {
                    GameObject prefab = prefabInfo.prefab?.gameObject;
                    if (prefab == null) continue;
                    CheckPrefab(prefab);
                }

                if (entity.objectInfo.prefabInfos.Count >= 1)
                {
                    if (!entityPrefabs.ContainsKey(entity.objectInfo.objectID))
                    {
                        PrefabInfo info = entity.objectInfo.prefabInfos._items[0];
                        entityPrefabs.Add(entity.objectInfo.objectID, info.prefab);
                    }
                }
            }

            isReady = true;
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
}