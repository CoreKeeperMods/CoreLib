using Il2CppSystem.Collections.Generic;

namespace CoreLib {
    public class Players {
        public static List<PlayerController> GetAllPlayers() {
            Manager manager = GameManagers.GetMainManager();

            if (manager.allPlayers == null) {
                CoreLib.Logger.LogError("Could not retrieve players");
                throw new System.NullReferenceException();
            }

            return manager.allPlayers;
        }

        public static PlayerController GetCurrentPlayer() {
            Manager manager = GameManagers.GetMainManager();

            if (manager.player == null) {
                CoreLib.Logger.LogError("Could not retrieve players");
                throw new System.NullReferenceException();
            }

            return manager.player;
        }

        public static PlayerController GetPlayerByName(string name) {
            List<PlayerController> playerList = GetAllPlayers();
            foreach (var playerController in playerList) {
                CoreLib.Logger.LogInfo(playerController.playerName);
                if (playerController.playerName == name) {
                    return playerController;
                }
            }

            CoreLib.Logger.LogError($"Could not find player with name {name}");
            throw new System.Exception();
        }
    }
}
