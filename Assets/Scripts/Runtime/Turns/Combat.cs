using System.Collections.Generic;

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

            attacker.RefreshVolumeStatuses();
            CreatureSpriteAnimator.PlayAttack(attacker, target.Cell);

            ActionLog.BeginKey(
                TextKey.LogHitWith,
                ActionLogPart.Creature(attacker.Kind),
                ActionLogPart.Creature(target.Kind),
                ActionLogPart.Substance(substance)
            );
            try
            {
                ResolveHit(
                    attacker,
                    target,
                    power: substance.StrikePower(target),
                    corrosion: substance.Corrosion,
                    afterDamageAlive: () => substance.Apply(target),
                    afterMelee: null
                );
                return true;
            }
            finally
            {
                ActionLog.End();
            }
        }

        /// <summary>Melee attack without a substance (creature melee / bare hit).</summary>
        public static bool Attack(Creature attacker, Creature target)
        {
            if (target == null || !target.IsAlive)
            {
                return false;
            }

            if (attacker != null && target != null)
            {
                CreatureSpriteAnimator.PlayAttack(attacker, target.Cell);
            }

            var attackerPart =
                attacker != null ? ActionLogPart.Creature(attacker.Kind) : ActionLogPart.Plain("?");
            ActionLog.BeginKey(TextKey.LogHit, attackerPart, ActionLogPart.Creature(target.Kind));
            try
            {
                ResolveHit(
                    attacker,
                    target,
                    power: 1,
                    corrosion: 0,
                    afterDamageAlive: null,
                    afterMelee: () =>
                    {
                        // Mob melee vs slime sticks personality combat (Aggressive keeps chase).
                        if (target is Slime)
                        {
                            attacker?.MarkAggro();
                            attacker?.RememberPursuit(target.Cell);
                        }

                        attacker?.NotifyDealtMeleeHit(target);
                    }
                );
                return true;
            }
            finally
            {
                ActionLog.End();
            }
        }

        /// <summary>
        /// Shared strike pipeline. Order: capture → strip → damage → (alive) apply/dispatch →
        /// tip notify → refresh → death log.
        /// </summary>
        private static void ResolveHit(
            Creature attacker,
            Creature target,
            int power,
            int corrosion,
            System.Action afterDamageAlive,
            System.Action afterMelee
        )
        {
            // Snapshot before Damage: volume hits can drop dominance and clear retorts.
            var survivedHit = new List<StatusEffect>(2);
            target.CaptureSurvivedHitReactions(survivedHit);

            var armorStripped = target.StripArmor(corrosion);
            target.Damage(power, out var tipStruck, out var applied, blockedByArmor: true);
            ActionLog.DetailDamage(target, armorStripped, applied);

            if (target.IsAlive)
            {
                afterDamageAlive?.Invoke();
                target.DispatchSurvivedHitReactions(survivedHit, attacker);
                afterMelee?.Invoke();
            }

            attacker?.NotifyStruckVolumeTip(tipStruck);
            target.RefreshVolumeStatuses();
            LogDeath(target);
        }

        private static void LogDeath(Creature target)
        {
            if (target == null || target.IsAlive)
            {
                return;
            }

            ActionLog.DetailKey(TextKey.LogDies, ActionLogPart.Creature(target.Kind));
        }
    }
}
