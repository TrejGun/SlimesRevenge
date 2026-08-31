using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SlimesRevenge.Editor
{
    public static class MobileBuilder
    {
        private const string AndroidOutput = "Build/Android/SlimesRevenge.apk";
        private const string IosOutput = "Build/iOS";
        private const string OsxOutput = "Build/macOS/SlimesRevenge.app";

        public static void BuildAndroid()
        {
            Build(BuildTarget.Android, AndroidOutput);
        }

        public static void BuildIOS()
        {
            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.SimulatorSDK;
            Build(BuildTarget.iOS, IosOutput);
        }

        /// <summary>Splash → Main menu (full cold start).</summary>
        public static void PlayInEditor()
        {
            RunConfig.Clear();
            PlayScene("Assets/Scenes/Splash.unity");
        }

        /// <summary>Game campaign with default cast (skips splash/menu).</summary>
        public static void PlayCampaign()
        {
            RunConfig.SetCampaign();
            PlayScene("Assets/Scenes/Game.unity");
        }

        /// <summary>Duel vs Bat with default water loadout (skips splash/menu).</summary>
        public static void PlayDuel()
        {
            RunConfig.SetDuel(CreatureKind.Bat, RunConfig.DefaultWaterLoadout());
            PlayScene("Assets/Scenes/Game.unity");
        }

        private static void PlayScene(string scene)
        {
            var opened = EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
            if (!opened.IsValid())
            {
                throw new InvalidOperationException($"Failed to open {scene}");
            }

            if (Camera.main == null && UnityEngine.Object.FindFirstObjectByType<Camera>() == null)
            {
                throw new InvalidOperationException(
                    $"Scene '{scene}' has no Camera — refusing Play Mode (would show 'No cameras rendering')."
                );
            }

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.delayCall += () =>
            {
                EditorApplication.isPlaying = true;
            };
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode)
            {
                return;
            }

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            var cam = Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>();
            var scene = SceneManager.GetActiveScene().path;
            if (cam == null || !cam.enabled || !cam.gameObject.activeInHierarchy)
            {
                Debug.LogError(
                    $"[Play] No active camera in '{scene}'. Game view will show 'No cameras rendering'."
                );
                return;
            }

            Debug.Log(
                $"[Play] OK camera '{cam.name}' in '{scene}' (display {cam.targetDisplay}, enabled)."
            );

            // Game boots signal from WorldView ([PlayReady] Duel|Campaign). Menu cold-start here.
            if (scene.EndsWith("Splash.unity", StringComparison.Ordinal) || scene.EndsWith("Main.unity", StringComparison.Ordinal))
            {
                Debug.Log("[PlayReady] Splash");
            }
        }

        public static void BuildOSX()
        {
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            Build(BuildTarget.StandaloneOSX, OsxOutput);
        }

        public static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "TrejGun";
            PlayerSettings.productName = "Slime's Revenge";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.trejgun.slimesrevenge");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.trejgun.slimesrevenge");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;
            PlayerSettings.SplashScreen.show = true;
            AssetDatabase.SaveAssets();
        }

        private static void Build(BuildTarget target, string outputPath)
        {
            ConfigurePlayer();

            var scenes = EditorBuildSettings
                .scenes.Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                throw new InvalidOperationException("No enabled scenes in Build Settings.");
            }

            var absoluteOutput = Path.GetFullPath(
                Path.Combine(Directory.GetCurrentDirectory(), outputPath)
            );
            var outputDirectory =
                target == BuildTarget.iOS ? absoluteOutput : Path.GetDirectoryName(absoluteOutput);
            Directory.CreateDirectory(outputDirectory ?? "Build");

            if (target == BuildTarget.Android)
            {
                EditorUserBuildSettings.buildAppBundle = false;
                EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
            }

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = absoluteOutput,
                target = target,
                options = BuildOptions.CompressWithLz4,
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"{target} build failed: {report.summary.result} ({report.summary.totalErrors} errors)"
                );
            }

            Debug.Log($"{target} build succeeded: {absoluteOutput}");
        }
    }
}
