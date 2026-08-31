using NUnit.Framework;
using UnityEngine;

namespace SlimesRevenge.Tests.Duels.Corridor.TenUnit
{
    /// <summary>
    /// Living foes block the cell; corpses do not. Soften with Damage so MaxHitPoints (decay) stay
    /// at preset defaults (passive 5, cowardly 3) while each killing blow costs only one water unit.
    /// </summary>
    public class TwoEnemyCorpsesTests : DuelFixture
    {
        [Test]
        public void KillTwoEnemies_StacksTwoCorpsesOnFrontCell()
        {
            var slime = SpawnFilled(CorridorSlimeCell, new Water(), 10);
            var front = Passive(CorridorFoeCell);
            var rear = Cowardly(new Vector2Int(6, 0));
            Assert.AreEqual(5, front.MaxHitPoints);
            Assert.AreEqual(3, rear.MaxHitPoints);
            SoftenToOneHit(front);
            SoftenToOneHit(rear);
            var turns = Bind(Corridor(), slime, front, rear);

            Assert.IsTrue(turns.TryAttack(CorridorFoeCell, new Water()));
            Assert.IsFalse(front.IsAlive);
            // Cowardly foes flee instead of claiming the vacated cell; place for corpse-stack check.
            Assert.IsTrue(turns.Session.TryMoveOccupant(rear.Cell, CorridorFoeCell));
            rear.PlaceOn(CorridorFoeCell);
            Assert.AreEqual(CorridorFoeCell, rear.Cell);
            var afterFront = turns.Session.World.Floor.GetCorpses(CorridorFoeCell);
            Assert.AreEqual(1, afterFront.Count);
            Assert.AreEqual(CreatureKind.Cat, afterFront[0].Kind);
            Assert.AreEqual(5, afterFront[0].DecayTurnsLeft);
            Assert.IsTrue(turns.Session.IsOccupied(CorridorFoeCell));

            Assert.IsTrue(turns.TryAttack(CorridorFoeCell, new Water()));
            Assert.IsFalse(rear.IsAlive);
            Assert.IsFalse(turns.Session.IsOccupied(CorridorFoeCell));
            var corpses = turns.Session.World.Floor.GetCorpses(CorridorFoeCell);
            Assert.AreEqual(2, corpses.Count);
            Assert.AreEqual(CreatureKind.Cat, corpses[0].Kind);
            Assert.AreEqual(CreatureKind.Rat, corpses[1].Kind);
            Assert.AreEqual(2, corpses[0].Volume.UnitCount);
            Assert.AreEqual(1, corpses[1].Volume.UnitCount);
            // Death resolves pause floor aging, so both still show full HP budgets.
            Assert.AreEqual(5, corpses[0].DecayTurnsLeft);
            Assert.AreEqual(3, corpses[1].DecayTurnsLeft);
            Assert.IsTrue(slime.IsAlive);
        }

        private static void SoftenToOneHit(Creature creature)
        {
            while (creature.HitPoints > 1)
            {
                creature.Damage(1);
            }
        }
    }
}
