using System.Collections.Generic;

namespace CoreLib.Util
{
    public static class Players {
        public static List<PlayerController> GetAllPlayers() {
            Manager manager = GameManagers.GetMainManager();
            return manager.allPlayers;
        }

        public static PlayerController GetCurrentPlayer() {
            Manager manager = GameManagers.GetMainManager();
            return manager.player;
        }

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