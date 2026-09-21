// ========================================================
// Project: Core Library Mod (Core Keeper)
// File: GameManagers.cs
// Author: Minepatcher, Limoka
// Created: 2025-11-07
// Description: Provides static utility methods for retrieving main and sub-manager
//              instances within the game management system, ensuring safe access
//              and error handling.
// ========================================================

using System;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace CoreLib.Util
{
    /// Provides static helper methods for retrieving <see cref="Manager"/> and <see cref="ManagerBase"/>
    /// instances used within the Core Keeper game management system.
    /// <remarks>
    /// The <see cref="GameManagers"/> class offers consistent and safe access to the primary
    /// <see cref="Manager"/> singleton and its associated submanagers, with built-in validation
    /// and error logging through <see cref="CoreLibMod.log"/>.
    /// </remarks>
    /// <seealso cref="Manager"/>
    /// <seealso cref="ManagerBase"/>
    public static class GameManagers
    {
        #region Main Manager Retrieval

        /// Retrieves the singleton instance of the game's main <see cref="Manager"/>.
        /// <returns>The active <see cref="Manager"/> instance.</returns>
        /// <exception cref="NullReferenceException">
        /// Thrown if the main manager instance (<see cref="Manager.main"/>) is not yet initialized.
        /// </exception>
        /// <remarks>
        /// This method ensures that the main manager exists before allowing access.
        /// If unavailable, an error is logged via <see cref="CoreLibMod.log"/>.
        /// </remarks>
        /// <seealso cref="Manager"/>
        public static Manager GetMainManager() => Manager.main != null ? Manager.main : throw new NullReferenceException($"[{CoreLibMod.NAME}] Manager instance has not been instantiated.");

        #endregion

        #region Sub-Manager Retrieval

        /// Retrieves a submanager of the specified type from the active <see cref="Manager"/>.
        /// <typeparam name="T">
        /// The specific type of manager to retrieve. Must derive from <see cref="ManagerBase"/>.
        /// </typeparam>
        /// <returns>
        /// The submanager instance matching the specified type <typeparamref name="T"/>.
        /// </returns>
        /// <exception cref="Exception">
        /// Thrown if no manager of the specified type is found among <see cref="Manager.allManagers"/>.
        /// </exception>
        /// <remarks>
        /// This method lists all submanagers registered under <see cref="Manager.allManagers"/>,
        /// returning the first match of type <typeparamref name="T"/>.
        /// If no match is found, a descriptive error is logged and an exception is thrown.
        /// </remarks>
        /// <seealso cref="Manager.allManagers"/>
        /// <seealso cref="ManagerBase"/>
        public static T GetManager<T>() where T : ManagerBase => (T)GetMainManager().allManagers.Find(subManager => subManager is T) ?? throw new Exception($"[{CoreLibMod.NAME}] Could not retrieve manager of type {typeof(T)}.");

        #endregion
    }
}