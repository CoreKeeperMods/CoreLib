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
    /// Handles registration, dependency management, initialization, and lifecycle control
    /// for all CoreLib submodules. Ensures that each <see cref="BaseSubmodule"/> is properly
    /// loaded, patched, and post-initialized, while maintaining awareness of dependencies
    /// and version compatibility.
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

        /// The current game version that the handler was initialized with.
        private readonly GameVersion _currentBuild;


        /// Logger instance
        private readonly Logger _logger;

        /// Internal registry of all discovered submodules mapped to their respective types.
        private readonly Dictionary<Type, BaseSubmodule> _allModules;

        /// Tracks the number of submodules known at the time of the last discovery operation.
        /// Used to detect new submodules when updating.
        private int _lastSubmoduleCount;

        #endregion

        #region Constructor

        /// Initializes a new instance of the <see cref="SubmoduleHandler"/> class.
        /// <param name="build">The current <see cref="GameVersion"/> of the running game.</param>
        /// <param name="logger">The shared <see cref="Logger"/> used for diagnostic output.</param>
        internal SubmoduleHandler(GameVersion build, Logger logger)
        {
            _currentBuild = build;
            _logger = logger;
            _allModules = new Dictionary<Type, BaseSubmodule>();

            UpdateSubmoduleList();
        }

        #endregion

        #region Public Interface

        /// Retrieves an instance of a loaded submodule by type.
        /// <typeparam name="T">The submodule type to retrieve. Where T is a subclass of <see cref="BaseSubmodule"/></typeparam>
        internal T GetModuleInstance<T>() where T : BaseSubmodule => _allModules.TryGetValue(typeof(T), out var submodule) ? submodule as T : null;

        /// Requests loading of the specified submodule type.
        /// <param name="moduleType">The type of submodule to load.</param>
        internal bool RequestModuleLoad(Type moduleType) => RequestModuleLoad(moduleType, false);

        #endregion

        #region Module Loading and Dependencies

        /// Loads a submodule by type and optionally ignores dependency validation.
        /// <param name="moduleType">The type of submodule to load.</param>
        /// <param name="ignoreDependencies">If <c>true</c>, dependencies are not automatically loaded.</param>
        /// <returns><c>true</c> if successfully loaded; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="moduleType"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the requested module type is unknown or not registered.</exception>
        private bool RequestModuleLoad(Type moduleType, bool ignoreDependencies)
        {
            if (moduleType == null)
                throw new ArgumentNullException(nameof(moduleType));

            if (!_allModules.TryGetValue(moduleType, out var submodule))
                throw new InvalidOperationException($"Tried to load unknown submodule: '{moduleType.FullName}'!");

            string name = moduleType.GetNameChecked();

            if (submodule.Loaded)
                return true;

            //TODO find a better approach
            /*if (API.Server.World.IsServer() && !submodule.IsServerCompatible)
            {
                _logger.LogWarning($"{name} is not compatible with the server build! Skipping load.");
                return false;
            }*/

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

        /// Retrieves all dependencies associated with a given module type.
        /// <param name="moduleType">The module type to analyze for dependencies.</param>
        /// <returns>
        /// A sequence of <see cref="Type"/> objects representing both required and optional dependencies.
        /// </returns>
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
        
        /// Scans all loaded CoreLib mods for available submodules and registers new ones.
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