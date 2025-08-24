using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CoreLib.Submodule.Entity.Interfaces;
using HarmonyLib;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Entity.Patches
{
    /// <summary>
    /// Represents a Harmony patch for the <see cref="ColorReplacer"/> class, responsible for updating color replacement
    /// behavior based on object data. This patch integrates dynamic item handlers that determine whether
    /// specified color changes should be applied.
    /// </summary>
    [SuppressMessage("Method Declaration", "Harmony003:Harmony non-ref patch parameters modified")]
    // ReSharper disable once InconsistentNaming
    public class ColorReplacer_Patch
    {
        /// <summary>
        /// Updates the <see cref="ColorReplacer"/> instance based on the provided object's data.
        /// Uses dynamic item handlers to determine if colors should be applied for the current object data.
        /// If a handler is applicable and successfully applies the colors, an active color replacement is set.
        /// </summary>
        /// <param name="__instance">The instance of <see cref="ColorReplacer"/> being updated.</param>
        /// <param name="containedObject">The object data encapsulated in a <see cref="ContainedObjectsBuffer"/> used to determine color changes.</param>
        [HarmonyPatch(typeof(ColorReplacer), nameof(ColorReplacer.UpdateColorReplacerFromObjectData))]
        [HarmonyPostfix]
        public static void UpdateReplacer(ColorReplacer __instance, ContainedObjectsBuffer containedObject)
        {
            IDynamicItemHandler handler = EntityModule.DynamicItemHandlers.FirstOrDefault(handler => handler.ShouldApply(containedObject.objectData));
            if (handler == null) return;

            bool apply = handler.ApplyColors(containedObject.objectData, __instance.colorReplacementData);
            if (apply)
            {
                __instance.SetActiveColorReplacement(1);
            }
        }
    }
}