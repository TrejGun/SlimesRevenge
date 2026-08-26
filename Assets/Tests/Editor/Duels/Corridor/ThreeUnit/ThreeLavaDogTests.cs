using NUnit.Framework;
using UnityEngine;

namespace SlimesRevenge.Tests.Duels.Corridor.ThreeUnit
{
    /// <summary>3 units lava vs one dog: stand and strike, or mess and flee.</summary>
    public class ThreeLavaDogTests : DuelFixture
    {
        [Test]
        public void AttackUntilSlimeDies()
        {
            var duel = StartCorridorDuel(typeof(Dog), new Lava(), new Lava(), new Lava());

            Assert.IsTrue(duel.Turns.TryAttack(CorridorFoeCell, new Lava()));
            Assert.AreEqual(8, duel.Beast.HitPoints);
            Assert.AreEqual(2, duel.Beast.CountStatus<Burning>());
            Assert.AreEqual(1, duel.Slime.Volume.UnitCount);
            Assert.IsTrue(duel.Slime.IsAlive);

            Assert.IsTrue(duel.Turns.TryAttack(CorridorFoeCell, new Lava()));
            Assert.IsFalse(duel.Slime.IsAlive);
            Assert.AreEqual(4, duel.Beast.HitPoints);
            Assert.AreEqual(3, duel.Beast.CountStatus<Burning>());
        }

        [Test]
        public void MessAndFlee_DogCatchesAtWall()
        {
            var duel = StartCorridorDuel(typeof(Dog), new Lava(), new Lava(), new Lava());
            Assert.IsTrue(duel.Turns.TryMakeMess(new Lava()));
            Assert.IsNotNull(duel.Turns.Session.World.Floor.GetPuddle(CorridorSlimeCell));
            Assert.AreEqual(1, duel.Slime.Volume.UnitCount);
            Assert.AreEqual(1, duel.Beast.CountStatus<Burning>());
            Assert.AreEqual(10, duel.Beast.HitPoints);
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
            Assert.AreEqual(6, steps);
            Assert.IsTrue(duel.Beast.IsAlive);
            Assert.Greater(duel.Beast.CountStatus<Burning>(), 0);
            Assert.AreEqual(new Vector2Int(0, 0), duel.Slime.Cell);
        }
    }
}
