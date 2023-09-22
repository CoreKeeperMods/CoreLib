using System;
using System.Collections.Generic;
using System.Linq;
using CoreLib.Submodules.ChatCommands.Communication;
using CoreLib.Submodules.ChatCommands.Patches;
using CoreLib.Submodules.RewiredExtension;
using CoreLib.Util.Extensions;
using PugMod;
using Rewired;

// ReSharper disable SuspiciousTypeConversion.Global

namespace CoreLib.Submodules.ChatCommands
{
    /// <summary>
    /// This module provides means to add custom chat commands
    /// </summary>
    public class CommandsModule : BaseSubmodule
    {
        #region Public Interface

        public static CommandCommSystem ClientCommSystem => clientCommSystem;
        public static CommandCommSystem ServerCommSystem => serverCommSystem;

        internal override GameVersion Build => new GameVersion(0, 0, 0, 0, "");
        internal override Type[] Dependencies => new[] { typeof(RewiredExtensionModule) };
        internal static CommandsModule Instance => CoreLibMod.GetModuleInstance<CommandsModule>();

        //internal static ConfigEntry<bool> remindAboutHelpCommand;

        /// <summary>
        /// Add all commands from specified assembly
        /// </summary>
        //TODO remove reflection use
        public static void AddCommands(long modId, string modName)
        {
            Instance.ThrowIfNotLoaded();
            Type[] commands = API.Reflection.GetTypes(modId).Where(type => typeof(IChatCommandHandler).IsAssignableFrom(type)).ToArray();
            commandHandlers.Capacity += commands.Length;

            foreach (Type commandType in commands)
            {
                RegisterCommandHandler(commandType, modName);
            }
        }

        public static void RegisterCommandHandler(Type handlerType, string modName)
        {
            if (handlerType == typeof(IChatCommandHandler)) return;

            try
            {
                IChatCommandHandler handler = (IChatCommandHandler)Activator.CreateInstance(handlerType);
                commandHandlers.Add(new CommandPair(handler, modName));
            }
            catch (Exception e)
            {
                CoreLibMod.Log.LogWarning($"Failed to register command {handlerType}!\n{e}");
            }
        }

        public static void UnregisterCommandHandler(Type handlerType)
        {
            if (handlerType == typeof(IChatCommandHandler)) return;
            commandHandlers.RemoveAll(pair => pair.handler.GetType() == handlerType);
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
        
        internal static Player rewiredPlayer;
        
        internal static string UP_KEY = "CoreLib_UpKey";
        internal static string DOWN_KEY = "CoreLib_DownKey";
        internal static string COMPLETE_KEY = "CoreLib_CompleteKey";

        internal static List<CommandPair> commandHandlers = new List<CommandPair>();

        private static CommandCommSystem clientCommSystem;
        private static CommandCommSystem serverCommSystem;

        internal override void SetHooks()
        {
            HarmonyUtil.PatchAll(typeof(ChatWindow_Patch));
            HarmonyUtil.PatchAll(typeof(TitleScreenAnimator_Patch));
        }

        internal override void Load()
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
            clientCommSystem = world.GetOrCreateSystem<CommandCommSystem>();
            API.Client.AddScheduledSystem(clientCommSystem);
        }

        private static void ServerWorldReady()
        {
            var world = API.Server.World;
            serverCommSystem = world.GetOrCreateSystem<CommandCommSystem>();
            API.Server.AddScheduledSystem(serverCommSystem);
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