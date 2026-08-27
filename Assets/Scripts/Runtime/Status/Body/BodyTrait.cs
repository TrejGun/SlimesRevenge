namespace SlimesRevenge
{
    /// <summary>
    /// Dominance-owned pulse marker (e.g. <see cref="Regeneration"/> in the status queue).
    /// Passive dominance bonuses are supplied by <see cref="Substance.FindDominanceStatus{T}"/> —
    /// <see cref="Creature"/> does not name Fireproof / Flammable.
    /// </summary>
    public abstract class BodyTrait : StatusEffect
    {
        protected BodyTrait()
            : base(Forever) { }
    }
}
