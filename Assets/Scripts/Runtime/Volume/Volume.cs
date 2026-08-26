using System;
using System.Collections.Generic;

namespace SlimesRevenge
{
    [Serializable]
    public sealed class Volume
    {
        public const int Capacity = 10;

        private readonly List<VolumeUnit> units = new List<VolumeUnit>();

        public int UnitCount => units.Count;

        public IReadOnlyList<VolumeUnit> Units => units;

        public void Add(Substance substance)
        {
            if (units.Count >= Capacity || substance == null)
            {
                return;
            }

            units.Add(new VolumeUnit(substance));
        }

        public void Fill(params Substance[] substances)
        {
            Clear();
            foreach (var substance in substances)
            {
                Add(substance);
            }
        }

        public bool TryReplace<TFrom>(Substance into) where TFrom : Substance
        {
            if (into == null)
            {
                return false;
            }

            for (var i = 0; i < units.Count; i++)
            {
                if (units[i].Substance is TFrom)
                {
                    units[i] = new VolumeUnit(into);
                    return true;
                }
            }

            return false;
        }

        public bool TryRemove(Substance kind)
        {
            if (kind == null)
            {
                return false;
            }

            var type = kind.GetType();
            for (var i = 0; i < units.Count; i++)
            {
                if (units[i].Substance.GetType() == type)
                {
                    units.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Removes the newest matching unit (search from the stack end). Unlike
        /// <see cref="TryRemove"/>, prefers the most recently added match even when
        /// something else sits on top.
        /// </summary>
        public bool TryRemoveLast<T>() where T : Substance
        {
            for (var i = units.Count - 1; i >= 0; i--)
            {
                if (units[i].Substance is T)
                {
                    units.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Stack pop: newest units first (LIFO). Used by combat shield damage and digestion.
        /// <see cref="Add"/> appends, so the most recently gained matter is lost or digested first.
        /// </summary>
        /// <param name="removed">Optional: each popped substance, tip-first (same order as hits land).</param>
        public int Damage(int amount, ICollection<Substance> removed = null)
        {
            if (amount <= 0)
            {
                return 0;
            }

            var count = 0;
            while (count < amount && units.Count > 0)
            {
                var substance = units[units.Count - 1].Substance;
                units.RemoveAt(units.Count - 1);
                removed?.Add(substance);
                count++;
            }

            return count;
        }

        /// <summary>Peek and remove the newest unit (same end as <see cref="Damage"/>).</summary>
        public bool TryTakeEnd(out Substance substance)
        {
            substance = null;
            if (units.Count == 0)
            {
                return false;
            }

            substance = units[units.Count - 1].Substance;
            units.RemoveAt(units.Count - 1);
            return true;
        }

        public int CountOf<T>() where T : Substance
        {
            var count = 0;
            foreach (var unit in units)
            {
                if (unit.Substance is T)
                {
                    count++;
                }
            }

            return count;
        }

        public int CountOf(Type kind)
        {
            if (kind == null)
            {
                return 0;
            }

            var count = 0;
            foreach (var unit in units)
            {
                if (unit.Substance != null && unit.Substance.GetType() == kind)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Minimum share of units (percent) one substance needs for volume dominance.
        /// Example: 8 of 10 = 80% qualifies; 7 of 9 ≈ 77% does not.
        /// </summary>
        public const int DominantPercent = 80;

        public bool TryDominant(out Substance sample)
        {
            sample = null;
            if (units.Count == 0)
            {
                return false;
            }

            var counts = new Dictionary<Type, int>();
            Substance best = null;
            var bestCount = 0;
            foreach (var unit in units)
            {
                if (unit.Substance == null)
                {
                    continue;
                }

                var type = unit.Substance.GetType();
                counts.TryGetValue(type, out var count);
                count++;
                counts[type] = count;
                if (count > bestCount)
                {
                    bestCount = count;
                    best = unit.Substance;
                }
            }

            if (best == null || bestCount * 100 < units.Count * DominantPercent)
            {
                return false;
            }

            sample = best;
            return true;
        }

        public Volume Clone()
        {
            var copy = new Volume();
            foreach (var unit in units)
            {
                copy.Add(unit.Substance?.Clone());
            }

            return copy;
        }

        public IReadOnlyList<Substance> UniqueKinds()
        {
            var kinds = new List<Substance>();
            var seen = new HashSet<Type>();
            foreach (var unit in units)
            {
                if (unit.Substance != null && seen.Add(unit.Substance.GetType()))
                {
                    kinds.Add(unit.Substance);
                }
            }

            return kinds;
        }

        public void Clear()
        {
            units.Clear();
        }

        /// <summary>Null-safe wrapper around <see cref="Substance.Clone"/>.</summary>
        public static Substance CloneSubstance(Substance substance)
        {
            return substance?.Clone();
        }
    }
}
