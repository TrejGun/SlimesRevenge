using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SlimesRevenge.Tests
{
    /// <summary>
    /// Scorpion armor (Armor = 1): strike DR; acid strips armor; oil→lava Flammable.
    /// </summary>
    public class ScorpionArmorTests
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
        public void WaterStrike_IsBlockedByArmor_DoesNotReduceHitPoints()
        {
            var scorpion = SpawnScorpion();
            var slime = SpawnSlimeWith(new Water(), new Water(), new Water());
            Assert.AreEqual(1, scorpion.Armor);
            Assert.AreEqual(3, scorpion.HitPoints);

            Assert.IsTrue(Combat.Attack(slime, scorpion, new Water()));

            Assert.AreEqual(3, scorpion.HitPoints);
            Assert.IsTrue(scorpion.IsAlive);
            Assert.IsNotNull(scorpion.FindStatus<Wet>());
        }

        [Test]
        public void LavaPuddle_BurnsThroughArmor_WithoutHitting()
        {
            var scorpion = SpawnScorpion();
            Assert.AreEqual(3, scorpion.HitPoints);

            // Step on lava: status only, no immediate strike damage.
            new Lava().Apply(scorpion);
            Assert.AreEqual(3, scorpion.HitPoints);
            Assert.AreEqual(1, scorpion.CountStatus<Burning>());

            // Three start-of-turn pulses ignore Armor and drain HP.
            for (var i = 0; i < 3; i++)
            {
                scorpion.RefreshStatuses();
            }

            Assert.AreEqual(0, scorpion.HitPoints);
            Assert.IsFalse(scorpion.IsAlive);
        }

        [Test]
        public void OilThenLava_StrikeDoubleThroughArmor_BurnPulseDouble()
        {
            // Flammable only from oil — not innate on scorpion.
            var scorpion = SpawnScorpion();
            Assert.IsNull(scorpion.FindStatus<Flammable>());

            var slime = SpawnSlimeWith(new Oil(), new Lava());

            // Oil strike: 1 blocked by armor; applies Flammable (+ Instability).
            Assert.IsTrue(Combat.Attack(slime, scorpion, new Oil()));
            Assert.AreEqual(3, scorpion.HitPoints);
            Assert.IsNotNull(scorpion.FindStatus<Flammable>());

            // Lava strike while Flammable: StrikePower ×2 → 2 − 1 armor DR → −1 HP; applies Burning.
            Assert.IsTrue(Combat.Attack(slime, scorpion, new Lava()));
            Assert.AreEqual(2, scorpion.HitPoints);
            Assert.AreEqual(1, scorpion.CountStatus<Burning>());

            // Start of scorpion turn: Burning pulse ×2 ignores armor → kills from 2 HP.
            scorpion.RefreshStatuses();
            Assert.AreEqual(0, scorpion.HitPoints);
            Assert.IsFalse(scorpion.IsAlive);
        }

        [Test]
        public void AcidStrike_CorrosionAndPower_AppliesCorroding()
        {
            var scorpion = SpawnScorpion();
            Assert.AreEqual(1, scorpion.Armor);
            Assert.AreEqual(3, scorpion.HitPoints);

            Assert.IsTrue(Combat.Attack(SpawnSlimeWith(new Acid()), scorpion, new Acid()));

            Assert.AreEqual(0, scorpion.Armor);
            Assert.AreEqual(2, scorpion.HitPoints);
            Assert.AreEqual(1, scorpion.CountStatus<Corroding>());
        }

        [Test]
        public void AcidPuddle_OnlyAppliesCorroding_ArmorIntactUntilPulse()
        {
            var scorpion = SpawnScorpion();
            new Acid().Apply(scorpion);

            Assert.AreEqual(1, scorpion.Armor);
            Assert.AreEqual(3, scorpion.HitPoints);
            Assert.AreEqual(1, scorpion.CountStatus<Corroding>());

            scorpion.RefreshStatuses();
            Assert.AreEqual(0, scorpion.Armor);
            Assert.AreEqual(3, scorpion.HitPoints);
        }

        [Test]
        public void Corroding_StripsArmorBeforeHitPoints()
        {
            var scorpion = SpawnScorpion();
            Assert.AreEqual(1, scorpion.Armor);
            Assert.AreEqual(3, scorpion.HitPoints);

            scorpion.AddStatus(new Corroding());
            Assert.AreEqual(1, scorpion.Armor);

            scorpion.RefreshStatuses();
            Assert.AreEqual(0, scorpion.Armor);
            Assert.AreEqual(3, scorpion.HitPoints);

            scorpion.RefreshStatuses();
            Assert.AreEqual(0, scorpion.Armor);
            Assert.AreEqual(2, scorpion.HitPoints);
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
