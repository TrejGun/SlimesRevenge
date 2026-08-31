namespace SlimesRevenge
{
    /// <summary>Flee from dogs in vision when otherwise idle/wandering.</summary>
    public sealed class FearsDogs : InnateTrait
    {
        public override string Label => I18n.Get(TextKey.StatusFearsDogs);

        public override string Description => I18n.Get(TextKey.StatusFearsDogsDesc);

        public override bool IsPredator(Creature other) =>
            other != null && other.Kind == CreatureKind.Dog;
    }
}
