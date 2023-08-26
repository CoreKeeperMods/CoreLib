using HarmonyLib;
using Rewired;

namespace CoreLib.Submodules.RewiredExtension.Patches
{
    public static class Rewired_Patch
    {
        [HarmonyPatch(typeof(InputManager_Base), "Start")]
        [HarmonyPostfix]
        public static void OnRewiredStart()
        {
            RewiredExtensionModule.rewiredStart?.Invoke();
        }
    }
}