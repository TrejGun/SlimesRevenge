namespace SlimesRevenge
{
    /// <summary>
    /// Heal <see cref="HealAmount"/> when drinking blood: puddle/retort via <see cref="Blood.Apply"/>,
    /// or melee when the volume tip struck is blood (<see cref="OnStrike"/>).
    /// </summary>
    public sealed class Vampirism : InnateTrait
    {
        public const int HealAmount = 2;

        public override string Label => I18n.Get(TextKey.StatusVampirism);

        public void Drink(Creature self)
        {
            self?.Heal(HealAmount);
        }

        /// <summary>Same heal when the tip unit knocked off by a strike was blood.</summary>
        public void OnStrike(Creature self, Substance tipStruck)
        {
            if (tipStruck is Blood)
            {
                Drink(self);
            }
        }
    }
}
