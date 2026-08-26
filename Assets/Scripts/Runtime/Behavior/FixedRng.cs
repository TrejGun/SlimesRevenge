using UnityEngine;

namespace SlimesRevenge
{
    public sealed class FixedRng : IRng
    {
        private readonly int index;
        private readonly bool wander;

        public FixedRng(int index = 0, bool wander = false)
        {
            this.index = index;
            this.wander = wander;
        }

        public int Pick(int count)
        {
            if (count <= 1)
            {
                return 0;
            }

            return Mathf.Clamp(index, 0, count - 1);
        }

        public bool WanderRatherThanIdle()
        {
            return wander;
        }
    }
}
