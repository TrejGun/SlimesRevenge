namespace SlimesRevenge
{
    public sealed class Corroding : TickingHarm
    {
        private readonly int corrosion;

        public Corroding(int duration = OverTime.DefaultDuration, int corrosion = 1) : base(duration)
        {
            this.corrosion = corrosion < 0 ? 0 : corrosion;
        }

        public override int PulsePower => 1;

        /// <summary>Armor stripped each pulse while armor remains (from acid <see cref="Substance.Corrosion"/>).</summary>
        public int Corrosion => corrosion;

        public override string Label => I18n.Get(TextKey.StatusCorroding);

        /// <summary>Incoming <see cref="Wet"/> washes acid off without applying wet.</summary>
        public override bool CancelsWith(StatusEffect incoming) => incoming is Wet;

        protected override void OnPulse(Creature creature)
        {
            if (creature == null)
            {
                return;
            }

            if (creature.Armor > 0)
            {
                creature.StripArmor(Corrosion);
                return;
            }

            creature.Damage(PulsePowerFor(creature), blockedByArmor: false);
        }
    }
}
