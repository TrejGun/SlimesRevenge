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
            slime.RefreshBodyTraits();

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
            Assert.IsFalse(volume.TryDominant(out _));
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
        public void OilDominant_DoublesLavaAttackAndBurnPulse()
        {
            var slime = SpawnSlime();
            // Pure oil so Flammable survives the −2 strike and still doubles the burn pulse.
            slime.Volume.Clear();
            for (var i = 0; i < 10; i++)
            {
                slime.Volume.Add(new Oil());
            }

            slime.RefreshBodyTraits();
            Assert.IsNotNull(slime.FindStatus<Flammable>());

            var before = slime.Volume.UnitCount;
            var lava = new Lava();
            Combat.Attack(SpawnSlimeWith(lava), slime, lava);
            Assert.AreEqual(before - 2, slime.Volume.UnitCount);
            Assert.AreEqual(1, slime.CountStatus<Burning>());

            slime.TickStatuses();
            Assert.AreEqual(before - 4, slime.Volume.UnitCount);
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

        private static void FillDominant(Creature slime, Substance kind, int count)
        {
            slime.Volume.Clear();
            for (var i = 0; i < count; i++)
            {
                slime.Volume.Add(Volume.CloneSubstance(kind));
            }

            slime.Volume.Add(new Blood());
            slime.Volume.Add(new Water());
            slime.RefreshBodyTraits();
        }

        private Slime SpawnSlimeWith(Substance substance)
        {
            var slime = SpawnSlime();
            slime.Volume.Clear();
            slime.Volume.Add(Volume.CloneSubstance(substance));
            slime.RefreshBodyTraits();
            return slime;
        }

        private Slime SpawnSlime()
        {
            return Spawn<Slime>(Vector2Int.zero);
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
                creature.SetMaxHitPoints(1);
                creature.RefreshBodyTraits();
            }
            else if (creature is Rat)
            {
                creature.SetMaxHitPoints(3);
            }

            return creature;
        }
    }
}
