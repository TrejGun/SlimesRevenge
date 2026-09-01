using System.Collections.Generic;
using UnityEngine;

namespace SlimesRevenge
{
    public abstract class Creature : MonoBehaviour
    {
        [SerializeField]
        private Volume volume = new Volume();

        [SerializeField]
        private int visionRange = 5;

        [SerializeField]
        private int speed = 1;

        [SerializeField]
        private int maxHitPoints = 1;
        private readonly StatusQueue statusQueue = new StatusQueue();
        private readonly VolumeDominance volumeDominance = new VolumeDominance();

        public Volume Volume => volume;

        /// <summary>True while a <see cref="Digesting"/> meal is in the status queue.</summary>
        public bool IsDigesting => FindStatus<Digesting>() != null;

        public int VisionRange => visionRange;

        public int MaxHitPoints => maxHitPoints;

        public int HitPoints { get; private set; } = 1;

        public int Speed
        {
            get
            {
                var value = Mathf.Max(1, speed);
                foreach (var status in statusQueue.AsReadOnly)
                {
                    value += status.SpeedModifier;
                }

                return Mathf.Max(0, value);
            }
        }

        public IReadOnlyList<StatusEffect> Statuses => statusQueue.AsReadOnly;

        public bool InCombat => Aggroed;

        public bool Aggroed { get; private set; }

        public Vector2Int Cell { get; private set; }

        /// <summary>Turns to keep chasing a last-known cell after losing sight.</summary>
        public const int PursuitMemoryTurns = 3;

        /// <summary>Last known cell of hunted prey (or approach goal).</summary>
        public Vector2Int? PursuitCell { get; private set; }

        public int PursuitMemoryLeft { get; private set; }

        /// <summary>Waypoint while fleeing a predator.</summary>
        public Vector2Int? FleeCell { get; private set; }

        public virtual CreaturePersonality Personality => CreaturePersonality.Aggressive;

        public abstract CreatureKind Kind { get; }

        /// <summary>
        /// Flat strike/melee damage reduction before HP. Not spent on normal hits (DR).
        /// Strike <see cref="Substance.Corrosion"/> strips armor first; remaining armor still
        /// blocks <see cref="Substance.Power"/> on the same hit.
        /// <see cref="Corroding"/> pulses strip <see cref="Corroding.Corrosion"/> before HP.
        /// </summary>
        public int Armor { get; private set; }

        /// <summary>
        /// Slime lives only while its volume stack has matter. Everyone else: HP &gt; 0.
        /// Corpses stay in the scene but are not alive.
        /// </summary>
        public bool IsAlive =>
            gameObject.activeInHierarchy
            && !IsCorpse
            && (this is Slime ? Volume.UnitCount > 0 : HitPoints > 0);

        /// <summary>Dead body still present for devour / future revive; not an actor.</summary>
        public bool IsCorpse { get; private set; }

        /// <summary>Turns until the corpse vanishes; seeded from <see cref="MaxHitPoints"/>.</summary>
        public int DecayTurnsLeft { get; private set; }

        public bool CorpseExpired => IsCorpse && (DecayTurnsLeft <= 0 || Volume.UnitCount == 0);

        private bool skipCorpseAging;

        public void SetSpeed(int value)
        {
            speed = Mathf.Max(1, value);
        }

        public void SetMaxHitPoints(int value)
        {
            maxHitPoints = Mathf.Max(0, value);
            HitPoints = maxHitPoints;
        }

        public void SetArmor(int value)
        {
            Armor = Mathf.Max(0, value);
        }

        /// <summary>Strip armor by <paramref name="amount"/> (not below 0). Returns units actually stripped.</summary>
        public int StripArmor(int amount = 1)
        {
            if (amount <= 0 || Armor <= 0)
            {
                return 0;
            }

            var before = Armor;
            Armor = Mathf.Max(0, Armor - amount);
            return before - Armor;
        }

        public void MarkAggro()
        {
            Aggroed = true;
        }

        public void ClearAggro()
        {
            Aggroed = false;
        }

        public void RememberPursuit(Vector2Int cell)
        {
            PursuitCell = cell;
            PursuitMemoryLeft = PursuitMemoryTurns;
        }

        public void ClearPursuit()
        {
            PursuitCell = null;
            PursuitMemoryLeft = 0;
        }

        public void TickPursuitMemory()
        {
            if (PursuitCell == null)
            {
                return;
            }

            PursuitMemoryLeft = Mathf.Max(0, PursuitMemoryLeft - 1);
            if (PursuitMemoryLeft <= 0)
            {
                ClearPursuit();
            }
        }

        public void SetFleeCell(Vector2Int cell)
        {
            FleeCell = cell;
        }

        public void ClearFlee()
        {
            FleeCell = null;
        }

        public T FindStatus<T>()
            where T : StatusEffect
        {
            return statusQueue.Find<T>(volumeDominance);
        }

        public bool HasStatus<T>()
            where T : StatusEffect
        {
            return FindStatus<T>() != null;
        }

