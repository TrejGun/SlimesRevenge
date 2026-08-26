using System;
using System.Collections.Generic;
using Pathfinding;
using UnityEngine;

namespace SlimesRevenge
{
    /// <summary>
    /// Grid pathfinding via <b>A* Pathfinding Project Free</b> (Aron Granberg).
    /// Builds a temporary <see cref="GridGraph"/>, marks walls/blockers unwalkable, runs <see cref="ABPath"/>.
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
            Creature walker = null)
        {
            destination = from;
            if (maxStep < 1)
            {
                return false;
            }

            var path = Find(world, from, goals, blocked, walker);
            if (path == null || path.Count == 0)
            {
                return false;
            }

            var steps = Mathf.Max(1, maxStep);
            destination = path[Mathf.Min(steps, path.Count) - 1];
            return true;
        }

        public static List<Vector2Int> Find(
            World world,
            Vector2Int start,
            IEnumerable<Vector2Int> goals,
            Func<Vector2Int, bool> blocked,
            Creature walker = null)
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

            if (goalSet.Count == 0)
            {
                return null;
            }

            if (goalSet.Contains(start))
            {
                return new List<Vector2Int>();
            }

            var host = new GameObject("AstarPathfindingProject.Temp")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var astar = host.AddComponent<AstarPath>();
            EnsureAstarReady(astar);
            try
            {
                ConfigureGraph(astar, world, start, blocked, walker);
                return ShortestAstarPath(start, goalSet);
            }
            finally
            {
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(host);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(host);
                }
            }
        }

        /// <summary>
        /// <see cref="AstarPath.Awake"/> skips init when not playing; EditMode tests need the same setup.
        /// </summary>
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

        private static void ConfigureGraph(
            AstarPath astar,
            World world,
            Vector2Int start,
            Func<Vector2Int, bool> blocked,
            Creature walker)
        {
            var gg = astar.data.AddGraph(typeof(GridGraph)) as GridGraph;
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
        }

        /// <summary>
        /// Scales <see cref="Substance.FloorPriorityFor"/> into A* node penalty so safer puddles win ties.
        /// </summary>
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

        private static List<Vector2Int> ShortestAstarPath(Vector2Int start, HashSet<Vector2Int> goals)
        {
            List<Vector2Int> best = null;
            foreach (var goal in goals)
            {
                var ab = ABPath.Construct(Point(start), Point(goal), null);
                AstarPath.StartPath(ab);
                ab.BlockUntilCalculated();
                if (ab.error || ab.vectorPath == null || ab.vectorPath.Count == 0)
                {
                    continue;
                }

                var cells = ToCells(start, ab.vectorPath);
                if (cells == null || cells.Count == 0)
                {
                    continue;
                }

                if (best == null
                    || cells.Count < best.Count
                    || (cells.Count == best.Count && Compare(cells[cells.Count - 1], best[best.Count - 1]) < 0))
                {
                    best = cells;
                }
            }

            return best;
        }

        private static List<Vector2Int> ToCells(Vector2Int start, List<Vector3> vectorPath)
        {
            var cells = new List<Vector2Int>();
            var previous = start;
            foreach (var point in vectorPath)
            {
                var cell = Cell(point);
                AppendLine(cells, previous, cell);
                previous = cell;
            }

            if (cells.Count > 0 && cells[0] == start)
            {
                cells.RemoveAt(0);
            }

            return cells.Count == 0 ? null : cells;
        }

        private static Vector3 Point(Vector2Int cell)
        {
            return new Vector3(cell.x + 0.5f, 0f, cell.y + 0.5f);
        }

        private static Vector2Int Cell(Vector3 point)
        {
            return new Vector2Int(Mathf.FloorToInt(point.x), Mathf.FloorToInt(point.z));
        }

        private static void AppendLine(List<Vector2Int> cells, Vector2Int from, Vector2Int to)
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

                if (cells.Count == 0 || cells[cells.Count - 1] != current)
                {
                    cells.Add(current);
                }
            }
        }

        private static int Compare(Vector2Int a, Vector2Int b)
        {
            var byX = a.x.CompareTo(b.x);
            return byX != 0 ? byX : a.y.CompareTo(b.y);
        }
    }
}
