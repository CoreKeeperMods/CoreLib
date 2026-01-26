using System;
using System.Collections.Generic;
using System.Linq;
using CoreLib.Submodule.Command.Data;
using CoreLib.Submodule.Command.Handler;
using CoreLib.Submodule.Command.Interface;
using CoreLib.Submodule.Command.Patch;
using CoreLib.Submodule.Command.System;
using CoreLib.Submodule.Command.Util;
using CoreLib.Submodule.ControlMapping;
using CoreLib.Util;
using PugMod;
using QFSW.QC;
using QFSW.QC.Utilities;
using Rewired;

// ReSharper disable SuspiciousTypeConversion.Global

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Command
{
    /// This module provides means to add custom chat commands
    public class CommandModule : BaseSubmodule
    {
        #region Public Interface

        public const string NAME = "Core Library - Command";
        
        internal static Logger log = new(NAME);

        
        /// Represents the communication system used for handling client-side command transmissions.
        /// <remarks>
        /// This property provides access to the CommandCommSystem instance responsible for
        /// client-side command processing within the CommandsModule. The system facilitates
        /// communication for dispatching, handling, and retrieving client command data.
        /// </remarks>
        public static CommandCommSystem ClientCommSystem => _clientCommSystem;

        /// Represents the communication system used for handling server-side command transmissions.
        /// <remarks>
        /// This property provides access to the CommandCommSystem instance responsible for
        /// server-side command processing within the CommandsModule. The system facilitates
        /// communication for dispatching, handling, and executing server commands, enabling interactions
        /// between server components and connected clients.
        /// </remarks>
        public static CommandCommSystem ServerCommSystem => _serverCommSystem;

        /// Adds all commands from the specified assembly.
        /// <param name="modId">The unique identifier of the mod containing the commands.</param>
        /// <param name="modName">The name of the mod containing the commands.</param>
        public static void AddCommands(long modId, string modName)
        {
            Instance.ThrowIfNotLoaded();
            var commands = API.Reflection.GetTypes(modId)
                .Where(type => typeof(IServerCommandHandler).IsAssignableFrom(type) || typeof(IClientCommandHandler).IsAssignableFrom(type)).ToArray();
            commandHandlers.Capacity += commands.Length;

            foreach (var commandType in commands)
            {
                RegisterCommandHandler(commandType, modName);
            }
        }

        /// Registers a command handler with the specified module name.
        /// <param name="handlerType">The type of the command handler to register.</param>
        /// <param name="modName">The name of the module under which the command handler is being registered.</param>
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
                        log.LogWarning($"Failed to register command handler {handlerType.GetNameChecked()}, because it does not define any trigger names!");
                        return;
                    }
                    
                    RegisterCommand(modName, handler);
                }else if (typeof(IClientCommandHandler).IsAssignableFrom(handlerType))
                {
                    IClientCommandHandler handler = (IClientCommandHandler)Activator.CreateInstance(handlerType);
                    if (handler.GetTriggerNames().Length == 0)
                    {
                        log.LogWarning($"Failed to register command handler {handlerType.GetNameChecked()}, because it does not define any trigger names!");
                        return;
                    }
                    
                    RegisterCommand(modName, handler);
                }
            }
            catch (Exception e)
            {
                log.LogWarning($"Failed to register command {handlerType}!\n{e}");
            }
        }

        /// Unregisters a command handler of the specified type.
        /// <param name="handlerType">The type of the command handler to be unregistered.</param>
        public void UnregisterCommandHandler(Type handlerType)
        {
            ThrowIfNotLoaded();
            if (handlerType == typeof(ICommandInfo) ||
                handlerType == typeof(IServerCommandHandler) ||
                handlerType == typeof(IClientCommandHandler)) return;
            
            commandHandlers.RemoveAll(pair => pair.handler.GetType() == handlerType);
        }

        /// Retrieves the command handler associated with a specific command name.
        /// <param name="commandName">The name of the command to find the handler for.</param>
        /// <param name="commandHandler">
        /// When this method returns, contains the command handler as a <see cref="CommandPair"/>
        /// if the command name is found; otherwise, null.
        /// </param>
        /// <returns>True if a handler for the specified command name is found; otherwise, false.</returns>
        public static bool GetCommandHandler(string commandName, out CommandPair commandHandler)
        {
            Instance.ThrowIfNotLoaded();
            commandHandler = commandHandlers
                .FirstOrDefault(handler => handler
                    .handler.GetTriggerNames()
                    .Any(s => s.Equals(commandName, StringComparison.InvariantCultureIgnoreCase)));
            return commandHandler.handler != null;
        }

        /// Retrieves all server-side command handlers that are associated with the specified command name.
        /// <param name="commandName">The name of the command to match against the trigger names of server command handlers.</param>
        /// <returns>
        /// A collection of server command handlers (<see cref="IServerCommandHandler"/>) where at least one trigger name
        /// matches the specified command name, or an empty collection if no matching handlers are found.
        /// </returns>
        public IEnumerable<IServerCommandHandler> GetServerCommandHandlers(string commandName)
        {
            ThrowIfNotLoaded();
            return commandHandlers
                .Select(pair => pair.ServerHandler)
                .Where(handler => handler != null)
                .Where(handler => handler
                    .GetTriggerNames()
                    .Any(s => s.Equals(commandName, StringComparison.InvariantCultureIgnoreCase)));
        }

        /// Retrieves a collection of client-side command handlers associated with the specified command name.
        /// <param name="commandName">The name of the command to match against the handlers' trigger names.</param>
        /// <returns>An enumerable collection of client command handlers that match the specified command name.</returns>
        public IEnumerable<IClientCommandHandler> GetClientCommandHandlers(string commandName)
        {
            ThrowIfNotLoaded();
            return commandHandlers
                .Select(pair => pair.ClientHandler)
                .Where(handler => handler != null)
                .Where(handler => handler
                    .GetTriggerNames()
                    .Any(s => s.Equals(commandName, StringComparison.InvariantCultureIgnoreCase)));
        }

        #endregion

        #region Private Implementation

        /// Defines the submodules that the current module depends on to function correctly.
        internal override Type[] Dependencies => new[] { typeof(ControlMappingModule) };
        
        /// Provides access to the singleton instance of the CommandsModule class.
        internal static CommandModule Instance => CoreLibMod.GetModuleInstance<CommandModule>();
        
        /// Contains configuration settings specific to the CommandsModule for managing command behavior and permissions.
        internal static CommandModuleSettings settings = new CommandModuleSettings();

        /// Specifies the prefix used to identify and invoke commands within the CommandsModule.
        internal const string COMMAND_PREFIX = "/";

        /// Represents the set of bracket characters used for command processing and validation.
        private static readonly char[] BRACKETS = { '{', '}', '[', ']' };

        /// Represents the Rewired.Player instance utilized for input handling within the CommandsModule.
        internal static Player rewiredPlayer;

        /// Represents the binding key identifier for navigating to the previous command in the command input history.
        internal const string UP_KEY = "CoreLib_UpKey";

        /// Represents the key bind identifier for navigating to the previous command in the command history.
        internal const string DOWN_KEY = "CoreLib_DownKey";

        /// Represents the keybinding that triggers the autocomplete functionality for commands.
        internal const string COMPLETE_KEY = "CoreLib_CompleteKey";

        /// Represents the identifier for the keybind used to toggle the Quantum Console visibility.
        internal const string TOGGLE_QUANTUM_CONSOLE = "CoreLib_ToggleQC";

        /// Represents a collection of command pair instances used for handling client and server command interactions.
        internal static List<CommandPair> commandHandlers = new List<CommandPair>();

        /// Represents a mapping of user-friendly names to corresponding ObjectID values. It is primarily used for resolving
        /// item or object identifiers based on user input or descriptive names.
        internal static Dictionary<string, ObjectID> friendlyNameDict = new Dictionary<string, ObjectID>();

        /// Represents the static instance of the command communication system utilized within the client context.
        private static CommandCommSystem _clientCommSystem;

        /// Represents the server-side communication system utilized for command handling and relay.
        private static CommandCommSystem _serverCommSystem;

        /// Represents the instance of the QuantumConsole used for handling console commands and debugging.
        internal static QuantumConsole quantumConsole;

        /// Applies patches to integrate specific components of the mod, enabling their functionality within the system.
        internal override void SetHooks()
        {
            CoreLibMod.Patch(typeof(ChatWindowPatch));
            CoreLibMod.Patch(typeof(EcsManagerPatch));
            CoreLibMod.Patch(typeof(MenuManagerPatch));
        }

        /// Loads the CommandsModule by locating the associated CoreLib.Commands mod and attempting to load its burst assembly.
        /// <exception cref="Exception">Thrown when the CoreLib.Commands mod cannot be found.</exception>
        internal override void Load()
        {
            base.Load();
            
            //TODO fix burst compilation
            //Mod.TryLoadBurstAssembly();
        }

        /// Performs module-specific operations after the initial loading phase is completed.
        internal override void PostLoad()
        {
            log.LogInfo("Commands Module Post Load");

            LoadConfigData();

            RegisterCommandHandler(typeof(HelpCommandHandler), "Core Lib");
            RegisterCommandHandler(typeof(DirectMessageCommandHandler), "Core Lib");
            ControlMappingModule.rewiredStart += () => { rewiredPlayer = ReInput.players.GetPlayer(0); };

            int catID = ControlMappingModule.AddNewCategory("CoreLib");
            ControlMappingModule.AddKeyboardBind(UP_KEY, KeyboardKeyCode.UpArrow, categoryId: catID);
            ControlMappingModule.AddKeyboardBind(DOWN_KEY, KeyboardKeyCode.DownArrow, categoryId: catID);
            ControlMappingModule.AddKeyboardBind(COMPLETE_KEY, KeyboardKeyCode.Tab, categoryId: catID);
            ControlMappingModule.AddKeyboardBind(TOGGLE_QUANTUM_CONSOLE, KeyboardKeyCode.BackQuote, categoryId: catID);

            API.Client.OnWorldCreated += ClientWorldReady;
            API.Server.OnWorldCreated += ServerWorldReady;
        }

        /// Loads and initializes configuration settings related to the commands module.
        private static void LoadConfigData()
        {
            settings.displayAdditionalHints = CoreLibMod.config.Bind(
                "Commands",
                "DisplayAdditionalHints",
                true,
                "Should user be given hints when errors are found?");

            settings.logAllExecutedCommands = CoreLibMod.config.Bind(
                "Commands",
                "LogAllExecutedCommands",
                true,
                "Should all commands executed be logged to console/log file");

            settings.enableCommandSecurity = CoreLibMod.config.Bind(
                "Commands",
                "EnableCommandSecurity",
                false,
                "Should command security system be enabled? This system can check user permissions, and deny execution of any/specific commands");

            settings.allowUnknownClientCommands = CoreLibMod.config.Bind(
                "Commands",
                "AllowUnknownClientCommands",
                false,
                "Should client commands unknown to the server be allowed to be executed?");

        }

        /// Registers a command handler for a specified module.
        /// <param name="modName">The name of the module the command belongs to.</param>
        /// <param name="handler">The command handler instance implementing the ICommandInfo interface.</param>
        private static void RegisterCommand(string modName, ICommandInfo handler)
        {
            commandHandlers.Add(new CommandPair(handler, modName));

            string triggerName = handler.GetTriggerNames()[0];
            if (settings.userAllowedCommands.ContainsKey(triggerName)) return;
            
            var value = CoreLibMod.config.Bind(
                "CommandPermissions",
                $"{modName}_{triggerName}",
                true,
                $"Are users (IE not admins) allowed to execute {triggerName}?");
            settings.userAllowedCommands[triggerName] = value;
        }

        /// Initializes the client-side command communication system and adds it to the main thread systems.
        private static void ClientWorldReady()
        {
            var world = API.Client.World;
            _clientCommSystem = world.GetOrCreateSystemManaged<CommandCommSystem>();
            API.Client.AddMainThreadSystem(_clientCommSystem);
        }

        /// Initializes the server world by creating or retrieving the Command Communication System
        /// and adding it to the main thread systems for processing server-side commands.
        private static void ServerWorldReady()
        {
            var world = API.Server.World;
            _serverCommSystem = world.GetOrCreateSystemManaged<CommandCommSystem>();
            API.Server.AddMainThreadSystem(_serverCommSystem);
        }

        /// Toggles the visibility of the Quantum Console if it is initialized.
        public static void ToggleQc()
        {
            if (quantumConsole != null)
                quantumConsole.Toggle();
        }

        /// Sends a message to the Quantum Console with the specified text and status.
        /// <param name="text">The message text to be sent to the Quantum Console.</param>
        /// <param name="status">The status of the message, determining its display color in the Quantum Console.</param>
        public static void SendQcMessage(string text, CommandStatus status)
        {
            if (quantumConsole != null)
            {
                quantumConsole.LogToConsole(text.ColorText(status.GetColor()));
            }
        }

        /// Initializes the Quantum Console with the provided console instance.
        /// <param name="console">The QuantumConsole instance to be initialized and linked.</param>
        internal static void InitQuantumConsole(QuantumConsole console)
        {
            quantumConsole = console;
            quantumConsole.OnInvoke += HandleQuantumConsoleCommand;
        }

        /// Handles the execution of a Quantum Console command.
        /// <param name="command">The command string entered the Quantum Console.</param>
        private static void HandleQuantumConsoleCommand(string command)
        {
            if (!command.StartsWith("chat ")) return;
            string args = command.Replace("chat", "").TrimStart();
            SendCommand($"{COMMAND_PREFIX}{args}", true);
        }

        /// Sends a command to the communication system after processing the input and applying the appropriate flags.
        /// <param name="input">The command text input to be sent.</param>
        /// <param name="isQuantumConsole">Indicates whether the command is sent from the Quantum Console.</param>
        /// <returns>
        /// Returns false if the command is successfully processed and sent to the communication system,
        /// otherwise true if no command was sent or the input does not meet the criteria.
        /// </returns>
        internal static bool SendCommand(string input, bool isQuantumConsole = false)
        {
            string[] args = input.Split(' ');
            if (args.Length < 1 || !args[0].StartsWith(COMMAND_PREFIX)) return true;
            if (ClientCommSystem == null) return true;

            var flags = CommandFlags.None;
            
            if (settings.displayAdditionalHints.Value)
                flags |= CommandFlags.UserWantsHints;
            if (isQuantumConsole)
                flags |= CommandFlags.SentFromQuantumConsole;

            ClientCommSystem.SendCommand(input, flags);
            return false;
        }

        /// Handles the execution of server-side commands sent by clients.
        /// <param name="message">The command message sent by the client, containing the command string and its arguments.</param>
        internal static void ServerHandleCommand(CommandMessage message)
        {
            string[] args = message.message.Split(' ');
            if (args.Length < 1 || !args[0].StartsWith(COMMAND_PREFIX)) return;

            string cmdName = args[0].Substring(1);

            if (!GetCommandHandler(cmdName, out CommandPair commandPair))
            {
                if (settings.allowUnknownClientCommands.Value)
                {
                    if (settings.logAllExecutedCommands.Value)
                    {
                        string playerName = message.sender.GetPlayerEntity().GetPlayerName();
                        log.LogInfo($"[{playerName} unknown command relayed]: {message.message}");
                    }
                    
                    if (!CheckCommandPermission(message, commandPair))
                    {
                        _serverCommSystem.SendResponse($"Not enough permissions to execute {cmdName}! Please contact server owner for more info.", CommandStatus.Error, message.commandFlags);
                        return;
                    }
                    _serverCommSystem.SendRelayCommand(message.message);
                    return;
                }
                _serverCommSystem.SendResponse($"Command {cmdName} does not exist!", CommandStatus.Error, message.commandFlags);
                return;
            }

            if (!CheckCommandPermission(message, commandPair))
            {
                _serverCommSystem.SendResponse($"Not enough permissions to execute {cmdName}! Please contact server owner for more info.", CommandStatus.Error, message.commandFlags);
                return;
            }

            if (settings.logAllExecutedCommands.Value)
            {
                string playerName = message.sender.GetPlayerEntity().GetPlayerName();
                log.LogInfo($"[{playerName} executed]: {message.message}");
            }

            if (!commandPair.IsServer)
            {
                _serverCommSystem.SendRelayCommand(message.message);
                return;
            }

            string[] parameters = args.Skip(1).ToArray();

            var result = ExecuteCommand(commandPair, message, parameters);
            foreach (CommandOutput output in result)
            {
                _serverCommSystem.SendResponse(output.feedback, output.status, message.commandFlags);
            }
        }

        /// Checks whether the sender of the command has permission to execute the specified command.
        /// <param name="message">The command message containing information about the sender and the command.</param>
        /// <param name="command">The command pair containing the command handler and associated data.</param>
        /// <returns>True if the sender has the required permissions to execute the command; otherwise, false.</returns>
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
                adminLevel = entityManager.GetComponentData<ConnectionAdminLevelCD>(message.sender).adminPrivileges;
            }

            if (adminLevel > 0)
            {
                return true;
            }
            
            string triggerName = command.handler.GetTriggerNames()[0];

            return settings.userAllowedCommands.TryGetValue(triggerName, out var allowedCommand) && allowedCommand.Value;
        }

        /// Handles the processing of a client command message.
        /// <param name="message">The command message sent by the client containing the command string and associated flags.</param>
        internal static void ClientHandleCommand(CommandMessage message)
        {
            string[] args = message.message.Split(' ');
            if (args.Length < 1 || !args[0].StartsWith(COMMAND_PREFIX)) return;

            string cmdName = args[0].Substring(1);

            if (!GetCommandHandler(cmdName, out CommandPair commandPair))
            {
                _clientCommSystem.AppendMessage(new CommandMessage($"Command {cmdName} does not exist!", CommandStatus.Error, message.commandFlags));
                return;
            }

            if (commandPair.IsServer)
            {
                _clientCommSystem.AppendMessage(new CommandMessage($"Cannot execute {cmdName} locally. It's a server command!", CommandStatus.Error, message.commandFlags));
                return;
            }

            string[] parameters = args.Skip(1).ToArray();
            if (settings.displayAdditionalHints.Value)
                message.commandFlags |= CommandFlags.UserWantsHints;
            
            var result = ExecuteCommand(commandPair, message, parameters);
            foreach (CommandOutput output in result)
            {
                _clientCommSystem.AppendMessage(new CommandMessage(output, message.commandFlags));
            }
        }

        /// Executes the specified command with the provided message and parameters.
        /// <param name="command">The command to be executed, including its handler and metadata.</param>
        /// <param name="message">The message containing details about the command being executed.</param>
        /// <param name="parameters">An array of parameters to pass to the command during execution.</param>
        /// <returns>A list of command outputs representing the execution result, including error messages or hints if applicable.</returns>
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
                    if (BRACKETS.Any(c => message.message.Contains(c)))
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
                log.LogWarning($"Error executing command {cmdName}:\n{e}");

                result.Add(new CommandOutput($"Error executing command {cmdName}", CommandStatus.Error));
            }

            return result;
        }

        #endregion
    }
}