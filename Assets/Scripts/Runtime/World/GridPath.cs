using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SlimesRevenge
{
    public static class GridPath
    {
        public static bool TryWalk(
            World world,
            Vector2Int from,
            IEnumerable<Vector2Int> goals,
            Func<Vector2Int, bool> blocked,
            int maxStep,
            out Vector2Int destination)
        {
            destination = from;
            if (maxStep < 1)
            {
                return false;
            }

            var path = Find(world, from, goals, blocked);
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
            Func<Vector2Int, bool> blocked)
        {
            if (world == null || blocked == null || goals == null)
            {
                return null;
            }

            var goalSet = new HashSet<Vector2Int>();
            foreach (var goal in goals)
            {
                if (world.Contains(goal) && !blocked(goal))
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

            var sources = WalkableSources(world, start, blocked);
            if (sources.Count == 0)
            {
                return null;
            }

            var bounds = new Bounds(
                new Vector3(world.Width * 0.5f, 0f, world.Height * 0.5f),
                new Vector3(world.Width + 2f, 2f, world.Height + 2f));
            var data = NavMeshBuilder.BuildNavMeshData(
                AgentSettings(),
                sources,
                bounds,
                Vector3.zero,
                Quaternion.identity);
            if (data == null)
            {
                return null;
            }

            var instance = NavMesh.AddNavMeshData(data);
            try
            {
                return Shortest(start, goalSet);
            }
            finally
            {
                if (instance.valid)
                {
                    NavMesh.RemoveNavMeshData(instance);
                }

                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(data);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(data);
                }
            }
        }

        private static List<Vector2Int> Shortest(Vector2Int start, HashSet<Vector2Int> goals)
        {
            List<Vector2Int> best = null;
            foreach (var goal in goals)
            {
                var path = Trace(start, goal);
                if (path == null)
                {
                    continue;
                }

                if (best == null
                    || path.Count < best.Count
                    || (path.Count == best.Count && Compare(path[path.Count - 1], best[best.Count - 1]) < 0))
                {
                    best = path;
                }
            }

            return best;
        }

        private static List<Vector2Int> Trace(Vector2Int start, Vector2Int goal)
        {
            if (!NavMesh.SamplePosition(Point(start), out var from, 0.75f, NavMesh.AllAreas)
                || !NavMesh.SamplePosition(Point(goal), out var to, 0.75f, NavMesh.AllAreas))
            {
                return null;
            }

            var navPath = new NavMeshPath();
            if (!NavMesh.CalculatePath(from.position, to.position, NavMesh.AllAreas, navPath)
                || navPath.status != NavMeshPathStatus.PathComplete
                || navPath.corners.Length == 0)
            {
                return null;
            }

            var cells = new List<Vector2Int>();
            var previous = start;
            foreach (var corner in navPath.corners)
            {
                var cell = Cell(corner);
                AppendLine(cells, previous, cell);
                previous = cell;
            }

            if (cells.Count > 0 && cells[0] == start)
            {
                cells.RemoveAt(0);
            }

            return cells.Count == 0 ? null : cells;
        }

        private static List<NavMeshBuildSource> WalkableSources(
            World world,
            Vector2Int start,
            Func<Vector2Int, bool> blocked)
        {
            var sources = new List<NavMeshBuildSource>();
            for (var y = 0; y < world.Height; y++)
            {
                for (var x = 0; x < world.Width; x++)
                {
                    var cell = new Vector2Int(x, y);
                    if (cell != start && blocked(cell))
                    {
                        continue;
                    }

                    sources.Add(new NavMeshBuildSource
                    {
                        shape = NavMeshBuildSourceShape.Box,
                        area = 0,
                        size = new Vector3(1.1f, 0.2f, 1.1f),
                        transform = Matrix4x4.TRS(Point(cell), Quaternion.identity, Vector3.one)
                    });
                }
            }

            return sources;
        }

        private static NavMeshBuildSettings AgentSettings()
        {
            var settings = NavMesh.GetSettingsCount() > 0
                ? NavMesh.GetSettingsByIndex(0)
                : NavMesh.CreateSettings();
            settings.agentRadius = 0.2f;
            settings.agentHeight = 1f;
            settings.agentSlope = 60f;
            settings.agentClimb = 0.4f;
            settings.minRegionArea = 0f;
            settings.overrideVoxelSize = true;
            settings.voxelSize = settings.agentRadius / 3f;
            settings.overrideTileSize = true;
            settings.tileSize = 32;
            return settings;
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
