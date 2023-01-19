using System.Linq;
using CoreLib.Submodules.CustomEntity.Interfaces;
using HarmonyLib;

namespace CoreLib.Submodules.CustomEntity.Patches
{
    public class ColorReplacer_Patch
    {
        [HarmonyPatch(typeof(ColorReplacer), nameof(ColorReplacer.UpdateColorReplacerFromObjectData))]
        [HarmonyPostfix]
        public static void UpdateReplacer(ColorReplacer __instance, ObjectDataCD objectData)
        {
            IDynamicItemHandler handler = CustomEntityModule.dynamicItemHandlers.FirstOrDefault(handler => handler.ShouldApply(objectData));
            if (handler == null) return;

            bool apply = handler.ApplyColors(objectData, __instance.colorReplacementData);
            if (apply)
            {
                __instance.ResetTextureColors();
                __instance.SetActiveColorReplacement(1);
            }
        }
    }
}