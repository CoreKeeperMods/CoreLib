using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreLib.Data.Configuration;
using CoreLib.Submodule.ControlMapping.Extension;
using CoreLib.Submodule.ControlMapping.Patch;
using CoreLib.Util.Extension;
using Newtonsoft.Json;
using PugMod;
using Rewired;
using Rewired.Data;
using Rewired.UI.ControlMapper;
using UnityEngine;
using Logger = CoreLib.Util.Logger;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.ControlMapping
{
    /// This module provides the means to add custom Rewired Key binds.
    public class ControlMappingModule : BaseSubmodule
    {
        #region Fields
        
        public new const string Name = "Core Library - Rewired Extension";
        
        internal new static Logger Log = new(Name);
        
        internal static ConfigFile Config => CoreLibMod.Config;
        
        internal static string CategoriesFilePath => "CoreLib\\KeyBindsCategories.json";
        internal static string ActionsFilePath => "CoreLib\\KeyBindsActions.json";
        
        internal static InputManager_Base InputManagerBase = Resources.Load<InputManager_Base>($"Rewired Input Manager");
        
        internal static UserData UserData => InputManagerBase.userData;
        
        internal static ControlMapping_CategoryLayoutData ModCategoryLayout;
        
        internal static int[] ModsActionCategoryID;
        
        internal static Dictionary<string, int[]> KeyBindCategories; //[Category Name, [Category ID, Map Category ID]]
        internal static Dictionary<string, int[]> KeyBindActions; //[Action Name, [Action ID, Action Category ID]]
        
        internal static int ActionCategoryIdCounter => 100;
        internal static int MapCategoryIdCounter => 100;
        internal static int ActionIdCounter => 1000;
        internal static int MapJoystickIdCounter => 1000;
        internal static int MapKeyboardIdCounter => 1000;
        internal static int MapMouseIdCounter => 1000;
        
        /// Event that triggers a callback when the Rewired input system is fully initialized.
        public static Action RewiredStart;
        
        #endregion
        
        #region Public Interface
        
        /// Add a new rebindable keybind
        /// <param name="keyBindName">UNIQUE key bind name</param>
        /// <param name="defaultKeyCode">Default key bind KeyCode</param>
        /// <param name="modifier">Key bind modifier. Defaults to none</param>
        /// <param name="modifier2"></param>
        /// <param name="modifier3"></param>
        /// <param name="categoryId">Category ID to add KeyBind</param>
        public static void AddKeyboardBind(string keyBindName = "", KeyboardKeyCode defaultKeyCode = KeyboardKeyCode.None,
            ModifierKey modifier = ModifierKey.None, ModifierKey modifier2 = ModifierKey.None,
            ModifierKey modifier3 = ModifierKey.None, int categoryId = -1)
        {
            Instance.ThrowIfNotLoaded();
            if (string.IsNullOrEmpty(keyBindName))
            {
                Log.LogWarning($"No Name provided for keybind!");
                return;
            }
            
            if (categoryId == -1)
            {
                AddNewCategory_Internal("Mods");
                categoryId = ModsActionCategoryID[0];
            }
            
            var action = AddNewAction_Internal(keyBindName, categoryId);
            
            if (action.id >= ActionIdCounter && KeyBindActions.TryAdd(keyBindName, new []{action.id, categoryId})) 
                API.ConfigFilesystem.Write(ActionsFilePath, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(KeyBindActions)));
            
            int catInt = UserData.IndexOfActionCategory(categoryId);
            var actionCategory = UserData.GetActionCategory(catInt);
            var mapCategory = UserData.GetMapCategory(actionCategory.name);
            var keyboardMap = UserData.GetKeyboardMap(mapCategory.id, 0);
            keyboardMap.AddNewActionElementMap(categoryId, action.id, ControllerElementType.Button, keyCode: defaultKeyCode, modifierKey1: modifier,
            modifierKey2: modifier2, modifierKey3: modifier3);
        }

        public static void AddMouseBind(string keyBindName = "", int elementId = -1,
            int categoryId = -1)
        {
            Instance.ThrowIfNotLoaded();
            if (string.IsNullOrEmpty(keyBindName))
            {
                Log.LogWarning($"No Name provided for keybind!");
                return;
            }
            
            if (categoryId == -1)
            {
                AddNewCategory_Internal("Mods");
                categoryId = ModsActionCategoryID[0];
            }
            
            var action = AddNewAction_Internal(keyBindName, categoryId);
            
            if (action.id >= ActionIdCounter && KeyBindActions.TryAdd(keyBindName, new []{action.id, categoryId})) 
                API.ConfigFilesystem.Write(ActionsFilePath, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(KeyBindActions)));
            
            int catInt = UserData.IndexOfActionCategory(categoryId);
            var actionCategory = UserData.GetActionCategory(catInt);
            var mapCategory = UserData.GetMapCategory(actionCategory.name);
            var mouseMap = UserData.GetMouseMap(mapCategory.id, 0);
            mouseMap.AddNewActionElementMap(action.categoryId, action.id, ControllerElementType.Button, elementId);
        }

        /// Sets the default controller binding for an existing custom keybind. The keybind must be created beforehand using AddKeybind().
        /// <param name="keyBindName">The name of the existing keybind to set the default controller binding.</param>
        /// <param name="elementId">The element ID of the controller component to bind.</param>
        /// <param name="elementType">The type of the controller element (e.g., Button or Axis). Defaults to Button.</param>
        /// <param name="axisRange">The range of the axis, if applicable. Defaults to Full.</param>
        /// <param name="inverted"></param>
        /// <param name="axisContribution"></param>
        /// <param name="categoryId"></param>
        public static void AddControllerBind(string keyBindName = "", int elementId = -1,
            ControllerElementType elementType = ControllerElementType.Button,
            AxisRange axisRange = AxisRange.Full, bool inverted = false, Pole axisContribution = Pole.Positive, int categoryId = -1)
        {
            Instance.ThrowIfNotLoaded();
            if (string.IsNullOrEmpty(keyBindName))
            {
                Log.LogWarning($"No Name provided for keybind!");
                return;
            }

            if (categoryId == -1)
            {
                AddNewCategory_Internal("Mods");
                categoryId = ModsActionCategoryID[0];
            }
                

            var action = AddNewAction_Internal(keyBindName, categoryId);
            
            if (action.id >= ActionIdCounter && KeyBindActions.TryAdd(keyBindName, new []{action.id, categoryId})) 
                API.ConfigFilesystem.Write(ActionsFilePath, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(KeyBindActions)));
            
            int catInt = UserData.IndexOfActionCategory(categoryId);
            var actionCategory = UserData.GetActionCategory(catInt);
            var mapCategory = UserData.GetMapCategory(actionCategory.name);
            var joystickMap = UserData.GetJoystickMap(mapCategory.id, new Guid("83b427e4-086f-47f3-bb06-be266abd1ca5"), 0);
            joystickMap.AddNewActionElementMap(action.categoryId, action.id, elementType, elementId, axisRange, inverted, axisContribution);
        }
        
        public static int AddNewCategory(string categoryName)
        {
            Instance.ThrowIfNotLoaded();
            int[] categoryInt = AddNewCategory_Internal(categoryName);
            if (categoryInt[0] >= ActionCategoryIdCounter && KeyBindCategories.TryAdd(categoryName, categoryInt)) 
                API.ConfigFilesystem.Write(CategoriesFilePath, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(KeyBindCategories)));
            
            return categoryInt[0];
        }

        #endregion
        
        #region Internal Implementation
        
        internal static int[] AddNewCategory_Internal(string categoryName, bool userAssignable = true)
        {
            int categoryID = UserData.GetActionCategoryId(categoryName);
            if (categoryID != -1)
            {
                var category = UserData.GetActionCategory(categoryName);
                var mapCategory = UserData.GetMapCategory(categoryName);
                if (!userAssignable || (category.userAssignable && mapCategory.userAssignable))
                    return new[] { category.id, mapCategory.id };
                category.SetValue("_userAssignable", true);
                mapCategory.SetValue("_userAssignable", true);
                Log.LogInfo($"Enabled Category: {categoryName}");

                return new[]{ category.id, mapCategory.id };
            }
            int[] categories = UserData.AddNewCategory(categoryName, userAssignable);
            var newLayout = new CategoryLayoutData();
            newLayout.SetValue("MappingSet", new ControlMapper.MappingSet());
            newLayout.MappingSet.SetValue("_mapCategoryId", categories[1]);
            newLayout.MappingSet.SetValue("_actionListMode", 0);
            newLayout.MappingSet.SetValue("_actionCategoryIds", new[]{ categories[0] });
            newLayout.SetValue("_showActionCategoryName", new[] { categoryName != "Mods" });
            newLayout.SetValue("_showActionCategoryDescription", new[] { true });
            ModCategoryLayout.CategoryLayoutData.Add(newLayout);
            Log.LogInfo($"Added New Category: {categoryName} {(userAssignable ? "" : "(Disabled)")}");
            return categories;
        }
        
        internal static InputAction AddNewAction_Internal(string actionName, int categoryId, bool userAssignable = true)
        {
            var action = UserData.GetAction(actionName);
            if (action != null)
            {
                if (!userAssignable || action.userAssignable) return action;
                action.SetValue("_userAssignable", true);
                Log.LogInfo($"Enabled Action: {actionName}");

                return action;
            }
            UserData.AddNewAction(actionName, categoryId, userAssignable);
            Log.LogInfo($"Added New Action: {actionName} {(userAssignable ? "" : "(Disabled)")}");
            return UserData.GetAction(actionName);
        }
        
        #endregion

        #region Submodule Implementation

        /// Provides access to the singleton instance of the <see cref="ControlMappingModule"/> class.
        /// Ensures access to the module for managing Rewired keybinds and related functionality.
        internal static ControlMappingModule Instance => CoreLibMod.GetModuleInstance<ControlMappingModule>();

        /// Applies the necessary patches or hooks for the functionality of the module.
        internal override void SetHooks() => CoreLibMod.Patch(typeof(ControlMappingPatch));

        //internal override Type[] Dependencies  => new[] {typeof(LocalizationModule)};

        /// Initializes the Rewired extension module by setting up required configurations and resources.
        internal override void Load()
        {
            base.Load();
            UserData.SetValue("actionCategoryIdCounter", ActionCategoryIdCounter);
            UserData.SetValue("actionIdCounter", ActionIdCounter);
            UserData.SetValue("mapCategoryIdCounter", MapCategoryIdCounter);
            UserData.SetValue("joystickMapIdCounter", MapJoystickIdCounter);
            UserData.SetValue("keyboardMapIdCounter", MapKeyboardIdCounter);
            UserData.SetValue("mouseMapIdCounter", MapMouseIdCounter);
            ModCategoryLayout = Mod.Assets.OfType<ControlMapping_CategoryLayoutData>().FirstOrDefault();
            if (!API.ConfigFilesystem.FileExists(CategoriesFilePath) || !API.ConfigFilesystem.FileExists(ActionsFilePath))
            {
                var dic = new Dictionary<string, int[]>();
                API.ConfigFilesystem.Write(CategoriesFilePath, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(dic)));
                API.ConfigFilesystem.Write(ActionsFilePath, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(dic)));
            }

            KeyBindCategories = JsonConvert.DeserializeObject<Dictionary<string, int[]>>(Encoding.UTF8.GetString(API.ConfigFilesystem.Read(CategoriesFilePath)));
            KeyBindActions = JsonConvert.DeserializeObject<Dictionary<string, int[]>>(Encoding.UTF8.GetString(API.ConfigFilesystem.Read(ActionsFilePath)));
            
            ModsActionCategoryID ??= AddNewCategory_Internal("Mods", false);
            if (KeyBindCategories.TryAdd("Mods", ModsActionCategoryID))
            {
                API.ConfigFilesystem.Write(CategoriesFilePath, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(KeyBindCategories)));
            }
            foreach (var keyBindCategory in KeyBindCategories)
            {
                AddNewCategory_Internal(keyBindCategory.Key, false);
            }
            
            foreach (var keyBindAction in KeyBindActions)
            {
                AddNewAction_Internal(keyBindAction.Key, keyBindAction.Value[1], false);
            }
        }

        #endregion
    }
}