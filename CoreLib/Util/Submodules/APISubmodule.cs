using BepInEx.Logging;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

#pragma warning disable 649

// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace CoreLib;
// Source code is taken from R2API: https://github.com/risk-of-thunder/R2API/tree/master

[Flags]
internal enum InitStage
{
    SetHooks = 1 << 0,
    Load = 1 << 1,
    PostLoad = 1 << 2,
    Unload = 1 << 3,
    UnsetHooks = 1 << 4,
    LoadCheck = 1 << 5,
}

// ReSharper disable once InconsistentNaming
[AttributeUsage(AttributeTargets.Class)]
internal class CoreLibSubmodule : Attribute
{
    public GameVersion Build;
    public Type[] Dependencies;
}

// ReSharper disable once InconsistentNaming
[AttributeUsage(AttributeTargets.Method)]
internal class CoreLibSubmoduleInit : Attribute
{
    public InitStage Stage;
}

/// <summary>
/// Attribute to have at the top of your BaseUnityPlugin class if you want to load a specific R2API Submodule.
/// Parameter(s) are the nameof the submodules.
/// e.g: [CommonAPISubmoduleDependency("", "")]
/// </summary>
[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class CoreLibSubmoduleDependency : Attribute
{
    public string?[]? SubmoduleNames { get; }

    public CoreLibSubmoduleDependency(params string[] submoduleName)
    {
        SubmoduleNames = submoduleName;
    }
}

// ReSharper disable once InconsistentNaming
/// <summary>
///
/// </summary>
public class APISubmoduleHandler
{
    private readonly GameVersion _build;
    private readonly ManualLogSource logger;

    private readonly HashSet<string> moduleSet;
    private static readonly HashSet<string> loadedModules;

    private List<Type> allModules;

    static APISubmoduleHandler()
    {
        loadedModules = new HashSet<string>();
    }

    internal APISubmoduleHandler(GameVersion build, ManualLogSource logger)
    {
        _build = build;
        this.logger = logger;
        moduleSet = new HashSet<string>();

        allModules = GetSubmodules(true);
    }

    /// <summary>
    /// Return true if the specified submodule is loaded.
    /// </summary>
    /// <param name="submodule">nameof the submodule</param>
    public static bool IsLoaded(string submodule) => loadedModules.Contains(submodule);

    /// <summary>
    /// Load submodule
    /// </summary>
    /// <param name="moduleType">Module type</param>
    /// <returns>Is loading successful?</returns>
    public bool RequestModuleLoad(Type? moduleType)
    {
        if (moduleType == null) return false;
        if (IsLoaded(moduleType.Name)) return true;

        CoreLibPlugin.Logger.LogInfo($"Enabling CoreLib Submodule: {moduleType.Name}");

        try
        {
            InvokeStage(moduleType, InitStage.SetHooks, null);
            InvokeStage(moduleType, InitStage.Load, null);
            FieldInfo? fieldInfo = moduleType.GetField("_loaded", AccessTools.all);
            if (fieldInfo != null)
            {
                fieldInfo.SetValue(null, true);
            }
            loadedModules.Add(moduleType.Name);
            InvokeStage(moduleType, InitStage.PostLoad, null);
            return true;
        }
        catch (Exception e)
        {
            logger.Log(LogLevel.Error, $"{moduleType.Name} could not be initialized and has been disabled:\n\n{e.Message}");
        }

        return false;
    }

    internal HashSet<string> LoadRequested(PluginScanner pluginScanner)
    {
        void AddModuleToSet(IEnumerable<CustomAttributeArgument> arguments)
        {
            foreach (var arg in arguments)
            {
                foreach (var stringElement in (CustomAttributeArgument[])arg.Value)
                {
                    string moduleName = (string)stringElement.Value;
                    Type moduleType = allModules.First(type => type.Name.Equals(moduleName));

                    IEnumerable<string> modulesToAdd = moduleType.GetDependants(type =>
                            {
                                CoreLibSubmodule? attr = type.GetCustomAttribute<CoreLibSubmodule>();
                                return attr?.Dependencies ?? Array.Empty<Type>();
                            },
                            (start, end) =>
                            {
                                CoreLibPlugin.Logger.LogWarning(
                                    $"Found Submodule circular dependency! Submodule {start.FullName} depends on {end.FullName}, which depends on {start.FullName}! Submodule {start.FullName} and all of its dependencies will not be loaded.");
                            })
                        .Select(type => type.Name);

                    foreach (string module in modulesToAdd)
                    {
                        moduleSet.Add(module);
                    }
                }
            }
        }

        void CallWhenAssembliesAreScanned()
        {
            var moduleTypes = GetSubmodules();

            foreach (var moduleType in moduleTypes)
            {
                CoreLibPlugin.Logger.LogInfo($"Enabling CoreLib Submodule: {moduleType.Name}");
            }

            var faults = new Dictionary<Type, Exception>();

            moduleTypes.ForEachTry(t => InvokeStage(t, InitStage.SetHooks, null), faults);

            moduleTypes.Where(t => !faults.ContainsKey(t))
                .ForEachTry(t => InvokeStage(t, InitStage.Load, null), faults);

            moduleTypes.Where(t => !faults.ContainsKey(t))
                .ForEachTry(t =>
                {
                    FieldInfo? fieldInfo = t.GetField("_loaded", AccessTools.all);
                    if (fieldInfo != null)
                    {
                        fieldInfo.SetValue(null, true);
                    }
                }, faults);
            moduleTypes.Where(t => !faults.ContainsKey(t))
                .ForEachTry(t => loadedModules.Add(t.Name), faults);

            moduleTypes.Where(t => !faults.ContainsKey(t))
                .ForEachTry(t => InvokeStage(t, InitStage.PostLoad, null), faults);

            faults.Keys.ForEachTry(t =>
            {
                logger.Log(LogLevel.Error, $"{t.Name} could not be initialized and has been disabled:\n\n{faults[t]}");
                InvokeStage(t, InitStage.UnsetHooks, null);
            }, faults);
        }

        var scanRequest = new PluginScanner.AttributeScanRequest(typeof(CoreLibSubmoduleDependency).FullName,
            AttributeTargets.Assembly | AttributeTargets.Class,
            CallWhenAssembliesAreScanned, false,
            (assembly, arguments) =>
                AddModuleToSet(arguments),
            (type, arguments) =>
                AddModuleToSet(arguments)
        );

        pluginScanner.AddScanRequest(scanRequest);

        return loadedModules;
    }

    private List<Type> GetSubmodules(bool allSubmodules = false)
    {
        Type?[] types;
        try
        {
            types = Assembly.GetExecutingAssembly().GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            types = e.Types;
        }

        var moduleTypes = types.Where(type => APISubmoduleFilter(type, allSubmodules)).ToList();
        return moduleTypes!;
    }

    // ReSharper disable once InconsistentNaming
    private bool APISubmoduleFilter(Type? type, bool allSubmodules = false)
    {
        if (type == null) return false;
        var attr = type.GetCustomAttribute<CoreLibSubmodule>();

        if (attr == null)
            return false;

        if (allSubmodules)
        {
            return true;
        }

        // Comment this out if you want to try every submodules working (or not) state
        if (!moduleSet.Contains(type.Name))
        {
            var shouldload = new object[1];
            InvokeStage(type, InitStage.LoadCheck, shouldload);
            if (!(shouldload[0] is bool))
            {
                return false;
            }

            if (!(bool)shouldload[0])
            {
                return false;
            }
        }

        if (!attr.Build.Equals(GameVersion.zero) && attr.Build.CompatibleWith(_build))
            logger.Log(LogLevel.Debug,
                $"{type.Name} was built for build {attr.Build}, current build is {_build}.");

        return true;
    }

    internal void InvokeStage(Type? type, InitStage stage, object[]? parameters)
    {
        if (type == null) return;
        var method = type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            .Where(m => m.GetCustomAttributes(typeof(CoreLibSubmoduleInit))
                .Any(a => ((CoreLibSubmoduleInit)a).Stage.HasFlag(stage))).ToList();

        if (method.Count == 0)
        {
            logger.Log(LogLevel.Debug, $"{type.Name} has no static method registered for {stage}");
            return;
        }

        method.ForEach(m => m.Invoke(null, parameters));
    }
}

public static class EnumerableExtensions
{
    /// <summary>
    /// ForEach but with a try catch in it.
    /// </summary>
    /// <param name="list">the enumerable object</param>
    /// <param name="action">the action to do on it</param>
    /// <param name="exceptions">the exception dictionary that will get filled, null by default if you simply want to silence the errors if any pop.</param>
    /// <typeparam name="T"></typeparam>
    public static void ForEachTry<T>(this IEnumerable<T> list, Action<T> action, IDictionary<T, Exception> exceptions)
    {
        list.ToList().ForEach(element =>
        {
            if (element == null) return;
            try
            {
                action.Invoke(element);
            }
            catch (Exception exception)
            {
                exceptions.Add(element, exception);
            }
        });
    }
}