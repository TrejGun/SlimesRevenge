using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SlimesRevenge
{
    public sealed class CombatMenu : MonoBehaviour
    {
        private const float ButtonHeight = 48f;
        private const float ButtonWidth = 240f;
        private const float Gap = 8f;

        private Canvas canvas;
        private RectTransform panel;
        private Font font;
        private bool suppressWorldInput;
        private Action<Substance> onPick;
        private Action onMess;
        private Action onCollect;
        private Action<int> onDevour;
        private IReadOnlyList<Substance> kinds;
        private IReadOnlyList<Corpse> corpses;

        public bool BlocksInput => canvas != null && canvas.enabled || suppressWorldInput;

        private void Awake()
        {
            EnsureUi();
            Close();
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

            BuildCanvas();
        }

        private void LateUpdate()
        {
            if (suppressWorldInput && !PointerHeld())
            {
                suppressWorldInput = false;
            }
        }

        public void OpenActions(IReadOnlyList<Substance> substances, Action<Substance> onSubstance)
        {
            EnsureUi();
            kinds = substances;
            onPick = onSubstance;
            onMess = null;
            onCollect = null;
            onDevour = null;
            corpses = null;
            ShowActions();
        }

        public void OpenSelf(
            IReadOnlyList<Substance> substances,
            bool canMess,
            bool canCollect,
            IReadOnlyList<Corpse> floorCorpses,
            Action<Substance> mess,
            Action collect,
            Action<int> devour)
        {
            EnsureUi();
            kinds = substances;
            onPick = mess;
            onMess = canMess ? () => ShowSubstances() : (Action)null;
            onCollect = canCollect ? collect : null;
            onDevour = devour;
            corpses = floorCorpses;
            ShowSelf();
        }

        public void Close()
        {
            if (canvas != null)
            {
                canvas.enabled = false;
            }

            if (panel != null)
            {
                ClearPanel();
            }

            onPick = null;
            onMess = null;
            onCollect = null;
            onDevour = null;
            kinds = null;
            corpses = null;
        }

        private void ShowActions()
        {
            ClearPanel();
            canvas.enabled = true;
            AddButton(I18n.Get(TextKey.CombatAttack), ShowSubstances);
            Layout(1);
        }

        private void ShowSelf()
        {
            ClearPanel();
            canvas.enabled = true;
            var count = 0;
            if (onMess != null)
            {
                AddButton(I18n.Get(TextKey.CombatMess), onMess);
                count++;
            }

            if (onCollect != null)
            {
                var collect = onCollect;
                AddButton(I18n.Get(TextKey.CombatCollect), () =>
                {
                    suppressWorldInput = true;
                    Close();
                    collect();
                });
                count++;
            }

            if (onDevour != null && corpses != null && corpses.Count > 0 && !BusyDevourBlocked())
            {
                AddButton(I18n.Get(TextKey.CombatDevour), ShowCorpses);
                count++;
            }

            if (count == 0)
            {
                Close();
                return;
            }

            Layout(count);
        }

        private bool BusyDevourBlocked()
        {
            return false;
        }

        private void ShowCorpses()
        {
            ClearPanel();
            canvas.enabled = true;
            if (corpses == null)
            {
                return;
            }

            for (var i = 0; i < corpses.Count; i++)
            {
                var index = i;
                var corpse = corpses[i];
                AddButton($"{corpse.Kind} ({corpse.Volume.UnitCount})", () =>
                {
                    var devour = onDevour;
                    suppressWorldInput = true;
                    Close();
                    devour?.Invoke(index);
                });
            }

            Layout(corpses.Count);
        }

        private void ShowSubstances()
        {
            ClearPanel();
            canvas.enabled = true;
            if (kinds == null)
            {
                return;
            }

            foreach (var substance in kinds)
            {
                var chosen = substance;
                AddButton(chosen.Label, () => Pick(chosen), chosen.Color);
            }

            Layout(kinds.Count);
        }

        private void Pick(Substance substance)
        {
            var pick = onPick;
            suppressWorldInput = true;
            Close();
            pick?.Invoke(substance);
        }

        private void BuildCanvas()
        {
            var root = new GameObject("CombatMenu");
            canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(390f, 844f);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();

            var dim = CreateImage(root.transform, "Dim", new Color(0f, 0f, 0f, 0.45f));
            var dimRect = dim.rectTransform;
            dimRect.anchorMin = Vector2.zero;
            dimRect.anchorMax = Vector2.one;
            dimRect.offsetMin = Vector2.zero;
            dimRect.offsetMax = Vector2.zero;
            var dimButton = dim.gameObject.AddComponent<Button>();
            dimButton.transition = Selectable.Transition.None;
            dimButton.onClick.AddListener(Cancel);

            panel = new GameObject("Panel").AddComponent<RectTransform>();
            panel.SetParent(canvas.transform, false);
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = new Color32(24, 32, 20, 240);
            panelImage.raycastTarget = true;
        }

        private void AddButton(string label, Action action, Color? swatch = null)
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
            textRect.offsetMin = swatch.HasValue ? new Vector2(44f, 0f) : Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            if (!swatch.HasValue)
            {
                return;
            }

            var chip = CreateImage(row, "Swatch", swatch.Value);
            var chipRect = chip.rectTransform;
            chipRect.anchorMin = chipRect.anchorMax = new Vector2(0f, 0.5f);
            chipRect.pivot = new Vector2(0.5f, 0.5f);
            chipRect.anchoredPosition = new Vector2(24f, 0f);
            chipRect.sizeDelta = new Vector2(22f, 22f);
            chip.raycastTarget = false;
        }

        private void Layout(int count)
        {
            var height = count * ButtonHeight + (count - 1) * Gap + 24f;
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
                y -= ButtonHeight + Gap;
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

        private void Cancel()
        {
            suppressWorldInput = true;
            Close();
        }

        private static bool PointerHeld()
        {
            return Input.GetMouseButton(0) || Input.touchCount > 0;
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
