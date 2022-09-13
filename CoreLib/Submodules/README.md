# Submodules
## What is a submodule
Each folder on this page defines a submodule. Submodule is a building block of Core Lib. Each submodule has a single responsibility. For example RewiredExtensionModule allows to register new Rewired key binds.

A submodule is not loaded unless another mod requests that. This is needed to ensure that Core Lib does not break in an event where one of submodules gets broken due to game update. If that happens the player only needs to uninstall all mods that depend on the broken module, and other mods will continue to work. Because of this it is preferable to split unrelated functions into different submodules.

## Using submodules in your mods
To use any submodule you have to declare that in your plugin class. Use `CoreLibSubmoduleDependency` to declare all used submodules. make sure to only request submodules your code actually uses. Also don't forget to declare a dependency on Core Lib plugin.
```cs
[BepInPlugin(GUID, NAME, VERSION)]
[BepInDependency(CoreLibPlugin.GUID)]
[CoreLibSubmoduleDependency(nameof(LocalizationModule), nameof(RewiredExtensionModule))]
public class MyPlugin : BasePlugin
{
    public const string MODID = "myplugin";
    public const string GUID = "org.myname.plugin." + MODID;
    public const string NAME = "My Plugin";
    
    void Awake()
    {
        //Make use of modules here
    }
}
```

## Creating Submodules
To create a new submodule you need to create a new folder with the name of the module. In it create a new static class with the same name. Your actual submodule code can use any patterns you deem right. Here is a template of submodule class:
```cs
[CoreLibSubmodule]
public static class SubmoduleName
{
    public static bool Loaded {
        get => _loaded;
        internal set => _loaded = value;
    }

    private static bool _loaded;


    [CoreLibSubmoduleInit(Stage = InitStage.SetHooks)]
    internal static void SetHooks()
    {
        // Register all patches needed for this submodule here
    }


    [CoreLibSubmoduleInit(Stage = InitStage.Load)]
    internal static void load()
    {
        // Other actions not related to patches can be done here
    }
    
    [CoreLibSubmoduleInit(Stage = InitStage.PostLoad)]
    internal static void PostLoad()
    {
        // This method will be called after all modules are loaded
        // Here you can use other modules functions.
    }
    
    // Ensure that you call this method in ALL interface methods
    // This ensures that if your module is not loaded, a error will be thrown
    internal static void ThrowIfNotLoaded()
    {
        if (!Loaded)
        {
            Type submoduleType = MethodBase.GetCurrentMethod().DeclaringType;
            string message = $"{submoduleType.Name} is not loaded. Please use [{nameof(CoreLibSubmoduleDependency)}(nameof({submoduleType.Name})]";
            throw new InvalidOperationException(message);
        }
    }
}

```

### Submodule dependency
Submodules can depend on other submodules. To do that change your `CoreLibSubmodule` attribute to specify that. Example:
```cs
[CoreLibSubmodule(Dependencies = new []{typeof(LocalizationModule)})]
```
