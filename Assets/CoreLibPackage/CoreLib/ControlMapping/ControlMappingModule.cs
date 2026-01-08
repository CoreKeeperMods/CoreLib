using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using CoreLib.Submodule.ControlMapping.Extension;
using CoreLib.Submodule.ControlMapping.Patch;
using CoreLib.Submodule.Localization;
using CoreLib.Util.Extension;
using PugMod;
using Rewired;
using Rewired.Data;
using Rewired.Data.Mapping;
using Rewired.UI.ControlMapper;
using UnityEngine;
using Logger = CoreLib.Util.Logger;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.ControlMapping
{
    /// <summary>
    /// This module provides the means to add custom Rewired Key binds.
    /// </summary>
    public class ControlMappingModule : BaseSubmodule
    {
        #region Fields
        
        public new const string Name = "Core Library - Rewired Extension";
        
        internal new static Logger Log = new(Name);
        
        internal static Dictionary<string, ActionElementMap> KeyboardMapBinds = new();
        
        internal static InputManager_Base InputManagerBase = Resources.Load<InputManager_Base>($"Rewired Input Manager");
        
        internal static UserData UserData => InputManagerBase.userData;
        
        internal static ControlMapping_CategoryLayoutData ModCategoryLayout;
        
        internal static int[] ModsActionCategoryID;
        
        internal static Dictionary<string, int[]> KeyBindCategories = new();
        internal static Dictionary<string, int[]> KeyBindActions = new();
        
        /// <summary>
        /// Event that triggers a callback when the Rewired input system is fully initialized.
        /// </summary>
        public static Action RewiredStart;
        
        #endregion
        
        #region Public Interface
        

        
        /// <summary>
        /// Add a new rebindable keybind
        /// </summary>
        /// <param name="keyBindName">UNIQUE key bind name</param>
        /// <param name="defaultKeyCode">Default key bind KeyCode</param>
        /// <param name="modifier">Key bind modifier. Defaults to none</param>
        /// <param name="modifier2"></param>
        /// <param name="modifier3"></param>
        /// <param name="categoryID">Category ID to add KeyBind</param>
        public static void AddKeyBind(string keyBindName = "", KeyboardKeyCode defaultKeyCode = KeyboardKeyCode.None,
            ModifierKey modifier = ModifierKey.None, ModifierKey modifier2 = ModifierKey.None,
            ModifierKey modifier3 = ModifierKey.None, int categoryID = -1)
        {
            Instance.ThrowIfNotLoaded();
            if (string.IsNullOrEmpty(keyBindName))
            {
                Log.LogWarning($"No Name provided for keybind!");
                return;
            }
            
            if (categoryID == -1)
            {
                ModsActionCategoryID ??= AddNewCategory_Internal("Mods");
                categoryID = ModsActionCategoryID[0];
            }

            if (UserData.GetActionId(keyBindName) != -1)
            {
                Log.LogWarning($"Error trying to add keybind action with name {keyBindName}! This keybind name is already taken!");
                return;
            }
            UserData.AddNewAction(keyBindName, categoryID);
            
            int catInt = UserData.IndexOfActionCategory(categoryID);
            var actionCategory = UserData.GetActionCategory(catInt);
            var mapCategory = UserData.GetMapCategory(actionCategory.name);
            var keyboardMap = UserData.GetKeyboardMap(mapCategory.id, 0);
            keyboardMap.AddActionElementMap();
            var mapElement = keyboardMap.actionElementMaps.Last();
            mapElement.SetValue("_actionCategoryId", action.categoryId);
            mapElement.SetValue("_actionId", action.id);
            mapElement.SetValue("_elementType", ControllerElementType.Button);
            mapElement.SetValue("_keyboardKeyCode", defaultKeyCode);
            mapElement.SetValue("_modifierKey1", modifier);
            /*var mouseMap = UserData.GetMouseMap(mapCategory.id, 0);
            mouseMap.AddActionElementMap();
            var mapMouseElement = mouseMap.actionElementMaps.Last();
            mapMouseElement.SetValue("_actionCategoryId", action.categoryId);
            mapMouseElement.SetValue("_actionId", action.id);
            mapMouseElement.SetValue("_elementType", ControllerElementType.Button);
            var joystickMap = UserData.GetJoystickMap(mapCategory.id, new Guid("83b427e4-086f-47f3-bb06-be266abd1ca5"), 0);
            joystickMap.AddActionElementMap();
            var mapJoystickElement = joystickMap.actionElementMaps.Last();
            mapJoystickElement.SetValue("_actionCategoryId", action.categoryId);
            mapJoystickElement.SetValue("_actionId", action.id);
            mapJoystickElement.SetValue("_elementType", ControllerElementType.Button);*/
        }
        
        public static int AddNewCategory(string categoryName)
        {
            Instance.ThrowIfNotLoaded();
            int[] categoryInt = AddNewCategory_Internal(categoryName);
            if (KeyBindCategories.TryAdd(categoryName, categoryInt))
            {
                API.Config.Set(CoreLibMod.ID, "KeyBinds", "KeyBindCategories", KeyBindCategories);
            }
            
            return categoryInt[0];
        }

        /// <summary>
        /// Sets the default controller binding for an existing custom keybind. The keybind must be created beforehand using AddKeybind().
        /// </summary>
        /// <param name="keyBindName">The name of the existing keybind to set the default controller binding.</param>
        /// <param name="elementId">The element ID of the controller component to bind.</param>
        /// <param name="elementType">The type of the controller element (e.g., Button or Axis). Defaults to Button.</param>
        /// <param name="axisRange">The range of the axis, if applicable. Defaults to Full.</param>
        public static void SetDefaultControllerBinding(string keyBindName, int elementId,
            ControllerElementType elementType = ControllerElementType.Button,
            AxisRange axisRange = AxisRange.Full)
        {
            Instance.ThrowIfNotLoaded();
        }

        internal static int[] AddNewCategory_Internal(string categoryName, bool userAssignable = true)
        {
            int categoryID = UserData.GetActionCategoryId(categoryName);
            if (categoryID != -1)
            {
                var category = UserData.GetActionCategory(categoryID);
                var mapCategory = UserData.GetMapCategory(category.name);
                if (userAssignable && (!category.userAssignable || !mapCategory.userAssignable))
                {
                    category.SetValue("_userAssignable", true);
                    mapCategory.SetValue("_userAssignable", true);
                    Log.LogInfo($"Enabled user assignable for category {categoryName}");
                }
                else
                {
                    Log.LogWarning($"Warning: Category {categoryName} already exists!");
                }
                
                return new []{ category.id, mapCategory.id };
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
            Log.LogInfo($"Added new category {categoryName}");
            return categories;
        }

        #endregion

        #region Submodule Implementation

        /// <summary>
        /// Provides access to the singleton instance of the <see cref="ControlMappingModule"/> class.
        /// Ensures access to the module for managing Rewired keybinds and related functionality.
        /// </summary>
        internal static ControlMappingModule Instance => CoreLibMod.GetModuleInstance<ControlMappingModule>();

        /// <summary>
        /// Applies the necessary patches or hooks for the functionality of the module.
        /// </summary>
        internal override void SetHooks() => CoreLibMod.Patch(typeof(ControlMappingPatch));

        internal override Type[] Dependencies  => new[] {typeof(LocalizationModule)};

        /// <summary>
        /// Initializes the Rewired extension module by setting up required configurations and resources.
        /// </summary>
        internal override void Load()
        {
            base.Load();
            ModCategoryLayout = Mod.Assets.OfType<ControlMapping_CategoryLayoutData>().FirstOrDefault();
            API.Config.Register(CoreLibMod.ID, "KeyBinds", "Category Key Binds", "KeyBindCategories", new Dictionary<string, int[]>());
            API.Config.Register(CoreLibMod.ID, "KeyBinds", "Action Key Binds", "KeyBindActions", new Dictionary<string, int[]>());
            KeyBindCategories = API.Config.Get<Dictionary<string, int[]>>(CoreLibMod.ID, "KeyBinds", "KeyBindCategories");
            KeyBindActions = API.Config.Get<Dictionary<string, int[]>>(CoreLibMod.ID, "KeyBinds", "KeyBindActions");
            if (LocalizationModule.Instance.Loaded)
            {
                var controlMappingLocalizationTable = Mod.Assets.OfType<ModdedLocalizationTable>().First(x => x.name == "ControlMapping Localization Table");
                var localizationTerms = controlMappingLocalizationTable.GetTerms();
                foreach (var term in localizationTerms)
                {
                    foreach (var lang in term.languageTerms)
                    {
                        LocalizationModule.AddTerm_Internal(term.term, lang);
                    }
                }
            }

            foreach (var keyBindCategory in KeyBindCategories)
            {
                AddNewCategory_Internal(keyBindCategory.Key, false);
            }
            
            foreach (var keyBindAction in KeyBindActions)
            {
                UserData.AddNewAction(keyBindAction.Key, keyBindAction.Value[1], false);
            }
        }

        #endregion
    }
}