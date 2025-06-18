using HarmonyLib;
using PugMod;

namespace CoreLib.Submodules.ModEntity.Patches
{
    [HarmonyPatch]
    public class CraftingBuilding_Patch
    {
        [HarmonyPatch(typeof(CraftingBuilding), "GetCraftingUISettings")]
        [HarmonyPrefix]
        public static bool GetCraftingUISettings(CraftingBuilding __instance, CraftingBuilding.CraftingUISettings  __result)
        {
            var objectId = __instance.objectData.objectID;
            if (EntityModule.modWorkbenches.Find(x => API.Authoring.GetObjectID(x.itemId) == objectId) is null) return true;
            var window = Manager.ui.GetCraftingCategoryWindowInfo();
            var index = Manager.ui.GetCraftingCategoryWindowInfos().FindIndex(win => win == window) - 1;
            __result = index >= 0 ? __instance.craftingUIOverrideSettings[index] : 
                new CraftingBuilding.CraftingUISettings(objectId, __instance.craftingUITitle, __instance.craftingUITitleLeftBox,
                    __instance.craftingUITitleRightBox, __instance.craftingUIBackgroundVariation);
            return false;
        }
    }
}
