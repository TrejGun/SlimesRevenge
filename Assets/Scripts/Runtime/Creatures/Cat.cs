namespace SlimesRevenge
{
    public sealed class Cat : Creature
    {
        public override CreaturePersonality Personality => CreaturePersonality.Passive;

        public static void FillStarting(Volume volume)
        {
            volume.Fill(new Blood(), new Blood());
        }

        private void Awake()
        {
            SetMaxHitPoints(5);
            if (Volume.UnitCount == 0)
            {
                FillStarting(Volume);
            }
        }
    }
}
