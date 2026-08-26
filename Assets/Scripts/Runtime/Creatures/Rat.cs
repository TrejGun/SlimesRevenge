namespace SlimesRevenge
{
    public sealed class Rat : Creature
    {
        public override CreaturePersonality Personality => CreaturePersonality.Cowardly;

        public static void FillStarting(Volume volume)
        {
            volume.Fill(new Blood());
        }

        private void Awake()
        {
            SetMaxHitPoints(3);
            if (Volume.UnitCount == 0)
            {
                FillStarting(Volume);
            }
        }
    }
}
