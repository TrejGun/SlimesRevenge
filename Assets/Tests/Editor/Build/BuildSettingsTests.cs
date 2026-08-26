using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;

namespace SlimesRevenge.Tests
{
    public class BuildSettingsTests
    {
        [Test]
        public void MainScene_IsEnabledInBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes;
            Assert.IsNotEmpty(scenes, "Build Settings must contain at least one scene.");
            Assert.IsTrue(
                scenes.Any(scene => scene.enabled && scene.path.EndsWith("Main.unity")),
                "Assets/Scenes/Main.unity must be enabled in Build Settings.");
        }

        [Test]
        public void GameScene_IsEnabledInBuildSettings()
        {
            Assert.IsTrue(
                EditorBuildSettings.scenes.Any(scene => scene.enabled && scene.path.EndsWith("Game.unity")),
                "Assets/Scenes/Game.unity must be enabled in Build Settings.");
        }

        [Test]
        public void MobileIdentifiers_AreConfigured()
        {
            Assert.AreEqual("com.trejgun.slimesrevenge", PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android));
            Assert.AreEqual("com.trejgun.slimesrevenge", PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.iOS));
        }

        [Test]
        public void AndroidMinSdk_IsAtLeastApi26()
        {
            Assert.GreaterOrEqual(
                (int)PlayerSettings.Android.minSdkVersion,
                (int)AndroidSdkVersions.AndroidApiLevel26,
                "Unity 6 requires Android API 26+; API 23 is obsolete and will become an error.");
        }
    }
}
