using System;
using PugMod;

namespace CoreLib
{
    /// <summary>
    /// Represents an abstract base class for all submodules in the CoreLib framework.
    /// Provides fundamental properties and methods required for submodule management,
    /// such as loading, unloading, dependency handling, and versioning.
    /// </summary>
    /// <remarks>
    /// Subclasses of <see cref="BaseSubmodule"/> are expected to implement specific functionality
    /// for various modules. This class enforces the implementation of the <see cref="Version"/>
    /// property and provides virtual methods that can be overridden to customize module behavior.
    /// Typical usage of a submodule includes implementing methods for initialization and cleanup,
    /// defining dependencies, and providing specific build and version details.
    /// </remarks>
    public abstract class BaseSubmodule
    {
        /// <summary>
        /// Represents the state of the submodule, indicating whether it has been loaded successfully.
        /// </summary>
        public bool Loaded { get; internal set; }

        /// <summary>
        /// Represents the build version of the submodule, indicating its specific version and compatibility.
        /// </summary>
        internal virtual GameVersion Build => GameVersion.zero;

        /// <summary>
        /// Gets the version number of the submodule as a string.
        /// </summary>
        /// <remarks>
        /// The version indicates the release or development iteration of the specific submodule.
        /// It is typically used for compatibility checks and dependency management within the framework.
        /// </remarks>
        internal abstract String Version { get; }

        /// <summary>
        /// Represents the required dependencies that must be loaded for the submodule to function correctly.
        /// </summary>
        internal virtual Type[] Dependencies => Type.EmptyTypes;

        /// <summary>
        /// Ensures that the submodule has been properly loaded before performing any operations that depend on its loaded state.
        /// This method throws an exception if the submodule has not been loaded, providing a detailed error message to guide the user.
        /// </summary>
        /// <remarks>
        /// The <c>ThrowIfNotLoaded</c> method is typically used to safeguard operations that require a submodule to be in a loaded state.
        /// It checks the <c>Loaded</c> property of the submodule and raises an exception if the property is <c>false</c>.
        /// This is a utility method intended to prevent misconfiguration or improper usage of submodules within the CoreLib framework.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the submodule has not been loaded. The exception message provides the name of the submodule
        /// and suggests loading the module using the <c>CoreLibMod.LoadModules</c> method.
        /// </exception>
        internal void ThrowIfNotLoaded()
        {
            if (!Loaded)
            {
                var submoduleName = GetType().GetNameChecked();
                string message = $"{submoduleName} is not loaded. Please use CoreLibMod.LoadModules(typeof({submoduleName})) to load the module!";
                throw new InvalidOperationException(message);
            }
        }

        /// <summary>
        /// Configures and applies the necessary hooks for the current submodule.
        /// This method establishes any event bindings, listeners, or connections required for the submodule's operation within its context.
        /// </summary>
        /// <remarks>
        /// The <c>SetHooks</c> method is called as part of a submodule's lifecycle during its initialization phase,
        /// ensuring that the submodule is properly integrated with the overarching system or other dependent modules.
        /// Implementations of this method in derived classes are expected to define specific hooks relevant to the respective submodule.
        /// </remarks>
        internal virtual void SetHooks() { }

        /// <summary>
        /// Initiates the loading process for the submodule, performing any required logic to prepare the submodule for use.
        /// This method is invoked during the submodule initialization process and is intended to be overridden by derived classes.
        /// </summary>
        /// <remarks>
        /// The <c>Load</c> method is responsible for implementing the specific loading behaviors for a submodule.
        /// Derived classes should override this method to provide the custom loading logic needed for their functionality.
        /// Successful execution of this method transitions the submodule into a fully loaded state. Once loaded, the <c>Loaded</c>
        /// property will be set to <c>true</c>.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the loading process encounters unrecoverable issues, such as invalid or missing dependencies.
        /// </exception>
        internal virtual void Load() { }

        /// <summary>
        /// Performs additional initialization or setup tasks for the submodule after it has been loaded.
        /// This method is intended to be overridden by derived classes to implement specific post-load logic,
        /// such as subscribing to events, initializing configuration, binding keys, or registering handlers.
        /// </summary>
        /// <remarks>
        /// The <c>PostLoad</c> method is called automatically after the submodule's <c>Load</c> method completes
        /// successfully as part of the module loading process in the CoreLib framework.
        /// Implementations should not call this method directly unless explicitly required for specialized use cases.
        /// </remarks>
        /// <exception cref="Exception">
        /// An exception may be thrown if an error occurs during the execution of the overriden method implementation,
        /// which could affect subsequent module initialization.
        /// </exception>
        internal virtual void PostLoad() { }

        /// <summary>
        /// Releases resources and performs necessary cleanup operations associated with the submodule.
        /// This method is called when the submodule is being unloaded and should be used to reverse
        /// or undo changes made by the submodule during its operation.
        /// </summary>
        /// <remarks>
        /// The <c>Unload</c> method is designed to handle the graceful shutdown of a submodule. It can
        /// implement cleanup logic specific to the submodule, such as releasing memory, closing
        /// connections, deregistering event handlers, or deallocating any other external resources.
        /// Subclasses of <see cref="BaseSubmodule"/> can override this method to provide their own
        /// cleanup implementation as required.
        /// </remarks>
        /// <exception cref="Exception">
        /// Exceptions that occur during the cleanup process should be handled appropriately, as any
        /// thrown exceptions may disrupt the unloading process of dependent submodules.
        /// </exception>
        internal virtual void Unload() { }

        /// <summary>
        /// Removes all hooks associated with the submodule, effectively undoing changes made during the hook setup process.
        /// This method is intended to clean up any modifications to external components or systems caused by the <c>SetHooks</c> method.
        /// </summary>
        /// <remarks>
        /// The <c>UnsetHooks</c> method is typically called during the unloading or shutdown phase of a submodule.
        /// It ensures that any previously applied hooks, event handlers, or custom logic are properly removed to leave the application
        /// in a consistent state. Subclasses can override this method to implement specific hook removal logic based on their behavior.
        /// </remarks>
        internal virtual void UnsetHooks() { }

        /// <summary>
        /// Determines whether the module meets the conditions required for loading.
        /// This method allows for load validation and can be overridden by derived classes
        /// to implement custom logic for assessing whether the module can be safely loaded.
        /// </summary>
        /// <remarks>
        /// The default implementation of the <c>LoadCheck</c> method always returns <c>true</c>,
        /// indicating that the module can be loaded without any specific validation.
        /// Subclasses can override this method to enforce conditions such as dependency availability
        /// or other prerequisites before the module is loaded.
        /// </remarks>
        /// <returns>
        /// A boolean value indicating whether the module satisfies the requirements to be loaded.
        /// Returns <c>true</c> by default, unless overridden with additional conditions.
        /// </returns>
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
        /// <remarks>
        /// The <c>GetOptionalDependencies</c> method provides a way to extend
        /// the capabilities of a submodule by listing other submodules or
        /// components that it can work with, if available. These dependencies
        /// are not mandatory, and the absence of such dependencies will not
        /// hinder the submodule's basic functionality.
        /// </remarks>
        /// <returns>
        /// An array of <see cref="Type"/> representing the optional dependencies
        /// of the submodule. If no optional dependencies are present, an empty
        /// array is returned.
        /// </returns>
        internal virtual Type[] GetOptionalDependencies()
        {
            return Array.Empty<Type>();
        }
    }
}