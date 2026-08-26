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

        public abstract string Label { get; }

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
        /// Advances this effect by one creature-turn. Called only at the start of
        /// the owning creature's turn — never in a global pass for all creatures.
        /// Effects are pulsed in application order (FIFO: first applied, first pulsed).
        /// </summary>
        public void Tick(Creature creature)
        {
            OnPulse(creature);
            if (!Permanent && Remaining > 0)
            {
                Remaining--;
            }
        }

        protected virtual void OnPulse(Creature creature)
        {
        }
    }
}
