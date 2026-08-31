using System;
using System.Collections.Generic;
using Pathfinding;
using UnityEngine;

namespace SlimesRevenge
{
    /// <summary>
    /// Grid pathfinding via <b>A* Pathfinding Project Free</b> (Aron Granberg).
    /// Builds a temporary <see cref="GridGraph"/>, marks walls/blockers unwalkable, runs <see cref="ABPath"/>.
    /// Walk steps come from grid-node indices (not <see cref="ABPath.vectorPath"/>), so Speed budget is not wasted on zigzag.
    /// </summary>
    public static class GridPath
    {
        public static bool TryWalk(
            World world,
            Vector2Int from,
            IEnumerable<Vector2Int> goals,
            Func<Vector2Int, bool> blocked,
            int maxStep,
            out Vector2Int destination,
            Creature walker = null,
            Vector2Int? fleeFrom = null
        )
        {
            destination = from;
            if (maxStep < 1)
            {
                return false;
            }

            var path = Find(world, from, goals, blocked, walker, fleeFrom);
            if (path == null || path.Count == 0)
            {
                return false;
            }

            // Walk at most Speed steps along the A* path (path index), never jump to a
            // later cell that is merely within Chebyshev Speed — that teleports through blockers.
            var steps = Mathf.Min(Mathf.Max(1, maxStep), path.Count);
            destination = path[steps - 1];
            if (destination == from || GridStep.Chebyshev(from, destination) < 1)
            {
                destination = from;
                return false;
            }

            // If the prefix zigzags and lands farther than Speed in Chebyshev, back up.
            while (steps > 0 && GridStep.Chebyshev(from, path[steps - 1]) > maxStep)
            {
                steps--;
            }

            if (steps < 1)
            {
                destination = from;
                return false;
            }

            destination = path[steps - 1];
            return GridStep.Chebyshev(from, destination) >= 1;
        }

