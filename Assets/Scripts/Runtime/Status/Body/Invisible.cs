namespace SlimesRevenge
{
    /// <summary>
    /// Mercury dominance: mobs ignore the slime until they are aggroed (hit or drawn into combat).
    /// </summary>
    public sealed class Invisible : BodyTrait
    {
        public override string Label => I18n.Get(TextKey.StatusInvisible);

        public override string Description => I18n.Get(TextKey.StatusInvisibleDesc);

        public override bool BlocksDetectionFrom(Creature observer, Creature target)
        {
            return observer != null && observer != target;
        }
    }
}
