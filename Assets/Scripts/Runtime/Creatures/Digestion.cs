namespace SlimesRevenge
{
    /// <summary>
    /// Devouring a corpse: one unit moves into the slime each player-turn start.
    /// Leftover matter is discarded if the slime is already at capacity.
    /// </summary>
    public sealed class Digestion
    {
        public Corpse Current { get; private set; }

        public bool IsBusy => Current != null && Current.Volume.UnitCount > 0;

        public bool TryBegin(Corpse corpse)
        {
            if (IsBusy || corpse == null || corpse.Volume.UnitCount == 0)
            {
                return false;
            }

            Current = corpse;
            return true;
        }

        public bool Tick(Volume destination)
        {
            if (Current == null || destination == null)
            {
                return false;
            }

            if (Current.Volume.UnitCount == 0)
            {
                Current = null;
                return false;
            }

            if (destination.UnitCount >= Volume.Capacity)
            {
                // No room left — remaining corpse volume vanishes with the body.
                Current.Volume.Clear();
                Current = null;
                return false;
            }

            if (!Current.Volume.TryTakeEnd(out var substance))
            {
                Current = null;
                return false;
            }

            destination.Add(Volume.CloneSubstance(substance));
            if (Current.Volume.UnitCount == 0)
            {
                Current = null;
            }

            return true;
        }
    }
}
