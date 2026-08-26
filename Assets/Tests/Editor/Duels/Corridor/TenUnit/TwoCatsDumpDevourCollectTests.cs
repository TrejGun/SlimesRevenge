using NUnit.Framework;
using UnityEngine;

namespace SlimesRevenge.Tests.Duels.Corridor.TenUnit
{
    /// <summary>
    /// Passive rear cat stays put (FixedRng does not wander), so the slime can stand on the
    /// first corpse, dump one unit, devour both blood units, strike the next cat, then
    /// collect the puddle back to capacity. Cats are softened to one hit; MaxHitPoints stay 5
    /// so the first corpse's HP-decay outlives the dump/devour/strike sequence.
    /// </summary>
    public class TwoCatsDumpDevourCollectTests : DuelFixture
    {
        [Test]
        public void DumpDevourAttackCollect_ReturnsToFull()
        {
            var slime = SpawnFilled(CorridorSlimeCell, new Water(), 10);
            var front = Spawn<Cat>(CorridorFoeCell);
            var rear = Spawn<Cat>(new Vector2Int(6, 0));
            Assert.AreEqual(5, front.MaxHitPoints);
            SoftenToOneHit(front);
            SoftenToOneHit(rear);
            var turns = Bind(Corridor(), slime, front, rear);

            Assert.IsTrue(turns.TryAttack(CorridorFoeCell, new Water()));
            Assert.IsFalse(front.IsAlive);
            Assert.AreEqual(9, slime.Volume.UnitCount);
            Assert.AreEqual(new Vector2Int(6, 0), rear.Cell);
            Assert.IsFalse(turns.Session.IsOccupied(CorridorFoeCell));
            Assert.AreEqual(5, turns.Session.World.Floor.GetCorpses(CorridorFoeCell)[0].DecayTurnsLeft);

            Assert.IsTrue(turns.TryMoveTo(CorridorFoeCell));
            Assert.IsTrue(turns.TryMakeMess(new Water()));
            Assert.AreEqual(8, slime.Volume.UnitCount);
            Assert.IsInstanceOf<Water>(turns.Session.World.Floor.GetPuddle(CorridorFoeCell).Substance);
            Assert.AreEqual(1, turns.Session.World.Floor.GetCorpses(CorridorFoeCell).Count);

            Assert.IsTrue(turns.TryDevourCorpse(0));
            Assert.AreEqual(9, slime.Volume.UnitCount);
            Assert.IsTrue(slime.Digestion.IsBusy);
            Assert.IsTrue(turns.TryWait());
            Assert.AreEqual(Volume.Capacity, slime.Volume.UnitCount);
            Assert.IsFalse(slime.Digestion.IsBusy);
            Assert.IsNotNull(turns.Session.World.Floor.GetPuddle(CorridorFoeCell));

            Assert.IsTrue(turns.TryAttack(new Vector2Int(6, 0), new Water()));
            Assert.IsFalse(rear.IsAlive);
            Assert.AreEqual(9, slime.Volume.UnitCount);

            Assert.IsTrue(turns.TryCollectPuddle());
            Assert.AreEqual(Volume.Capacity, slime.Volume.UnitCount);
            Assert.IsNull(turns.Session.World.Floor.GetPuddle(CorridorFoeCell));
            Assert.AreEqual(8, slime.Volume.CountOf<Water>());
            Assert.AreEqual(2, slime.Volume.CountOf<Blood>());
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
