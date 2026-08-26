using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SlimesRevenge.Tests
{
    /// <summary>
    /// Volume ↔ status refresh timing: mid-queue RefreshVolumeStatuses after volume change,
    /// and refresh after spend / mess / hit.
    /// </summary>
    public class StatusVolumeRefreshTests
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
        public void RefreshStatuses_FirstBurnAtFlammableDouble_DropsOilDominance_SecondBurnIsSingle()
        {
            // Tip = oil (newest). 8 oil / 10 = 80% → Forever Flammable.
            // First Burning pops 2 oil → 6/8 → dominance lost → Forever cleared mid-queue.
            // Second Burning must pulse at ×1.
            var slime = SpawnOilDominantSlime(oilCount: 8, waterUnder: 2);
            Assert.IsTrue(slime.FindStatus<Flammable>().Permanent);
            Assert.AreEqual(10, slime.Volume.UnitCount);

            slime.AddStatus(new Burning());
            slime.AddStatus(new Burning());

            slime.RefreshStatuses();

            Assert.AreEqual(7, slime.Volume.UnitCount);
            Assert.AreEqual(5, slime.Volume.CountOf<Oil>());
            Assert.AreEqual(2, slime.Volume.CountOf<Water>());
            Assert.IsFalse(slime.Volume.TryDominant(out _));
            Assert.IsNull(slime.FindStatus<Flammable>());
            Assert.IsTrue(slime.IsAlive);
        }

        [Test]
        public void RefreshStatuses_TwoBurnsWhileStillOilDominant_BothDouble()
        {
            // Pure oil: after −2 still 8/8 dominant → second burn still ×2 → total −4.
            var slime = SpawnOilDominantSlime(oilCount: 10, waterUnder: 0);
            Assert.IsTrue(slime.FindStatus<Flammable>().Permanent);

            slime.AddStatus(new Burning());
            slime.AddStatus(new Burning());
            slime.RefreshStatuses();

            Assert.AreEqual(6, slime.Volume.UnitCount);
            Assert.IsTrue(slime.FindStatus<Flammable>().Permanent);
        }

        [Test]
        public void MessOil_LosesForeverFlammable_BeforeNextRefreshStatuses_BurnIsSingle()
        {
            var slime = SpawnOilDominantSlime(oilCount: 8, waterUnder: 2);
            Assert.IsTrue(slime.FindStatus<Flammable>().Permanent);

            var turns = SpawnObject("Turns").AddComponent<TurnManager>();
            turns.Rng = new FixedRng();
            turns.Bind(World.CreateGrass(3), slime);

            Assert.IsTrue(turns.TryMakeMess(new Oil()));
            Assert.AreEqual(9, slime.Volume.UnitCount);
            Assert.AreEqual(7, slime.Volume.CountOf<Oil>());
            Assert.IsFalse(slime.Volume.TryDominant(out _));
            Assert.IsNull(slime.FindStatus<Flammable>());

            slime.AddStatus(new Burning());
            var before = slime.Volume.UnitCount;
            slime.RefreshStatuses();
            Assert.AreEqual(before - 1, slime.Volume.UnitCount);
        }

        [Test]
        public void MessOil_ThenCollect_RestoresForeverFlammable()
        {
            var slime = SpawnOilDominantSlime(oilCount: 8, waterUnder: 2);
            var turns = SpawnObject("Turns").AddComponent<TurnManager>();
            turns.Rng = new FixedRng();
            turns.Bind(World.CreateGrass(3), slime);

            Assert.IsTrue(turns.TryMakeMess(new Oil()));
            Assert.IsNull(slime.FindStatus<Flammable>());

            Assert.IsTrue(turns.TryCollectPuddle());
            Assert.AreEqual(10, slime.Volume.UnitCount);
            Assert.AreEqual(8, slime.Volume.CountOf<Oil>());
            Assert.IsTrue(slime.FindStatus<Flammable>().Permanent);
        }

        [Test]
        public void OpponentLavaHit_OnOilDominant_RefreshVolume_AfterDamage()
        {
            var slime = SpawnOilDominantSlime(oilCount: 8, waterUnder: 2);
            Assert.IsTrue(slime.FindStatus<Flammable>().Permanent);

            var attacker = SpawnSlimeWith(new Lava(), new Lava());
            Assert.IsTrue(Combat.Attack(attacker, slime, new Lava()));

            // Lava strike ×2 on flammable tip-oil: −2 oil → lose dominance.
            Assert.AreEqual(8, slime.Volume.UnitCount);
            Assert.IsFalse(slime.Volume.TryDominant(out _));
            Assert.IsNull(slime.FindStatus<Flammable>());
            Assert.AreEqual(1, slime.CountStatus<Burning>());
        }

        [Test]
        public void SpendOilAttack_RefreshVolume_ClearsForeverFlammable_AtThreshold()
        {
            var slime = SpawnOilDominantSlime(oilCount: 8, waterUnder: 2);
            var rat = Spawn<Rat>(Vector2Int.right);
            Assert.IsTrue(Combat.Attack(slime, rat, new Oil()));

            Assert.AreEqual(9, slime.Volume.UnitCount);
            Assert.IsFalse(slime.Volume.TryDominant(out _));
            Assert.IsNull(slime.FindStatus<Flammable>());
            Assert.IsNotNull(rat.FindStatus<Flammable>());
            Assert.IsFalse(rat.FindStatus<Flammable>().Permanent);
        }

        private Slime SpawnOilDominantSlime(int oilCount, int waterUnder)
        {
            var slime = Spawn<Slime>(Vector2Int.zero);
            slime.Volume.Clear();
            for (var i = 0; i < waterUnder; i++)
            {
                slime.Volume.Add(new Water());
            }

            for (var i = 0; i < oilCount; i++)
            {
                slime.Volume.Add(new Oil());
            }

            slime.RefreshVolumeStatuses();
            return slime;
        }

        private Slime SpawnSlimeWith(params Substance[] units)
        {
            var slime = Spawn<Slime>(new Vector2Int(1, 0));
            slime.Volume.Clear();
            foreach (var unit in units)
            {
                slime.Volume.Add(Volume.CloneSubstance(unit));
            }

            slime.RefreshVolumeStatuses();
            return slime;
        }

        private T Spawn<T>(Vector2Int cell) where T : Creature
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

        private GameObject SpawnObject(string name)
        {
            var go = new GameObject(name);
            spawned.Add(go);
            return go;
        }
    }
}
