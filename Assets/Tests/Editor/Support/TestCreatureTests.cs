using NUnit.Framework;
using UnityEngine;

namespace SlimesRevenge.Tests
{
    public class TestCreatureTests : SpawnFixture
    {
        [Test]
        public void HatesCats_MatchesKind_NotCatClass()
        {
            var hunter = Body(Vector2Int.zero).Trait<HatesCats>();
            var prey = Body(Vector2Int.one).As(CreatureKind.Cat);
            var other = Body(new Vector2Int(2, 0)).As(CreatureKind.Bat);
            Assert.IsTrue(hunter.FindStatus<HatesCats>().IsPrey(prey));
            Assert.IsFalse(hunter.FindStatus<HatesCats>().IsPrey(other));
            Assert.AreNotEqual(typeof(Cat), prey.GetType());
        }

        [Test]
        public void CustomFillAndTrait_Stick()
        {
            var body = TestCreature
                .Spawn(spawned, Vector2Int.zero)
                .Role(CreaturePersonality.Passive)
                .WithHp(7)
                .WithSpeed(3)
                .Fill(new Water(), new Oil())
                .Trait<Vampirism>();
            Assert.AreEqual(7, body.HitPoints);
            Assert.AreEqual(3, body.Speed);
            Assert.AreEqual(CreaturePersonality.Passive, body.Personality);
            Assert.AreEqual(1, body.Volume.CountOf<Water>());
            Assert.AreEqual(1, body.Volume.CountOf<Oil>());
            Assert.IsNotNull(body.FindStatus<Vampirism>());
        }

        [Test]
        public void Catalog_DoesNotDiscoverTestCreature()
        {
            CreatureCatalog.ResetCacheForTests();
            try
            {
                Assert.IsFalse(CreatureCatalog.TryKindOf(typeof(TestCreature), out _));
            }
            finally
            {
                CreatureCatalog.ResetCacheForTests();
            }
        }
    }
}
