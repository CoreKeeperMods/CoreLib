using System.Collections.Generic;
using System.Linq;
using CoreLib.Data.Configuration;
using CoreLib.Util.Extensions;
using HarmonyLib;
using Rewired;
using Rewired.Data;
using Rewired.Data.Mapping;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.RewiredExtension.Patches
{
    /// <summary>
    /// Rewired_Init_Patch provides functionality to customize and extend the initialization process of Rewired's input system,
    /// specifically handling the integration and management of custom input categories and actions.
    /// </summary>
    /// <remarks>
    /// Utilizes Harmony to apply patches that enable interception and manipulation of Rewired data during its startup.
    /// This class ensures custom input configurations are properly registered and prevents invalid or potentially obfuscated input categories.
    /// </remarks>
    public static class Rewired_Init_Patch
    {
        /// <summary>
        /// An array of predefined strings representing keywords relevant to the initialization
        /// and customization of input-related components in the Rewired integration.
        /// These keywords are used to identify non-obfuscated names and validate custom input configurations.
        /// </summary>
        private static string[] words =
        {
            "Set",
            "Default",
            "Values",
            "On",
            "Creation",
            "Add",
            "Player",
            "Clear",
            "Mouse",
            "Assignments",
            "Keyboard",
            "Action",
            "Category",
            "Input",
            "Behavior",
            "Map",
            "Joystick",
            "Layout",
            "Custom",
            "Controller",
            "Manager",
            "Rule",
            "Enabler",
        };

        /// Determines if the specified name is considered obfuscated based on predefined criteria.
        /// A name is deemed obfuscated if it does not contain any of the predefined words within the allowed list.
        /// <param name="name">The name string to evaluate for obfuscation.</param>
        /// <return>Returns true if the name is obfuscated, otherwise false.</return>
        internal static bool IsObfuscated(string name)
        {
            return words.All(word => !name.Contains(word));
        }

        /// <summary>
        /// The index representing the last assigned ID for dynamically created input action categories within the Rewired integration.
        /// This value is incremented whenever a new category is added to ensure unique identification.
        /// </summary>
        private static int lastFreeCategoryId = 100;

        /// <summary>
        /// A list of custom input action categories used for managing extended functionality within the Rewired integration.
        /// This collection is populated with additional categories created during runtime.
        /// </summary>
        internal static List<InputActionCategory> customCategories = new List<InputActionCategory>();

        /// Creates a new input action category within the Rewired user data and assigns it a unique ID.
        /// The category is also added to the internal custom categories list for tracking purposes.
        /// <param name="userData">The Rewired user data object to which the category will be added.</param>
        /// <param name="name">The name of the category, used as its identifier, descriptive name, and tag.</param>
        /// <return>Returns the newly created input action category.</return>
        private static InputActionCategory CreateCategory(UserData userData, string name)
        {
            var category = new InputActionCategory();
            category.SetValue("_id", lastFreeCategoryId++);
            category.SetValue("_name", name);
            category.SetValue("_descriptiveName", name);
            category.SetValue("_tag", name);

            userData.GetValue<List<InputActionCategory>>("actionCategories").Add(category);
            userData.GetValue<ActionCategoryMap>("actionCategoryMap").AddCategory(category.id);

            customCategories.Add(category);
            
            return category;
        }

        /// Handles the initialization of Rewired user data by adding custom input action categories
        /// and attempting to integrate mod-defined keybinds while ensuring invalid keybinds are removed.
        /// <param name="__instance">The instance of the Rewired UserData being initialized.</param>
        [HarmonyPatch(typeof(UserData), "yDABbxiARLBWAQcRokAdOcDrDbkT")]
        [HarmonyPrefix]
        public static void OnRewiredDataInit(UserData __instance)
        {
            List<string> invalidKeybinds = new List<string>();
            
            var defaultCategory = CreateCategory(__instance, "ModDefault");

            foreach (var pair in RewiredExtensionModule.keyBinds)
            {
                TryAddKeybind(__instance, pair, invalidKeybinds, defaultCategory);
            }

            foreach (string keybindName in invalidKeybinds)
            {
                RewiredExtensionModule.keyBinds.Remove(keybindName);
            }

            CoreLibMod.Log.LogInfo("Done adding mod keybinds!");
        }

        /// Attempts to add a new keybind action to the provided user data, while handling potential conflicts and invalid data.
        /// <param name="userData">The user data object where the keybind action will be added.</param>
        /// <param name="pair">A key-value pair containing the keybind name and associated keybind data.</param>
        /// <param name="invalidKeybinds">A list to store the names of keybinds that could not be added due to conflicts or errors.</param>
        /// <param name="category">The input action category to which the new keybind action belongs.</param>
        private static void TryAddKeybind(
            UserData userData, 
            KeyValuePair<string, KeyBindData> pair, 
            List<string> invalidKeybinds,
            InputActionCategory category
            )
        {
            int index = userData.IndexOfAction(pair.Key);
            if (index != -1)
            {
                CoreLibMod.Log.LogWarning($"Error trying to add keybind action with name {pair.Key}! This keybind name is already taken!");
                invalidKeybinds.Add(pair.Key);
                return;
            }

            int actionIdCounter = userData.GetValue<int>("actionIdCounter");
            ConfigEntry<int> keyBindId = RewiredExtensionModule.keybindIdCache.Bind("KeyBinds", pair.Key, actionIdCounter + 50);

            if (ActionExists(userData, keyBindId.Value))
            {
                CoreLibMod.Log.LogWarning($"Found keybind cache id conflict, force rebinding {pair.Key} keybind id!");
                keyBindId.Value = actionIdCounter + 50;
            }

            InputAction newAction = new InputAction();
            newAction.SetValue("_id", keyBindId.Value);
            newAction.SetValue("_categoryId", category.id);
            newAction.SetValue("_name", pair.Key);
            newAction.SetValue("_type", InputActionType.Button);
            newAction.SetValue("_descriptiveName", pair.Key);
            newAction.SetValue("_userAssignable", true);

            userData.GetValue<List<InputAction>>("actions").Add(newAction);
            userData.GetValue<ActionCategoryMap>("actionCategoryMap").AddAction(newAction.categoryId, newAction.id);
            pair.Value.actionId = newAction.id;

            userData.SetValue("actionIdCounter", actionIdCounter + 1);


            var keyboardMaps = userData.GetValue<List<ControllerMap_Editor>>("keyboardMaps");

            foreach (ControllerMap_Editor map in keyboardMaps)
            {
                if (map.categoryId == newAction.categoryId)
                {
                    ActionElementMap newElementMap = new ActionElementMap();
                    newElementMap.SetValue("_actionId", keyBindId.Value);
                    newElementMap.SetValue("_elementType", ControllerElementType.Button);
                    newElementMap.SetValue("_actionCategoryId", category.id);
                    newElementMap.SetValue("_keyboardKeyCode", pair.Value.defaultKeyCode);
                    newElementMap.SetValue("_modifierKey1", pair.Value.modifierKey);

                    map.actionElementMaps.Add(newElementMap);
                }
            }
            
            if (pair.Value.gamepadElementId == 0) return;

            var joystickMaps = userData.GetValue<List<ControllerMap_Editor>>("joystickMaps");
            foreach (ControllerMap_Editor map in joystickMaps)
            {
                if (map.categoryId == newAction.categoryId)
                {
                    ActionElementMap newElementMap = new ActionElementMap();
                    newElementMap.SetValue("_actionId", keyBindId.Value);
                    newElementMap.SetValue("_elementType", pair.Value.gamepadElementType);
                    newElementMap.SetValue("_axisRange", pair.Value.gamepadAxisRange);
                    newElementMap.SetValue("_invert", pair.Value.gamepadInvert);
                    newElementMap.SetValue("_actionCategoryId", category.id);
                    newElementMap.SetValue("_elementIdentifierId", pair.Value.gamepadElementId);

                    map.actionElementMaps.Add(newElementMap);
                }
            }
        }

        /// Determines whether an action with the specified keyBindId exists in the given user data.
        /// <param name="userData">The user data containing a list of input actions.</param>
        /// <param name="keyBindId">The ID of the keybind to check for existence.</param>
        /// <return>True if an action with the specified keyBindId exists; otherwise, false.</return>
        private static bool ActionExists(UserData userData, int keyBindId)
        {
            var actions = userData.GetValue<List<InputAction>>("actions");
            foreach (InputAction action in actions)
            {
                if (action.GetValue<int>("_id") == keyBindId) return true;
            }

            return false;
        }
    }
}