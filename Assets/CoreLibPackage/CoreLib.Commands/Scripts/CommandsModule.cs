using System;
using System.Collections.Generic;
using System.Linq;
using CoreLib.Commands.Communication;
using CoreLib.Commands.Handlers;
using CoreLib.Commands.Patches;
using CoreLib.RewiredExtension;
using CoreLib.Util.Extensions;
using Newtonsoft.Json;
using PugMod;
using QFSW.QC;
using QFSW.QC.Utilities;
using Rewired;
using Unity.Burst;
using UnityEngine;
using UnityEngine.Scripting;

// ReSharper disable SuspiciousTypeConversion.Global

namespace CoreLib.Commands
{
    /// <summary>
    /// This module provides means to add custom chat commands
    /// </summary>
    public class CommandsModule : BaseSubmodule
    {
        #region Public Interface

        public static CommandCommSystem ClientCommSystem => clientCommSystem;
        public static CommandCommSystem ServerCommSystem => serverCommSystem;

        /// <summary>
        /// Add all commands from specified assembly
        /// </summary>
        public static void AddCommands(long modId, string modName)
        {
            Instance.ThrowIfNotLoaded();
            Type[] commands = API.Reflection.GetTypes(modId).Where(type =>
            {
                return typeof(IServerCommandHandler).IsAssignableFrom(type) ||
                       typeof(IClientCommandHandler).IsAssignableFrom(type);
            }).ToArray();
            commandHandlers.Capacity += commands.Length;

            foreach (Type commandType in commands)
            {
                RegisterCommandHandler(commandType, modName);
            }
        }

        public static void RegisterCommandHandler(Type handlerType, string modName)
        {
            Instance.ThrowIfNotLoaded();
            if (handlerType == typeof(ICommandInfo) ||
                handlerType == typeof(IServerCommandHandler) ||
                handlerType == typeof(IClientCommandHandler)) return;

            try
            {
                if (typeof(IServerCommandHandler).IsAssignableFrom(handlerType))
                {
                    IServerCommandHandler handler = (IServerCommandHandler)Activator.CreateInstance(handlerType);
                    if (handler.GetTriggerNames().Length == 0)
                    {
                        CoreLibMod.Log.LogWarning($"Failed to register command handler {handlerType.GetNameChecked()}, because it does not define any trigger names!");
                        return;
                    }
                    
                    RegisterCommand(modName, handler);
                }else if (typeof(IClientCommandHandler).IsAssignableFrom(handlerType))
                {
                    IClientCommandHandler handler = (IClientCommandHandler)Activator.CreateInstance(handlerType);
                    if (handler.GetTriggerNames().Length == 0)
                    {
                        CoreLibMod.Log.LogWarning($"Failed to register command handler {handlerType.GetNameChecked()}, because it does not define any trigger names!");
                        return;
                    }
                    
                    RegisterCommand(modName, handler);
                }
            }
            catch (Exception e)
            {
                CoreLibMod.Log.LogWarning($"Failed to register command {handlerType}!\n{e}");
            }
        }

        public static void UnregisterCommandHandler(Type handlerType)
        {
            Instance.ThrowIfNotLoaded();
            if (handlerType == typeof(ICommandInfo) ||
                handlerType == typeof(IServerCommandHandler) ||
                handlerType == typeof(IClientCommandHandler)) return;
            
            commandHandlers.RemoveAll(pair => pair.handler.GetType() == handlerType);
        }

        public static bool GetCommandHandler(string commandName, out CommandPair commandHandler)
        {
            Instance.ThrowIfNotLoaded();
            commandHandler = commandHandlers
                .FirstOrDefault(handler => handler
                    .handler.GetTriggerNames()
                    .Any(s => s.Equals(commandName, StringComparison.InvariantCultureIgnoreCase)));
            return commandHandler.handler != null;
        }

