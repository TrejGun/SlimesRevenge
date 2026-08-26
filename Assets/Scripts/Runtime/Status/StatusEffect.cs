namespace SlimesRevenge
{
    public abstract class StatusEffect
    {
        public const int Forever = -1;

        private bool delayPulse;

        protected StatusEffect(int duration, bool delayPulse = false)
        {
            Remaining = duration;
            this.delayPulse = delayPulse;
        }

        public int Remaining { get; private set; }

        public bool Permanent => Remaining == Forever;

        public bool Expired => !Permanent && Remaining <= 0;

        public abstract string Label { get; }

        public virtual int SpeedModifier => 0;

        public void Extend(int turns)
        {
            if (Permanent || turns <= 0)
            {
                return;
            }

            Remaining += turns;
        }

        /// <summary>
        /// Advances this effect by one creature-turn. Called only at the start of
        /// the owning creature's turn — never in a global pass for all creatures.
        /// </summary>
        public void Tick(Creature creature)
        {
            if (delayPulse)
            {
                delayPulse = false;
                return;
            }

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
