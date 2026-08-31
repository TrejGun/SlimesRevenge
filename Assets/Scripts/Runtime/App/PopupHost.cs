using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SlimesRevenge
{
    /// <summary>
    /// Stacked card shell (panel + Back). Full-screen dim is optional via <see cref="Dim"/>
    /// and applied on each refresh — info cards leave it off; choice-blocking callers can turn it on.
    /// </summary>
    public sealed class PopupHost : MonoBehaviour
    {
        private const float PanelWidth = 320f;
        private const float PanelHeight = 420f;
        private const float FooterHeight = 48f;
        private const float Pad = 12f;
        private static readonly Color DimColor = new Color(0f, 0f, 0f, 0.45f);

        private readonly Stack<IPopupContent> stack = new Stack<IPopupContent>();

        private Canvas canvas;
        private GameObject dimRoot;
        private RectTransform contentRoot;
        private Button backButton;
        private Text backLabel;
        private Font font;
        private bool dim;

        public static PopupHost Instance { get; private set; }

        public bool IsOpen => canvas != null && canvas.enabled && stack.Count > 0;

        /// <summary>
        /// When true, a full-screen dim is shown on refresh and blocks world / log input.
        /// Default false for informational card stacks.
        /// </summary>
        public bool Dim
        {
            get => dim;
            set
            {
                if (dim == value)
                {
                    return;
                }

                dim = value;
                if (IsOpen)
                {
                    ApplyDim();
                }
            }
        }

        /// <summary>Blocks the world only while open with <see cref="Dim"/> enabled.</summary>
        public bool BlocksInput => Dim && IsOpen;

        public int Depth => stack.Count;

        public Type PeekType => stack.Count > 0 ? stack.Peek().GetType() : null;

        /// <summary>
        /// Returns the shared host, creating one if the scene has none yet
        /// (action-log links and Info must not silently no-op).
        /// </summary>
        public static PopupHost Ensure()
        {
            if (Instance != null)
            {
                return Instance;
            }

            var host = new GameObject("PopupHost").AddComponent<PopupHost>();
            Instance = host;
            host.EnsureUi();
            return host;
        }

        private void Awake()
        {
            Instance = this;
            EnsureUi();
            Close();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            canvas = null;
        }

        public void Push(IPopupContent content)
        {
            if (content == null)
            {
                return;
            }

            EnsureUi();
            stack.Push(content);
            Refresh();
        }

        public void Back()
        {
            if (stack.Count == 0)
            {
                Close();
                return;
            }

            stack.Pop();
            if (stack.Count == 0)
            {
                Close();
                return;
            }

            Refresh();
        }

        public void Close()
        {
            stack.Clear();
            if (canvas != null)
            {
                canvas.enabled = false;
            }

            ApplyDim();
            ClearContent();
        }

        private void Refresh()
        {
            EnsureUi();
            ClearContent();
            canvas.enabled = true;
            if (stack.Count == 0)
            {
                canvas.enabled = false;
                ApplyDim();
                return;
            }

            ApplyDim();
            stack.Peek().Build(contentRoot, this);
            if (backLabel != null)
            {
                backLabel.text = SafeBackLabel();
            }
        }

        private void ApplyDim()
        {
            if (dimRoot == null)
            {
                return;
            }

            var show = Dim && IsOpen;
            dimRoot.SetActive(show);
        }

        private static string SafeBackLabel() => I18n.GetOr(TextKey.UiBack, "Back");

        private void ClearContent()
        {
            if (contentRoot == null)
            {
                return;
            }

            for (var i = contentRoot.childCount - 1; i >= 0; i--)
            {
                var child = contentRoot.GetChild(i).gameObject;
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

            CardUi.Font = font;

            var root = new GameObject("PopupCanvas");
            root.transform.SetParent(transform, false);
            canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 220;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(390f, 844f);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();

            dimRoot = new GameObject("Dim");
            var dimRect = dimRoot.AddComponent<RectTransform>();
            dimRect.SetParent(canvas.transform, false);
            dimRect.anchorMin = Vector2.zero;
            dimRect.anchorMax = Vector2.one;
            dimRect.offsetMin = Vector2.zero;
            dimRect.offsetMax = Vector2.zero;
            var dimImage = dimRoot.AddComponent<Image>();
            dimImage.color = DimColor;
            var dimButton = dimRoot.AddComponent<Button>();
            dimButton.transition = Selectable.Transition.None;
            dimButton.onClick.AddListener(Back);
            dimRoot.SetActive(false);

            var panel = new GameObject("Panel").AddComponent<RectTransform>();
            panel.SetParent(canvas.transform, false);
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = new Color32(24, 32, 28, 245);

            contentRoot = new GameObject("Content").AddComponent<RectTransform>();
            contentRoot.SetParent(panel, false);
            contentRoot.anchorMin = new Vector2(0f, 0f);
            contentRoot.anchorMax = new Vector2(1f, 1f);
            contentRoot.offsetMin = new Vector2(Pad, FooterHeight + Pad);
            contentRoot.offsetMax = new Vector2(-Pad, -Pad);
            var layout = contentRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.spacing = 6f;
            layout.padding = new RectOffset(0, 0, 0, 0);
            var fitter = contentRoot.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var footer = new GameObject("Footer").AddComponent<RectTransform>();
            footer.SetParent(panel, false);
            footer.anchorMin = new Vector2(0f, 0f);
            footer.anchorMax = new Vector2(1f, 0f);
            footer.pivot = new Vector2(0.5f, 0f);
            footer.sizeDelta = new Vector2(0f, FooterHeight);
            footer.anchoredPosition = Vector2.zero;

            var backGo = new GameObject("Back").AddComponent<RectTransform>();
            backGo.SetParent(footer, false);
            backGo.anchorMin = new Vector2(0.1f, 0.15f);
            backGo.anchorMax = new Vector2(0.9f, 0.85f);
            backGo.offsetMin = Vector2.zero;
            backGo.offsetMax = Vector2.zero;
            var backImage = backGo.gameObject.AddComponent<Image>();
            backImage.color = new Color32(48, 64, 44, 255);
            backButton = backGo.gameObject.AddComponent<Button>();
            backButton.targetGraphic = backImage;
            backButton.onClick.AddListener(Back);

            var labelGo = new GameObject("Label").AddComponent<RectTransform>();
            labelGo.SetParent(backGo, false);
            labelGo.anchorMin = Vector2.zero;
            labelGo.anchorMax = Vector2.one;
            labelGo.offsetMin = Vector2.zero;
            labelGo.offsetMax = Vector2.zero;
            backLabel = labelGo.gameObject.AddComponent<Text>();
            backLabel.font = font;
            backLabel.fontSize = 18;
            backLabel.alignment = TextAnchor.MiddleCenter;
            backLabel.color = Color.white;
            backLabel.text = SafeBackLabel();
            backLabel.raycastTarget = false;
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
