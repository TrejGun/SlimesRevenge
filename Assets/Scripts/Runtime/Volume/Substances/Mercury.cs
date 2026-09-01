using UnityEngine;

namespace SlimesRevenge
{
    public sealed class Mercury : Substance
    {
        public override int FloorPriority => MildFloorPriority;

        public override Color Color => new Color32(176, 192, 208, 255);

        public override string Label => I18n.Get(TextKey.SubstanceMercury);

        public override SlimeLook Look => SlimeLook.Mercury;

        public override int AppearanceTieBreak => 6;

        public override Substance Clone() => new Mercury();

        public override T FindDominanceStatus<T>()
        {
            if (typeof(T) == typeof(Invisible))
            {
                return (T)(StatusEffect)new Invisible();
            }

            return base.FindDominanceStatus<T>();
        }

        public override void CollectDominancePassives(
            System.Collections.Generic.IList<StatusEffect> sink
        )
        {
            if (sink == null)
            {
                return;
            }

            var invisible = FindDominanceStatus<Invisible>();
            if (invisible != null)
            {
                sink.Add(invisible);
            }

            base.CollectDominancePassives(sink);
        }
    }
}
