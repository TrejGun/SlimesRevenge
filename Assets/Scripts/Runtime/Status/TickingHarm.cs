namespace SlimesRevenge
{
    /// <summary>
    /// HP DoT that pulses once at the start of each of the victim's turns
    /// (see TurnManager creature/player turn start), for <see cref="StatusEffect.Remaining"/> turns.
    /// </summary>
    public abstract class TickingHarm : StatusEffect
    {
        protected TickingHarm(int duration = 3) : base(duration)
        {
        }

        protected virtual int PulseDamage(Creature creature) => 1;

        protected override void OnPulse(Creature creature)
        {
            creature?.Damage(PulseDamage(creature));
        }
    }
}
