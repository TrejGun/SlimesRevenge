using NUnit.Framework;
using UnityEngine;

namespace SlimesRevenge.Tests.Duels.Corridor.ThreeUnit
{
    /// <summary>3 units lava vs one enemy: stand and strike, or mess and flee.</summary>
    public class ThreeLavaEnemyTests : DuelFixture
    {
        [Test]
        public void AttackUntilSlimeDies()
        {
            var duel = StartCorridorDuel(
                TestCreatures.Aggressive,
                new Lava(),
                new Lava(),
                new Lava(),
                new Lava()
            );

            Assert.IsTrue(duel.Turns.TryAttack(CorridorFoeCell, new Lava()));
            Assert.AreEqual(8, duel.Enemy.HitPoints);
            Assert.AreEqual(2, duel.Enemy.CountStatus<Burning>());
            Assert.AreEqual(2, duel.Slime.Volume.UnitCount);
            Assert.IsTrue(duel.Slime.IsAlive);

            Assert.IsTrue(duel.Turns.TryAttack(CorridorFoeCell, new Lava()));
            Assert.IsFalse(duel.Slime.IsAlive);
            Assert.AreEqual(4, duel.Enemy.HitPoints);
            Assert.AreEqual(3, duel.Enemy.CountStatus<Burning>());
        }

        [Test]
        public void MessAndFlee_EnemyCatchesAtWall()
        {
            var duel = StartCorridorDuel(
                TestCreatures.Aggressive,
                new Lava(),
                new Lava(),
                new Lava()
            );
            Assert.IsTrue(duel.Turns.TryMakeMess(new Lava()));
            Assert.IsNotNull(duel.Turns.Session.World.Floor.GetPuddle(CorridorSlimeCell));
            Assert.AreEqual(1, duel.Slime.Volume.UnitCount);
            Assert.AreEqual(1, duel.Enemy.CountStatus<Burning>());
            Assert.AreEqual(10, duel.Enemy.HitPoints);
            Assert.IsTrue(duel.Slime.IsAlive);

            var steps = 0;
            for (var x = 3; x >= 0 && duel.Slime.IsAlive; x--)
            {
                steps++;
                Assert.IsTrue(duel.Turns.TryMoveTo(new Vector2Int(x, 0)), $"flee to {x}");
            }

            while (duel.Slime.IsAlive && steps < 12)
            {
                steps++;
                Assert.IsTrue(duel.Turns.TryWait());
            }

            Assert.IsFalse(duel.Slime.IsAlive);
            Assert.AreEqual(5, steps);
            Assert.IsTrue(duel.Enemy.IsAlive);
            Assert.AreEqual(new Vector2Int(0, 0), duel.Slime.Cell);
        }
    }
}
