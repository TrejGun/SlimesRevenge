using NUnit.Framework;
using UnityEngine;

namespace SlimesRevenge.Tests.Duels.Corridor.TenUnit
{
    /// <summary>
    /// 10 units lava vs two enemies: front burns out in its status phase;
    /// rear steps onto the cell and bites (Retaliation stack).
    /// </summary>
    public class TenLavaTwoEnemiesTests : DuelFixture
    {
        [Test]
        public void FrontBurnsOut_RearStepsInAndAttacks()
        {
            var slime = SpawnFilled(CorridorSlimeCell, new Lava(), 10);
            var front = Aggressive(CorridorFoeCell);
            var rear = Aggressive(new Vector2Int(6, 0));
            var turns = Bind(Corridor(), slime, front, rear);

            Assert.IsTrue(turns.TryAttack(CorridorFoeCell, new Lava()));
            Assert.AreEqual(8, front.HitPoints);
            Assert.AreEqual(2, front.CountStatus<Burning>());
            Assert.AreEqual(8, slime.Volume.UnitCount);

            Assert.IsTrue(turns.TryAttack(CorridorFoeCell, new Lava()));
            Assert.AreEqual(4, front.HitPoints);
            Assert.AreEqual(4, front.CountStatus<Burning>());
            Assert.AreEqual(6, slime.Volume.UnitCount);

            Assert.IsTrue(turns.TryAttack(CorridorFoeCell, new Lava()));
            Assert.IsFalse(front.IsAlive);
            Assert.AreEqual(CorridorFoeCell, rear.Cell);
            Assert.AreEqual(0, rear.CountStatus<Burning>());
            Assert.AreEqual(5, slime.Volume.UnitCount);

            Assert.IsTrue(turns.TryWait());
            Assert.AreEqual(1, rear.CountStatus<Burning>());
            Assert.AreEqual(4, slime.Volume.UnitCount);
            Assert.IsTrue(slime.IsAlive);
            Assert.IsTrue(rear.IsAlive);
        }
    }
}
