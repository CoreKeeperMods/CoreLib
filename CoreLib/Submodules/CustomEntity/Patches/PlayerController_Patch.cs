using System;
using System.Linq;
using CoreLib.Submodules.CustomEntity.Interfaces;
using HarmonyLib;
using UnityEngine;

namespace CoreLib.Submodules.CustomEntity.Patches
{
    public class PlayerController_Patch
    {
        [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.GetObjectName))]
        [HarmonyPostfix]
        public static void GetObjectName(ObjectDataCD objectData, bool localize, TextAndFormatFields __result)
        {
            IDynamicItemHandler handler = CustomEntityModule.dynamicItemHandlers.FirstOrDefault(handler => handler.ShouldApply(objectData));
            handler?.ApplyText(objectData, __result);
        }
    }
}