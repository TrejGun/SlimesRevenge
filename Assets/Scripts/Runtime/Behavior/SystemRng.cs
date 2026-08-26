using System;

namespace SlimesRevenge
{
    public sealed class SystemRng : IRng
    {
        private readonly Random random = new Random();

        public int Pick(int count)
        {
            if (count <= 1)
            {
                return 0;
            }

            return random.Next(count);
        }

        public bool WanderRatherThanIdle()
        {
            return random.Next(2) == 0;
        }
    }
}
