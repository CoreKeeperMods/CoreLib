using System.Collections.Generic;
using System.Reflection;
using CoreLib.Extensions;
using CoreLib.Util;
using HarmonyLib;
using Rewired;
using Rewired.Data;
using Rewired.Data.Mapping;

namespace CoreLib.Submodules.RewiredExtension.Patches
{
    public static class Rewired_Init_Patch
    {
        // This searches for a method with void name() signature where name is always obfuscated
        // It initializes user data, and is a good entry point
        public static MethodBase TargetMethod()
        {
            var method = AccessTools.FirstMethod(typeof(UserData), method => method.ReturnType == typeof(void) && method.GetParameters().Length == 0);
            if (method == null)
                return null;

            CoreLibMod.Log.LogDebug($"Rewired patch. Found method: {method.FullDescription()}");
            return method;
        }

        [HarmonyPrefix]
        public static void OnRewiredDataInit(UserData __instance)
        {
            List<string> invalidKeybinds = new List<string>();

            foreach (var pair in RewiredExtensionModule.keyBinds)
            {
                int index = __instance.IndexOfAction(pair.Key);
                if (index != -1)
                {
                    CoreLibMod.Log.LogWarning($"Error trying to add keybind action with name {pair.Key}! This keybind name is already taken!");
                    invalidKeybinds.Add(pair.Key);
                    continue;
                }

                int actionIdCounter = __instance.GetField<int>("actionIdCounter");
                ConfigEntry<int> keyBindId = RewiredExtensionModule.keybindIdCache.Bind(pair.Key, actionIdCounter + 50);

                if (ActionExists(__instance, keyBindId.Value))
                {
                    CoreLibMod.Log.LogWarning($"Found keybind cache id conflict, force rebinding {pair.Key} keybind id!");
                    keyBindId.Value = actionIdCounter + 50;
                }

                InputAction newAction = new InputAction();
                newAction.SetField("_id", keyBindId.Value);
                newAction.SetField("_categoryId", 0);
                newAction.SetField("_name", pair.Key);
                newAction.SetField("_type", InputActionType.Button);
                newAction.SetField("_descriptiveName", pair.Key);
                newAction.SetField("_userAssignable", true);

                __instance.GetField<List<InputAction>>("actions").Add(newAction);
                __instance.GetField<ActionCategoryMap>("actionCategoryMap").AddAction(newAction.categoryId, newAction.id);
                pair.Value.actionId = newAction.id;

                __instance.SetField("actionIdCounter", actionIdCounter + 1);


                var keyboardMaps = __instance.GetField<List<ControllerMap_Editor>>("keyboardMaps");

                foreach (ControllerMap_Editor map in keyboardMaps)
                {
                    if (map.categoryId == newAction.categoryId)
                    {
                        ActionElementMap newElementMap = new ActionElementMap();
                        newElementMap.SetField("_actionId", keyBindId.Value);
                        newElementMap.SetField("_elementType", ControllerElementType.Button);
                        newElementMap.SetField("_actionCategoryId", 0);
                        newElementMap.SetField("_keyboardKeyCode", pair.Value.defaultKeyCode);
                        newElementMap.SetField("_modifierKey1", pair.Value.modifierKey);

                        map.actionElementMaps.Add(newElementMap);
                    }
                }
            }

            foreach (string keybindName in invalidKeybinds)
            {
                RewiredExtensionModule.keyBinds.Remove(keybindName);
            }

            CoreLibMod.Log.LogInfo("Done adding mod keybinds!");
        }

        private static bool ActionExists(UserData userData, int keyBindId)
        {
            var actions = userData.GetField<List<InputAction>>("actions");
            foreach (InputAction action in actions)
            {
                if (action.GetField<int>("_id") == keyBindId) return true;
            }

            return false;
        }
    }
}