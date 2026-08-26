using UnityEngine;

namespace SlimesRevenge
{
    public sealed class Oil : Substance
    {
        public override Color Color => new Color32(168, 108, 36, 255);

        public override string Label => I18n.Get(TextKey.SubstanceOil);

        public override void Apply(Creature target)
        {
            if (target == null)
            {
                return;
            }

            target.AddStatus(new Instability());
            target.AddStatus(new Oiled());
            for (var i = 0; i < target.Statuses.Count; i++)
            {
                if (target.Statuses[i] is Burning burning)
                {
                    burning.Extend(Oiled.FireTurns);
                }
            }
        }
    }
}
