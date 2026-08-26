using NUnit.Framework;

namespace SlimesRevenge.Tests
{
    public class SlimeAppearanceTests
    {
        [Test]
        public void FromVolume_EachSubstance_MapsToOwnLook()
        {
            Assert.AreEqual(SlimeLook.Water, SlimeAppearance.FromVolume(Filled(new Water())));
            Assert.AreEqual(SlimeLook.Oil, SlimeAppearance.FromVolume(Filled(new Oil())));
            Assert.AreEqual(SlimeLook.Poison, SlimeAppearance.FromVolume(Filled(new Poison())));
            Assert.AreEqual(SlimeLook.Acid, SlimeAppearance.FromVolume(Filled(new Acid())));
            Assert.AreEqual(SlimeLook.Blood, SlimeAppearance.FromVolume(Filled(new Blood())));
            Assert.AreEqual(SlimeLook.Lava, SlimeAppearance.FromVolume(Filled(new Lava())));
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

        private static Volume Filled(params Substance[] substances)
        {
            var volume = new Volume();
            volume.Fill(substances);
            return volume;
        }
    }
}
