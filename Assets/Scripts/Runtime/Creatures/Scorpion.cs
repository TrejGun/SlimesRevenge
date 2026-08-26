namespace SlimesRevenge
{
    public sealed class Scorpion : Creature
    {
        public override CreatureKind Kind => CreatureKind.Scorpion;

        public override CreaturePersonality Personality => CreaturePersonality.Passive;

        public static void FillStarting(Volume volume)
        {
            volume.Fill(new Acid());
        }

        private void Awake()
        {
            SetMaxHitPoints(3);
            SetArmor(1);
            if (Volume.UnitCount == 0)
            {
                FillStarting(Volume);
            }

            EnsureInnateTraits();
        }

        protected override void SeedInnateTraits()
        {
            EnsureInnate<Poisonous>();
        }
    }
}
