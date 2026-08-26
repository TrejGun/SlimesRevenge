namespace SlimesRevenge
{
    /// <summary>Water puddles rank as high as DoT hazards for pathfinding.</summary>
    public sealed class FearsWater : InnateTrait
    {
        public override string Label => I18n.Get(TextKey.StatusFearsWater);

        public override int? FloorPriorityOverride(Substance substance)
        {
            return substance is Water ? Substance.DamageOverTimeFloorPriority : null;
        }
    }
}
