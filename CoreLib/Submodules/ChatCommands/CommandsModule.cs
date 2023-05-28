using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using CoreLib.Submodules.ChatCommands.Patches;
using CoreLib.Submodules.RewiredExtension;
using CoreLib.Submodules.Security;
using Rewired;
// ReSharper disable SuspiciousTypeConversion.Global

namespace CoreLib.Submodules.ChatCommands;

/// <summary>
/// This module provides means to add custom chat commands
/// </summary>
[CoreLibSubmodule(Dependencies = new []{typeof(RewiredExtensionModule), typeof(SecurityModule)})]
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
            RegisterCommandHandler(commandType, modName);
        }
    }

    public static void RegisterCommandHandler(Type commandType, string modName)
    {
        if (commandType == typeof(IChatCommandHandler)) return;

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

    [Obsolete]
    public static bool GetCommandHandler(string commandName, out IChatCommandHandler commandHandler)
    {
        bool result = GetCommandHandler(commandName, out CommandPair pair);
        commandHandler = pair.handler;
        return result;
    }
    
    public static bool GetCommandHandler(string commandName, out CommandPair commandHandler)
    {
        commandHandler = commandHandlers
            .FirstOrDefault(handler => handler
                .handler.GetTriggerNames()
                .Any(s => s.Equals(commandName, StringComparison.InvariantCultureIgnoreCase)));
        return commandHandler.handler != null;
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

    internal static CommandInfo currentCommandInfo;
    internal static List<CommandPair> commandHandlers = new List<CommandPair>();

    internal static CommandPair FindCommand(CommandInfo commandInfo)
    {
        return commandHandlers.Where(pair =>
        {
            return pair.modName.Equals(commandInfo.modId) &&
                   pair.handler.GetType().Name.Equals(commandInfo.id);
        }).FirstOrDefault();
    } 
    
    internal static CommandKind DetermineCommandKind(CommandPair commandPair)
    {
        if (commandPair.handler is ICommandKind kindData)
            return kindData.commandKind;
        
        return CommandKind.Cheat;
    }

    [CoreLibSubmoduleInit(Stage = InitStage.SetHooks)]
    internal static void SetHooks()
    {
        CoreLibPlugin.harmony.PatchAll(typeof(ChatWindow_Patch));
        CoreLibPlugin.harmony.PatchAll(typeof(TitleScreenAnimator_Patch));
    }
    
    [CoreLibSubmoduleInit(Stage = InitStage.PostLoad)]
    internal static void Load()
    {
        RegisterCommandHandler(typeof(HelpCommandHandler), CoreLibPlugin.NAME);
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

    #endregion
}