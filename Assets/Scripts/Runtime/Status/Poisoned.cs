namespace SlimesRevenge
{
    public sealed class Poisoned : TickingHarm
    {
        public Poisoned(int duration = OverTime.DefaultDuration) : base(duration)
        {
        }

        public override int PulsePower => 1;

        public override string Label => I18n.Get(TextKey.StatusPoisoned);
    }
}
