# Commands Module
Commands Module is a submodule that allows to add new Chat Commands. 

## Usage example:
Make sure to add `[CoreLibSubmoduleDependency(nameof(CommandsModule))]` to your plugin attributes. This will load the submodule.

To make a command create a new class implementing `IChatCommandHandler` interface:
```c#
public class MyCommandHanlder : IChatCommandHandler
{
    public CommandOutput Execute(string[] parameters)
    {
        // This method will get executed when player enters the command.
        // Your return value gets printed to chat. You can change the text color to indicate result success or something else
    }

    public string GetDescription()
    {
        return "Use /mycommand to do ...";
    }

    public string[] GetTriggerNames()
    {
        return new[] {"mycommand"};
    }
    
    public string GetModName()
    {
        return "My Amazing Mod";
    }
}
```

Now in your plugin `Load()` method write:
```c#
AddCommands(Assembly.GetExecutingAssembly());
```
This will register any command handlers in your mod assembly. If you have multiple assemblies call this method for each one.