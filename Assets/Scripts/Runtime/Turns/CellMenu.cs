using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SlimesRevenge
{
    public sealed class CellMenu : MonoBehaviour
    {
        private const float ButtonHeight = 48f;
        private const float ButtonWidth = 280f;
        private const float Gap = 8f;

        private static readonly IMenuEntry[] RootEntries =
        {
            new AttackEntry(),
            new MessEntry(),
            new CollectEntry(),
            new DevourEntry(),
            new InfoEntry(),
        };

        private Canvas canvas;
        private RectTransform panel;
        private Font font;
        private bool suppressWorldInput;
        private CellMenuContext context;
        private readonly Stack<Action> levels = new Stack<Action>();

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

        public void Open(CellMenuContext menuContext)
        {
            EnsureUi();
            context = menuContext;
            levels.Clear();
            Push(RenderRoot);
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

            levels.Clear();
            context = null;
        }

        /// <summary>Close after a successful action; suppress world input until the pointer is released.</summary>
        public void CompleteAction()
        {
            suppressWorldInput = true;
            Close();
        }

        public void ShowSubstancePicker(Action<Substance> onPick)
        {
            if (context?.Actor == null || !context.Actor.Volume.CanSpend)
            {
                return;
            }

            var kinds = context.Actor.Volume.UniqueKinds();
            if (kinds.Count == 0)
            {
                return;
            }

            Push(() =>
            {
                for (var i = 0; i < kinds.Count; i++)
                {
                    var chosen = kinds[i];
                    AddButton(
                        chosen.Label,
                        () =>
                        {
                            suppressWorldInput = true;
                            onPick?.Invoke(chosen);
                        },
                        chosen.Color
                    );
                }

                AddBackButton();
                Layout(kinds.Count + 1);
            });
        }

        public void ShowCorpsePicker(CellMenuContext menuContext)
        {
            Push(() =>
            {
                var corpses = menuContext.Corpses;
                for (var i = 0; i < corpses.Count; i++)
                {
                    var index = i;
                    var corpse = corpses[i];
                    var edible = Digesting.CanBegin(menuContext.Actor?.Volume, corpse);
                    AddButton(
                        CorpseChoiceLabel(corpse),
                        () => DevourEntry.TryDevourAt(this, menuContext, index),
                        enabled: edible
                    );
                }

                AddBackButton();
                Layout(corpses.Count + 1);
            });
        }

        public void ShowInspectablePicker(IReadOnlyList<CellInspectable> inspectables)
        {
            Push(() =>
            {
                for (var i = 0; i < inspectables.Count; i++)
                {
                    var target = inspectables[i];
                    AddButton(target.PickerLabel, () => ShowInfoPanel(target));
                }

                AddBackButton();
                Layout(inspectables.Count + 1);
            });
        }

        public void ShowInfoPanel(CellInspectable target)
        {
            var host = PopupHost.Ensure();
            if (target == null)
            {
                return;
            }

            if (target.Puddle != null)
            {
                host.Push(new SubstanceCard(target.Puddle.Substance));
                return;
            }

            if (target.Creature != null)
            {
                host.Push(new CreatureCard(target.Creature));
            }
        }

        /// <summary>
        /// Menu line: name · filled volume · remaining/max decay turns.
        /// </summary>
        public static string CorpseChoiceLabel(Creature corpse)
        {
            if (corpse == null)
            {
                return string.Empty;
            }

            var units = corpse.Volume != null ? corpse.Volume.UnitCount : 0;
            var maxTurns = Mathf.Max(1, corpse.MaxHitPoints);
            return CardUi.SafeFormat(
                TextKey.MenuCorpseChoice,
                "{0} · {1} vol · {2}/{3} turns",
                CardUi.CreatureName(corpse.Kind),
                units,
                corpse.DecayTurnsLeft,
                maxTurns
            );
        }

        private void RenderRoot()
        {
            if (context == null)
            {
                Close();
                return;
            }

            var visible = 0;
            for (var i = 0; i < RootEntries.Length; i++)
            {
                var entry = RootEntries[i];
                if (!entry.IsVisible(context))
                {
                    continue;
                }

                var captured = entry;
                AddButton(
                    captured.Label,
                    () => captured.Activate(this, context),
                    enabled: captured.IsEnabled(context)
                );
                visible++;
            }

            if (visible == 0)
            {
                Close();
                return;
            }

            Layout(visible);
        }

        private void Push(Action render)
        {
            levels.Push(render);
            Refresh();
        }

        private void Back()
        {
            if (levels.Count <= 1)
            {
                Cancel();
                return;
            }

            levels.Pop();
            Refresh();
        }

        private void Refresh()
        {
            if (levels.Count == 0)
            {
                Close();
                return;
            }

            ClearPanel();
            canvas.enabled = true;
            levels.Peek().Invoke();
        }

        private void BuildCanvas()
        {
            var root = new GameObject("CellMenu");
            root.transform.SetParent(transform, false);
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

        private void AddButton(
            string label,
            Action action,
            Color? swatch = null,
            bool enabled = true
        )
        {
            var row = new GameObject(label).AddComponent<RectTransform>();
            row.SetParent(panel, false);
            row.sizeDelta = new Vector2(ButtonWidth, ButtonHeight);
            var image = row.gameObject.AddComponent<Image>();
            image.color = enabled ? new Color32(48, 64, 40, 255) : new Color32(36, 40, 32, 255);
            var button = row.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.interactable = enabled;
            if (enabled)
            {
                button.onClick.AddListener(() => action());
            }

            var text = new GameObject("Label").AddComponent<Text>();
            text.transform.SetParent(row, false);
            text.font = font;
            text.fontSize = 22;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = enabled ? Color.white : new Color(1f, 1f, 1f, 0.4f);
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

        private void AddBackButton()
        {
            AddButton(CardUi.Safe(TextKey.UiBack, "Back"), Back);
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
            if (canvas == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(canvas.gameObject);
            }
            else
            {
                DestroyImmediate(canvas.gameObject);
            }
        }
    }
}
