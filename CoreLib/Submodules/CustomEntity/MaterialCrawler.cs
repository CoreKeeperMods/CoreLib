using UnhollowerBaseLib;
using UnityEngine;

namespace CoreLib.Submodules.CustomEntity
{
    public static class MaterialCrawler
    {
        public static Il2CppSystem.Collections.Generic.Dictionary<string, Material> materials =
            new Il2CppSystem.Collections.Generic.Dictionary<string, Material>();

        public static bool isReady { get; private set; }

        public static void FindAllMaterials(PugDatabaseAuthoring pugDatabaseAuthoring)
        {
            Il2CppSystem.Collections.Generic.List<EntityMonoBehaviourData> entities = pugDatabaseAuthoring.prefabList;

            if (entities == null)
            {
                CoreLibPlugin.Logger.LogWarning($"Entities is null!");
                return;
            }

            foreach (EntityMonoBehaviourData entity in entities)
            {
                foreach (PrefabInfo prefabInfo in entity.objectInfo.prefabInfos)
                {
                    GameObject prefab = prefabInfo.prefab?.gameObject;
                    if (prefab == null) continue;
                    CheckPrefab(prefab);
                }
            }

            isReady = true;
            foreach (RuntimeMaterial material in RuntimeMaterial.applyQueue)
            {
                RuntimeMaterial.Apply(material);
            }
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