using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CoreLib.Submodules.Entity.Interfaces;
using HarmonyLib;
using UnityEngine;

namespace CoreLib.Submodules.Entity.Patches
{
    [SuppressMessage("Method Declaration", "Harmony003:Harmony non-ref patch parameters modified")]
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