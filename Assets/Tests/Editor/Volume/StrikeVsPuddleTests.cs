using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SlimesRevenge.Tests
{
    /// <summary>
    /// Strike deals Damage then Apply; puddle Apply only — no immediate HP/armor loss.
    /// </summary>
    public class StrikeVsPuddleTests
    {
        private readonly List<GameObject> spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in spawned)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }

            spawned.Clear();
        }

        [Test]
        public void Water_StrikeDamages_PuddleOnlyWets()
        {
            var ratStrike = SpawnRat();
            Assert.IsTrue(Combat.Attack(SpawnSlimeWith(new Water()), ratStrike, new Water()));
            Assert.AreEqual(2, ratStrike.HitPoints);
            Assert.IsNotNull(ratStrike.FindStatus<Wet>());

            var ratPuddle = SpawnRat();
            new Water().Apply(ratPuddle);
            Assert.AreEqual(3, ratPuddle.HitPoints);
            Assert.IsNotNull(ratPuddle.FindStatus<Wet>());
        }

        [Test]
        public void Oil_StrikeDamages_PuddleOnlyStatuses()
        {
            var ratStrike = SpawnRat();
            Assert.IsTrue(Combat.Attack(SpawnSlimeWith(new Oil()), ratStrike, new Oil()));
            Assert.AreEqual(2, ratStrike.HitPoints);
            Assert.IsNotNull(ratStrike.FindStatus<Flammable>());
            Assert.IsNotNull(ratStrike.FindStatus<Instability>());

            var ratPuddle = SpawnRat();
            new Oil().Apply(ratPuddle);
            Assert.AreEqual(3, ratPuddle.HitPoints);
            Assert.IsNotNull(ratPuddle.FindStatus<Flammable>());
            Assert.IsNotNull(ratPuddle.FindStatus<Instability>());
        }

        [Test]
        public void Poison_StrikeDamages_PuddleOnlyPoisons()
        {
            var ratStrike = SpawnRat();
            Assert.IsTrue(Combat.Attack(SpawnSlimeWith(new Poison()), ratStrike, new Poison()));
            Assert.AreEqual(2, ratStrike.HitPoints);
            Assert.IsNotNull(ratStrike.FindStatus<Poisoned>());

            var ratPuddle = SpawnRat();
            new Poison().Apply(ratPuddle);
            Assert.AreEqual(3, ratPuddle.HitPoints);
            Assert.IsNotNull(ratPuddle.FindStatus<Poisoned>());
        }

        [Test]
        public void NonAcid_StrikeBlockedByArmor_PuddleStatusOnly_ArmorIntact()
        {
            Substance[] kinds =
            {
                new Water(), new Oil(), new Poison(), new Blood(), new Lava()
            };

            foreach (var kind in kinds)
            {
                var scorpionStrike = SpawnScorpion();
                Assert.IsTrue(Combat.Attack(SpawnSlimeWith(Volume.CloneSubstance(kind)), scorpionStrike, Volume.CloneSubstance(kind)));
                Assert.AreEqual(1, scorpionStrike.Armor, kind.GetType().Name);
                Assert.AreEqual(3, scorpionStrike.HitPoints, kind.GetType().Name);

                var scorpionPuddle = SpawnScorpion();
                Volume.CloneSubstance(kind).Apply(scorpionPuddle);
                Assert.AreEqual(1, scorpionPuddle.Armor, kind.GetType().Name);
                Assert.AreEqual(3, scorpionPuddle.HitPoints, kind.GetType().Name);
            }
        }

        [Test]
        public void Acid_StrikeConsumesArmorOrHp_PuddleOnlyCorrodes()
        {
            var scorpionStrike = SpawnScorpion();
            Assert.IsTrue(Combat.Attack(SpawnSlimeWith(new Acid()), scorpionStrike, new Acid()));
            // Acid: Corrosion=1 strips armor, then Power=1 vs remaining DR (0 → 1 HP).
            Assert.AreEqual(0, scorpionStrike.Armor);
            Assert.AreEqual(2, scorpionStrike.HitPoints);
            Assert.IsNotNull(scorpionStrike.FindStatus<Corroding>());
            Assert.AreEqual(1, scorpionStrike.FindStatus<Corroding>().Corrosion);

            var scorpionPuddle = SpawnScorpion();
            new Acid().Apply(scorpionPuddle);
            Assert.AreEqual(1, scorpionPuddle.Armor);
            Assert.AreEqual(3, scorpionPuddle.HitPoints);
            Assert.IsNotNull(scorpionPuddle.FindStatus<Corroding>());
        }

        [Test]
        public void Acid_Strike_CorrosionThenRemainingArmorBlocksPower()
        {
            // Acid 1/1 on Armor 10: strip 1 → 9 DR fully blocks Power 1.
            var scorpion = SpawnScorpion();
            scorpion.SetArmor(10);
            scorpion.SetMaxHitPoints(10);
            Assert.AreEqual(10, scorpion.HitPoints);

            Assert.IsTrue(Combat.Attack(SpawnSlimeWith(new Acid()), scorpion, new Acid()));
            Assert.AreEqual(9, scorpion.Armor);
            Assert.AreEqual(10, scorpion.HitPoints);
        }

        [Test]
        public void Strike_CorrosionThenPower_VsRemainingArmor_Examples()
        {
            // Armor 10 / HP 10 — user examples for Corrosion/Power pairs.
            AssertStrike(5, 5, expectArmor: 5, expectHp: 10);
            AssertStrike(7, 7, expectArmor: 3, expectHp: 6);
            AssertStrike(5, 10, expectArmor: 5, expectHp: 5);
        }

        private void AssertStrike(int corrosion, int power, int expectArmor, int expectHp)
        {
            var sample = new StrikeSample(corrosion, power);
            var target = SpawnScorpion();
            target.SetArmor(10);
            target.SetMaxHitPoints(10);
            Assert.IsTrue(Combat.Attack(SpawnSlimeWith(sample), target, new StrikeSample(corrosion, power)));
            Assert.AreEqual(expectArmor, target.Armor, $"Corrosion={corrosion} Power={power} armor");
            Assert.AreEqual(expectHp, target.HitPoints, $"Corrosion={corrosion} Power={power} hp");
        }

        /// <summary>Test-only strike with configurable Corrosion/Power.</summary>
        private sealed class StrikeSample : Substance
        {
            private readonly int corrosion;
            private readonly int power;

            public StrikeSample(int corrosion, int power)
            {
                this.corrosion = corrosion;
                this.power = power;
            }

            public override int Corrosion => corrosion;

            public override int Power => power;

            public override Color Color => Color.magenta;

            public override string Label => "strike-sample";

            public override SlimeLook Look => SlimeLook.Acid;

            public override int AppearanceTieBreak => 99;

            public override Substance Clone() => new StrikeSample(corrosion, power);
        }

        [Test]
        public void Blood_StrikeDamages_PuddleNoHpUnlessVampirism()
        {
            var ratStrike = SpawnRat();
            Assert.IsTrue(Combat.Attack(SpawnSlimeWith(new Blood()), ratStrike, new Blood()));
            Assert.AreEqual(2, ratStrike.HitPoints);

            var ratPuddle = SpawnRat();
            new Blood().Apply(ratPuddle);
            Assert.AreEqual(3, ratPuddle.HitPoints);
        }

        [Test]
        public void Lava_StrikeDamages_PuddleOnlyBurns()
        {
            var ratStrike = SpawnRat();
            Assert.IsTrue(Combat.Attack(SpawnSlimeWith(new Lava()), ratStrike, new Lava()));
            Assert.AreEqual(2, ratStrike.HitPoints);
            Assert.AreEqual(1, ratStrike.CountStatus<Burning>());

            var ratPuddle = SpawnRat();
            new Lava().Apply(ratPuddle);
            Assert.AreEqual(3, ratPuddle.HitPoints);
            Assert.AreEqual(1, ratPuddle.CountStatus<Burning>());
        }

        private Rat SpawnRat()
        {
            var rat = new GameObject("Rat").AddComponent<Rat>();
            spawned.Add(rat.gameObject);
            rat.PlaceOn(Vector2Int.zero);
            if (rat.Volume.UnitCount == 0)
            {
                Rat.FillStarting(rat.Volume);
            }

            rat.SetMaxHitPoints(3);
            return rat;
        }

        private Scorpion SpawnScorpion()
        {
            var scorpion = new GameObject("Scorpion").AddComponent<Scorpion>();
            spawned.Add(scorpion.gameObject);
            scorpion.PlaceOn(Vector2Int.zero);
            if (scorpion.Volume.UnitCount == 0)
            {
                Scorpion.FillStarting(scorpion.Volume);
            }

            scorpion.SetMaxHitPoints(3);
            scorpion.SetArmor(1);
            scorpion.EnsureInnateTraits();
            return scorpion;
        }

        private Slime SpawnSlimeWith(params Substance[] units)
        {
            var slime = new GameObject("Slime").AddComponent<Slime>();
            spawned.Add(slime.gameObject);
            slime.PlaceOn(Vector2Int.right);
            slime.Volume.Clear();
            foreach (var unit in units)
            {
                slime.Volume.Add(Volume.CloneSubstance(unit));
            }

            slime.SetMaxHitPoints(0);
            slime.RefreshVolumeStatuses();
            return slime;
        }
    }
}
