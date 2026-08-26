using System.Collections.Generic;
using UnityEngine;

namespace SlimesRevenge
{
    public abstract class Creature : MonoBehaviour
    {
        [SerializeField] private Volume volume = new Volume();
        [SerializeField] private int visionRange = 5;
        [SerializeField] private int speed = 1;
        [SerializeField] private int maxHitPoints = 1;
        private readonly List<StatusEffect> statuses = new List<StatusEffect>();
        private Digestion digestion;

        public Volume Volume => volume;

        public Digestion Digestion => digestion ??= new Digestion();

        public int VisionRange => visionRange;

        public int MaxHitPoints => maxHitPoints;

        public int HitPoints { get; private set; } = 1;

        public int Speed
        {
            get
            {
                var value = Mathf.Max(1, speed);
                foreach (var status in statuses)
                {
                    value += status.SpeedModifier;
                }

                return Mathf.Max(0, value);
            }
        }

        public IReadOnlyList<StatusEffect> Statuses => statuses;

        public bool InCombat => Aggroed;

        public bool Aggroed { get; private set; }

        public Vector2Int Cell { get; private set; }

        public virtual CreaturePersonality Personality => CreaturePersonality.Aggressive;

        public virtual bool UsesVolumeAsShield => false;

        public bool IsAlive => gameObject.activeInHierarchy && (UsesVolumeAsShield
            ? Volume.UnitCount > 0 || HitPoints > 0
            : HitPoints > 0);

        public void SetSpeed(int value)
        {
            speed = Mathf.Max(1, value);
        }

        public void SetMaxHitPoints(int value)
        {
            maxHitPoints = Mathf.Max(1, value);
            HitPoints = maxHitPoints;
        }

        public void MarkAggro()
        {
            Aggroed = true;
        }

        public void ClearAggro()
        {
            Aggroed = false;
        }

        public T FindStatus<T>() where T : StatusEffect
        {
            for (var i = 0; i < statuses.Count; i++)
            {
                if (statuses[i] is T match)
                {
                    return match;
                }
            }

            return null;
        }

        public int CountStatus<T>() where T : StatusEffect
        {
            var count = 0;
            for (var i = 0; i < statuses.Count; i++)
            {
                if (statuses[i] is T)
                {
                    count++;
                }
            }

            return count;
        }

        public void Extinguish()
        {
            for (var i = statuses.Count - 1; i >= 0; i--)
            {
                if (statuses[i] is Burning)
                {
                    statuses.RemoveAt(i);
                }
            }
        }

        public virtual void Damage(int amount)
        {
            if (amount <= 0 || !IsAlive)
            {
                return;
            }

            if (UsesVolumeAsShield && Volume.UnitCount > 0)
            {
                var taken = Volume.Damage(amount);
                amount -= taken;
                if (amount <= 0)
                {
                    return;
                }
            }

            HitPoints = Mathf.Max(0, HitPoints - amount);
        }

        public void AddStatus(StatusEffect effect)
        {
            if (effect == null)
            {
                return;
            }

            if (effect is Burning)
            {
                if (FindStatus<Fireproof>() != null)
                {
                    return;
                }

                effect.Extend(Oiled.FireTurns * CountStatus<Oiled>());
            }

            statuses.Add(effect);
        }

        public void OnStruck(Creature attacker)
        {
            FindStatus<Retaliation>()?.Retort?.Apply(attacker);
        }

        public void RefreshBodyTraits()
        {
            for (var i = statuses.Count - 1; i >= 0; i--)
            {
                if (statuses[i] is BodyTrait)
                {
                    statuses.RemoveAt(i);
                }
            }

            if (!UsesVolumeAsShield || !Volume.TryDominant(out var dominant))
            {
                return;
            }

            if (dominant is Water)
            {
                statuses.Add(new Fireproof());
                Extinguish();
                statuses.Add(new Retaliation(new Water()));
            }
            else if (dominant is Oil)
            {
                statuses.Add(new Flammable());
                statuses.Add(new Retaliation(new Oil()));
            }
            else if (dominant is Poison)
            {
                statuses.Add(new Retaliation(new Poison()));
            }
            else if (dominant is Acid)
            {
                statuses.Add(new Retaliation(new Acid()));
            }
            else if (dominant is Lava)
            {
                statuses.Add(new Retaliation(new Lava()));
            }
            else if (dominant is Blood)
            {
                statuses.Add(new Retaliation(new Blood()));
            }
        }

        public void TickStatuses()
        {
            // Application order (oldest first). Stop if a pulse empties volume / kills.
            var snapshot = statuses.ToArray();
            for (var i = 0; i < snapshot.Length; i++)
            {
                var effect = snapshot[i];
                if (!statuses.Contains(effect))
                {
                    continue;
                }

                effect.Tick(this);
                if (effect.Expired)
                {
                    statuses.Remove(effect);
                }

                if (!IsAlive)
                {
                    break;
                }
            }

            if (UsesVolumeAsShield && IsAlive)
            {
                RefreshBodyTraits();
            }
        }

        public void TakeTurn(GameSession session, Creature player, IRng rng)
        {
            var intent = CreatureBrain.Decide(this, player, session, rng);
            CreatureMoves.Perform(intent, this, player, session, rng);
        }

        public void PlaceOn(Vector2Int cell)
        {
            Cell = cell;
            transform.position = new Vector3(cell.x + 0.5f, cell.y + 0.5f, -0.1f);
        }

        public void Die()
        {
            gameObject.SetActive(false);
        }
    }
}
