using UnityEngine;

namespace SlimesRevenge
{
    public sealed class Blood : Substance
    {
        public override int FloorPriority => MildFloorPriority;

        public override Color Color => new Color32(204, 44, 52, 255);

        public override string Label => I18n.Get(TextKey.SubstanceBlood);

        public override SlimeLook Look => SlimeLook.Blood;

        public override int AppearanceTieBreak => 4;

        public override Substance Clone() => new Blood();

        protected override void OnApply(Creature target)
        {
            target.FindStatus<Vampirism>()?.Drink(target);
        }

        public override void ApplyDominance(Creature slime, System.Collections.Generic.IList<StatusEffect> queue)
        {
            if (queue == null)
            {
                return;
            }

            for (var i = queue.Count - 1; i >= 0; i--)
            {
                if (queue[i] is Regeneration)
                {
                    queue.RemoveAt(i);
                }
            }

            queue.Insert(0, new Regeneration());
        }
    }
}
