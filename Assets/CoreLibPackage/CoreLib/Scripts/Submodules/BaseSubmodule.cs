using System;
using CoreLib.Util;
using PugMod;

// ReSharper disable once CheckNamespace
namespace CoreLib
{
    /// <summary>
    /// Represents an abstract base class for all submodules in the CoreLib framework.
    /// Provides fundamental properties and methods required for submodule management,
    /// such as loading, unloading, dependency handling, and versioning.
    /// </summary>
    public abstract class BaseSubmodule
    {
        public bool Loaded { get; internal set; }
        
        public const string Name = "Core Lib";
        
        internal static Logger Log = new(Name);

        internal virtual GameVersion Build =>  new(1, 1, 2, 0, "7da5");

        internal string Version => "4.0.0";
        
        internal virtual Type[] Dependencies => Type.EmptyTypes;

        /// <summary>
        /// Ensures that the submodule has been properly loaded before performing any operations that depend on its loaded state.
        /// This method throws an exception if the submodule has not been loaded, providing a detailed error message to guide the user.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the submodule has not been loaded. The exception message provides the name of the submodule
        /// and suggests loading the module using the code. <code>CoreLibMod.LoadModules(typeof(submodule))</code>
        /// </exception>
        internal void ThrowIfNotLoaded()
        {
            if (Loaded) return;
            string submoduleName = GetType().GetNameChecked();
            string message = $"{submoduleName} is not loaded. Please use CoreLibMod.LoadModules(typeof({submoduleName})) to load the module!";
            throw new InvalidOperationException(message);
        }

        /// <summary>
        /// Configures and applies the necessary hooks for the current submodule.
        /// This method establishes any event bindings, listeners, or connections required for the submodule's operation within its context.
        /// </summary>
        internal virtual void SetHooks() { }

        /// <summary>
        /// Initiates the loading process for the submodule, performing any required logic to prepare the submodule for use.
        /// This method is invoked during the submodule initialization process and is intended to be overridden by derived classes.
        /// </summary>
        internal virtual void Load() { }

        /// <summary>
        /// Performs additional initialization or setup tasks for the submodule after it has been loaded.
        /// This method is intended to be overridden by derived classes to implement specific post-load logic,
        /// such as subscribing to events, initializing configuration, binding keys, or registering handlers.
        /// </summary>
        internal virtual void PostLoad() { }

        /// <summary>
        /// Releases resources and performs necessary cleanup operations associated with the submodule.
        /// This method is called when the submodule is being unloaded and should be used to reverse
        /// or undo changes made by the submodule during its operation.
        /// </summary>
        internal virtual void Unload() { }

        /// <summary>
        /// Removes all hooks associated with the submodule, effectively undoing changes made during the hook setup process.
        /// This method is intended to clean up any modifications to external components or systems caused by the <c>SetHooks</c> method.
        /// </summary>
        internal virtual void UnsetHooks() { }

        /// <summary>
        /// Determines whether the module meets the conditions required for loading.
        /// This method allows for load validation and can be overridden by derived classes
        /// to implement custom logic for assessing whether the module can be safely loaded.
        /// </summary>
        internal virtual bool LoadCheck()
        {
            return true;
        }

        /// <summary>
        /// Retrieves the optional dependencies of the submodule.
        /// Optional dependencies are additional components or submodules
        /// that can enhance functionality but are not strictly required
        /// for the submodule to operate.
        /// </summary>
        internal virtual Type[] GetOptionalDependencies()
        {
            return Array.Empty<Type>();
        }
    }
}