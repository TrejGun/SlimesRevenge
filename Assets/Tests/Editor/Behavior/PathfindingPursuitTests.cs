using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SlimesRevenge.Tests
{
    public class PathfindingPursuitTests
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
        public void Aggressive_PathsAroundWall_EvenIfFirstStepIncreasesDistance_PuddleIsWalkable()
        {
            // Dog must not see the slime (else Aggressive chases slime). Wall at x=7 with puddle doorway.
            var world = World.CreateGrass(12);
            for (var y = 0; y < world.Height; y++)
            {
                world.SetTerrain(new Vector2Int(7, y), TerrainType.Wall);
            }

            world.SetTerrain(new Vector2Int(7, 8), TerrainType.Grass);
            world.Floor.TryPlacePuddle(new Vector2Int(7, 8), new Oil());

            var player = Spawn<Slime>(new Vector2Int(0, 0));
            var dog = TestCreatures.Aggressive(spawned, new Vector2Int(5, 8));
            var cat = TestCreatures.Passive(spawned, new Vector2Int(10, 8));
            var session = Occupied(world, player, dog, cat);
            var others = new List<Creature> { dog, cat };

            Assert.IsFalse(CreatureMoves.CanSee(dog, player));
            Assert.IsTrue(CreatureMoves.CanSee(dog, cat));
            Assert.AreEqual(
                CreatureIntent.Chase,
                CreatureBrain.Decide(dog, player, session, new FixedRng(), others)
            );

            dog.TakeTurn(session, player, new FixedRng(), others);
            Assert.AreEqual(cat.Cell, dog.PursuitCell);
            Assert.AreEqual(Creature.PursuitMemoryTurns, dog.PursuitMemoryLeft);
            Assert.AreNotEqual(new Vector2Int(5, 8), dog.Cell);
            Assert.IsFalse(world.IsWall(dog.Cell));

            var turns = 0;
            while (!GridStep.IsAdjacent(dog.Cell, cat.Cell) && turns < 20)
            {
                dog.TakeTurn(session, player, new FixedRng(), others);
                turns++;
            }

            Assert.IsTrue(GridStep.IsAdjacent(dog.Cell, cat.Cell));
            Assert.IsNull(
                session.World.Floor.GetPuddle(new Vector2Int(7, 8)),
                "Dog consumes the oil puddle when stepping on it."
            );
            Assert.IsNotNull(dog.FindStatus<Flammable>());
            Assert.IsNotNull(dog.FindStatus<Instability>());
        }

        [Test]
        public void DotPuddle_IsWalkableTrap_HigherFloorPriorityThanNonDot(
            [Values(typeof(Lava), typeof(Acid), typeof(Poison))] System.Type dotType,
            [Values(typeof(Oil), typeof(Water), typeof(Blood))] System.Type safeType
        )
        {
            var world = World.CreateGrass(8);
            var dotCell = new Vector2Int(3, 3);
            var safeCell = new Vector2Int(4, 3);
            var dot = (Substance)System.Activator.CreateInstance(dotType);
            var safe = (Substance)System.Activator.CreateInstance(safeType);
            Assert.IsInstanceOf<IDamageOverTime>(dot);
            Assert.IsNotInstanceOf<IDamageOverTime>(safe);
            Assert.IsTrue(world.Floor.TryPlacePuddle(dotCell, dot));
            Assert.IsTrue(world.Floor.TryPlacePuddle(safeCell, safe));

            var player = Spawn<Slime>(new Vector2Int(0, 0));
            var dog = TestCreatures.Aggressive(spawned, new Vector2Int(1, 3));
            var session = Occupied(world, player, dog);

            Assert.IsFalse(CreatureMoves.IsBlocked(session, dog.Cell, dotCell));
            Assert.IsFalse(CreatureMoves.IsBlocked(session, dog.Cell, safeCell));
            Assert.Greater(dot.FloorPriority, safe.FloorPriority);
        }

        [Test]
        public void Poisonous_ZeroFloorPriorityOnPoison_AndNoPoisonedFromPuddle()
        {
            var world = World.CreateGrass(8);
            var poisonCell = new Vector2Int(2, 2);
            Assert.IsTrue(world.Floor.TryPlacePuddle(poisonCell, new Poison()));

            var player = Spawn<Slime>(new Vector2Int(0, 0));
            var scorpion = Spawn<Scorpion>(new Vector2Int(1, 2));
            var dog = TestCreatures.Aggressive(spawned, new Vector2Int(1, 4));
            var session = Occupied(world, player, scorpion, dog);

            Assert.IsNotNull(scorpion.FindStatus<Poisonous>());
            Assert.AreEqual(0, new Poison().FloorPriorityFor(scorpion));
            Assert.AreEqual(new Poison().FloorPriority, new Poison().FloorPriorityFor(dog));
            Assert.Greater(new Poison().FloorPriorityFor(dog), 0);

            var goals = new[] { new Vector2Int(4, 2) };
            var scorpioPath = GridPath.Find(
                world,
                scorpion.Cell,
                goals,
                cell => CreatureMoves.IsBlocked(session, scorpion.Cell, cell),
                scorpion
            );
            Assert.IsNotNull(scorpioPath);
            // Poison costs 0 for poisonous walkers — must not be treated as a hard avoid.
            Assert.LessOrEqual(
                scorpioPath.Count,
                GridStep.Chebyshev(scorpion.Cell, goals[0]) + 1,
                "Poison must not add meaningful path cost for poisonous walkers."
            );

            var hp = scorpion.HitPoints;
            Assert.IsTrue(session.TryMoveOccupant(scorpion.Cell, poisonCell));
            scorpion.PlaceOn(poisonCell);
            world.Floor.ApplyContact(scorpion);

            Assert.AreEqual(hp, scorpion.HitPoints);
            Assert.IsNull(scorpion.FindStatus<Poisoned>());
            Assert.IsNull(world.Floor.GetPuddle(poisonCell));
        }

        [Test]
        public void Aggressive_DetourAroundLongWall_FirstStepMayIncreaseChebyshev()
        {
            var world = World.CreateGrass(14);
            // Short enough that the dog can keep the cat in VisionRange 5 while skirting the end.
            for (var x = 3; x <= 7; x++)
            {
                world.SetTerrain(new Vector2Int(x, 6), TerrainType.Wall);
            }

            var player = Spawn<Slime>(new Vector2Int(13, 13));
            var dog = TestCreatures.Aggressive(spawned, new Vector2Int(5, 8));
            var cat = TestCreatures.Passive(spawned, new Vector2Int(5, 4));
            var session = Occupied(world, player, dog, cat);
            var others = new List<Creature> { dog, cat };

            Assert.IsFalse(CreatureMoves.CanSee(dog, player));
            Assert.AreEqual(
                CreatureIntent.Chase,
                CreatureBrain.Decide(dog, player, session, new FixedRng(), others)
            );

            var turns = 0;
            while (!GridStep.IsAdjacent(dog.Cell, cat.Cell) && turns < 40)
            {
                dog.TakeTurn(session, player, new FixedRng(), others);
                turns++;
            }

            Assert.IsTrue(
                GridStep.IsAdjacent(dog.Cell, cat.Cell),
                $"Dog stuck at {dog.Cell} after {turns} turns; cat at {cat.Cell}."
            );
        }

        [Test]
        public void Aggressive_RemembersLastKnownCell_ForThreeTurns_ThenGivesUp()
        {
            var world = World.CreateGrass(14);
            var player = Spawn<Slime>(new Vector2Int(0, 0));
            var dog = TestCreatures.Aggressive(spawned, new Vector2Int(12, 12));
            var cat = TestCreatures.Passive(spawned, new Vector2Int(12, 10));
            var session = Occupied(world, player, dog, cat);
            var others = new List<Creature> { dog, cat };

            dog.TakeTurn(session, player, new FixedRng(), others);
            Assert.IsNotNull(dog.PursuitCell);
            Assert.AreEqual(3, dog.PursuitMemoryLeft);
            var remembered = dog.PursuitCell.Value;

            // Cat leaves vision; dog is teleported far so it cannot reach the memory cell within 3 turns.
            // VisionRange 5: keep Chebyshev > 5 between dog and cat after teleport.
            session.TryMoveOccupant(cat.Cell, new Vector2Int(1, 0));
            cat.PlaceOn(new Vector2Int(1, 0));
            session.TryMoveOccupant(dog.Cell, new Vector2Int(0, 13));
            dog.PlaceOn(new Vector2Int(0, 13));
            dog.RememberPursuit(remembered);
            Assert.IsFalse(CreatureMoves.CanSee(dog, cat));
            Assert.IsFalse(CreatureMoves.CanSee(dog, player));
            Assert.Greater(GridStep.Chebyshev(dog.Cell, remembered), 6);

            for (var i = 0; i < 3; i++)
            {
                Assert.IsNotNull(dog.PursuitCell, $"memory should remain on turn {i}");
                Assert.AreEqual(
                    CreatureIntent.Chase,
                    CreatureBrain.Decide(dog, player, session, new FixedRng(), others)
                );
                dog.TakeTurn(session, player, new FixedRng(), others);
            }

            Assert.IsNull(dog.PursuitCell);
            Assert.AreEqual(
                CreatureIntent.Idle,
                CreatureBrain.Decide(dog, player, session, new FixedRng(), others)
            );
        }

        [Test]
        public void Aggressive_ReachesLastKnownCell_WithoutSeeingPrey_ClearsPursuit()
        {
            var world = World.CreateGrass(14);
            // Keep slime out of dog vision so Chase uses pursuit memory, not the player.
            var player = Spawn<Slime>(new Vector2Int(0, 0));
            var dog = TestCreatures.Aggressive(spawned, new Vector2Int(12, 12));
            var cat = TestCreatures.Passive(spawned, new Vector2Int(12, 11));
            var session = Occupied(world, player, dog, cat);
            var others = new List<Creature> { dog, cat };

            var lastKnown = cat.Cell;
            dog.RememberPursuit(lastKnown);
            session.TryMoveOccupant(cat.Cell, new Vector2Int(0, 13));
            cat.PlaceOn(new Vector2Int(0, 13));
            Assert.IsFalse(CreatureMoves.CanSee(dog, cat));
            Assert.IsFalse(CreatureMoves.CanSee(dog, player));

            for (var i = 0; i < 8 && dog.PursuitCell != null; i++)
            {
                dog.TakeTurn(session, player, new FixedRng(), others);
            }

            Assert.IsNull(dog.PursuitCell);
            Assert.LessOrEqual(GridStep.Chebyshev(dog.Cell, lastKnown), 1);
        }

        [Test]
        public void Passive_FleesToFarWaypoint_UntilReached()
        {
            var world = World.CreateGrass(12);
            var player = Spawn<Slime>(new Vector2Int(0, 0));
            var dog = TestCreatures.Aggressive(spawned, new Vector2Int(6, 6));
            var cat = TestCreatures.Passive(spawned, new Vector2Int(6, 5));
            var session = Occupied(world, player, dog, cat);
            var others = new List<Creature> { dog, cat };

            Assert.AreEqual(
                CreatureIntent.Flee,
                CreatureBrain.Decide(cat, player, session, new FixedRng(), others)
            );
            cat.TakeTurn(session, player, new FixedRng(), others);
            Assert.IsNotNull(cat.FleeCell);
            var flee = cat.FleeCell.Value;
            Assert.Greater(
                GridStep.Chebyshev(flee, dog.Cell),
                GridStep.Chebyshev(cat.Cell, dog.Cell)
            );

            var turns = 0;
            while (cat.FleeCell != null && cat.Cell != flee && turns < 30)
            {
                // Dog stays put so cat can finish the flee path.
                cat.TakeTurn(session, player, new FixedRng(), others);
                turns++;
            }

            Assert.AreEqual(flee, cat.Cell);
            Assert.IsNull(cat.FleeCell);
        }

        [Test]
        public void Passive_CaughtByHunter_ClearsFleeAndAggroes()
        {
            var world = World.CreateGrass();
            var player = Spawn<Slime>(new Vector2Int(0, 0));
            var dog = TestCreatures.Aggressive(spawned, new Vector2Int(4, 4));
            var cat = TestCreatures.Passive(spawned, new Vector2Int(4, 5));
            var session = Occupied(world, player, dog, cat);
            var others = new List<Creature> { dog, cat };

            cat.TakeTurn(session, player, new FixedRng(), others);
            Assert.IsNotNull(cat.FleeCell);

            // Force adjacency after flee start: dog already adjacent — AfterAct should clear flee + aggro.
            if (!GridStep.IsAdjacent(cat.Cell, dog.Cell))
            {
                // place cat next to dog without path
                session.TryMoveOccupant(cat.Cell, new Vector2Int(5, 4));
                cat.PlaceOn(new Vector2Int(5, 4));
            }

            cat.SetFleeCell(new Vector2Int(9, 9));
            CreatureTurnContext.Push(session, player, new FixedRng(), others);
            try
            {
                CreatureTurnContext.FocusTarget = dog;
                CreatureHunt.AfterAct(cat, CreatureIntent.Flee, dog, others);
            }
            finally
            {
                CreatureTurnContext.Pop();
            }

            Assert.IsNull(cat.FleeCell);
            Assert.IsTrue(cat.Aggroed);
        }

        [Test]
        public void GridPath_WallBlocks_PuddlesDoNot_IncludingDot()
        {
            var world = World.CreateGrass(8);
            world.SetTerrain(new Vector2Int(3, 3), TerrainType.Wall);
            world.Floor.TryPlacePuddle(new Vector2Int(4, 3), new Water());
            world.Floor.TryPlacePuddle(new Vector2Int(5, 3), new Lava());

            var session = new GameSession(
                world,
                new[] { new Vector2Int(0, 0), new Vector2Int(1, 3) }
            );
            Assert.IsTrue(
                CreatureMoves.IsBlocked(session, new Vector2Int(1, 3), new Vector2Int(3, 3))
            );
            Assert.IsFalse(
                CreatureMoves.IsBlocked(session, new Vector2Int(1, 3), new Vector2Int(4, 3))
            );
            Assert.IsFalse(
                CreatureMoves.IsBlocked(session, new Vector2Int(1, 3), new Vector2Int(5, 3))
            );
        }

        private T Spawn<T>(Vector2Int cell)
            where T : Creature
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
                    case Scorpion:
                        Scorpion.FillStarting(creature.Volume);
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
                case Scorpion:
                    creature.SetMaxHitPoints(3);
                    break;
            }

            creature.EnsureInnateTraits();
            return creature;
        }

        private static GameSession Occupied(World world, Creature player, params Creature[] others)
        {
            return SessionFactory.WithControlled(world, player, others);
        }
    }
}
