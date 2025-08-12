using System;

namespace CoreLib.Util
{
    /// <summary>
    /// The GameManagers class provides static utility methods for retrieving main and sub-manager instances
    /// within the game management system.
    /// </summary>
    public static class GameManagers {
        /// <summary>
        /// Retrieves the main manager instance within the game management system. This method ensures that
        /// the main manager is available and throws a <see cref="NullReferenceException"/> if it has not been instantiated.
        /// </summary>
        /// <returns>The main manager instance.</returns>
        /// <exception cref="NullReferenceException">Thrown if the main manager instance is null.</exception>
        public static Manager GetMainManager() {
            if (Manager.main == null) {
                CoreLibMod.Log.LogError("Could not retrieve Manager instance, has it been instantiated yet?");
                throw new NullReferenceException();
            }
            return Manager.main;

        }

        /// <summary>
        /// Retrieves an instance of the requested manager type from the main manager's list of sub-managers.
        /// If the requested manager type cannot be found, an error is logged, and an exception is thrown.
        /// </summary>
        /// <typeparam name="TManager">The specific type of manager to retrieve. Must derive from <see cref="ManagerBase"/>.</typeparam>
        /// <returns>The manager instance of the specified type.</returns>
        /// <exception cref="Exception">Thrown if a manager of the specified type is not found.</exception>
        public static TManager GetManager<TManager>() where TManager : ManagerBase {
            Manager mainManager = GetMainManager();

            foreach (var subManager in mainManager.allManagers) {
                try {
                    if (subManager is TManager castMng) {
                        return castMng;
                    }
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            CoreLibMod.Log.LogError($"Could not retrieve manager of type {typeof(TManager).ToString()}");
            throw new Exception();
        }

    }
}