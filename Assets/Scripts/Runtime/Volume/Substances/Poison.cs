using UnityEngine;

namespace SlimesRevenge
{
    public sealed class Poison : Substance
    {
        public override Color Color => new Color32(148, 72, 204, 255);

        public override string Label => I18n.Get(TextKey.SubstancePoison);

        public override void Apply(Creature target)
        {
            target?.AddStatus(new Poisoned());
        }
    }
}
