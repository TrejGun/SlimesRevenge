using System.Collections.Generic;
using UnityEngine;

namespace SlimesRevenge
{
    public static class CreatureMoves
    {
        public static bool CanSee(Creature self, Creature other)
        {
            return self != null
                && other != null
                && other.IsAlive
                && GridStep.InRange(self.Cell, other.Cell, self.VisionRange);
        }

        public static bool IsAdjacent(Creature self, Creature other)
        {
            return self != null && other != null && GridStep.IsAdjacent(self.Cell, other.Cell);
        }

        public static bool TryWander(Creature self, GameSession session, IRng rng)
        {
            if (self == null || self.Speed < 1)
            {
                return false;
            }

            return TryStep(self, session, Candidates(session, self.Cell, 1), rng);
        }

        public static bool TryFlee(Creature self, Creature threat, GameSession session, IRng rng)
        {
            if (self == null || session == null)
            {
                return false;
            }

            var goal = self.FleeCell ?? threat?.Cell;
            if (goal == null)
            {
                return TryToward(self, session, rng, threat.Cell, farther: true);
            }

            if (self.Cell == goal.Value)
            {
                return true;
            }

            if (GridPath.TryWalk(
                    session.World,
                    self.Cell,
                    new[] { goal.Value },
                    cell => IsBlocked(session, self.Cell, cell),
                    self.Speed,
                    out var destination,
                    self))
            {
                return TryStep(self, session, new List<Vector2Int> { destination }, rng);
            }

            return TryToward(self, session, rng, threat != null ? threat.Cell : goal.Value, farther: true);
        }

        public static bool TryChase(Creature self, Creature focus, GameSession session, IRng rng)
        {
            if (self == null || session == null)
            {
                return false;
            }

            Vector2Int targetCell;
            if (focus != null && focus.IsAlive && CanSee(self, focus))
            {
                targetCell = focus.Cell;
                self.RememberPursuit(targetCell);
            }
            else if (self.PursuitCell != null)
            {
                targetCell = self.PursuitCell.Value;
            }
            else if (focus != null)
            {
                targetCell = focus.Cell;
            }
            else
            {
                return false;
            }

            var goals = ApproachCells(session, targetCell, self.Cell);
            if (goals.Count == 0 && session.World.IsTerrainWalkable(targetCell) && !session.IsOccupied(targetCell))
            {
                goals.Add(targetCell);
            }

            if (!GridPath.TryWalk(
                    session.World,
                    self.Cell,
                    goals,
                    cell => IsBlocked(session, self.Cell, cell),
                    self.Speed,
                    out var destination,
                    self))
            {
                return false;
            }

            return TryStep(self, session, new List<Vector2Int> { destination }, rng);
        }

        public static bool Attack(Creature attacker, Creature target)
        {
            return Combat.Attack(attacker, target);
        }

        public static bool Attack(Creature target)
        {
            return Attack(null, target);
        }

        public static bool Perform(CreatureIntent intent, Creature self, Creature focus, GameSession session, IRng rng)
        {
            switch (intent)
            {
                case CreatureIntent.Attack:
                    return Attack(self, focus);
                case CreatureIntent.Chase:
                    return TryChase(self, focus, session, rng);
                case CreatureIntent.Flee:
                    return TryFlee(self, focus, session, rng);
                case CreatureIntent.Wander:
                    return TryWander(self, session, rng);
                case CreatureIntent.Idle:
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryToward(Creature self, GameSession session, IRng rng, Vector2Int anchor, bool farther)
        {
            var speed = self.Speed;
            if (speed < 1)
            {
                return false;
            }

            var current = GridStep.Chebyshev(self.Cell, anchor);
            if (speed > 1 && TryStep(self, session, Filtered(session, self.Cell, speed, anchor, current, farther), rng))
            {
                return true;
            }

            return TryStep(self, session, Filtered(session, self.Cell, 1, anchor, current, farther), rng);
        }

        private static bool TryStep(Creature self, GameSession session, List<Vector2Int> options, IRng rng)
        {
            if (self == null || session == null || options == null || options.Count == 0)
            {
                return false;
            }

            var pick = rng == null ? 0 : rng.Pick(options.Count);
            var to = options[pick];
            if (!session.TryMoveOccupant(self.Cell, to))
            {
                return false;
            }

            self.PlaceOn(to);
            session.World.Floor.ApplyContact(self);
            return true;
        }

        private static List<Vector2Int> ApproachCells(GameSession session, Vector2Int target, Vector2Int selfCell)
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

                    var cell = target + new Vector2Int(x, y);
                    if (cell == selfCell)
                    {
                        cells.Add(cell);
                        continue;
                    }

                    if (session.World.IsTerrainWalkable(cell)
                        && cell != session.PlayerCell
                        && !session.IsOccupied(cell))
                    {
                        cells.Add(cell);
                    }
                }
            }

            return cells;
        }

        /// <summary>
        /// Hard blocks: walls and occupants only. Puddles are walkable traps ranked by
        /// <see cref="Substance.FloorPriorityFor"/>.
        /// </summary>
        public static bool IsBlocked(GameSession session, Vector2Int self, Vector2Int cell)
        {
            if (session == null || !session.World.Contains(cell))
            {
                return true;
            }

            if (session.World.IsWall(cell))
            {
                return true;
            }

            if (cell == session.PlayerCell)
            {
                return true;
            }

            return cell != self && session.IsOccupied(cell);
        }

        private static List<Vector2Int> Filtered(
            GameSession session,
            Vector2Int from,
            int step,
            Vector2Int anchor,
            int current,
            bool farther)
        {
            var cells = Candidates(session, from, step);
            cells.RemoveAll(to =>
            {
                var next = GridStep.Chebyshev(to, anchor);
                return farther ? next <= current : next >= current;
            });
            return cells;
        }

        private static List<Vector2Int> Candidates(GameSession session, Vector2Int from, int step)
        {
            var cells = new List<Vector2Int>();
            for (var y = -step; y <= step; y++)
            {
                for (var x = -step; x <= step; x++)
                {
                    if (GridStep.Chebyshev(Vector2Int.zero, new Vector2Int(x, y)) != step)
                    {
                        continue;
                    }

                    var to = from + new Vector2Int(x, y);
                    if (session != null && session.CanOccupantStep(from, to))
                    {
                        cells.Add(to);
                    }
                }
            }

            cells.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            return cells;
        }
    }
}
