using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using CoreLib.Submodules.ChatCommands.Patches;
using CoreLib.Submodules.RewiredExtension;
using Rewired;

namespace CoreLib.Submodules.ChatCommands;

/// <summary>
/// This module provides means to add custom chat commands
/// </summary>
[CoreLibSubmodule(Dependencies = new []{typeof(RewiredExtensionModule)})]
public static class CommandsModule
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

    internal static ConfigEntry<bool> remindAboutHelpCommand;

    /// <summary>
    /// Add all commands from specified assembly
    /// </summary>
    public static void AddCommands(Assembly assembly, string modName)
    {
        ThrowIfNotLoaded();
        Type[] commands = assembly.GetTypes().Where(type => typeof(IChatCommandHandler).IsAssignableFrom(type)).ToArray();
        commandHandlers.Capacity += commands.Length;

        foreach (Type commandType in commands)
        {
            if (commandType == typeof(IChatCommandHandler)) continue;

            try
            {
                IChatCommandHandler handler = (IChatCommandHandler)Activator.CreateInstance(commandType);
                commandHandlers.Add(new CommandPair(handler, modName));
            }
            catch (Exception e)
            {
                CoreLibPlugin.Logger.LogWarning($"Failed to register command {commandType}!\n{e}");
            }
        }
    }
    
    public static bool GetCommandHandler(string commandName, out IChatCommandHandler commandHandler)
    {
        commandHandler = commandHandlers
            .Select(pair => pair.handler)
            .FirstOrDefault(handler => handler
                    .GetTriggerNames()
                    .Any(s => s.Equals(commandName, StringComparison.InvariantCultureIgnoreCase)));
        return commandHandler != null;
    }

    public static IEnumerable<IChatCommandHandler> GetChatCommandHandlers(string commandName)
    {
        return commandHandlers
            .Select(pair => pair.handler)
            .Where(handler => handler
                .GetTriggerNames()
                .Any(s => s.Equals(commandName, StringComparison.InvariantCultureIgnoreCase)));
    }

    #endregion

    #region Private Implementation

    private static bool _loaded;
    internal static Player rewiredPlayer;

    internal static string UP_KEY = "CoreLib_UpKey";
    internal static string DOWN_KEY = "CoreLib_DownKey";
    internal static string COMPLETE_KEY = "CoreLib_CompleteKey";


    [CoreLibSubmoduleInit(Stage = InitStage.SetHooks)]
    internal static void SetHooks()
    {
        CoreLibPlugin.harmony.PatchAll(typeof(ChatWindow_Patch));
        CoreLibPlugin.harmony.PatchAll(typeof(TitleScreenAnimator_Patch));
    }
    
    [CoreLibSubmoduleInit(Stage = InitStage.PostLoad)]
    internal static void Load()
    {
        AddCommands(Assembly.GetExecutingAssembly(), CoreLibPlugin.NAME);
        RewiredExtensionModule.rewiredStart += () =>
        {
            rewiredPlayer = ReInput.players.GetPlayer(0);
        };
        
        RewiredExtensionModule.AddKeybind(UP_KEY, "Next command", KeyboardKeyCode.UpArrow);
        RewiredExtensionModule.AddKeybind(DOWN_KEY, "Previous command", KeyboardKeyCode.DownArrow);
        RewiredExtensionModule.AddKeybind(COMPLETE_KEY, "Autocomplete command", KeyboardKeyCode.Tab);
        remindAboutHelpCommand = CoreLibPlugin.Instance.Config.Bind(
            "ChatModule",
            "remindAboutHelp",
            true,
            "Should user be reminded about existance of /help command any time a command returns error code output?");
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
    
    
    internal static List<CommandPair> commandHandlers = new List<CommandPair>();

    #endregion
}