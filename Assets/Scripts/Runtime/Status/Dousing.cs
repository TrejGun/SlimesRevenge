namespace SlimesRevenge
{
    public sealed class Dousing : StatusEffect
    {
        public Dousing() : base(1)
        {
        }

        public override string Label => I18n.Get(TextKey.StatusDousing);
    }
}
