using System;
using Il2CppInterop.Runtime.InteropTypes.Fields;

namespace CoreLib.Submodules.CustomEntity
{
    public class EntityPrefabOverride : ModCDAuthoringBase
    {
        public Il2CppValueField<ObjectID> sourceEntity;

        public EntityPrefabOverride(IntPtr ptr) : base(ptr) { }

        public override bool Apply(EntityMonoBehaviourData data)
        {
            ObjectID entityId = sourceEntity.Value;
            if (PrefabCrawler.entityPrefabs.ContainsKey(entityId))
            {
                CoreLibPlugin.Logger.LogInfo($"Overriding prefab for {data.objectInfo.objectID.ToString()} to {entityId.ToString()} prefab!");
                data.objectInfo.prefabInfos._items[0].prefab = PrefabCrawler.entityPrefabs[entityId];
                Destroy(this);
            }
            else
            {
                CoreLibPlugin.Logger.LogWarning(
                    $"Prefab from entity {entityId.ToString()} was not found!");
                return false;
            }

            return true;
        }
    }
}
