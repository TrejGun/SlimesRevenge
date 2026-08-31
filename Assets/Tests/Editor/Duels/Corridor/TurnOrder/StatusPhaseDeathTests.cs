using NUnit.Framework;
using UnityEngine;

namespace SlimesRevenge.Tests.Duels.Corridor.TurnOrder
{
    /// <summary>
    /// Statuses tick only at the start of each creature's turn.
    /// Death in that phase skips the act and vacates for later creatures in the same round.
    /// </summary>
    public class StatusPhaseDeathTests : DuelFixture
    {
        [Test]
        public void StatusDeath_SkipsActAndVacatesCell()
        {
            var slime = SpawnFilled(CorridorSlimeCell, new Water(), 6);
            var rat = Cowardly(CorridorFoeCell);
            rat.SetMaxHitPoints(1);
            rat.AddStatus(new Burning());
            var turns = Bind(Corridor(), slime, rat);

            var unitsBefore = slime.Volume.UnitCount;
            Assert.IsTrue(turns.TryWait());
            Assert.IsFalse(rat.IsAlive);
            Assert.IsFalse(turns.Session.IsOccupied(CorridorFoeCell));
            Assert.AreEqual(unitsBefore, slime.Volume.UnitCount);
            Assert.AreEqual(1, turns.Session.World.Floor.GetCorpses(CorridorFoeCell).Count);
        }

        [Test]
        public void StatusDeath_AllowsNextCreatureToEnterVacatedCellSameRound()
        {
            var slime = SpawnSlimeWith(CorridorSlimeCell, new Water());
            var rat = Cowardly(CorridorFoeCell);
            rat.SetMaxHitPoints(1);
            rat.AddStatus(new Burning());
            var dog = Aggressive(new Vector2Int(7, 0));
            var turns = Bind(Corridor(), slime, rat, dog);

            Assert.IsTrue(turns.TryWait());
            Assert.IsFalse(rat.IsAlive);
            Assert.IsTrue(dog.IsAlive);
            Assert.AreEqual(CorridorFoeCell, dog.Cell);
            Assert.IsTrue(turns.Session.IsOccupied(CorridorFoeCell));
        }
    }
}
