using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SlimesRevenge.Tests
{
    public class CorpseTests
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
        public void Death_LeavesWalkableCorpse_DecayEqualsMaxHitPoints()
        {
            var player = Spawn<Slime>(new Vector2Int(1, 1));
            // Rat attacks between strikes and Volume.Damage pops newest shield units — keep spare water.
            player.Volume.Add(new Water());
            player.Volume.Add(new Water());
            player.Volume.Add(new Water());
            player.RefreshVolumeStatuses();
            var rat = Spawn<Rat>(new Vector2Int(2, 1));
            var turns = SpawnObject("Turns").AddComponent<TurnManager>();
            turns.Rng = new FixedRng();
            turns.Bind(World.CreateGrass(3), player, rat);

            while (rat.IsAlive)
            {
                Assert.IsTrue(turns.TryAttack(rat.Cell, new Water()));
            }

            Assert.IsFalse(turns.Session.IsOccupied(new Vector2Int(2, 1)));
            var corpses = turns.Session.World.Floor.GetCorpses(new Vector2Int(2, 1));
            Assert.AreEqual(1, corpses.Count);
            Assert.IsTrue(corpses[0].IsCorpse);
            Assert.AreSame(rat, corpses[0]);
            Assert.AreEqual(1, corpses[0].Volume.UnitCount);
            // Decay tracks max HP (3), not blood units (1). Death resolve skipped aging.
            Assert.AreEqual(3, corpses[0].DecayTurnsLeft);
            Assert.IsTrue(turns.TryMoveTo(new Vector2Int(2, 1)));
        }

        [Test]
        public void Devour_TransfersBulkOnlyOnFinalPulse()
        {
            var slime = Spawn<Slime>(Vector2Int.zero);
            slime.Volume.Clear();
            slime.Volume.Add(new Water());
            slime.Volume.Add(new Water());
            slime.RefreshVolumeStatuses();
            var cat = SpawnCorpse<Cat>(Vector2Int.zero);
            Assert.AreEqual(2, cat.Volume.UnitCount);
            Assert.AreEqual(2, slime.Volume.UnitCount);

            Assert.IsTrue(Digesting.CanBegin(slime.Volume, cat));
            Assert.IsTrue(slime.AddStatus(new Digesting(cat)));
            Assert.AreEqual(2, slime.FindStatus<Digesting>().Remaining);

            slime.TickStatuses();
            Assert.AreEqual(2, slime.Volume.UnitCount);
            Assert.IsTrue(slime.IsDigesting);
            Assert.AreEqual(1, slime.FindStatus<Digesting>().Remaining);
            Assert.AreEqual(2, cat.Volume.UnitCount);

            slime.TickStatuses();
            Assert.AreEqual(4, slime.Volume.UnitCount);
            Assert.IsFalse(slime.IsDigesting);
        }

        [Test]
        public void StackedCorpses_OnlyTopIsVisible_MenuListsAll()
        {
            var floor = new Floor();
            var cell = Vector2Int.zero;
            var rat = SpawnCorpseWithRenderer<Rat>(cell);
            var cat = SpawnCorpseWithRenderer<Cat>(cell);
            floor.AddCorpse(rat);
            floor.AddCorpse(cat);

            var stack = floor.GetCorpses(cell);
            Assert.AreEqual(2, stack.Count);
            Assert.IsFalse(rat.GetComponent<SpriteRenderer>().enabled);
            Assert.IsTrue(cat.GetComponent<SpriteRenderer>().enabled);

            Assert.IsTrue(floor.TryTakeCorpse(cell, 1, out var taken));
            Assert.AreSame(cat, taken);
            Assert.IsFalse(cat.GetComponent<SpriteRenderer>().enabled);
            Assert.IsTrue(rat.GetComponent<SpriteRenderer>().enabled);
            Assert.AreEqual(1, floor.GetCorpses(cell).Count);
        }

        private T SpawnCorpseWithRenderer<T>(Vector2Int cell)
            where T : Creature
        {
            var creature = SpawnCorpse<T>(cell);
            if (creature.GetComponent<SpriteRenderer>() == null)
            {
                var renderer = creature.gameObject.AddComponent<SpriteRenderer>();
                renderer.enabled = true;
            }

            return creature;
        }

        [Test]
        public void Digestion_CannotBegin_WhenSlimeSmallerThanCorpse()
        {
            var slime = Spawn<Slime>(Vector2Int.zero);
            slime.Volume.Clear();
            slime.Volume.Add(new Water());
            slime.Volume.Add(new Water());
            slime.RefreshVolumeStatuses();
            var dog = SpawnCorpse<Dog>(Vector2Int.zero);
            Assert.AreEqual(3, dog.Volume.UnitCount);
            Assert.AreEqual(2, slime.Volume.UnitCount);

            Assert.IsFalse(Digesting.CanBegin(slime.Volume, dog));
            Assert.IsFalse(slime.IsDigesting);
        }

        [Test]
        public void Digestion_CanBegin_WhenSlimeVolumeEqualsCorpse()
        {
            var slime = Spawn<Slime>(Vector2Int.zero);
            slime.Volume.Clear();
            for (var i = 0; i < 3; i++)
            {
                slime.Volume.Add(new Water());
            }

            slime.RefreshVolumeStatuses();
            var dog = SpawnCorpse<Dog>(Vector2Int.zero);
            Assert.AreEqual(3, dog.Volume.UnitCount);
            Assert.AreEqual(3, slime.Volume.UnitCount);

            Assert.IsTrue(Digesting.CanBegin(slime.Volume, dog));
            Assert.IsTrue(slime.AddStatus(new Digesting(dog)));
            Assert.IsTrue(slime.IsDigesting);
        }

        [Test]
        public void RatCorpse_ExpiresAfterThreeAgingTicks_NotOne()
        {
            // skip + 3 decrements (HP) — cat (5) still remains if left on the floor.
            var floor = new Floor();
            var cell = Vector2Int.zero;
            var rat = SpawnCorpse<Rat>(cell);
            var cat = SpawnCorpse<Cat>(cell);
            floor.AddCorpse(rat);
            floor.AddCorpse(cat);

            Assert.IsTrue(floor.TryTakeCorpse(cell, 1, out var taken));
            Assert.AreSame(cat, taken);
            Assert.AreEqual(1, floor.GetCorpses(cell).Count);

            floor.Tick(); // skip
            Assert.AreEqual(1, floor.GetCorpses(cell).Count);
            floor.Tick(); // 3 → 2
            Assert.AreEqual(1, floor.GetCorpses(cell).Count);
            floor.Tick(); // 2 → 1
            Assert.AreEqual(1, floor.GetCorpses(cell).Count);
            floor.Tick(); // 1 → 0, remove
            Assert.AreEqual(0, floor.GetCorpses(cell).Count);
        }

        private T SpawnCorpse<T>(Vector2Int cell)
            where T : Creature
        {
            var creature = Spawn<T>(cell);
            creature.BecomeCorpse();
            return creature;
        }

        private T Spawn<T>(Vector2Int cell)
            where T : Creature
        {
            var creature = SpawnObject(typeof(T).Name).AddComponent<T>();
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
                else if (creature is Cat)
                {
                    Cat.FillStarting(creature.Volume);
                }
                else if (creature is Dog)
                {
                    Dog.FillStarting(creature.Volume);
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
            else if (creature is Cat)
            {
                creature.SetMaxHitPoints(5);
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
