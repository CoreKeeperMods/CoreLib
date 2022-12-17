using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace CoreLib;

[BepInPlugin(GUID, NAME, VERSION)]
public class CoreLibPlugin : BasePlugin {

    public const string GUID = "com.le4fless.corelib";
    public const string NAME = "CoreLib";
    public const string VERSION = ThisAssembly.AssemblyVersion;
        
    public static readonly GameVersion buildFor = new GameVersion(0,5,0, 0, "9b96");
    internal static HashSet<string> LoadedSubmodules;
    internal static APISubmoduleHandler submoduleHandler;
    internal static Harmony harmony;
        
    internal static CoreLibPlugin Instance { get; private set; }
    public static ManualLogSource Logger { get; private set; }

    public override void Load() {
        Instance = this;
        Logger = base.Log;  
            
        harmony = new Harmony("com.le4fless.corelib");

        CheckIfUsedOnRightGameVersion();
            
        var pluginScanner = new PluginScanner();
        submoduleHandler = new APISubmoduleHandler(buildFor, Logger);
        LoadedSubmodules = submoduleHandler.LoadRequested(pluginScanner);
        pluginScanner.ScanPlugins();

        Log.LogInfo($"{PluginInfo.PLUGIN_NAME} is loaded!");
    }
        
    internal static void CheckIfUsedOnRightGameVersion() {
        var buildId = new GameVersion(Application.version);

        if (buildFor.CompatibleWith(buildId))
            return;

        // ReSharper disable once PossiblyImpureMethodCallOnReadonlyVariable
        Logger.LogWarning($"This version of CoreLib was built for game version \"{buildFor}\", but you are running \"{buildId}\".");
        Logger.LogWarning("Should any problems arise, please check for a new version before reporting issues.");
    }
        
    /// <summary>
    /// Return true if the specified submodule is loaded.
    /// </summary>
    /// <param name="submodule">nameof the submodule</param>
    public static bool IsSubmoduleLoaded(string submodule) {
        if (LoadedSubmodules == null) {
            Logger.LogWarning("IsLoaded called before submodules were loaded, result may not reflect actual load status.");
            return false;
        }
        return LoadedSubmodules.Contains(submodule);
    }

    /// <summary>
    /// Try load specified module manually. This is useful if you are using ScriptEngine and can't request using attributes.
    /// Do not use unless you can't make use of <see cref="CoreLibSubmoduleDependency"/>.
    /// </summary>
    /// <param name="moduleType">Type of needed module</param>
    /// <returns>Is loading successful?</returns>
    public static bool TryLoadModule(Type moduleType)
    {
        return submoduleHandler.RequestModuleLoad(moduleType);
    }
}