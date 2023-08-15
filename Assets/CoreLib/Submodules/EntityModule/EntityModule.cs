using System.Collections.Generic;
using CoreLib.Util;
using UnityEngine;

namespace CoreLib.Submodules.EntityModule
{
    public static class EntityModule
    {
        internal static List<GameObject> modAuthoringTargets = new List<GameObject>();
        internal static List<GameObject> poolablePrefabs = new List<GameObject>();

        public static void AddToAuthoringList(GameObject gameObject)
        {
            modAuthoringTargets.Add(gameObject);
        }
        
        public static void EnablePooling(GameObject gameObject)
        {
            poolablePrefabs.Add(gameObject);
        }

        internal static void ApplyAll()
        {
            foreach (GameObject gameObject in modAuthoringTargets)
            {
                var objectAuthoring = gameObject.GetComponent<ObjectAuthoring>();
                var entityData = gameObject.GetComponent<EntityMonoBehaviourData>();

                MonoBehaviour dataMonoBehaviour = objectAuthoring != null ? (MonoBehaviour)objectAuthoring : (MonoBehaviour)entityData;
                MonoBehaviourUtils.ApplyPrefabModAuthorings(dataMonoBehaviour, gameObject);
                if (objectAuthoring != null && objectAuthoring.graphicalPrefab != null)
                {
                    MonoBehaviourUtils.ApplyPrefabModAuthorings(dataMonoBehaviour, objectAuthoring.graphicalPrefab);
                }
                else if (entityData != null && entityData.objectInfo.prefabInfos[0].prefab != null)
                {
                    MonoBehaviourUtils.ApplyPrefabModAuthorings(dataMonoBehaviour, entityData.objectInfo.prefabInfos[0].prefab.gameObject);
                }
            }
        }
    }
}