        public static IEnumerable<IServerCommandHandler> GetServerCommandHandlers(string commandName)
        {
            Instance.ThrowIfNotLoaded();
            return commandHandlers
                .Select(pair => pair.serverHandler)
                .Where(handler => handler != null)
                .Where(handler => handler
                    .GetTriggerNames()
                    .Any(s => s.Equals(commandName, StringComparison.InvariantCultureIgnoreCase)));
        }
        
        public static IEnumerable<IClientCommandHandler> GetClientCommandHandlers(string commandName)
        {
            Instance.ThrowIfNotLoaded();
            return commandHandlers
                .Select(pair => pair.clientHandler)
                .Where(handler => handler != null)
                .Where(handler => handler
                    .GetTriggerNames()
                    .Any(s => s.Equals(commandName, StringComparison.InvariantCultureIgnoreCase)));
        }

        #endregion

        #region Private Implementation

        internal override GameVersion Build => new GameVersion(0, 7, 3, "a28f");
        internal override string Version => "3.1.0";
        internal override Type[] Dependencies => new[] { typeof(RewiredExtensionModule) };
        internal static CommandsModule Instance => CoreLibMod.GetModuleInstance<CommandsModule>();

        internal static CommandModuleSettings settings = new CommandModuleSettings();
        
        internal const string CommandPrefix = "/";
        private static readonly char[] brackets = { '{', '}', '[', ']' };
        
        internal static Player rewiredPlayer;
        
        internal static string UP_KEY = "CoreLib_UpKey";
        internal static string DOWN_KEY = "CoreLib_DownKey";
        internal static string COMPLETE_KEY = "CoreLib_CompleteKey";

        internal static List<CommandPair> commandHandlers = new List<CommandPair>();
        internal static Dictionary<string, ObjectID> friendlyNameDict = new Dictionary<string, ObjectID>();

        private static CommandCommSystem clientCommSystem;
        private static CommandCommSystem serverCommSystem;
        
        internal static QuantumConsole quantumConsole;

        internal override void SetHooks()
        {
            CoreLibMod.Patch(typeof(ChatWindow_Patch));
            CoreLibMod.Patch(typeof(TitleScreenAnimator_Patch));
            CoreLibMod.Patch(typeof(ECSManager_Patch));
            CoreLibMod.Patch(typeof(MenuManager_Patch));
        }

        internal override void Load()
        {
            var mod = API.ModLoader.LoadedMods.FirstOrDefault(loadedMod => loadedMod.Metadata.name.Equals("CoreLib.Commands"));
            if (mod == null)
            {
                CoreLibMod.Log.LogError("Failed to find CoreLib.Commands mod info!");
                throw new Exception("Burst Load Failed!");
            }
            
            mod.TryLoadBurstAssembly();
        }

        internal override void PostLoad()
        {
            CoreLibMod.Log.LogInfo("Commands Module Post Load");

            LoadConfigData();

            RegisterCommandHandler(typeof(HelpCommandHandler), "Core Lib");
            RegisterCommandHandler(typeof(DirectMessageCommandHandler), "Core Lib");
            RewiredExtensionModule.rewiredStart += () => { rewiredPlayer = ReInput.players.GetPlayer(0); };

            RewiredExtensionModule.AddKeybind(UP_KEY, "Next command", KeyboardKeyCode.UpArrow);
            RewiredExtensionModule.AddKeybind(DOWN_KEY, "Previous command", KeyboardKeyCode.DownArrow);
            RewiredExtensionModule.AddKeybind(COMPLETE_KEY, "Autocomplete command", KeyboardKeyCode.Tab);

            API.Client.OnWorldCreated += ClientWorldReady;
            API.Server.OnWorldCreated += ServerWorldReady;
        }

