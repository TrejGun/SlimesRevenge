using System;
using System.Collections.Generic;

namespace SlimesRevenge
{
    /// <summary>
    /// FIFO status list: Blocks / CancelsWith on add, longevity pick on find,
    /// and same-pass pulse (mid-pass inserts are processed when still unprocessed).
    /// Optional <see cref="VolumeDominance"/> merges passive lookups and extra blocks.
    /// </summary>
    public sealed class StatusQueue
    {
        private readonly List<StatusEffect> items = new List<StatusEffect>();

        public IList<StatusEffect> Items => items;

        public IReadOnlyList<StatusEffect> AsReadOnly => items;

        public int Count => items.Count;

        /// <summary>
        /// Apply <paramref name="effect"/> unless blocked (dominance or queue) or mutually
        /// cancelled with an existing entry. Cancel removes the existing entry and drops incoming.
        /// </summary>
        public void Add(StatusEffect effect, VolumeDominance dominance = null)
        {
            if (effect == null)
            {
                return;
            }

            if (dominance != null && dominance.Blocks(effect))
            {
                return;
            }

            for (var i = 0; i < items.Count; i++)
            {
                if (items[i].Blocks(effect))
                {
                    return;
                }
            }

            var cancelled = false;
            for (var i = items.Count - 1; i >= 0; i--)
            {
                if (items[i].CancelsWith(effect))
                {
                    items.RemoveAt(i);
                    cancelled = true;
                }
            }

            if (cancelled)
            {
                return;
            }

            items.Add(effect);
        }

        public bool Clear<T>() where T : StatusEffect
        {
            var removed = false;
            for (var i = items.Count - 1; i >= 0; i--)
            {
                if (items[i] is T)
                {
                    items.RemoveAt(i);
                    removed = true;
                }
            }

            return removed;
        }

        public T Find<T>(VolumeDominance dominance = null) where T : StatusEffect
        {
            T best = dominance != null ? dominance.FindStatus<T>() : null;

            for (var i = 0; i < items.Count; i++)
            {
                if (items[i] is not T match)
                {
                    continue;
                }

                if (best == null || Longevity(match) > Longevity(best))
                {
                    best = match;
                }
            }

            return best;
        }

        public int CountOf<T>(VolumeDominance dominance = null) where T : StatusEffect
        {
            var count = 0;
            if (dominance != null && dominance.FindStatus<T>() != null)
            {
                count++;
            }

            for (var i = 0; i < items.Count; i++)
            {
                if (items[i] is T)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Pulse FIFO. After each tick, <paramref name="afterEachPulse"/> may edit the queue
        /// (e.g. dominance insert); new entries pulse in the same pass when unprocessed.
        /// </summary>
        public void Pulse(Creature owner, Action afterEachPulse = null)
        {
            if (owner == null)
            {
                return;
            }

            var pulsed = new HashSet<StatusEffect>();
            while (pulsed.Count < items.Count + 8)
            {
                StatusEffect next = null;
                for (var i = 0; i < items.Count; i++)
                {
                    if (!pulsed.Contains(items[i]))
                    {
                        next = items[i];
                        break;
                    }
                }

                if (next == null)
                {
                    break;
                }

                pulsed.Add(next);
                if (!items.Contains(next))
                {
                    continue;
                }

                next.Tick(owner);
                if (next.Expired)
                {
                    items.Remove(next);
                }

                if (!owner.IsAlive)
                {
                    break;
                }

                afterEachPulse?.Invoke();
            }
        }

        /// <summary>
        /// Display / pick priority: innate &gt; Forever (dominance) &gt; timed Remaining.
        /// </summary>
        private static int Longevity(StatusEffect effect)
        {
            if (effect is InnateTrait)
            {
                return int.MaxValue;
            }

            if (effect.Permanent)
            {
                return int.MaxValue - 1;
            }

            return effect.Remaining;
        }
    }
}
