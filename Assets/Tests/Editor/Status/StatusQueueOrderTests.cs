using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SlimesRevenge.Tests
{
    /// <summary>
    /// Status queue pulses FIFO. Slime dominance is a separate single slot: on change it edits
    /// the queue (water extinguishes Burning; blood inserts Regeneration at the front). Passive
    /// dominance pieces (Fireproof, Forever Flammable, Retaliation) are not in the pulse queue.
    /// </summary>
    public class StatusQueueOrderTests
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
        public void RefreshStatuses_PulsesInApplicationOrder_Fifo()
        {
            // First applied, first pulsed — Poisoned then Burning.
            var rat = SpawnRat();
            rat.AddStatus(new Poisoned());
            rat.AddStatus(new Burning());
            Assert.IsInstanceOf<Poisoned>(rat.Statuses[0]);
            Assert.IsInstanceOf<Burning>(rat.Statuses[1]);

            rat.RefreshStatuses();
            // Poisoned −1, Burning −1 → HP 3→1.
            Assert.AreEqual(1, rat.HitPoints);
        }

        [Test]
        public void BloodDominance_AppendsRegeneration_GrowsOnlyOnPulse()
        {
            var slime = BloodWaterSlime(blood: 4, water: 1);
            Assert.AreEqual(5, slime.Volume.UnitCount);
            Assert.IsNull(slime.FindStatus<Regeneration>());

            slime.RefreshVolumeStatuses();
            Assert.IsNotNull(slime.FindStatus<Regeneration>());
            Assert.AreEqual(5, slime.Volume.UnitCount);

            slime.RefreshStatuses();
            Assert.AreEqual(6, slime.Volume.UnitCount);
            Assert.AreEqual(5, slime.Volume.CountOf<Blood>());
        }

        [Test]
        public void BurnThenRegeneration_AtCapacity_RegenFrontNoops_ThenBurnsWater()
        {
            // Blood sync puts Regeneration at front; Burning stays behind.
            // At 10: regen noops, burn pops tip water → 9.
            var slime = SpawnSlime();
            slime.Volume.Clear();
            for (var i = 0; i < 8; i++)
            {
                slime.Volume.Add(new Blood());
            }

            slime.Volume.Add(new Water());
            slime.Volume.Add(new Water());
            Assert.AreEqual(10, slime.Volume.UnitCount);

            slime.AddStatus(new Burning());
            slime.RefreshVolumeStatuses();
            Assert.IsInstanceOf<Regeneration>(slime.Statuses[0]);
            Assert.IsInstanceOf<Burning>(slime.Statuses[1]);

            slime.RefreshStatuses();
            Assert.AreEqual(9, slime.Volume.UnitCount);
            Assert.AreEqual(8, slime.Volume.CountOf<Blood>());
            Assert.AreEqual(1, slime.Volume.CountOf<Water>());
            Assert.IsNotNull(slime.FindStatus<Regeneration>());
        }

        [Test]
        public void RegenerationThenBurn_AtCapacity_RegenNoops_ThenBurnsWater()
        {
            // Dominance first → Regen at front; Burning appended later.
            // At 10 units regen cannot grow; burn pops tip water → 9.
            var slime = BloodWaterSlime(blood: 8, water: 2);
            Assert.AreEqual(10, slime.Volume.UnitCount);
            slime.RefreshVolumeStatuses();
            Assert.IsNotNull(slime.FindStatus<Regeneration>());

            slime.AddStatus(new Burning());
            Assert.Less(IndexOf<Regeneration>(slime), IndexOf<Burning>(slime));

            slime.RefreshStatuses();
            Assert.AreEqual(9, slime.Volume.UnitCount);
            Assert.AreEqual(8, slime.Volume.CountOf<Blood>());
            Assert.AreEqual(1, slime.Volume.CountOf<Water>());
        }

        [Test]
        public void RegenerationThenBurn_FlammableOilTip_BurnsDoubleOil()
        {
            // Oil Forever Flammable is a dominance-slot flag (not in the pulse queue).
            var slime = OilDominantSlime(oil: 8, waterUnder: 2);
            Assert.IsTrue(slime.FindStatus<Flammable>().Permanent);
            Assert.IsInstanceOf<Oil>(slime.Volume.Units[slime.Volume.UnitCount - 1].Substance);

            slime.AddStatus(new Burning());
            var before = slime.Volume.UnitCount;
            slime.RefreshStatuses();
            Assert.AreEqual(before - 2, slime.Volume.UnitCount);
            Assert.AreEqual(6, slime.Volume.CountOf<Oil>());
        }

        [Test]
        public void SameDominance_KeepsRegenerationAtFront_AcrossPulses()
        {
            var slime = BloodWaterSlime(blood: 4, water: 1);
            slime.AddStatus(new Poisoned());
            slime.RefreshVolumeStatuses();
            var regen = slime.FindStatus<Regeneration>();
            Assert.IsNotNull(regen);
            Assert.AreEqual(0, IndexOf(slime, regen));
            Assert.Less(IndexOf(slime, regen), IndexOf<Poisoned>(slime));

            slime.RefreshStatuses();
            Assert.AreSame(regen, slime.FindStatus<Regeneration>());
            Assert.AreEqual(0, IndexOf(slime, regen));
        }

        [Test]
        public void DominanceChange_ClearsOilSlot_KeepsTimedBurning()
        {
            // Burning in queue; Forever Flammable / Retaliation on dominance slot only.
            var slime = OilDominantSlime(oil: 8, waterUnder: 2, refresh: false);
            slime.AddStatus(new Burning());
            slime.RefreshVolumeStatuses();
            Assert.IsTrue(slime.FindStatus<Flammable>().Permanent);
            Assert.IsNotNull(slime.FindStatus<Retaliation>());
            Assert.AreEqual(0, IndexOf<Burning>(slime));

            var rat = SpawnRat();
            Assert.IsTrue(Combat.Attack(slime, rat, new Oil()));
            Assert.IsFalse(slime.Volume.TryDominant(out _));
            Assert.IsNull(slime.FindStatus<Retaliation>());
            Assert.IsNull(slime.FindStatus<Flammable>());
            Assert.IsNotNull(slime.FindStatus<Burning>());
        }

        [Test]
        public void DominanceChange_ToBlood_InsertsRegenerationAtFront()
        {
            var slime = SpawnSlime();
            slime.Volume.Clear();
            for (var i = 0; i < 8; i++)
            {
                slime.Volume.Add(new Water());
            }

            slime.Volume.Add(new Oil());
            slime.Volume.Add(new Oil());
            slime.RefreshVolumeStatuses();
            Assert.IsNotNull(slime.FindStatus<Fireproof>());
            Assert.AreEqual(
                -1,
                IndexOf<Fireproof>(slime),
                "Fireproof lives on the dominance slot, not the queue."
            );

            slime.AddStatus(new Poisoned());
            Assert.AreEqual(0, IndexOf<Poisoned>(slime));

            slime.Volume.Clear();
            for (var i = 0; i < 8; i++)
            {
                slime.Volume.Add(new Blood());
            }

            slime.Volume.Add(new Water());
            slime.Volume.Add(new Water());
            slime.RefreshVolumeStatuses();
            Assert.IsNull(slime.FindStatus<Fireproof>());
            Assert.IsNotNull(slime.FindStatus<Regeneration>());
            Assert.IsNotNull(slime.FindStatus<Poisoned>());
            Assert.AreEqual(0, IndexOf<Regeneration>(slime));
            Assert.Less(IndexOf<Regeneration>(slime), IndexOf<Poisoned>(slime));
        }

        [Test]
        public void TwoBurns_MidQueueDominanceDrop_SecondBurnSeesNoForeverFlammable()
        {
            // Tip oil: first burn ×2 drops oil dominance mid-queue; second burn ×1 → 10−2−1 = 7.
            var slime = OilDominantSlime(oil: 8, waterUnder: 2);
            Assert.IsTrue(slime.FindStatus<Flammable>().Permanent);

            slime.AddStatus(new Burning());
            slime.AddStatus(new Burning());
            slime.RefreshStatuses();

            Assert.AreEqual(7, slime.Volume.UnitCount);
            Assert.AreEqual(5, slime.Volume.CountOf<Oil>());
            Assert.IsNull(slime.FindStatus<Flammable>());
        }

        [Test]
        public void TimedFlammable_SurvivesOilDominanceLoss_BurnStillDouble()
        {
            // Forever Flammable is cleared with dominance; timed Flammable from Apply stays in queue.
            var slime = OilDominantSlime(oil: 8, waterUnder: 2);
            slime.AddStatus(new Flammable());
            Assert.AreEqual(2, slime.CountStatus<Flammable>());

            var rat = SpawnRat();
            Assert.IsTrue(Combat.Attack(slime, rat, new Oil()));
            Assert.IsFalse(slime.Volume.TryDominant(out _));
            Assert.AreEqual(1, slime.CountStatus<Flammable>());
            Assert.IsFalse(slime.FindStatus<Flammable>().Permanent);

            slime.AddStatus(new Burning());
            var before = slime.Volume.UnitCount;
            slime.RefreshStatuses();
            Assert.AreEqual(before - 2, slime.Volume.UnitCount);
        }

        [Test]
        public void WaterDominance_FireproofBlocksBurning_RegenerationNeverQueued()
        {
            // Water traits stay in slot; Burning is blocked by Fireproof — queue order unchanged.
            var slime = SpawnSlime();
            slime.Volume.Clear();
            for (var i = 0; i < 8; i++)
            {
                slime.Volume.Add(new Water());
            }

            slime.Volume.Add(new Oil());
            slime.Volume.Add(new Oil());
            slime.RefreshVolumeStatuses();
            Assert.IsNotNull(slime.FindStatus<Fireproof>());
            Assert.IsNull(slime.FindStatus<Regeneration>());

            slime.AddStatus(new Burning());
            Assert.IsNull(slime.FindStatus<Burning>());

            slime.RefreshStatuses();
            Assert.AreEqual(10, slime.Volume.UnitCount);
            Assert.IsNotNull(slime.FindStatus<Fireproof>());
        }

        [Test]
        public void PoisonedThenRegeneration_BloodSync_PutsRegenInFront()
        {
            // Poisoned first; blood sync inserts Regeneration at front → regen then poison.
            var slime = BloodWaterSlime(blood: 4, water: 1);
            slime.AddStatus(new Poisoned());
            slime.RefreshVolumeStatuses();
            Assert.Less(IndexOf<Regeneration>(slime), IndexOf<Poisoned>(slime));

            slime.RefreshStatuses();
            // Regen adds blood on tip, then poison pops that tip blood → still 4 blood + 1 water.
            Assert.AreEqual(5, slime.Volume.UnitCount);
            Assert.AreEqual(4, slime.Volume.CountOf<Blood>());
            Assert.AreEqual(1, slime.Volume.CountOf<Water>());
        }

        [Test]
        public void MidPassBloodDominance_AppendedRegeneration_GrowsSameTurn()
        {
            // 6 blood / 8 = 75% — no dominant. Burn tip water → 6/7 blood dominant.
            // Regen inserted at front; Burn already pulsed → Regen pulses same pass → 7 blood + 1 water.
            var slime = BloodWaterSlime(blood: 6, water: 2);
            Assert.IsFalse(slime.Volume.TryDominant(out _));
            Assert.IsNull(slime.FindStatus<Regeneration>());

            slime.AddStatus(new Burning());
            slime.RefreshStatuses();

            Assert.IsNotNull(slime.FindStatus<Regeneration>());
            Assert.AreEqual(8, slime.Volume.UnitCount);
            Assert.AreEqual(7, slime.Volume.CountOf<Blood>());
            Assert.AreEqual(1, slime.Volume.CountOf<Water>());
        }

        [Test]
        public void MidPassBloodDominance_RegenAtFront_SavesFromSecondBurn()
        {
            // Burn1 flips to blood → Regen inserted at front before Burn2 → grow then Burn2.
            var slime = BloodWaterSlime(blood: 1, water: 1);
            Assert.IsFalse(slime.Volume.TryDominant(out _));

            slime.AddStatus(new Burning());
            slime.AddStatus(new Burning());
            slime.RefreshStatuses();

            Assert.IsTrue(slime.IsAlive);
            Assert.AreEqual(1, slime.Volume.UnitCount);
            Assert.AreEqual(1, slime.Volume.CountOf<Blood>());
            Assert.IsNotNull(slime.FindStatus<Regeneration>());
        }

        private static int IndexOf<T>(Creature creature)
            where T : StatusEffect
        {
            for (var i = 0; i < creature.Statuses.Count; i++)
            {
                if (creature.Statuses[i] is T)
                {
                    return i;
                }
            }

            return -1;
        }

        private static int IndexOf(Creature creature, StatusEffect effect)
        {
            for (var i = 0; i < creature.Statuses.Count; i++)
            {
                if (ReferenceEquals(creature.Statuses[i], effect))
                {
                    return i;
                }
            }

            return -1;
        }

        private Slime BloodWaterSlime(int blood, int water)
        {
            var slime = SpawnSlime();
            slime.Volume.Clear();
            for (var i = 0; i < blood; i++)
            {
                slime.Volume.Add(new Blood());
            }

            for (var i = 0; i < water; i++)
            {
                slime.Volume.Add(new Water());
            }

            return slime;
        }

        /// <summary>Water under, oil on tip — oil burns first when flammable.</summary>
        private Slime OilDominantSlime(int oil, int waterUnder, bool refresh = true)
        {
            var slime = SpawnSlime();
            slime.Volume.Clear();
            for (var i = 0; i < waterUnder; i++)
            {
                slime.Volume.Add(new Water());
            }

            for (var i = 0; i < oil; i++)
            {
                slime.Volume.Add(new Oil());
            }

            if (refresh)
            {
                slime.RefreshVolumeStatuses();
            }

            return slime;
        }

        private Slime SpawnSlime()
        {
            var slime = new GameObject("Slime").AddComponent<Slime>();
            spawned.Add(slime.gameObject);
            slime.PlaceOn(Vector2Int.zero);
            slime.Volume.Clear();
            slime.SetMaxHitPoints(0);
            return slime;
        }

        private Rat SpawnRat()
        {
            var rat = new GameObject("Rat").AddComponent<Rat>();
            spawned.Add(rat.gameObject);
            rat.PlaceOn(Vector2Int.right);
            if (rat.Volume.UnitCount == 0)
            {
                Rat.FillStarting(rat.Volume);
            }

            rat.SetMaxHitPoints(3);
            return rat;
        }
    }
}