        public int CountStatus<T>()
            where T : StatusEffect
        {
            return statusQueue.CountOf<T>(volumeDominance);
        }

        /// <summary>
        /// Snapshot reactions that must fire after a surviving hit even if dominance clears mid-strike.
        /// </summary>
        public void CaptureSurvivedHitReactions(System.Collections.Generic.IList<StatusEffect> sink)
        {
            volumeDominance.CaptureSurvivedHitReactions(sink);
        }

        /// <summary>
        /// True when <paramref name="observer"/> may spot this creature for chase / attack.
        /// Dominance passives (e.g. mercury <see cref="Invisible"/>) count even when not queued.
        /// </summary>
        public bool IsDetectedBy(Creature observer)
        {
            if (!IsAlive)
            {
                return false;
            }

            if (observer != null && observer.Aggroed)
            {
                return true;
            }

            if (this is Slime)
            {
                var passives = new System.Collections.Generic.List<StatusEffect>(4);
                volumeDominance.CollectPassives(passives);
                for (var i = 0; i < passives.Count; i++)
                {
                    if (passives[i].BlocksDetectionFrom(observer, this))
                    {
                        return false;
                    }
                }
            }

            var statuses = Statuses;
            for (var i = 0; i < statuses.Count; i++)
            {
                if (statuses[i].BlocksDetectionFrom(observer, this))
                {
                    return false;
                }
            }

            return true;
        }

        public void DispatchSurvivedHitReactions(
            System.Collections.Generic.IReadOnlyList<StatusEffect> reactions,
            Creature attacker
        )
        {
            if (reactions == null)
            {
                return;
            }

            for (var i = 0; i < reactions.Count; i++)
            {
                reactions[i]?.OnOwnerSurvivedHit(this, attacker);
            }
        }

        public void NotifyDealtMeleeHit(Creature target)
        {
            var statuses = statusQueue.AsReadOnly;
            for (var i = 0; i < statuses.Count; i++)
            {
                statuses[i].OnOwnerDealtMeleeHit(this, target);
            }
        }

        public void NotifyStruckVolumeTip(Substance tip)
        {
            if (tip == null)
            {
                return;
            }

            var statuses = statusQueue.AsReadOnly;
            for (var i = 0; i < statuses.Count; i++)
            {
                statuses[i].OnOwnerStruckVolumeTip(this, tip);
            }
        }

        /// <summary>
        /// Substance residue landed on this creature (puddle / retort / strike Apply).
        /// </summary>
        public void NotifyReceivedSubstance(Substance substance)
        {
            if (substance == null)
            {
                return;
            }

            var statuses = statusQueue.AsReadOnly;
            for (var i = 0; i < statuses.Count; i++)
            {
                statuses[i].OnOwnerReceivedSubstance(this, substance);
            }
        }

        /// <summary>
        /// Let statuses scale incoming harm (e.g. oil <see cref="Flammable"/>). Includes
        /// dominance passives that are not queued.
        /// </summary>
        public int ModifyIncomingHarm(int amount)
        {
            if (amount <= 0)
            {
                return amount;
            }

            var statuses = statusQueue.AsReadOnly;
            for (var i = 0; i < statuses.Count; i++)
            {
                amount = statuses[i].ModifyIncomingHarm(this, amount);
            }

            amount = volumeDominance.ModifyIncomingHarm(this, amount, statuses);
            return amount;
        }

        public virtual void Damage(int amount)
        {
            Damage(amount, out _, out _, blockedByArmor: true);
        }

        public virtual void Damage(int amount, bool blockedByArmor)
        {
            Damage(amount, out _, out _, blockedByArmor);
        }

        /// <summary>
        /// Apply damage. Slime: pops volume tip (newest first). Others: optional armor DR, then HP.
        /// <paramref name="tipStruck"/> is the first volume unit knocked off a slime (else null).
        /// </summary>
        public virtual void Damage(int amount, out Substance tipStruck)
        {
            Damage(amount, out tipStruck, out _, blockedByArmor: true);
        }

        /// <summary>
        /// Apply damage. Slime: pops volume tip (newest first). Others: optional armor DR, then HP.
        /// <paramref name="tipStruck"/> is the first volume unit knocked off a slime (else null).
        /// <paramref name="applied"/> is volume units lost (slime) or HP lost (others).
        /// Pass <paramref name="blockedByArmor"/> false for status pulses that ignore DR.
        /// </summary>
        public virtual void Damage(int amount, out Substance tipStruck, bool blockedByArmor)
        {
            Damage(amount, out tipStruck, out _, blockedByArmor);
        }

