namespace CoreLib.Util
{
    public static class State {
        public static bool IsInGame() {
            Manager manager = GameManagers.GetMainManager();
            return manager.currentSceneHandler.isInGame;
        }
    }
}