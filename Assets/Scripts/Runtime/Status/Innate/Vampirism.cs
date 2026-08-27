namespace SlimesRevenge
{
    /// <summary>
    /// Heal <see cref="HealAmount"/> when drinking blood: puddle/retort via
    /// <see cref="OnOwnerReceivedSubstance"/>, or melee when the volume tip struck is blood.
    /// </summary>
    public sealed class Vampirism : InnateTrait
    {
        public const int HealAmount = 2;

        public override string Label => I18n.Get(TextKey.StatusVampirism);

        public override string Description => I18n.Get(TextKey.StatusVampirismDesc);

        public override void OnOwnerReceivedSubstance(Creature owner, Substance substance)
        {
            if (substance is Blood)
            {
                Drink(owner);
            }
        }

        public override void OnOwnerStruckVolumeTip(Creature owner, Substance tip)
        {
            if (tip is Blood)
            {
                Drink(owner);
            }
        }

        private void Drink(Creature self)
        {
            if (self == null)
            {
                return;
            }

            var healed = self.Heal(HealAmount);
            if (healed <= 0 || !ActionLog.HasOpenGroup)
            {
                return;
            }

            LogMarker();
            ActionLog.DetailHeal(healed);
        }
    }
}
