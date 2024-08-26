using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CoreLib.Data;
using CoreLib.Data.Configuration;
using CoreLib.Util.Extensions;
using HarmonyLib;
using Rewired;
using Rewired.Data;
using Rewired.Data.Mapping;

namespace CoreLib.RewiredExtension.Patches
{
    public static class Rewired_Init_Patch
    {
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
        
        internal static bool IsObfuscated(string name)
        {
            return words.All(word => !name.Contains(word));
        }
        
        [HarmonyPatch(typeof(UserData), "wLpCiqgeHDZMiGZKbMZMEEzhQahoA")]
        [HarmonyPrefix]
        public static void OnRewiredDataInit(UserData __instance)
        {
            List<string> invalidKeybinds = new List<string>();

            foreach (var pair in RewiredExtensionModule.keyBinds)
            {
                TryAddKeybind(__instance, pair, invalidKeybinds);
            }

            foreach (string keybindName in invalidKeybinds)
            {
                RewiredExtensionModule.keyBinds.Remove(keybindName);
            }

            CoreLibMod.Log.LogInfo("Done adding mod keybinds!");
        }

        private static void TryAddKeybind(UserData userData, KeyValuePair<string, KeyBindData> pair, List<string> invalidKeybinds)
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
            newAction.SetValue("_categoryId", 0);
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
                    newElementMap.SetValue("_actionCategoryId", 0);
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
                    newElementMap.SetValue("_actionCategoryId", 0);
                    newElementMap.SetValue("_elementIdentifierId", pair.Value.gamepadElementId);

                    map.actionElementMaps.Add(newElementMap);
                }
            }
        }

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