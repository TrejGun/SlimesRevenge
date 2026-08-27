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

        public override string Description => I18n.Get(TextKey.StatusRetaliationDesc);

        public override bool CapturesSurvivedHitReaction => true;

        public override void OnOwnerSurvivedHit(Creature owner, Creature attacker)
        {
            ApplyRetort(attacker);
        }

        /// <summary>Log section + apply dominant substance residue to the attacker.</summary>
        private void ApplyRetort(Creature attacker)
        {
            if (Retort == null || attacker == null)
            {
                return;
            }

            if (ActionLog.HasOpenGroup)
            {
                LogMarker();
            }

            Retort.Apply(attacker);
        }
    }
}
