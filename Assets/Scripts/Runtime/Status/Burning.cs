namespace SlimesRevenge
{
    /// <summary>
    /// Timed fire DoT. Pulses in FIFO application order with other statuses; after each pulse
    /// the slime volume is refreshed so the next effect sees updated dominance.
    /// Amplification is owned by <see cref="Flammable"/> via
    /// <see cref="Creature.ModifyIncomingHarm"/> — not named here.
    /// </summary>
    public sealed class Burning : TickingHarm
    {
        public Burning(int duration = OverTime.DefaultDuration)
            : base(duration) { }

        public override int PulsePower => 1;

        public override string Label => I18n.Get(TextKey.StatusBurning);

        public override string Description => I18n.Get(TextKey.StatusBurningDesc);

        /// <summary>Incoming <see cref="Wet"/> extinguishes fire without applying wet.</summary>
        public override bool CancelsWith(StatusEffect incoming) => incoming is Wet;

        protected override int PulsePowerFor(Creature creature) =>
            creature != null ? creature.ModifyIncomingHarm(PulsePower) : PulsePower;
    }
}
