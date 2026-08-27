using System.Collections.Generic;
using NUnit.Framework;

namespace SlimesRevenge.Tests.Duels.Corridor.OneUnit
{
    /// <summary>1 unit soft attack (Water/Blood): slime dies to the bite; beast at MaxHitPoints−1.</summary>
    public class SoftAttackTests : DuelFixture
    {
        public static IEnumerable<TestCaseData> Cases()
        {
            yield return Case(new Water(), typeof(Rat), 2);
            yield return Case(new Water(), typeof(Cat), 4);
            yield return Case(new Water(), typeof(Dog), 9);
            yield return Case(new Blood(), typeof(Rat), 2);
            yield return Case(new Blood(), typeof(Cat), 4);
            yield return Case(new Blood(), typeof(Dog), 9);
        }

        [TestCaseSource(nameof(Cases))]
        public void SoftAttack_SlimeDiesBeastLives(
            Substance substance,
            System.Type beastType,
            int expectedHitPoints
        )
        {
            var duel = StartCorridorDuel(beastType, substance);
            Assert.IsTrue(duel.Turns.TryAttack(CorridorFoeCell, Clone(substance)));
            Assert.IsFalse(duel.Slime.IsAlive);
            Assert.IsTrue(duel.Beast.IsAlive);
            Assert.AreEqual(expectedHitPoints, duel.Beast.HitPoints);
        }

        private static TestCaseData Case(Substance substance, System.Type beastType, int hits)
        {
            return new TestCaseData(substance, beastType, hits).SetName(
                $"{substance.GetType().Name}_{beastType.Name}"
            );
        }
    }
}
