using System;
using System.Collections.Generic;
using CoreLib.Data;
using CoreLib.Data.Configuration;
using CoreLib.RewiredExtension.Patches;
using CoreLib.Localization;
using CoreLib.Util.Extensions;
using Rewired;
using Rewired.Data;

namespace CoreLib.RewiredExtension
{
    /// <summary>
    /// This module provides means to add custom Rewired Key binds
    /// </summary>
    public class RewiredExtensionModule : BaseSubmodule
    {
        #region Public Interface

        /// <summary>
        /// Use this event to receive a callback when rewired input system is initialized
        /// </summary>
        public static Action rewiredStart;

        /// <summary>
        /// Add new rebindable keybind
        /// </summary>
        /// <param name="keyBindName">UNIQUE key bind name</param>
        /// <param name="description">key bind short description</param>
        /// <param name="defaultKeyCode">Default key bind KeyCode</param>
        /// <param name="modifier">key bind modifier</param>
        public static void AddKeybind(string keyBindName, string description, KeyboardKeyCode defaultKeyCode, ModifierKey modifier = ModifierKey.None)
        {
            Instance.ThrowIfNotLoaded();

            AddKeybind(keyBindName, new Dictionary<string, string> { { "en", description } }, defaultKeyCode, modifier);
        }

        /// <summary>
        /// Add new rebindable keybind
        /// </summary>
        /// <param name="keyBindName">UNIQUE key bind name</param>
        /// <param name="descriptions">translation dictionary with descriptions</param>
        /// <param name="defaultKeyCode">Default key bind KeyCode</param>
        /// <param name="modifier">key bind modifier</param>
        public static void AddKeybind(string keyBindName, Dictionary<string, string> descriptions, KeyboardKeyCode defaultKeyCode,
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
        /// Add default controller binding for existing custom keybind. Ensure the keybind has been created with <see cref="AddKeybind"/>
        /// </summary>
        /// <param name="keyBindName">Existing key bind name</param>
        /// <param name="elementId">Element Id. Reference <see cref="GamepadTemplate"/></param>
        /// <param name="elementType">Element type (Button or Axis)</param>
        /// <param name="axisRange">Axis Range</param>
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
        /// Get key bind numeric ID.
        /// </summary>
        /// <param name="keyBindName">UNIQUE key bind name</param>
        /// <returns>key bind numeric ID</returns>
        /// <exception cref="ArgumentException">thrown if there are no key binds registered with keyBindName</exception>
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

        internal override GameVersion Build => new GameVersion(0, 7, 5, "3339");
        internal override string Version => "3.1.2";

        internal override Type[] Dependencies => new[] { typeof(LocalizationModule) };
        internal static RewiredExtensionModule Instance => CoreLibMod.GetModuleInstance<RewiredExtensionModule>();

        internal override void SetHooks()
        {
            CoreLibMod.Patch(typeof(Rewired_Patch));
            CoreLibMod.Patch(typeof(Rewired_Init_Patch));

            //UniversalRewiredPatch();
        }
/*
        private static void UniversalRewiredPatch()
        {
            var method = (MemberInfo)typeof(UserData)
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(method =>
                    method.ReturnType == typeof(void) &&
                    method.GetParameters().Length == 0 &&
                    Rewired_Init_Patch.IsObfuscated(method.Name));

            if (method == null) return;

            var patch = typeof(Rewired_Init_Patch).GetMethod("OnRewiredDataInit");

            CoreLibMod.Log.LogDebug($"Found rewired init method: {method.GetNameChecked()}");
            Patch(method, patch);
        }

        private static void Patch(MemberInfo original,
            MemberInfo prefix = null,
            MemberInfo postfix = null,
            MemberInfo transpiler = null,
            MemberInfo finalizer = null,
            MemberInfo ilmanipulator = null)
        {
            Harmony harmony = new Harmony("CoreLib.Rewired");
            harmony.Patch((MethodBase)original, 
                prefix != null ? new HarmonyMethod((MethodInfo)prefix) : null, 
                postfix != null ? new HarmonyMethod((MethodInfo)postfix) : null, 
                transpiler != null ? new HarmonyMethod((MethodInfo)transpiler) : null, 
                finalizer != null ? new HarmonyMethod((MethodInfo)finalizer) : null, 
                ilmanipulator != null ? new HarmonyMethod((MethodInfo)ilmanipulator) : null);
        }
        */

        internal override void Load()
        {
           keybindIdCache = new ConfigFile($"{CoreLibMod.CONFIG_FOLDER}CoreLib.KeybindID.cfg", true, CoreLibMod.modInfo);
        }

        internal static Dictionary<string, KeyBindData> keyBinds = new Dictionary<string, KeyBindData>();
        internal static ConfigFile keybindIdCache;

        #endregion
    }
}