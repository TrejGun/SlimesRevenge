using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SlimesRevenge
{
    public sealed class StaticScreen : MonoBehaviour
    {
        [SerializeField]
        private int targetFrameRate = 30;

        [SerializeField]
        private string nextScene = "Game";

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
            if (string.IsNullOrEmpty(nextScene) || SceneManager.GetActiveScene().name == nextScene)
            {
                yield break;
            }

            if (holdSeconds > 0f)
            {
                yield return new WaitForSeconds(holdSeconds);
            }

            SceneManager.LoadScene(nextScene);
        }
    }
}
