using UnityEngine;

namespace SlimesRevenge
{
    public sealed class StatusCard : IPopupContent
    {
        private readonly StatusEffect live;
        private readonly StatusLogRef? snapshot;

        public StatusCard(StatusEffect effect)
        {
            live = effect;
            snapshot = null;
        }

        public StatusCard(StatusLogRef snapshot)
        {
            live = null;
            this.snapshot = snapshot;
        }

        public void Build(RectTransform parent, PopupHost host)
        {
            string label;
            string description;
            string timer;
            Sprite icon;

            if (live != null)
            {
                label = CardUi.SafeStatusLabel(live);
                description = CardUi.SafeStatusDescription(live);
                icon = live.Icon;
                timer = FormatTimer(live.Permanent, live.Remaining);
            }
            else if (snapshot != null)
            {
                var s = snapshot.Value;
                label = string.IsNullOrEmpty(s.Label) ? "?" : s.Label;
                description = s.Description ?? string.Empty;
                icon = IconCatalog.Load(s.IconKey);
                timer = FormatTimer(s.Permanent, s.Remaining);
            }
            else
            {
                CardUi.AddTitle(parent, "?");
                return;
            }

            CardUi.AddPortrait(parent, icon, new Color(0.4f, 0.5f, 0.4f, 1f), 72f);
            CardUi.AddTitle(parent, label);
            CardUi.AddBodyText(parent, description, 15);
            CardUi.AddBodyText(parent, timer, 14);
        }

        private static string FormatTimer(bool permanent, int remaining)
        {
            if (permanent)
            {
                return CardUi.Safe(TextKey.StatusForever, "Forever");
            }

            return CardUi.SafeFormat(TextKey.StatusTimer, "{0} turns", remaining);
        }
    }
}
