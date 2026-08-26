namespace SlimesRevenge
{
    public sealed class Burning : TickingHarm
    {
        public Burning(int duration = 3) : base(duration)
        {
        }

        public override string Label => I18n.Get(TextKey.StatusBurning);

        protected override int PulseDamage(Creature creature)
        {
            return creature != null && creature.FindStatus<Flammable>() != null ? 2 : 1;
        }
    }
}
