using System;
using System.Collections.Generic;
using System.Linq;
using CoreLib.Util;
using CoreLib.Util.Extensions;
using HarmonyLib;
using PugMod;

// ReSharper disable once CheckNamespace
namespace CoreLib
{
    /// <summary>
    /// The SubmoduleHandler class is responsible for managing the lifecycle of submodules within the application.
    /// </summary>
    /// <remarks>
    /// This class supports operations such as initialization, retrieval, and management of submodule instances.
    /// It leverages the provided game version and logging instance to handle submodule operations.
    /// </remarks>
    public class SubmoduleHandler
    {
        /// <summary>
        /// Represents the current game version being used by the SubmoduleHandler.
        /// </summary>
        /// <remarks>
        /// This variable is of type <see cref="GameVersion"/> and is used to ensure compatibility
        /// of submodules with the current application build. During module loading,
        /// the version stored in this variable is compared to the version requirements
        /// of the submodules.
        /// </remarks>
        private readonly GameVersion _currentBuild;

        /// <summary>
        /// Instance of the <see cref="Logger"/> class used for logging messages, warnings,
        /// debug information, and errors related to submodule handling and loading processes.
        /// </summary>
        /// <remarks>
        /// This logger is specifically utilized within the <see cref="SubmoduleHandler"/> class
        /// for tracking both successful operations and issues encountered during submodule
        /// management, such as dependency resolution and version compatibility warnings.
        /// </remarks>
        private readonly Logger _logger;

        /// <summary>
        /// Represents a collection of the names of all currently loaded submodules within the system.
        /// </summary>
        /// <remarks>
        /// The <c>loadedModules</c> variable is utilized internally to track which submodules have been successfully loaded.
        /// Submodule names are stored as unique strings, and this collection is maintained to ensure that
        /// submodules cannot be loaded multiple times unnecessarily. The variable is a <c>HashSet</c>,
        /// providing quick lookup and preventing duplicate entries.
        /// </remarks>
        /// <threadsafety>
        /// This member is not inherently thread-safe. Synchronization might be required in multi-threaded scenarios
        /// where simultaneous access to <c>loadedModules</c> may occur.
        /// </threadsafety>
        private readonly HashSet<string> _loadedModules;

        /// <summary>
        /// A private dictionary that holds instances of all loaded submodules,
        /// mapped by their respective types.
        /// </summary>
        /// <remarks>
        /// This dictionary is utilized within the <see cref="SubmoduleHandler"/> class
        /// to manage and access submodules dynamically at runtime. It associates a specific
        /// <see cref="Type"/> with its corresponding <see cref="BaseSubmodule"/> instance,
        /// which allows for efficient retrieval and interaction with submodules.
        /// The key represents the submodule type, and the value represents the initialized
        /// instance of that submodule.
        /// </remarks>
        private readonly Dictionary<Type, BaseSubmodule> _allModules;

        /// <summary>
        /// Stores the count of the last known submodules loaded into the system.
        /// </summary>
        /// <remarks>
        /// Used to keep track of the number of submodules detected during the latest update.
        /// This variable is primarily utilized in the submodule management logic to determine
        /// if new submodules have been identified since the last check.
        /// </remarks>
        private int _lastSubmoduleCount;

        /// <summary>
        /// Handles the initialization, registration, and management of submodules within the application.
        /// </summary>
        /// <remarks>
        /// Utilizes the specified game version and logger instance to manage and maintain loaded submodules.
        /// </remarks>
        internal SubmoduleHandler(GameVersion build, Logger logger)
        {
            _currentBuild = build;
            this._logger = logger;
            _loadedModules = new HashSet<string>();

            _allModules = new Dictionary<Type, BaseSubmodule>();
            UpdateSubmoduleList();
        }

        /// <summary>
        /// Retrieves an instance of the specified submodule type if it exists.
        /// </summary>
        /// <typeparam name="T">The type of the submodule to retrieve.</typeparam>
        /// <returns>
        /// The instance of the specified submodule type, or null if the submodule is not found.
        /// </returns>
        internal T GetModuleInstance<T>()
            where T : BaseSubmodule
        {
            if (_allModules.TryGetValue(typeof(T), out BaseSubmodule submodule))
            {
                return (T)submodule;
            }

            return null;
        }

        /// <summary>
        /// Determines whether the specified submodule is currently loaded.
        /// </summary>
        /// <param name="submodule">The name of the submodule to check.</param>
        /// <returns>
        /// True if the submodule is loaded; otherwise, false.
        /// </returns>
        public bool IsLoaded(string submodule) => _loadedModules.Contains(submodule);

        /// <summary>
        /// Requests the loading of a specified submodule by type.
        /// </summary>
        /// <param name="moduleType">The type of the submodule to be loaded.</param>
        /// <returns>True if the submodule is successfully loaded; otherwise, false.</returns>
        public bool RequestModuleLoad(Type moduleType)
        {
            UpdateSubmoduleList();
            return RequestModuleLoad(moduleType, false);
        }

