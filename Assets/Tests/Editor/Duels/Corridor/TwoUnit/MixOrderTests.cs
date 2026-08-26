using NUnit.Framework;
using UnityEngine;

namespace SlimesRevenge.Tests.Duels.Corridor.TwoUnit
{
    /// <summary>2 units mixes on the corridor: order of units changes retaliation.</summary>
    public class MixOrderTests : DuelFixture
    {
        [Test]
        public void WaterThenFlee_RatBurnsToDeath()
        {
            var duel = StartCorridorDuel(typeof(Rat), new Water(), new Lava());
            Assert.IsTrue(duel.Turns.TryAttack(CorridorFoeCell, new Water()));
            Assert.AreEqual(2, duel.Beast.HitPoints);
            Assert.AreEqual(1, duel.Beast.CountStatus<Burning>());
            Assert.AreEqual(0, duel.Slime.Volume.UnitCount);
            Assert.IsTrue(duel.Slime.IsAlive);

            Assert.IsTrue(duel.Turns.TryMoveTo(new Vector2Int(3, 0)));
            Assert.AreEqual(1, duel.Beast.HitPoints);
            Assert.IsTrue(duel.Beast.IsAlive);

            Assert.IsTrue(duel.Turns.TryMoveTo(new Vector2Int(2, 0)));
            Assert.IsFalse(duel.Beast.IsAlive);
            Assert.IsTrue(duel.Slime.IsAlive);
        }

        [Test]
        public void LavaThenAttack_RatExtinguishesAtOneHit()
        {
            var duel = StartCorridorDuel(typeof(Rat), new Lava(), new Water());
            Assert.IsTrue(duel.Turns.TryAttack(CorridorFoeCell, new Lava()));
            Assert.AreEqual(0, duel.Slime.Volume.UnitCount);
            Assert.AreEqual(1, duel.Beast.HitPoints);
            Assert.AreEqual(0, duel.Beast.CountStatus<Burning>());
            Assert.IsTrue(duel.Beast.IsAlive);
            Assert.IsTrue(duel.Slime.IsAlive);
        }

        [Test]
        public void TwoLava_RetaliationAddsSecondBurnWithoutExtraDamage()
        {
            var duel = StartCorridorDuel(typeof(Rat), new Lava(), new Lava());
            Assert.IsTrue(duel.Turns.TryAttack(CorridorFoeCell, new Lava()));
            Assert.AreEqual(1, duel.Beast.HitPoints);
            Assert.AreEqual(2, duel.Beast.CountStatus<Burning>());
            Assert.AreEqual(0, duel.Slime.Volume.UnitCount);
            Assert.IsTrue(duel.Slime.IsAlive);
        }
    }
}
