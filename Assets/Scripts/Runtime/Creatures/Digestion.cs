using UnityEngine;

namespace SlimesRevenge
{
    /// <summary>
    /// Devouring a corpse creature: one unit moves into the slime each player-turn start.
    /// Leftover matter is discarded if the slime is already at capacity.
    /// </summary>
    public sealed class Digestion
    {
        public Creature Current { get; private set; }

        public bool IsBusy => Current != null && Current.Volume.UnitCount > 0;

        public bool TryBegin(Creature corpse)
        {
            if (IsBusy || corpse == null || !corpse.IsCorpse || corpse.Volume.UnitCount == 0)
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
                FinishCurrent();
                return false;
            }

            if (destination.UnitCount >= Volume.Capacity)
            {
                // No room left — remaining corpse volume vanishes with the body.
                Current.Volume.Clear();
                FinishCurrent();
                return false;
            }

            if (!Current.Volume.TryTakeEnd(out var substance))
            {
                FinishCurrent();
                return false;
            }

            destination.Add(Volume.CloneSubstance(substance));
            if (Current.Volume.UnitCount == 0)
            {
                FinishCurrent();
            }

            return true;
        }

        private void FinishCurrent()
        {
            var body = Current;
            Current = null;
            if (body == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(body.gameObject);
            }
            else
            {
                Object.DestroyImmediate(body.gameObject);
            }
        }
    }
}
