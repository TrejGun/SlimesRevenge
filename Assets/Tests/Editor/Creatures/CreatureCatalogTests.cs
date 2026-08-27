using NUnit.Framework;

namespace SlimesRevenge.Tests
{
    public class CreatureCatalogTests
    {
        [SetUp]
        public void SetUp()
        {
            CreatureCatalog.ResetCacheForTests();
            SubstanceCatalog.ResetCacheForTests();
            RunConfig.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            RunConfig.Clear();
            CreatureCatalog.ResetCacheForTests();
            SubstanceCatalog.ResetCacheForTests();
        }

        [Test]
        public void CreatureCatalog_FindsAllConcreteCreatures_IncludingNewSubclassesViaReflection()
        {
            var all = CreatureCatalog.All;
            Assert.GreaterOrEqual(all.Count, 6);
            Assert.IsTrue(CreatureCatalog.TryKindOf(typeof(Rat), out var ratKind));
            Assert.AreEqual(CreatureKind.Rat, ratKind);
            Assert.AreEqual(typeof(Dog), CreatureCatalog.TypeOf(CreatureKind.Dog));
        }

        [Test]
        public void CreatureCatalog_Opponents_ExcludeSlime()
        {
            var opponents = CreatureCatalog.Opponents;
            Assert.GreaterOrEqual(opponents.Count, 5);
            for (var i = 0; i < opponents.Count; i++)
            {
                Assert.AreNotEqual(CreatureKind.Slime, opponents[i].Kind);
            }
        }

        [Test]
        public void SubstanceCatalog_FindsAllConcreteSubstances()
        {
            Assert.GreaterOrEqual(SubstanceCatalog.AllTypes.Count, 6);
            Assert.GreaterOrEqual(SubstanceCatalog.Samples.Count, 6);
            var lava = SubstanceCatalog.Create(typeof(Lava));
            Assert.IsInstanceOf<Lava>(lava);
        }

        [Test]
        public void RunConfig_SetDuel_RoundTripsPeekAndConsume()
        {
            var loadout = new Substance[]
            {
                new Water(),
                new Oil(),
                new Lava(),
                new Blood(),
                new Acid(),
            };
            RunConfig.SetDuel(CreatureKind.Scorpion, loadout);
            Assert.IsTrue(RunConfig.HasPending);
            Assert.IsTrue(RunConfig.TryPeek(out var peeked));
            Assert.AreEqual(RunKind.Duel, peeked.Kind);
            Assert.AreEqual(CreatureKind.Scorpion, peeked.Opponent);
            Assert.AreEqual(RunConfig.LoadoutSlots, peeked.Loadout.Length);
            Assert.IsInstanceOf<Lava>(peeked.Loadout[2]);

            Assert.IsTrue(RunConfig.TryConsume(out var consumed));
            Assert.AreSame(peeked, consumed);
            Assert.IsFalse(RunConfig.HasPending);
        }

        [Test]
        public void RunConfig_SetCampaign_HasNoLoadout()
        {
            RunConfig.SetCampaign();
            Assert.IsTrue(RunConfig.TryPeek(out var run));
            Assert.AreEqual(RunKind.Campaign, run.Kind);
            Assert.IsNull(run.Loadout);
        }
    }
}
