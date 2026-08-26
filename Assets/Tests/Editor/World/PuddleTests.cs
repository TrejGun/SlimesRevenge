using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SlimesRevenge.Tests
{
    public class PuddleTests
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
        public void ThreeLavaPuddles_LeaveDogAtOneHit()
        {
            var dog = Spawn<Dog>(new Vector2Int(1, 1));
            Assert.AreEqual(10, dog.HitPoints);
            for (var i = 0; i < 3; i++)
            {
                new Lava().Apply(dog);
            }

            Assert.AreEqual(3, dog.CountStatus<Burning>());
            for (var pulse = 0; pulse < 3; pulse++)
            {
                dog.TickStatuses();
            }

            Assert.AreEqual(1, dog.HitPoints);
            Assert.IsTrue(dog.IsAlive);
        }

        [Test]
        public void CannotPlaceSecondPuddle()
        {
            var floor = new Floor();
            var cell = Vector2Int.zero;
            Assert.IsTrue(floor.TryPlacePuddle(cell, new Water()));
            Assert.IsFalse(floor.TryPlacePuddle(cell, new Lava()));
            Assert.IsInstanceOf<Water>(floor.GetPuddle(cell).Substance);
        }

        [Test]
        public void Collect_ReturnsUnit()
        {
            var player = Spawn<Slime>(Vector2Int.zero);
            player.Volume.Clear();
            player.RefreshBodyTraits();
            var turns = SpawnObject("Turns").AddComponent<TurnManager>();
            turns.Rng = new FixedRng();
            turns.Bind(World.CreateGrass(3), player);
            Assert.IsTrue(turns.Session.World.Floor.TryPlacePuddle(player.Cell, new Oil()));
            Assert.IsTrue(turns.TryCollectPuddle());
            Assert.AreEqual(1, player.Volume.UnitCount);
            Assert.IsInstanceOf<Oil>(player.Volume.Units[0].Substance);
            Assert.IsNull(turns.Session.World.Floor.GetPuddle(player.Cell));
        }

        [Test]
        public void EnteringPuddle_AppliesWithoutDamage()
        {
            var dog = Spawn<Dog>(Vector2Int.zero);
            var before = dog.HitPoints;
            new Lava().Apply(dog);
            Assert.AreEqual(before, dog.HitPoints);
            Assert.AreEqual(1, dog.CountStatus<Burning>());
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
                else if (creature is Dog)
                {
                    Dog.FillStarting(creature.Volume);
                }
            }

            if (creature is Slime)
            {
                creature.SetMaxHitPoints(1);
                creature.RefreshBodyTraits();
            }
            else if (creature is Dog)
            {
                creature.SetSpeed(2);
                creature.SetMaxHitPoints(10);
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
