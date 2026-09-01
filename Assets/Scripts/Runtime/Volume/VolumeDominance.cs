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

        public T FindStatus<T>()
            where T : StatusEffect
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
            LogDominanceChange(owner, dominant);
            dominant?.ApplyDominance(owner, queue);
        }

        public void CaptureSurvivedHitReactions(IList<StatusEffect> sink)
        {
            dominant?.CaptureSurvivedHitReactions(sink);
        }

        public void CollectPassives(IList<StatusEffect> sink)
        {
            dominant?.CollectDominancePassives(sink);
        }

        /// <summary>
        /// Scale harm via dominance passives that are not already queued
        /// (e.g. Forever <see cref="Flammable"/> under oil dominance).
        /// </summary>
        public int ModifyIncomingHarm(
            Creature owner,
            int amount,
            IReadOnlyList<StatusEffect> queued
        )
        {
            if (dominant == null || amount <= 0)
            {
                return amount;
            }

            var passives = new List<StatusEffect>(4);
            dominant.CollectDominancePassives(passives);
            for (var i = 0; i < passives.Count; i++)
            {
                var passive = passives[i];
                if (passive == null || QueuedContainsType(queued, passive.GetType()))
                {
                    continue;
                }

                amount = passive.ModifyIncomingHarm(owner, amount);
            }

            return amount;
        }

        private static bool QueuedContainsType(IReadOnlyList<StatusEffect> queued, System.Type type)
        {
            if (queued == null || type == null)
            {
                return false;
            }

            for (var i = 0; i < queued.Count; i++)
            {
                if (queued[i] != null && queued[i].GetType() == type)
                {
                    return true;
                }
            }

            return false;
        }

        private static void LogDominanceChange(Creature owner, Substance next)
        {
            if (!ActionLog.HasOpenGroup)
            {
                return;
            }

            if (next == null)
            {
                ActionLog.Detail(I18n.Get(TextKey.LogDominanceLost));
                return;
            }

            ActionLog.Detail(
                ActionLogLine.FormatKey(TextKey.LogDominance, ActionLogPart.Substance(next))
            );
            next.LogDominanceGains(owner);
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
