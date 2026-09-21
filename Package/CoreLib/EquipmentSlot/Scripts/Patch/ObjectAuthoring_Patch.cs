using CoreLib.Submodule.EquipmentSlot.Component;
using HarmonyLib;

namespace CoreLib.Submodule.EquipmentSlot.Patch
{
    public static class ObjectAuthoring_Patch
    {
    
        [HarmonyPatch(typeof(ObjectAuthoring), "ObjectAuthoringToObjectInfo")]
        [HarmonyPostfix]
        public static void OnGetObjectInfo(ObjectAuthoring objectAuthoring, ObjectInfo __result)
        {
            var component = objectAuthoring.GetComponent<ModToolSizeAuthoring>();
            if (component != null)
            {
                __result.prefabTileSize = component.prefabTileSize;
            }
        }
    }
}