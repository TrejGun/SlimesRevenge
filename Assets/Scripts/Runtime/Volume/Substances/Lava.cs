using UnityEngine;

namespace SlimesRevenge
{
    public sealed class Lava : Substance
    {
        public override Color Color => new Color32(236, 96, 32, 255);

        public override string Label => I18n.Get(TextKey.SubstanceLava);

        public override void Apply(Creature target)
        {
            target?.AddStatus(new Burning());
        }
    }
}
