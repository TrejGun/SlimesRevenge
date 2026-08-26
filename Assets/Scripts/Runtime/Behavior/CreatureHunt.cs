using System.Collections.Generic;
using UnityEngine;

namespace SlimesRevenge
{
    /// <summary>
    /// Overrides Wander/Idle from Unity Behavior graphs.
    /// Order: fear/flee first, then visible prey hunt, then last-known pursuit memory.
    /// Prey/predator matching is driven by status <see cref="StatusEffect.IsPrey"/> /
    /// <see cref="StatusEffect.IsPredator"/>.
    /// </summary>
    public static class CreatureHunt
    {
        public static CreatureIntent Redirect(Creature self, CreatureIntent intent, IReadOnlyList<Creature> others)
        {
            if (self == null)
            {
                return intent;
            }

            if (intent != CreatureIntent.Wander && intent != CreatureIntent.Idle)
            {
                return intent;
            }

            var predator = FindPredator(self, others);
            if (predator != null)
            {
                CreatureTurnContext.FocusTarget = predator;
                EnsureFleeWaypoint(self, predator, CreatureTurnContext.Session);
                return CreatureIntent.Flee;
            }

            self.ClearFlee();

            var prey = FindPrey(self, others);
            if (prey != null)
            {
                CreatureTurnContext.FocusTarget = prey;
                self.RememberPursuit(prey.Cell);
                return CreatureMoves.IsAdjacent(self, prey) ? CreatureIntent.Attack : CreatureIntent.Chase;
            }

            if (self.PursuitCell != null && self.PursuitMemoryLeft > 0)
            {
                return CreatureIntent.Chase;
            }

            self.ClearPursuit();
            return intent;
        }

        public static void AfterAct(
            Creature self,
            CreatureIntent intent,
            Creature focus,
            IReadOnlyList<Creature> others)
        {
            if (self == null)
            {
                return;
            }

            if (intent == CreatureIntent.Flee)
            {
                if (self.FleeCell != null && self.Cell == self.FleeCell.Value)
                {
                    self.ClearFlee();
                }

                if (focus != null && CreatureMoves.IsAdjacent(self, focus))
                {
                    self.ClearFlee();
                    self.MarkAggro();
                }

                return;
            }

            if (intent != CreatureIntent.Chase && intent != CreatureIntent.Attack)
            {
                return;
            }

            var prey = focus != null && IsPreyOf(self, focus) ? focus : FindPrey(self, others);
            if (prey != null && CreatureMoves.CanSee(self, prey))
            {
                self.RememberPursuit(prey.Cell);
                return;
            }

            if (self.PursuitCell == null)
            {
                return;
            }

            // Arrived at (or onto) the last-known cell and still no sight → drop pursuit.
            if (self.Cell == self.PursuitCell.Value
                || GridStep.IsAdjacent(self.Cell, self.PursuitCell.Value))
            {
                self.ClearPursuit();
                return;
            }

            self.TickPursuitMemory();
        }

        public static Creature FindPrey(Creature self, IReadOnlyList<Creature> others)
        {
            return NearestMatching(self, others, other => IsPreyOf(self, other));
        }

        public static Creature FindPredator(Creature self, IReadOnlyList<Creature> others)
        {
            return NearestMatching(self, others, other => IsPredatorOf(self, other));
        }

        public static bool IsPreyOf(Creature self, Creature other)
        {
            return MatchesTrait(self, other, (trait, candidate) => trait.IsPrey(candidate));
        }

        public static bool IsPredatorOf(Creature self, Creature other)
        {
            return MatchesTrait(self, other, (trait, candidate) => trait.IsPredator(candidate));
        }

        private static bool MatchesTrait(
            Creature self,
            Creature other,
            System.Func<StatusEffect, Creature, bool> match)
        {
            if (self == null || other == null)
            {
                return false;
            }

            var list = self.Statuses;
            for (var i = 0; i < list.Count; i++)
            {
                if (match(list[i], other))
                {
                    return true;
                }
            }

            return false;
        }

        private static void EnsureFleeWaypoint(Creature self, Creature threat, GameSession session)
        {
            if (self == null || threat == null || session == null)
            {
                return;
            }

            if (self.FleeCell != null
                && session.World.IsTerrainWalkable(self.FleeCell.Value)
                && !session.IsOccupied(self.FleeCell.Value)
                && self.Cell != self.FleeCell.Value)
            {
                return;
            }

            self.SetFleeCell(PickFleeCell(self, threat, session));
        }

        public static Vector2Int PickFleeCell(Creature self, Creature threat, GameSession session)
        {
            var best = self.Cell;
            var bestScore = -1;
            for (var y = 0; y < session.World.Height; y++)
            {
                for (var x = 0; x < session.World.Width; x++)
                {
                    var cell = new Vector2Int(x, y);
                    if (!session.World.IsTerrainWalkable(cell) || session.IsOccupied(cell) || cell == session.PlayerCell)
                    {
                        continue;
                    }

                    var away = GridStep.Chebyshev(cell, threat.Cell);
                    var fromSelf = GridStep.Chebyshev(cell, self.Cell);
                    // Prefer far from threat; break ties by reachable distance from self.
                    var score = away * 1000 - fromSelf;
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = cell;
                    }
                }
            }

            return best;
        }

        private static Creature NearestMatching(
            Creature self,
            IReadOnlyList<Creature> others,
            System.Func<Creature, bool> match)
        {
            if (self == null || match == null)
            {
                return null;
            }

            Creature best = null;
            var bestDist = int.MaxValue;

            void Consider(Creature other)
            {
                if (other == null || other == self || !other.IsAlive || !match(other))
                {
                    return;
                }

                if (!CreatureMoves.CanSee(self, other))
                {
                    return;
                }

                var dist = GridStep.Chebyshev(self.Cell, other.Cell);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = other;
                }
            }

            if (others != null)
            {
                for (var i = 0; i < others.Count; i++)
                {
                    Consider(others[i]);
                }
            }

            Consider(CreatureTurnContext.Player);
            return best;
        }
    }
}
