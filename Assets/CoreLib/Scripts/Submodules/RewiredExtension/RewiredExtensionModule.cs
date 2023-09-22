using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CoreLib.Data;
using CoreLib.Submodules.Localization;
using CoreLib.Submodules.RewiredExtension.Patches;
using CoreLib.Util.Extensions;
using PugMod;
using Rewired;
using Rewired.Data;
using MemberInfo = PugMod.MemberInfo;

namespace CoreLib.Submodules.RewiredExtension
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

            int index = typeof(ReInput).GetProperty<UserData>("UserData").IndexOfAction(keyBindName);
            if (index != -1)
            {
                return index;
            }

            throw new ArgumentException($"Keybind action with name {keyBindName} is not registered!");
        }

        #endregion

        #region Private Implementation

        internal override GameVersion Build => new GameVersion(0, 0, 0, 0, "");

        internal override Type[] Dependencies => new[] { typeof(LocalizationModule) };
        internal static RewiredExtensionModule Instance => CoreLibMod.GetModuleInstance<RewiredExtensionModule>();

        internal override void SetHooks()
        {
            HarmonyUtil.PatchAll(typeof(Rewired_Patch));
            HarmonyUtil.PatchAll(typeof(Rewired_Init_Patch));

            var method = (MemberInfo)typeof(UserData)
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(method =>
                    method.ReturnType == typeof(void) &&
                    method.GetParameters().Length == 0 &&
                    Rewired_Init_Patch.IsObfuscated(method.Name));

            if (method == null) return;

            var patch = typeof(Rewired_Init_Patch).GetMethod("OnRewiredDataInit");

            CoreLibMod.Log.LogDebug($"Found rewired init method: {method.GetNameChecked()}");
            HarmonyUtil.Patch(method,patch);
        }

        internal override void Load()
        {
           keybindIdCache = new JsonConfigFile("CoreLib", "CoreLib.KeybindID", true);
        }

        internal static Dictionary<string, KeyBindData> keyBinds = new Dictionary<string, KeyBindData>();
        internal static JsonConfigFile keybindIdCache;

        #endregion
    }
}