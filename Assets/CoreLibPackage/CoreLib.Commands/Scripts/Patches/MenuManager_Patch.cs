using CoreLib.Util.Extensions;
using HarmonyLib;
using QFSW.QC;
using UnityEngine;

namespace CoreLib.Commands.Patches
{
    public class MenuManager_Patch
    {
        [HarmonyPatch(typeof(MenuManager), nameof(MenuManager.Init))]
        [HarmonyPostfix]
        public static void OnInit(MenuManager __instance)
        {
            if (__instance.quantumConsolePrefab == null) return;
            
            var prefab = __instance.quantumConsolePrefab.gameObject;
            var console = Object.Instantiate(prefab, null, true);
            
            var quantumConsole = console.GetComponent<QuantumConsole>();
            Object.DontDestroyOnLoad(console);
            
            CommandsModule.InitQuantumConsole(quantumConsole);
        }

        [HarmonyPatch(typeof(SceneHandler), "Awake")]
        [HarmonyPostfix]
        public static void OnAwake(SceneHandler __instance)
        {
            if (CommandsModule.quantumConsole == null) return;
            
            CoreLibMod.Log.LogInfo("Looking for Unity Explorer canvas!");
            var universeLibCanvasGo = GameObject.Find("UniverseLibCanvas");
            if (universeLibCanvasGo != null)
            {
                CoreLibMod.Log.LogInfo("Success!");
                CommandsModule.quantumConsole.gameObject.transform.parent = universeLibCanvasGo.transform;
            }
        }

        [HarmonyPatch(typeof(MenuManager), nameof(MenuManager.Update))]
        [HarmonyPostfix]
        public static void OnUpdate(MenuManager __instance)
        {
            if (Input.GetKeyDown(KeyCode.Delete))
            {
                CommandsModule.ToggleQC();
            }

            __instance.SetValue("isConsoleActive", CommandsModule.quantumConsole.IsActive);
        }
    }
}