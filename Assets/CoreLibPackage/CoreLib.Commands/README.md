# Commands Module
Commands Module is a CoreLib submodule that allows to add new Chat Commands. 

This submodule also enables the use of Quantum console if you prefer it. Use `Del` key on your keyboard to open it. Use `chat` command to use chat commands from it. For example: `chat give iron bar 5`

## Usage example:
Make sure to call `CoreLibMod.LoadModules(typeof(CommandsModule));` to in your mod `EarlyInit()` function, before using the module. This will load the submodule.

To make a command create a new class implementing `IClientCommandHandler` interface:
```cs
public class MyCommandHanlder : IClientCommandHandler
{
    public CommandOutput Execute(string[] parameters)
    {
        // This method will get executed on the CLIENT when player enters the command.
        // Your return value gets printed to chat.
        return "Successfully done stuff";
        
        // Or you can change the status to indicate result success or something else
        return new CommandOutput("That didn't work. Not sure what I expected...", CommandStatus.Error);
    }

    public string GetDescription()
    {
        return "Use /mycommand to do ...";
    }

    public string[] GetTriggerNames()
    {
        return new[] {"mycommand"};
    }
}
```

You can also implement `IServerCommandHandler` interface, this will allow your command to be executed on the server side:

```cs
public class MyCommandHanlder : IClientCommandHandler
{
    public CommandOutput Execute(string[] parameters, Entity sender)
    {
        // This method will get executed on the SERVER when player enters the command.
        // This version of the method gets 'sender' argument. This is the entity representing player connection who executed the command.
        // If you want to get their PLAYER entity do this:
        Entity playerEntity = sender.GetPlayerEntity();
        // Use this method, if you want to get the player name.
        var playerName = playerEntity.GetPlayerName();
        //Also you can get their player controller:
        PlayerController pc = sender.GetPlayerController();
        
        // Be aware that your code is being executed on the SERVER. As such don't try to access client specific things here.
        
        // Your return value gets printed to chat. You can change the status to indicate result success or something else
        return "Successfully done stuff";
    }

    // rest same as in IClientCommandHandler
}
```

Now in your mod `EarlyInit()` method write:
```cs
var modInfo = GetModInfo(this);
CommandsModule.AddCommands(modInfo.ModId, "My Mod");
```
This will register any command handlers in your mod.
