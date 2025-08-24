using System;
using CoreLib.Submodule.Entity.Components;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Util
{
    /// <summary>
    /// A utility class that provides methods for working with MonoBehaviour objects
    /// in the context of prefab modification and management.
    /// </summary>
    public static class MonoBehaviourUtils
    {
        /// <summary>
        /// Determines whether any entity in the given list of EntityMonoBehaviourData
        /// includes a valid prefab override. This is checked via the presence of an
        /// EntityPrefabOverride component with a valid source entity.
        /// </summary>
        /// <param name="data">A list of EntityMonoBehaviourData objects to inspect for prefab overrides.</param>
        /// <returns>True if any entity in the list has a valid prefab override; otherwise, false.</returns>
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

        /// <summary>
        /// Applies all custom prefab modifications provided via ModCDAuthoringBase components
        /// to a given MonoBehaviour and GameObject. This enables dynamic customization or initialization
        /// of prefabs and their associated entities at runtime using user-defined authoring scripts.
        /// </summary>
        /// <param name="entityData">A MonoBehaviour representing entity-specific data to apply custom modifications to.</param>
        /// <param name="gameObject">The GameObject containing ModCDAuthoringBase components to execute the modifications.</param>
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