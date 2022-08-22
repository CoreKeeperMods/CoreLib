using HarmonyLib;
using UnityEngine.SceneManagement;

namespace CoreLib.Submodules.CustomEntity.Patches;

public static class Loading_Patch
{
   /* private static int lastState = -1;

    [HarmonyPatch(typeof(LoadingScene._ProgressiveLoader_d__13), nameof(LoadingScene._ProgressiveLoader_d__13.MoveNext))]
    [HarmonyPrefix]
    private static void OnItterator(LoadingScene._ProgressiveLoader_d__13 __instance)
    {
        if (__instance.__1__state != lastState)
        {
            CoreLibPlugin.Logger.LogInfo($"Loading Scene Itterator enter state {__instance.__1__state}!");
            lastState = __instance.__1__state;
        }
    }*/

    [HarmonyPatch(typeof(LoadManager), nameof(LoadManager.QueueScene))]
    [HarmonyPrefix]
    private static void LoadScene(LoadManager __instance, string sceneName)
    {
        CoreLibPlugin.Logger.LogInfo($"Queue scene {sceneName} load!");
    }
    
    [HarmonyPatch(typeof(TitleScreenAnimator), nameof(TitleScreenAnimator.Start))]
    [HarmonyPrefix]
    private static void TitleAnimatorStart(TitleScreenAnimator __instance)
    {
        CoreLibPlugin.Logger.LogInfo($"Title animator start!");
    }
    
    [HarmonyPatch(typeof(SceneHandler), nameof(SceneHandler.Awake))]
    [HarmonyPrefix]
    private static void SceneHandlerAwake(SceneHandler __instance)
    {
        CoreLibPlugin.Logger.LogInfo($"SceneHandler awake! Scene type {__instance.sceneHandlerType.ToString()}");
    }
    
    [HarmonyPatch(typeof(SceneHandler), nameof(SceneHandler.Start))]
    [HarmonyPrefix]
    private static void SceneHandlerStart(SceneHandler __instance)
    {
        CoreLibPlugin.Logger.LogInfo($"SceneHandler start! Scene type {__instance.sceneHandlerType.ToString()}");
    }
}