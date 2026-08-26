namespace SlimesRevenge
{
    /// <summary>Flee from cats in vision when otherwise idle/wandering.</summary>
    public sealed class FearsCats : InnateTrait
    {
        public override string Label => I18n.Get(TextKey.StatusFearsCats);

        public override bool IsPredator(Creature other) => other is Cat;
    }
}
