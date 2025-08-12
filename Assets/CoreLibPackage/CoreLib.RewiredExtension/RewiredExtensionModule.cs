using System;
using System.Collections.Generic;
using CoreLib.Data.Configuration;
using CoreLib.RewiredExtension.Patches;
using CoreLib.Localization;
using CoreLib.Util.Extensions;
using Rewired;
using Rewired.Data;

namespace CoreLib.RewiredExtension
{
    /// <summary>
    /// This module provides means to add custom Rewired Key binds.
    /// </summary>
    public class RewiredExtensionModule : BaseSubmodule
    {
        #region Public Interface

        /// <summary>
        /// Event that triggers a callback when the Rewired input system is fully initialized.
        /// </summary>
        public static Action rewiredStart;

        /// <summary>
        /// Add new rebindable keybind
        /// </summary>
        /// <param name="keyBindName">UNIQUE key bind name</param>
        /// <param name="description">Key bind short description</param>
        /// <param name="defaultKeyCode">Default key bind KeyCode</param>
        /// <param name="modifier">Key bind modifier. Defaults to none</param>
        public static void AddKeybind(string keyBindName, string description, KeyboardKeyCode defaultKeyCode,
            ModifierKey modifier = ModifierKey.None)
        {
            Instance.ThrowIfNotLoaded();

            AddKeybind(keyBindName, new Dictionary<string, string> { { "en", description } }, defaultKeyCode, modifier);
        }

        /// <summary>
        /// Add new rebindable keybind with localized descriptions
        /// </summary>
        /// <param name="keyBindName">UNIQUE key bind name</param>
        /// <param name="descriptions">Translation dictionary containing descriptions in various languages</param>
        /// <param name="defaultKeyCode">Default key bind KeyCode</param>
        /// <param name="modifier">Key bind modifier. Defaults to none</param>
        public static void AddKeybind(string keyBindName, Dictionary<string, string> descriptions,
            KeyboardKeyCode defaultKeyCode,
            ModifierKey modifier = ModifierKey.None)
        {
            Instance.ThrowIfNotLoaded();

            if (keyBinds.ContainsKey(keyBindName))
            {
                CoreLibMod.Log.LogWarning($"Error trying to add keybind action with name {keyBindName}! This keybind name is already taken!");
                return;
            }

            keyBinds.Add(keyBindName, new KeyBindData(defaultKeyCode, modifier));
            LocalizationModule.AddTerm($"ControlMapper/{keyBindName}", descriptions);
        }

        /// <summary>
        /// Sets the default controller binding for an existing custom keybind. The keybind must be created beforehand using <see cref="AddKeybind"/>.
        /// </summary>
        /// <param name="keyBindName">The name of the existing keybind to set the default controller binding for.</param>
        /// <param name="elementId">The element ID of the controller component to bind.</param>
        /// <param name="elementType">The type of the controller element (e.g., Button or Axis). Defaults to Button.</param>
        /// <param name="axisRange">The range of the axis, if applicable. Defaults to Full.</param>
        public static void SetDefaultControllerBinding(
            string keyBindName,
            int elementId,
            ControllerElementType elementType = ControllerElementType.Button,
            AxisRange axisRange = AxisRange.Full)
        {
            Instance.ThrowIfNotLoaded();

            if (!keyBinds.ContainsKey(keyBindName))
            {
                CoreLibMod.Log.LogWarning($"Error trying to set default controller binding for keybind {keyBindName}! No such keybind found!");
                return;
            }

            var keybind = keyBinds[keyBindName];
            keybind.gamepadElementType = elementType;
            keybind.gamepadAxisRange = axisRange;
            keybind.gamepadElementId = elementId;
        }

        /// <summary>
        /// Get the numeric ID for a keybind action by its unique name.
        /// </summary>
        /// <param name="keyBindName">The unique name of the keybind action</param>
        /// <returns>The numeric ID of the keybind action</returns>
        /// <exception cref="ArgumentException">Thrown if no keybind with the specified name exists</exception>
        public static int GetKeybindId(string keyBindName)
        {
            Instance.ThrowIfNotLoaded();

            if (keyBinds.ContainsKey(keyBindName))
            {
                return keyBinds[keyBindName].actionId;
            }

            int index = typeof(ReInput).GetValue<UserData>("UserData").IndexOfAction(keyBindName);
            if (index != -1)
            {
                return index;
            }

            throw new ArgumentException($"Keybind action with name {keyBindName} is not registered!");
        }

        #endregion

        #region Private Implementation

        internal override GameVersion Build => new GameVersion(1, 1, 0, "90bc");
        internal override string Version => "3.1.3";

        internal override Type[] Dependencies => new[] { typeof(LocalizationModule) };

        /// <summary>
        /// Provides access to the singleton instance of the <see cref="RewiredExtensionModule"/> class.
        /// Ensures access to the module for managing Rewired keybinds and related functionality.
        /// </summary>
        internal static RewiredExtensionModule Instance => CoreLibMod.GetModuleInstance<RewiredExtensionModule>();

        /// <summary>
        /// Applies necessary patches or hooks for the functionality of the module.
        /// </summary>
        internal override void SetHooks()
        {
            CoreLibMod.Patch(typeof(Rewired_Patch));
            CoreLibMod.Patch(typeof(Rewired_Init_Patch));
            CoreLibMod.Patch(typeof(ControlMappingMenu_Patch));
        }

        /// <summary>
        /// Initializes the Rewired extension module by setting up required configurations and resources.
        /// </summary>
        internal override void Load()
        {
           keybindIdCache = new ConfigFile($"{CoreLibMod.CONFIG_FOLDER}CoreLib.KeybindID.cfg", true, CoreLibMod.modInfo);
        }

        /// <summary>
        /// A dictionary containing all configured key binds, where the key is the name of the bind
        /// and the value is the associated <see cref="KeyBindData"/> object. This is used to manage
        /// and store keyboard and controller input bindings within the Rewired extension module.
        /// </summary>
        internal static Dictionary<string, KeyBindData> keyBinds = new Dictionary<string, KeyBindData>();

        /// <summary>
        /// Represents a configuration file used to store and manage cached keybind IDs for the Rewired input system.
        /// </summary>
        internal static ConfigFile keybindIdCache;

        #endregion
    }
}