using CoreLib.Util.Extension;
using HarmonyLib;
using QFSW.QC;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Command.Patch
{
    public class MenuManagerPatch
    {
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MenuManager), nameof(MenuManager.quantumConsole), MethodType.Setter)]
        public static void On_QC_Set(QuantumConsole value)
        {
            CommandModule.InitQuantumConsole(value);
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
    }
}