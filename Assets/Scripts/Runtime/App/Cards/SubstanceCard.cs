using System.Collections.Generic;
using UnityEngine;

namespace SlimesRevenge
{
    public sealed class SubstanceCard : IPopupContent
    {
        private readonly Substance substance;

        public SubstanceCard(Substance substance)
        {
            this.substance = substance?.Clone();
        }

        public void Build(RectTransform parent, PopupHost host)
        {
            if (substance == null)
            {
                CardUi.AddTitle(parent, "?");
                return;
            }

            CardUi.AddPortrait(parent, substance.Icon, substance.Color, 72f);
            CardUi.AddTitle(parent, CardUi.SafeSubstanceLabel(substance));
            CardUi.AddBodyText(
                parent,
                CardUi.SafeFormat(TextKey.CardPower, "Power {0}", substance.Power)
            );
            if (substance.Corrosion > 0)
            {
                CardUi.AddBodyText(
                    parent,
                    CardUi.SafeFormat(TextKey.CardCorrosion, "Corrosion {0}", substance.Corrosion)
                );
            }

            AddStatusSection(
                parent,
                host,
                TextKey.CardApplies,
                "Applies",
                collect: substance.CollectApplyPreview
            );

            AddStatusSection(
                parent,
                host,
                TextKey.CardDominance,
                "Dominance",
                collect: substance.CollectDominancePassives
            );
        }

        private static void AddStatusSection(
            RectTransform parent,
            PopupHost host,
            string titleKey,
            string titleFallback,
            System.Action<IList<StatusEffect>> collect
        )
        {
            CardUi.AddSectionLabel(parent, CardUi.Safe(titleKey, titleFallback));
            var preview = new List<StatusEffect>();
            collect?.Invoke(preview);
            if (preview.Count == 0)
            {
                CardUi.AddBodyText(parent, CardUi.Safe(TextKey.CardNone, "none"), 13);
                return;
            }

            var row = CardUi.AddRow(parent);
            for (var i = 0; i < preview.Count; i++)
            {
                var effect = preview[i];
                CardUi.AddIconButton(
                    row,
                    effect.Icon,
                    new Color(0.4f, 0.5f, 0.4f, 1f),
                    () => host.Push(new StatusCard(effect)),
                    40f
                );
            }
        }
    }
}
