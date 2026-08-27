using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SlimesRevenge.Tests
{
    public class CreatureAiTests
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
        public void RatFlee_MovesOneCellWhenItSeesThePlayer()
        {
            var origin = new Vector2Int(4, 4);
            var player = Spawn<Slime>(new Vector2Int(4, 0));
            var rat = Spawn<Rat>(origin);
            var session = Occupied(World.CreateGrass(), player, rat);

            Assert.AreEqual(1, rat.Speed);
            Assert.AreEqual(CreaturePersonality.Cowardly, rat.Personality);
            rat.TakeTurn(session, player, new FixedRng());

            Assert.AreEqual(1, GridStep.Chebyshev(origin, rat.Cell));
            Assert.Greater(GridStep.Chebyshev(rat.Cell, player.Cell), 4);
        }

        [Test]
        public void DogChase_PrefersATwoCellStep()
        {
            // Chebyshev 5 keeps the player in vision (range 5); 6 would idle instead of chase.
            var origin = new Vector2Int(4, 5);
            var player = Spawn<Slime>(new Vector2Int(4, 0));
            var dog = Spawn<Dog>(origin);
            var session = Occupied(World.CreateGrass(), player, dog);

            Assert.AreEqual(2, dog.Speed);
            dog.TakeTurn(session, player, new FixedRng());

            Assert.AreEqual(2, GridStep.Chebyshev(origin, dog.Cell));
            Assert.Less(GridStep.Chebyshev(dog.Cell, player.Cell), 5);
        }

        [Test]
        public void Rat_DoesNotAttackUntilInCombat()
        {
            var player = Spawn<Slime>(new Vector2Int(1, 1));
            var rat = Spawn<Rat>(new Vector2Int(0, 0));
            var session = Occupied(World.CreateGrass(3), player, rat);

            rat.TakeTurn(session, player, new FixedRng());
            Assert.AreEqual(6, player.Volume.UnitCount, "Cowardly rat must not bite before aggro.");
            Assert.AreEqual(1, rat.Volume.UnitCount);

            rat.MarkAggro();
            // Re-adjacent if the rat fled on the first turn.
            if (!GridStep.IsAdjacent(rat.Cell, player.Cell))
            {
                Assert.IsTrue(session.TryMoveOccupant(rat.Cell, new Vector2Int(0, 0)));
                rat.PlaceOn(new Vector2Int(0, 0));
                if (!GridStep.IsAdjacent(rat.Cell, player.Cell))
                {
                    Assert.IsTrue(session.TryMoveOccupant(player.Cell, new Vector2Int(1, 0)));
                    player.PlaceOn(new Vector2Int(1, 0));
                }
            }

            Assert.IsTrue(GridStep.IsAdjacent(rat.Cell, player.Cell));
            rat.TakeTurn(session, player, new FixedRng());
            Assert.AreEqual(5, player.Volume.UnitCount);
            Assert.AreEqual(1, rat.Volume.UnitCount);
            Assert.AreEqual(CreaturePersonality.Cowardly, rat.Personality);
        }

        [Test]
        public void Cat_DoesNothingWhenItSeesThePlayer()
        {
            var player = Spawn<Slime>(new Vector2Int(0, 0));
            var cat = Spawn<Cat>(new Vector2Int(3, 0));
            var session = Occupied(World.CreateGrass(), player, cat);

            Assert.IsTrue(CreatureMoves.CanSee(cat, player));
            cat.TakeTurn(session, player, new FixedRng());
            Assert.AreEqual(new Vector2Int(3, 0), cat.Cell);
            Assert.AreEqual(6, player.Volume.UnitCount);
            Assert.AreEqual(CreaturePersonality.Passive, cat.Personality);
        }

        [Test]
        public void Cat_IgnoresPlayerUntilInCombat()
        {
            var player = Spawn<Slime>(new Vector2Int(4, 4));
            var cat = Spawn<Cat>(new Vector2Int(5, 5));
            var session = Occupied(World.CreateGrass(), player, cat);

            cat.TakeTurn(session, player, new FixedRng());
            Assert.AreEqual(6, player.Volume.UnitCount);
            Assert.AreEqual(new Vector2Int(5, 5), cat.Cell);

            cat.MarkAggro();
            cat.TakeTurn(session, player, new FixedRng());
            Assert.AreEqual(5, player.Volume.UnitCount);
            Assert.AreEqual(2, cat.Volume.UnitCount);
            Assert.AreEqual(CreaturePersonality.Passive, cat.Personality);
        }

        [Test]
        public void Dog_ChasesWhenPlayerIsInVision()
        {
            var origin = new Vector2Int(5, 0);
            var player = Spawn<Slime>(new Vector2Int(0, 0));
            var dog = Spawn<Dog>(origin);
            var session = Occupied(World.CreateGrass(), player, dog);

            Assert.IsTrue(GridStep.InRange(origin, player.Cell, dog.VisionRange));
            dog.TakeTurn(session, player, new FixedRng());
            Assert.Less(GridStep.Chebyshev(dog.Cell, player.Cell), 5);
            Assert.AreEqual(3, dog.Volume.UnitCount);
        }

        [Test]
        public void Rat_FleesAndDropsAggroWhenPlayerBreaksContact()
        {
            var player = Spawn<Slime>(new Vector2Int(4, 4));
            var rat = Spawn<Rat>(new Vector2Int(5, 4));
            var session = Occupied(World.CreateGrass(), player, rat);

            Assert.AreEqual(CreaturePersonality.Cowardly, rat.Personality);
            rat.MarkAggro();
            Assert.IsTrue(session.TryStep(Vector2Int.left));
            player.PlaceOn(session.ControlledCell.Value);
            Assert.AreEqual(new Vector2Int(3, 4), player.Cell);

            rat.TakeTurn(session, player, new FixedRng());
            Assert.Greater(GridStep.Chebyshev(rat.Cell, player.Cell), 2);
            Assert.AreEqual(CreaturePersonality.Cowardly, rat.Personality);
            Assert.IsFalse(rat.InCombat);
        }

        [Test]
        public void Dog_GoesAroundAWallOfThreeCatsToReachTheSlime()
        {
            var player = Spawn<Slime>(new Vector2Int(1, 5));
            var top = Spawn<Cat>(new Vector2Int(2, 6));
            var middle = Spawn<Cat>(new Vector2Int(2, 5));
            var bottom = Spawn<Cat>(new Vector2Int(2, 4));
            var dog = Spawn<Dog>(new Vector2Int(3, 5));
            var session = Occupied(World.CreateGrass(), player, top, middle, bottom, dog);
            var others = new List<Creature> { top, middle, bottom, dog };
            var cats = new[] { top.Cell, middle.Cell, bottom.Cell };
            var start = dog.Cell;

            Assert.AreEqual(2, GridStep.Chebyshev(player.Cell, dog.Cell));

            dog.TakeTurn(session, player, new FixedRng(), others);
            Assert.AreNotEqual(start, dog.Cell);
            Assert.IsFalse(
                GridStep.IsAdjacent(dog.Cell, player.Cell),
                "A jump through the cats is not allowed."
            );
            Assert.GreaterOrEqual(GridStep.Chebyshev(dog.Cell, player.Cell), 2);
            foreach (var cat in cats)
            {
                Assert.AreNotEqual(cat, dog.Cell);
                Assert.IsTrue(session.IsOccupied(cat));
            }

            var turns = 0;
            while (!GridStep.IsAdjacent(dog.Cell, player.Cell) && turns < 12)
            {
                dog.TakeTurn(session, player, new FixedRng(), others);
                foreach (var cat in cats)
                {
                    Assert.AreNotEqual(cat, dog.Cell);
                }

                turns++;
            }

            Assert.IsTrue(GridStep.IsAdjacent(dog.Cell, player.Cell));
            Assert.AreEqual(6, player.Volume.UnitCount);
            Assert.AreEqual(3, dog.Volume.UnitCount);
        }

        [Test]
        public void Dog_GoesAroundAWallOfFiveCatsToReachTheSlime()
        {
            var player = Spawn<Slime>(new Vector2Int(1, 5));
            var cats = new[]
            {
                Spawn<Cat>(new Vector2Int(2, 7)),
                Spawn<Cat>(new Vector2Int(2, 6)),
                Spawn<Cat>(new Vector2Int(2, 5)),
                Spawn<Cat>(new Vector2Int(2, 4)),
                Spawn<Cat>(new Vector2Int(2, 3)),
            };
            var dog = Spawn<Dog>(new Vector2Int(3, 5));
            var session = Occupied(
                World.CreateGrass(),
                player,
                cats[0],
                cats[1],
                cats[2],
                cats[3],
                cats[4],
                dog
            );
            var others = new List<Creature> { cats[0], cats[1], cats[2], cats[3], cats[4], dog };
            var start = dog.Cell;
            var catCells = new[]
            {
                cats[0].Cell,
                cats[1].Cell,
                cats[2].Cell,
                cats[3].Cell,
                cats[4].Cell,
            };

            Assert.AreEqual(2, GridStep.Chebyshev(player.Cell, dog.Cell));
            dog.TakeTurn(session, player, new FixedRng(), others);
            Assert.AreNotEqual(start, dog.Cell);
            Assert.IsFalse(GridStep.IsAdjacent(dog.Cell, player.Cell));
            foreach (var cat in catCells)
            {
                Assert.AreNotEqual(cat, dog.Cell);
                Assert.IsTrue(session.IsOccupied(cat));
            }

            var turns = 0;
            while (!GridStep.IsAdjacent(dog.Cell, player.Cell) && turns < 16)
            {
                dog.TakeTurn(session, player, new FixedRng(), others);
                foreach (var cat in catCells)
                {
                    Assert.AreNotEqual(cat, dog.Cell);
                }

                turns++;
            }

            Assert.IsTrue(GridStep.IsAdjacent(dog.Cell, player.Cell));
        }

        [Test]
        public void CreatureVisionRange_DefaultsToFive()
        {
            var rat = Spawn<Rat>(Vector2Int.zero);
            var cat = Spawn<Cat>(Vector2Int.one);
            var dog = Spawn<Dog>(new Vector2Int(2, 2));
            var bat = Spawn<Bat>(new Vector2Int(3, 3));
            var scorpion = Spawn<Scorpion>(new Vector2Int(4, 4));
            Assert.AreEqual(5, rat.VisionRange);
            Assert.AreEqual(5, cat.VisionRange);
            Assert.AreEqual(5, dog.VisionRange);
            Assert.AreEqual(1, rat.Speed);
            Assert.AreEqual(1, cat.Speed);
            Assert.AreEqual(2, dog.Speed);
            Assert.AreEqual(CreaturePersonality.Cowardly, rat.Personality);
            Assert.AreEqual(CreaturePersonality.Passive, cat.Personality);
            Assert.AreEqual(CreaturePersonality.Aggressive, dog.Personality);
            Assert.AreEqual(CreaturePersonality.Passive, bat.Personality);
            Assert.AreEqual(CreaturePersonality.Passive, scorpion.Personality);
        }

        [Test]
        public void Occupant_CanStepTwoCellsOntoAnEmptyCell()
        {
            var from = new Vector2Int(4, 0);
            var session = new GameSession(
                World.CreateGrass(),
                new[] { new Vector2Int(0, 0), from }
            );

            Assert.IsTrue(session.TryMoveOccupant(from, new Vector2Int(2, 0)));
            Assert.IsTrue(session.IsOccupied(new Vector2Int(2, 0)));
            Assert.IsFalse(session.IsOccupied(from));
        }

        [Test]
        public void AfterPlayerKills_AnotherMobCanEnterTheVacatedCell()
        {
            var world = new World(4, 1, TerrainType.Grass);
            var player = Spawn<Slime>(new Vector2Int(0, 0));
            player.Volume.Add(new Water());
            player.Volume.Add(new Water());
            player.Volume.Add(new Water());
            player.RefreshVolumeStatuses();
            var rat = Spawn<Rat>(new Vector2Int(1, 0));
            var dog = Spawn<Dog>(new Vector2Int(3, 0));
            var turns = SpawnObject("Turns").AddComponent<TurnManager>();
            turns.Rng = new FixedRng();
            turns.Bind(world, player, rat, dog);

            while (rat.IsAlive)
            {
                Assert.IsTrue(turns.TryAttack(rat.Cell, new Water()));
            }

            Assert.IsFalse(rat.IsAlive);
            Assert.AreEqual(new Vector2Int(1, 0), dog.Cell);
            Assert.IsTrue(turns.Session.IsOccupied(dog.Cell));
            Assert.IsFalse(turns.Session.IsOccupied(new Vector2Int(3, 0)));
        }

        [Test]
        public void Cat_HuntsRatWhenIdleOrWandering()
        {
            var player = Spawn<Slime>(new Vector2Int(0, 0));
            var cat = Spawn<Cat>(new Vector2Int(3, 0));
            var rat = Spawn<Rat>(new Vector2Int(4, 0));
            var session = Occupied(World.CreateGrass(), player, cat, rat);
            var others = new List<Creature> { cat, rat };

            Assert.IsNotNull(cat.FindStatus<HatesRats>());
            Assert.AreEqual(
                CreatureIntent.Attack,
                CreatureBrain.Decide(cat, player, session, new FixedRng(), others)
            );

            var ratHp = rat.HitPoints;
            cat.TakeTurn(session, player, new FixedRng(), others);
            Assert.AreEqual(ratHp - 1, rat.HitPoints);
            Assert.AreEqual(6, player.Volume.UnitCount);
        }

        [Test]
        public void Dog_HuntsCatWhenIdleAwayFromPlayer()
        {
            var player = Spawn<Slime>(new Vector2Int(0, 0));
            var dog = Spawn<Dog>(new Vector2Int(8, 8));
            var cat = Spawn<Cat>(new Vector2Int(8, 7));
            var session = Occupied(World.CreateGrass(), player, dog, cat);
            var others = new List<Creature> { dog, cat };

            Assert.IsFalse(CreatureMoves.CanSee(dog, player));
            Assert.IsTrue(CreatureMoves.CanSee(dog, cat));
            Assert.IsNotNull(dog.FindStatus<HatesCats>());
            Assert.AreEqual(
                CreatureIntent.Attack,
                CreatureBrain.Decide(dog, player, session, new FixedRng(), others)
            );

            var catHp = cat.HitPoints;
            dog.TakeTurn(session, player, new FixedRng(), others);
            Assert.AreEqual(catHp - 1, cat.HitPoints);
        }

        private T Spawn<T>(Vector2Int cell)
            where T : Creature
        {
            var creature = SpawnObject(typeof(T).Name).AddComponent<T>();
            creature.PlaceOn(cell);
            if (creature.Volume.UnitCount == 0)
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
                    case Bat:
                        Bat.FillStarting(creature.Volume);
                        break;
                    case Scorpion:
                        Scorpion.FillStarting(creature.Volume);
                        break;
                }
            }

            switch (creature)
            {
                case Slime:
                    creature.SetMaxHitPoints(0);
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
                case Bat:
                    creature.SetMaxHitPoints(3);
                    break;
                case Scorpion:
                    creature.SetMaxHitPoints(3);
                    break;
            }

            if (creature is Slime)
            {
                creature.RefreshVolumeStatuses();
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

        private static GameSession Occupied(World world, Creature player, params Creature[] others)
        {
            return SessionFactory.WithControlled(world, player, others);
        }
    }
}
