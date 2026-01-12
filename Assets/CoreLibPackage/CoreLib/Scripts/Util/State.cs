// ========================================================
// Project: Core Library Mod (Core Keeper)
// File: State.cs
// Author: Minepatcher, Limoka
// Created: 2025-11-07
// Description: Provides utility methods for checking the current game state,
//              including whether the game is actively running or in a specific scene.
// ========================================================

using System;

// ReSharper disable once CheckNamespace
namespace CoreLib.Util
{
    /// Provides utility methods for determining the current state of the game.
    /// <remarks>
    /// This static helper class allows CoreLib modules and other systems to easily check
    /// whether the game is currently active, paused, or within an in-game scene.
    /// </remarks>
    /// <seealso cref="GameManagers"/>
    /// <seealso cref="Manager.currentSceneHandler"/>
    public static class State
    {
        #region Game State

        /// Determines whether the game is currently in an active gameplay scene.
        /// <returns>
        /// <c>true</c> if the current scene handler indicates that gameplay is in progress; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method retrieves the main <see cref="Manager"/> instance and checks its
        /// <see cref="Manager.currentSceneHandler"/> to confirm whether the game is in a playable state.
        /// </remarks>
        /// <exception cref="NullReferenceException">
        /// Thrown if the main manager or its scene handler is not initialized.
        /// </exception>
        /// <seealso cref="GameManagers.GetMainManager"/>
        /// <seealso cref="Manager"/>
        public static bool IsInGame()
        {
            var sceneHandler = GameManagers.GetMainManager().currentSceneHandler;
            return sceneHandler != null ? sceneHandler.isInGame
                : throw new NullReferenceException($"[{CoreLibMod.Name}] Scene Handler is not initialized.");
        }

        #endregion
    }
}