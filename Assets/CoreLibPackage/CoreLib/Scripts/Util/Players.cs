// ========================================================
// Project: Core Library Mod (Core Keeper)
// File: Players.cs
// Author: Minepatcher, Limoka
// Created: 2025-11-07
// Description: Provides utility methods for retrieving player-related data,
//              including active players, the current player, and lookups by name.
// ========================================================

using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace CoreLib.Util
{
    /// Provides utility methods for retrieving and managing player information
    /// within the Core Keeper game environment.
    /// <remarks>
    /// This static class serves as a high-level access layer for player-related
    /// operations, allowing safe retrieval of player controllers and validation
    /// of player state. Each method performs null checks and logs errors through
    /// <see cref="CoreLibMod.Log"/> when player data is unavailable.
    /// </remarks>
    /// <seealso cref="PlayerController"/>
    /// <seealso cref="GameManagers"/>
    public static class Players
    {
        #region Player Retrieval

        /// Retrieves a list of all active players currently known to the game.
        /// <returns>
        /// A list of <see cref="PlayerController"/> instances representing all connected players.
        /// </returns>
        /// <exception cref="NullReferenceException">
        /// Thrown if the player list has not been instantiated.
        /// </exception>
        /// <remarks>
        /// This method accesses <see cref="Manager.allPlayers"/> from the active game manager.
        /// If the manager or player list is unavailable, the method logs an error and throws an exception.
        /// </remarks>
        /// <seealso cref="GameManagers.GetMainManager"/>
        /// <seealso cref="Manager.allPlayers"/>
        public static List<PlayerController> GetAllPlayers() => GameManagers.GetMainManager().allPlayers ?? 
                                                                throw new NullReferenceException($"[{CoreLibMod.Name}] Players are not instantiated.");

        /// Retrieves the <see cref="PlayerController"/> instance for the current (local) player.
        /// <returns>
        /// The <see cref="PlayerController"/> representing the current player.
        /// </returns>
        /// <exception cref="NullReferenceException">
        /// Thrown if the current player instance is not available.
        /// </exception>
        /// <remarks>
        /// Accesses <see cref="Manager.player"/> through the main game manager.
        /// This method is used to query information about the player currently in control.
        /// </remarks>
        /// <seealso cref="GameManagers.GetMainManager"/>
        /// <seealso cref="Manager.player"/>
        public static PlayerController GetCurrentPlayer() => GameManagers.GetMainManager().player ?? 
                                                             throw new NullReferenceException($"[{CoreLibMod.Name}] Current player is not instantiated.");

        /// Retrieves a specific player by their in-game name.
        /// <param name="name">The name of the player to look up.</param>
        /// <returns>
        /// A <see cref="PlayerController"/> corresponding to the specified player name.
        /// </returns>
        /// <exception cref="Exception">
        /// Thrown when no player with the given name exists in the current player list.
        /// </exception>
        /// <remarks>
        /// This method iterates through <see cref="GetAllPlayers"/> and matches against
        /// <see cref="PlayerController.playerName"/>. If no player is found, an error is logged
        /// and an exception is thrown.
        /// </remarks>
        /// <seealso cref="GetAllPlayers"/>
        /// <seealso cref="PlayerController.playerName"/>
        public static PlayerController GetPlayerByName(string name) => GetAllPlayers().Find(player => player.playerName == name) ?? 
                                                                       throw new Exception($"[{CoreLibMod.Name}] Could not find player with name '{name}'.");

        #endregion
    }
}