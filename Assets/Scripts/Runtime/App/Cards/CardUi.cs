using System;
using UnityEngine;
using UnityEngine.UI;

namespace SlimesRevenge
{
    /// <summary>Shared UI helpers for popup cards.</summary>
    public static class CardUi
    {
        public static Font Font { get; set; }

        public static Text AddTitle(RectTransform parent, string text)
        {
            return AddText(parent, text, 20, FontStyle.Bold, Color.white, 28f);
        }

        public static Text AddSectionLabel(RectTransform parent, string text)
        {
            return AddText(
                parent,
                text,
                15,
                FontStyle.Bold,
                new Color(0.75f, 0.85f, 0.7f, 1f),
                22f
            );
        }

        public static Text AddBodyText(RectTransform parent, string text, int fontSize = 15)
        {
            var label = AddText(
                parent,
                text,
                fontSize,
                FontStyle.Normal,
                new Color(0.85f, 0.9f, 0.8f, 1f),
                0f
            );
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            var le = label.GetComponent<LayoutElement>();
            if (le != null)
            {
                le.minHeight = 20f;
                le.preferredHeight = Mathf.Max(
                    20f,
                    fontSize * 1.4f * Mathf.Max(1, text.Split('\n').Length)
                );
                le.flexibleHeight = 0f;
            }

            return label;
        }

        public static Image AddPortrait(
            RectTransform parent,
            Sprite sprite,
            Color fallback,
            float size = 64f
        )
        {
            var go = new GameObject("Portrait").AddComponent<RectTransform>();
            go.SetParent(parent, false);
            var image = go.gameObject.AddComponent<Image>();
            if (sprite != null)
            {
                image.sprite = sprite;
                image.color = Color.white;
                image.preserveAspect = true;
            }
            else
            {
                image.color = fallback;
            }

            var le = go.gameObject.AddComponent<LayoutElement>();
            le.minWidth = size;
            le.minHeight = size;
            le.preferredWidth = size;
            le.preferredHeight = size;
            return image;
        }

        public static RectTransform AddRow(RectTransform parent)
        {
            var row = new GameObject("Row").AddComponent<RectTransform>();
            row.SetParent(parent, false);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;
            layout.spacing = 6f;
            var le = row.gameObject.AddComponent<LayoutElement>();
            le.minHeight = 40f;
            le.preferredHeight = 40f;
            return row;
        }

        public static Button AddIconButton(
            RectTransform parent,
            Sprite sprite,
            Color fallback,
            Action onClick,
            float size = 40f,
            string badge = null
        )
        {
            var go = new GameObject("IconButton").AddComponent<RectTransform>();
            go.SetParent(parent, false);
            var image = go.gameObject.AddComponent<Image>();
            if (sprite != null)
            {
                image.sprite = sprite;
                image.color = Color.white;
                image.preserveAspect = true;
            }
            else
            {
                image.color = fallback;
            }

            var button = go.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            if (onClick != null)
            {
                button.onClick.AddListener(() => onClick());
            }

            var le = go.gameObject.AddComponent<LayoutElement>();
            le.minWidth = size;
            le.minHeight = size;
            le.preferredWidth = size;
            le.preferredHeight = size;

            if (!string.IsNullOrEmpty(badge))
            {
                var badgeGo = new GameObject("Badge").AddComponent<RectTransform>();
                badgeGo.SetParent(go, false);
                badgeGo.anchorMin = new Vector2(1f, 0f);
                badgeGo.anchorMax = new Vector2(1f, 0f);
                badgeGo.pivot = new Vector2(1f, 0f);
                badgeGo.anchoredPosition = new Vector2(2f, -2f);
                badgeGo.sizeDelta = new Vector2(18f, 16f);
                var badgeBg = badgeGo.gameObject.AddComponent<Image>();
                badgeBg.color = new Color32(16, 20, 14, 220);
                var badgeText = new GameObject("T").AddComponent<RectTransform>();
                badgeText.SetParent(badgeGo, false);
                badgeText.anchorMin = Vector2.zero;
                badgeText.anchorMax = Vector2.one;
                badgeText.offsetMin = Vector2.zero;
                badgeText.offsetMax = Vector2.zero;
                var t = badgeText.gameObject.AddComponent<Text>();
                t.font = Font;
                t.fontSize = 11;
                t.alignment = TextAnchor.MiddleCenter;
                t.color = Color.white;
                t.text = badge;
                t.raycastTarget = false;
            }

            return button;
        }

        private static Text AddText(
            RectTransform parent,
            string text,
            int fontSize,
            FontStyle style,
            Color color,
            float height
        )
        {
            var go = new GameObject("Text").AddComponent<RectTransform>();
            go.SetParent(parent, false);
            var label = go.gameObject.AddComponent<Text>();
            label.font = Font;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.alignment = TextAnchor.UpperLeft;
            label.color = color;
            label.text = text ?? string.Empty;
            label.raycastTarget = false;
            var le = go.gameObject.AddComponent<LayoutElement>();
            if (height > 0f)
            {
                le.minHeight = height;
                le.preferredHeight = height;
            }

            return label;
        }

        public static string Safe(string key, string fallback) => I18n.GetOr(key, fallback);

        public static string SafeFormat(string key, string fallbackFormat, params object[] args) =>
            I18n.FormatOr(key, fallbackFormat, args);

        /// <summary>Never throws mid-card-build if a locale table is incomplete.</summary>
        public static string SafeSubstanceLabel(Substance substance)
        {
            if (substance == null)
            {
                return "?";
            }

            try
            {
                return substance.Label;
            }
            catch
            {
                return substance.GetType().Name;
            }
        }

        /// <summary>Never throws mid-card-build if a locale table is incomplete.</summary>
        public static string SafeStatusLabel(StatusEffect effect)
        {
            if (effect == null)
            {
                return "?";
            }

            try
            {
                return effect.Label;
            }
            catch
            {
                return effect.GetType().Name;
            }
        }

        /// <summary>Never throws mid-card-build if a locale table is incomplete.</summary>
        public static string SafeStatusDescription(StatusEffect effect)
        {
            if (effect == null)
            {
                return string.Empty;
            }

            try
            {
                return effect.Description;
            }
            catch
            {
                return string.Empty;
            }
        }

        public static string CreatureName(CreatureKind kind) => I18n.Creature(kind);
    }
}
