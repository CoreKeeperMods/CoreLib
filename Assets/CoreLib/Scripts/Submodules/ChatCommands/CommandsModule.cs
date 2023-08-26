using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CoreLib.Submodules.ChatCommands.Communication;
using CoreLib.Submodules.ChatCommands.Patches;
using CoreLib.Submodules.RewiredExtension;
using PugMod;
using Rewired;

// ReSharper disable SuspiciousTypeConversion.Global

namespace CoreLib.Submodules.ChatCommands
{
    /// <summary>
    /// This module provides means to add custom chat commands
    /// </summary>
    [CoreLibSubmodule(Dependencies = new[] { typeof(RewiredExtensionModule) })]
    public static class CommandsModule
    {
        #region Public Interface

        public static CommandCommSystem ClientCommSystem => clientCommSystem;
        public static CommandCommSystem ServerCommSystem => serverCommSystem;

        /// <summary>
        /// Return true if the submodule is loaded.
        /// </summary>
        public static bool Loaded
        {
            get => _loaded;
            internal set => _loaded = value;
        }

        //internal static ConfigEntry<bool> remindAboutHelpCommand;

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
                CoreLibMod.Log.LogWarning($"Failed to register command {commandType}!\n{e}");
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

        internal const string CommandPrefix = "/";
        private static readonly char[] brackets = { '{', '}', '[', ']' };

        private static bool _loaded;
        internal static Player rewiredPlayer;

        internal static string UP_KEY = "CoreLib_UpKey";
        internal static string DOWN_KEY = "CoreLib_DownKey";
        internal static string COMPLETE_KEY = "CoreLib_CompleteKey";

        internal static List<CommandPair> commandHandlers = new List<CommandPair>();

        private static CommandCommSystem clientCommSystem;
        private static CommandCommSystem serverCommSystem;

        [CoreLibSubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks()
        {
            CoreLibMod.harmony.PatchAll(typeof(ChatWindow_Patch));
            CoreLibMod.harmony.PatchAll(typeof(TitleScreenAnimator_Patch));
        }

        [CoreLibSubmoduleInit(Stage = InitStage.PostLoad)]
        internal static void Load()
        {
            CoreLibMod.Log.LogInfo("Commands Module Post Load");
            RegisterCommandHandler(typeof(HelpCommandHandler), "Core Lib");
            RegisterCommandHandler(typeof(DirectMessageCommandHandler), "Core Lib");
            RewiredExtensionModule.rewiredStart += () => { rewiredPlayer = ReInput.players.GetPlayer(0); };

            RewiredExtensionModule.AddKeybind(UP_KEY, "Next command", KeyboardKeyCode.UpArrow);
            RewiredExtensionModule.AddKeybind(DOWN_KEY, "Previous command", KeyboardKeyCode.DownArrow);
            RewiredExtensionModule.AddKeybind(COMPLETE_KEY, "Autocomplete command", KeyboardKeyCode.Tab);
            /*remindAboutHelpCommand = CoreLibMod.Instance.Config.Bind(
                "ChatModule",
                "remindAboutHelp",
                true,
                "Should user be reminded about existance of /help command any time a command returns error code output?");*/

            API.Client.OnWorldCreated += ClientWorldReady;
            API.Server.OnWorldCreated += ServerWorldReady;
        }

        private static void ClientWorldReady()
        {
            var world = API.Client.World;
            clientCommSystem = world.CreateSystem<CommandCommSystem>();
            API.Client.AddScheduledSystem(clientCommSystem);
        }

        private static void ServerWorldReady()
        {
            var world = API.Server.World;
            serverCommSystem = world.CreateSystem<CommandCommSystem>();
            API.Server.AddScheduledSystem(clientCommSystem);
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

        internal static void HandleCommand(CommandMessage message)
        {
            string[] args = message.message.Split(' ');
            if (args.Length < 1 || !args[0].StartsWith(CommandPrefix)) return;

            string cmdName = args[0].Substring(1);

            if (!GetCommandHandler(cmdName, out CommandPair commandPair))
            {
                serverCommSystem.SendResponse($"Command {cmdName} does not exist!", CommandStatus.Error);
                return;
            }

            string[] parameters = args.Skip(1).ToArray();

            try
            {
                CommandOutput output = commandPair.handler.Execute(parameters, message.sender);
                serverCommSystem.SendResponse(output.feedback, output.status);

                if (output.status == CommandStatus.Error) // && CommandsModule.remindAboutHelpCommand.Value)
                {
                    if (brackets.Any(c => message.message.Contains(c)))
                    {
                        serverCommSystem.SendResponse("Do not use brackets in your command! Brackets are meant as placeholder name separators only.",
                            CommandStatus.Hint);
                    }
                    else
                    {
                        serverCommSystem.SendResponse($"Use /help {cmdName} to learn command usage!", CommandStatus.Hint);
                    }
                }
            }
            catch (Exception e)
            {
                CoreLibMod.Log.LogWarning($"Error executing command {cmdName}:\n{e}");

                serverCommSystem.SendResponse($"Error executing command {cmdName}", CommandStatus.Error);
            }
        }

        #endregion
    }
}