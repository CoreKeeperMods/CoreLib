using HarmonyLib;
using Rewired;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.RewiredExtension.Patches
{
    /// <summary>
    /// Provides a Harmony patch for the Rewired input system's initialization process. This class contains a method
    /// that executes after Rewired's input manager has started, enabling additional functionality by invoking custom
    /// events or logic.
    /// </summary>
    public static class Rewired_Patch
    {
        /// <summary>
        /// Executes after the Rewired input system has been initialized. This method invokes the static event
        /// <c>RewiredExtensionModule.rewiredStart</c>, allowing external modules or methods to perform custom logic
        /// upon the completion of Rewired's initialization process.
        /// </summary>
        [HarmonyPatch(typeof(InputManager_Base), "Start")]
        [HarmonyPostfix]
        public static void OnRewiredStart()
        {
            RewiredExtensionModule.rewiredStart?.Invoke();
        }
    }
}