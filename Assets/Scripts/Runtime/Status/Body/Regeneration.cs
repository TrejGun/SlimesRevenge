namespace SlimesRevenge
{
    /// <summary>
    /// Blood-dominance pulse status: inserted at the <b>front</b> of the status queue when
    /// blood becomes dominant. Pulses FIFO with other statuses (+1 <see cref="Blood"/> while
    /// under <see cref="Volume.Capacity"/>). Removed when dominance leaves blood.
    /// Does not grow on apply — only on <see cref="OnPulse"/>.
    /// </summary>
    public sealed class Regeneration : BodyTrait
    {
        public override string Label => I18n.Get(TextKey.StatusRegeneration);

        public override string Description => I18n.Get(TextKey.StatusRegenerationDesc);

        public override bool ClearedWhenDominanceChanges => true;

        public override bool LogsPulse => true;

        protected override void OnPulse(Creature creature)
        {
            TryGrow(creature);
        }

        /// <summary>Add one blood if under capacity. Does not refresh statuses.</summary>
        public static void TryGrow(Creature slime)
        {
            if (slime?.Volume == null || slime.Volume.UnitCount >= Volume.Capacity)
            {
                return;
            }

            var blood = new Blood();
            slime.Volume.Add(blood);
            if (ActionLog.HasOpenGroup)
            {
                ActionLog.Detail(
                    ActionLogLine.FormatKey(TextKey.LogGrows, ActionLogPart.Substance(blood))
                );
            }
        }
    }
}
