using UnityEngine;

namespace SlimesRevenge
{
    public sealed class Water : Substance
    {
        public override int FloorPriority => MildFloorPriority;

        public override Color Color => new Color32(64, 168, 236, 255);

        public override string Label => I18n.Get(TextKey.SubstanceWater);

        public override SlimeLook Look => SlimeLook.Water;

        public override int AppearanceTieBreak => 0;

        public override Substance Clone() => new Water();

        protected override void OnApply(Creature target)
        {
            // Always try Wet; Burning/Corroding CancelsWith(Wet) extinguish without applying Wet.
            target.AddStatus(new Wet());
        }

        public override bool DominanceBlocks(StatusEffect incoming) => incoming is Burning;

        public override T FindDominanceStatus<T>()
        {
            if (typeof(T) == typeof(Fireproof))
            {
                return (T)(StatusEffect)new Fireproof();
            }

            return base.FindDominanceStatus<T>();
        }

        public override void ApplyDominance(Creature slime, System.Collections.Generic.IList<StatusEffect> queue)
        {
            if (queue == null)
            {
                return;
            }

            for (var i = queue.Count - 1; i >= 0; i--)
            {
                if (queue[i] is Burning)
                {
                    queue.RemoveAt(i);
                }
            }
        }
    }
}
