namespace SlimesRevenge
{
    /// <summary>
    /// Oil residue: multiplies lava strike and <see cref="Burning"/> pulse by
    /// <see cref="DamageMultiplier"/>. Timed from puddles/attacks; Forever from oil dominance
    /// via <see cref="Oil.FindDominanceStatus{T}"/>.
    /// </summary>
    public sealed class Flammable : OverTime
    {
        public const int DamageMultiplier = 2;

        public Flammable(int duration = DefaultDuration)
            : base(duration) { }

        public override string Label => I18n.Get(TextKey.StatusFlammable);

        public override string Description => I18n.Get(TextKey.StatusFlammableDesc);

        public override int ModifyIncomingHarm(Creature owner, int amount)
        {
            if (amount <= 0)
            {
                return amount;
            }

            return amount * DamageMultiplier;
        }
    }
}
