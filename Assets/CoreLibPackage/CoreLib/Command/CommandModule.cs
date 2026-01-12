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
using CoreLib.Util.Extension;
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

        public new const string Name = "Core Library - Command";
        
        internal new static Logger Log = new(Name);

        
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
            CommandHandlers.Capacity += commands.Length;

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
                        Log.LogWarning($"Failed to register command handler {handlerType.GetNameChecked()}, because it does not define any trigger names!");
                        return;
                    }
                    
                    RegisterCommand(modName, handler);
                }else if (typeof(IClientCommandHandler).IsAssignableFrom(handlerType))
                {
                    IClientCommandHandler handler = (IClientCommandHandler)Activator.CreateInstance(handlerType);
                    if (handler.GetTriggerNames().Length == 0)
                    {
                        Log.LogWarning($"Failed to register command handler {handlerType.GetNameChecked()}, because it does not define any trigger names!");
                        return;
                    }
                    
                    RegisterCommand(modName, handler);
                }
            }
            catch (Exception e)
            {
                Log.LogWarning($"Failed to register command {handlerType}!\n{e}");
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
            
            CommandHandlers.RemoveAll(pair => pair.Handler.GetType() == handlerType);
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
            commandHandler = CommandHandlers
                .FirstOrDefault(handler => handler
                    .Handler.GetTriggerNames()
                    .Any(s => s.Equals(commandName, StringComparison.InvariantCultureIgnoreCase)));
            return commandHandler.Handler != null;
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
            return CommandHandlers
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
            return CommandHandlers
                .Select(pair => pair.ClientHandler)
                .Where(handler => handler != null)
                .Where(handler => handler
                    .GetTriggerNames()
                    .Any(s => s.Equals(commandName, StringComparison.InvariantCultureIgnoreCase)));
        }

        #endregion

        #region Private Implementation

        /// Defines the submodules that the current module depends on to function correctly.
        /// <remarks>
        /// This property specifies an array of module types that represent required dependencies
        /// for the current module's operation. Ensuring these dependencies are met is critical
        /// for maintaining proper interaction and functionality within the module system.
        /// </remarks>
        internal override Type[] Dependencies => new[] { typeof(ControlMappingModule) };
        /// Provides access to the singleton instance of the CommandsModule class.
        /// <remarks>
        /// This property retrieves the instance of the CommandsModule, facilitating access to the module's methods, settings,
        /// and command handling capabilities. It ensures that all operations within the CommandsModule are properly routed
        /// through the centralized instance.
        /// </remarks>
        internal static CommandModule Instance => CoreLibMod.GetModuleInstance<CommandModule>();
        /// Contains configuration settings specific to the CommandsModule for managing command behavior and permissions.
        /// <remarks>
        /// This field stores an instance of CommandModuleSettings, which encapsulates various options and configurations
        /// related to command execution, logging, permissions, and security within the CommandsModule. These settings
        /// are loaded and managed to determine runtime behavior of client and server commands.
        /// </remarks>
        internal static CommandModuleSettings Settings = new CommandModuleSettings();

        /// Specifies the prefix used to identify and invoke commands within the CommandsModule.
        /// <remarks>
        /// This constant string represents the character or sequence that must precede any command
        /// to differentiate it from regular text input. It is primarily utilized in processing
        /// commands for both the client and server communication systems.
        /// </remarks>
        internal const string CommandPrefix = "/";

        /// Represents the set of bracket characters used for command processing and validation.
        /// <remarks>
        /// This field defines a collection of bracket characters, including curly braces '{', '}',
        /// and square brackets '[', ']', which are used as delimiters or syntax elements within
        /// various command processing routines in the CommandsModule. These characters are
        /// commonly employed to detect or validate specific structures in incoming command messages.
        /// </remarks>
        private static readonly char[] Brackets = { '{', '}', '[', ']' };

        /// Represents the Rewired.Player instance utilized for input handling within the CommandsModule.
        /// <remarks>
        /// This variable is internally initialized to retrieve the primary input player from the Rewired Input library.
        /// It serves as the input abstraction for managing interactions related to the CommandsModule, such as command toggling
        /// and navigation inputs.
        /// </remarks>
        internal static Player RewiredPlayer;

        /// Represents the binding key identifier for navigating to the previous command in the command input history.
        /// <remarks>
        /// This variable is utilized in the command-handling system to map the "up navigation" functionality,
        /// allowing users to cycle backward through the history of previously entered commands.
        /// It is often bound to a specific input, such as the Up Arrow key, for intuitive navigation.
        /// </remarks>
        internal static string UpKey = "CoreLib_UpKey";

        /// Represents the key bind identifier for navigating to the previous command in the command history.
        /// <remarks>
        /// This variable is used within the Rewired input system to bind a specific key for accessing the
        /// previous command in the user's input history. It is primarily utilized to improve the usability
        /// of command entry fields, allowing users to quickly recall past commands.
        /// </remarks>
        internal static string DownKey = "CoreLib_DownKey";

        /// Represents the keybinding that triggers the autocomplete functionality for commands.
        /// <remarks>
        /// This keybind is associated with the "CoreLib_CompleteKey" action and is intended
        /// to provide a shortcut for autocomplete operations within the command system.
        /// It is utilized in conjunction with the Rewired API to detect input and execute the
        /// corresponding behavior, such as completing partial command inputs in the chat window.
        /// </remarks>
        internal static string CompleteKey = "CoreLib_CompleteKey";

        /// Represents the identifier for the keybind used to toggle the Quantum Console visibility.
        /// <remarks>
        /// This constant is a string value used as a key in the Rewired input system for triggering
        /// the Quantum Console toggle functionality. It is primarily configured and registered
        /// during the CommandsModule initialization and utilized for handling player input in relevant modules.
        /// </remarks>
        internal static string ToggleQuantumConsole = "CoreLib_ToggleQC";

        /// Represents a collection of command pair instances used for handling client and server command interactions.
        /// <remarks>
        /// This field contains a list of CommandPair objects, each encapsulating both client-side and server-side
        /// handlers for various commands. It serves as the core storage for registering, retrieving, and managing
        /// command handlers within the CommandsModule. The list dynamically adjusts its capacity based on the
        /// registered commands and is crucial for facilitating communication and command execution.
        /// </remarks>
        internal static List<CommandPair> CommandHandlers = new List<CommandPair>();

        /// Represents a mapping of user-friendly names to corresponding ObjectID values.
        /// <remarks>
        /// This dictionary facilitates the association between user-friendly text identifiers and their
        /// corresponding ObjectID enums within the CommandsModule. It is primarily used for resolving
        /// item or object identifiers based on user input or descriptive names. The mapping is accessed
        /// and modified in various systems and utilities, such as during item dictionary loading or
        /// command parsing operations.
        /// </remarks>
        internal static Dictionary<string, ObjectID> FriendlyNameDict = new Dictionary<string, ObjectID>();

        /// Represents the static instance of the command communication system utilized within the client context.
        /// <remarks>
        /// This variable is used internally by the CommandsModule to manage the lifecycle and operations
        /// of the CommandCommSystem instance. It is responsible for handling communication processes,
        /// such as appending messages or interacting with commands specific to the client.
        /// The system is initialized during client world readiness.
        /// </remarks>
        private static CommandCommSystem _clientCommSystem;

        /// Represents the server-side communication system utilized for command handling and relay.
        /// <remarks>
        /// This variable provides an instance of the CommandCommSystem class, which is primarily responsible
        /// for processing server-related command exchanges within the CommandsModule. It facilitates
        /// tasks such as sending responses, relaying commands, and ensuring proper communication flow
        /// during server operations.
        /// </remarks>
        private static CommandCommSystem _serverCommSystem;

        /// Represents the instance of the QuantumConsole used for handling console commands and debugging.
        /// <remarks>
        /// This variable is used to reference the QuantumConsole component within the application, providing
        /// the ability to toggle its visibility and functionality during runtime. It is essential for managing
        /// developer tools and command execution associated with the CommandsModule.
        /// </remarks>
        internal static QuantumConsole QuantumConsole;

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
            
            Mod.TryLoadBurstAssembly();
        }

        /// Performs module-specific operations after the initial loading phase is completed.
        internal override void PostLoad()
        {
            Log.LogInfo("Commands Module Post Load");

            LoadConfigData();

            RegisterCommandHandler(typeof(HelpCommandHandler), "Core Lib");
            RegisterCommandHandler(typeof(DirectMessageCommandHandler), "Core Lib");
            ControlMappingModule.RewiredStart += () => { RewiredPlayer = ReInput.players.GetPlayer(0); };

            //ControlMappingModule.AddKeybind(UpKey, "Next command", KeyboardKeyCode.UpArrow);
            //ControlMappingModule.AddKeybind(DownKey, "Previous command", KeyboardKeyCode.DownArrow);
            //ControlMappingModule.AddKeybind(CompleteKey, "Autocomplete command", KeyboardKeyCode.Tab);
            
            //ControlMappingModule.AddKeybind(ToggleQuantumConsole, "Toggle Quantum console", KeyboardKeyCode.BackQuote);

            API.Client.OnWorldCreated += ClientWorldReady;
            API.Server.OnWorldCreated += ServerWorldReady;
        }

        /// Loads and initializes configuration settings related to the commands module.
        private static void LoadConfigData()
        {
            Settings.DisplayAdditionalHints = API.Config.Register(CoreLibMod.ID,
                "Commands",
                "Should user be given hints when errors are found?",
                "DisplayAdditionalHints",
                true);

            Settings.LOGAllExecutedCommands = API.Config.Register(CoreLibMod.ID,
                "Commands",
                "Should all commands executed be logged to console/log file",
                "LogAllExecutedCommands",
                true);

            Settings.EnableCommandSecurity = API.Config.Register(CoreLibMod.ID,
                "Commands",
                "Should command security system be enabled? This system can check user permissions, and deny execution of any/specific commands",
                "EnableCommandSecurity",
                false);

            Settings.AllowUnknownClientCommands = API.Config.Register(CoreLibMod.ID,
                "Commands",
                "Should client commands unknown to the server be allowed to be executed?",
                "AllowUnknownClientCommands",
                false);
        }

        /// Registers a command handler for a specified module.
        /// <param name="modName">The name of the module the command belongs to.</param>
        /// <param name="handler">The command handler instance implementing the ICommandInfo interface.</param>
        private static void RegisterCommand(string modName, ICommandInfo handler)
        {
            CommandHandlers.Add(new CommandPair(handler, modName));

            string triggerName = handler.GetTriggerNames()[0];
            if (Settings.UserAllowedCommands.ContainsKey(triggerName)) return;
            
            var value = API.Config.Register(CoreLibMod.ID,
                "CommandPermissions",
                $"Are users (IE not admins) allowed to execute {triggerName}?",
                $"{modName}_{triggerName}",
                true);
            Settings.UserAllowedCommands[triggerName] = value;
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
            if (QuantumConsole != null)
                QuantumConsole.Toggle();
        }

        /// Sends a message to the Quantum Console with the specified text and status.
        /// <param name="text">The message text to be sent to the Quantum Console.</param>
        /// <param name="status">The status of the message, determining its display color in the Quantum Console.</param>
        public static void SendQcMessage(string text, CommandStatus status)
        {
            if (QuantumConsole != null)
            {
                QuantumConsole.LogToConsole(text.ColorText(status.GetColor()));
            }
        }

        /// Initializes the Quantum Console with the provided console instance.
        /// <param name="console">The QuantumConsole instance to be initialized and linked.</param>
        internal static void InitQuantumConsole(QuantumConsole console)
        {
            QuantumConsole = console;
            QuantumConsole.OnInvoke += HandleQuantumConsoleCommand;
        }

        /// Handles the execution of a Quantum Console command.
        /// <param name="command">The command string entered the Quantum Console.</param>
        private static void HandleQuantumConsoleCommand(string command)
        {
            if (!command.StartsWith("chat ")) return;
            string args = command.Replace("chat", "").TrimStart();
            SendCommand($"{CommandPrefix}{args}", true);
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
            if (args.Length < 1 || !args[0].StartsWith(CommandPrefix)) return true;
            if (ClientCommSystem == null) return true;

            var flags = CommandFlags.None;
            
            if (Settings.DisplayAdditionalHints.Value)
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
            string[] args = message.Message.Split(' ');
            if (args.Length < 1 || !args[0].StartsWith(CommandPrefix)) return;

            string cmdName = args[0].Substring(1);

            if (!GetCommandHandler(cmdName, out CommandPair commandPair))
            {
                if (Settings.AllowUnknownClientCommands.Value)
                {
                    if (Settings.LOGAllExecutedCommands.Value)
                    {
                        string playerName = message.Sender.GetPlayerEntity().GetPlayerName();
                        Log.LogInfo($"[{playerName} unknown command relayed]: {message.Message}");
                    }
                    
                    if (!CheckCommandPermission(message, commandPair))
                    {
                        _serverCommSystem.SendResponse($"Not enough permissions to execute {cmdName}! Please contact server owner for more info.", CommandStatus.Error, message.CommandFlags);
                        return;
                    }
                    _serverCommSystem.SendRelayCommand(message.Message);
                    return;
                }
                _serverCommSystem.SendResponse($"Command {cmdName} does not exist!", CommandStatus.Error, message.CommandFlags);
                return;
            }

            if (!CheckCommandPermission(message, commandPair))
            {
                _serverCommSystem.SendResponse($"Not enough permissions to execute {cmdName}! Please contact server owner for more info.", CommandStatus.Error, message.CommandFlags);
                return;
            }

            if (Settings.LOGAllExecutedCommands.Value)
            {
                string playerName = message.Sender.GetPlayerEntity().GetPlayerName();
                Log.LogInfo($"[{playerName} executed]: {message.Message}");
            }

            if (!commandPair.IsServer)
            {
                _serverCommSystem.SendRelayCommand(message.Message);
                return;
            }

            string[] parameters = args.Skip(1).ToArray();

            var result = ExecuteCommand(commandPair, message, parameters);
            foreach (CommandOutput output in result)
            {
                _serverCommSystem.SendResponse(output.Feedback, output.Status, message.CommandFlags);
            }
        }

        /// Checks whether the sender of the command has permission to execute the specified command.
        /// <param name="message">The command message containing information about the sender and the command.</param>
        /// <param name="command">The command pair containing the command handler and associated data.</param>
        /// <returns>True if the sender has the required permissions to execute the command; otherwise, false.</returns>
        internal static bool CheckCommandPermission(CommandMessage message, CommandPair command)
        {
            if (!Settings.EnableCommandSecurity.Value) return true;
            var entityManager = API.Server.World.EntityManager;
            if (!entityManager.Exists(message.Sender)) return false;
            
            bool guestMode = entityManager.World.GetExistingSystemManaged<WorldInfoSystem>().WorldInfo.guestMode;
            if (guestMode) return false;
            int adminLevel = 0;
            
            if (entityManager.HasComponent<ConnectionAdminLevelCD>(message.Sender))
            {
                adminLevel = entityManager.GetComponentData<ConnectionAdminLevelCD>(message.Sender).adminPrivileges;
            }

            if (adminLevel > 0)
            {
                return true;
            }
            
            string triggerName = command.Handler.GetTriggerNames()[0];

            return Settings.UserAllowedCommands.TryGetValue(triggerName, out var allowedCommand) && allowedCommand.Value;
        }

        /// Handles the processing of a client command message.
        /// <param name="message">The command message sent by the client containing the command string and associated flags.</param>
        internal static void ClientHandleCommand(CommandMessage message)
        {
            string[] args = message.Message.Split(' ');
            if (args.Length < 1 || !args[0].StartsWith(CommandPrefix)) return;

            string cmdName = args[0].Substring(1);

            if (!GetCommandHandler(cmdName, out CommandPair commandPair))
            {
                _clientCommSystem.AppendMessage(new CommandMessage($"Command {cmdName} does not exist!", CommandStatus.Error, message.CommandFlags));
                return;
            }

            if (commandPair.IsServer)
            {
                _clientCommSystem.AppendMessage(new CommandMessage($"Cannot execute {cmdName} locally. It's a server command!", CommandStatus.Error, message.CommandFlags));
                return;
            }

            string[] parameters = args.Skip(1).ToArray();
            if (Settings.DisplayAdditionalHints.Value)
                message.CommandFlags |= CommandFlags.UserWantsHints;
            
            var result = ExecuteCommand(commandPair, message, parameters);
            foreach (CommandOutput output in result)
            {
                _clientCommSystem.AppendMessage(new CommandMessage(output, message.CommandFlags));
            }
        }

        /// Executes the specified command with the provided message and parameters.
        /// <param name="command">The command to be executed, including its handler and metadata.</param>
        /// <param name="message">The message containing details about the command being executed.</param>
        /// <param name="parameters">An array of parameters to pass to the command during execution.</param>
        /// <returns>A list of command outputs representing the execution result, including error messages or hints if applicable.</returns>
        internal static List<CommandOutput> ExecuteCommand(CommandPair command, CommandMessage message, string[] parameters)
        {
            string cmdName = command.Handler.GetTriggerNames().First();
            var result = new List<CommandOutput>();
            try
            {
                CommandOutput output = command.Execute(message, parameters);
                result.Add(output);
                
                if (output.Status == CommandStatus.Error && message.CommandFlags.HasFlag(CommandFlags.UserWantsHints))
                {
                    if (Brackets.Any(c => message.Message.Contains(c)))
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
                Log.LogWarning($"Error executing command {cmdName}:\n{e}");

                result.Add(new CommandOutput($"Error executing command {cmdName}", CommandStatus.Error));
            }

            return result;
        }

        #endregion
    }
}