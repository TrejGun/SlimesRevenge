using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SlimesRevenge
{
    /// <summary>Shared uGUI helpers for code-built menus (Main / duel setup / game over).</summary>
    public static class MenuUi
    {
        public const float ButtonHeight = 48f;
        public const float ButtonWidth = 160f;
        public const float ChipWidth = 88f;
        public const float ChipHeight = 110f;
        public static readonly Vector2 ReferenceResolution = new Vector2(390f, 844f);
        public static readonly Color32 PanelColor = new Color32(24, 32, 20, 240);
        public static readonly Color32 ButtonColor = new Color32(48, 64, 40, 255);
        public static readonly Color32 ChipColor = new Color32(40, 56, 34, 255);
        public static readonly Color32 HighlightColor = new Color32(210, 180, 48, 255);
        public static readonly Color32 DimColor = new Color(0f, 0f, 0f, 0.55f);

        public static Font ResolveFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Font.CreateDynamicFontFromOSFont("Helvetica", 18);
        }

        public static Canvas CreateCanvas(string name, int sortingOrder)
        {
            EnsureEventSystem();
            var root = new GameObject(name);
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static RectTransform CreateHost(Transform parent, string name)
        {
            var host = new GameObject(name).AddComponent<RectTransform>();
            host.SetParent(parent, false);
            host.anchorMin = Vector2.zero;
            host.anchorMax = Vector2.one;
            host.offsetMin = Vector2.zero;
            host.offsetMax = Vector2.zero;
            return host;
        }

        public static Image CreateFullscreenDim(Transform parent, Color color)
        {
            var image = new GameObject("Dim").AddComponent<Image>();
            image.transform.SetParent(parent, false);
            image.color = color;
            var rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return image;
        }

        public static Text AddFloatingLabel(
            Transform parent,
            Font font,
            string text,
            int fontSize,
            Vector2 anchoredPos
        )
        {
            var row = new GameObject("Title").AddComponent<RectTransform>();
            row.SetParent(parent, false);
            row.anchorMin = row.anchorMax = new Vector2(0.5f, 0.5f);
            row.pivot = new Vector2(0.5f, 0.5f);
            row.sizeDelta = new Vector2(320f, 40f);
            row.anchoredPosition = anchoredPos;
            var label = row.gameObject.AddComponent<Text>();
            label.font = font;
            label.fontSize = fontSize;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.text = text;
            label.raycastTarget = false;
            return label;
        }

        public static Button AddFloatingButton(
            Transform parent,
            Font font,
            string label,
            Vector2 anchoredPos,
            Action action,
            float width = ButtonWidth,
            float height = ButtonHeight
        )
        {
            var row = new GameObject(label).AddComponent<RectTransform>();
            row.SetParent(parent, false);
            row.anchorMin = row.anchorMax = new Vector2(0.5f, 0.5f);
            row.pivot = new Vector2(0.5f, 0.5f);
            row.sizeDelta = new Vector2(width, height);
            row.anchoredPosition = anchoredPos;
            var image = row.gameObject.AddComponent<Image>();
            image.color = ButtonColor;
            var button = row.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            if (action != null)
            {
                button.onClick.AddListener(() => action());
            }

            var text = new GameObject("Label").AddComponent<Text>();
            text.transform.SetParent(row, false);
            text.font = font;
            text.fontSize = 20;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = label;
            text.raycastTarget = false;
            var textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            return button;
        }

        /// <summary>
        /// Standalone chip (own frame, no shared plate): optional static icon or animated frames.
        /// </summary>
        public static Button AddFloatingChip(
            Transform parent,
            Font font,
            string caption,
            Sprite[] animFrames,
            Sprite staticIcon,
            Vector2 anchoredPos,
            Action action,
            float width = ChipWidth,
            float height = ChipHeight
        )
        {
            var row = new GameObject(caption).AddComponent<RectTransform>();
            row.SetParent(parent, false);
            row.anchorMin = row.anchorMax = new Vector2(0.5f, 0.5f);
            row.pivot = new Vector2(0.5f, 0.5f);
            row.sizeDelta = new Vector2(width, height);
            row.anchoredPosition = anchoredPos;

            var image = row.gameObject.AddComponent<Image>();
            image.color = ChipColor;
            var button = row.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            if (action != null)
            {
                button.onClick.AddListener(() => action());
            }

            var iconGo = new GameObject("Icon").AddComponent<RectTransform>();
            iconGo.SetParent(row, false);
            iconGo.anchorMin = new Vector2(0.5f, 0.48f);
            iconGo.anchorMax = new Vector2(0.5f, 0.48f);
            iconGo.pivot = new Vector2(0.5f, 0.5f);
            iconGo.sizeDelta = new Vector2(56f, 56f);
            var iconImage = iconGo.gameObject.AddComponent<Image>();
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            if (animFrames != null && animFrames.Length > 0)
            {
                var anim = iconGo.gameObject.AddComponent<UiSpriteAnimator>();
                anim.Play(animFrames);
            }
            else if (staticIcon != null)
            {
                iconImage.sprite = staticIcon;
            }

            var text = new GameObject("Label").AddComponent<Text>();
            text.transform.SetParent(row, false);
            text.font = font;
            text.fontSize = 14;
            text.alignment = TextAnchor.LowerCenter;
            text.color = Color.white;
            text.text = caption;
            text.raycastTarget = false;
            var textRect = text.rectTransform;
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 0.32f);
            textRect.offsetMin = new Vector2(2f, 4f);
            textRect.offsetMax = new Vector2(-2f, 0f);

            return button;
        }

        public static void PlaceRow(IReadOnlyList<RectTransform> items, float y, float gap)
        {
            if (items == null || items.Count == 0)
            {
                return;
            }

            var total = 0f;
            for (var i = 0; i < items.Count; i++)
            {
                total += items[i].sizeDelta.x;
            }

            total += gap * (items.Count - 1);
            var x = -total * 0.5f;
            for (var i = 0; i < items.Count; i++)
            {
                var w = items[i].sizeDelta.x;
                items[i].anchoredPosition = new Vector2(x + w * 0.5f, y);
                x += w + gap;
            }
        }

        public static void PlacePair(
            RectTransform left,
            RectTransform right,
            float y,
            float gap = 16f
        )
        {
            var lw = left.sizeDelta.x;
            var rw = right.sizeDelta.x;
            var total = lw + gap + rw;
            left.anchoredPosition = new Vector2(-total * 0.5f + lw * 0.5f, y);
            right.anchoredPosition = new Vector2(total * 0.5f - rw * 0.5f, y);
        }

        public static IEnumerator FlashHighlight(Button button, float seconds = 0.2f)
        {
            if (button == null)
            {
                yield break;
            }

            var image = button.targetGraphic as Image;
            if (image == null)
            {
                yield break;
            }

            var previous = image.color;
            image.color = HighlightColor;
            yield return new WaitForSeconds(seconds);
            if (image != null)
            {
                image.color = previous;
            }
        }

        public static void ClearChildren(Transform parent)
        {
            if (parent == null)
            {
                return;
            }

            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i).gameObject;
                child.transform.SetParent(null, false);
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(child);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(child);
                }
            }
        }

        public static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        public static void QuitApp()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
