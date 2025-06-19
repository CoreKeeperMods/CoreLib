using System.Linq;
using HarmonyLib;
using PugMod;
using UnityEngine;

namespace CoreLib.Submodules.ModEntity.Patches
{
    [HarmonyPatch]
    public class CraftingBuilding_Patch
    {
        [HarmonyPatch(typeof(CraftingBuilding), "GetCraftingUISettings")]
        [HarmonyPrefix]
        public static bool GetCraftingUISettings(CraftingBuilding __instance, ref CraftingBuilding.CraftingUISettings __result)
        {
            var objectId = __instance.objectData.objectID;
            var workbench = EntityModule.modWorkbenches.FirstOrDefault(definition =>
                API.Authoring.GetObjectID(definition.itemId) == objectId);
            if (workbench is null || workbench.refreshRelatedWorkbenchTitles != true) return true;
            var window = Manager.ui.GetCraftingCategoryWindowInfo();
            var index = Manager.ui.GetCraftingCategoryWindowInfos().FindIndex(win => win == window) - 1;
            __result = index == -1 ? new CraftingBuilding.CraftingUISettings(objectId, __instance.craftingUITitle, __instance.craftingUITitleLeftBox,
                    __instance.craftingUITitleRightBox, __instance.craftingUIBackgroundVariation) : __instance.craftingUIOverrideSettings[index];
            return false;
        }
    }
}
