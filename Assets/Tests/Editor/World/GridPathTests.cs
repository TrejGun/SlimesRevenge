using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SlimesRevenge.Tests
{
    public class GridPathTests
    {
        [Test]
        public void WallOfThreeEnemies_ForcesADetourAroundTheBlock()
        {
            var world = World.CreateGrass();
            var slime = new Vector2Int(1, 5);
            var cats = new[] { new Vector2Int(2, 6), new Vector2Int(2, 5), new Vector2Int(2, 4) };
            var dog = new Vector2Int(3, 5);

            Assert.AreEqual(2, GridStep.Chebyshev(slime, dog));

            var blocked = new HashSet<Vector2Int>(cats) { slime };
            var goals = Approach(world, slime, blocked);
            var path = GridPath.Find(world, dog, goals, blocked.Contains);

            Assert.IsNotNull(path);
            Assert.GreaterOrEqual(
                path.Count,
                3,
                "Direct Chebyshev jumps through the cats are not a path."
            );
            foreach (var cat in cats)
            {
                Assert.IsFalse(path.Contains(cat));
            }

            var afterTwoSteps = path[1];
            Assert.GreaterOrEqual(GridStep.Chebyshev(afterTwoSteps, slime), 2);
            Assert.IsFalse(
                GridStep.IsAdjacent(afterTwoSteps, slime),
                "The dog must go around the cats, not jump through them."
            );
            Assert.IsTrue(GridStep.IsAdjacent(path[path.Count - 1], slime));
        }

        [Test]
        public void WallOfFiveEnemies_StillHasADetourAroundTheBlock()
        {
            var world = World.CreateGrass();
            var slime = new Vector2Int(1, 5);
            var cats = new[]
            {
                new Vector2Int(2, 7),
                new Vector2Int(2, 6),
                new Vector2Int(2, 5),
                new Vector2Int(2, 4),
                new Vector2Int(2, 3),
            };
            var dog = new Vector2Int(3, 5);

            Assert.AreEqual(2, GridStep.Chebyshev(slime, dog));

            var blocked = new HashSet<Vector2Int>(cats) { slime };
            var goals = Approach(world, slime, blocked);
            var path = GridPath.Find(world, dog, goals, blocked.Contains);

            Assert.IsNotNull(path);
            Assert.GreaterOrEqual(path.Count, 3);
            foreach (var cat in cats)
            {
                Assert.IsFalse(path.Contains(cat));
            }

            Assert.IsFalse(GridStep.IsAdjacent(path[1], slime));
            Assert.IsTrue(GridStep.IsAdjacent(path[path.Count - 1], slime));
        }

        [Test]
        public void OpenPlane_FindsAPathTwentyFiveCellsAway()
        {
            var world = World.CreateGrass(30);
            var slime = new Vector2Int(25, 15);
            var dog = new Vector2Int(0, 15);
            var blocked = new HashSet<Vector2Int> { slime };
            var goals = Approach(world, slime, blocked);
            var path = GridPath.Find(world, dog, goals, blocked.Contains);

            Assert.IsNotNull(path);
            Assert.GreaterOrEqual(path.Count, 24);
            Assert.AreEqual(2, GridStep.Chebyshev(dog, path[1]));
            Assert.IsTrue(GridStep.IsAdjacent(path[path.Count - 1], slime));
        }

        [Test]
        public void DuelDist5_Speed2Walk_LandsAtChebyshev3()
        {
            var world = new World(RunConfig.DuelWidth, RunConfig.DuelHeight, TerrainType.Grass);
            var slime = new Vector2Int(6, 2);
            var dog = new Vector2Int(11, 2);
            var blocked = new HashSet<Vector2Int> { slime };
            var goals = Approach(world, slime, blocked);

            Assert.IsTrue(GridPath.TryWalk(world, dog, goals, blocked.Contains, 2, out var dest));
            Assert.AreEqual(3, GridStep.Chebyshev(dest, slime));
            Assert.IsTrue(GridStep.IsAdjacent(dog, dest) || GridStep.Chebyshev(dog, dest) == 2);

            var path = GridPath.Find(world, dog, goals, blocked.Contains);
            Assert.IsNotNull(path);
            Assert.GreaterOrEqual(path.Count, 2);
            Assert.IsTrue(GridStep.IsAdjacent(dog, path[0]));
            Assert.IsTrue(GridStep.IsAdjacent(path[0], path[1]));
            Assert.AreEqual(2, GridStep.Chebyshev(dog, path[1]));
        }

        private static List<Vector2Int> Approach(
            World world,
            Vector2Int player,
            HashSet<Vector2Int> blocked
        )
        {
            var cells = new List<Vector2Int>();
            for (var y = -1; y <= 1; y++)
            {
                for (var x = -1; x <= 1; x++)
                {
                    if (x == 0 && y == 0)
                    {
                        continue;
                    }

                    var cell = player + new Vector2Int(x, y);
                    if (world.Contains(cell) && !blocked.Contains(cell))
                    {
                        cells.Add(cell);
                    }
                }
            }

            return cells;
        }
    }
}
