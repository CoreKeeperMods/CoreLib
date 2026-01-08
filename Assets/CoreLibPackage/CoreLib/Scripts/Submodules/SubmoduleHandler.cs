// ========================================================
// Project: Core Library Mod (Core Keeper)
// File: SubmoduleHandler.cs
// Author: Minepatcher, Limoka
// Created: 2025-11-07
// Description: Manages discovery, initialization, dependency resolution,
//              and lifecycle control for CoreLib submodules.
// ========================================================

using CoreLib.Util;
using CoreLib.Util.Extension;
using HarmonyLib;
using PugMod;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.NetCode;

// ReSharper disable once CheckNamespace
namespace CoreLib
{
    /// <summary>
    /// Handles registration, dependency management, initialization, and lifecycle control
    /// for all CoreLib submodules. Ensures that each <see cref="BaseSubmodule"/> is properly
    /// loaded, patched, and post-initialized, while maintaining awareness of dependencies
    /// and version compatibility.
    /// </summary>
    /// <remarks>
    /// This class acts as the internal submodule orchestrator for <see cref="CoreLibMod"/>.
    /// It dynamically discovers available submodules, resolves dependencies, and manages
    /// load ordering based on inter-module relationships.
    /// </remarks>
    /// <seealso cref="BaseSubmodule"/>
    /// <seealso cref="CoreLibMod"/>
    /// <seealso cref="Logger"/>
    internal class SubmoduleHandler
    {
        #region Fields

        /// <summary>
        /// The current game version that the handler was initialized with.
        /// Used to verify submodule compatibility.
        /// </summary>
        private readonly GameVersion _currentBuild;

        /// <summary>
        /// Logger instance used to record information, warnings, and errors
        /// related to submodule discovery and loading.
        /// </summary>
        private readonly Logger _logger;

        /// <summary>
        /// Internal registry of all discovered submodules mapped to their respective types.
        /// </summary>
        private readonly Dictionary<Type, BaseSubmodule> _allModules;

