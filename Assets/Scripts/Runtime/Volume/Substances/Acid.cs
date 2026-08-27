using UnityEngine;

namespace SlimesRevenge
{
    public sealed class Acid : Substance, IDamageOverTime
    {
        public override int FloorPriority => DamageOverTimeFloorPriority;

        public override Color Color => new Color32(204, 228, 48, 255);

        public override string Label => I18n.Get(TextKey.SubstanceAcid);

        public override SlimeLook Look => SlimeLook.Acid;

        public override int AppearanceTieBreak => 2;

        public override Substance Clone() => new Acid();

        public override void CollectApplyPreview(
            System.Collections.Generic.IList<StatusEffect> sink
        )
        {
            sink?.Add(new Corroding(corrosion: Corrosion));
        }

        /// <summary>Strike and <see cref="Corroding"/> strip this many armor per hit/pulse.</summary>
        public override int Corrosion => 1;

        protected override void OnApply(Creature target)
        {
            target.AddStatus(new Corroding(corrosion: Corrosion));
        }
    }
}
