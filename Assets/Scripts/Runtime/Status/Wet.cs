namespace SlimesRevenge
{
    /// <summary>
    /// Wet from water: lasts <see cref="OverTime.DefaultDuration"/> turns.
    /// Mutual cancel with <see cref="Burning"/> (<see cref="CancelsWith"/> both ways);
    /// <see cref="Burning"/> / <see cref="Corroding"/> also cancel incoming Wet (extinguish without Wet).
    /// </summary>
    public sealed class Wet : OverTime
    {
        public Wet(int duration = DefaultDuration)
            : base(duration) { }

        public override string Label => I18n.Get(TextKey.StatusWet);

        public override string Description => I18n.Get(TextKey.StatusWetDesc);

        public override bool CancelsWith(StatusEffect incoming) => incoming is Burning;
    }
}
