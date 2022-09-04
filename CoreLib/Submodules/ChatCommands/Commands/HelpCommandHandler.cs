using System.Linq;
using UnityEngine;

namespace CoreLib.Submodules.ChatCommands;

public class HelpCommandHandler : IChatCommandHandler
{
    public CommandOutput Execute(string[] parameters)
    {
        string commandsString;
        switch (parameters.Length)
        {
            case 0:
                commandsString = CommandsModule.commandHandlers
                    .Select(pair => pair.handler)
                    .Aggregate("\n", GetNames);
                return $"Use /help {{command}} for more information.\nCommands:{commandsString}";
            case 1:
                try
                {
                    IChatCommandHandler validCommandHandler = CommandsModule.commandHandlers
                        .Select(pair => pair.handler)
                        .First(element => element.GetTriggerNames().Contains(parameters[0]));
                    return validCommandHandler.GetDescription();
                } catch { return "This command does not exist. Do /help to view all commands.";}
            case 2 when parameters[0].Equals("mod"):
                string search = parameters[1].ToLowerInvariant();
                commandsString = CommandsModule.commandHandlers
                    .Where(pair => pair.modName.ToLowerInvariant().Contains(search))
                    .Select(pair => pair.handler)
                    .Aggregate("\n", GetNames);

                return $"Mod {parameters[1]} commands:\n{commandsString}";
                
                    
            default:
                return new CommandOutput("Invalid arguments. Do /help to view all commands.", Color.red);
        }
    }

    private string GetNames(string str, IChatCommandHandler handler)
    {
        return handler.GetTriggerNames().Length > 0 ? $"{str}\n{handler.GetTriggerNames()[0]}" : str;
    }

    public string GetDescription()
    {
        return "Use /help {command} for more information on a command.\n/help mod {modname} to list all commands added by a mod";
    }

    public string[] GetTriggerNames()
    {
        return new[] {"help"};
    }
}