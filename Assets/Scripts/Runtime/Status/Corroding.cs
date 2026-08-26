namespace SlimesRevenge
{
    public sealed class Corroding : TickingHarm
    {
        public Corroding(int duration = 3) : base(duration)
        {
        }

        public override string Label => I18n.Get(TextKey.StatusCorroding);
    }
}
