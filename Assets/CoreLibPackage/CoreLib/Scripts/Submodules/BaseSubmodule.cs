using System;
using PugMod;

namespace CoreLib
{
    public abstract class BaseSubmodule
    {
        /// <summary>
        /// Return true if the submodule is loaded.
        /// </summary>
        public bool Loaded { get; internal set; }

        internal virtual GameVersion Build => GameVersion.zero;

        internal abstract String Version { get; }

        internal virtual Type[] Dependencies => Type.EmptyTypes;

        internal void ThrowIfNotLoaded()
        {
            if (!Loaded)
            {
                var submoduleName = GetType().GetNameChecked();
                string message = $"{submoduleName} is not loaded. Please use CoreLibMod.LoadModules(typeof({submoduleName})) to load the module!";
                throw new InvalidOperationException(message);
            }
        }

        internal virtual void SetHooks() { }
        internal virtual void Load() { }
        internal virtual void PostLoad() { }
        internal virtual void Unload() { }
        internal virtual void UnsetHooks() { }

        internal virtual bool LoadCheck()
        {
            return true;
        }

        internal virtual Type[] GetOptionalDependencies()
        {
            return Array.Empty<Type>();
        }
    }
}