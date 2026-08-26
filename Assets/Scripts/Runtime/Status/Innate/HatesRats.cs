namespace SlimesRevenge
{
    /// <summary>While idle/wandering, hunt any <see cref="Rat"/> in vision.</summary>
    public sealed class HatesRats : InnateTrait
    {
        public override string Label => I18n.Get(TextKey.StatusHatesRats);

        public override bool IsPrey(Creature other) => other is Rat;
    }
}
