namespace SlimesRevenge
{
    /// <summary>While idle/wandering, hunt any rat-kind body in vision.</summary>
    public sealed class HatesRats : InnateTrait
    {
        public override string Label => I18n.Get(TextKey.StatusHatesRats);

        public override string Description => I18n.Get(TextKey.StatusHatesRatsDesc);

        public override bool IsPrey(Creature other) =>
            other != null && other.Kind == CreatureKind.Rat;
    }
}
