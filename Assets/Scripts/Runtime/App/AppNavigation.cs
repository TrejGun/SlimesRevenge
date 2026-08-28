using UnityEngine.SceneManagement;

namespace SlimesRevenge
{
    /// <summary>
    /// Scene hops. Build order: Splash (cold start only) → Main ↔ Game.
    /// Exit never reloads Splash.
    /// </summary>
    public static class AppNavigation
    {
        public const string SplashScene = "Splash";
        public const string MainScene = "Main";
        public const string GameScene = "Game";

        /// <summary>Clears pending run so reloading Game cannot silently become campaign/duel.</summary>
        public static void PrepareReturnToMainMenu()
        {
            RunConfig.Clear();
        }

        public static void GoToMainMenu()
        {
            PrepareReturnToMainMenu();
            SceneManager.LoadScene(MainScene);
        }

        public static void GoToGame()
        {
            SceneManager.LoadScene(GameScene);
        }

        public static void ResetForTests()
        {
            RunConfig.Clear();
        }
    }
}
