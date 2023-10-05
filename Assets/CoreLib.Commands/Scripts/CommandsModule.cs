using System;
using System.Collections.Generic;
using System.Linq;
using CoreLib.Commands.Communication;
using CoreLib.Commands.Handlers;
using CoreLib.Commands.Patches;
using CoreLib.RewiredExtension;
using Newtonsoft.Json;
using PugMod;
using Rewired;
using Unity.Burst;

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
                    
                    commandHandlers.Add(new CommandPair(handler, modName));
                }else if (typeof(IClientCommandHandler).IsAssignableFrom(handlerType))
                {
                    IClientCommandHandler handler = (IClientCommandHandler)Activator.CreateInstance(handlerType);
                    if (handler.GetTriggerNames().Length == 0)
                    {
                        CoreLibMod.Log.LogWarning($"Failed to register command handler {handlerType.GetNameChecked()}, because it does not define any trigger names!");
                        return;
                    }
                    
                    commandHandlers.Add(new CommandPair(handler, modName));
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

        internal override GameVersion Build => new GameVersion(0, 0, 0, 0, "");
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
        
        private static CommandCommSystem clientCommSystem;
        private static CommandCommSystem serverCommSystem;

        internal override void SetHooks()
        {
            CoreLibMod.Patch(typeof(ChatWindow_Patch));
            CoreLibMod.Patch(typeof(TitleScreenAnimator_Patch));
            CoreLibMod.Patch(typeof(ECSManager_Patch));
        }

        internal override void Load()
        {
            var mod = API.ModLoader.LoadedMods.FirstOrDefault(loadedMod => loadedMod.Metadata.name.Equals("CoreLib.Commands"));
            if (mod == null)
            {
                CoreLibMod.Log.LogError("Failed to find CoreLib.Commands mod info!");
                throw new Exception("Burst Load Failed!");
            }
            
            string directory = API.ModLoader.GetDirectory(mod.ModId);
            BurstRuntime.LoadAdditionalLibrary($"{directory}/CoreLib.Commands_burst_generated.dll");
        }

        internal override void PostLoad()
        {
            CoreLibMod.Log.LogInfo("Commands Module Post Load");
            if (API.Config.TryGet(CoreLibMod.ID, "CommandModule", "Settings", out string jsonData))
            {
                if (!string.IsNullOrEmpty(jsonData))
                {
                    settings = JsonConvert.DeserializeObject<CommandModuleSettings>(jsonData);
                }
            }

            string jsonString = JsonConvert.SerializeObject(settings, Formatting.Indented);
            API.Config.Set(CoreLibMod.ID, "CommandModule", "Settings", jsonString);

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
            API.Client.AddMainThreadSystem(clientCommSystem);
        }

        private static void ServerWorldReady()
        {
            var world = API.Server.World;
            serverCommSystem = world.GetOrCreateSystem<CommandCommSystem>();
            API.Server.AddMainThreadSystem(serverCommSystem);
        }

        internal static void ServerHandleCommand(CommandMessage message)
        {
            string[] args = message.message.Split(' ');
            if (args.Length < 1 || !args[0].StartsWith(CommandPrefix)) return;

            string cmdName = args[0].Substring(1);

            if (!GetCommandHandler(cmdName, out CommandPair commandPair))
            {
                if (settings.allowUnknownClientCommands)
                {
                    if (settings.logAllExecutedCommands)
                    {
                        string playerName = message.sender.GetPlayerEntity().GetPlayerName();
                        CoreLibMod.Log.LogInfo($"[{playerName} unknown command relayed]: {message.message}");
                    }
                    serverCommSystem.SendRelayCommand(message.message);
                    return;
                }
                serverCommSystem.SendResponse($"Command {cmdName} does not exist!", CommandStatus.Error);
                return;
            }

            if (!CheckCommandPermission(message, commandPair))
            {
                serverCommSystem.SendResponse($"Not enough permissions to execute {cmdName}! Please contact server owner for more info.", CommandStatus.Error);
                return;
            }

            if (settings.logAllExecutedCommands)
            {
                string playerName = message.sender.GetPlayerEntity().GetPlayerName();
                CoreLibMod.Log.LogInfo($"[{playerName} executed]: {message.message}");
            }

            string[] parameters = args.Skip(1).ToArray();

            var result = ExecuteCommand(commandPair, message, parameters);
            foreach (CommandOutput output in result)
            {
                serverCommSystem.SendResponse(output.feedback, output.status);
            }
        }

        internal static bool CheckCommandPermission(CommandMessage message, CommandPair command)
        {
            if (!settings.enableCommandSecurity) return true;
            var entityManager = API.Server.World.EntityManager;
            if (!entityManager.Exists(message.sender)) return false;
            
            bool guestMode = entityManager.World.GetExistingSystem<WorldInfoSystem>().WorldInfo.guestMode;
            int adminLevel = 0;
            
            if (entityManager.HasComponent<ConnectionAdminLevelCD>(message.sender))
            {
                adminLevel = entityManager.GetComponentData<ConnectionAdminLevelCD>(message.sender).Value;
            }

            if (!guestMode || adminLevel > 0)
            {
                return true;
            }

            //TODO implement additional security
            return false;
        }
        
        internal static void ClientHandleCommand(CommandMessage message)
        {
            string[] args = message.message.Split(' ');
            if (args.Length < 1 || !args[0].StartsWith(CommandPrefix)) return;

            string cmdName = args[0].Substring(1);

            if (!GetCommandHandler(cmdName, out CommandPair commandPair))
            {
                clientCommSystem.AppendMessage(new CommandMessage($"Command {cmdName} does not exist!", CommandStatus.Error));
                return;
            }

            if (commandPair.isServer)
            {
                clientCommSystem.AppendMessage(new CommandMessage($"Cannot execute {cmdName} locally. It's a server command!", CommandStatus.Error));
                return;
            }

            string[] parameters = args.Skip(1).ToArray();
            message.userWantsHints = settings.displayAdditionalHints;
            
            var result = ExecuteCommand(commandPair, message, parameters);
            foreach (CommandOutput output in result)
            {
                clientCommSystem.AppendMessage(new CommandMessage(output));
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
                
                if (output.status == CommandStatus.Error && message.userWantsHints)
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