using System;
using System.Linq;
using CoreLib.Submodule.Command.Communication;
using Unity.Entities;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Command.Handlers
{
    /// <summary>
    /// Handles the "/help" command, providing users with a list of available commands,
    /// descriptions of individual commands, and other command-related functionalities.
    /// Implements pagination to handle a large number of listed commands.
    /// </summary>
    public class HelpCommandHandler : IServerCommandHandler
    {
        /// <summary>
        /// Specifies the number of lines to display per page when listing commands.
        /// Used to determine pagination logic in command handlers.
        /// </summary>
        public static int LinesPerPageCount = 10;

        /// <summary>
        /// Processes the "help" command and executes functionality based on the provided parameters.
        /// Allows viewing a list of commands, searching for a specific command, or filtering commands by page or module.
        /// </summary>
        /// <param name="parameters">An array of strings representing the command arguments.
        /// Depending on the input, it can specify a command to search for, a page index, or a module.</param>
        /// <param name="sender">The <see cref="Entity"/> that initiated the command.
        /// Represents the source of the request.</param>
        /// <returns>
        /// A <see cref="CommandOutput"/> object that contains the result of the command execution.
        /// This could be a list of commands, a command description, or an error message if invalid arguments are provided.
        /// </returns>
        public CommandOutput Execute(string[] parameters, Unity.Entities.Entity sender)
        {
            var usePaging = CommandsModule.CommandHandlers.Count > LinesPerPageCount;
            int totalPages = Mathf.CeilToInt(CommandsModule.CommandHandlers.Count / (float)LinesPerPageCount);

            string search;
            switch (parameters.Length)
            {
                case 0:
                    return ListCommands(usePaging, 1, totalPages);
                
                case 1:
                    search = parameters[0].ToLowerInvariant();
                    return ShowCommandDescription(search);
                
                case 2 when parameters[0].Equals("page") && usePaging:
                    if (int.TryParse(parameters[1], out int page))
                    {
                        return ListCommands(true, page, totalPages);
                    }

                    return new CommandOutput($"{parameters[1]} is not a valid number!", CommandStatus.Error);
                
                case 2 when parameters[0].Equals("mod"):
                    search = parameters[1].ToLowerInvariant();
                    return ListModCommands(search);
                
                default:
                    return new CommandOutput("Invalid arguments. Do /help to view all commands.", CommandStatus.Error);
            }
        }

        /// <summary>
        /// Provides detailed information about a specific command, including its description and usage details.
        /// </summary>
        /// <param name="search">The name of the command to retrieve the description for.</param>
        /// <returns>
        /// A <see cref="CommandOutput"/> containing the description of the specified command if it exists,
        /// or an error message if the command is not found.
        /// </returns>
        private static CommandOutput ShowCommandDescription(string search)
        {
            ICommandInfo validCommandHandler = CommandsModule.CommandHandlers
                .Select(pair => pair.Handler)
                .FirstOrDefault(element => element.GetTriggerNames().Any(name => name.Equals(search, StringComparison.InvariantCultureIgnoreCase)));
            if (validCommandHandler == null)
            {
                return new CommandOutput("This command does not exist. Use /help to view all commands.", CommandStatus.Error);
            }
            
            return validCommandHandler.GetDescription();
        }

        /// <summary>
        /// Lists all available commands, optionally using pagination if the number of commands exceeds the configured limit.
        /// </summary>
        /// <param name="usePaging">Indicates whether pagination should be used to display commands.</param>
        /// <param name="page">The current page number to display, if pagination is enabled.</param>
        /// <param name="totalPages">The total number of pages available for display.</param>
        /// <returns>
        /// A <see cref="CommandOutput"/> containing a formatted string of commands for the specified page or the full list of commands if pagination is not used.
        /// </returns>
        private CommandOutput ListCommands(bool usePaging, int page, int totalPages)
        {
            if (page > totalPages)
            {
                return new CommandOutput($"Invalid page {page}. There are only {totalPages} pages!", CommandStatus.Warning);
            }
            
            var skipItems = LinesPerPageCount * (page - 1);
            string commandsString = CommandsModule.CommandHandlers
                .Select(pair => pair.Handler)
                .Skip(skipItems)
                .Take(LinesPerPageCount)
                .Aggregate("", GetNames);
            if (usePaging)
            {
                return "Use /help {{command}} for more information.\n" +
                       $"Commands: (page {page} of {totalPages})\n" +
                       $"{commandsString}";
            }

            return $"Use /help {{command}} for more information.\nCommands:\n{commandsString}";
        }

        /// <summary>
        /// Lists all commands associated with a specified mod, based on the provided search term.
        /// </summary>
        /// <param name="search">The search term used to filter mod-related commands by mod name.</param>
        /// <returns>
        /// A <see cref="CommandOutput"/> containing a formatted string of all commands related to the specified mod.
        /// </returns>
        private CommandOutput ListModCommands(string search)
        {
            string commandsString = CommandsModule.CommandHandlers
                .Where(pair => pair.ModName.ToLowerInvariant().Contains(search))
                .Select(pair => pair.Handler)
                .Aggregate("\n", GetNames);

            return $"Mod {search} commands:\n{commandsString}";
        }


        /// <summary>
        /// Appends the trigger name of a command to a given string.
        /// </summary>
        /// <param name="str">The existing string to which the trigger name will be appended.</param>
        /// <param name="handler">The command handler containing the trigger name to append.</param>
        /// <returns>
        /// The updated string including the appended trigger name of the specified command handler.
        /// </returns>
        private string GetNames(string str, ICommandInfo handler)
        {
            return $"{str}\n{handler.GetTriggerNames()[0]}";
        }

        /// <summary>
        /// Retrieves the description for the Help command.
        /// </summary>
        /// <returns>
        /// A string containing the usage information and examples for the Help command.
        /// </returns>
        public string GetDescription()
        {
            return "Use /help {command} for more information on a command.\n/help mod {mod-name} to list all commands added by a mod";
        }

        /// <summary>
        /// Retrieves the trigger names associated with this command.
        /// </summary>
        /// <returns>
        /// An array of strings containing the names of the triggers that activate this command.
        /// </returns>
        public string[] GetTriggerNames()
        {
            return new[] { "help" };
        }
    }
}