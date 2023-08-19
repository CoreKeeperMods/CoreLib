using System;
using CoreLib.Submodules;
using PugMod;
using UnityEngine;

namespace CoreLib.Components
{
    public class EntityPrefabOverride : ModCDAuthoringBase
    {
        public ObjectID sourceEntity;
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
                CoreLibMod.Log.LogInfo($"Overriding prefab for {objectAuthoring.objectID.ToString()} to {entityId.ToString()} prefab!");
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