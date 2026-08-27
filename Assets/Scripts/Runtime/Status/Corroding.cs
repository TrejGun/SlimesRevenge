namespace SlimesRevenge
{
    public sealed class Corroding : TickingHarm
    {
        private readonly int corrosion;

        public Corroding(int duration = OverTime.DefaultDuration, int corrosion = 1)
            : base(duration)
        {
            this.corrosion = corrosion < 0 ? 0 : corrosion;
        }

        public override int PulsePower => 1;

        /// <summary>Armor stripped each pulse while armor remains (from acid <see cref="Substance.Corrosion"/>).</summary>
        public int Corrosion => corrosion;

        public override string Label => I18n.Get(TextKey.StatusCorroding);

        public override string Description => I18n.Get(TextKey.StatusCorrodingDesc);

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
                var stripped = creature.StripArmor(Corrosion);
                ActionLog.DetailDamage(creature, stripped, applied: 0);
                return;
            }

            creature.Damage(PulsePowerFor(creature), out _, out var applied, blockedByArmor: false);
            ActionLog.DetailDamage(creature, armorStripped: 0, applied);
        }
    }
}
