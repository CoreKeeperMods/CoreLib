namespace CoreLib {
    public class State {
        public static bool IsInGame() {
            Manager manager = GameManagers.GetMainManager();
            return manager.currentSceneHandler.isInGame;
        }
    }
}
