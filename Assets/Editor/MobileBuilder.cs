using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Slime.Editor
{
    public static class MobileBuilder
    {
        private const string AndroidOutput = "Build/Android/slime.apk";
        private const string IosOutput = "Build/iOS";

        public static void BuildAndroid()
        {
            Build(BuildTarget.Android, AndroidOutput);
        }

        public static void BuildIOS()
        {
            Build(BuildTarget.iOS, IosOutput);
        }

        public static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "TrejGun";
            PlayerSettings.productName = "Slime";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.overrideDefaultApplicationIdentifier = true;
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.trejgun.slime");
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, "com.trejgun.slime");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel22;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.iOS.targetOSVersionString = "13.0";
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
