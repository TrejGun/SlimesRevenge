using System.Collections.Generic;
using UnityEngine;

namespace SlimesRevenge
{
    /// <summary>
    /// Timed meal: duration = corpse volume units. Mid-pulses only advance the timer;
    /// the final pulse bulk-transfers into the eater (capacity permitting) and logs unique
    /// substances. A second <see cref="Digesting"/> is blocked while one is active.
    /// </summary>
    public sealed class Digesting : OverTime
    {
        private readonly List<Substance> absorbedUnique = new List<Substance>();
        private Creature meal;
        private bool finished;

        public Digesting(Creature corpse)
            : base(DurationFor(corpse))
        {
            meal = corpse;
        }

        public Creature Meal => meal;

        public override bool LogsPulse => true;

        public override string Label => I18n.Get(TextKey.StatusDigesting);

        public override string Description => I18n.Get(TextKey.StatusDigestingDesc);

        public override bool Blocks(StatusEffect incoming) => incoming is Digesting;

        /// <summary>
        /// True when <paramref name="eater"/> filled units are greater than or equal to the corpse's.
        /// </summary>
        public static bool CanBegin(Volume eater, Creature corpse)
        {
            if (eater == null || corpse == null || !corpse.IsCorpse)
            {
                return false;
            }

            var body = corpse.Volume.UnitCount;
            return body > 0 && eater.UnitCount >= body;
        }

        /// <summary>
        /// Drop the meal without transfer (softcore death / clear). Safe to call more than once.
        /// </summary>
        public void Abort()
        {
            if (finished)
            {
                return;
            }

            DestroyMeal();
            finished = true;
            ExpireNow();
        }

        public override void OnRemoved(Creature owner)
        {
            if (!finished)
            {
                Abort();
            }
        }

        protected override void OnPulse(Creature creature)
        {
            if (finished || meal == null || creature == null)
            {
                return;
            }

            // Timer only until the last pulse.
            if (Remaining > 1)
            {
                return;
            }

            FinishAbsorb(creature);
        }

        private void FinishAbsorb(Creature creature)
        {
            var destination = creature.Volume;
            if (destination != null && meal.Volume != null)
            {
                while (meal.Volume.UnitCount > 0)
                {
                    if (destination.UnitCount >= Volume.Capacity)
                    {
                        meal.Volume.Clear();
                        break;
                    }

                    if (!meal.Volume.TryTakeEnd(out var substance))
                    {
                        break;
                    }

                    destination.Add(Volume.CloneSubstance(substance));
                    RememberAbsorbed(substance);
                }
            }

            LogDigestionComplete();
            DestroyMeal();
            finished = true;
        }

        private void RememberAbsorbed(Substance substance)
        {
            if (substance == null)
            {
                return;
            }

            var type = substance.GetType();
            for (var i = 0; i < absorbedUnique.Count; i++)
            {
                if (absorbedUnique[i].GetType() == type)
                {
                    return;
                }
            }

            absorbedUnique.Add(substance.Clone());
        }

        private void LogDigestionComplete()
        {
            if (!ActionLog.HasOpenGroup || meal == null)
            {
                return;
            }

            ActionLog.DetailKey(TextKey.LogDigests, ActionLogPart.Creature(meal.Kind));
            for (var i = 0; i < absorbedUnique.Count; i++)
            {
                var sample = absorbedUnique[i];
                ActionLog.DetailSubstance(sample);
            }
        }

        private void DestroyMeal()
        {
            var body = meal;
            meal = null;
            absorbedUnique.Clear();
            if (body == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(body.gameObject);
            }
            else
            {
                Object.DestroyImmediate(body.gameObject);
            }
        }

        private static int DurationFor(Creature corpse)
        {
            var n = corpse?.Volume != null ? corpse.Volume.UnitCount : 0;
            return Mathf.Max(1, n);
        }
    }
}
