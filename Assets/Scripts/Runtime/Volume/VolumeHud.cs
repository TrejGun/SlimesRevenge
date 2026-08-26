using UnityEngine;
using UnityEngine.UI;

namespace SlimesRevenge
{
    public sealed class VolumeHud : MonoBehaviour
    {
        private const int Columns = 5;
        private const float Slot = 36f;
        private const float Gap = 4f;

        private static readonly Color EmptyFill = new Color32(28, 32, 24, 180);
        private static readonly Color FrameTint = new Color32(92, 104, 72, 255);

        private Creature target;
        private Image[] fills;
        private RectTransform panel;
        private Canvas canvas;
        private Texture2D fillTexture;
        private Texture2D frameTexture;

        public void Bind(Creature creature)
        {
            target = creature;
            Refresh();
        }

        private void Awake()
        {
            Build();
        }

        private void LateUpdate()
        {
            Refresh();
            ApplySafePadding();
        }

        private void OnDestroy()
        {
            if (canvas != null)
            {
                Destroy(canvas.gameObject);
            }
        }

        private void Build()
        {
            var canvasObject = new GameObject("VolumeHud");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(390f, 844f);
            scaler.matchWidthOrHeight = 0.5f;

            panel = new GameObject("Slots").AddComponent<RectTransform>();
            panel.SetParent(canvas.transform, false);
            panel.anchorMin = panel.anchorMax = new Vector2(1f, 1f);
            panel.pivot = new Vector2(1f, 1f);
            var rows = Mathf.CeilToInt((float)Volume.Capacity / Columns);
            panel.sizeDelta = new Vector2(
                Columns * Slot + (Columns - 1) * Gap,
                rows * Slot + (rows - 1) * Gap);

            var fillSprite = CreateSprite(ref fillTexture, Color.white, false);
            var frameSprite = CreateSprite(ref frameTexture, Color.white, true);
            fills = new Image[Volume.Capacity];
            for (var i = 0; i < Volume.Capacity; i++)
            {
                var column = i % Columns;
                var row = i / Columns;
                var slot = CreateSlot(panel, fillSprite, frameSprite);
                slot.anchoredPosition = new Vector2(
                    column * (Slot + Gap),
                    -row * (Slot + Gap));
                fills[i] = slot.GetComponent<Image>();
            }
        }

        private static RectTransform CreateSlot(RectTransform parent, Sprite fillSprite, Sprite frameSprite)
        {
            var slot = new GameObject("Slot").AddComponent<RectTransform>();
            slot.SetParent(parent, false);
            slot.anchorMin = slot.anchorMax = new Vector2(0f, 1f);
            slot.pivot = new Vector2(0f, 1f);
            slot.sizeDelta = new Vector2(Slot, Slot);

            var fill = slot.gameObject.AddComponent<Image>();
            fill.sprite = fillSprite;
            fill.color = EmptyFill;
            fill.raycastTarget = false;

            var frame = new GameObject("Frame").AddComponent<RectTransform>();
            frame.SetParent(slot, false);
            frame.anchorMin = Vector2.zero;
            frame.anchorMax = Vector2.one;
            frame.offsetMin = Vector2.zero;
            frame.offsetMax = Vector2.zero;
            var frameImage = frame.gameObject.AddComponent<Image>();
            frameImage.sprite = frameSprite;
            frameImage.color = FrameTint;
            frameImage.raycastTarget = false;
            return slot;
        }

        private Sprite CreateSprite(ref Texture2D texture, Color color, bool frameOnly)
        {
            const int size = 16;
            texture = new Texture2D(size, size, TextureFormat.ARGB32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var border = x <= 1 || y <= 1 || x >= size - 2 || y >= size - 2;
                    if (frameOnly)
                    {
                        texture.SetPixel(x, y, border ? color : Color.clear);
                    }
                    else
                    {
                        texture.SetPixel(x, y, border ? Color.clear : color);
                    }
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private void Refresh()
        {
            if (fills == null)
            {
                return;
            }

            var units = target != null ? target.Volume.Units : null;
            for (var i = 0; i < fills.Length; i++)
            {
                if (units != null && i < units.Count && units[i].Substance != null)
                {
                    fills[i].color = units[i].Substance.Color;
                }
                else
                {
                    fills[i].color = EmptyFill;
                }
            }
        }

        private void ApplySafePadding()
        {
            if (panel == null || canvas == null)
            {
                return;
            }

            var scale = Mathf.Max(canvas.scaleFactor, 0.001f);
            var safe = Screen.safeArea;
            var top = (Screen.height - (safe.y + safe.height)) / scale + 12f;
            var right = (Screen.width - (safe.x + safe.width)) / scale + 12f;
            panel.anchoredPosition = new Vector2(-right, -top);
        }
    }
}
