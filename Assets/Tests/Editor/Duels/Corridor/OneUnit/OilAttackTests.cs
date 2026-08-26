using System.Collections.Generic;
using NUnit.Framework;

namespace SlimesRevenge.Tests.Duels.Corridor.OneUnit
{
    /// <summary>1 unit Oil: slime dies; beast slowed (Instability + Oiled) at MaxHitPoints−1.</summary>
    public class OilAttackTests : DuelFixture
    {
        public static IEnumerable<TestCaseData> Cases()
        {
            yield return Case(typeof(Rat), 2);
            yield return Case(typeof(Cat), 4);
            yield return Case(typeof(Dog), 9);
        }

        [TestCaseSource(nameof(Cases))]
        public void OilAttack_SlimeDiesBeastSlowed(System.Type beastType, int expectedHitPoints)
        {
            var duel = StartCorridorDuel(beastType, new Oil());
            Assert.IsTrue(duel.Turns.TryAttack(CorridorFoeCell, new Oil()));
            Assert.IsFalse(duel.Slime.IsAlive);
            Assert.IsTrue(duel.Beast.IsAlive);
            Assert.AreEqual(expectedHitPoints, duel.Beast.HitPoints);
            Assert.IsNotNull(duel.Beast.FindStatus<Instability>());
            Assert.IsNotNull(duel.Beast.FindStatus<Oiled>());
        }

        private static TestCaseData Case(System.Type beastType, int hits)
        {
            return new TestCaseData(beastType, hits).SetName($"Oil_{beastType.Name}");
        }
    }
}
