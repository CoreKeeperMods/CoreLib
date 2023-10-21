using System.Collections.Generic;
using System.Linq;
using CoreLib.Commands.Communication;
using Unity.Entities;
using UnityEngine;

namespace CoreLib.Commands.Handlers
{
    public class HelpCommandHandler : IServerCommandHandler
    {
        public static int linesPerPageCount = 10;

        public CommandOutput Execute(string[] parameters, Entity sender)
        {
            var usePaging = CommandsModule.commandHandlers.Count > linesPerPageCount;
            int totalPages = Mathf.CeilToInt(CommandsModule.commandHandlers.Count / (float)linesPerPageCount);

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

        private static CommandOutput ShowCommandDescription(string search)
        {
            ICommandInfo validCommandHandler = CommandsModule.commandHandlers
                .Select(pair => pair.handler)
                .FirstOrDefault(element => element.GetTriggerNames().Contains(search));
            if (validCommandHandler == null)
            {
                return new CommandOutput("This command does not exist. Use /help to view all commands.", CommandStatus.Error);
            }
            
            return validCommandHandler.GetDescription();
        }

        private CommandOutput ListCommands(bool usePaging, int page, int totalPages)
        {
            if (page > totalPages)
            {
                return new CommandOutput($"Invalid page {page}. There are only {totalPages} pages!", CommandStatus.Warning);
            }
            
            var skipItems = linesPerPageCount * (page - 1);
            string commandsString = CommandsModule.commandHandlers
                .Select(pair => pair.handler)
                .Skip(skipItems)
                .Take(linesPerPageCount)
                .Aggregate("", GetNames);
            if (usePaging)
            {
                return "Use /help {{command}} for more information.\n" +
                       $"Commands: (page {page} of {totalPages})\n" +
                       $"{commandsString}";
            }

            return $"Use /help {{command}} for more information.\nCommands:\n{commandsString}";
        }
        
        private CommandOutput ListModCommands(string search)
        {
            string commandsString = CommandsModule.commandHandlers
                .Where(pair => pair.modName.ToLowerInvariant().Contains(search))
                .Select(pair => pair.handler)
                .Aggregate("\n", GetNames);

            return $"Mod {search} commands:\n{commandsString}";
        }


        private string GetNames(string str, ICommandInfo handler)
        {
            return $"{str}\n{handler.GetTriggerNames()[0]}";
        }

        public string GetDescription()
        {
            return "Use /help {command} for more information on a command.\n/help mod {modname} to list all commands added by a mod";
        }

        public string[] GetTriggerNames()
        {
            return new[] { "help" };
        }
    }
}