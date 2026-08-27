using UnityEngine;

namespace SlimesRevenge
{
    public sealed class Poison : Substance, IDamageOverTime
    {
        public override int FloorPriority => DamageOverTimeFloorPriority;

        public override Color Color => new Color32(148, 72, 204, 255);

        public override string Label => I18n.Get(TextKey.SubstancePoison);

        public override SlimeLook Look => SlimeLook.Poison;

        public override int AppearanceTieBreak => 1;

        public override Substance Clone() => new Poison();

        public override void CollectApplyPreview(
            System.Collections.Generic.IList<StatusEffect> sink
        )
        {
            sink?.Add(new Poisoned());
        }

        protected override void OnApply(Creature target)
        {
            target.AddStatus(new Poisoned());
        }
    }
}
