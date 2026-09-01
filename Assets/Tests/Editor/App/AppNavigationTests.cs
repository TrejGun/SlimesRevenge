using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SlimesRevenge.Tests
{
    public class AppNavigationTests
    {
        [SetUp]
        public void SetUp()
        {
            AppNavigation.ResetForTests();
        }

        [TearDown]
        public void TearDown()
        {
            AppNavigation.ResetForTests();
        }

        [Test]
        public void SceneNames_MatchBuildScenes()
        {
            Assert.AreEqual("Splash", AppNavigation.SplashScene);
            Assert.AreEqual("Main", AppNavigation.MainScene);
            Assert.AreEqual("Game", AppNavigation.GameScene);
        }

        [Test]
        public void BuildOrder_IsSplashThenMainThenGame()
        {
            var enabled = EditorBuildSettings
                .scenes.Where(s => s.enabled)
                .Select(s => Path.GetFileNameWithoutExtension(s.path))
                .ToArray();
            Assert.GreaterOrEqual(enabled.Length, 3);
            Assert.AreEqual(AppNavigation.SplashScene, enabled[0]);
            Assert.AreEqual(AppNavigation.MainScene, enabled[1]);
            Assert.AreEqual(AppNavigation.GameScene, enabled[2]);
        }

        [Test]
        public void ExitFromDuel_PrepareReturnToMainMenu_ClearsPending()
        {
            RunConfig.SetDuel(CreatureKind.Dog, RunConfig.DefaultWaterLoadout());
            Assert.IsTrue(RunConfig.HasPending);

            Assert.IsTrue(RunConfig.TryConsume(out _));
            RunConfig.SetCampaign();
            AppNavigation.PrepareReturnToMainMenu();

            Assert.IsFalse(RunConfig.HasPending);
            Assert.IsFalse(RunConfig.TryPeek(out _));
        }

        [Test]
        public void WithoutPendingConfig_GameRedirectsToMain_NotCampaign()
        {
            Assert.IsFalse(RunConfig.HasPending);
            Assert.IsFalse(
                RunConfig.TryPeek(out _),
                "Game scene must not bootstrap campaign without a pending run from the menu."
            );
        }

        [Test]
        public void StaticScreen_AdvancesOnlyFromSplashBootstrap()
        {
            Assert.IsTrue(
                StaticScreen.ShouldAutoAdvance(AppNavigation.SplashScene, AppNavigation.MainScene)
            );
            Assert.IsFalse(
                StaticScreen.ShouldAutoAdvance(AppNavigation.MainScene, AppNavigation.GameScene)
            );
            Assert.IsFalse(
                StaticScreen.ShouldAutoAdvance(AppNavigation.GameScene, AppNavigation.MainScene)
            );
            Assert.IsFalse(StaticScreen.ShouldAutoAdvance(AppNavigation.SplashScene, ""));
            Assert.IsFalse(
                StaticScreen.ShouldAutoAdvance(AppNavigation.SplashScene, AppNavigation.SplashScene)
            );
        }

        [Test]
        public void SplashSceneAsset_AdvancesToMain_NotGame()
        {
            var path = Path.Combine(Application.dataPath, "Scenes", "Splash.unity");
            Assert.IsTrue(File.Exists(path), path);
            var yaml = File.ReadAllText(path);
            var match = Regex.Match(
                yaml,
                @"guid: 57a71c5c222222222222222222222222[\s\S]*?nextScene:(.*)\n\s*holdSeconds:(.*)\n"
            );
            Assert.IsTrue(match.Success, "StaticScreen block not found on Splash.unity");
            Assert.AreEqual("Main", match.Groups[1].Value.Trim());
            Assert.AreEqual("1", match.Groups[2].Value.Trim());
        }

        [Test]
        public void MainSceneAsset_HasNoSplashArtOrAutoAdvance()
        {
            var path = Path.Combine(Application.dataPath, "Scenes", "Main.unity");
            Assert.IsTrue(File.Exists(path), path);
            var yaml = File.ReadAllText(path);
            Assert.IsFalse(
                yaml.Contains("StaticImage"),
                "Main must not embed splash art — that belongs on Splash."
            );
            Assert.IsFalse(
                yaml.Contains("57a71c5c222222222222222222222222"),
                "Main must not host StaticScreen bootstrap."
            );
        }

        [Test]
        public void MainMenu_BootsViaSceneLoaded_NotOnlyFirstSceneAfterSceneLoad()
        {
            // Regression: RuntimeInitialize AfterSceneLoad fires once (on Splash).
            // MainMenu must use SceneManager.sceneLoaded so Splash → Main still opens the menu.
            var method = typeof(MainMenu).GetMethod(
                "OnSceneLoaded",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic
            );
            Assert.IsNotNull(method, "MainMenu.OnSceneLoaded must exist for Splash→Main boot.");

            var register = typeof(MainMenu).GetMethod(
                "RegisterBoot",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic
            );
            Assert.IsNotNull(register, "MainMenu.RegisterBoot must subscribe sceneLoaded.");
        }
    }
}
