using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SlimesRevenge.Tests
{
    public class HuntSimulationTests
    {
        [Test]
        public void AnyDistinctCellsOnTenByTen_SlimeCanForceAdjacency()
        {
            var world = World.CreateGrass();
            Assert.AreEqual(10, world.Width);
            Assert.AreEqual(10, world.Height);

            var win = ForcedAdjacent(world);
            for (var sy = 0; sy < world.Height; sy++)
            {
                for (var sx = 0; sx < world.Width; sx++)
                {
                    for (var ry = 0; ry < world.Height; ry++)
                    {
                        for (var rx = 0; rx < world.Width; rx++)
                        {
                            var slime = new Vector2Int(sx, sy);
                            var rat = new Vector2Int(rx, ry);
                            if (slime == rat)
                            {
                                continue;
                            }

                            Assert.IsTrue(
                                win.Contains((slime, rat)),
                                $"No finite hunt from slime {slime} to rat {rat}."
                            );
                        }
                    }
                }
            }
        }

        private static HashSet<(Vector2Int slime, Vector2Int rat)> ForcedAdjacent(World world)
        {
            var win = new HashSet<(Vector2Int, Vector2Int)>();
            for (var sy = 0; sy < world.Height; sy++)
            {
                for (var sx = 0; sx < world.Width; sx++)
                {
                    for (var ry = 0; ry < world.Height; ry++)
                    {
                        for (var rx = 0; rx < world.Width; rx++)
                        {
                            var slime = new Vector2Int(sx, sy);
                            var rat = new Vector2Int(rx, ry);
                            if (slime != rat && GridStep.IsAdjacent(slime, rat))
                            {
                                win.Add((slime, rat));
                            }
                        }
                    }
                }
            }

            var changed = true;
            while (changed)
            {
                changed = false;
                for (var sy = 0; sy < world.Height; sy++)
                {
                    for (var sx = 0; sx < world.Width; sx++)
                    {
                        for (var ry = 0; ry < world.Height; ry++)
                        {
                            for (var rx = 0; rx < world.Width; rx++)
                            {
                                var slime = new Vector2Int(sx, sy);
                                var rat = new Vector2Int(rx, ry);
                                if (slime == rat || win.Contains((slime, rat)))
                                {
                                    continue;
                                }

                                if (!CanForce(world, win, slime, rat))
                                {
                                    continue;
                                }

                                win.Add((slime, rat));
                                changed = true;
                            }
                        }
                    }
                }
            }

            return win;
        }

        private static bool CanForce(
            World world,
            HashSet<(Vector2Int, Vector2Int)> win,
            Vector2Int slime,
            Vector2Int rat
        )
        {
            foreach (var next in Neighbors(world, slime))
            {
                if (next == rat)
                {
                    continue;
                }

                if (AllRepliesWin(world, win, next, rat))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool AllRepliesWin(
            World world,
            HashSet<(Vector2Int, Vector2Int)> win,
            Vector2Int slime,
            Vector2Int rat
        )
        {
            var replies = 0;
            foreach (var flee in Neighbors(world, rat))
            {
                if (
                    flee == slime
                    || GridStep.Chebyshev(flee, slime) <= GridStep.Chebyshev(rat, slime)
                )
                {
                    continue;
                }

                replies++;
                if (!win.Contains((slime, flee)))
                {
                    return false;
                }
            }

            return replies == 0 ? win.Contains((slime, rat)) : true;
        }

        private static IEnumerable<Vector2Int> Neighbors(World world, Vector2Int from)
        {
            for (var y = -1; y <= 1; y++)
            {
                for (var x = -1; x <= 1; x++)
                {
                    if (x == 0 && y == 0)
                    {
                        continue;
                    }

                    var to = from + new Vector2Int(x, y);
                    if (world.Contains(to))
                    {
                        yield return to;
                    }
                }
            }
        }
    }
}
