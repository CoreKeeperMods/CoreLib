// ========================================================
// Project: Core Library Mod (Core Keeper)
// File: BaseSubmodule.cs
// Author: Minepatcher, Limoka
// Created: 2025-11-07
// Description: Defines the abstract base class for all CoreLib submodules,
//              providing standardized lifecycle management, dependency handling,
//              and state validation for modular extensions.
// ========================================================

using CoreLib.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using PugMod;

// ReSharper disable once CheckNamespace
namespace CoreLib
{
    /// Abstract foundation class for all CoreLib submodules.
    /// Provides a standardized structure for lifecycle management,
    /// dependency handling, and loading checks across all CoreLib components.
    public abstract class BaseSubmodule
    {
        #region Constants

        /// Default internal module ID used when a derived submodule does not override <see cref="ID"/>.
        private const string DefaultID = "CoreLib";

        /// Default display name used when a submodule does not override <see cref="Name"/>.
        private const string DefaultName = "Core Library";

        /// Default semantic version assigned to the submodule if none is provided.
        private const string DefaultVersion = "4.0.0";

        /// Message format string displayed when attempting to use a module before it has been loaded.
        private const string NotLoadedMessageFormat =
            "{0} is not loaded. Please use `CoreLibMod.LoadSubmodule(typeof({0}))` to load the module!";

        #endregion

        #region Properties

        /// Gets the unique string identifier for this module.
        public virtual string ID => DefaultID;

        /// Gets the human-readable name of this module.
        public virtual string Name => DefaultName;

        /// Gets the module version string.
        public virtual string Version => DefaultVersion;

        /// Gets the list of required dependencies (types of other submodules) that must be loaded before this one.
        internal virtual Type[] Dependencies => Type.EmptyTypes;

        /// Provides access to a logger instance for recording module activity.
        internal static Logger Log { get; } = new(DefaultName);

        /// Gets or sets whether this submodule has completed its loading process.
        internal bool Loaded { get; set; } = false;

        internal static LoadedMod Mod { get; private set; } = new();
        
        internal static List<LoadedMod> DependentMods { get; private set; } = new();
        
        internal bool IsServerCompatible = false;

        #endregion

        #region Validation

        /// Ensures that the module is in a loaded state.
        /// Throws an <see cref="InvalidOperationException"/> if the module has not yet been initialized.
        internal void ThrowIfNotLoaded()
        {
            if (!Loaded) throw new InvalidOperationException(string.Format(NotLoadedMessageFormat, Name));
        }

        #endregion

        #region Lifecycle Methods

        /// Called before the module is loaded to apply any required patches or hooks.
        /// <remarks>
        /// This method should be overridden to define all Harmony patches or event subscriptions
        /// necessary for the submodule to function properly.
        /// </remarks>
        internal virtual void SetHooks() { }

        /// Called when the module is being loaded.
        /// <remarks>
        /// Override this method to initialize runtime data, allocate resources, or perform
        /// setup logic specific to the submodule. Called once per load.
        /// </remarks>
        internal virtual void Load()
        {
            Mod = API.ModLoader.LoadedMods.First(mod => mod.Metadata.name == ID);
            DependentMods = API.ModLoader.LoadedMods
                .Where(mod => mod.Metadata.dependencies.Any(dep => dep.modName == "CoreLib")).ToList();
            if (Mod != null) return;
            Log.LogError("Failed to find CoreLib mod info!");
        }

        /// Called immediately after <see cref="Load"/> when all modules have completed their initialization.
        /// <remarks>
        /// Use this to safely access other submodules and perform dependency-aware setup.
        /// </remarks>
        internal virtual void PostLoad() { }

        /// Called after CoreLib's Load method is called
        internal virtual void LateLoad()
        {
            
        }
        
        #endregion

        #region Dependency Management

        /// Returns a list of optional dependencies that may be loaded if available.
        /// <returns>
        /// An array of <see cref="Type"/> objects representing optional module dependencies.
        /// </returns>
        /// <remarks>
        /// Unlike <see cref="Dependencies"/>, these are not required for initialization but may extend functionality.
        /// </remarks>
        internal virtual Type[] GetOptionalDependencies() => Type.EmptyTypes;

        #endregion
    }
}
