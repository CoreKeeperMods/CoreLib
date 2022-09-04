using Il2CppSystem.Collections.Generic;

namespace CoreLib;

public static class Players {
    public static List<PlayerController> GetAllPlayers() {
        Manager manager = GameManagers.GetMainManager();

        if (manager.allPlayers == null) {
            CoreLibPlugin.Logger.LogError("Could not retrieve players");
            throw new System.NullReferenceException();
        }

        return manager.allPlayers;
    }

    public static PlayerController GetCurrentPlayer() {
        Manager manager = GameManagers.GetMainManager();

        if (manager.player == null) {
            CoreLibPlugin.Logger.LogError("Could not retrieve players");
            throw new System.NullReferenceException();
        }

        return manager.player;
    }

    public static PlayerController GetPlayerByName(string name) {
        List<PlayerController> playerList = GetAllPlayers();
        foreach (var playerController in playerList) {
            CoreLibPlugin.Logger.LogInfo(playerController.playerName);
            if (playerController.playerName == name) {
                return playerController;
            }
        }

        CoreLibPlugin.Logger.LogError($"Could not find player with name {name}");
        throw new System.Exception();
    }
}