        /// <summary>
        /// Requests the loading of a submodule of the specified type.
        /// </summary>
        /// <param name="moduleType">The type of the submodule to be loaded.</param>
        /// <param name="ignoreDependencies"></param>
        /// <returns>True if the submodule was successfully loaded, otherwise false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="moduleType"/> is null.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the specified submodule type is not recognized or is not defined in the <see cref="_allModules"/> dictionary.
        /// </exception>
        private bool RequestModuleLoad(Type moduleType, bool ignoreDependencies)
        {
            if (moduleType == null)
                throw new ArgumentNullException(nameof(moduleType));

            if (!_allModules.ContainsKey(moduleType))
                throw new InvalidOperationException($"Tried to load unknown submodule: '{moduleType.FullName}'!");

            var name = moduleType.GetNameChecked();

            if (IsLoaded(name)) return true;
            var version = _allModules[moduleType].Version;

            CoreLibMod.Log.LogInfo($"Enabling CoreLib Submodule: {name}, version {version}");

            try
            {
                if (!ignoreDependencies)
                {
                    var dependencies = GetModuleDependencies(moduleType);
                    foreach (Type dependency in dependencies)
                    {
                        if (dependency == moduleType) continue;
                        if (!RequestModuleLoad(dependency, true))
                        {
                            _logger.LogError($"{name} could not be initialized because one of it's dependencies failed to load.");
                        }
                    }
                }

                BaseSubmodule submodule = _allModules[moduleType];

                if (!submodule.Build.Equals(GameVersion.Zero) &&
                    !submodule.Build.CompatibleWith(_currentBuild))
                {
                    _logger.LogWarning($"Submodule {name} was built for {submodule.Build}, but current build is {_currentBuild}.");
                }

                submodule.SetHooks();
                submodule.Load();

                submodule.Loaded = true;
                _loadedModules.Add(name);

                submodule.PostLoad();
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError($"{name} could not be initialized and has been disabled:\n{e}");
            }

            return false;
        }

        /// <summary>
        /// Retrieves the list of dependencies for a given module type.
        /// </summary>
        /// <param name="moduleType">The type of the module for which dependencies are to be retrieved.</param>
        /// <returns>A collection of types representing the dependencies of the specified module.</returns>
        /// <remarks>
        /// This method identifies both required and optional dependencies for the specified module type.
        /// It utilizes topological sorting to resolve dependencies and detects circular dependencies,
        /// logging a warning if any are found.
        /// </remarks>
        private IEnumerable<Type> GetModuleDependencies(Type moduleType)
        {
            IEnumerable<Type> modulesToAdd = moduleType.GetDependants(type =>
                {
                    BaseSubmodule submodule = _allModules[type];
                    return submodule.Dependencies.AddRangeToArray(submodule.GetOptionalDependencies());
                },
                (start, end) =>
                {
                    CoreLibMod.Log.LogWarning(
                        $"Found Submodule circular dependency! Submodule {start.FullName} depends on {end.FullName}, which depends on {start.FullName}! Submodule {start.FullName} and all of its dependencies will not be loaded.");
                });
            return modulesToAdd;
        }

        /// <summary>
        /// Updates the internal submodule list by identifying and registering new submodules available within the loaded core library modules.
        /// </summary>
        /// <remarks>
        /// This method scans through loaded mods to identify submodule types that are part of the core library framework.
        /// New submodules discovered during this process are instantiated and added to the internal module dictionary.
        /// Keeps track of the count of discovered submodules to avoid re-processing already known modules.
        /// </remarks>
        private void UpdateSubmoduleList()
        {
            var coreLibModules = API.ModLoader.LoadedMods.Where(IsCoreLibModuleMod);
            var moduleTypes = coreLibModules
                .SelectMany(mod => API.Reflection.GetTypes(mod.ModId).Where(IsSubmodule))
                .ToList();

            if (moduleTypes.Count > _lastSubmoduleCount)
            {
                foreach (Type moduleType in moduleTypes)
                {
                    if (_allModules.ContainsKey(moduleType)) continue;

                    _allModules.Add(moduleType, (BaseSubmodule)Activator.CreateInstance(moduleType));
                }

                _lastSubmoduleCount = moduleTypes.Count;
            }
        }

        /// <summary>
        /// Determines whether the specified mod belongs to the CoreLib module.
        /// </summary>
        /// <param name="mod">The mod to check for CoreLib module association.</param>
        /// <returns>
        /// True if the specified mod is part of the CoreLib module; otherwise, false.
        /// </returns>
        private bool IsCoreLibModuleMod(LoadedMod mod)
        {
            return mod.Metadata.name.Contains("CoreLib");
        }

        /// <summary>
        /// Determines whether a given type is a submodule in the system.
        /// </summary>
        /// <param name="type">The type to evaluate as a potential submodule.</param>
        /// <returns>True if the type is a submodule, otherwise false.</returns>
        private static bool IsSubmodule(Type type)
        {
            return type.IsAssignableTo(typeof(BaseSubmodule)) &&
                   type != typeof(BaseSubmodule);
        }
    }
}