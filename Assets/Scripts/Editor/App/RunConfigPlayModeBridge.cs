using UnityEditor;
using UnityEngine;

namespace SlimesRevenge.Editor
{
    /// <summary>
    /// Keeps <see cref="RunConfig"/> across play-mode domain reload using SessionState (RAM only).
    /// </summary>
    [InitializeOnLoad]
    public static class RunConfigPlayModeBridge
    {
        private const string SessionKey = "SlimesRevenge.RunConfig.Pending";

        static RunConfigPlayModeBridge()
        {
            // After domain reload into Play Mode, reinject before scene Awake.
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Restore();
            }

            RunConfig.Cleared -= Erase;
            RunConfig.Cleared += Erase;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                Save();
            }
        }

        private static void Save()
        {
            if (RunConfig.TryExportPending(out var payload))
            {
                SessionState.SetString(SessionKey, payload);
            }
            else
            {
                Erase();
            }
        }

        private static void Restore()
        {
            var payload = SessionState.GetString(SessionKey, string.Empty);
            if (string.IsNullOrEmpty(payload))
            {
                return;
            }

            RunConfig.ImportPending(payload);
            Debug.Log($"[RunConfig] Restored {payload} after domain reload.");
        }

        private static void Erase()
        {
            SessionState.EraseString(SessionKey);
        }
    }
}
