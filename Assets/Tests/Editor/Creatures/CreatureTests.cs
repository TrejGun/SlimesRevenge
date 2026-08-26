using System.Linq;
using NUnit.Framework;

namespace SlimesRevenge.Tests
{
    public class CreatureTests
    {
        [Test]
        public void SlimeVolume_IsSixUnitsIncludingLava()
        {
            var volume = new Volume();
            Slime.FillStarting(volume);

            Assert.AreEqual(6, volume.UnitCount);
            Assert.AreEqual(2, volume.Units.Count(unit => unit.Substance is Water));
            Assert.IsTrue(volume.Units.Any(unit => unit.Substance is Oil));
            Assert.IsTrue(volume.Units.Any(unit => unit.Substance is Poison));
            Assert.IsTrue(volume.Units.Any(unit => unit.Substance is Acid));
            Assert.IsTrue(volume.Units.Any(unit => unit.Substance is Lava));
        }

        [Test]
        public void RatVolume_IsOneUnitOfBlood()
        {
            var volume = new Volume();
            Rat.FillStarting(volume);

            Assert.AreEqual(1, volume.UnitCount);
            Assert.IsInstanceOf<Blood>(volume.Units[0].Substance);
        }

        [Test]
        public void CatVolume_IsTwoUnitsOfBlood()
        {
            var volume = new Volume();
            Cat.FillStarting(volume);

            Assert.AreEqual(2, volume.UnitCount);
            Assert.IsTrue(volume.Units.All(unit => unit.Substance is Blood));
        }

        [Test]
        public void DogVolume_IsThreeUnitsOfBlood()
        {
            var volume = new Volume();
            Dog.FillStarting(volume);

            Assert.AreEqual(3, volume.UnitCount);
            Assert.IsTrue(volume.Units.All(unit => unit.Substance is Blood));
        }
    }
}