        private static void LoadConfigData()
        {
            settings.displayAdditionalHints = CoreLibMod.Config.Bind(
                "Commands",
                "DisplayAdditionalHints",
                true,
                "Should user be given hints when errors are found?");

            settings.logAllExecutedCommands = CoreLibMod.Config.Bind(
                "Commands",
                "LogAllExecutedCommands",
                true,
                "Should all commands executed be logged to console/log file");

            settings.enableCommandSecurity = CoreLibMod.Config.Bind(
                "Commands",
                "EnableCommandSecurity",
                false,
                "Should command security system be enabled? This system can check user permissions, and deny execution of any/specific commands");

            settings.allowUnknownClientCommands = CoreLibMod.Config.Bind(
                "Commands",
                "AllowUnknownClientCommands",
                false,
                "Should client commands unknown to the server be allowed to be executed?");
        }
        
        private static void RegisterCommand(string modName, ICommandInfo handler)
        {
            commandHandlers.Add(new CommandPair(handler, modName));

            string triggerName = handler.GetTriggerNames()[0];
            if (settings.userAllowedCommands.ContainsKey(triggerName)) return;
            
            var value = CoreLibMod.Config.Bind(
                "CommandPermissions",
                $"{modName}_{triggerName}",
                true,
                $"Are users (IE not admins) allowed to execute {triggerName}?");
            settings.userAllowedCommands[triggerName] = value;
        }

        private static void ClientWorldReady()
        {
            var world = API.Client.World;
            clientCommSystem = world.GetOrCreateSystemManaged<CommandCommSystem>();
            API.Client.AddMainThreadSystem(clientCommSystem);
        }

        private static void ServerWorldReady()
        {
            var world = API.Server.World;
            serverCommSystem = world.GetOrCreateSystemManaged<CommandCommSystem>();
            API.Server.AddMainThreadSystem(serverCommSystem);
        }

        public static void ToggleQC()
        {
            if (quantumConsole != null)
                quantumConsole.Toggle();
        }

        public static void SendQCMessage(string text, CommandStatus status)
        {
            if (quantumConsole != null)
            {
                quantumConsole.LogToConsole(text.ColorText(status.GetColor()));
            }
        }

        internal static void InitQuantumConsole(QuantumConsole console)
        {
            quantumConsole = console;
            quantumConsole.OnInvoke += HandleQuantumeConsoleCommand;
        }

        private static void HandleQuantumeConsoleCommand(string command)
        {
            if (command.StartsWith("chat "))
            {
                var args = command.Replace("chat", "").TrimStart();
                SendCommand($"{CommandPrefix}{args}", true);
            }
        }

        internal static bool SendCommand(string input, bool isQuantumConsole = false)
        {
            string[] args = input.Split(' ');
            if (args.Length < 1 || !args[0].StartsWith(CommandPrefix)) return true;
            if (ClientCommSystem == null) return true;

            CommandFlags flags = CommandFlags.None;
            
            if (settings.displayAdditionalHints.Value)
                flags |= CommandFlags.UserWantsHints;
            if (isQuantumConsole)
                flags |= CommandFlags.SentFromQuantumConsole;

            ClientCommSystem.SendCommand(input, flags);
            return false;
        }

        internal static void ServerHandleCommand(CommandMessage message)
        {
            string[] args = message.message.Split(' ');
            if (args.Length < 1 || !args[0].StartsWith(CommandPrefix)) return;

            string cmdName = args[0].Substring(1);

            if (!GetCommandHandler(cmdName, out CommandPair commandPair))
            {
                if (settings.allowUnknownClientCommands.Value)
                {
                    if (settings.logAllExecutedCommands.Value)
                    {
                        string playerName = message.sender.GetPlayerEntity().GetPlayerName();
                        CoreLibMod.Log.LogInfo($"[{playerName} unknown command relayed]: {message.message}");
                    }
                    
                    if (!CheckCommandPermission(message, commandPair))
                    {
                        serverCommSystem.SendResponse($"Not enough permissions to execute {cmdName}! Please contact server owner for more info.", CommandStatus.Error, message.commandFlags);
                        return;
                    }
                    serverCommSystem.SendRelayCommand(message.message);
                    return;
                }
                serverCommSystem.SendResponse($"Command {cmdName} does not exist!", CommandStatus.Error, message.commandFlags);
                return;
            }

            if (!CheckCommandPermission(message, commandPair))
            {
                serverCommSystem.SendResponse($"Not enough permissions to execute {cmdName}! Please contact server owner for more info.", CommandStatus.Error, message.commandFlags);
                return;
            }

            if (settings.logAllExecutedCommands.Value)
            {
                string playerName = message.sender.GetPlayerEntity().GetPlayerName();
                CoreLibMod.Log.LogInfo($"[{playerName} executed]: {message.message}");
            }

            if (!commandPair.isServer)
            {
                serverCommSystem.SendRelayCommand(message.message);
                return;
            }

            string[] parameters = args.Skip(1).ToArray();

            var result = ExecuteCommand(commandPair, message, parameters);
            foreach (CommandOutput output in result)
            {
                serverCommSystem.SendResponse(output.feedback, output.status, message.commandFlags);
            }
        }

