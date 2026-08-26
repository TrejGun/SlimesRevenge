using System.Collections.Generic;
using UnityEngine;

namespace SlimesRevenge
{
    public static class CreatureMoves
    {
        public static bool CanSee(Creature self, Creature player)
        {
            return self != null
                && player != null
                && GridStep.InRange(self.Cell, player.Cell, self.VisionRange);
        }

        public static bool IsAdjacent(Creature self, Creature player)
        {
            return self != null && player != null && GridStep.IsAdjacent(self.Cell, player.Cell);
        }

        public static bool TryWander(Creature self, GameSession session, IRng rng)
        {
            if (self == null || self.Speed < 1)
            {
                return false;
            }

            return TryStep(self, session, Candidates(session, self.Cell, 1), rng);
        }

        public static bool TryFlee(Creature self, Creature player, GameSession session, IRng rng)
        {
            return TryToward(self, session, rng, player.Cell, farther: true);
        }

        public static bool TryChase(Creature self, Creature player, GameSession session, IRng rng)
        {
            if (self == null || player == null || session == null)
            {
                return false;
            }

            var goals = ApproachCells(session, player.Cell);
            if (!GridPath.TryWalk(
                    session.World,
                    self.Cell,
                    goals,
                    cell => IsBlocked(session, self.Cell, cell),
                    self.Speed,
                    out var destination))
            {
                return false;
            }

            return TryStep(self, session, new List<Vector2Int> { destination }, rng);
        }

        public static bool Attack(Creature attacker, Creature target)
        {
            // Death (corpse / game over) is resolved by TurnManager after the act.
            return Combat.Attack(attacker, target);
        }

        public static bool Attack(Creature target)
        {
            return Attack(null, target);
        }

        public static bool Perform(CreatureIntent intent, Creature self, Creature player, GameSession session, IRng rng)
        {
            switch (intent)
            {
                case CreatureIntent.Attack:
                    return Attack(self, player);
                case CreatureIntent.Chase:
                    return TryChase(self, player, session, rng);
                case CreatureIntent.Flee:
                    return TryFlee(self, player, session, rng);
                case CreatureIntent.Wander:
                    return TryWander(self, session, rng);
                case CreatureIntent.Idle:
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryToward(Creature self, GameSession session, IRng rng, Vector2Int player, bool farther)
        {
            var speed = self.Speed;
            if (speed < 1)
            {
                return false;
            }

            var current = GridStep.Chebyshev(self.Cell, player);
            if (speed > 1 && TryStep(self, session, Filtered(session, self.Cell, speed, player, current, farther), rng))
            {
                return true;
            }

            return TryStep(self, session, Filtered(session, self.Cell, 1, player, current, farther), rng);
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
            return true;
        }

        private static List<Vector2Int> ApproachCells(GameSession session, Vector2Int player)
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
                    if (session.World.Contains(cell) && !session.IsOccupied(cell))
                    {
                        cells.Add(cell);
                    }
                }
            }

            return cells;
        }

        private static bool IsBlocked(GameSession session, Vector2Int self, Vector2Int cell)
        {
            return cell == session.PlayerCell || (cell != self && session.IsOccupied(cell));
        }

        private static List<Vector2Int> Filtered(
            GameSession session,
            Vector2Int from,
            int step,
            Vector2Int player,
            int current,
            bool farther)
        {
            var cells = Candidates(session, from, step);
            cells.RemoveAll(to =>
            {
                var next = GridStep.Chebyshev(to, player);
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
