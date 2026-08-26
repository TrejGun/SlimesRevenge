using System.Collections.Generic;

namespace SlimesRevenge
{
    /// <summary>
    /// Slime volume-dominance collaborator: holds the dominant substance sample, clears
    /// dominance-owned queue entries on change, and applies <see cref="Substance.ApplyDominance"/>.
    /// Passive lookups go through <see cref="Substance.FindDominanceStatus{T}"/>.
    /// </summary>
    public sealed class VolumeDominance
    {
        private Substance dominant;

        public Substance Dominant => dominant;

        public T FindStatus<T>() where T : StatusEffect
        {
            return dominant != null ? dominant.FindDominanceStatus<T>() : null;
        }

        public bool Blocks(StatusEffect incoming)
        {
            return dominant != null && dominant.DominanceBlocks(incoming);
        }

        /// <summary>
        /// Sync with <paramref name="volume"/>. No-op if dominant type unchanged.
        /// </summary>
        public void Refresh(Volume volume, Creature owner, IList<StatusEffect> queue)
        {
            if (volume == null || queue == null)
            {
                return;
            }

            volume.TryDominant(out var next);
            if (next?.GetType() == dominant?.GetType())
            {
                return;
            }

            ClearOwned(queue);
            dominant = next?.Clone();
            dominant?.ApplyDominance(owner, queue);
        }

        private static void ClearOwned(IList<StatusEffect> queue)
        {
            for (var i = queue.Count - 1; i >= 0; i--)
            {
                if (queue[i].ClearedWhenDominanceChanges)
                {
                    queue.RemoveAt(i);
                }
            }
        }
    }
}
