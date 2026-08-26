namespace SlimesRevenge
{
    /// <summary>
    /// Oil residue: multiplies lava strike and <see cref="Burning"/> pulse by
    /// <see cref="DamageMultiplier"/>. Timed from puddles/attacks; Forever from oil dominance
    /// via <see cref="Oil.FindDominanceStatus{T}"/>. Amplification stays here — not on Creature
    /// or abstract <see cref="Substance"/>.
    /// </summary>
    public sealed class Flammable : OverTime
    {
        public const int DamageMultiplier = 2;

        public Flammable(int duration = DefaultDuration) : base(duration)
        {
        }

        public override string Label => I18n.Get(TextKey.StatusFlammable);

        /// <summary>Apply oil/flammable bonus if the creature has this status (timed or Forever).</summary>
        public static int Amplify(Creature creature, int amount)
        {
            if (amount <= 0 || creature?.FindStatus<Flammable>() == null)
            {
                return amount;
            }

            return amount * DamageMultiplier;
        }
    }
}
