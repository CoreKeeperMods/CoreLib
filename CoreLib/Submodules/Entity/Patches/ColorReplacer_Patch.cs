using System.Linq;
using CoreLib.Submodules.ModEntity.Interfaces;
using HarmonyLib;

namespace CoreLib.Submodules.ModEntity.Patches
{
    public class ColorReplacer_Patch
    {
        [HarmonyPatch(typeof(ColorReplacer), nameof(ColorReplacer.UpdateColorReplacerFromObjectData))]
        [HarmonyPostfix]
        public static void UpdateReplacer(ColorReplacer __instance, ContainedObjectsBuffer containedObject)
        {
            IDynamicItemHandler handler = EntityModule.dynamicItemHandlers.FirstOrDefault(handler => handler.ShouldApply(containedObject.objectData));
            if (handler == null) return;

            bool apply = handler.ApplyColors(containedObject.objectData, __instance.colorReplacementData);
            if (apply)
            {
                __instance.SetActiveColorReplacement(1);
            }
        }
    }
}