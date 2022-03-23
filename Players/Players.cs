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
    }
}
