namespace SlimesRevenge
{
    public sealed class Instability : OverTime
    {
        public Instability(int duration = DefaultDuration)
            : base(duration) { }

        public override string Label => I18n.Get(TextKey.StatusInstability);

        public override string Description => I18n.Get(TextKey.StatusInstabilityDesc);

        public override int SpeedModifier => -1;
    }
}
