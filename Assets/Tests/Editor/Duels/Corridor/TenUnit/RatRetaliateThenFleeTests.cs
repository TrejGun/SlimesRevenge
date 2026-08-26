using NUnit.Framework;
using UnityEngine;

namespace SlimesRevenge.Tests.Duels.Corridor.TenUnit
{
    /// <summary>
    /// Cowardly rat: retaliates while adjacent after taking a hit; flees once the slime breaks contact.
    /// </summary>
    public class RatRetaliateThenFleeTests : DuelFixture
    {
        [Test]
        public void Hit_RatAttacksBack_ThenFleeWhenSlimeStepsAway()
        {
            var slime = SpawnFilled(CorridorSlimeCell, new Water(), 10);
            var rat = Spawn<Rat>(CorridorFoeCell);
            var turns = Bind(Corridor(), slime, rat);

            var slimeUnitsBefore = slime.Volume.UnitCount;
            Assert.IsTrue(turns.TryAttack(CorridorFoeCell, new Water()));

            Assert.IsTrue(rat.IsAlive);
            Assert.AreEqual(2, rat.HitPoints);
            Assert.AreEqual(CorridorFoeCell, rat.Cell);
            // Attack spent 1 water; rat bite spent another.
            Assert.AreEqual(slimeUnitsBefore - 2, slime.Volume.UnitCount);
            Assert.IsTrue(slime.IsAlive);
            Assert.IsTrue(rat.InCombat);

            Assert.IsTrue(turns.TryStep(Vector2Int.left));
            Assert.AreEqual(new Vector2Int(3, 0), slime.Cell);

            Assert.Greater(rat.Cell.x, CorridorFoeCell.x);
            Assert.IsFalse(rat.InCombat);
            Assert.AreEqual(CreaturePersonality.Cowardly, rat.Personality);
        }
    }
}
