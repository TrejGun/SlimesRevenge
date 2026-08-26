namespace SlimesRevenge
{
    public sealed class Oiled : StatusEffect
    {
        public const int FireTurns = 3;

        public Oiled(int duration = 2) : base(duration)
        {
        }

        public override string Label => I18n.Get(TextKey.StatusOiled);
    }
}
