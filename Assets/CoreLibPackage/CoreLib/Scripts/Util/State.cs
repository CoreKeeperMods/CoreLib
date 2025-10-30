// ReSharper disable once CheckNamespace
namespace CoreLib.Util
{
    /// The State class provides utility methods to determine the current state of the game.
    public static class State {
        /// <returns>Returns <c>true</c> if the current scene handler indicates the game is in play; otherwise, returns <c>false</c>.</returns>
        public static bool IsInGame() {
            var manager = GameManagers.GetMainManager();
            return manager.currentSceneHandler.isInGame;
        }
    }
}