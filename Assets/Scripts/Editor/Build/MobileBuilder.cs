using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

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

        public static void PlayInEditor()
        {
            var scene = "Assets/Scenes/Main.unity";
            if (!EditorSceneManager.OpenScene(scene).IsValid())
            {
                throw new InvalidOperationException($"Failed to open {scene}");
            }

            EditorApplication.delayCall += () => { EditorApplication.isPlaying = true; };
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
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
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

            var scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                throw new InvalidOperationException("No enabled scenes in Build Settings.");
            }

            var absoluteOutput = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), outputPath));
            var outputDirectory = target == BuildTarget.iOS
                ? absoluteOutput
                : Path.GetDirectoryName(absoluteOutput);
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
                options = BuildOptions.CompressWithLz4
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"{target} build failed: {report.summary.result} ({report.summary.totalErrors} errors)");
            }

            Debug.Log($"{target} build succeeded: {absoluteOutput}");
        }
    }
}
