using System.Collections.Generic;
using NUnit.Framework;

namespace SlimesRevenge.Tests.Duels.Corridor.OneUnit
{
    /// <summary>1 unit DoT (Poison/Acid/Lava): slime dies to the bite; enemy finishes the 3-pulse DoT.</summary>
    public class DotAttackTests : DuelFixture
    {
        public static IEnumerable<TestCaseData> Cases()
        {
            yield return Case(new Poison(), TestFoePreset.Cowardly, 0, false);
            yield return Case(new Poison(), TestFoePreset.Passive, 1, true);
            yield return Case(new Poison(), TestFoePreset.Aggressive, 6, true);
            yield return Case(new Acid(), TestFoePreset.Cowardly, 0, false);
            yield return Case(new Acid(), TestFoePreset.Passive, 1, true);
            yield return Case(new Acid(), TestFoePreset.Aggressive, 6, true);
            yield return Case(new Lava(), TestFoePreset.Cowardly, 0, false);
            yield return Case(new Lava(), TestFoePreset.Passive, 1, true);
            yield return Case(new Lava(), TestFoePreset.Aggressive, 6, true);
        }

        [TestCaseSource(nameof(Cases))]
        public void DotAttack_FullBurnout(
            Substance substance,
            TestFoePreset foe,
            int expectedHitPoints,
            bool enemyAlive
        )
        {
            var duel = StartCorridorDuel(foe, substance, Clone(substance));
            Assert.IsTrue(duel.Turns.TryAttack(CorridorFoeCell, Clone(substance)));
            Assert.IsFalse(duel.Slime.IsAlive);
            Assert.IsTrue(duel.Turns.IsGameOver);

            // Player turns stop on hardcore defeat; DoT on the enemy still ticks on its own turns.
            for (var i = 0; i < 3; i++)
            {
                if (!duel.Enemy.IsAlive)
                {
                    break;
                }

                duel.Enemy.TickStatuses();
            }

            Assert.AreEqual(enemyAlive, duel.Enemy.IsAlive);
            if (enemyAlive)
            {
                Assert.AreEqual(expectedHitPoints, duel.Enemy.HitPoints);
            }
        }

        private static TestCaseData Case(
            Substance substance,
            TestFoePreset foe,
            int hits,
            bool alive
        )
        {
            return new TestCaseData(substance, foe, hits, alive).SetName(
                $"{substance.GetType().Name}_{foe}"
            );
        }
    }
}
