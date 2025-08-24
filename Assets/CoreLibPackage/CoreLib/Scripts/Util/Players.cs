using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace CoreLib.Util
{
    /// <summary>
    /// Provides utility methods for managing and retrieving information about game players.
    /// </summary>
    public static class Players
    {
        /// <summary>
        /// Retrieves a list of all player controllers currently active in the game.
        /// </summary>
        /// <returns>A list of <see cref="PlayerController"/> instances representing all players.</returns>
        public static List<PlayerController> GetAllPlayers() {
            Manager manager = GameManagers.GetMainManager();
            return manager.allPlayers;
        }

        /// <summary>
        /// Retrieves the player controller associated with the current active player.
        /// </summary>
        /// <returns>The <see cref="PlayerController"/> representing the current player.</returns>
        public static PlayerController GetCurrentPlayer() {
            Manager manager = GameManagers.GetMainManager();
            return manager.player;
        }

        /// <summary>
        /// Retrieves the player controller for a player with the specified name.
        /// </summary>
        /// <param name="name">The name of the player whose controller is to be retrieved.</param>
        /// <returns>A <see cref="PlayerController"/> instance representing the player with the specified name.</returns>
        /// <exception cref="System.Exception">Thrown when no player with the specified name is found.</exception>
        public static PlayerController GetPlayerByName(string name) {
            List<PlayerController> playerList = GetAllPlayers();
            foreach (var playerController in playerList) {
                if (playerController.playerName == name) {
                    return playerController;
                }
            }

            CoreLibMod.Log.LogError($"Could not find player with name {name}");
            throw new System.Exception();
        }
    }
}