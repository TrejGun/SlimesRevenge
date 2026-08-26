namespace SlimesRevenge
{
    /// <summary>Flee from dogs in vision when otherwise idle/wandering.</summary>
    public sealed class FearsDogs : InnateTrait
    {
        public override string Label => I18n.Get(TextKey.StatusFearsDogs);

        public override bool IsPredator(Creature other) => other is Dog;
    }
}
