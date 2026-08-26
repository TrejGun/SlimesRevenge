namespace SlimesRevenge
{
    public static class Combat
    {
        /// <summary>Attack with a substance unit.</summary>
        public static bool Attack(Creature attacker, Creature target, Substance substance)
        {
            if (attacker == null || target == null || substance == null)
            {
                return false;
            }

            if (!attacker.Volume.TryRemove(substance))
            {
                return false;
            }

            if (attacker is Slime)
            {
                attacker.RefreshVolumeStatuses();
            }

            var power = substance.StrikePower(target);

            // Snapshot before Damage: volume hits can drop dominance and clear retaliation.
            var retaliation = target.FindStatus<Retaliation>();

            // Corrosion strips armor first; Power then faces remaining armor as flat DR.
            target.StripArmor(substance.Corrosion);
            target.Damage(power, out var tipStruck, blockedByArmor: true);

            if (target.IsAlive)
            {
                substance.Apply(target);
                retaliation?.Retort?.Apply(attacker);
            }

            attacker.FindStatus<Vampirism>()?.OnStrike(attacker, tipStruck);

            if (target is Slime)
            {
                target.RefreshVolumeStatuses();
            }

            return true;
        }

        /// <summary>Melee attack without a substance (creature melee / bare hit).</summary>
        public static bool Attack(Creature attacker, Creature target)
        {
            if (target == null || !target.IsAlive)
            {
                return false;
            }

            var retaliation = target.FindStatus<Retaliation>();
            target.Damage(1, out var tipStruck);
            if (target.IsAlive && attacker != null)
            {
                retaliation?.Retort?.Apply(attacker);
                attacker.FindStatus<Poisonous>()?.ApplyOnHit(target);
            }

            attacker?.FindStatus<Vampirism>()?.OnStrike(attacker, tipStruck);

            if (target is Slime)
            {
                target.RefreshVolumeStatuses();
            }

            return true;
        }
    }
}
