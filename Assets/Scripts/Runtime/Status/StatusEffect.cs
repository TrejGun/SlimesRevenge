using UnityEngine;

namespace SlimesRevenge
{
    public abstract class StatusEffect
    {
        public const int Forever = -1;

        protected StatusEffect(int duration)
        {
            Remaining = duration;
        }

        public int Remaining { get; private set; }

        public bool Permanent => Remaining == Forever;

        public bool Expired => !Permanent && Remaining <= 0;

        /// <summary>Force a timed effect to expire at the end of the current pulse / clear.</summary>
        protected void ExpireNow()
        {
            if (!Permanent)
            {
                Remaining = 0;
            }
        }

        /// <summary>
        /// Called when this effect leaves the queue (expired, cleared, or cancelled).
        /// </summary>
        public virtual void OnRemoved(Creature owner) { }

        public abstract string Label { get; }

        /// <summary>Longer blurb for action-log / info popups.</summary>
        public abstract string Description { get; }

        /// <summary>UI icon (placeholder chip from <see cref="IconCatalog"/>).</summary>
        public virtual Sprite Icon => IconCatalog.Status(this);

        public virtual int SpeedModifier => 0;

        /// <summary>When true, removed from the status queue when volume dominance changes.</summary>
        public virtual bool ClearedWhenDominanceChanges => false;

        /// <summary>When true, <paramref name="incoming"/> is not applied to the owner.</summary>
        public virtual bool Blocks(StatusEffect incoming) => false;

        /// <summary>
        /// Mutual cancel with <paramref name="incoming"/>: this effect is removed and
        /// <paramref name="incoming"/> is not applied (e.g. <see cref="Wet"/> vs <see cref="Burning"/>).
        /// </summary>
        public virtual bool CancelsWith(StatusEffect incoming) => false;

        /// <summary>Hunt: this trait marks <paramref name="other"/> as prey.</summary>
        public virtual bool IsPrey(Creature other) => false;

        /// <summary>Hunt: this trait marks <paramref name="other"/> as a predator to flee.</summary>
        public virtual bool IsPredator(Creature other) => false;

        /// <summary>
        /// Path cost override for a floor substance. Null = no opinion.
        /// </summary>
        public virtual int? FloorPriorityOverride(Substance substance) => null;

        /// <summary>
        /// When true, turn-start pulse writes a status detail line before <see cref="OnPulse"/>.
        /// </summary>
        public virtual bool LogsPulse => false;

        /// <summary>
        /// When true, this effect is snapshotted before damage so it can still fire after
        /// dominance clears mid-hit (e.g. <see cref="Retaliation"/>).
        /// </summary>
        public virtual bool CapturesSurvivedHitReaction => false;

        /// <summary>
        /// Scale incoming non-armor harm (lava strike power, DoT pulse). Default: unchanged.
        /// </summary>
        public virtual int ModifyIncomingHarm(Creature owner, int amount) => amount;

        /// <summary>
        /// Owner received substance residue (puddle / retort / strike Apply).
        /// </summary>
        public virtual void OnOwnerReceivedSubstance(Creature owner, Substance substance) { }

        /// <summary>
        /// Owner survived a hit; <paramref name="attacker"/> may receive a retort.
        /// Prefer capturing the instance before damage (dominance can clear mid-hit).
        /// </summary>
        public virtual void OnOwnerSurvivedHit(Creature owner, Creature attacker) { }

        /// <summary>Owner landed a bare melee hit on <paramref name="target"/>.</summary>
        public virtual void OnOwnerDealtMeleeHit(Creature owner, Creature target) { }

        /// <summary>Owner knocked a volume tip off a slime with a strike.</summary>
        public virtual void OnOwnerStruckVolumeTip(Creature owner, Substance tip) { }

        /// <summary>Section marker in an open action-log group (clickable status link).</summary>
        protected void LogMarker()
        {
            if (!ActionLog.HasOpenGroup)
            {
                return;
            }

            ActionLog.Detail(
                new ActionLogLine(new[] { ActionLogPart.Status(StatusLogRef.From(this)) })
            );
        }

        /// <summary>
        /// Advances this effect by one creature-turn. Called only at the start of
        /// the owning creature's turn — never in a global pass for all creatures.
        /// Effects are pulsed in application order (FIFO: first applied, first pulsed).
        /// </summary>
        public void Tick(Creature creature)
        {
            if (ActionLog.HasOpenGroup && LogsPulse)
            {
                LogMarker();
            }

            OnPulse(creature);
            if (!Permanent && Remaining > 0)
            {
                Remaining--;
            }
        }

        protected virtual void OnPulse(Creature creature) { }
    }
}
