namespace SlimesRevenge
{
    public sealed class Fireproof : BodyTrait
    {
        public override string Label => I18n.Get(TextKey.StatusFireproof);

        public override bool Blocks(StatusEffect incoming) => incoming is Burning;
    }
}
