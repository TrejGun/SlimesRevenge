using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SlimesRevenge.Tests
{
    /// <summary>
    /// Dog (speed 2) hunts cat; cat flees to a far cell. Dog retargets cat cell / last-seen each turn.
    /// </summary>
    public class DogCatHuntSimulationTests
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
        public void Corridor10_DogAtVisionRange_HuntsCat_CatFlees_DogCatches()
        {
            // 10×10: open corridor on y=4; walls everywhere else so they stay in the lane (like Armata corridor).
            var world = World.CreateGrass(10);
            for (var y = 0; y < 10; y++)
            {
                for (var x = 0; x < 10; x++)
                {
                    if (y != 4)
                    {
                        world.SetTerrain(new Vector2Int(x, y), TerrainType.Wall);
                    }
                }
            }

            // Slime tucked where neither sees it.
            var player = Spawn<Slime>(new Vector2Int(9, 0));
            world.SetTerrain(new Vector2Int(9, 0), TerrainType.Grass);

            var dog = Spawn<Dog>(new Vector2Int(0, 4));
            var cat = Spawn<Cat>(new Vector2Int(5, 4));
            Assert.AreEqual(5, GridStep.Chebyshev(dog.Cell, cat.Cell));
            Assert.AreEqual(dog.VisionRange, GridStep.Chebyshev(dog.Cell, cat.Cell));

            var session = Occupied(world, player, dog, cat);
            var others = new List<Creature> { dog, cat };

            Assert.IsFalse(CreatureMoves.CanSee(dog, player));
            Assert.IsTrue(CreatureMoves.CanSee(dog, cat));
            Assert.AreEqual(CreatureIntent.Chase, CreatureBrain.Decide(dog, player, session, new FixedRng(), others));
            Assert.AreEqual(CreatureIntent.Flee, CreatureBrain.Decide(cat, player, session, new FixedRng(), others));

            cat.TakeTurn(session, player, new FixedRng(), others);
            Assert.IsNotNull(cat.FleeCell);
            Assert.Greater(GridStep.Chebyshev(cat.FleeCell.Value, dog.Cell), GridStep.Chebyshev(cat.Cell, dog.Cell));

            dog.TakeTurn(session, player, new FixedRng(), others);
            Assert.AreEqual(cat.Cell, dog.PursuitCell);

            var turns = 0;
            while (!GridStep.IsAdjacent(dog.Cell, cat.Cell) && turns < 30)
            {
                var sawCat = CreatureMoves.CanSee(dog, cat);
                cat.TakeTurn(session, player, new FixedRng(), others);
                dog.TakeTurn(session, player, new FixedRng(), others);

                Assert.IsFalse(world.IsWall(dog.Cell));
                Assert.IsFalse(world.IsWall(cat.Cell));
                Assert.AreEqual(4, dog.Cell.y);
                Assert.AreEqual(4, cat.Cell.y);

                if (sawCat || CreatureMoves.CanSee(dog, cat))
                {
                    Assert.AreEqual(cat.Cell, dog.PursuitCell);
                }
                else
                {
                    Assert.IsNotNull(dog.PursuitCell);
                }

                turns++;
            }

            Assert.IsTrue(GridStep.IsAdjacent(dog.Cell, cat.Cell), "Speed-2 dog should catch the cat in a straight corridor.");
            Assert.AreEqual(CreatureIntent.Attack, CreatureBrain.Decide(dog, player, session, new FixedRng(), others));
        }

        [Test]
        public void Maze10x10_DogHuntsCatAroundWalls_BothDetour()
        {
            var world = BuildMaze10();
            // Keep slime out of the dog's vision so Aggressive targets the cat, not the player.
            world.SetTerrain(new Vector2Int(9, 5), TerrainType.Grass);
            var player = Spawn<Slime>(new Vector2Int(9, 5));
            var dog = Spawn<Dog>(new Vector2Int(1, 1));
            var cat = Spawn<Cat>(new Vector2Int(5, 5));
            var session = Occupied(world, player, dog, cat);
            var others = new List<Creature> { dog, cat };

            Assert.IsFalse(CreatureMoves.CanSee(dog, player));
            Assert.LessOrEqual(GridStep.Chebyshev(dog.Cell, cat.Cell), dog.VisionRange);
            Assert.IsTrue(CreatureMoves.CanSee(dog, cat));
            Assert.IsTrue(StraightLineHitsWall(world, dog.Cell, cat.Cell), "Direct line must hit a wall so both must detour.");

            cat.TakeTurn(session, player, new FixedRng(), others);
            Assert.IsNotNull(cat.FleeCell);
            Assert.IsTrue(StraightLineHitsWall(world, cat.Cell, cat.FleeCell.Value)
                || GridStep.Chebyshev(cat.Cell, cat.FleeCell.Value) > 1);

            dog.TakeTurn(session, player, new FixedRng(), others);
            Assert.AreEqual(cat.Cell, dog.PursuitCell);

            var dogDetoured = false;
            var catDetoured = false;
            var turns = 0;
            while (!GridStep.IsAdjacent(dog.Cell, cat.Cell) && turns < 40)
            {
                var dogBefore = dog.Cell;
                var catBefore = cat.Cell;
                var dogTarget = CreatureMoves.CanSee(dog, cat) ? cat.Cell : dog.PursuitCell;
                var catTarget = cat.FleeCell;

                cat.TakeTurn(session, player, new FixedRng(), others);
                dog.TakeTurn(session, player, new FixedRng(), others);

                Assert.IsFalse(world.IsWall(dog.Cell));
                Assert.IsFalse(world.IsWall(cat.Cell));

                if (dogTarget != null
                    && GridStep.Chebyshev(dog.Cell, dogTarget.Value) >= GridStep.Chebyshev(dogBefore, dogTarget.Value)
                    && dog.Cell != dogBefore)
                {
                    dogDetoured = true;
                }

                if (catTarget != null
                    && GridStep.Chebyshev(cat.Cell, catTarget.Value) >= GridStep.Chebyshev(catBefore, catTarget.Value)
                    && cat.Cell != catBefore)
                {
                    catDetoured = true;
                }

                if (CreatureMoves.CanSee(dog, cat))
                {
                    Assert.AreEqual(cat.Cell, dog.PursuitCell);
                }

                turns++;
            }

            Assert.IsTrue(dogDetoured || catDetoured, "At least one agent should take a non-decreasing step toward its goal (wall forced detour).");
            Assert.IsTrue(GridStep.IsAdjacent(dog.Cell, cat.Cell), "Dog speed 2 should still catch the cat in the maze.");
        }

        [Test]
        public void WallTwoDoors_NearDot_DogTakesFarSafeDoor([Values(typeof(Lava), typeof(Acid), typeof(Poison))] System.Type dotType)
        {
            // 10×10: vertical wall splits dog (west) and cat (east). Near doorway has a DoT puddle;
            // far doorway is clear — dog must detour through the safe opening.
            var world = World.CreateGrass(10);
            for (var y = 0; y < 10; y++)
            {
                world.SetTerrain(new Vector2Int(5, y), TerrainType.Wall);
            }

            var nearDoor = new Vector2Int(5, 5);
            var farDoor = new Vector2Int(5, 0);
            world.SetTerrain(nearDoor, TerrainType.Grass);
            world.SetTerrain(farDoor, TerrainType.Grass);
            var dot = (Substance)System.Activator.CreateInstance(dotType);
            Assert.IsInstanceOf<IDamageOverTime>(dot);
            Assert.IsTrue(world.Floor.TryPlacePuddle(nearDoor, dot));

            var player = Spawn<Slime>(new Vector2Int(9, 0));
            var dog = Spawn<Dog>(new Vector2Int(3, 5));
            var cat = Spawn<Cat>(new Vector2Int(8, 5));
            var session = Occupied(world, player, dog, cat);
            var others = new List<Creature> { dog, cat };

            Assert.AreEqual(5, GridStep.Chebyshev(dog.Cell, cat.Cell));
            Assert.IsFalse(CreatureMoves.CanSee(dog, player));
            Assert.IsTrue(CreatureMoves.CanSee(dog, cat));
            Assert.IsFalse(CreatureMoves.IsBlocked(session, dog.Cell, nearDoor), "DoT puddles are traps, not hard blocks.");
            Assert.IsFalse(CreatureMoves.IsBlocked(session, dog.Cell, farDoor));
            Assert.AreEqual(CreatureIntent.Chase, CreatureBrain.Decide(dog, player, session, new FixedRng(), others));

            var goals = new List<Vector2Int>();
            for (var y = -1; y <= 1; y++)
            {
                for (var x = -1; x <= 1; x++)
                {
                    if (x == 0 && y == 0)
                    {
                        continue;
                    }

                    var cell = cat.Cell + new Vector2Int(x, y);
                    if (world.IsTerrainWalkable(cell)
                        && cell != player.Cell
                        && !session.IsOccupied(cell))
                    {
                        goals.Add(cell);
                    }
                }
            }

            var path = GridPath.Find(
                world,
                dog.Cell,
                goals,
                cell => CreatureMoves.IsBlocked(session, dog.Cell, cell),
                dog);
            Assert.IsNotNull(path);
            Assert.IsTrue(path.Contains(farDoor), "Soft FloorPriority must prefer the safe doorway.");
            Assert.IsFalse(path.Contains(nearDoor), "Path must not prefer the DoT doorway when a safe route exists.");

            var turns = 0;
            while (!GridStep.IsAdjacent(dog.Cell, cat.Cell) && turns < 30)
            {
                dog.TakeTurn(session, player, new FixedRng(), others);
                Assert.AreNotEqual(nearDoor, dog.Cell, "Dog must prefer the safe doorway when both exist.");
                turns++;
            }

            Assert.IsTrue(GridStep.IsAdjacent(dog.Cell, cat.Cell), "Dog should still catch the cat via the far door.");
            Assert.IsNotNull(session.World.Floor.GetPuddle(nearDoor));
            Assert.IsNull(dog.FindStatus<Burning>());
            Assert.IsNull(dog.FindStatus<Poisoned>());
            Assert.IsNull(dog.FindStatus<Corroding>());
        }

        [Test]
        public void OnlyDotDoor_DogWalksThroughTrap_GetsDebuff([Values(typeof(Lava), typeof(Acid), typeof(Poison))] System.Type dotType)
        {
            // Single doorway is a DoT puddle — dog must walk through (trap) and receive the status.
            var world = World.CreateGrass(10);
            for (var y = 0; y < 10; y++)
            {
                world.SetTerrain(new Vector2Int(5, y), TerrainType.Wall);
            }

            var door = new Vector2Int(5, 5);
            world.SetTerrain(door, TerrainType.Grass);
            var dot = (Substance)System.Activator.CreateInstance(dotType);
            Assert.IsInstanceOf<IDamageOverTime>(dot);
            Assert.IsTrue(world.Floor.TryPlacePuddle(door, dot));

            var player = Spawn<Slime>(new Vector2Int(9, 0));
            var dog = Spawn<Dog>(new Vector2Int(3, 5));
            var cat = Spawn<Cat>(new Vector2Int(8, 5));
            var session = Occupied(world, player, dog, cat);
            var others = new List<Creature> { dog, cat };

            Assert.IsFalse(CreatureMoves.IsBlocked(session, dog.Cell, door));
            Assert.AreEqual(CreatureIntent.Chase, CreatureBrain.Decide(dog, player, session, new FixedRng(), others));

            var turns = 0;
            var steppedOnDoor = false;
            while (!GridStep.IsAdjacent(dog.Cell, cat.Cell) && turns < 30)
            {
                dog.TakeTurn(session, player, new FixedRng(), others);
                if (dog.Cell == door)
                {
                    steppedOnDoor = true;
                }

                turns++;
            }

            Assert.IsTrue(steppedOnDoor, "Only path is through the DoT doorway.");
            Assert.IsTrue(GridStep.IsAdjacent(dog.Cell, cat.Cell));
            Assert.IsNull(session.World.Floor.GetPuddle(door), "Non-slime consumes the puddle on step.");

            if (dot is Lava)
            {
                Assert.IsNotNull(dog.FindStatus<Burning>());
            }
            else if (dot is Acid)
            {
                Assert.IsNotNull(dog.FindStatus<Corroding>());
            }
            else
            {
                Assert.IsNotNull(dog.FindStatus<Poisoned>());
            }
        }

        [Test]
        public void Cat_HuntingRat_SwitchesToFlee_WhenDogAppears()
        {
            // Cat is mid-hunt on a rat; dog enters vision → FearsDogs beats HatesRats.
            var world = World.CreateGrass(10);
            var player = Spawn<Slime>(new Vector2Int(0, 0));
            var cat = Spawn<Cat>(new Vector2Int(6, 5));
            var rat = Spawn<Rat>(new Vector2Int(8, 5));
            var session = Occupied(world, player, cat, rat);
            var others = new List<Creature> { cat, rat };

            Assert.Greater(GridStep.Chebyshev(cat.Cell, player.Cell), cat.VisionRange);
            Assert.IsFalse(CreatureMoves.CanSee(cat, player));
            Assert.AreEqual(CreatureIntent.Chase, CreatureBrain.Decide(cat, player, session, new FixedRng(), others));

            var beforeRat = GridStep.Chebyshev(cat.Cell, rat.Cell);
            cat.TakeTurn(session, player, new FixedRng(), others);
            Assert.Less(GridStep.Chebyshev(cat.Cell, rat.Cell), beforeRat);
            Assert.AreEqual(rat.Cell, cat.PursuitCell);

            var dog = Spawn<Dog>(new Vector2Int(6, 7));
            session = Occupied(world, player, cat, rat, dog);
            others.Add(dog);

            Assert.IsTrue(CreatureMoves.CanSee(cat, dog));
            Assert.AreEqual(dog, CreatureHunt.FindPredator(cat, others));
            Assert.AreEqual(rat, CreatureHunt.FindPrey(cat, others));
            Assert.AreEqual(CreatureIntent.Flee, CreatureBrain.Decide(cat, player, session, new FixedRng(), others));

            var ratHp = rat.HitPoints;
            var beforeDog = GridStep.Chebyshev(cat.Cell, dog.Cell);
            cat.TakeTurn(session, player, new FixedRng(), others);
            Assert.AreEqual(ratHp, rat.HitPoints, "Cat must abandon the rat hunt when the dog appears.");
            Assert.Greater(GridStep.Chebyshev(cat.Cell, dog.Cell), beforeDog);
            Assert.IsNotNull(cat.FleeCell);
        }

        /// <summary>
        /// Simple 10×10 maze: walls force a long route between (1,1) and (8,8).
        /// '#' = wall, '.' = grass. Border walls + inner baffles.
        /// </summary>
        private static World BuildMaze10()
        {
            var world = World.CreateGrass(10);
            string[] rows =
            {
                "##########",
                "#........#",
                "#.######.#",
                "#.#....#.#",
                "#.#.##.#.#",
                "#.#.#..#.#",
                "#.#.####.#",
                "#.#......#",
                "#.########",
                "##########",
            };

            // rows[0] is y=9 (top) if we draw visually; map so rows[y] is world y from top.
            for (var row = 0; row < 10; row++)
            {
                var y = 9 - row;
                for (var x = 0; x < 10; x++)
                {
                    if (rows[row][x] == '#')
                    {
                        world.SetTerrain(new Vector2Int(x, y), TerrainType.Wall);
                    }
                }
            }

            // Ensure spawns are open.
            world.SetTerrain(new Vector2Int(1, 1), TerrainType.Grass);
            world.SetTerrain(new Vector2Int(5, 5), TerrainType.Grass);
            return world;
        }

        private static bool StraightLineHitsWall(World world, Vector2Int from, Vector2Int to)
        {
            var current = from;
            while (current != to)
            {
                var dx = to.x - current.x;
                var dy = to.y - current.y;
                if (dx != 0)
                {
                    current.x += dx > 0 ? 1 : -1;
                }

                if (dy != 0)
                {
                    current.y += dy > 0 ? 1 : -1;
                }

                if (current != to && world.IsWall(current))
                {
                    return true;
                }
            }

            return false;
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
                    case Cat:
                        Cat.FillStarting(creature.Volume);
                        break;
                    case Dog:
                        Dog.FillStarting(creature.Volume);
                        break;
                    case Rat:
                        Rat.FillStarting(creature.Volume);
                        break;
                }
            }

            switch (creature)
            {
                case Slime:
                    creature.SetMaxHitPoints(0);
                    creature.RefreshVolumeStatuses();
                    break;
                case Cat:
                    creature.SetMaxHitPoints(5);
                    break;
                case Dog:
                    creature.SetSpeed(2);
                    creature.SetMaxHitPoints(10);
                    break;
                case Rat:
                    creature.SetMaxHitPoints(3);
                    break;
            }

            creature.EnsureInnateTraits();
            return creature;
        }

        private static GameSession Occupied(World world, Creature player, params Creature[] others)
        {
            var cells = new Vector2Int[others.Length];
            for (var i = 0; i < others.Length; i++)
            {
                cells[i] = others[i].Cell;
            }

            return new GameSession(world, player.Cell, cells);
        }
    }
}
