using System;
using System.Collections.Generic;
using CoreLib.Util.Extension;
using Rewired.Data;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.ControlMapping.Extension
{
    /// <summary>
    /// Provides extension methods for working with the Rewired library.
    /// </summary>
    public static class ControlMappingExtensions
    {
        internal static int[] AddNewCategory(this UserData userData, string categoryName, bool userAssignable = true)
        {
            int categoryID = userData.GetActionCategoryId(categoryName);
            int mapID = userData.GetMapCategoryId(categoryName);
            if (categoryID > -1) return new[] { categoryID, mapID };
            userData.AddActionCategory();
            int lastCatIndex = userData.GetActionCategoryIds().Length - 1;
            var modActionCategory = userData.GetActionCategory(lastCatIndex);
            modActionCategory.SetValue("_name", categoryName);
            modActionCategory.SetValue("_descriptiveName", $"{categoryName}Description");
            modActionCategory.SetValue("_tag", "player");
            modActionCategory.SetValue("_userAssignable", userAssignable);
            userData.AddMapCategory();
            int lastIndex = userData.GetMapCategoryIds().Length - 1;
            var modMapCategory = userData.GetMapCategory(lastIndex);
            modMapCategory.SetValue("_name", categoryName);
            modMapCategory.SetValue("_descriptiveName", $"{categoryName}Description");
            modMapCategory.SetValue("_tag", "gameplay");
            modMapCategory.SetValue("_userAssignable", userAssignable);
            modMapCategory.SetValue("_checkConflictsWithAllCategories", false);
            modMapCategory.SetValue("_checkConflictsCategoryIds", new List<int> { 0, modMapCategory.id });
            userData.CreateKeyboardMap(modMapCategory.id, 0);
            userData.CreateMouseMap(modMapCategory.id, 0);
            userData.CreateJoystickMap(modMapCategory.id, new Guid("83b427e4-086f-47f3-bb06-be266abd1ca5"), 0);
            var player = userData.GetPlayer(1);
            player.defaultKeyboardMaps.Add(new Player_Editor.Mapping(true, modMapCategory.id, 0));
            player.defaultMouseMaps.Add(new Player_Editor.Mapping(true, modMapCategory.id, 0));
            player.defaultJoystickMaps.Add(new Player_Editor.Mapping(true, modMapCategory.id, 0));
            ControlMappingModule.Log.LogInfo($"Added new category {categoryName}");
            return new[] { modActionCategory.id, modMapCategory.id };
        }
        
        internal static int[] InsertNewCategory(this UserData userData, int[] index, string categoryName, bool userAssignable = true)
        {
            int categoryID = userData.GetActionCategoryId(categoryName);
            int mapID = userData.GetMapCategoryId(categoryName);
            if (new[] { categoryID, mapID } == index) return new[] { categoryID, mapID };
            userData.AddActionCategory();
            int lastCatIndex = userData.GetActionCategoryIds().Length - 1;
            var modActionCategory = userData.GetActionCategory(lastCatIndex);
            modActionCategory.SetValue("_id", index[0]);
            modActionCategory.SetValue("_name", categoryName);
            modActionCategory.SetValue("_descriptiveName", $"{categoryName}Description");
            modActionCategory.SetValue("_tag", "player");
            modActionCategory.SetValue("_userAssignable", userAssignable);
            userData.AddMapCategory();
            int lastIndex = userData.GetMapCategoryIds().Length - 1;
            var modMapCategory = userData.GetMapCategory(lastIndex);
            modMapCategory.SetValue("_id", index[1]);
            modMapCategory.SetValue("_name", categoryName);
            modMapCategory.SetValue("_descriptiveName", $"{categoryName}Description");
            modMapCategory.SetValue("_tag", "gameplay");
            modMapCategory.SetValue("_userAssignable", userAssignable);
            modMapCategory.SetValue("_checkConflictsWithAllCategories", false);
            modMapCategory.SetValue("_checkConflictsCategoryIds", new List<int> { 0, modMapCategory.id });
            userData.CreateKeyboardMap(modMapCategory.id, 0);
            userData.CreateMouseMap(modMapCategory.id, 0);
            userData.CreateJoystickMap(modMapCategory.id, new Guid("83b427e4-086f-47f3-bb06-be266abd1ca5"), 0);
            var player = userData.GetPlayer(1);
            player.defaultKeyboardMaps.Add(new Player_Editor.Mapping(true, modMapCategory.id, 0));
            player.defaultMouseMaps.Add(new Player_Editor.Mapping(true, modMapCategory.id, 0));
            player.defaultJoystickMaps.Add(new Player_Editor.Mapping(true, modMapCategory.id, 0));
            ControlMappingModule.Log.LogInfo($"Inserted new category {categoryName}");
            return new[] { modActionCategory.id, modMapCategory.id };
        }

        internal static int AddNewAction(this UserData userData, string actionName, int categoryId, bool userAssignable = true)
        {
            int actionID = userData.GetActionId(actionName);
            if(actionID > -1) return actionID;
            userData.AddAction(categoryId);
            int lastActionIndex = userData.GetActionIds().Length - 1;
            var modAction = userData.GetAction(lastActionIndex);
            modAction.SetValue("_name", actionName);
            modAction.SetValue("_descriptiveName", $"{actionName}");
            modAction.SetValue("_userAssignable", userAssignable);
            ControlMappingModule.Log.LogInfo($"Added new action {actionName}");
            return modAction.id;
        }
        internal static int InsertNewAction(this UserData userData, int id, string actionName, int categoryId, bool userAssignable = true)
        {
            int actionID = userData.GetActionId(actionName);
            if(actionID > -1) return actionID;
            userData.AddAction(categoryId);
            int lastActionIndex = userData.GetActionIds().Length - 1;
            var modAction = userData.GetAction(lastActionIndex);
            modAction.SetValue("_id", id);
            modAction.SetValue("_name", actionName);
            modAction.SetValue("_descriptiveName", $"{actionName}");
            modAction.SetValue("_userAssignable", userAssignable);
            ControlMappingModule.Log.LogInfo($"Inserted new action {actionName}");
            return modAction.id;
        }
    }
}