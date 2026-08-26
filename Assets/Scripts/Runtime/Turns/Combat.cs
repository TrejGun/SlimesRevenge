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
                attacker.RefreshBodyTraits();
            }

            var damage = 1;
            if (substance is Lava && target.FindStatus<Flammable>() != null)
            {
                damage = 2;
            }

            // Snapshot before Damage: volume shield can drop dominance and clear Retaliation.
            var retaliation = target.FindStatus<Retaliation>();
            target.Damage(damage);
            if (target.IsAlive)
            {
                substance.Apply(target);
                retaliation?.Retort?.Apply(attacker);
            }

            if (target is Slime)
            {
                target.RefreshBodyTraits();
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
            target.Damage(1);
            if (target.IsAlive && attacker != null)
            {
                retaliation?.Retort?.Apply(attacker);
            }

            if (target is Slime)
            {
                target.RefreshBodyTraits();
            }

            return true;
        }
    }
}
