using System.Collections.Generic;
using NUnit.Framework;

namespace SlimesRevenge.Tests.Duels.Corridor.OneUnit
{
    /// <summary>1 unit Oil: slime dies; enemy slowed (Instability + Flammable) at MaxHitPoints−1.</summary>
    public class OilAttackTests : DuelFixture
    {
        public static IEnumerable<TestCaseData> Cases()
        {
            yield return Case(TestFoePreset.Cowardly, 2);
            yield return Case(TestFoePreset.Passive, 4);
            yield return Case(TestFoePreset.Aggressive, 9);
        }

        [TestCaseSource(nameof(Cases))]
        public void OilAttack_SlimeDiesEnemySlowed(TestFoePreset foe, int expectedHitPoints)
        {
            var duel = StartCorridorDuel(foe, new Oil(), new Oil());
            Assert.IsTrue(duel.Turns.TryAttack(CorridorFoeCell, new Oil()));
            Assert.IsFalse(duel.Slime.IsAlive);
            Assert.IsTrue(duel.Enemy.IsAlive);
            Assert.AreEqual(expectedHitPoints, duel.Enemy.HitPoints);
            Assert.IsNotNull(duel.Enemy.FindStatus<Instability>());
            Assert.IsNotNull(duel.Enemy.FindStatus<Flammable>());
        }

        private static TestCaseData Case(TestFoePreset foe, int hits)
        {
            return new TestCaseData(foe, hits).SetName($"Oil_{foe}");
        }
    }
}
