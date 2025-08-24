using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Entity.Components
{
    public class EntityPrefabOverride : ModCDAuthoringBase
    {
        /// <summary>
        /// Represents the ID of the source entity used for overriding prefabs.
        /// This ID is used to map and retrieve corresponding prefab data from the entityPrefabs dictionary in PrefabCrawler.
        /// </summary>
        public ObjectID sourceEntity;

        /// <summary>
        /// Overrides the prefab of a specified MonoBehaviour instance using an entity ID from the sourceEntity.
        /// </summary>
        /// <param name="data">The MonoBehaviour instance to which the prefab override will be applied.</param>
        /// <returns>
        /// Returns true if the override was successful; otherwise, returns false if the entity ID
        /// does not exist in PrefabCrawler's entityPrefabs dictionary.
        /// </returns>
        public override bool Apply(MonoBehaviour data)
        {
            ObjectID entityId = sourceEntity;
            if (PrefabCrawler.entityPrefabs.ContainsKey(entityId) &&
                data is EntityMonoBehaviourData monoData)
            {
                CoreLibMod.Log.LogInfo($"Overriding prefab for {monoData.ObjectInfo.objectID.ToString()} to {entityId.ToString()} prefab!");
                monoData.ObjectInfo.prefabInfos[0].prefab = PrefabCrawler.entityPrefabs[entityId].GetComponent<EntityMonoBehaviour>();
                Destroy(this);
            }else if (PrefabCrawler.entityPrefabs.ContainsKey(entityId) &&
                      data is ObjectAuthoring objectAuthoring)
            {
                CoreLibMod.Log.LogInfo($"Overriding prefab for {objectAuthoring.objectName} to {entityId.ToString()} prefab!");
                objectAuthoring.graphicalPrefab = PrefabCrawler.entityPrefabs[entityId];
                Destroy(this);
            }
            else
            {
                CoreLibMod.Log.LogWarning(
                    $"Prefab from entity {entityId.ToString()} was not found!");
                return false;
            }

            return true;
        }
    }
}