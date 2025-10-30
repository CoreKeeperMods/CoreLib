using CoreLib.Util;
using CoreLib.Util.Extensions;
using HarmonyLib;
using PugMod;
using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace CoreLib
{
    /// The SubmoduleHandler class is responsible for managing the lifecycle of submodules within the application.
    public class SubmoduleHandler
    {
        /// Represents the current game version being used by the SubmoduleHandler.
        private readonly GameVersion _currentBuild;
        
        /// Instance of the <see cref="Logger"/> class used for logging messages, warnings,
        /// debug information, and errors related to submodule handling and loading processes.
        private readonly Logger _logger;

        /// <summary>
        /// Represents a collection of the names of all currently loaded submodules within the system.
        /// </summary>
        /// <threadsafety>
        /// This member is not inherently thread-safe. Synchronization might be required in multithreaded scenarios
        /// where simultaneous access to <c>loadedModules</c> may occur.
        /// </threadsafety>
        private readonly HashSet<string> _loadedModules;
        
        /// A private dictionary that holds instances of all loaded submodules,
        /// mapped by their respective types.
        private readonly Dictionary<Type, BaseSubmodule> _allModules;
        
        /// Stores the count of the last known submodules loaded into the system.
        private int _lastSubmoduleCount;
        
        /// Handles the initialization, registration, and management of submodules within the application.
        internal SubmoduleHandler(GameVersion build, Logger logger)
        {
            _currentBuild = build;
            _logger = logger;
            _loadedModules = new HashSet<string>();

            _allModules = new Dictionary<Type, BaseSubmodule>();
            UpdateSubmoduleList();
        }
        
        /// Retrieves an instance of the specified submodule type if it exists.
        /// <typeparam name="T">The type of the submodule to retrieve.</typeparam>
        internal T GetModuleInstance<T>() where T : BaseSubmodule => _allModules.TryGetValue(typeof(T), out var submodule) ? submodule as T : null;
        
        /// Determines whether the specified submodule is currently loaded.
        /// <param name="submodule">The name of the submodule to check.</param>
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
            if (moduleType == null) throw new ArgumentNullException(nameof(moduleType));

            if (!_allModules.ContainsKey(moduleType)) throw new InvalidOperationException($"Tried to load unknown submodule: '{moduleType.FullName}'!");

            string name = moduleType.GetNameChecked();

            if (IsLoaded(name)) return true;
            string version = _allModules[moduleType].Version;

            CoreLibMod.Log.LogInfo($"Enabling CoreLib Submodule: {name}, version {version}");

            try
            {
                if (!ignoreDependencies)
                {
                    var dependencies = GetModuleDependencies(moduleType);
                    foreach (var dependency in dependencies)
                    {
                        if (dependency == moduleType) continue;
                        if (!RequestModuleLoad(dependency, true))
                        {
                            _logger.LogError($"{name} could not be initialized because one of it's dependencies failed to load.");
                        }
                    }
                }

                var submodule = _allModules[moduleType];

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
        
        /// Retrieves the list of dependencies for a given module type.
        /// <param name="moduleType">The type of the module for which dependencies are to be retrieved.</param>
        /// <returns>A collection of types representing the dependencies of the specified module.</returns>
        private IEnumerable<Type> GetModuleDependencies(Type moduleType)
        {
            var modulesToAdd = moduleType.GetDependants(type =>
                {
                    var submodule = _allModules[type];
                    return submodule.Dependencies.AddRangeToArray(submodule.GetOptionalDependencies());
                },
                (start, end) =>
                {
                    CoreLibMod.Log.LogWarning(
                        $"Found Submodule circular dependency! Submodule {start.FullName} depends on {end.FullName}, which depends on {start.FullName}!" +
                        $"Submodule {start.FullName} and all of its dependencies will not be loaded.");
                });
            return modulesToAdd;
        }
        
        /// Updates the internal submodule list by identifying and registering new submodules available within the loaded core library modules.
        private void UpdateSubmoduleList()
        {
            var coreLibModules = API.ModLoader.LoadedMods.Where(IsCoreLibModuleMod);
            var moduleTypes = coreLibModules
                .SelectMany(mod => API.Reflection.GetTypes(mod.ModId).Where(IsSubmodule))
                .ToList();

            if (moduleTypes.Count <= _lastSubmoduleCount) return;
            foreach (var moduleType in moduleTypes)
            {
                if (_allModules.ContainsKey(moduleType)) continue;
                _allModules.Add(moduleType, (BaseSubmodule)Activator.CreateInstance(moduleType));
            }
            _lastSubmoduleCount = moduleTypes.Count;
        }

        /// Determines whether the specified mod belongs to the CoreLib module.
        /// <param name="mod">The mod to check for CoreLib module association.</param>
        private static bool IsCoreLibModuleMod(LoadedMod mod) => mod.Metadata.name.Contains("CoreLib");

        /// Determines whether a given type is a submodule in the system.
        /// <param name="type">The type to evaluate as a potential CoreLib submodule.</param>
        private static bool IsSubmodule(Type type) => typeof(BaseSubmodule).IsAssignableFrom(type) && type != typeof(BaseSubmodule);
    }
}