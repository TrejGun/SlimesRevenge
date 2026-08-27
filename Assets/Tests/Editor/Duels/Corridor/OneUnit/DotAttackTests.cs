using System.Collections.Generic;
using NUnit.Framework;

namespace SlimesRevenge.Tests.Duels.Corridor.OneUnit
{
    /// <summary>1 unit DoT (Poison/Acid/Lava): slime dies to the bite; beast finishes the 3-pulse DoT.</summary>
    public class DotAttackTests : DuelFixture
    {
        public static IEnumerable<TestCaseData> Cases()
        {
            yield return Case(new Poison(), typeof(Rat), 0, false);
            yield return Case(new Poison(), typeof(Cat), 1, true);
            yield return Case(new Poison(), typeof(Dog), 6, true);
            yield return Case(new Acid(), typeof(Rat), 0, false);
            yield return Case(new Acid(), typeof(Cat), 1, true);
            yield return Case(new Acid(), typeof(Dog), 6, true);
            yield return Case(new Lava(), typeof(Rat), 0, false);
            yield return Case(new Lava(), typeof(Cat), 1, true);
            yield return Case(new Lava(), typeof(Dog), 6, true);
        }

        [TestCaseSource(nameof(Cases))]
        public void DotAttack_FullBurnout(
            Substance substance,
            System.Type beastType,
            int expectedHitPoints,
            bool beastAlive
        )
        {
            var duel = StartCorridorDuel(beastType, substance);
            Assert.IsTrue(duel.Turns.TryAttack(CorridorFoeCell, Clone(substance)));
            Assert.IsFalse(duel.Slime.IsAlive);
            Assert.IsTrue(duel.Turns.IsGameOver);

            // Player turns stop on hardcore defeat; DoT on the beast still ticks on its own turns.
            for (var i = 0; i < 3; i++)
            {
                if (!duel.Beast.IsAlive)
                {
                    break;
                }

                duel.Beast.TickStatuses();
            }

            Assert.AreEqual(beastAlive, duel.Beast.IsAlive);
            if (beastAlive)
            {
                Assert.AreEqual(expectedHitPoints, duel.Beast.HitPoints);
            }
        }

        private static TestCaseData Case(
            Substance substance,
            System.Type beastType,
            int hits,
            bool alive
        )
        {
            return new TestCaseData(substance, beastType, hits, alive).SetName(
                $"{substance.GetType().Name}_{beastType.Name}"
            );
        }
    }
}
