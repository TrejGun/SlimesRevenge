namespace SlimesRevenge
{
    /// <summary>Flee from cats in vision when otherwise idle/wandering.</summary>
    public sealed class FearsCats : InnateTrait
    {
        public override string Label => I18n.Get(TextKey.StatusFearsCats);

        public override string Description => I18n.Get(TextKey.StatusFearsCatsDesc);

        public override bool IsPredator(Creature other) =>
            other != null && other.Kind == CreatureKind.Cat;
    }
}
