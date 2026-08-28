using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SlimesRevenge
{
    /// <summary>
    /// Bootstrap-only: hold on Splash art, then hop to Main. Never used as a return target from Game.
    /// </summary>
    public sealed class StaticScreen : MonoBehaviour
    {
        [SerializeField]
        private int targetFrameRate = 30;

        [SerializeField]
        private string nextScene = "";

        [SerializeField]
        private float holdSeconds = 1f;

        private void Awake()
        {
            Application.targetFrameRate = targetFrameRate;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.orientation = ScreenOrientation.LandscapeLeft;
        }

        private IEnumerator Start()
        {
            if (!ShouldAutoAdvance(SceneManager.GetActiveScene().name, nextScene))
            {
                yield break;
            }

            if (holdSeconds > 0f)
            {
                yield return new WaitForSeconds(holdSeconds);
            }

            if (this == null || !isActiveAndEnabled)
            {
                yield break;
            }

            if (!ShouldAutoAdvance(SceneManager.GetActiveScene().name, nextScene))
            {
                yield break;
            }

            SceneManager.LoadScene(nextScene);
        }

        /// <summary>Pure gate for auto scene advance (EditMode-testable).</summary>
        public static bool ShouldAutoAdvance(string activeScene, string nextSceneName)
        {
            if (string.IsNullOrEmpty(nextSceneName) || activeScene == nextSceneName)
            {
                return false;
            }

            // Exit / menu must never bounce through Splash again.
            if (activeScene == AppNavigation.MainScene || activeScene == AppNavigation.GameScene)
            {
                return false;
            }

            return true;
        }
    }
}
