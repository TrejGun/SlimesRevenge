namespace SlimesRevenge
{
    /// <summary>
    /// Timed (or Forever) status — one of three lifecycles
    /// (<see cref="OverTime"/>, <see cref="BodyTrait"/>, <see cref="InnateTrait"/>).
    /// <para>
    /// <b>Applied:</b> gameplay (hit/puddle) with <see cref="DefaultDuration"/>, or Forever from
    /// volume dominance (e.g. oil <see cref="Flammable"/> via <see cref="Substance.ApplyDominance"/>).
    /// </para>
    /// <para>
    /// <b>Removed:</b> timed — <see cref="Creature.RefreshStatuses"/> when expired / cancel rules;
    /// Forever — <see cref="Creature.RefreshVolumeStatuses"/> when dominance changes.
    /// Timed and Forever instances of the same type may coexist;
    /// <see cref="Creature.FindStatus{T}"/> returns the longest (innate &gt; Forever &gt; timer).
    /// </para>
    /// </summary>
    public abstract class OverTime : StatusEffect
    {
        public const int DefaultDuration = 3;

        protected OverTime(int duration = DefaultDuration)
            : base(duration) { }
    }
}
