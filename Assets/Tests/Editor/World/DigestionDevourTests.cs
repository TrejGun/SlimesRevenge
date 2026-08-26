using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SlimesRevenge.Tests
{
    /// <summary>
    /// Devouring stacked corpses: floor decay follows max HP; units are only matter transferred.
    /// </summary>
    public class DigestionDevourTests
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
        public void DevourMixedCorpse_TakesFromEndOfStack_DiscardsRemainder()
        {
            // Stack end first: Water, Oil, Blood — room for two → keep Blood then Oil; Water discarded.
            var cell = Vector2Int.zero;
            var world = new World(3, 3, TerrainType.Grass);
            var slime = SpawnObject("Slime").AddComponent<Slime>();
            slime.PlaceOn(cell);
            slime.Volume.Clear();
            for (var i = 0; i < 8; i++)
            {
                slime.Volume.Add(new Acid());
            }

            slime.SetMaxHitPoints(0);
            slime.RefreshVolumeStatuses();

            var turns = SpawnObject("Turns").AddComponent<TurnManager>();
            turns.Rng = new FixedRng();
            turns.Bind(world, slime);

            var dog = SpawnObject("Dog").AddComponent<Dog>();
            dog.PlaceOn(cell);
            dog.Volume.Clear();
            dog.Volume.Add(new Water());
            dog.Volume.Add(new Oil());
            dog.Volume.Add(new Blood());
            dog.SetMaxHitPoints(10);
            dog.BecomeCorpse();
            world.Floor.AddCorpse(dog);

            Assert.IsTrue(turns.TryDevourCorpse(0));
            Assert.AreEqual(9, slime.Volume.UnitCount);
            Assert.IsInstanceOf<Blood>(slime.Volume.Units[8].Substance);
            Assert.IsTrue(slime.Digestion.IsBusy);
            Assert.IsInstanceOf<Oil>(slime.Digestion.Current.Volume.Units[1].Substance);

            Assert.IsTrue(turns.TryWait());
            Assert.AreEqual(Volume.Capacity, slime.Volume.UnitCount);
            Assert.IsInstanceOf<Oil>(slime.Volume.Units[9].Substance);
            Assert.IsTrue(slime.Digestion.IsBusy);
            Assert.IsInstanceOf<Water>(slime.Digestion.Current.Volume.Units[0].Substance);

            Assert.IsTrue(turns.TryWait());
            Assert.AreEqual(Volume.Capacity, slime.Volume.UnitCount);
            Assert.IsFalse(slime.Digestion.IsBusy);
            Assert.AreEqual(8, slime.Volume.CountOf<Acid>());
            Assert.AreEqual(0, slime.Volume.CountOf<Water>());
            Assert.AreEqual(1, slime.Volume.CountOf<Oil>());
            Assert.AreEqual(1, slime.Volume.CountOf<Blood>());
        }

        [Test]
        public void DevourRatThenCat_AllBloodReachesSlime()
        {
            var cell = Vector2Int.zero;
            var (slime, turns, floor) = SetupEmptySlimeOnCorpses(cell);

            Assert.AreEqual(2, floor.GetCorpses(cell).Count);
            Assert.AreEqual(CreatureKind.Rat, floor.GetCorpses(cell)[0].Kind);
            Assert.AreEqual(CreatureKind.Cat, floor.GetCorpses(cell)[1].Kind);

            Assert.IsTrue(turns.TryDevourCorpse(0));
            Assert.AreEqual(1, slime.Volume.UnitCount);
            Assert.IsInstanceOf<Blood>(slime.Volume.Units[0].Substance);
            Assert.IsFalse(slime.Digestion.IsBusy);
            Assert.AreEqual(1, floor.GetCorpses(cell).Count);
            Assert.AreEqual(CreatureKind.Cat, floor.GetCorpses(cell)[0].Kind);

            Assert.IsTrue(turns.TryDevourCorpse(0));
            Assert.AreEqual(2, slime.Volume.UnitCount);
            Assert.IsTrue(slime.Digestion.IsBusy);
            Assert.AreEqual(0, floor.GetCorpses(cell).Count);

            Assert.IsTrue(turns.TryWait());
            Assert.AreEqual(3, slime.Volume.UnitCount);
            Assert.IsFalse(slime.Digestion.IsBusy);
            AssertAllBlood(slime.Volume);
        }

        [Test]
        public void DevourCatFirst_RatStillPresent_CanDevourBoth()
        {
            // Cat digest = begin + one wait (2 units). Rat decay = 3 HP → still on the floor afterward.
            var cell = Vector2Int.zero;
            var (slime, turns, floor) = SetupEmptySlimeOnCorpses(cell);

            Assert.IsTrue(turns.TryDevourCorpse(1));
            Assert.AreEqual(1, slime.Volume.UnitCount);
            Assert.IsInstanceOf<Blood>(slime.Volume.Units[0].Substance);
            Assert.IsTrue(slime.Digestion.IsBusy);
            Assert.AreEqual(1, floor.GetCorpses(cell).Count);
            Assert.AreEqual(CreatureKind.Rat, floor.GetCorpses(cell)[0].Kind);

            Assert.IsTrue(turns.TryWait());
            Assert.AreEqual(2, slime.Volume.UnitCount);
            Assert.IsFalse(slime.Digestion.IsBusy);
            Assert.AreEqual(1, floor.GetCorpses(cell).Count);
            Assert.AreEqual(CreatureKind.Rat, floor.GetCorpses(cell)[0].Kind);

            Assert.IsTrue(turns.TryDevourCorpse(0));
            Assert.AreEqual(3, slime.Volume.UnitCount);
            Assert.IsFalse(slime.Digestion.IsBusy);
            Assert.AreEqual(0, floor.GetCorpses(cell).Count);
            AssertAllBlood(slime.Volume);
        }

        [Test]
        public void DevourRatCatDog_FromSixUnits_FillsToTenDiscardingOverflow()
        {
            // 6 + rat1 + cat2 = 9; dog has 3 units → only 1 unit fits, remaining 2 vanish.
            // HP decay (3/5/10) keeps all three bodies available through this short sequence.
            var cell = Vector2Int.zero;
            var world = new World(3, 3, TerrainType.Grass);
            var slime = SpawnObject("Slime").AddComponent<Slime>();
            slime.PlaceOn(cell);
            slime.Volume.Clear();
            for (var i = 0; i < 6; i++)
            {
                slime.Volume.Add(new Water());
            }

            slime.SetMaxHitPoints(0);
            slime.RefreshVolumeStatuses();

            var turns = SpawnObject("Turns").AddComponent<TurnManager>();
            turns.Rng = new FixedRng();
            turns.Bind(world, slime);

            AddBeastCorpse(world.Floor, cell, CreatureKind.Rat);
            AddBeastCorpse(world.Floor, cell, CreatureKind.Cat);
            AddBeastCorpse(world.Floor, cell, CreatureKind.Dog);
            Assert.AreEqual(3, world.Floor.GetCorpses(cell).Count);

            Assert.IsTrue(turns.TryDevourCorpse(0)); // rat
            Assert.AreEqual(7, slime.Volume.UnitCount);
            Assert.IsTrue(turns.TryDevourCorpse(0)); // cat start
            Assert.AreEqual(8, slime.Volume.UnitCount);
            Assert.IsTrue(turns.TryWait()); // cat finish
            Assert.AreEqual(9, slime.Volume.UnitCount);
            Assert.IsFalse(slime.Digestion.IsBusy);

            Assert.IsTrue(turns.TryDevourCorpse(0)); // dog: +1 → 10
            Assert.AreEqual(10, slime.Volume.UnitCount);
            Assert.IsTrue(slime.Digestion.IsBusy);

            Assert.IsTrue(turns.TryWait()); // capacity full → discard rest, clear digestion
            Assert.AreEqual(Volume.Capacity, slime.Volume.UnitCount);
            Assert.IsFalse(slime.Digestion.IsBusy);
            // 6 water + rat1 + cat2 + dog1 (2 discarded) = 10
            Assert.AreEqual(6, slime.Volume.CountOf<Water>());
            Assert.AreEqual(4, slime.Volume.CountOf<Blood>());
        }

        [Test]
        public void LastVolumeHit_KillsSlime_EvenWhileDigestingCorpse()
        {
            // Empty volume = dead immediately. Digestion in the belly does not keep the slime alive.
            var cell = Vector2Int.zero;
            var slime = SpawnObject("Slime").AddComponent<Slime>();
            slime.PlaceOn(cell);
            slime.Volume.Clear();
            slime.Volume.Add(new Blood());
            slime.SetMaxHitPoints(0);
            slime.RefreshVolumeStatuses();

            var rat = SpawnObject("Rat").AddComponent<Rat>();
            rat.PlaceOn(cell);
            rat.Volume.Clear();
            Rat.FillStarting(rat.Volume);
            rat.SetMaxHitPoints(3);
            rat.BecomeCorpse();
            Assert.IsTrue(slime.Digestion.TryBegin(rat));
            Assert.IsTrue(slime.Digestion.IsBusy);
            Assert.AreEqual(1, slime.Volume.UnitCount);
            Assert.AreEqual(1, rat.Volume.UnitCount);

            var dog = SpawnObject("Dog").AddComponent<Dog>();
            dog.PlaceOn(cell + Vector2Int.right);
            dog.SetMaxHitPoints(10);
            Assert.IsTrue(Combat.Attack(dog, slime));

            Assert.IsFalse(slime.IsAlive);
            Assert.AreEqual(0, slime.Volume.UnitCount);
            Assert.IsTrue(slime.Digestion.IsBusy);
            Assert.AreSame(rat, slime.Digestion.Current);
            Assert.AreEqual(1, slime.Digestion.Current.Volume.UnitCount);
        }

        private (Slime slime, TurnManager turns, Floor floor) SetupEmptySlimeOnCorpses(Vector2Int cell)
        {
            var world = new World(3, 3, TerrainType.Grass);
            var slime = SpawnObject("Slime").AddComponent<Slime>();
            slime.PlaceOn(cell);
            slime.Volume.Clear();
            slime.SetMaxHitPoints(0);
            slime.RefreshVolumeStatuses();

            var turns = SpawnObject("Turns").AddComponent<TurnManager>();
            turns.Rng = new FixedRng();
            turns.Bind(world, slime);

            AddBeastCorpse(world.Floor, cell, CreatureKind.Rat);
            AddBeastCorpse(world.Floor, cell, CreatureKind.Cat);
            return (slime, turns, world.Floor);
        }

        private void AddBeastCorpse(Floor floor, Vector2Int cell, CreatureKind kind)
        {
            Creature creature;
            switch (kind)
            {
                case CreatureKind.Rat:
                    creature = SpawnObject("Rat").AddComponent<Rat>();
                    creature.PlaceOn(cell);
                    creature.Volume.Clear();
                    Rat.FillStarting(creature.Volume);
                    creature.SetMaxHitPoints(3);
                    break;
                case CreatureKind.Cat:
                    creature = SpawnObject("Cat").AddComponent<Cat>();
                    creature.PlaceOn(cell);
                    creature.Volume.Clear();
                    Cat.FillStarting(creature.Volume);
                    creature.SetMaxHitPoints(5);
                    break;
                case CreatureKind.Dog:
                    creature = SpawnObject("Dog").AddComponent<Dog>();
                    creature.PlaceOn(cell);
                    creature.Volume.Clear();
                    Dog.FillStarting(creature.Volume);
                    creature.SetMaxHitPoints(10);
                    break;
                default:
                    Assert.Fail($"Unexpected kind {kind}");
                    return;
            }

            creature.BecomeCorpse();
            floor.AddCorpse(creature);
        }

        private static void AssertAllBlood(Volume volume)
        {
            Assert.Greater(volume.UnitCount, 0);
            foreach (var unit in volume.Units)
            {
                Assert.IsInstanceOf<Blood>(unit.Substance);
            }
        }

        private GameObject SpawnObject(string name)
        {
            var go = new GameObject(name);
            spawned.Add(go);
            return go;
        }
    }
}
