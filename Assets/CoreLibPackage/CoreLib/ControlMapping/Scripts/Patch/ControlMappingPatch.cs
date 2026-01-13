using System.Collections.Generic;
using CoreLib.Util.Extension;
using HarmonyLib;
using Rewired;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.ControlMapping.Patch
{
    /// Provides a Harmony patch for modifying the "ControlMappingMenu" behavior in the
    /// Rewired system. This patch specifically updates the category layout used in the
    /// control mapping menu by adding a custom "Mods" category with additional sub-sections
    /// based on the configuration provided in the custom categories.
    public class ControlMappingPatch
    {
        /// Handles the post-creation process of category selection for the Control Mapping Menu.
        /// <param name="__instance">
        /// The instance of the ControlMappingMenu for which the category selection is being managed.
        /// </param>
        [HarmonyPatch(typeof(ControlMappingMenu), "Initialize")]
        [HarmonyPrefix]
        public static void OnControlMappingMenuAwake(ControlMappingMenu __instance)
        {
            var layouts = __instance.GetValue<List<ControlMapping_CategoryLayoutData>>("_mappingLayoutData");
            var modLayout = ControlMappingModule.ModCategoryLayout;
            if(modLayout.CategoryLayoutData.Count == 0 || layouts.Contains(modLayout)) return;
            layouts.Add(modLayout);
            __instance.SetValue("_mappingLayoutData", layouts);
        }
        
        /// Executes after the Rewired input system has been initialized. This method invokes the static event
        /// <c>RewiredExtensionModule.rewiredStart</c>, allowing external modules or methods to perform custom logic
        /// upon the completion of Rewired's initialization process.
        [HarmonyPatch(typeof(InputManager_Base), "Start")]
        [HarmonyPostfix]
        public static void OnInputManagerBaseStart() => ControlMappingModule.RewiredStart?.Invoke();
    }
}