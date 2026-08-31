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
            return GridStep.IsAdjacent(self, other);
        }

        public static bool TryWander(Creature self, GameSession session, IRng rng)
        {
            if (self == null || self.Speed < 1)
            {
                return false;
            }

            return TryStep(self, session, WanderCandidates(session, self.Cell), rng);
        }

        public static bool TryFlee(Creature self, Creature threat, GameSession session, IRng rng)
        {
            if (self == null || session == null || threat == null)
            {
                return false;
            }

            if (
                self.FleeCell == null
                || !session.World.IsTerrainWalkable(self.FleeCell.Value)
                || session.IsOccupied(self.FleeCell.Value)
                || self.Cell == self.FleeCell.Value
            )
            {
                self.SetFleeCell(CreatureHunt.PickFleeCell(self, threat, session));
            }

            var goal = self.FleeCell;
            if (goal == null || self.Cell == goal.Value)
            {
                return goal != null && self.Cell == goal.Value;
            }

            var fromThreat = GridStep.Chebyshev(self.Cell, threat.Cell);
            if (
                GridPath.TryWalk(
                    session.World,
                    self.Cell,
                    new[] { goal.Value },
                    cell => IsBlocked(session, self.Cell, cell),
                    self.Speed,
                    out var destination,
                    self,
                    threat.Cell
                )
                && GridStep.Chebyshev(destination, threat.Cell) > fromThreat
                && TryStep(self, session, new List<Vector2Int> { destination }, rng)
            )
            {
                return true;
            }

            // A* to a far waypoint can skirt the threat first; fall back to any adjacent
            // step that increases Chebyshev distance (still no chase-style BestToward).
            var away = new List<Vector2Int>();
            for (var y = -1; y <= 1; y++)
            {
                for (var x = -1; x <= 1; x++)
                {
                    if (x == 0 && y == 0)
                    {
                        continue;
                    }

                    var cell = self.Cell + new Vector2Int(x, y);
                    if (IsBlocked(session, self.Cell, cell))
                    {
                        continue;
                    }

                    if (GridStep.Chebyshev(cell, threat.Cell) > fromThreat)
                    {
                        away.Add(cell);
                    }
                }
            }

            return TryStep(self, session, away, rng);
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
            if (
                goals.Count == 0
                && session.World.IsTerrainWalkable(targetCell)
                && !session.IsOccupied(targetCell)
            )
            {
                goals.Add(targetCell);
            }

            if (goals.Count == 0)
            {
                return false;
            }

            return GridPath.TryWalk(
                    session.World,
                    self.Cell,
                    goals,
                    cell => IsBlocked(session, self.Cell, cell),
                    self.Speed,
                    out var destination,
                    self
                ) && TryStep(self, session, new List<Vector2Int> { destination }, rng);
        }

        public static bool Attack(Creature attacker, Creature target)
        {
            return Combat.Attack(attacker, target);
        }

        public static bool Attack(Creature target)
        {
            return Attack(null, target);
        }

        public static bool Perform(
            CreatureIntent intent,
            Creature self,
            Creature focus,
            GameSession session,
            IRng rng
        )
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
                    CreatureSpriteAnimator.PlayIdle(self);
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryStep(
            Creature self,
            GameSession session,
            List<Vector2Int> options,
            IRng rng
        )
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
            CreatureSpriteAnimator.PlayWalk(self, to);
            return true;
        }

        private static List<Vector2Int> ApproachCells(
            GameSession session,
            Vector2Int target,
            Vector2Int selfCell
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

                    var cell = target + new Vector2Int(x, y);
                    if (cell == selfCell)
                    {
                        cells.Add(cell);
                        continue;
                    }

                    if (session.World.IsTerrainWalkable(cell) && !session.IsOccupied(cell))
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

            return cell != self && session.IsOccupied(cell);
        }

        private static List<Vector2Int> WanderCandidates(GameSession session, Vector2Int from)
        {
            var cells = new List<Vector2Int>();
            foreach (var to in GridStep.Ring(from, 1))
            {
                if (session != null && session.CanOccupantStep(from, to))
                {
                    cells.Add(to);
                }
            }

            cells.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            return cells;
        }
    }
}
