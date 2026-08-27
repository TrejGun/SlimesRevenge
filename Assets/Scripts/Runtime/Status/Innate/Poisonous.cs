namespace SlimesRevenge
{
    /// <summary>
    /// Immune to <see cref="Poisoned"/>. Melee hits apply <see cref="Poisoned"/> to the target.
    /// Poison puddles have zero path cost.
    /// </summary>
    public sealed class Poisonous : InnateTrait
    {
        public override string Label => I18n.Get(TextKey.StatusPoisonous);

        public override string Description => I18n.Get(TextKey.StatusPoisonousDesc);

        public override bool Blocks(StatusEffect incoming) => incoming is Poisoned;

        public override int? FloorPriorityOverride(Substance substance)
        {
            return substance is Poison ? 0 : null;
        }

        public override void OnOwnerDealtMeleeHit(Creature owner, Creature target)
        {
            if (target == null)
            {
                return;
            }

            LogMarker();
            target.AddStatus(new Poisoned());
        }
    }
}
