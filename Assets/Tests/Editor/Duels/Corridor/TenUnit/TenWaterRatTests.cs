using NUnit.Framework;
using UnityEngine;

namespace SlimesRevenge.Tests.Duels.Corridor.TenUnit
{
    /// <summary>10 units water: single rat duel and two-rat cell handoff.</summary>
    public class TenWaterRatTests : DuelFixture
    {
        [Test]
        public void VsOneRat_RatDies()
        {
            var slime = SpawnFilled(CorridorSlimeCell, new Water(), 10);
            var rat = Spawn<Rat>(CorridorFoeCell);
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
        public void VsRatThenDog_DogTakesVacatedCell()
        {
            var slime = SpawnFilled(CorridorSlimeCell, new Water(), 10);
            var front = Spawn<Rat>(CorridorFoeCell);
            var rear = Spawn<Dog>(new Vector2Int(6, 0));
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
