using System.Collections.Generic;
using CoreLib.Util.Extensions;
using HarmonyLib;
using I2.Loc;
using Rewired.UI.ControlMapper;
using UnityEngine;

namespace CoreLib.RewiredExtension.Patches
{
    /// <summary>
    /// Provides a Harmony patch for modifying the "ControlMappingMenu" behavior in the
    /// Rewired system. This patch specifically updates the category layout used in the
    /// control mapping menu by adding a custom "Mods" category with additional sub-sections
    /// based on the configuration provided in the custom categories.
    /// </summary>
    public class ControlMappingMenu_Patch
    {
        /// Handles the post-creation process of category selection for the Control Mapping Menu.
        /// <param name="__instance">
        /// The instance of the ControlMappingMenu for which the category selection is being managed.
        /// </param>
        [HarmonyPatch(typeof(ControlMappingMenu), "CreateCategorySelection")]
        [HarmonyPostfix]
        public static void OnCreateCategorySelection(ControlMappingMenu __instance)
        {
            var layouts = __instance.GetValue<List<ControlMapping_CategoryLayoutData>>("_mappingLayoutData");
            var modLayout = ScriptableObject.CreateInstance<ControlMapping_CategoryLayoutData>();
            modLayout.SetValue("_categoryName", (LocalizedString)"ControlMapper/ModsCategory");

            var sections = new List<CategoryLayoutData>();
            
            foreach (var category in Rewired_Init_Patch.customCategories)
            {
                var layout = new CategoryLayoutData();
                layout.SetValue("_showActionCategoryName", new[] { true });
                layout.SetValue("_showActionCategoryDescription", new[] { category.name != "ModDefault" });

                var mappingSet = new ControlMapper.MappingSet();
                mappingSet.SetValue("_mapCategoryId", category.id);

                layout.SetValue("MappingSet", mappingSet);
                sections.Add(layout);
            }
            
            modLayout.SetValue("_categoryLayoutData", sections);
            layouts.Add(modLayout);
        }
    }
}