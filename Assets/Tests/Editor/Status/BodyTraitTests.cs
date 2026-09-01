using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SlimesRevenge.Tests
{
    public class BodyTraitTests
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
        public void EightOfTenLava_IsDominant()
        {
            var slime = SpawnSlime();
            slime.Volume.Clear();
            for (var i = 0; i < 8; i++)
            {
                slime.Volume.Add(new Lava());
            }

            slime.Volume.Add(new Water());
            slime.Volume.Add(new Oil());
            slime.RefreshVolumeStatuses();

            Assert.IsTrue(slime.Volume.TryDominant(out var dominant));
            Assert.IsInstanceOf<Lava>(dominant);
            Assert.IsNotNull(slime.FindStatus<Retaliation>());
            Assert.IsInstanceOf<Lava>(slime.FindStatus<Retaliation>().Retort);
        }

        [Test]
        public void SevenOfNine_IsNotDominant()
        {
            var volume = new Volume();
            for (var i = 0; i < 7; i++)
            {
                volume.Add(new Lava());
            }

            volume.Add(new Water());
            volume.Add(new Oil());
            Assert.AreEqual(Volume.DominantPercent, 80);
            Assert.IsFalse(volume.TryDominant(out _));
        }

        [Test]
        public void EightOfTen_MeetsDominantPercent()
        {
            var volume = new Volume();
            for (var i = 0; i < 8; i++)
            {
                volume.Add(new Oil());
            }

            volume.Add(new Water());
            volume.Add(new Water());
            Assert.IsTrue(volume.TryDominant(out var dominant));
            Assert.IsInstanceOf<Oil>(dominant);
        }

        [Test]
        public void WaterDominant_IsFireproof()
        {
            var slime = SpawnSlime();
            FillDominant(slime, new Water(), 8);
            Assert.IsNotNull(slime.FindStatus<Fireproof>());
            slime.AddStatus(new Burning());
            Assert.AreEqual(0, slime.CountStatus<Burning>());
        }

        [Test]
        public void WaterDominant_LavaStrike_DoesNotIgnite()
        {
            var defender = SpawnSlime();
            FillDominant(defender, new Water(), 8);
            Assert.IsNotNull(defender.FindStatus<Fireproof>());

            var before = defender.Volume.UnitCount;
            Assert.IsTrue(Combat.Attack(SpawnSlimeWith(new Lava()), defender, new Lava()));

            Assert.AreEqual(before - 1, defender.Volume.UnitCount);
            Assert.AreEqual(0, defender.CountStatus<Burning>());
            Assert.IsNotNull(defender.FindStatus<Fireproof>());
        }

        [Test]
        public void Burning_VolumeShiftToWaterDominance_Extinguishes_NoPulseDamage()
        {
            var slime = SpawnSlime();
            slime.Volume.Clear();
            for (var i = 0; i < 7; i++)
            {
                slime.Volume.Add(new Water());
            }

            slime.Volume.Add(new Oil());
            slime.Volume.Add(new Oil()); // tip
            slime.RefreshVolumeStatuses();
            Assert.IsFalse(slime.Volume.TryDominant(out _));
            Assert.IsNull(slime.FindStatus<Fireproof>());

            slime.AddStatus(new Burning());
            Assert.AreEqual(1, slime.CountStatus<Burning>());

            // Pop tip oil → 7 water / 8 = water dominant → Fireproof + Extinguish.
            slime.Damage(1);
            Assert.AreEqual(8, slime.Volume.UnitCount);
            Assert.AreEqual(7, slime.Volume.CountOf<Water>());
            Assert.IsNotNull(slime.FindStatus<Fireproof>());
            Assert.AreEqual(0, slime.CountStatus<Burning>());

            var units = slime.Volume.UnitCount;
            slime.RefreshStatuses();
            Assert.AreEqual(units, slime.Volume.UnitCount);
        }

        [Test]
        public void OilDominant_DoublesLavaAttackAndBurnPulse()
        {
            var slime = SpawnSlime();
            // Pure oil so permanent Flammable survives the −2 strike and still doubles the burn pulse.
            slime.Volume.Clear();
            for (var i = 0; i < 10; i++)
            {
                slime.Volume.Add(new Oil());
            }

            slime.RefreshVolumeStatuses();
            Assert.IsNotNull(slime.FindStatus<Flammable>());
            Assert.IsTrue(slime.FindStatus<Flammable>().Permanent);

            var before = slime.Volume.UnitCount;
            var lava = new Lava();
            Combat.Attack(SpawnSlimeWith(lava), slime, lava);
            Assert.AreEqual(before - 2, slime.Volume.UnitCount);
            Assert.AreEqual(1, slime.CountStatus<Burning>());

            slime.RefreshStatuses();
            Assert.AreEqual(before - 4, slime.Volume.UnitCount);
        }

        [Test]
        public void OilDominant_AtThreshold_SpendOilAttack_ClearsForeverFlammable_KeepsTimedIfAny()
        {
            var slime = SpawnSlime();
            slime.Volume.Clear();
            for (var i = 0; i < 8; i++)
            {
                slime.Volume.Add(new Oil());
            }

            slime.Volume.Add(new Water());
            slime.Volume.Add(new Water());
            slime.RefreshVolumeStatuses();

            Assert.AreEqual(Volume.DominantPercent, 80);
            Assert.IsTrue(slime.Volume.TryDominant(out var dominant));
            Assert.IsInstanceOf<Oil>(dominant);
            Assert.IsTrue(slime.FindStatus<Flammable>().Permanent);
            Assert.IsNotNull(slime.FindStatus<Retaliation>());

            // Timed residue under Forever: FindStatus still prefers Forever.
            slime.AddStatus(new Flammable());
            Assert.AreEqual(2, slime.CountStatus<Flammable>());
            Assert.IsTrue(slime.FindStatus<Flammable>().Permanent);

            var rat = Spawn<Rat>(Vector2Int.right);
            Assert.IsTrue(Combat.Attack(slime, rat, new Oil()));

            Assert.AreEqual(7, slime.Volume.CountOf<Oil>());
            Assert.AreEqual(2, slime.Volume.CountOf<Water>());
            Assert.AreEqual(9, slime.Volume.UnitCount);
            Assert.IsFalse(slime.Volume.TryDominant(out _));
            Assert.IsNull(slime.FindStatus<Retaliation>());
            // Forever wiped; timed Flammable remains and is what FindStatus returns.
            Assert.AreEqual(1, slime.CountStatus<Flammable>());
            Assert.IsFalse(slime.FindStatus<Flammable>().Permanent);
            Assert.IsNotNull(rat.FindStatus<Flammable>());
            Assert.IsFalse(rat.FindStatus<Flammable>().Permanent);
        }

        [Test]
        public void PoisonDominant_RetaliatesWithPoison()
        {
            var slime = SpawnSlime();
            FillDominant(slime, new Poison(), 8);
            var rat = Spawn<Rat>(Vector2Int.right);
            Combat.Attack(rat, slime);
            Assert.IsNotNull(rat.FindStatus<Poisoned>());
        }

        [Test]
        public void LavaDominant_RetaliatesWithBurning()
        {
            var slime = SpawnSlime();
            FillDominant(slime, new Lava(), 8);
            var rat = Spawn<Rat>(Vector2Int.right);
            Combat.Attack(rat, slime);
            Assert.AreEqual(1, rat.CountStatus<Burning>());
        }

        [Test]
        public void MercuryDominant_GrantsInvisible()
        {
            var slime = SpawnSlime();
            FillDominant(slime, new Mercury(), 8);
            Assert.IsNotNull(slime.FindStatus<Invisible>());
        }

        [Test]
        public void BloodDominant_RegenerationGrowsOnPulseUntilCapacity()
        {
            // RefreshVolumeStatuses only hangs Regeneration; growth is FIFO OnPulse at turn start.
            var slime = SpawnSlime();
            slime.Volume.Clear();
            for (var i = 0; i < 4; i++)
            {
                slime.Volume.Add(new Blood());
            }

            slime.Volume.Add(new Water());
            Assert.AreEqual(5, slime.Volume.UnitCount);

            slime.RefreshVolumeStatuses();
            Assert.IsNotNull(slime.FindStatus<Regeneration>());
            Assert.AreEqual(4, slime.Volume.CountOf<Blood>());
            Assert.AreEqual(5, slime.Volume.UnitCount);

            slime.RefreshStatuses();
            Assert.AreEqual(5, slime.Volume.CountOf<Blood>());
            Assert.AreEqual(6, slime.Volume.UnitCount);

            while (slime.Volume.UnitCount < Volume.Capacity)
            {
                slime.RefreshStatuses();
            }

            Assert.AreEqual(Volume.Capacity, slime.Volume.UnitCount);
            Assert.AreEqual(9, slime.Volume.CountOf<Blood>());
            Assert.AreEqual(1, slime.Volume.CountOf<Water>());

            slime.RefreshStatuses();
            Assert.AreEqual(Volume.Capacity, slime.Volume.UnitCount);
        }

        private static void FillDominant(Creature slime, Substance kind, int count)
        {
            slime.Volume.Clear();
            for (var i = 0; i < count; i++)
            {
                slime.Volume.Add(Volume.CloneSubstance(kind));
            }

            slime.Volume.Add(new Blood());
            slime.Volume.Add(new Water());
            slime.RefreshVolumeStatuses();
        }

        private Slime SpawnSlimeWith(Substance substance)
        {
            var slime = SpawnSlime();
            slime.Volume.Clear();
            slime.Volume.Add(Volume.CloneSubstance(substance));
            slime.RefreshVolumeStatuses();
            return slime;
        }

        private Slime SpawnSlime()
        {
            return Spawn<Slime>(Vector2Int.zero);
        }

        private T Spawn<T>(Vector2Int cell)
            where T : Creature
        {
            var creature = new GameObject(typeof(T).Name).AddComponent<T>();
            spawned.Add(creature.gameObject);
            creature.PlaceOn(cell);
            if (creature.Volume.UnitCount == 0)
            {
                if (creature is Slime)
                {
                    Slime.FillStarting(creature.Volume);
                }
                else if (creature is Rat)
                {
                    Rat.FillStarting(creature.Volume);
                }
            }

            if (creature is Slime)
            {
                creature.SetMaxHitPoints(0);
                creature.RefreshVolumeStatuses();
            }
            else if (creature is Rat)
            {
                creature.SetMaxHitPoints(3);
            }

            return creature;
        }
    }
}
