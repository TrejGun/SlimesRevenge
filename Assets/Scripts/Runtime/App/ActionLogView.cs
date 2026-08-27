using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SlimesRevenge
{
    /// <summary>Bottom-left scrolling action log overlay. Links open <see cref="PopupHost"/> cards.</summary>
    public sealed class ActionLogView : MonoBehaviour
    {
        private const float PanelWidth = 360f;
        private const float PanelHeight = 220f;
        private const float CollapsedHeight = 56f;
        private const float LineHeight = 22f;
        private const float Pad = 8f;

        private Canvas canvas;
        private RectTransform panel;
        private ScrollRect scroll;
        private RectTransform content;
        private RectTransform scrollbarRect;
        private Text collapseLabel;
        private Font font;
        private bool dirty = true;
        private bool collapsed = true;
        private int syncedGeneration = -1;
        private int syncedEntryCount;
        private readonly List<int> syncedDetailCounts = new List<int>();

        private void Awake()
        {
            EnsureUi();
            ActionLog.Changed += OnLogChanged;
            Sync();
        }

        private void OnDestroy()
        {
            ActionLog.Changed -= OnLogChanged;
            if (canvas != null)
            {
                Destroy(canvas.gameObject);
            }
        }

        private void LateUpdate()
        {
            if (!dirty)
            {
                return;
            }

            dirty = false;
            Sync();
        }

        private void OnLogChanged()
        {
            dirty = true;
        }

        private void ToggleCollapsed()
        {
            collapsed = !collapsed;
            ApplyCollapsedLayout();
            syncedGeneration = -1;
            dirty = true;
        }

        private void ApplyCollapsedLayout()
        {
            if (panel == null)
            {
                return;
            }

            panel.sizeDelta = new Vector2(PanelWidth, collapsed ? CollapsedHeight : PanelHeight);
            if (collapseLabel != null)
            {
                collapseLabel.text = collapsed
                    ? I18n.GetOr(TextKey.LogExpand, "▴")
                    : I18n.GetOr(TextKey.LogCollapse, "▾");
            }

            if (scrollbarRect != null)
            {
                scrollbarRect.gameObject.SetActive(!collapsed);
            }

            if (scroll != null)
            {
                scroll.vertical = !collapsed;
            }
        }

        private void ClearContentChildren()
        {
            for (var i = content.childCount - 1; i >= 0; i--)
            {
                var child = content.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }
        }

        private void Sync()
        {
            EnsureUi();
            var entries = ActionLog.Entries;

            if (collapsed)
            {
                ClearContentChildren();
                syncedGeneration = ActionLog.Generation;
                syncedEntryCount = entries.Count;
                syncedDetailCounts.Clear();
                for (var i = 0; i < entries.Count; i++)
                {
                    syncedDetailCounts.Add(entries[i].Details.Count);
                }

                if (entries.Count > 0)
                {
                    // Headlines only — last open action / effect, no indented details.
                    AddLine(entries[entries.Count - 1].HeadlineLine, indent: false);
                }

                Canvas.ForceUpdateCanvases();
                return;
            }

            var stickToBottom = ShouldStickToBottom();

            if (syncedGeneration != ActionLog.Generation)
            {
                ClearContentChildren();
                syncedGeneration = ActionLog.Generation;
                syncedEntryCount = 0;
                syncedDetailCounts.Clear();
            }

            while (syncedEntryCount < entries.Count)
            {
                var entry = entries[syncedEntryCount];
                AddLine(entry.HeadlineLine, indent: false);
                syncedDetailCounts.Add(0);
                syncedEntryCount++;
            }

            for (var i = 0; i < entries.Count; i++)
            {
                var details = entries[i].Details;
                while (syncedDetailCounts[i] < details.Count)
                {
                    AddLine(details[syncedDetailCounts[i]], indent: true);
                    syncedDetailCounts[i]++;
                }
            }

            Canvas.ForceUpdateCanvases();
            if (stickToBottom)
            {
                scroll.verticalNormalizedPosition = 0f;
            }
        }

        /// <summary>
        /// Auto-scroll only when already at (or unable to leave) the bottom.
        /// Any upward scroll stops jump-on-new-entry until the user returns to bottom.
        /// </summary>
        private bool ShouldStickToBottom()
        {
            if (scroll == null || content == null || scroll.viewport == null)
            {
                return true;
            }

            var overflow = content.rect.height - scroll.viewport.rect.height;
            if (overflow <= 1f)
            {
                return true;
            }

            return scroll.verticalNormalizedPosition <= 0.01f;
        }

        private void EnsureUi()
        {
            if (canvas != null)
            {
                return;
            }

            EnsureEventSystem();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Font.CreateDynamicFontFromOSFont("Helvetica", 16);
            }

            var root = new GameObject("ActionLog");
            canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(390f, 844f);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();

            var panelGo = new GameObject("Panel").AddComponent<RectTransform>();
            panel = panelGo;
            panel.SetParent(canvas.transform, false);
            panel.anchorMin = new Vector2(0f, 0f);
            panel.anchorMax = new Vector2(0f, 0f);
            panel.pivot = new Vector2(0f, 0f);
            panel.anchoredPosition = new Vector2(12f, 12f);
            panel.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = new Color32(16, 20, 14, 200);
            // Background must not steal world clicks; only text/links/scrollbar raycast.
            panelImage.raycastTarget = false;

            var collapseGo = new GameObject("Collapse").AddComponent<RectTransform>();
            collapseGo.SetParent(panel, false);
            collapseGo.anchorMin = collapseGo.anchorMax = new Vector2(1f, 1f);
            collapseGo.pivot = new Vector2(1f, 1f);
            collapseGo.anchoredPosition = new Vector2(-2f, -2f);
            collapseGo.sizeDelta = new Vector2(28f, 22f);
            var collapseImage = collapseGo.gameObject.AddComponent<Image>();
            collapseImage.color = new Color32(48, 64, 40, 255);
            var collapseButton = collapseGo.gameObject.AddComponent<Button>();
            collapseButton.targetGraphic = collapseImage;
            collapseButton.onClick.AddListener(ToggleCollapsed);
            collapseLabel = new GameObject("Label").AddComponent<Text>();
            collapseLabel.transform.SetParent(collapseGo, false);
            collapseLabel.font = font;
            collapseLabel.fontSize = 14;
            collapseLabel.alignment = TextAnchor.MiddleCenter;
            collapseLabel.color = Color.white;
            collapseLabel.text = I18n.GetOr(TextKey.LogCollapse, "▾");
            collapseLabel.raycastTarget = false;
            var collapseTextRect = collapseLabel.rectTransform;
            collapseTextRect.anchorMin = Vector2.zero;
            collapseTextRect.anchorMax = Vector2.one;
            collapseTextRect.offsetMin = Vector2.zero;
            collapseTextRect.offsetMax = Vector2.zero;

            var viewport = new GameObject("Viewport").AddComponent<RectTransform>();
            viewport.SetParent(panel, false);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(Pad, Pad);
            viewport.offsetMax = new Vector2(-Pad - 14f, -Pad);
            viewport.gameObject.AddComponent<RectMask2D>();
            // No full-bleed raycast Graphic on the viewport — empty log areas pass clicks through.

            content = new GameObject("Content").AddComponent<RectTransform>();
            content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 0f);
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.spacing = 2f;
            layout.padding = new RectOffset(4, 4, 4, 4);
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll = panel.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;

            scrollbarRect = new GameObject("Scrollbar").AddComponent<RectTransform>();
            scrollbarRect.SetParent(panel, false);
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = new Vector2(1f, 1f);
            scrollbarRect.pivot = new Vector2(1f, 0.5f);
            scrollbarRect.anchoredPosition = Vector2.zero;
            scrollbarRect.sizeDelta = new Vector2(12f, -Pad * 2f - 18f);
            var sbImage = scrollbarRect.gameObject.AddComponent<Image>();
            sbImage.color = new Color32(32, 40, 28, 255);
            var scrollbar = scrollbarRect.gameObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            var handleArea = new GameObject("Sliding Area").AddComponent<RectTransform>();
            handleArea.SetParent(scrollbarRect, false);
            handleArea.anchorMin = Vector2.zero;
            handleArea.anchorMax = Vector2.one;
            handleArea.offsetMin = Vector2.zero;
            handleArea.offsetMax = Vector2.zero;

            var handle = new GameObject("Handle").AddComponent<RectTransform>();
            handle.SetParent(handleArea, false);
            handle.anchorMin = Vector2.zero;
            handle.anchorMax = Vector2.one;
            handle.offsetMin = Vector2.zero;
            handle.offsetMax = Vector2.zero;
            var handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.color = new Color32(72, 96, 56, 255);
            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = handleImage;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            // Draw above log lines so the first headline can sit underneath the control.
            collapseGo.SetAsLastSibling();
            ApplyCollapsedLayout();
        }

        private void AddLine(ActionLogLine line, bool indent)
        {
            if (line == null)
            {
                return;
            }

            if (line.HasLink)
            {
                AddPartsLine(line, indent);
                return;
            }

            AddPlainLine(line.Text, indent, line.Style);
        }

        private void AddPlainLine(
            string text,
            bool indent,
            ActionLogStyle style = ActionLogStyle.Normal
        )
        {
            var banner = !indent && style == ActionLogStyle.Banner;
            var row = new GameObject(
                indent ? "Detail"
                : banner ? "Banner"
                : "Headline"
            ).AddComponent<RectTransform>();
            row.SetParent(content, false);
            var label = row.gameObject.AddComponent<Text>();
            label.font = font;
            label.fontSize =
                banner ? 18
                : indent ? 15
                : 16;
            label.fontStyle = banner ? FontStyle.Bold : FontStyle.Normal;
            label.alignment = TextAnchor.UpperLeft;
            label.color =
                banner ? new Color(1f, 0.86f, 0.25f, 1f)
                : indent ? new Color(0.85f, 0.9f, 0.8f, 1f)
                : Color.white;
            label.text = indent ? "  " + text : text;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            // Raycast so ScrollRect can drag/wheel over text; empty panel still pass-through.
            label.raycastTarget = true;
            var le = row.gameObject.AddComponent<LayoutElement>();
            FitRowHeight(row, le, label.fontSize);
        }

        private void AddPartsLine(ActionLogLine line, bool indent)
        {
            var banner = !indent && line.Style == ActionLogStyle.Banner;
            var row = new GameObject(
                indent ? "DetailParts"
                : banner ? "BannerParts"
                : "HeadlineParts"
            ).AddComponent<RectTransform>();
            row.SetParent(content, false);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;
            layout.spacing = 2f;
            var le = row.gameObject.AddComponent<LayoutElement>();

            var fontSize =
                banner ? 18
                : indent ? 15
                : 16;
            var plainColor =
                banner ? new Color(1f, 0.86f, 0.25f, 1f)
                : indent ? new Color(0.85f, 0.9f, 0.8f, 1f)
                : Color.white;
            var first = true;
            for (var i = 0; i < line.Parts.Count; i++)
            {
                var part = line.Parts[i];
                if (string.IsNullOrEmpty(part.Text) && !part.IsLink)
                {
                    continue;
                }

                if (first && indent)
                {
                    AddInlineText(row, "  ", fontSize, plainColor, flexible: false, bold: false);
                    first = false;
                }
                else
                {
                    first = false;
                }

                if (part.IsLink)
                {
                    MaybeAddPartIcon(row, part);
                    AddInlineLink(row, part, fontSize);
                }
                else
                {
                    AddInlineText(
                        row,
                        part.Text,
                        fontSize,
                        plainColor,
                        flexible: true,
                        bold: banner
                    );
                }
            }

            FitRowHeight(row, le, fontSize);
        }

        /// <summary>
        /// Grow the row past <see cref="LineHeight"/> when wrapped text needs more vertical space.
        /// </summary>
        private void FitRowHeight(RectTransform row, LayoutElement le, int fontSize)
        {
            if (row == null || le == null)
            {
                return;
            }

            var width =
                content != null && content.rect.width > 1f
                    ? content.rect.width - 8f
                    : PanelWidth - Pad * 2f - 24f;
            row.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);

            Canvas.ForceUpdateCanvases();
            var height = LineHeight;
            var texts = row.GetComponentsInChildren<Text>(true);
            for (var i = 0; i < texts.Length; i++)
            {
                var t = texts[i];
                if (t == null)
                {
                    continue;
                }

                height = Mathf.Max(height, t.preferredHeight + 2f);
            }

            height = Mathf.Max(height, fontSize + 6f);
            le.minHeight = height;
            le.preferredHeight = height;
        }

        private static void MaybeAddPartIcon(RectTransform row, ActionLogPart part)
        {
            Sprite icon = null;
            if (part.Kind == ActionLogLinkKind.Status && part.StatusSnapshot != null)
            {
                icon = IconCatalog.Load(part.StatusSnapshot.Value.IconKey);
            }
            else if (part.Kind == ActionLogLinkKind.Substance && part.SubstanceSnapshot != null)
            {
                icon = part.SubstanceSnapshot.Icon;
            }

            if (icon == null)
            {
                return;
            }

            var iconGo = new GameObject("Icon").AddComponent<RectTransform>();
            iconGo.SetParent(row, false);
            var image = iconGo.gameObject.AddComponent<Image>();
            image.sprite = icon;
            image.preserveAspect = true;
            image.raycastTarget = false;
            var ile = iconGo.gameObject.AddComponent<LayoutElement>();
            ile.minWidth = 18f;
            ile.minHeight = 18f;
            ile.preferredWidth = 18f;
            ile.preferredHeight = 18f;
        }

        private void AddInlineText(
            RectTransform parent,
            string text,
            int fontSize,
            Color color,
            bool flexible,
            bool bold = false
        )
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            var go = new GameObject("Text").AddComponent<RectTransform>();
            go.SetParent(parent, false);
            var label = go.gameObject.AddComponent<Text>();
            label.font = font;
            label.fontSize = fontSize;
            label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            label.alignment = TextAnchor.UpperLeft;
            label.color = color;
            label.text = text;
            label.horizontalOverflow = flexible
                ? HorizontalWrapMode.Wrap
                : HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = true;
            var le = go.gameObject.AddComponent<LayoutElement>();
            le.minHeight = LineHeight;
            if (flexible)
            {
                le.flexibleWidth = 1f;
                le.minWidth = 0f;
            }
            else
            {
                le.preferredWidth = label.preferredWidth;
                le.minWidth = le.preferredWidth;
            }
        }

        private void AddInlineLink(RectTransform parent, ActionLogPart part, int fontSize)
        {
            var go = new GameObject("Link").AddComponent<RectTransform>();
            go.SetParent(parent, false);
            var image = go.gameObject.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.01f);
            var button = go.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.highlightedColor = new Color(0.35f, 0.45f, 0.3f, 0.35f);
            colors.pressedColor = new Color(0.25f, 0.35f, 0.2f, 0.5f);
            button.colors = colors;
            var captured = part;
            button.onClick.AddListener(() => ActionLogLinkRouter.Open(captured));

            var labelGo = new GameObject("Label").AddComponent<RectTransform>();
            labelGo.SetParent(go, false);
            labelGo.anchorMin = Vector2.zero;
            labelGo.anchorMax = Vector2.one;
            labelGo.offsetMin = Vector2.zero;
            labelGo.offsetMax = Vector2.zero;
            var label = labelGo.gameObject.AddComponent<Text>();
            label.font = font;
            label.fontSize = fontSize;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.UpperLeft;
            label.color = new Color(0.55f, 0.9f, 0.55f, 1f);
            label.text = part.Text;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;

            var le = go.gameObject.AddComponent<LayoutElement>();
            le.minHeight = LineHeight;
            le.preferredWidth = label.preferredWidth + 4f;
            le.minWidth = le.preferredWidth;
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }
    }
}
