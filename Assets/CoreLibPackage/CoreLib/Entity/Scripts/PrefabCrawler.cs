using System.Collections.Generic;
using PugTilemap.Quads;
using UnityEngine;
using PugMod;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Entity
{
    /// <summary>
    /// Provides utility methods and centralized registries for managing and resolving Unity prefab and material assets.
    /// This static class is used to streamline the handling of GameObject prefabs and Materials within the project.
    /// </summary>
    public static class PrefabCrawler
    {
        /// Dictionary that stores material objects mapped by their unique names.
        /// Acts as a centralized registry for materials, enabling efficient lookup and reuse across various components.
        /// This facilitates the management of shared material instances and helps reduce redundancy in material creation.
        public static Dictionary<string, Material> materials = new Dictionary<string, Material>();

        /// Boolean flag indicating whether the process of discovering and registering materials has been completed.
        /// Used to prevent redundant executions of material discovery logic and ensure the initialization process is only performed once.
        private static bool materialsReady;

        /// Dictionary that maps unique object identifiers (ObjectID) to their associated GameObject prefabs.
        /// Used as a central repository for managing and retrieving prefab instances tied to specific objects.
        /// This mapping is built during the initialization process and enables dynamic prefab resolution.
        public static Dictionary<ObjectID, GameObject> entityPrefabs = new Dictionary<ObjectID, GameObject>();

        /// Indicates whether the entity prefabs have been successfully initialized.
        /// Determines if the mapping between entity object IDs and their corresponding GameObject prefabs is ready for use.
        /// Once set to true, the entity prefab setup will not be re-executed during the application's lifetime.
        private static bool entityPrefabsReady;

        /// Sets up a mapping between entity object IDs and their corresponding GameObject prefabs.
        /// Processes a list of MonoBehaviour components, identifies valid entities using their associated
        /// `EntityMonoBehaviourData` component, and adds the prefabs to the dictionary if not already present.
        /// Ensures the mapping is initialized only once during the lifetime of the application.
        /// <param name="prefabList">A list of MonoBehaviour components representing potential entity prefabs.
        /// Each prefab should have an associated `EntityMonoBehaviourData` component that contains the necessary
        /// metadata for establishing the mapping.</param>
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

        /// Finds and adds all materials associated with the layers in the provided tileset to the materials collection.
        /// Processes the main tileset material as well as any custom or override materials defined in the layers.
        /// Ensures the materials are only added to the collection if they are not already present.
        /// <param name="tileset">The tileset object containing the main tileset material and a collection of layers, each with possible custom or override materials.</param>
        public static void FindMaterialsInTilesetLayers(PugMapTileset tileset)
        {
            TryAddMaterial(tileset.tilesetMaterial);
            foreach (var layer in tileset.layers)
            {
                TryAddMaterial(layer.customMaterial);
                TryAddMaterial(layer.overrideMaterial);
            }
        }

        /// Finds all materials associated with the provided list of prefabs and adds them to the materials collection.
        /// Ensures the materials are only processed once during the application lifecycle.
        /// <param name="prefabs">The list of poolable prefabs to process. Each prefab in the list is checked for associated renderers and materials.</param>
        public static void FindMaterials(List<PoolablePrefabBank.PoolablePrefab> prefabs)
        {
            if (materialsReady) return;

            foreach (var prefab in prefabs)
            {
                var prefabRoot = prefab.prefab;
                CheckPrefab(prefabRoot);
            }

            materialsReady = true;
        }

        /// Checks the specified prefab for any renderers and attempts to add their associated materials to the materials collection.
        /// <param name="prefab">The prefab to be checked for renderers. If the prefab is null or does not have any renderers with associated materials, no materials will be added.</param>
        private static void CheckPrefab(GameObject prefab)
        {
            var renderers = prefab.GetComponentsInChildren<Renderer>();

            foreach (var renderer in renderers)
            {
                if (renderer == null || renderer.sharedMaterial == null) continue;
                TryAddMaterial(renderer.sharedMaterial);
            }

        }

        /// Attempts to add the specified material to the materials collection if it is not already present.
        /// <param name="material">The material to be added to the materials collection. If the material is null or already exists in the collection, it will not be added.</param>
        private static void TryAddMaterial(Material material)
        {
            materials.TryAdd(material.name, material);
        }
    }
}