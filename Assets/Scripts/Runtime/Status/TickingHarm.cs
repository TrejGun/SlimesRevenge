namespace SlimesRevenge
{
    /// <summary>
    /// HP damage-over-time: pulses once at the start of each of the victim's turns
    /// for <see cref="StatusEffect.Remaining"/> turns.
    /// Each subclass declares its own <see cref="PulsePower"/> per pulse.
    /// </summary>
    public abstract class TickingHarm : OverTime
    {
        protected TickingHarm(int duration = DefaultDuration)
            : base(duration) { }

        public override bool LogsPulse => true;

        /// <summary>Base HP removed each pulse (before status multipliers such as flammable).</summary>
        public abstract int PulsePower { get; }

        protected virtual int PulsePowerFor(Creature creature) => PulsePower;

        protected override void OnPulse(Creature creature)
        {
            if (creature == null)
            {
                return;
            }

            creature.Damage(PulsePowerFor(creature), out _, out var applied, blockedByArmor: false);
            ActionLog.DetailDamage(creature, armorStripped: 0, applied);
        }
    }
}