        /// <summary>
        /// Tracks the number of submodules known at the time of the last discovery operation.
        /// Used to detect new submodules when updating.
        /// </summary>
        private int _lastSubmoduleCount;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="SubmoduleHandler"/> class.
        /// </summary>
        /// <param name="build">The current <see cref="GameVersion"/> of the running game.</param>
        /// <param name="logger">The shared <see cref="Logger"/> used for diagnostic output.</param>
        /// <remarks>
        /// The constructor sets up the internal submodule registry and triggers the initial
        /// discovery of submodules present in all loaded CoreLib mods.
        /// </remarks>
        internal SubmoduleHandler(GameVersion build, Logger logger)
        {
            _currentBuild = build;
            _logger = logger;
            _allModules = new Dictionary<Type, BaseSubmodule>();

            UpdateSubmoduleList();
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Retrieves an instance of a loaded submodule by type.
        /// </summary>
        /// <typeparam name="T">The submodule type to retrieve. Where T is a subclass of <see cref="BaseSubmodule"/></typeparam>
        internal T GetModuleInstance<T>() where T : BaseSubmodule => _allModules.TryGetValue(typeof(T), out var submodule) ? submodule as T : null;

        /// <summary>
        /// Requests loading of the specified submodule type.
        /// </summary>
        /// <param name="moduleType">The type of submodule to load.</param>
        internal bool RequestModuleLoad(Type moduleType) => RequestModuleLoad(moduleType, false);

        #endregion

        #region Module Loading and Dependencies

        /// <summary>
        /// Loads a submodule by type and optionally ignores dependency validation.
        /// </summary>
        /// <param name="moduleType">The type of submodule to load.</param>
        /// <param name="ignoreDependencies">If <c>true</c>, dependencies are not automatically loaded.</param>
        /// <returns><c>true</c> if successfully loaded; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="moduleType"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the requested module type is unknown or not registered.</exception>
        /// <remarks>
        /// This method performs:
        /// <list type="number">
        /// <item>Dependency resolution (unless ignored)</item>
        /// <item>Version compatibility validation</item>
        /// <item>Module hook registration via <see cref="BaseSubmodule.SetHooks"/></item>
        /// <item>Module load and post-load execution</item>
        /// </list>
        /// </remarks>
        private bool RequestModuleLoad(Type moduleType, bool ignoreDependencies)
        {
            if (moduleType == null)
                throw new ArgumentNullException(nameof(moduleType));

            if (!_allModules.TryGetValue(moduleType, out var submodule))
                throw new InvalidOperationException($"Tried to load unknown submodule: '{moduleType.FullName}'!");

            string name = moduleType.GetNameChecked();

            if (submodule.Loaded)
                return true;

            if (API.Server.World.IsServer() && !submodule.IsServerCompatible)
            {
                _logger.LogWarning($"{name} is not compatible with the server build! Skipping load.");
                return false;
            }

            _logger.LogInfo($"Enabling CoreLib Submodule: {name}");

            try
            {
                if (!ignoreDependencies)
                {
                    var dependencies = GetModuleDependencies(moduleType);
                    foreach (var dependency in dependencies)
                    {
                        if (dependency == moduleType)
                            continue;

                        if (!RequestModuleLoad(dependency, true))
                        {
                            _logger.LogError($"{name} could not be initialized because one of its dependencies failed to load.");
                        }
                    }
                }

                submodule.SetHooks();
                submodule.Load();
                submodule.Loaded = true;
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
        /// Retrieves all dependencies associated with a given module type.
        /// </summary>
        /// <param name="moduleType">The module type to analyze for dependencies.</param>
        /// <returns>
        /// A sequence of <see cref="Type"/> objects representing both required and optional dependencies.
        /// </returns>
        /// <remarks>
        /// This method relies on <see cref="TopologicalSortExtension.GetDependants{T}"/> to resolve direct and transitive
        /// dependencies. It also detects and logs circular dependencies to prevent recursive loading loops.
        /// </remarks>
        /// <seealso cref="BaseSubmodule.Dependencies"/>
        /// <seealso cref="BaseSubmodule.GetOptionalDependencies"/>
        private IEnumerable<Type> GetModuleDependencies(Type moduleType)
        {
            var modulesToAdd = moduleType.GetDependants(
                type =>
                {
                    var submodule = _allModules[type];
                    return submodule.Dependencies.AddRangeToArray(submodule.GetOptionalDependencies());
                },
                (start, end) =>
                {
                    _logger.LogWarning(
                        $"Detected circular dependency! {start.FullName} ↔ {end.FullName}. " +
                        $"These submodules will not be loaded.");
                });

            return modulesToAdd;
        }

        #endregion

        #region Discovery and Registration

        /// <summary>
        /// Scans all loaded CoreLib mods for available submodules and registers new ones.
        /// </summary>
        /// <remarks>
        /// Uses <see cref="ModAPIReflection.GetTypes(long)"/> to locate all classes deriving from
        /// <see cref="BaseSubmodule"/> within loaded mods whose metadata name contains "CoreLib".
        /// Any new module types found are instantiated and stored in <see cref="_allModules"/>.
        /// </remarks>
        private void UpdateSubmoduleList()
        {
            var moduleTypes = API.ModLoader.LoadedMods
                .Where(mod => mod.Metadata.name.Contains("CoreLib"))
                .SelectMany(mod => API.Reflection.GetTypes(mod.ModId)
                    .Where(type => type != typeof(BaseSubmodule) && typeof(BaseSubmodule).IsAssignableFrom(type)))
                .ToList();

            if (moduleTypes.Count <= _lastSubmoduleCount) return;
            
            foreach (var moduleType in moduleTypes)
                _allModules.TryAdd(moduleType, (BaseSubmodule)Activator.CreateInstance(moduleType));

            _lastSubmoduleCount = moduleTypes.Count;
        }

        #endregion
    }
}