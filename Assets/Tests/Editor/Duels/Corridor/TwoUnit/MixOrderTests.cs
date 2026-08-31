using NUnit.Framework;
using UnityEngine;

namespace SlimesRevenge.Tests.Duels.Corridor.TwoUnit
{
    /// <summary>2 units mixes on the corridor: order of units changes retaliation.</summary>
    public class MixOrderTests : DuelFixture
    {
        [Test]
        public void WaterThenFlee_EnemyIsWetNotBurning()
        {
            var duel = StartCorridorDuel(
                TestCreatures.Cowardly,
                new Water(),
                new Lava(),
                new Lava()
            );
            Assert.IsTrue(duel.Turns.TryAttack(CorridorFoeCell, new Water()));
            Assert.AreEqual(2, duel.Enemy.HitPoints);
            Assert.AreEqual(0, duel.Enemy.CountStatus<Burning>());
            Assert.IsNull(duel.Enemy.FindStatus<Wet>());
            Assert.IsTrue(duel.Slime.IsAlive);

            Assert.IsTrue(duel.Turns.TryMoveTo(new Vector2Int(3, 0)));
            Assert.IsTrue(duel.Slime.IsAlive);
            Assert.IsTrue(duel.Enemy.IsAlive);
        }

        [Test]
        public void LavaThenAttack_EnemyExtinguishesAtOneHit()
        {
            var duel = StartCorridorDuel(
                TestCreatures.Cowardly,
                new Lava(),
                new Water(),
                new Water()
            );
            Assert.IsTrue(duel.Turns.TryAttack(CorridorFoeCell, new Lava()));
            Assert.AreEqual(1, duel.Slime.Volume.UnitCount);
            Assert.AreEqual(1, duel.Enemy.HitPoints);
            Assert.AreEqual(0, duel.Enemy.CountStatus<Burning>());
            Assert.IsTrue(duel.Enemy.IsAlive);
            Assert.IsTrue(duel.Slime.IsAlive);
        }

        [Test]
        public void TwoLava_RetaliationAddsSecondBurnWithoutExtraDamage()
        {
            var duel = StartCorridorDuel(
                TestCreatures.Cowardly,
                new Lava(),
                new Lava(),
                new Lava()
            );
            Assert.IsTrue(duel.Turns.TryAttack(CorridorFoeCell, new Lava()));
            Assert.AreEqual(1, duel.Enemy.HitPoints);
            Assert.AreEqual(2, duel.Enemy.CountStatus<Burning>());
            Assert.AreEqual(1, duel.Slime.Volume.UnitCount);
            Assert.IsTrue(duel.Slime.IsAlive);
        }
    }
}
