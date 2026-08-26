namespace SlimesRevenge
{
    public sealed class Instability : StatusEffect
    {
        public Instability(int duration = 2) : base(duration)
        {
        }

        public override string Label => I18n.Get(TextKey.StatusInstability);

        public override int SpeedModifier => -1;
    }
}
