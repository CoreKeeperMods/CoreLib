using System.Collections.Generic;
using System.Reflection;
using CoreLib.Util;
using HarmonyLib;
using Rewired;
using Rewired.Data;
using Rewired.Data.Mapping;

namespace CoreLib.Submodules.RewiredExtension.Patches;

[HarmonyPatch]
public static class Rewired_Patch
{
    [HarmonyPatch(typeof(InputManager_Base), nameof(InputManager_Base.Start))]
    [HarmonyPostfix]
    public static void OnRewiredStart()
    {
        RewiredExtensionModule.rewiredStart?.Invoke();
    }
}