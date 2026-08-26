using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SlimesRevenge.Tests
{
    /// <summary>Paths calculated through A* Pathfinding Project Free (<see cref="GridPath"/>).</summary>
    public class AstarGridPathTests
    {
        [Test]
        public void Astar_FindsPathOnOpenGrass()
        {
            var world = World.CreateGrass(10);
            var start = new Vector2Int(1, 1);
            var goal = new Vector2Int(8, 8);
            var blocked = new HashSet<Vector2Int>();
            var path = GridPath.Find(world, start, new[] { goal }, blocked.Contains);

            Assert.IsNotNull(path);
            Assert.Greater(path.Count, 0);
            Assert.AreEqual(goal, path[path.Count - 1]);
        }

        [Test]
        public void Astar_GoesAroundWall_NotThroughIt()
        {
            var world = World.CreateGrass(10);
            for (var y = 0; y < 10; y++)
            {
                world.SetTerrain(new Vector2Int(4, y), TerrainType.Wall);
            }

            world.SetTerrain(new Vector2Int(4, 9), TerrainType.Grass);

            var start = new Vector2Int(1, 5);
            var goal = new Vector2Int(7, 5);
            bool Blocked(Vector2Int cell) => world.IsWall(cell);
            var path = GridPath.Find(world, start, new[] { goal }, Blocked);

            Assert.IsNotNull(path);
            Assert.IsFalse(path.Exists(world.IsWall));
            Assert.IsTrue(path.Contains(new Vector2Int(4, 9)), "Must use the open doorway.");
            Assert.AreEqual(goal, path[path.Count - 1]);
        }

        [Test]
        public void Astar_TreatsPuddleAsWalkable()
        {
            var world = World.CreateGrass(8);
            world.Floor.TryPlacePuddle(new Vector2Int(3, 3), new Oil());
            for (var y = 0; y < 8; y++)
            {
                if (y == 3)
                {
                    continue;
                }

                world.SetTerrain(new Vector2Int(3, y), TerrainType.Wall);
            }

            var start = new Vector2Int(1, 3);
            var goal = new Vector2Int(5, 3);
            bool Blocked(Vector2Int cell) => world.IsWall(cell);
            var path = GridPath.Find(world, start, new[] { goal }, Blocked);

            Assert.IsNotNull(path);
            Assert.IsTrue(path.Contains(new Vector2Int(3, 3)));
            Assert.AreEqual(goal, path[path.Count - 1]);
        }

        [Test]
        public void Astar_EqualLengthDoors_PrefersLowerFloorPriority()
        {
            // Wall with two equal-length doorways: lava (100) vs water (10). A* must pick water.
            var world = World.CreateGrass(10);
            for (var y = 0; y < 10; y++)
            {
                world.SetTerrain(new Vector2Int(4, y), TerrainType.Wall);
            }

            var lavaDoor = new Vector2Int(4, 7);
            var waterDoor = new Vector2Int(4, 1);
            world.SetTerrain(lavaDoor, TerrainType.Grass);
            world.SetTerrain(waterDoor, TerrainType.Grass);
            Assert.IsTrue(world.Floor.TryPlacePuddle(lavaDoor, new Lava()));
            Assert.IsTrue(world.Floor.TryPlacePuddle(waterDoor, new Water()));
            Assert.Greater(new Lava().FloorPriority, new Water().FloorPriority);

            var start = new Vector2Int(1, 4);
            var goal = new Vector2Int(7, 4);
            Assert.AreEqual(
                GridStep.Chebyshev(start, lavaDoor) + GridStep.Chebyshev(lavaDoor, goal),
                GridStep.Chebyshev(start, waterDoor) + GridStep.Chebyshev(waterDoor, goal));
            bool Blocked(Vector2Int cell) => world.IsWall(cell);
            var path = GridPath.Find(world, start, new[] { goal }, Blocked);

            Assert.IsNotNull(path);
            Assert.IsTrue(path.Contains(waterDoor), "Lower FloorPriority (water) must win over lava.");
            Assert.IsFalse(path.Contains(lavaDoor));
        }

        [Test]
        public void FloorPriority_RanksSpillableSubstances()
        {
            Assert.AreEqual(Substance.MildFloorPriority, new Water().FloorPriority);
            Assert.AreEqual(Substance.MildFloorPriority, new Blood().FloorPriority);
            Assert.AreEqual(Substance.MildFloorPriority, new Oil().FloorPriority);
            Assert.Greater(new Water().FloorPriority, 0);
            Assert.AreEqual(Substance.DamageOverTimeFloorPriority, new Lava().FloorPriority);
            Assert.AreEqual(new Lava().FloorPriority, new Acid().FloorPriority);
            Assert.AreEqual(new Acid().FloorPriority, new Poison().FloorPriority);
            Assert.Greater(new Lava().FloorPriority, new Oil().FloorPriority);
            Assert.IsInstanceOf<IDamageOverTime>(new Lava());
            Assert.IsInstanceOf<IDamageOverTime>(new Acid());
            Assert.IsInstanceOf<IDamageOverTime>(new Poison());
            Assert.IsNotInstanceOf<IDamageOverTime>(new Oil());
            Assert.IsNotInstanceOf<IDamageOverTime>(new Water());
            Assert.IsNotInstanceOf<IDamageOverTime>(new Blood());
        }

        [Test]
        public void Poison_FloorPriorityFor_PoisonousWalkerIsZero()
        {
            var poisonous = new GameObject("Poisonous").AddComponent<Scorpion>();
            poisonous.PlaceOn(Vector2Int.zero);
            if (poisonous.FindStatus<Poisonous>() == null)
            {
                poisonous.AddStatus(new Poisonous());
            }

            try
            {
                Assert.AreEqual(0, new Poison().FloorPriorityFor(poisonous));
                Assert.AreEqual(Substance.DamageOverTimeFloorPriority, new Poison().FloorPriorityFor(null));
                Assert.AreEqual(Substance.DamageOverTimeFloorPriority, new Lava().FloorPriorityFor(poisonous));
            }
            finally
            {
                Object.DestroyImmediate(poisonous.gameObject);
            }
        }

        [Test]
        public void Water_FloorPriorityFor_CatWithFearsWater_MatchesDot()
        {
            var cat = new GameObject("Cat").AddComponent<Cat>();
            cat.PlaceOn(Vector2Int.zero);
            if (cat.FindStatus<FearsWater>() == null)
            {
                cat.AddStatus(new FearsWater());
            }

            try
            {
                Assert.AreEqual(Substance.DamageOverTimeFloorPriority, new Water().FloorPriorityFor(cat));
                Assert.AreEqual(new Lava().FloorPriority, new Water().FloorPriorityFor(cat));
                Assert.AreEqual(Substance.MildFloorPriority, new Water().FloorPriorityFor(null));
                Assert.AreEqual(Substance.MildFloorPriority, new Water().FloorPriority);
            }
            finally
            {
                Object.DestroyImmediate(cat.gameObject);
            }
        }

        [Test]
        public void Astar_TryWalk_RespectsMaxStep()
        {
            var world = World.CreateGrass(12);
            var start = new Vector2Int(0, 0);
            var goal = new Vector2Int(10, 0);
            Assert.IsTrue(GridPath.TryWalk(world, start, new[] { goal }, _ => false, maxStep: 2, out var dest));
            Assert.AreEqual(2, GridStep.Chebyshev(start, dest));
        }

        [Test]
        public void Astar_NoPath_WhenFullyWalledOff()
        {
            var world = World.CreateGrass(6);
            for (var y = 0; y < 6; y++)
            {
                world.SetTerrain(new Vector2Int(2, y), TerrainType.Wall);
            }

            var path = GridPath.Find(world, new Vector2Int(0, 3), new[] { new Vector2Int(4, 3) }, world.IsWall);
            Assert.IsNull(path);
        }
    }
}
