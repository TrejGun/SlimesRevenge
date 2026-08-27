namespace SlimesRevenge
{
    /// <summary>While idle/wandering, hunt any <see cref="Cat"/> in vision.</summary>
    public sealed class HatesCats : InnateTrait
    {
        public override string Label => I18n.Get(TextKey.StatusHatesCats);

        public override string Description => I18n.Get(TextKey.StatusHatesCatsDesc);

        public override bool IsPrey(Creature other) => other is Cat;
    }
}
