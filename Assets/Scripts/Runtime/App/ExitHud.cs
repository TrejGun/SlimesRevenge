using UnityEngine;
using UnityEngine.UI;

namespace SlimesRevenge
{
    /// <summary>
    /// Always-on HUD exit for duel and campaign (same Game scene) → Main menu.
    /// </summary>
    public sealed class ExitHud : MonoBehaviour
    {
        private const float ButtonWidth = 120f;
        private const float ButtonHeight = 40f;

        private Canvas canvas;
        private RectTransform buttonRect;

        private void Awake()
        {
            Build();
        }

        private void LateUpdate()
        {
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
            canvas = MenuUi.CreateCanvas("ExitHud", 110);
            var font = MenuUi.ResolveFont();
            var label = I18n.GetOr(TextKey.GameMenu, "Menu");

            var button = MenuUi.AddFloatingButton(
                canvas.transform,
                font,
                label,
                Vector2.zero,
                AppNavigation.GoToMainMenu,
                ButtonWidth,
                ButtonHeight
            );

            buttonRect = button.GetComponent<RectTransform>();
            buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0f, 1f);
            buttonRect.pivot = new Vector2(0f, 1f);
            buttonRect.anchoredPosition = new Vector2(12f, -12f);
        }

        private void ApplySafePadding()
        {
            if (buttonRect == null || canvas == null)
            {
                return;
            }

            var scale = Mathf.Max(canvas.scaleFactor, 0.001f);
            var safe = Screen.safeArea;
            var top = (Screen.height - (safe.y + safe.height)) / scale + 12f;
            var left = safe.x / scale + 12f;
            buttonRect.anchoredPosition = new Vector2(left, -top);
        }
    }
}
