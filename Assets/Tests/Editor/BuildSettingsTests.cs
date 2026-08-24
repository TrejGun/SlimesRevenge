using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Slime.Tests
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
        public void TitleImage_Exists()
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Title.png");
            Assert.IsNotNull(texture, "Assets/Art/Title.png must exist.");
        }

        [Test]
        public void MobileIdentifiers_AreConfigured()
        {
            Assert.AreEqual("com.trejgun.slime", PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android));
            Assert.AreEqual("com.trejgun.slime", PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.iOS));
        }
    }
}
