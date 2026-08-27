using UnityEngine;

namespace SlimesRevenge
{
    public sealed class Lava : Substance, IDamageOverTime
    {
        public override int FloorPriority => DamageOverTimeFloorPriority;

        public override Color Color => new Color32(236, 96, 32, 255);

        public override string Label => I18n.Get(TextKey.SubstanceLava);

        public override SlimeLook Look => SlimeLook.Lava;

        public override int AppearanceTieBreak => 5;

        public override Substance Clone() => new Lava();

        public override void CollectApplyPreview(
            System.Collections.Generic.IList<StatusEffect> sink
        )
        {
            sink?.Add(new Burning());
        }

        public override int StrikePower(Creature target) =>
            target != null ? target.ModifyIncomingHarm(Power) : Power;

        protected override void OnApply(Creature target)
        {
            target.AddStatus(new Burning());
        }
    }
}
