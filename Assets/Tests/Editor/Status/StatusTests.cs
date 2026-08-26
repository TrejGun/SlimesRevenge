using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SlimesRevenge.Tests
{
    public class StatusTests
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
        public void WaterAndBlood_ExtinguishFireAndApplyDousing()
        {
            var rat = Spawn<Rat>(Vector2Int.zero);
            rat.AddStatus(new Burning());
            new Water().Apply(rat);
            Assert.IsNull(rat.FindStatus<Burning>());
            Assert.IsInstanceOf<Dousing>(rat.FindStatus<Dousing>());

            rat.AddStatus(new Burning());
            new Blood().Apply(rat);
            Assert.IsNull(rat.FindStatus<Burning>());
        }

        [Test]
        public void Oil_AppliesInstabilityAndSlowsByOne()
        {
            var slime = Spawn<Slime>(Vector2Int.zero);
            var dog = Spawn<Dog>(Vector2Int.right);
            Assert.AreEqual(2, dog.Speed);
            Assert.IsTrue(Combat.Attack(slime, dog, new Oil()));
            Assert.AreEqual(3, dog.Volume.UnitCount);
            Assert.AreEqual(9, dog.HitPoints);
            Assert.AreEqual(1, dog.CountStatus<Instability>());
            Assert.AreEqual(1, dog.CountStatus<Oiled>());
            Assert.AreEqual(1, dog.Speed);
        }

        [Test]
        public void Oil_OnSpeedOne_FreezesMovement()
        {
            var origin = new Vector2Int(4, 4);
            var player = Spawn<Slime>(new Vector2Int(4, 0));
            var cat = Spawn<Cat>(origin);
            var session = new GameSession(World.CreateGrass(), player.Cell, new[] { origin });
            Assert.IsTrue(Combat.Attack(player, cat, new Oil()));
            Assert.AreEqual(0, cat.Speed);
            cat.TakeTurn(session, player, new FixedRng());
            Assert.AreEqual(origin, cat.Cell);
        }

        [Test]
        public void ThreeLavas_StackThreeBurningOnDog()
        {
            var slime = Spawn<Slime>(Vector2Int.zero);
            slime.Volume.Clear();
            slime.Volume.Fill(new Lava(), new Lava(), new Lava());
            slime.RefreshBodyTraits();
            var dog = Spawn<Dog>(Vector2Int.right);

            Assert.IsTrue(Combat.Attack(slime, dog, new Lava()));
            Assert.IsTrue(Combat.Attack(slime, dog, new Lava()));
            Assert.IsTrue(Combat.Attack(slime, dog, new Lava()));
            Assert.AreEqual(3, dog.CountStatus<Burning>());
            Assert.AreEqual(7, dog.HitPoints);
        }

        [Test]
        public void Oil_OnBurning_AddsThreeTurnsToEachStack()
        {
            var dog = Spawn<Dog>(Vector2Int.zero);
            dog.AddStatus(new Burning());
            dog.AddStatus(new Burning());
            Assert.AreEqual(3, dog.Statuses[0].Remaining);
            new Oil().Apply(dog);
            Assert.AreEqual(6, dog.Statuses[0].Remaining);
            Assert.AreEqual(6, dog.Statuses[1].Remaining);
            Assert.AreEqual(1, dog.CountStatus<Oiled>());
        }

        [Test]
        public void OilStacks_LengthenLaterIgnition()
        {
            var dog = Spawn<Dog>(Vector2Int.zero);
            new Oil().Apply(dog);
            new Oil().Apply(dog);
            dog.AddStatus(new Burning());
            Assert.AreEqual(9, dog.FindStatus<Burning>().Remaining);
        }

        [Test]
        public void Water_ClearsAllBurning_KeepsOil()
        {
            var dog = Spawn<Dog>(Vector2Int.zero);
            dog.AddStatus(new Burning());
            new Oil().Apply(dog);
            new Oil().Apply(dog);
            Assert.AreEqual(9, dog.FindStatus<Burning>().Remaining);

            new Water().Apply(dog);
            Assert.IsNull(dog.FindStatus<Burning>());
            Assert.AreEqual(2, dog.CountStatus<Oiled>());
            Assert.AreEqual(2, dog.CountStatus<Instability>());
            Assert.IsNotNull(dog.FindStatus<Dousing>());
        }

        [Test]
        public void Poison_DoesNotConvertBlood()
        {
            var dog = Spawn<Dog>(Vector2Int.zero);
            Assert.AreEqual(3, dog.Volume.UnitCount);
            new Poison().Apply(dog);
            Assert.AreEqual(3, Count<Blood>(dog));
            Assert.AreEqual(0, Count<Poison>(dog));
            Assert.IsNotNull(dog.FindStatus<Poisoned>());
        }

        [Test]
        public void Acid_DoesNotConvertBlood()
        {
            var dog = Spawn<Dog>(Vector2Int.zero);
            new Acid().Apply(dog);
            Assert.AreEqual(3, Count<Blood>(dog));
            Assert.IsNotNull(dog.FindStatus<Corroding>());
        }

        [Test]
        public void Dog_SurvivesPoisonStatus()
        {
            AssertDogSurvives(new Poison(), 6);
        }

        [Test]
        public void Dog_SurvivesAcidStatus()
        {
            AssertDogSurvives(new Acid(), 6);
        }

        [Test]
        public void PermanentStatus_DoesNotExpire()
        {
            var rat = Spawn<Rat>(Vector2Int.zero);
            rat.AddStatus(new Instability(StatusEffect.Forever));
            Assert.AreEqual(0, rat.Speed);
            for (var i = 0; i < 5; i++)
            {
                rat.TickStatuses();
            }

            Assert.AreEqual(1, rat.CountStatus<Instability>());
            Assert.IsTrue(rat.Statuses[0].Permanent);
            Assert.AreEqual(0, rat.Speed);
        }

        private void AssertDogSurvives(Substance substance, int expectedHitPoints)
        {
            var player = Spawn<Slime>(new Vector2Int(1, 1));
            var dog = Spawn<Dog>(new Vector2Int(2, 1));
            var turns = SpawnObject("Turns").AddComponent<TurnManager>();
            turns.Rng = new FixedRng();
            turns.Bind(World.CreateGrass(3), player, dog);

            Assert.IsTrue(turns.TryAttack(dog.Cell, substance));
            Assert.AreEqual(3, dog.Volume.UnitCount);
            // Attack −1, then on the dog's turn the DoT pulses once before it acts.
            Assert.AreEqual(8, dog.HitPoints);

            Assert.IsTrue(turns.TryWait());
            Assert.AreEqual(7, dog.HitPoints);
            Assert.IsTrue(turns.TryWait());
            Assert.AreEqual(expectedHitPoints, dog.HitPoints);
            Assert.IsTrue(dog.IsAlive);
            Assert.IsTrue(turns.TryWait());
            Assert.AreEqual(expectedHitPoints, dog.HitPoints);
        }

        private static int Count<T>(Creature creature) where T : Substance
        {
            var count = 0;
            foreach (var unit in creature.Volume.Units)
            {
                if (unit.Substance is T)
                {
                    count++;
                }
            }

            return count;
        }

        private T Spawn<T>(Vector2Int cell) where T : Creature
        {
            var creature = SpawnObject(typeof(T).Name).AddComponent<T>();
            creature.PlaceOn(cell);
            if (creature.Volume.UnitCount == 0)
            {
                FillStarting(creature);
            }

            EnsureHitPoints(creature);
            if (creature is Slime)
            {
                creature.RefreshBodyTraits();
            }

            return creature;
        }

        private static void EnsureHitPoints(Creature creature)
        {
            switch (creature)
            {
                case Slime:
                    creature.SetMaxHitPoints(1);
                    break;
                case Rat:
                    creature.SetMaxHitPoints(3);
                    break;
                case Cat:
                    creature.SetMaxHitPoints(5);
                    break;
                case Dog:
                    creature.SetSpeed(2);
                    creature.SetMaxHitPoints(10);
                    break;
            }
        }

        private static void FillStarting(Creature creature)
        {
            switch (creature)
            {
                case Slime:
                    Slime.FillStarting(creature.Volume);
                    break;
                case Rat:
                    Rat.FillStarting(creature.Volume);
                    break;
                case Cat:
                    Cat.FillStarting(creature.Volume);
                    break;
                case Dog:
                    Dog.FillStarting(creature.Volume);
                    break;
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
