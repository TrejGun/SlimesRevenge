using NUnit.Framework;
using UnityEngine;

namespace SlimesRevenge.Tests.Duels.Corridor.TenUnit
{
    /// <summary>10 units water: single enemy duel and two-enemy cell handoff.</summary>
    public class TenWaterEnemyTests : DuelFixture
    {
        [Test]
        public void VsOneEnemy_EnemyDies()
        {
            var slime = SpawnFilled(CorridorSlimeCell, new Water(), 10);
            var rat = Cowardly(CorridorFoeCell);
            var turns = Bind(Corridor(), slime, rat);

            while (rat.IsAlive)
            {
                Assert.IsTrue(turns.TryAttack(CorridorFoeCell, new Water()));
            }

            Assert.IsFalse(rat.IsAlive);
            Assert.IsTrue(slime.IsAlive);
            Assert.IsFalse(turns.Session.IsOccupied(CorridorFoeCell));
            Assert.AreEqual(1, turns.Session.World.Floor.GetCorpses(CorridorFoeCell).Count);
            Assert.AreEqual(5, slime.Volume.UnitCount);
        }

        [Test]
        public void VsFrontThenRear_RearTakesVacatedCell()
        {
            var slime = SpawnFilled(CorridorSlimeCell, new Water(), 10);
            var front = Cowardly(CorridorFoeCell);
            var rear = Aggressive(new Vector2Int(6, 0));
            var turns = Bind(Corridor(), slime, front, rear);

            while (front.IsAlive)
            {
                Assert.IsTrue(turns.TryAttack(CorridorFoeCell, new Water()));
            }

            Assert.IsFalse(front.IsAlive);
            Assert.IsTrue(rear.IsAlive);
            Assert.AreEqual(CorridorFoeCell, rear.Cell);
            Assert.IsTrue(turns.Session.IsOccupied(CorridorFoeCell));
        }
    }
}
