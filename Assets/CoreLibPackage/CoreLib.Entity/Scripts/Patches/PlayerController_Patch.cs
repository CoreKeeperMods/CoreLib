using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CoreLib.Submodules.ModEntity.Interfaces;
using HarmonyLib;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodules.ModEntity.Patches
{
    [SuppressMessage("Method Declaration", "Harmony003:Harmony non-ref patch parameters modified")]
    // ReSharper disable once InconsistentNaming
    public class PlayerController_Patch
    {
        /// <summary>
        /// Applies custom logic to modify the result of the GetObjectName method of the PlayerController class.
        /// This method checks for applicable dynamic item handlers and applies text modifications based on the provided object data.
        /// </summary>
        /// <param name="containedObject">The buffer containing the object data whose name is being retrieved.</param>
        /// <param name="localize">A flag indicating if the object name should be localized.</param>
        /// <param name="__result">The result object used for storing the modified text and format fields.</param>
        [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.GetObjectName))]
        [HarmonyPostfix]
        public static void GetObjectName(ContainedObjectsBuffer containedObject, bool localize, TextAndFormatFields __result)
        {
            IDynamicItemHandler handler = EntityModule.dynamicItemHandlers.FirstOrDefault(handler => handler.ShouldApply(containedObject.objectData));
            handler?.ApplyText(containedObject.objectData, __result);
        }
    }
}