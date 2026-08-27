using System.Collections.Generic;
using NUnit.Framework;

namespace SlimesRevenge.Tests
{
    public class CollectApplyPreviewTests
    {
        [Test]
        public void Oil_Preview_InstabilityAndFlammable()
        {
            var sink = new List<StatusEffect>();
            new Oil().CollectApplyPreview(sink);
            Assert.AreEqual(2, sink.Count);
            Assert.IsInstanceOf<Instability>(sink[0]);
            Assert.IsInstanceOf<Flammable>(sink[1]);
        }

        [Test]
        public void Acid_Preview_Corroding()
        {
            var sink = new List<StatusEffect>();
            new Acid().CollectApplyPreview(sink);
            Assert.AreEqual(1, sink.Count);
            Assert.IsInstanceOf<Corroding>(sink[0]);
        }

        [Test]
        public void Water_Preview_Wet()
        {
            var sink = new List<StatusEffect>();
            new Water().CollectApplyPreview(sink);
            Assert.AreEqual(1, sink.Count);
            Assert.IsInstanceOf<Wet>(sink[0]);
        }

        [Test]
        public void Blood_Preview_Empty_DoesNotLieAboutVampirism()
        {
            var sink = new List<StatusEffect>();
            new Blood().CollectApplyPreview(sink);
            Assert.AreEqual(0, sink.Count);
        }

        [Test]
        public void Water_DominancePreview_FireproofAndRetaliation()
        {
            var sink = new List<StatusEffect>();
            new Water().CollectDominancePassives(sink);
            Assert.AreEqual(2, sink.Count);
            Assert.IsInstanceOf<Fireproof>(sink[0]);
            Assert.IsInstanceOf<Retaliation>(sink[1]);
            Assert.IsTrue(sink[0].Permanent);
        }

        [Test]
        public void Oil_DominancePreview_ForeverFlammableAndRetaliation()
        {
            var sink = new List<StatusEffect>();
            new Oil().CollectDominancePassives(sink);
            Assert.AreEqual(2, sink.Count);
            Assert.IsInstanceOf<Flammable>(sink[0]);
            Assert.IsTrue(sink[0].Permanent);
            Assert.IsInstanceOf<Retaliation>(sink[1]);
        }

        [Test]
        public void Oil_ApplyPreview_TimedFlammable_NotForever()
        {
            var sink = new List<StatusEffect>();
            new Oil().CollectApplyPreview(sink);
            Assert.IsInstanceOf<Flammable>(sink[1]);
            Assert.IsFalse(sink[1].Permanent);
            Assert.AreEqual(OverTime.DefaultDuration, sink[1].Remaining);
        }

        [Test]
        public void Blood_DominancePreview_RegenerationAndRetaliation()
        {
            var sink = new List<StatusEffect>();
            new Blood().CollectDominancePassives(sink);
            Assert.AreEqual(2, sink.Count);
            Assert.IsInstanceOf<Regeneration>(sink[0]);
            Assert.IsInstanceOf<Retaliation>(sink[1]);
        }

        [Test]
        public void Lava_Preview_Burning()
        {
            var sink = new List<StatusEffect>();
            new Lava().CollectApplyPreview(sink);
            Assert.AreEqual(1, sink.Count);
            Assert.IsInstanceOf<Burning>(sink[0]);
        }

        [Test]
        public void IconCatalog_Register_OverridesMap()
        {
            IconCatalog.RegisterSubstance(typeof(Oil), "Icons/substance_oil");
            Assert.AreEqual("Icons/substance_oil", IconCatalog.SubstanceKey(typeof(Oil)));
            IconCatalog.RegisterStatus(typeof(Burning), "Icons/status_burning");
            Assert.AreEqual("Icons/status_burning", IconCatalog.StatusKey(typeof(Burning)));
        }
    }
}
