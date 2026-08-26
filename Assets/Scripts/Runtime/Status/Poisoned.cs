namespace SlimesRevenge
{
    public sealed class Poisoned : TickingHarm
    {
        public Poisoned(int duration = 3) : base(duration)
        {
        }

        public override string Label => I18n.Get(TextKey.StatusPoisoned);
    }
}
