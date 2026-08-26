using UnityEngine;

namespace SlimesRevenge
{
    public abstract class Substance
    {
        public abstract Color Color { get; }

        public abstract string Label { get; }

        public virtual int Damage => 1;

        public virtual void Apply(Creature target)
        {
        }
    }
}
