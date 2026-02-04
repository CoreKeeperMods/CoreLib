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
        
        public const string NAME = "Core Library - Control Mapping";
        
        internal static Logger log = new(NAME);
        
        internal static ConfigFile Config => CoreLibMod.config;
        
        internal const string CATEGORIES_FILE_PATH = "CoreLib\\KeyBindsCategories.json";
        internal const string ACTIONS_FILE_PATH = "CoreLib\\KeyBindsActions.json";
        
        internal static InputManager_Base inputManagerBase = Resources.Load<InputManager_Base>($"Rewired Input Manager");
        
        internal static UserData UserData => inputManagerBase.userData;
        
        internal static ControlMapping_CategoryLayoutData modCategoryLayout;
        
        internal static int[] modsActionCategoryID;
        
        internal static Dictionary<string, int[]> keyBindCategories; //[Category Name, [Category ID, Map Category ID]]
        internal static Dictionary<string, int[]> keyBindActions; //[Action Name, [Action ID, Action Category ID]]
        
        internal static int ActionCategoryIdCounter => 100;
        internal static int MapCategoryIdCounter => 100;
        internal static int ActionIdCounter => 1000;
        internal static int MapJoystickIdCounter => 1000;
        internal static int MapKeyboardIdCounter => 1000;
        internal static int MapMouseIdCounter => 1000;
        
        public static Action rewiredStart;
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Add a new Keyboard Keybind.
        /// </summary>
        /// <param name="keyBindName">Unique Keybind Name</param>
        /// <param name="defaultKeyCode">Keyboard KeyCode</param>
        /// <param name="modifier">First KeyCode Modifier</param>
        /// <param name="modifier2">Second KeyCode Modifier</param>
        /// <param name="modifier3">Third KeyCode Modifier</param>
        /// <param name="categoryId">Category ID for Keybind</param>
        public static void AddKeyboardBind(string keyBindName = "", KeyboardKeyCode defaultKeyCode = KeyboardKeyCode.None,
            ModifierKey modifier = ModifierKey.None, ModifierKey modifier2 = ModifierKey.None,
            ModifierKey modifier3 = ModifierKey.None, int categoryId = -1)
        {
            Instance.ThrowIfNotLoaded();
            if (string.IsNullOrEmpty(keyBindName))
            {
                log.LogWarning($"No Name provided for keybind!");
                return;
            }
            
            if (categoryId == -1)
            {
                AddNewCategory_Internal("Mods");
                categoryId = modsActionCategoryID[0];
            }
            
            var action = AddNewAction_Internal(keyBindName, categoryId);
            
            if (action.id >= ActionIdCounter && keyBindActions.TryAdd(keyBindName, new []{action.id, categoryId})) 
                API.ConfigFilesystem.Write(ACTIONS_FILE_PATH, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(keyBindActions)));
            
            int catInt = UserData.IndexOfActionCategory(categoryId);
            var actionCategory = UserData.GetActionCategory(catInt);
            var mapCategory = UserData.GetMapCategory(actionCategory.name);
            var keyboardMap = UserData.GetKeyboardMap(mapCategory.id, 0);
            keyboardMap.AddNewActionElementMap(categoryId, action.id, ControllerElementType.Button, keyCode: defaultKeyCode, modifierKey1: modifier,
            modifierKey2: modifier2, modifierKey3: modifier3);
        }

        /// <summary>
        /// Add a new Mouse Button KeyBind.
        /// </summary>
        /// <param name="keyBindName">Unique Keybind name</param>
        /// <param name="elementId">Mouse Element ID</param>
        /// <param name="categoryId">Category ID for Keybind</param>
        public static void AddMouseBind(string keyBindName = "", int elementId = -1,
            int categoryId = -1)
        {
            Instance.ThrowIfNotLoaded();
            if (string.IsNullOrEmpty(keyBindName))
            {
                log.LogWarning($"No Name provided for keybind!");
                return;
            }
            
            if (categoryId == -1)
            {
                AddNewCategory_Internal("Mods");
                categoryId = modsActionCategoryID[0];
            }
            
            var action = AddNewAction_Internal(keyBindName, categoryId);
            
            if (action.id >= ActionIdCounter && keyBindActions.TryAdd(keyBindName, new []{action.id, categoryId})) 
                API.ConfigFilesystem.Write(ACTIONS_FILE_PATH, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(keyBindActions)));
            
            int catInt = UserData.IndexOfActionCategory(categoryId);
            var actionCategory = UserData.GetActionCategory(catInt);
            var mapCategory = UserData.GetMapCategory(actionCategory.name);
            var mouseMap = UserData.GetMouseMap(mapCategory.id, 0);
            mouseMap.AddNewActionElementMap(action.categoryId, action.id, ControllerElementType.Button, elementId);
        }

        /// <summary>
        /// Add a new Joystick/Controller KeyBind.
        /// </summary>
        /// <param name="keyBindName">Unique Keybind Name</param>
        /// <param name="elementId">Element ID for the controller keybind</param>
        /// <param name="elementType">Controller Element Type</param>
        /// <param name="axisRange">Axis Range</param>
        /// <param name="inverted">Invert Axis boolean</param>
        /// <param name="axisContribution">Pole Axis Contribution</param>
        /// <param name="categoryId">Category ID for Keybind</param>
        public static void AddControllerBind(string keyBindName = "", int elementId = -1,
            ControllerElementType elementType = ControllerElementType.Button,
            AxisRange axisRange = AxisRange.Full, bool inverted = false, Pole axisContribution = Pole.Positive, int categoryId = -1)
        {
            Instance.ThrowIfNotLoaded();
            if (string.IsNullOrEmpty(keyBindName))
            {
                log.LogWarning($"No Name provided for keybind!");
                return;
            }

            if (categoryId == -1)
            {
                AddNewCategory_Internal("Mods");
                categoryId = modsActionCategoryID[0];
            }
                

            var action = AddNewAction_Internal(keyBindName, categoryId);
            
            if (action.id >= ActionIdCounter && keyBindActions.TryAdd(keyBindName, new []{action.id, categoryId})) 
                API.ConfigFilesystem.Write(ACTIONS_FILE_PATH, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(keyBindActions)));
            
            int catInt = UserData.IndexOfActionCategory(categoryId);
            var actionCategory = UserData.GetActionCategory(catInt);
            var mapCategory = UserData.GetMapCategory(actionCategory.name);
            var joystickMap = UserData.GetJoystickMap(mapCategory.id, new Guid("83b427e4-086f-47f3-bb06-be266abd1ca5"), 0);
            joystickMap.AddNewActionElementMap(action.categoryId, action.id, elementType, elementId, axisRange, inverted, axisContribution);
        }
        
        /// <summary>
        /// Add a new Category for Keybinds.
        /// </summary>
        /// <param name="categoryName">Category Name</param>
        /// <returns>ID of the new Category</returns>
        public static int AddNewCategory(string categoryName)
        {
            Instance.ThrowIfNotLoaded();
            int[] categoryInt = AddNewCategory_Internal(categoryName);
            if (categoryInt[0] >= ActionCategoryIdCounter && keyBindCategories.TryAdd(categoryName, categoryInt)) 
                API.ConfigFilesystem.Write(CATEGORIES_FILE_PATH, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(keyBindCategories)));
            
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
                log.LogInfo($"Enabled Category: {categoryName}");

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
            modCategoryLayout.CategoryLayoutData.Add(newLayout);
            log.LogInfo($"Added New Category: {categoryName} {(userAssignable ? "" : "(Disabled)")}");
            return categories;
        }
        
        internal static InputAction AddNewAction_Internal(string actionName, int categoryId, bool userAssignable = true)
        {
            var action = UserData.GetAction(actionName);
            if (action != null)
            {
                if (action.categoryId != categoryId)
                {
                    UserData.ChangeActionCategory(action.id, categoryId);
                }
                
                if (!userAssignable || action.userAssignable) return action;
                action.SetValue("_userAssignable", true);
                log.LogInfo($"Enabled Action: {actionName}");

                return action;
            }
            UserData.AddNewAction(actionName, categoryId, userAssignable);
            log.LogInfo($"Added New Action: {actionName} {(userAssignable ? "" : "(Disabled)")}");
            return UserData.GetAction(actionName);
        }
        
        #endregion

        #region Submodule Implementation
        
        internal static ControlMappingModule Instance => CoreLibMod.GetModuleInstance<ControlMappingModule>();

        internal override void SetHooks() => CoreLibMod.Patch(typeof(ControlMappingPatch));

        internal override void Load()
        {
            base.Load();
            UserData.SetValue("actionCategoryIdCounter", ActionCategoryIdCounter);
            UserData.SetValue("actionIdCounter", ActionIdCounter);
            UserData.SetValue("mapCategoryIdCounter", MapCategoryIdCounter);
            UserData.SetValue("joystickMapIdCounter", MapJoystickIdCounter);
            UserData.SetValue("keyboardMapIdCounter", MapKeyboardIdCounter);
            UserData.SetValue("mouseMapIdCounter", MapMouseIdCounter);
            modCategoryLayout = Mod.Assets.OfType<ControlMapping_CategoryLayoutData>().FirstOrDefault();
            if (!API.ConfigFilesystem.FileExists(CATEGORIES_FILE_PATH) || !API.ConfigFilesystem.FileExists(ACTIONS_FILE_PATH))
            {
                var dic = new Dictionary<string, int[]>();
                API.ConfigFilesystem.Write(CATEGORIES_FILE_PATH, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(dic)));
                API.ConfigFilesystem.Write(ACTIONS_FILE_PATH, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(dic)));
            }

            keyBindCategories = JsonConvert.DeserializeObject<Dictionary<string, int[]>>(Encoding.UTF8.GetString(API.ConfigFilesystem.Read(CATEGORIES_FILE_PATH)));
            keyBindActions = JsonConvert.DeserializeObject<Dictionary<string, int[]>>(Encoding.UTF8.GetString(API.ConfigFilesystem.Read(ACTIONS_FILE_PATH)));
            
            modsActionCategoryID ??= AddNewCategory_Internal("Mods", false);
            if (keyBindCategories.TryAdd("Mods", modsActionCategoryID))
            {
                API.ConfigFilesystem.Write(CATEGORIES_FILE_PATH, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(keyBindCategories)));
            }
            foreach (var keyBindCategory in keyBindCategories)
            {
                AddNewCategory_Internal(keyBindCategory.Key, false);
            }
            
            foreach (var keyBindAction in keyBindActions)
            {
                AddNewAction_Internal(keyBindAction.Key, keyBindAction.Value[1], false);
            }
        }

        #endregion
    }
}