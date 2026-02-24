using CoreLib.Util.Extension;
using HarmonyLib;
using QFSW.QC;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Command.Patch
{
    public class MenuManagerPatch
    {
        /// Called after the MenuManager's Init method is executed. Responsible for initializing
        /// the Quantum Console linked to the MenuManager if a Quantum Console prefab exists.
        /// This ensures the console is properly instantiated and persistent across scenes.
        /// <param name="__instance">The instance of the MenuManager being patched.</param>
        [HarmonyPatch(typeof(MenuManager), nameof(MenuManager.Init))]
        [HarmonyPostfix]
        // ReSharper disable once InconsistentNaming
        public static void OnInit(MenuManager __instance)
        {
            if (__instance.quantumConsolePrefab == null) return;

            var result = __instance.quantumConsolePrefab.LoadAssetAsync<GameObject>();
            var prefab = result.WaitForCompletion();
            
            var console = Object.Instantiate(prefab, null, true);
            
            var quantumConsole = console.GetComponent<QuantumConsole>();
            Object.DontDestroyOnLoad(console);
            
            CommandModule.InitQuantumConsole(quantumConsole);
        }

        /// Ensures that the Quantum Console, if available, is properly parented to the
        /// UniverseLibCanvas to maintain consistency in the scene hierarchy. This method
        /// processes automatically after the SceneHandler's Awake method is executed.
        /// <param name="__instance">The instance of the SceneHandler being patched.</param>
        [HarmonyPatch(typeof(SceneHandler), "Awake")]
        [HarmonyPostfix]
        // ReSharper disable once InconsistentNaming
        public static void OnAwake(SceneHandler __instance)
        {
            if (CommandModule.quantumConsole == null) return;
            
            CommandModule.log.LogInfo("Looking for Unity Explorer canvas!");
            var universeLibCanvasGo = GameObject.Find("UniverseLibCanvas");
            if (universeLibCanvasGo != null)
            {
                Transform consoleTransform = CommandModule.quantumConsole.gameObject.transform;
                if (consoleTransform.parent != null &&
                    consoleTransform.parent.name.Contains("UniverseLibCanvas")) return;
                
                consoleTransform.parent = universeLibCanvasGo.transform;
            }
        }

        /// Called during the Update lifecycle method of the MenuManager. Handles toggling the Quantum Console
        /// and updates the console's active state status within the MenuManager.
        /// <param name="__instance">The instance of the MenuManager being updated.</param>
        [HarmonyPatch(typeof(MenuManager), nameof(MenuManager.Update))]
        [HarmonyPostfix]
        // ReSharper disable once InconsistentNaming
        public static void OnUpdate(MenuManager __instance)
        {
            if (CommandModule.rewiredPlayer.GetButtonDown(CommandModule.TOGGLE_QUANTUM_CONSOLE))
            {
                CommandModule.ToggleQc();
            }

            __instance.SetValue("isConsoleActive", CommandModule.quantumConsole.IsActive);
        }
    }
}