        internal static bool CheckCommandPermission(CommandMessage message, CommandPair command)
        {
            if (!settings.enableCommandSecurity.Value) return true;
            var entityManager = API.Server.World.EntityManager;
            if (!entityManager.Exists(message.sender)) return false;
            
            bool guestMode = entityManager.World.GetExistingSystemManaged<WorldInfoSystem>().WorldInfo.guestMode;
            if (guestMode) return false;
            int adminLevel = 0;
            
            if (entityManager.HasComponent<ConnectionAdminLevelCD>(message.sender))
            {
                adminLevel = entityManager.GetComponentData<ConnectionAdminLevelCD>(message.sender).Value;
            }

            if (adminLevel > 0)
            {
                return true;
            }
            
            string triggerName = command.handler.GetTriggerNames()[0];

            if (settings.userAllowedCommands.ContainsKey(triggerName))
            {
                return settings.userAllowedCommands[triggerName].Value;
            }
            return false;
        }
        
        internal static void ClientHandleCommand(CommandMessage message)
        {
            string[] args = message.message.Split(' ');
            if (args.Length < 1 || !args[0].StartsWith(CommandPrefix)) return;

            string cmdName = args[0].Substring(1);

            if (!GetCommandHandler(cmdName, out CommandPair commandPair))
            {
                clientCommSystem.AppendMessage(new CommandMessage($"Command {cmdName} does not exist!", CommandStatus.Error, message.commandFlags));
                return;
            }

            if (commandPair.isServer)
            {
                clientCommSystem.AppendMessage(new CommandMessage($"Cannot execute {cmdName} locally. It's a server command!", CommandStatus.Error, message.commandFlags));
                return;
            }

            string[] parameters = args.Skip(1).ToArray();
            if (settings.displayAdditionalHints.Value)
                message.commandFlags |= CommandFlags.UserWantsHints;
            
            var result = ExecuteCommand(commandPair, message, parameters);
            foreach (CommandOutput output in result)
            {
                clientCommSystem.AppendMessage(new CommandMessage(output, message.commandFlags));
            }
        }
        
        internal static List<CommandOutput> ExecuteCommand(CommandPair command, CommandMessage message, string[] parameters)
        {
            string cmdName = command.handler.GetTriggerNames().First();
            var result = new List<CommandOutput>();
            try
            {
                CommandOutput output = command.Execute(message, parameters);
                result.Add(output);
                
                if (output.status == CommandStatus.Error && message.commandFlags.HasFlag(CommandFlags.UserWantsHints))
                {
                    if (brackets.Any(c => message.message.Contains(c)))
                    {
                        result.Add(new CommandOutput("Do not use brackets in your command! Brackets are meant as placeholder name separators only.",
                            CommandStatus.Hint));
                    }
                    else
                    {
                        result.Add(new CommandOutput($"Use /help {cmdName} to learn command usage!", CommandStatus.Hint));
                    }
                }
            }
            catch (Exception e)
            {
                CoreLibMod.Log.LogWarning($"Error executing command {cmdName}:\n{e}");

                result.Add(new CommandOutput($"Error executing command {cmdName}", CommandStatus.Error));
            }

            return result;
        }

        #endregion
    }
}