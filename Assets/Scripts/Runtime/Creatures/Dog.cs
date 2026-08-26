namespace SlimesRevenge
{
    public sealed class Dog : Creature
    {
        public override CreatureKind Kind => CreatureKind.Dog;

        public override CreaturePersonality Personality => CreaturePersonality.Aggressive;

        public static void FillStarting(Volume volume)
        {
            volume.Fill(new Blood(), new Blood(), new Blood());
        }

        private void Reset()
        {
            SetSpeed(2);
            SetMaxHitPoints(10);
        }

        private void Awake()
        {
            SetSpeed(2);
            SetMaxHitPoints(10);
            if (Volume.UnitCount == 0)
            {
                FillStarting(Volume);
            }

            EnsureInnateTraits();
        }

        protected override void SeedInnateTraits()
        {
            EnsureInnate<HatesCats>();
        }
    }
}
