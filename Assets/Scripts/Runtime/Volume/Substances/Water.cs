using UnityEngine;

namespace SlimesRevenge
{
    public sealed class Water : Substance
    {
        public override Color Color => new Color32(64, 168, 236, 255);

        public override string Label => I18n.Get(TextKey.SubstanceWater);

        public override void Apply(Creature target)
        {
            if (target == null)
            {
                return;
            }

            target.Extinguish();
            target.AddStatus(new Dousing());
        }
    }
}
