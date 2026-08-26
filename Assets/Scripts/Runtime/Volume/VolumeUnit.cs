namespace SlimesRevenge
{
    public sealed class VolumeUnit
    {
        public VolumeUnit(Substance substance)
        {
            Substance = substance;
        }

        public Substance Substance { get; }
    }
}
