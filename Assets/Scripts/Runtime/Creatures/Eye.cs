namespace SlimesRevenge
{
    public sealed class Eye : Creature
    {
        public override CreatureKind Kind => CreatureKind.Eye;

        public static void FillStarting(Volume volume)
        {
            volume.Fill(new Water());
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
