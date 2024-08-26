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

            var result = __instance.quantumConsolePrefab.LoadAssetAsync<GameObject>();
            var prefab = result.WaitForCompletion();
            
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
                Transform consoleTransform = CommandsModule.quantumConsole.gameObject.transform;
                if (consoleTransform.parent != null &&
                    consoleTransform.parent.name.Contains("UniverseLibCanvas")) return;
                
                consoleTransform.parent = universeLibCanvasGo.transform;
            }
        }

        [HarmonyPatch(typeof(MenuManager), nameof(MenuManager.Update))]
        [HarmonyPostfix]
        public static void OnUpdate(MenuManager __instance)
        {
            if (CommandsModule.rewiredPlayer.GetButtonDown(CommandsModule.TOGGLE_QC))
            {
                CommandsModule.ToggleQC();
            }

            __instance.SetValue("isConsoleActive", CommandsModule.quantumConsole.IsActive);
        }
    }
}