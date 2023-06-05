using HarmonyLib;
using Unity.Entities;
#pragma warning disable CS0618

namespace CoreLib.Submodules.ModSystem.Patches
{
    public static class SceneHandler_Patch
    {
        [HarmonyPatch(typeof(SceneHandler), nameof(SceneHandler.SetSceneHandlerReady))]
        [HarmonyPostfix]
        public static void OnSceneStart()
        {
            SystemModule.OnWorldsReady();
        }
    }
}