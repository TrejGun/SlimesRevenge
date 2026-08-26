using UnityEngine;

namespace SlimesRevenge
{
    public sealed class Corpse
    {
        private bool skipNextTick = true;

        public Corpse(Creature creature, CreatureKind kind)
            : this(kind, creature.Volume.Clone(), creature.Cell, creature.MaxHitPoints)
        {
        }

        public Corpse(CreatureKind kind, Volume volume, Vector2Int cell)
            : this(kind, volume, cell, MaxHitPointsOf(kind))
        {
        }

        private Corpse(CreatureKind kind, Volume volume, Vector2Int cell, int maxHitPoints)
        {
            Kind = kind;
            Volume = volume ?? new Volume();
            Cell = cell;
            // Decay follows max HP (from the creature, or the kind's default).
            DecayTurnsLeft = Mathf.Max(1, maxHitPoints);
        }

        public CreatureKind Kind { get; }

        public Volume Volume { get; }

        public Vector2Int Cell { get; }

        public int DecayTurnsLeft { get; private set; }

        public bool Expired => DecayTurnsLeft <= 0 || Volume.UnitCount == 0;

        public static int MaxHitPointsOf(CreatureKind kind)
        {
            switch (kind)
            {
                case CreatureKind.Rat:
                    return 3;
                case CreatureKind.Cat:
                    return 5;
                case CreatureKind.Dog:
                    return 10;
                default:
                    return 1;
            }
        }

        public void Tick()
        {
            // Created mid-turn; do not age on the same environment tick as death.
            if (skipNextTick)
            {
                skipNextTick = false;
                return;
            }

            if (DecayTurnsLeft > 0)
            {
                DecayTurnsLeft--;
            }
        }
    }
}