        public static List<Vector2Int> Find(
            World world,
            Vector2Int start,
            IEnumerable<Vector2Int> goals,
            Func<Vector2Int, bool> blocked,
            Creature walker = null,
            Vector2Int? fleeFrom = null
        )
        {
            if (world == null || blocked == null || goals == null)
            {
                return null;
            }

            var goalSet = new HashSet<Vector2Int>();
            foreach (var goal in goals)
            {
                if (world.Contains(goal) && (goal == start || !blocked(goal)))
                {
                    goalSet.Add(goal);
                }
            }

            if (goalSet.Count == 0 || goalSet.Contains(start))
            {
                return null;
            }

            var host = new GameObject("AstarPathfindingProject.Temp")
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
            var astar = host.AddComponent<AstarPath>();
            EnsureAstarReady(astar);
            try
            {
                var gg = ConfigureGraph(astar, world, start, blocked, walker);
                return ShortestAstarPath(gg, start, goalSet, fleeFrom);
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                if (AstarPath.active == astar)
                {
                    AstarPath.active = null;
                }

                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        private static void EnsureAstarReady(AstarPath astar)
        {
            if (astar.data != null)
            {
                return;
            }

            astar.ConfigureReferencesInternal();
            astar.data.FindGraphTypes();
            astar.data.Awake();
            astar.data.UpdateShortcuts();
        }

        private static GridGraph ConfigureGraph(
            AstarPath astar,
            World world,
            Vector2Int start,
            Func<Vector2Int, bool> blocked,
            Creature walker
        )
        {
            var gg = astar.data.AddGraph(typeof(GridGraph)) as GridGraph;
            // Node centers at integer (x,0,z) so GetNearest(Point(cell)) never sits
            // halfway between two nodes (which snapped chase starts one cell off).
            gg.center = new Vector3(world.Width * 0.5f - 0.5f, 0f, world.Height * 0.5f - 0.5f);
            gg.SetDimensions(world.Width, world.Height, 1f);
            gg.neighbours = NumNeighbours.Eight;
            gg.collision.collisionCheck = false;
            gg.collision.heightCheck = false;
            astar.Scan(gg);

            for (var z = 0; z < world.Height; z++)
            {
                for (var x = 0; x < world.Width; x++)
                {
                    var cell = new Vector2Int(x, z);
                    var node = gg.GetNode(x, z);
                    if (node == null)
                    {
                        continue;
                    }

                    node.Walkable = cell == start || !blocked(cell);
                    if (node.Walkable)
                    {
                        node.Penalty = FloorPenalty(world, cell, walker);
                    }
                }
            }

            for (var z = 0; z < world.Height; z++)
            {
                for (var x = 0; x < world.Width; x++)
                {
                    gg.CalculateConnections(x, z);
                }
            }

            astar.FloodFill();
            return gg;
        }

        private static uint FloorPenalty(World world, Vector2Int cell, Creature walker)
        {
            var puddle = world.Floor.GetPuddle(cell);
            if (puddle?.Substance == null)
            {
                return 0;
            }

            var priority = Mathf.Max(0, puddle.Substance.FloorPriorityFor(walker));
            return (uint)(priority * 1000);
        }

        private static List<Vector2Int> ShortestAstarPath(
            GridGraph gg,
            Vector2Int start,
            HashSet<Vector2Int> goals,
            Vector2Int? fleeFrom
        )
        {
            List<Vector2Int> best = null;
            var bestEarly = -1;
            var bestFleeGain = int.MinValue;
            var startNode = gg.GetNode(start.x, start.y);
            if (startNode == null || !startNode.Walkable)
            {
                return null;
            }

            var startFleeDist = fleeFrom != null ? GridStep.Chebyshev(start, fleeFrom.Value) : 0;

            foreach (var goal in goals)
            {
                var goalNode = gg.GetNode(goal.x, goal.y);
                if (goalNode == null || !goalNode.Walkable)
                {
                    continue;
                }

                // Construct from node positions (integer centers), never half-cell probes.
                var ab = ABPath.Construct(
                    (Vector3)startNode.position,
                    (Vector3)goalNode.position,
                    null
                );
                AstarPath.StartPath(ab);
                ab.BlockUntilCalculated();
                if (ab.error || ab.path == null || ab.path.Count == 0)
                {
                    continue;
                }

                var cells = ToCells(start, ab.path);
                if (
                    cells == null
                    || cells.Count == 0
                    || !IsContiguousFrom(start, cells)
                    || !goals.Contains(cells[cells.Count - 1])
                )
                {
                    continue;
                }

                // Among equal-length routes, prefer early progress: chase closes on start→path,
                // flee gains distance from the threat on the first step(s).
                var earlyIndex = Mathf.Min(1, cells.Count - 1);
                var early = GridStep.Chebyshev(start, cells[earlyIndex]);
                var fleeGain = 0;
                if (fleeFrom != null)
                {
                    fleeGain = GridStep.Chebyshev(cells[0], fleeFrom.Value) - startFleeDist;
                }

                var better =
                    best == null
                    || cells.Count < best.Count
                    || (cells.Count == best.Count && fleeFrom != null && fleeGain > bestFleeGain)
                    || (
                        cells.Count == best.Count
                        && fleeFrom != null
                        && fleeGain == bestFleeGain
                        && early > bestEarly
                    )
                    || (cells.Count == best.Count && fleeFrom == null && early > bestEarly)
                    || (
                        cells.Count == best.Count
                        && fleeGain == bestFleeGain
                        && early == bestEarly
                        && Compare(cells[cells.Count - 1], best[best.Count - 1]) < 0
                    );
                if (better)
                {
                    best = cells;
                    bestEarly = early;
                    bestFleeGain = fleeGain;
                }
            }

            return best;
        }

        private static List<Vector2Int> ToCells(Vector2Int start, List<GraphNode> path)
        {
            var cells = new List<Vector2Int>();
            var pastStart = false;
            foreach (var node in path)
            {
                if (!(node is GridNodeBase grid))
                {
                    continue;
                }

                var cell = new Vector2Int(grid.XCoordinateInGrid, grid.ZCoordinateInGrid);
                if (!pastStart)
                {
                    if (cell == start)
                    {
                        pastStart = true;
                        continue;
                    }

                    // Path must begin at the requested start cell.
                    return null;
                }

                if (cell == start)
                {
                    // Ignore a rare revisit of start; do not drop later cells that
                    // merely match start's coordinates after leaving.
                    continue;
                }

                if (cells.Count == 0 || cells[cells.Count - 1] != cell)
                {
                    cells.Add(cell);
                }
            }

            return cells.Count == 0 ? null : cells;
        }

        private static bool IsContiguousFrom(Vector2Int start, List<Vector2Int> cells)
        {
            var prev = start;
            for (var i = 0; i < cells.Count; i++)
            {
                if (!GridStep.IsAdjacent(prev, cells[i]))
                {
                    return false;
                }

                prev = cells[i];
            }

            return true;
        }

        private static int Compare(Vector2Int a, Vector2Int b)
        {
            var byX = a.x.CompareTo(b.x);
            return byX != 0 ? byX : a.y.CompareTo(b.y);
        }
    }
}
