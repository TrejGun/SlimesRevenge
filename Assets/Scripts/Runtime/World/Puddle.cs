namespace SlimesRevenge
{
    public sealed class Puddle
    {
        public Puddle(Substance substance)
        {
            Substance = substance;
        }

        public Substance Substance { get; }
    }
}
