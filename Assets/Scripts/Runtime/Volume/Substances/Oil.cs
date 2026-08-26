using UnityEngine;

namespace SlimesRevenge
{
    public sealed class Oil : Substance
    {
        public override int FloorPriority => MildFloorPriority;

        public override Color Color => new Color32(168, 108, 36, 255);

        public override string Label => I18n.Get(TextKey.SubstanceOil);

        public override SlimeLook Look => SlimeLook.Oil;

        public override int AppearanceTieBreak => 3;

        public override Substance Clone() => new Oil();

        protected override void OnApply(Creature target)
        {
            target.AddStatus(new Instability());
            target.AddStatus(new Flammable());
        }

        public override T FindDominanceStatus<T>()
        {
            if (typeof(T) == typeof(Flammable))
            {
                return (T)(StatusEffect)new Flammable(StatusEffect.Forever);
            }

            return base.FindDominanceStatus<T>();
        }

        public override void ApplyDominance(Creature slime, System.Collections.Generic.IList<StatusEffect> queue)
        {
        }
    }
}
