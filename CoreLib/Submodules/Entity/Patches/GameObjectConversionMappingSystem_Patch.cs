using HarmonyLib;
using Unity.Entities.Conversion;
using UnityEngine;

namespace CoreLib.Submodules.ModEntity.Patches
{
    public class GameObjectConversionMappingSystem_Patch
    {
        [HarmonyPatch(typeof(GameObjectConversionMappingSystem), nameof(GameObjectConversionMappingSystem.CreateEntitiesForGameObjectsRecurse))]
        [HarmonyPrefix]
        public static void DeclarePrefix(Transform transform, out bool __state)
        {
            __state = false;
            var go = transform.gameObject;
            var hiddenFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            if ((go.hideFlags & hiddenFlags) != 0)
            {
                __state = true;
                go.hideFlags = HideFlags.None;
            }
        }
    
        [HarmonyPatch(typeof(GameObjectConversionMappingSystem), nameof(GameObjectConversionMappingSystem.CreateEntitiesForGameObjectsRecurse))]
        [HarmonyPostfix]
        public static void DeclarePostfix(Transform transform, bool __state)
        {
            var go = transform.gameObject;
            if (__state)
            {
                go.hideFlags = HideFlags.HideAndDontSave;
            }
        }
    }
}