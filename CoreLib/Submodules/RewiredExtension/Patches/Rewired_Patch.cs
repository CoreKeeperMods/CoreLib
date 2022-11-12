using System.Collections.Generic;
using CoreLib.Util;
using HarmonyLib;
using Rewired;
using Rewired.Data;
using Rewired.Data.Mapping;

namespace CoreLib.Submodules.RewiredExtension.Patches;

[HarmonyPatch]
public static class Rewired_Patch
{
    // Method named 'zQQfvDZMmpVqPPLYlLuSJXXpwJcI' initializes user data, and is a good entry point
    [HarmonyPatch(typeof(UserData), nameof(UserData.zQQfvDZMmpVqPPLYlLuSJXXpwJcI))]
    [HarmonyPrefix]
    public static void OnRewiredDataInit(UserData __instance)
    {
        List<string> invalidKeybinds = new List<string>();
        

        foreach (var pair in RewiredExtensionModule.keyBinds)
        {
            int index = __instance.IndexOfAction(pair.Key);
            if (index != -1)
            {
                CoreLibPlugin.Logger.LogWarning($"Error trying to add keybind action with name {pair.Key}! This keybind name is already taken!");
                invalidKeybinds.Add(pair.Key);
                continue;
            }

            int keyBindId = RewiredExtensionModule.keybindIdCache.Bind("KeyBinds", pair.Key, __instance.actionIdCounter + 50).Value;
            
            InputAction newAction = new InputAction()
            {
                _id = keyBindId,
                _categoryId = 0,
                _name = pair.Key,
                _type = InputActionType.Button,
                _descriptiveName = pair.Key,
                _userAssignable = true,
            };
        
            __instance.actions.Add(newAction);
            __instance.actionCategoryMap.AddAction(newAction.categoryId, newAction.id);
            pair.Value.actionId = newAction.id;
            
            __instance.actionIdCounter++;


            for (int i = 0; i < __instance.keyboardMaps.Count; i++)
            {
                ControllerMap_Editor map = __instance.keyboardMaps._items[i];
                if (map.categoryId == newAction.categoryId)
                {
                    ActionElementMap newElementMap = new ActionElementMap()
                    {
                        _actionId = newAction._id,
                        _elementType = ControllerElementType.Button,
                        _actionCategoryId = newAction._categoryId,
                        _keyboardKeyCode = pair.Value.defaultKeyCode,
                        _modifierKey1 = pair.Value.modifierKey
                    };
                
                    map.actionElementMaps.Add(newElementMap);
                }
            }
        }

        foreach (string keybindName in invalidKeybinds)
        {
            RewiredExtensionModule.keyBinds.Remove(keybindName);
        }
        CoreLibPlugin.Logger.LogInfo("Done adding mod keybinds!");
    }


    [HarmonyPatch(typeof(InputManager_Base), nameof(InputManager_Base.Start))]
    [HarmonyPostfix]
    public static void OnRewiredStart()
    {
        RewiredExtensionModule.rewiredStart?.Invoke();
    }
}