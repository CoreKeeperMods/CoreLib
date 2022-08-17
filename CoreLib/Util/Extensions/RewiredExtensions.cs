using System;
using CoreLib.Submodules.RewiredExtension;
using Rewired;
using Rewired.Data.Mapping;

namespace CoreLib.Util;

public static class RewiredExtensions
{
    public static bool AddAction(this ActionCategoryMap map, int categoryId, int actionId)
    {
        if (map.list == null)
        {
            return false;
        }
        int num = map.IndexOfCategory(categoryId);
        if (num < 0)
        {
            return false;
        }
        if (!map.list._items[num].actionIds.Contains(actionId))
        {
            map.list._items[num].actionIds.Add(actionId);
        }
        return true;
    }

    public static bool GetButton(this Player player, string actionName)
    {
        int actionId = RewiredExtensionModule.GetKeybindId(actionName);
        return player.GetButton(actionId);
    }
    
    public static bool GetButtonDown(this Player player, string actionName)
    {
        int actionId = RewiredExtensionModule.GetKeybindId(actionName);
        return player.GetButtonDown(actionId);
    }
    
    public static bool GetButtonUp(this Player player, string actionName)
    {
        int actionId = RewiredExtensionModule.GetKeybindId(actionName);
        return player.GetButtonUp(actionId);
    }
}