using CoreLib.Submodules.ModEntity.Components;
using HarmonyLib;

namespace CoreLib.Submodules.ModEntity.Patches
{
    public static class SimpleCraftingBuilding_Patch
    {
        [HarmonyPatch(typeof(CraftingBuilding), nameof(CraftingBuilding.OnOccupied))]
        [HarmonyPrefix]
        public static void OnOccupied(CraftingBuilding __instance)
        {
            var modSkins = __instance.GetComponent<ModWorkbenchSkins>();
            if (modSkins != null)
            {
                modSkins.Apply();
            }
        }
    }
}