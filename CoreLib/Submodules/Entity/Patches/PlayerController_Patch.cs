using System;
using System.Linq;
using CoreLib.Submodules.ModEntity.Interfaces;
using HarmonyLib;
using UnityEngine;

namespace CoreLib.Submodules.ModEntity.Patches
{
    public class PlayerController_Patch
    {
        [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.GetObjectName))]
        [HarmonyPostfix]
        public static void GetObjectName(ObjectDataCD objectData, bool localize, TextAndFormatFields __result)
        {
            IDynamicItemHandler handler = EntityModule.dynamicItemHandlers.FirstOrDefault(handler => handler.ShouldApply(objectData));
            handler?.ApplyText(objectData, __result);
        }
    }
}