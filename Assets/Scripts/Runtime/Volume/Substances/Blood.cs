using UnityEngine;

namespace SlimesRevenge
{
    public sealed class Blood : Substance
    {
        public override Color Color => new Color32(204, 44, 52, 255);

        public override string Label => I18n.Get(TextKey.SubstanceBlood);

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
