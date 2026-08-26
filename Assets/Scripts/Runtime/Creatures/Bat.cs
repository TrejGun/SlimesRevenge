namespace SlimesRevenge
{
    public sealed class Bat : Creature
    {
        public override CreatureKind Kind => CreatureKind.Bat;

        public override CreaturePersonality Personality => CreaturePersonality.Passive;

        public static void FillStarting(Volume volume)
        {
            volume.Fill(new Blood(), new Blood());
        }

        private void Awake()
        {
            SetMaxHitPoints(3);
            if (Volume.UnitCount == 0)
            {
                FillStarting(Volume);
            }

            EnsureInnateTraits();
        }

        protected override void SeedInnateTraits()
        {
            EnsureInnate<Vampirism>();
        }
    }
}
