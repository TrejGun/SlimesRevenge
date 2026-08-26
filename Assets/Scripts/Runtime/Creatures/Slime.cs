namespace SlimesRevenge
{
    public sealed class Slime : Creature
    {
        public override bool UsesVolumeAsShield => true;

        public static void FillStarting(Volume volume)
        {
            volume.Fill(new Water(), new Water(), new Oil(), new Poison(), new Acid(), new Lava());
        }

        private void Awake()
        {
            SetMaxHitPoints(1);
            if (Volume.UnitCount == 0)
            {
                FillStarting(Volume);
            }

            RefreshBodyTraits();
        }
    }
}
