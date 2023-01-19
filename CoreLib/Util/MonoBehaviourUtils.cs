using System;
using CoreLib.Components;
using UnityEngine;

namespace CoreLib.Util
{
    public static class MonoBehaviourUtils
    {
        internal static void CallAlloc(EntityMonoBehaviourData entityData)
        {
            foreach (PrefabInfo prefabInfo in entityData.objectInfo.prefabInfos)
            {
                if (prefabInfo.prefab == null) continue;

                AllocObject(prefabInfo.prefab.gameObject);
            }
        
            AllocObject(entityData.gameObject);
        }

        internal static void AllocObject(GameObject prefab)
        {
            var components = prefab.GetComponentsInChildren<MonoBehaviour>();
            foreach (MonoBehaviour component in components)
            {
                component.TryInvokeAction(nameof(IAllocate.Allocate));
            }
        }

        internal static bool HasPrefabOverrides(System.Collections.Generic.List<EntityMonoBehaviourData> data)
        {
            foreach (EntityMonoBehaviourData entity in data)
            {
                EntityPrefabOverride prefabOverride = entity.GetComponent<EntityPrefabOverride>();
                if (prefabOverride != null)
                {
                    ObjectID entityId = prefabOverride.sourceEntity.Value;
                    if (entityId != ObjectID.None)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal static void ApplyPrefabModAuthorings(EntityMonoBehaviourData entityData, GameObject gameObject)
        {
            var customAuthorings = gameObject.GetComponentsInChildren<ModCDAuthoringBase>();
            foreach (ModCDAuthoringBase customAuthoring in customAuthorings)
            {
                try
                {
                    customAuthoring.Apply(entityData);
                }
                catch (Exception e)
                {
                    CoreLibPlugin.Logger.LogWarning($"Failed to apply {customAuthoring.GetIl2CppType().FullName} on {customAuthoring.name}:\n{e}");
                }
            }
        }
    }
}