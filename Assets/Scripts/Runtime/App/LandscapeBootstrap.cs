using System.Collections;
using UnityEngine;

namespace SlimesRevenge
{
    /// <summary>
    /// Standalone ignores <see cref="Screen.orientation"/> for window shape.
    /// Ensure a wide window from the current display (no fixed pixel sizes).
    /// </summary>
    public static class LandscapeBootstrap
    {
        private const float Aspect = 16f / 9f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void BeforeSceneLoad()
        {
            Screen.orientation = ScreenOrientation.LandscapeLeft;
#if UNITY_STANDALONE || UNITY_EDITOR
            ApplyLandscapeWindow();
            var runner = new GameObject("LandscapeBootstrap").AddComponent<LandscapeBootstrapRunner>();
            Object.DontDestroyOnLoad(runner.gameObject);
#endif
        }

        public static void ApplyLandscapeWindow()
        {
#if UNITY_STANDALONE || UNITY_EDITOR
            var displayW = Display.main.systemWidth;
            var displayH = Display.main.systemHeight;
            if (displayW <= 0 || displayH <= 0)
            {
                displayW = Screen.currentResolution.width;
                displayH = Screen.currentResolution.height;
            }

            var width = Mathf.Max(1, Mathf.RoundToInt(displayW * 0.9f));
            var height = Mathf.Max(1, Mathf.RoundToInt(width / Aspect));
            if (height > displayH)
            {
                height = Mathf.Max(1, Mathf.RoundToInt(displayH * 0.9f));
                width = Mathf.Max(1, Mathf.RoundToInt(height * Aspect));
            }

            Screen.fullScreenMode = FullScreenMode.Windowed;
            Screen.SetResolution(width, height, FullScreenMode.Windowed);
#endif
        }
    }

    internal sealed class LandscapeBootstrapRunner : MonoBehaviour
    {
        private IEnumerator Start()
        {
            yield return null;
            yield return new WaitForEndOfFrame();
            LandscapeBootstrap.ApplyLandscapeWindow();
            Destroy(gameObject);
        }
    }
}
