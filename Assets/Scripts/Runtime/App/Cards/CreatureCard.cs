using UnityEngine;
using UnityEngine.UI;

namespace SlimesRevenge
{
    public sealed class CreatureCard : IPopupContent
    {
        private readonly Creature creature;

        public CreatureCard(Creature creature)
        {
            this.creature = creature;
        }

        public void Build(RectTransform parent, PopupHost host)
        {
            if (creature == null)
            {
                CardUi.AddTitle(parent, "?");
                return;
            }

            var header = CardUi.AddRow(parent);
            var sr = creature.GetComponent<SpriteRenderer>();
            CardUi.AddPortrait(
                header,
                sr != null ? sr.sprite : null,
                new Color(0.35f, 0.4f, 0.35f, 1f),
                64f
            );

            var info = new GameObject("Info").AddComponent<RectTransform>();
            info.SetParent(header, false);
            var v = info.gameObject.AddComponent<VerticalLayoutGroup>();
            v.childAlignment = TextAnchor.UpperLeft;
            v.childControlHeight = true;
            v.childControlWidth = true;
            v.childForceExpandHeight = false;
            v.childForceExpandWidth = true;
            v.spacing = 2f;
            var ile = info.gameObject.AddComponent<LayoutElement>();
            ile.flexibleWidth = 1f;
            ile.minHeight = 64f;

            var title = CardUi.CreatureName(creature.Kind);
            if (creature.IsCorpse)
            {
                title += CardUi.Safe(TextKey.CardCorpseSuffix, " (Corpse)");
            }

            CardUi.AddTitle(info, title);

            if (creature is Slime)
            {
                CardUi.AddBodyText(
                    info,
                    CardUi.SafeFormat(
                        TextKey.CardVolumeOf,
                        "Volume {0}/{1}",
                        creature.Volume.UnitCount,
                        Volume.Capacity
                    ),
                    14
                );
                var dominant =
                    creature.Volume != null && creature.Volume.TryDominant(out var d)
                        ? CardUi.SafeSubstanceLabel(d)
                        : CardUi.Safe(TextKey.CardNone, "none");
                CardUi.AddBodyText(
                    info,
                    CardUi.SafeFormat(TextKey.CardDominant, "Dominant: {0}", dominant),
                    13
                );
                var meal = creature.FindStatus<Digesting>()?.Meal;
                var dig =
                    meal != null
                        ? CardUi.CreatureName(meal.Kind)
                        : CardUi.Safe(TextKey.CardNone, "none");
                CardUi.AddBodyText(
                    info,
                    CardUi.SafeFormat(TextKey.CardDigesting, "Digesting: {0}", dig),
                    13
                );
            }
            else
            {
                CardUi.AddBodyText(
                    info,
                    CardUi.SafeFormat(
                        TextKey.CardHp,
                        "HP {0}/{1}",
                        creature.HitPoints,
                        creature.MaxHitPoints
                    ),
                    14
                );
                if (creature.Armor > 0)
                {
                    CardUi.AddBodyText(
                        info,
                        CardUi.SafeFormat(TextKey.CardArmor, "Armor {0}", creature.Armor),
                        14
                    );
                }

                CardUi.AddBodyText(
                    info,
                    CardUi.SafeFormat(
                        TextKey.CardVolumeCount,
                        "Volume {0}",
                        creature.Volume.UnitCount
                    ),
                    14
                );
            }

            if (creature.IsCorpse)
            {
                var maxDecay = Mathf.Max(1, creature.MaxHitPoints);
                CardUi.AddBodyText(
                    info,
                    CardUi.SafeFormat(
                        TextKey.CardDecay,
                        "Decay {0}/{1}",
                        creature.DecayTurnsLeft,
                        maxDecay
                    ),
                    14
                );
            }

            CardUi.AddSectionLabel(parent, CardUi.Safe(TextKey.CardVolume, "Volume"));
            var volumeRow = CardUi.AddRow(parent);
            var kinds = creature.Volume != null ? creature.Volume.UniqueKinds() : null;
            if (kinds == null || kinds.Count == 0)
            {
                CardUi.AddBodyText(parent, CardUi.Safe(TextKey.CardNone, "none"), 13);
            }
            else
            {
                for (var i = 0; i < kinds.Count; i++)
                {
                    var sample = kinds[i];
                    var count = creature.Volume.CountOf(sample.GetType());
                    var captured = sample.Clone();
                    CardUi.AddIconButton(
                        volumeRow,
                        sample.Icon,
                        sample.Color,
                        () => host.Push(new SubstanceCard(captured)),
                        40f,
                        count > 1 ? count.ToString() : null
                    );
                }
            }

            CardUi.AddSectionLabel(parent, CardUi.Safe(TextKey.CardStatuses, "Statuses"));
            var statusRow = CardUi.AddRow(parent);
            var statuses = creature.Statuses;
            if (statuses == null || statuses.Count == 0)
            {
                CardUi.AddBodyText(parent, CardUi.Safe(TextKey.CardNone, "none"), 13);
            }
            else
            {
                for (var i = 0; i < statuses.Count; i++)
                {
                    var effect = statuses[i];
                    CardUi.AddIconButton(
                        statusRow,
                        effect.Icon,
                        new Color(0.4f, 0.5f, 0.4f, 1f),
                        () => host.Push(new StatusCard(effect)),
                        40f
                    );
                }
            }
        }
    }
}
