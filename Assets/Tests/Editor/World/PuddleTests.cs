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
            player.RefreshVolumeStatuses();
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

        [Test]
        public void Dog_StepsThroughThreeWaterPuddles_TheyVanish()
        {
            var world = World.CreateGrass(8);
            var cells = new[]
            {
                new Vector2Int(1, 0),
                new Vector2Int(2, 0),
                new Vector2Int(3, 0),
            };
            foreach (var cell in cells)
            {
                Assert.IsTrue(world.Floor.TryPlacePuddle(cell, new Water()));
            }

            var player = Spawn<Slime>(new Vector2Int(7, 7));
            var dog = Spawn<Dog>(new Vector2Int(0, 0));
            var session = new GameSession(world, player.Cell, new[] { dog.Cell });

            foreach (var cell in cells)
            {
                Assert.IsTrue(session.TryMoveOccupant(dog.Cell, cell));
                dog.PlaceOn(cell);
                world.Floor.ApplyContact(dog);
                Assert.IsNull(world.Floor.GetPuddle(cell));
            }

            Assert.AreEqual(3, dog.CountStatus<Wet>());
        }

        [Test]
        public void Dog_OilPuddleThenPoisonPuddle_GetsBothStatuses_PuddlesVanish()
        {
            var world = World.CreateGrass(8);
            var oilCell = new Vector2Int(1, 0);
            var poisonCell = new Vector2Int(2, 0);
            Assert.IsTrue(world.Floor.TryPlacePuddle(oilCell, new Oil()));
            Assert.IsTrue(world.Floor.TryPlacePuddle(poisonCell, new Poison()));

            var player = Spawn<Slime>(new Vector2Int(7, 7));
            var dog = Spawn<Dog>(new Vector2Int(0, 0));
            var session = new GameSession(world, player.Cell, new[] { dog.Cell });

            Assert.IsTrue(session.TryMoveOccupant(dog.Cell, oilCell));
            dog.PlaceOn(oilCell);
            world.Floor.ApplyContact(dog);
            Assert.IsNotNull(dog.FindStatus<Instability>());
            Assert.IsNotNull(dog.FindStatus<Flammable>());
            Assert.IsNull(world.Floor.GetPuddle(oilCell));

            Assert.IsTrue(session.TryMoveOccupant(dog.Cell, poisonCell));
            dog.PlaceOn(poisonCell);
            world.Floor.ApplyContact(dog);
            Assert.IsNotNull(dog.FindStatus<Instability>());
            Assert.IsNotNull(dog.FindStatus<Flammable>());
            Assert.IsNotNull(dog.FindStatus<Poisoned>());
            Assert.IsNull(world.Floor.GetPuddle(poisonCell));
        }

        [Test]
        public void Dog_LavaPuddleThenWaterPuddle_ClearsBurning_AppliesWet()
        {
            var world = World.CreateGrass(8);
            var lavaCell = new Vector2Int(1, 0);
            var waterCell = new Vector2Int(2, 0);
            Assert.IsTrue(world.Floor.TryPlacePuddle(lavaCell, new Lava()));
            Assert.IsTrue(world.Floor.TryPlacePuddle(waterCell, new Water()));

            var player = Spawn<Slime>(new Vector2Int(7, 7));
            var dog = Spawn<Dog>(new Vector2Int(0, 0));
            var session = new GameSession(world, player.Cell, new[] { dog.Cell });

            Assert.IsTrue(session.TryMoveOccupant(dog.Cell, lavaCell));
            dog.PlaceOn(lavaCell);
            world.Floor.ApplyContact(dog);
            Assert.IsNotNull(dog.FindStatus<Burning>());
            Assert.IsNull(world.Floor.GetPuddle(lavaCell));

            Assert.IsTrue(session.TryMoveOccupant(dog.Cell, waterCell));
            dog.PlaceOn(waterCell);
            world.Floor.ApplyContact(dog);
            Assert.IsNull(dog.FindStatus<Burning>());
            Assert.IsNull(dog.FindStatus<Wet>(), "Water on burning extinguishes without applying Wet.");
            Assert.IsNull(world.Floor.GetPuddle(waterCell));
        }

        [Test]
        public void Dog_AcidPuddleThenWaterPuddle_ClearsCorroding_NoWet()
        {
            var world = World.CreateGrass(8);
            var acidCell = new Vector2Int(1, 0);
            var waterCell = new Vector2Int(2, 0);
            Assert.IsTrue(world.Floor.TryPlacePuddle(acidCell, new Acid()));
            Assert.IsTrue(world.Floor.TryPlacePuddle(waterCell, new Water()));

            var player = Spawn<Slime>(new Vector2Int(7, 7));
            var dog = Spawn<Dog>(new Vector2Int(0, 0));
            var session = new GameSession(world, player.Cell, new[] { dog.Cell });

            Assert.IsTrue(session.TryMoveOccupant(dog.Cell, acidCell));
            dog.PlaceOn(acidCell);
            world.Floor.ApplyContact(dog);
            Assert.IsNotNull(dog.FindStatus<Corroding>());
            Assert.IsNull(world.Floor.GetPuddle(acidCell));

            Assert.IsTrue(session.TryMoveOccupant(dog.Cell, waterCell));
            dog.PlaceOn(waterCell);
            world.Floor.ApplyContact(dog);
            Assert.IsNull(dog.FindStatus<Corroding>());
            Assert.IsNull(dog.FindStatus<Wet>());
            Assert.IsNull(world.Floor.GetPuddle(waterCell));
        }

        [Test]
        public void Dog_WetThenLavaPuddle_ClearsWet_NoBurning()
        {
            var world = World.CreateGrass(8);
            var waterCell = new Vector2Int(1, 0);
            var lavaCell = new Vector2Int(2, 0);
            Assert.IsTrue(world.Floor.TryPlacePuddle(waterCell, new Water()));
            Assert.IsTrue(world.Floor.TryPlacePuddle(lavaCell, new Lava()));

            var player = Spawn<Slime>(new Vector2Int(7, 7));
            var dog = Spawn<Dog>(new Vector2Int(0, 0));
            var session = new GameSession(world, player.Cell, new[] { dog.Cell });

            Assert.IsTrue(session.TryMoveOccupant(dog.Cell, waterCell));
            dog.PlaceOn(waterCell);
            world.Floor.ApplyContact(dog);
            Assert.IsNotNull(dog.FindStatus<Wet>());

            Assert.IsTrue(session.TryMoveOccupant(dog.Cell, lavaCell));
            dog.PlaceOn(lavaCell);
            world.Floor.ApplyContact(dog);
            Assert.IsNull(dog.FindStatus<Burning>());
            Assert.IsNull(dog.FindStatus<Wet>());
            Assert.IsNull(world.Floor.GetPuddle(lavaCell));
        }

        [Test]
        public void Slime_StepsThroughTwoPuddles_NoStatuses_BothRemain()
        {
            var world = World.CreateGrass(8);
            var oilCell = new Vector2Int(1, 0);
            var lavaCell = new Vector2Int(2, 0);
            Assert.IsTrue(world.Floor.TryPlacePuddle(oilCell, new Oil()));
            Assert.IsTrue(world.Floor.TryPlacePuddle(lavaCell, new Lava()));

            var slime = Spawn<Slime>(new Vector2Int(0, 0));
            var statusCountBefore = slime.Statuses.Count;

            slime.PlaceOn(oilCell);
            world.Floor.ApplyContact(slime);
            Assert.IsNotNull(world.Floor.GetPuddle(oilCell));
            Assert.IsInstanceOf<Oil>(world.Floor.GetPuddle(oilCell).Substance);
            Assert.IsNull(slime.FindStatus<Instability>());
            Assert.IsNull(slime.FindStatus<Flammable>());
            Assert.AreEqual(statusCountBefore, slime.Statuses.Count);

            slime.PlaceOn(lavaCell);
            world.Floor.ApplyContact(slime);
            Assert.IsNotNull(world.Floor.GetPuddle(lavaCell));
            Assert.IsInstanceOf<Lava>(world.Floor.GetPuddle(lavaCell).Substance);
            Assert.IsNull(slime.FindStatus<Burning>());
            Assert.AreEqual(statusCountBefore, slime.Statuses.Count);
            Assert.IsNotNull(world.Floor.GetPuddle(oilCell));
        }

        [Test]
        public void Slime_StepsThroughWaterPuddles_TheyRemain()
        {
            var world = World.CreateGrass(8);
            var cells = new[]
            {
                new Vector2Int(1, 0),
                new Vector2Int(2, 0),
                new Vector2Int(3, 0),
            };
            foreach (var cell in cells)
            {
                Assert.IsTrue(world.Floor.TryPlacePuddle(cell, new Water()));
            }

            var slime = Spawn<Slime>(new Vector2Int(0, 0));
            var wetBefore = slime.CountStatus<Wet>();

            foreach (var cell in cells)
            {
                slime.PlaceOn(cell);
                world.Floor.ApplyContact(slime);
                Assert.IsNotNull(world.Floor.GetPuddle(cell));
                Assert.IsInstanceOf<Water>(world.Floor.GetPuddle(cell).Substance);
            }

            Assert.AreEqual(wetBefore, slime.CountStatus<Wet>());
        }

        [Test]
        public void Bat_StepsOnBloodPuddle_HealsByVampirismHealAmount_AndPuddleVanishes()
        {
            var world = World.CreateGrass(6);
            var bloodCell = new Vector2Int(1, 0);
            Assert.IsTrue(world.Floor.TryPlacePuddle(bloodCell, new Blood()));

            var player = Spawn<Slime>(new Vector2Int(5, 5));
            var bat = Spawn<Bat>(new Vector2Int(0, 0));
            bat.Damage(1);
            Assert.AreEqual(2, bat.HitPoints);
            Assert.AreEqual(2, Vampirism.HealAmount);

            var session = new GameSession(world, player.Cell, new[] { bat.Cell });
            Assert.IsTrue(session.TryMoveOccupant(bat.Cell, bloodCell));
            bat.PlaceOn(bloodCell);
            world.Floor.ApplyContact(bat);

            Assert.AreEqual(Mathf.Min(bat.MaxHitPoints, 2 + Vampirism.HealAmount), bat.HitPoints);
            Assert.IsNull(world.Floor.GetPuddle(bloodCell));
        }

        [Test]
        public void Dog_StepsOnWater_WithBurnPoisonCorrode_WashesFireAndAcid_KeepsPoison_NoWet()
        {
            var world = World.CreateGrass(6);
            var waterCell = new Vector2Int(1, 0);
            Assert.IsTrue(world.Floor.TryPlacePuddle(waterCell, new Water()));

            var dog = Spawn<Dog>(new Vector2Int(0, 0));
            dog.AddStatus(new Burning());
            dog.AddStatus(new Poisoned());
            dog.AddStatus(new Corroding());

            dog.PlaceOn(waterCell);
            world.Floor.ApplyContact(dog);

            Assert.IsNull(dog.FindStatus<Burning>());
            Assert.IsNull(dog.FindStatus<Corroding>());
            Assert.IsNotNull(dog.FindStatus<Poisoned>());
            Assert.IsNull(dog.FindStatus<Wet>());
            Assert.IsNull(world.Floor.GetPuddle(waterCell));
        }

        private T Spawn<T>(Vector2Int cell) where T : Creature
        {
            var creature = new GameObject(typeof(T).Name).AddComponent<T>();
            spawned.Add(creature.gameObject);
            creature.PlaceOn(cell);
            if (creature.Volume.UnitCount == 0)
            {
                switch (creature)
                {
                    case Slime:
                        Slime.FillStarting(creature.Volume);
                        break;
                    case Dog:
                        Dog.FillStarting(creature.Volume);
                        break;
                    case Bat:
                        Bat.FillStarting(creature.Volume);
                        break;
                }
            }

            switch (creature)
            {
                case Slime:
                    creature.SetMaxHitPoints(0);
                    creature.RefreshVolumeStatuses();
                    break;
                case Dog:
                    creature.SetSpeed(2);
                    creature.SetMaxHitPoints(10);
                    break;
                case Bat:
                    creature.SetMaxHitPoints(3);
                    break;
            }

            creature.EnsureInnateTraits();
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
