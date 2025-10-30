using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace CoreLib.Util
{
    /// Provides utility methods for managing and retrieving information about game players.
    public static class Players
    {
        /// <returns>A list of <see cref="PlayerController"/> instances representing all players.</returns>
        public static List<PlayerController> GetAllPlayers() {
            var manager = GameManagers.GetMainManager();
            if(manager.allPlayers != null) return manager.allPlayers;
            string m = $"Players are not instantiated.";
            CoreLibMod.Log.LogError(m);
            throw new NullReferenceException(m);
        }
        
        /// <returns>The <see cref="PlayerController"/> representing the current player.</returns>
        public static PlayerController GetCurrentPlayer() {
            var manager = GameManagers.GetMainManager();
            if(manager.player != null) return manager.player;
            string m = $"Current Player are not instantiated.";
            CoreLibMod.Log.LogError(m);
            throw new NullReferenceException(m);
        }
        
        /// <param name="name">The name of the player whose controller is to be retrieved.</param>
        /// <returns>A <see cref="PlayerController"/> instance representing the player with the specified name.</returns>
        /// <exception cref="System.Exception">Thrown when no player with the specified name is found.</exception>
        public static PlayerController GetPlayerByName(string name) {
            var playerList = GetAllPlayers();
            foreach (var playerController in playerList.Where(playerController => playerController.playerName == name))
                return playerController;
            string m = $"Could not find player with name {name}";
            CoreLibMod.Log.LogError(m);
            throw new Exception(m);
        }
    }
}