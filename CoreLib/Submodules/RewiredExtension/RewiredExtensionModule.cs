using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using CoreLib.Submodules.Localization;
using CoreLib.Submodules.RewiredExtension.Patches;
using Rewired;

namespace CoreLib.Submodules.RewiredExtension;

/// <summary>
/// This module provides means to add custom Rewired Key binds
/// </summary>
[CoreLibSubmodule(Dependencies = new[] { typeof(LocalizationModule) })]
public static class RewiredExtensionModule
{
    #region Public Interface

    /// <summary>
    /// Return true if the submodule is loaded.
    /// </summary>
    public static bool Loaded
    {
        get => _loaded;
        internal set => _loaded = value;
    }

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
        ThrowIfNotLoaded();

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
        ThrowIfNotLoaded();

        if (keyBinds.ContainsKey(keyBindName))
        {
            CoreLibPlugin.Logger.LogWarning($"Error trying to add keybind action with name {keyBindName}! This keybind name is already taken!");
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
        ThrowIfNotLoaded();

        if (keyBinds.ContainsKey(keyBindName))
        {
            return keyBinds[keyBindName].actionId;
        }

        int index = ReInput.UserData.IndexOfAction(keyBindName);
        if (index != -1)
        {
            return index;
        }

        throw new ArgumentException($"Keybind action with name {keyBindName} is not registered!");
    }

    #endregion

    #region Private Implementation

    private static bool _loaded;


    [CoreLibSubmoduleInit(Stage = InitStage.SetHooks)]
    internal static void SetHooks()
    {
        CoreLibPlugin.harmony.PatchAll(typeof(Rewired_Patch));
        CoreLibPlugin.harmony.PatchAll(typeof(Rewired_Init_Patch));
    }

    [CoreLibSubmoduleInit(Stage = InitStage.Load)]
    internal static void Load()
    {
        BepInPlugin metadata = MetadataHelper.GetMetadata(typeof(CoreLibPlugin));
        keybindIdCache = new ConfigFile($"{Paths.ConfigPath}/CoreLib/CoreLib.KeybindID.cfg", true, metadata);
    }

    internal static void ThrowIfNotLoaded()
    {
        if (!Loaded)
        {
            Type submoduleType = MethodBase.GetCurrentMethod().DeclaringType;
            string message = $"{submoduleType.Name} is not loaded. Please use [{nameof(CoreLibSubmoduleDependency)}(nameof({submoduleType.Name})]";
            throw new InvalidOperationException(message);
        }
    }

    internal static Dictionary<string, KeyBindData> keyBinds = new Dictionary<string, KeyBindData>();
    internal static ConfigFile keybindIdCache;

    #endregion
}