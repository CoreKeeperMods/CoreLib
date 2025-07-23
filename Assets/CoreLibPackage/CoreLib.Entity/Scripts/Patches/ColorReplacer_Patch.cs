using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CoreLib.Submodules.ModEntity.Interfaces;
using HarmonyLib;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodules.ModEntity.Patches
{
    [SuppressMessage("Method Declaration", "Harmony003:Harmony non-ref patch parameters modified")]
    // ReSharper disable once InconsistentNaming
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