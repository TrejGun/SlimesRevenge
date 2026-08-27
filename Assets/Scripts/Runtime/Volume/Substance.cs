using UnityEngine;

namespace SlimesRevenge
{
    public abstract class Substance
    {
        /// <summary>
        /// Path ranking for floor puddles. Higher = more hazardous; A* prefers lower when routes tie.
        /// </summary>
        public virtual int FloorPriority => 0;

        /// <summary>Shared rank for mild puddles (water, blood, oil). Empty cell is 0.</summary>
        public const int MildFloorPriority = 10;

        /// <summary>Shared high rank for DoT puddles (and cat fear of water).</summary>
        public const int DamageOverTimeFloorPriority = 100;

        /// <summary>
        /// Walker-aware floor ranking via status <see cref="StatusEffect.FloorPriorityOverride"/>.
        /// </summary>
        public virtual int FloorPriorityFor(Creature walker)
        {
            if (walker == null)
            {
                return FloorPriority;
            }

            var list = walker.Statuses;
            for (var i = 0; i < list.Count; i++)
            {
                var value = list[i].FloorPriorityOverride(this);
                if (value != null)
                {
                    return value.Value;
                }
            }

            return FloorPriority;
        }

        public abstract Color Color { get; }

        public abstract string Label { get; }

        /// <summary>UI icon (placeholder blob from <see cref="IconCatalog"/>).</summary>
        public virtual Sprite Icon => IconCatalog.Substance(this);

        /// <summary>Slime body look when this substance wins plurality in the volume.</summary>
        public abstract SlimeLook Look { get; }

        /// <summary>
        /// Tie-break when two looks share the same unit count. Lower wins.
        /// Order: water → poison → acid → oil → blood → lava.
        /// </summary>
        public abstract int AppearanceTieBreak { get; }

        /// <summary>Independent copy of this substance kind (for puddles, retaliation, volume clone).</summary>
        public abstract Substance Clone();

        public virtual int Power => 1;

        /// <summary>
        /// Armor units stripped on strike (and used by <see cref="Corroding"/> pulses).
        /// Independent of <see cref="Power"/> — both can apply on the same hit.
        /// </summary>
        public virtual int Corrosion => 0;

        /// <summary>Volume units removed / HP dealt when this substance is used in a strike.</summary>
        public virtual int StrikePower(Creature target) => Power;

        /// <summary>Dominance: block applying this status while this substance is dominant.</summary>
        public virtual bool DominanceBlocks(StatusEffect incoming) => false;

        /// <summary>
        /// Passive statuses provided while this substance is volume-dominant (for lookups).
        /// Creature looks up via this — it does not name Fireproof / Flammable / etc.
        /// </summary>
        public virtual T FindDominanceStatus<T>()
            where T : StatusEffect
        {
            if (typeof(T) == typeof(Retaliation))
            {
                return (T)(StatusEffect)new Retaliation(Clone());
            }

            return null;
        }

        /// <summary>
        /// Snapshot hit-reactions that must run even if dominance clears mid-strike.
        /// </summary>
        public virtual void CaptureSurvivedHitReactions(
            System.Collections.Generic.IList<StatusEffect> sink
        )
        {
            if (sink == null)
            {
                return;
            }

            var passives = new System.Collections.Generic.List<StatusEffect>(4);
            CollectDominancePassives(passives);
            for (var i = 0; i < passives.Count; i++)
            {
                if (passives[i].CapturesSurvivedHitReaction)
                {
                    sink.Add(passives[i]);
                }
            }
        }

        /// <summary>
        /// Passive dominance traits to show when this substance becomes dominant (log / UI).
        /// </summary>
        public virtual void CollectDominancePassives(
            System.Collections.Generic.IList<StatusEffect> sink
        )
        {
            if (sink == null)
            {
                return;
            }

            var retaliation = FindDominanceStatus<Retaliation>();
            if (retaliation != null)
            {
                sink.Add(retaliation);
            }
        }

        /// <summary>Write dominance-passive gains into an open action-log group.</summary>
        public void LogDominanceGains(Creature owner)
        {
            if (!ActionLog.HasOpenGroup || owner == null)
            {
                return;
            }

            var passives = new System.Collections.Generic.List<StatusEffect>(4);
            CollectDominancePassives(passives);
            for (var i = 0; i < passives.Count; i++)
            {
                ActionLog.DetailGains(owner.Kind, passives[i]);
            }
        }

        /// <summary>
        /// Apply this substance onto <paramref name="target"/> (puddle, retaliation, strike effect).
        /// Puddles never deal strike damage — only this residue.
        /// </summary>
        public void Apply(Creature target)
        {
            if (target == null)
            {
                return;
            }

            OnApply(target);
            target.NotifyReceivedSubstance(this);
        }

        /// <summary>Contact / strike residue effect on the target.</summary>
        protected virtual void OnApply(Creature target) { }

        /// <summary>
        /// Statuses this substance would apply via <see cref="OnApply"/> (for UI cards).
        /// Must not mutate the world — preview instances only.
        /// </summary>
        public virtual void CollectApplyPreview(
            System.Collections.Generic.IList<StatusEffect> sink
        ) { }

        /// <summary>
        /// Volume dominance sync: edit the slime status <paramref name="queue"/> in place.
        /// Passive bonuses come from <see cref="FindDominanceStatus{T}"/> / <see cref="DominanceBlocks"/>.
        /// Default: queue unchanged.
        /// </summary>
        public virtual void ApplyDominance(
            Creature slime,
            System.Collections.Generic.IList<StatusEffect> queue
        ) { }
    }
}
