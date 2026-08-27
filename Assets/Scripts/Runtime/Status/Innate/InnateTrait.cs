namespace SlimesRevenge
{
    /// <summary>
    /// Innate creature status — one of three status lifecycles
    /// (<see cref="OverTime"/>, <see cref="BodyTrait"/>, <see cref="InnateTrait"/>).
    /// <para>
    /// <b>Applied:</b> at creature setup via <see cref="Creature.EnsureInnateTraits"/> /
    /// <see cref="Creature.SeedInnateTraits"/> (species personality, vampirism, poisonous, …).
    /// Always <see cref="StatusEffect.Forever"/>.
    /// </para>
    /// <para>
    /// <b>Removed:</b> not by timer, not by other status effects, and not by
    /// <see cref="Creature.RefreshVolumeStatuses"/>. Stays for the creature's lifetime;
    /// in practice it disappears only when the creature dies / is destroyed.
    /// </para>
    /// </summary>
    public abstract class InnateTrait : StatusEffect
    {
        protected InnateTrait()
            : base(Forever) { }
    }
}
