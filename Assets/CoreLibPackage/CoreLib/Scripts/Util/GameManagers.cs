using System;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace CoreLib.Util
{
    /// The GameManagers class provides static utility methods for retrieving main and sub-manager instances within the game management system.
    public static class GameManagers {
        /// <returns>The main manager instance.</returns>
        /// <exception cref="NullReferenceException">Thrown if the main manager instance is null.</exception>
        public static Manager GetMainManager() {
            if (Manager.main != null) return Manager.main;
            string m = $"Manager instance has not been instantiated.";
            CoreLibMod.Log.LogError(m);
            throw new NullReferenceException(m);
        }

        /// <typeparam name="T">The specific type of manager to retrieve. Must derive from <see cref="ManagerBase"/>.</typeparam>
        /// <returns>The manager instance of the specified type.</returns>
        /// <exception cref="Exception">Thrown if a manager of the specified type is not found.</exception>
        public static T GetManager<T>() where T : ManagerBase {
            var mainManager = GetMainManager();
            
            foreach (var subManager in mainManager.allManagers.Where(subManager => subManager is T))
                return (T)subManager;

            string m = $"Could not retrieve manager of type {typeof(T)}";
            CoreLibMod.Log.LogError(m);
            throw new Exception(m);
        }

    }
}