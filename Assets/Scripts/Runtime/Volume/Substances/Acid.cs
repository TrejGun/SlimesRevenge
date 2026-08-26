using UnityEngine;

namespace SlimesRevenge
{
    public sealed class Acid : Substance
    {
        public override Color Color => new Color32(204, 228, 48, 255);

        public override string Label => I18n.Get(TextKey.SubstanceAcid);

        public override void Apply(Creature target)
        {
            target?.AddStatus(new Corroding());
        }
    }
}
