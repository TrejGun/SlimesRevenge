namespace SlimesRevenge
{
    /// <summary>Flee from slimes in vision when otherwise idle/wandering.</summary>
    public sealed class FearsSlimes : InnateTrait
    {
        public override string Label => I18n.Get(TextKey.StatusFearsSlimes);

        public override bool IsPredator(Creature other) => other is Slime;
    }
}
