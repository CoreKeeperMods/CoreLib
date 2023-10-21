using System;
using CoreLib.Submodules.ModEntity.Components;
using UnityEngine;

namespace CoreLib.Util
{
    public static class MonoBehaviourUtils
    {
        internal static bool HasPrefabOverrides(System.Collections.Generic.List<EntityMonoBehaviourData> data)
        {
            foreach (EntityMonoBehaviourData entity in data)
            {
                EntityPrefabOverride prefabOverride = entity.GetComponent<EntityPrefabOverride>();
                if (prefabOverride != null)
                {
                    ObjectID entityId = prefabOverride.sourceEntity;
                    if (entityId != ObjectID.None)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal static void ApplyPrefabModAuthorings(MonoBehaviour entityData, GameObject gameObject)
        {
            var customAuthorings = gameObject.GetComponentsInChildren<ModCDAuthoringBase>(true);
            foreach (ModCDAuthoringBase customAuthoring in customAuthorings)
            {
                try
                {
                    customAuthoring.Apply(entityData);
                }
                catch (Exception e)
                {
                    CoreLibMod.Log.LogWarning($"Failed to apply {customAuthoring.GetType().FullName} on {customAuthoring.name}:\n{e}");
                }
            }
        }
    }
}