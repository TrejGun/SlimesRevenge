using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SlimesRevenge.Tests
{
    public class SlimeAppearanceTests
    {
        [SetUp]
        public void SetUp()
        {
            SlimeSprites.ResetCacheForTests();
        }

        [TearDown]
        public void TearDown()
        {
            SlimeSprites.ResetCacheForTests();
        }

        [Test]
        public void FromVolume_EachSubstance_MapsToOwnLook()
        {
            Assert.AreEqual(SlimeLook.Water, SlimeAppearance.FromVolume(Filled(new Water())));
            Assert.AreEqual(SlimeLook.Oil, SlimeAppearance.FromVolume(Filled(new Oil())));
            Assert.AreEqual(SlimeLook.Poison, SlimeAppearance.FromVolume(Filled(new Poison())));
            Assert.AreEqual(SlimeLook.Acid, SlimeAppearance.FromVolume(Filled(new Acid())));
            Assert.AreEqual(SlimeLook.Blood, SlimeAppearance.FromVolume(Filled(new Blood())));
            Assert.AreEqual(SlimeLook.Lava, SlimeAppearance.FromVolume(Filled(new Lava())));
            Assert.AreEqual(SlimeLook.Mercury, SlimeAppearance.FromVolume(Filled(new Mercury())));
        }

        [Test]
        public void FromVolume_StartingMix_PrefersWater()
        {
            var volume = new Volume();
            Slime.FillStarting(volume);
            Assert.AreEqual(SlimeLook.Water, SlimeAppearance.FromVolume(volume));
        }

        [Test]
        public void FromVolume_Empty_IsWater()
        {
            Assert.AreEqual(SlimeLook.Water, SlimeAppearance.FromVolume(new Volume()));
        }

        [Test]
        public void FromVolume_Tie_PrefersEarlierPriority()
        {
            // water and poison both 2 → water wins
            var volume = new Volume();
            volume.Fill(new Water(), new Water(), new Poison(), new Poison());
            Assert.AreEqual(SlimeLook.Water, SlimeAppearance.FromVolume(volume));
        }

        [Test]
        public void Walk_WaterEast_MatchesFdrRightRow()
        {
            Sprite water0 = null;
            Sprite water32 = null;
            Sprite water33 = null;
            var assets = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Slimes/Water.png");
            for (var i = 0; i < assets.Length; i++)
            {
                if (assets[i] is not Sprite sprite)
                {
                    continue;
                }

                if (sprite.name == "Water_0")
                {
                    water0 = sprite;
                }
                else if (sprite.name == "Water_32")
                {
                    water32 = sprite;
                }
                else if (sprite.name == "Water_33")
                {
                    water33 = sprite;
                }
            }

            Assert.IsNotNull(water0);
            Assert.IsNotNull(water32);
            Assert.IsNotNull(water33);
            var frames = SlimeSprites.Walk(water0, WalkFacing.East);
            Assert.AreEqual(FdrSheetLayout.WalkFrameCount, frames.Length);
            Assert.AreEqual(water32.rect, frames[0].rect);
            Assert.AreEqual(water33.rect, frames[1].rect);
        }

        private static Volume Filled(params Substance[] substances)
        {
            var volume = new Volume();
            volume.Fill(substances);
            return volume;
        }
    }
}
