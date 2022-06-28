using System;
using System.Collections.Generic;
using Rewired;

namespace CoreLib;

public static class RewiredKeybinds
{
    internal static Dictionary<string, KeyBindData> keyBinds = new Dictionary<string, KeyBindData>();

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
        AddKeybind(keyBindName, new Dictionary<string, string> {{"en", description}}, defaultKeyCode, modifier);
    }

    /// <summary>
    /// Add new rebindable keybind
    /// </summary>
    /// <param name="keyBindName">UNIQUE key bind name</param>
    /// <param name="descriptions">translation dictionary with descriptions</param>
    /// <param name="defaultKeyCode">Default key bind KeyCode</param>
    /// <param name="modifier">key bind modifier</param>
    public static void AddKeybind(string keyBindName, Dictionary<string, string> descriptions, KeyboardKeyCode defaultKeyCode, ModifierKey modifier = ModifierKey.None)
    {
        if (keyBinds.ContainsKey(keyBindName))
        {
            CoreLib.Logger.LogWarning($"Error trying to add keybind action with name {keyBindName}! This keybind name is already taken!");
            return;
        }
        
        keyBinds.Add(keyBindName, new KeyBindData(defaultKeyCode, modifier));
        Localization.AddTerm($"ControlMapper/{keyBindName}", descriptions);
    }

    /// <summary>
    /// Get key bind numeric ID.
    /// </summary>
    /// <param name="keyBindName">UNIQUE key bind name</param>
    /// <returns>key bind numeric ID</returns>
    /// <exception cref="ArgumentException">thrown if there are no key binds registered with keyBindName</exception>
    public static int GetKeybindId(string keyBindName)
    {
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
    
}

public class KeyBindData
{
    public KeyboardKeyCode defaultKeyCode;
    public ModifierKey modifierKey;
    public int actionId;

    public KeyBindData(KeyboardKeyCode defaultKeyCode, ModifierKey modifierKey)
    {
        this.defaultKeyCode = defaultKeyCode;
        this.modifierKey = modifierKey;
    }
}