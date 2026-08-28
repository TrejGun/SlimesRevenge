using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SlimesRevenge
{
    /// <summary>
    /// Hardcore defeat screen: title + return to Main. Softcore can skip this via TurnManager hooks.
    /// </summary>
    public sealed class GameOverScreen : MonoBehaviour
    {
        private const float ButtonHeight = 48f;
        private const float ButtonWidth = 240f;
        private Canvas canvas;
        private RectTransform panel;
        private Font font;

        public bool IsOpen => canvas != null && canvas.enabled;

        private void Awake()
        {
            EnsureUi();
            Hide();
        }

        public void Show(Action onMenu = null)
        {
            EnsureUi();
            ClearPanel();
            canvas.enabled = true;
            AddLabel(I18n.Get(TextKey.GameOver));
            AddButton(
                I18n.Get(TextKey.GameMenu),
                () =>
                {
                    Hide();
                    if (onMenu != null)
                    {
                        onMenu();
                    }
                    else
                    {
                        AppNavigation.GoToMainMenu();
                    }
                }
            );
            Layout(2);
        }

        public void Hide()
        {
            if (canvas != null)
            {
                canvas.enabled = false;
            }

            if (panel != null)
            {
                ClearPanel();
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
                font = Font.CreateDynamicFontFromOSFont("Helvetica", 18);
            }

            var root = new GameObject("GameOverScreen");
            canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 300;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(390f, 844f);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();

            var dim = CreateImage(root.transform, "Dim", new Color(0f, 0f, 0f, 0.65f));
            var dimRect = dim.rectTransform;
            dimRect.anchorMin = Vector2.zero;
            dimRect.anchorMax = Vector2.one;
            dimRect.offsetMin = Vector2.zero;
            dimRect.offsetMax = Vector2.zero;

            panel = new GameObject("Panel").AddComponent<RectTransform>();
            panel.SetParent(canvas.transform, false);
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = new Color32(24, 32, 20, 240);
        }

        private void AddLabel(string text)
        {
            var row = new GameObject("Title").AddComponent<RectTransform>();
            row.SetParent(panel, false);
            row.sizeDelta = new Vector2(ButtonWidth, ButtonHeight);
            var label = row.gameObject.AddComponent<Text>();
            label.font = font;
            label.fontSize = 28;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.text = text;
            label.raycastTarget = false;
        }

        private void AddButton(string label, Action action)
        {
            var row = new GameObject(label).AddComponent<RectTransform>();
            row.SetParent(panel, false);
            row.sizeDelta = new Vector2(ButtonWidth, ButtonHeight);
            var image = row.gameObject.AddComponent<Image>();
            image.color = new Color32(48, 64, 40, 255);
            var button = row.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => action());

            var text = new GameObject("Label").AddComponent<Text>();
            text.transform.SetParent(row, false);
            text.font = font;
            text.fontSize = 22;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = label;
            text.raycastTarget = false;
            var textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        private void Layout(int count)
        {
            const float gap = 8f;
            var height = count * ButtonHeight + (count - 1) * gap + 24f;
            panel.sizeDelta = new Vector2(ButtonWidth + 24f, height);
            var y = height * 0.5f - 12f - ButtonHeight * 0.5f;
            for (var i = 0; i < panel.childCount; i++)
            {
                var child = panel.GetChild(i) as RectTransform;
                if (child == null)
                {
                    continue;
                }

                child.anchorMin = child.anchorMax = new Vector2(0.5f, 0.5f);
                child.pivot = new Vector2(0.5f, 0.5f);
                child.anchoredPosition = new Vector2(0f, y);
                y -= ButtonHeight + gap;
            }
        }

        private void ClearPanel()
        {
            for (var i = panel.childCount - 1; i >= 0; i--)
            {
                var child = panel.GetChild(i).gameObject;
                child.transform.SetParent(null, false);
                Destroy(child);
            }
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            var image = new GameObject(name).AddComponent<Image>();
            image.transform.SetParent(parent, false);
            image.color = color;
            return image;
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

        private void OnDestroy()
        {
            if (canvas != null)
            {
                Destroy(canvas.gameObject);
            }
        }
    }
}