        public virtual void Damage(
            int amount,
            out Substance tipStruck,
            out int applied,
            bool blockedByArmor
        )
        {
            tipStruck = null;
            applied = 0;
            if (amount <= 0 || !IsAlive)
            {
                return;
            }

            if (this is Slime)
            {
                var popped = new List<Substance>();
                applied = Volume.Damage(amount, popped);
                if (popped.Count > 0)
                {
                    tipStruck = popped[0];
                }

                if (IsAlive)
                {
                    RefreshVolumeStatuses();
                }

                return;
            }

            // Armor = flat strike DR (not consumed by Power). Corrosion strips it before this call.
            if (blockedByArmor && Armor > 0)
            {
                amount -= Mathf.Min(Armor, amount);
                if (amount <= 0)
                {
                    return;
                }
            }

            var before = HitPoints;
            HitPoints = Mathf.Max(0, HitPoints - amount);
            applied = before - HitPoints;
            if (applied > 0 && HitPoints > 0)
            {
                CreatureSpriteAnimator.PlayDamage(this);
            }
        }

        public int Heal(int amount)
        {
            if (amount <= 0 || !IsAlive)
            {
                return 0;
            }

            var before = HitPoints;
            HitPoints = Mathf.Min(maxHitPoints, HitPoints + amount);
            return HitPoints - before;
        }

        public bool AddStatus(StatusEffect effect)
        {
            if (!statusQueue.Add(effect, volumeDominance))
            {
                return false;
            }

            if (ActionLog.HasOpenGroup)
            {
                ActionLog.DetailGains(Kind, effect);
            }

            return true;
        }

        public bool ClearStatus<T>()
            where T : StatusEffect
        {
            return statusQueue.Clear<T>(this);
        }

        /// <summary>Drop every queued status (notifies each via <see cref="StatusEffect.OnRemoved"/>).</summary>
        public void ClearAllStatuses()
        {
            statusQueue.ClearAll(this);
        }

        /// <summary>Idempotent innate seeding; override in concrete animals.</summary>
        public void EnsureInnateTraits()
        {
            SeedInnateTraits();
        }

        protected virtual void SeedInnateTraits() { }

        protected void EnsureInnate<T>()
            where T : InnateTrait, new()
        {
            if (FindStatus<T>() == null)
            {
                AddStatus(new T());
            }
        }

        /// <summary>
        /// Sync slime volume dominance via <see cref="VolumeDominance"/>.
        /// </summary>
        public void RefreshVolumeStatuses()
        {
            if (this is not Slime)
            {
                return;
            }

            volumeDominance.Refresh(Volume, this, statusQueue.Items);
        }

        /// <summary>
        /// Start-of-turn status pass: delegates FIFO pulse to <see cref="StatusQueue"/>.
        /// After each pulse, <see cref="RefreshVolumeStatuses"/> may edit the queue.
        /// </summary>
        public void RefreshStatuses()
        {
            statusQueue.Pulse(this, RefreshVolumeStatuses);
        }

        /// <summary>Obsolete name for <see cref="RefreshStatuses"/>.</summary>
        public void TickStatuses() => RefreshStatuses();

        public void TakeTurn(
            GameSession session,
            Creature player,
            IRng rng,
            System.Collections.Generic.IReadOnlyList<Creature> others = null
        )
        {
            CreatureTurnContext.Push(session, player, rng, others);
            try
            {
                var intent = CreatureHunt.Redirect(this, CreatureBrain.Decide(this), others);
                var target = CreatureTurnContext.FocusTarget ?? player;
                if (
                    target == null
                    && (
                        intent == CreatureIntent.Chase
                        || intent == CreatureIntent.Attack
                        || intent == CreatureIntent.Flee
                    )
                )
                {
                    intent = CreatureIntent.Idle;
                }

                CreatureMoves.Perform(intent, this, target, session, rng);
                CreatureHunt.AfterAct(this, intent, target, others);
            }
            finally
            {
                CreatureTurnContext.Pop();
            }
        }

        public void PlaceOn(Vector2Int cell)
        {
            Cell = cell;
            transform.position = new Vector3(cell.x + 0.5f, cell.y + 0.5f, -0.1f);
        }

        /// <summary>
        /// Mark this creature as a corpse on the floor: keeps the GameObject for visuals / revive,
        /// seeds decay from <see cref="MaxHitPoints"/>, skips aging on the death turn.
        /// </summary>
        public void BecomeCorpse()
        {
            if (IsCorpse)
            {
                return;
            }

            IsCorpse = true;
            HitPoints = 0;
            DecayTurnsLeft = Mathf.Max(1, maxHitPoints);
            skipCorpseAging = true;
            ClearAggro();
            ClearPursuit();
            ClearFlee();
            CreatureSpriteAnimator.PlayDeath(this);
        }

        public void TickCorpseDecay()
        {
            if (!IsCorpse)
            {
                return;
            }

            if (skipCorpseAging)
            {
                skipCorpseAging = false;
                return;
            }

            if (DecayTurnsLeft > 0)
            {
                DecayTurnsLeft--;
            }
        }

        /// <summary>Remove from play entirely (game over / decayed / digested).</summary>
        public void Die()
        {
            IsCorpse = false;
            gameObject.SetActive(false);
        }
    }
}
