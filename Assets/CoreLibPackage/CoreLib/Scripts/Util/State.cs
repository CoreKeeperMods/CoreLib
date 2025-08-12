namespace CoreLib.Util
{
    /// <summary>
    /// The State class provides utility methods to determine the current state of the game.
    /// </summary>
    public static class State {
        /// <summary>
        /// Determines whether the game is currently in a playable state.
        /// </summary>
        /// <returns>
        /// Returns <c>true</c> if the current scene handler indicates the game is in play; otherwise, returns <c>false</c>.
        /// </returns>
        public static bool IsInGame() {
            Manager manager = GameManagers.GetMainManager();
            return manager.currentSceneHandler.isInGame;
        }
    }
}