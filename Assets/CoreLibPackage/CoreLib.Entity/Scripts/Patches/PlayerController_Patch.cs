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
        [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.GetObjectName))]
        [HarmonyPostfix]
        public static void GetObjectName(ContainedObjectsBuffer containedObject, bool localize, TextAndFormatFields __result)
        {
            IDynamicItemHandler handler = EntityModule.dynamicItemHandlers.FirstOrDefault(handler => handler.ShouldApply(containedObject.objectData));
            handler?.ApplyText(containedObject.objectData, __result);
        }
    }
}