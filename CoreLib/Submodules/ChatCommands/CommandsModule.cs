using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CoreLib.Submodules.ChatCommands.Patches;

namespace CoreLib.Submodules.ChatCommands;

/// <summary>
/// This module provides means to add custom chat commands
/// </summary>
[CoreLibSubmodule]
public static class CommandsModule
{
    /// <summary>
    /// Return true if the submodule is loaded.
    /// </summary>
    public static bool Loaded
    {
        get => _loaded;
        internal set => _loaded = value;
    }

    private static bool _loaded;


    [CoreLibSubmoduleInit(Stage = InitStage.SetHooks)]
    internal static void SetHooks()
    {
        CoreLibPlugin.harmony.PatchAll(typeof(ChatWindow_Patch));
    }
    
    [CoreLibSubmoduleInit(Stage = InitStage.PostLoad)]
    internal static void Load()
    {
        AddCommands(Assembly.GetExecutingAssembly(), CoreLibPlugin.NAME);
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
    
    public struct CommandPair
    {
        public IChatCommandHandler handler;
        public string modName;

        public CommandPair(IChatCommandHandler handler, string modName)
        {
            this.handler = handler;
            this.modName = modName;
        }
    }
}