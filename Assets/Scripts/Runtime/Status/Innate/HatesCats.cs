namespace SlimesRevenge
{
    /// <summary>While idle/wandering, hunt any cat-kind body in vision.</summary>
    public sealed class HatesCats : InnateTrait
    {
        public override string Label => I18n.Get(TextKey.StatusHatesCats);

        public override string Description => I18n.Get(TextKey.StatusHatesCatsDesc);

        public override bool IsPrey(Creature other) =>
            other != null && other.Kind == CreatureKind.Cat;
    }
}
