using System.Collections.Generic;
using UnityEngine;

namespace SlimesRevenge
{
    /// <summary>
    /// Grid distance and 8-directional adjacency (Chebyshev).
    /// Single source for player attack range, mob melee, movement steps, and vision rings.
    /// </summary>
    public static class GridStep
    {
        /// <summary>The eight cells at Chebyshev distance 1 (orthogonal + diagonal).</summary>
        public static readonly Vector2Int[] AdjacentOffsets =
        {
            new Vector2Int(1, 0),
            new Vector2Int(1, 1),
            new Vector2Int(0, 1),
            new Vector2Int(-1, 1),
            new Vector2Int(-1, 0),
            new Vector2Int(-1, -1),
            new Vector2Int(0, -1),
            new Vector2Int(1, -1),
        };

        public static int Chebyshev(Vector2Int from, Vector2Int to)
        {
            return Mathf.Max(Mathf.Abs(from.x - to.x), Mathf.Abs(from.y - to.y));
        }

        /// <summary>True when cells share an edge or a corner (Chebyshev distance == 1).</summary>
        public static bool IsAdjacent(Vector2Int from, Vector2Int to)
        {
            return Chebyshev(from, to) == 1;
        }

        /// <summary>Creature melee / touch range — same 8-dir rule as cells.</summary>
        public static bool IsAdjacent(Creature a, Creature b)
        {
            return a != null && b != null && IsAdjacent(a.Cell, b.Cell);
        }

        public static bool InRange(Vector2Int from, Vector2Int to, int range)
        {
            return Chebyshev(from, to) <= range;
        }

        /// <summary>
        /// Every cell at exact Chebyshev distance <paramref name="distance"/> from <paramref name="origin"/>.
        /// </summary>
        public static IEnumerable<Vector2Int> Ring(Vector2Int origin, int distance)
        {
            if (distance <= 0)
            {
                yield break;
            }

            for (var y = -distance; y <= distance; y++)
            {
                for (var x = -distance; x <= distance; x++)
                {
                    if (Chebyshev(Vector2Int.zero, new Vector2Int(x, y)) != distance)
                    {
                        continue;
                    }

                    yield return origin + new Vector2Int(x, y);
                }
            }
        }

        public static Vector2Int FromSwipe(Vector2 delta)
        {
            if (delta.sqrMagnitude <= 0f)
            {
                return Vector2Int.zero;
            }

            var angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            if (angle < 0f)
            {
                angle += 360f;
            }

            if (angle < 22.5f || angle >= 337.5f)
            {
                return Vector2Int.right;
            }

            if (angle < 67.5f)
            {
                return new Vector2Int(1, 1);
            }

            if (angle < 112.5f)
            {
                return Vector2Int.up;
            }

            if (angle < 157.5f)
            {
                return new Vector2Int(-1, 1);
            }

            if (angle < 202.5f)
            {
                return Vector2Int.left;
            }

            if (angle < 247.5f)
            {
                return new Vector2Int(-1, -1);
            }

            if (angle < 292.5f)
            {
                return Vector2Int.down;
            }

            return new Vector2Int(1, -1);
        }

        public static bool TryFromKey(KeyCode key, out Vector2Int offset, out bool wait)
        {
            offset = Vector2Int.zero;
            wait = false;
            switch (key)
            {
                case KeyCode.W:
                case KeyCode.UpArrow:
                case KeyCode.Keypad8:
                case KeyCode.Alpha8:
                    offset = Vector2Int.up;
                    return true;
                case KeyCode.S:
                case KeyCode.DownArrow:
                case KeyCode.Keypad2:
                case KeyCode.Alpha2:
                    offset = Vector2Int.down;
                    return true;
                case KeyCode.A:
                case KeyCode.LeftArrow:
                case KeyCode.Keypad4:
                case KeyCode.Alpha4:
                    offset = Vector2Int.left;
                    return true;
                case KeyCode.D:
                case KeyCode.RightArrow:
                case KeyCode.Keypad6:
                case KeyCode.Alpha6:
                    offset = Vector2Int.right;
                    return true;
                case KeyCode.Keypad7:
                case KeyCode.Alpha7:
                    offset = new Vector2Int(-1, 1);
                    return true;
                case KeyCode.Keypad9:
                case KeyCode.Alpha9:
                    offset = new Vector2Int(1, 1);
                    return true;
                case KeyCode.Keypad1:
                case KeyCode.Alpha1:
                    offset = new Vector2Int(-1, -1);
                    return true;
                case KeyCode.Keypad3:
                case KeyCode.Alpha3:
                    offset = new Vector2Int(1, -1);
                    return true;
                case KeyCode.Keypad5:
                case KeyCode.Alpha5:
                case KeyCode.Space:
                    wait = true;
                    return true;
                default:
                    return false;
            }
        }
    }
}
