using System;
using CoreLib.Components;
using UnityEngine;

namespace CoreLib.Util
{
    public static class MonoBehaviourUtils
    {
        public static void ApplyPrefabModAuthorings(MonoBehaviour entityData, GameObject gameObject)
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
                    Logger.LogWarning($"Failed to apply {customAuthoring.GetType().FullName} on {customAuthoring.name}:\n{e}");
                }
            }
        }
    }
}