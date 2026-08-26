namespace SlimesRevenge
{
    public sealed class Retaliation : BodyTrait
    {
        public Retaliation(Substance retort)
        {
            Retort = retort;
        }

        public Substance Retort { get; }

        public override string Label => I18n.Get(TextKey.StatusRetaliation);
    }
}
