using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SlimesRevenge
{
    /// <summary>
    /// Main menu on the title scene (Duel / Campaign / Credits / Quit).
    /// Not a splash: cold-start art lives on the Splash bootstrap scene only.
    /// </summary>
    public sealed class MainMenu : MonoBehaviour
    {
        private const float ChipGap = 18f;
        private const float FooterY = -150f;
        private const float RowY = 28f;

        private Canvas canvas;
        private RectTransform host;
        private Font font;
        private Substance[] loadout;
        private CreatureKind opponent;
        private int editingSlot = -1;
        private Button selectedOpponentButton;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void RegisterBoot()
        {
            // AfterSceneLoad runs only for the first scene (Splash). Menu must spawn on every Main load.
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != AppNavigation.MainScene)
            {
                return;
            }

            if (FindAnyObjectByType<MainMenu>() != null)
            {
                return;
            }

            new GameObject("MainMenu").AddComponent<MainMenu>();
        }

        private void Awake()
        {
            Application.targetFrameRate = 30;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.orientation = ScreenOrientation.LandscapeLeft;

            loadout = RunConfig.DefaultWaterLoadout();
            var opponents = CreatureCatalog.Opponents;
            opponent = opponents.Count > 0 ? opponents[0].Kind : CreatureKind.Bat;
        }

        private void Start()
        {
            EnsureUi();
            ShowRoot();
        }

        private void EnsureUi()
        {
            if (canvas != null)
            {
                return;
            }

            font = MenuUi.ResolveFont();
            canvas = MenuUi.CreateCanvas("MainMenuCanvas", 100);
            MenuUi.CreateFullscreenDim(canvas.transform, MenuUi.DimColor);
            host = MenuUi.CreateHost(canvas.transform, "Content");
        }

        private void ClearContent()
        {
            selectedOpponentButton = null;
            editingSlot = -1;
            MenuUi.ClearChildren(host);
        }

        private void ShowRoot()
        {
            ClearContent();
            MenuUi.AddFloatingLabel(
                host,
                font,
                I18n.GetOr(TextKey.MenuTitle, "Slime's Revenge"),
                28,
                new Vector2(0f, 90f)
            );
            MenuUi.AddFloatingButton(
                host,
                font,
                I18n.GetOr(TextKey.MenuDuel, "Duel"),
                new Vector2(0f, 30f),
                ShowOpponentSelect
            );
            MenuUi.AddFloatingButton(
                host,
                font,
                I18n.GetOr(TextKey.MenuCampaign, "Campaign"),
                new Vector2(0f, -30f),
                StartCampaign
            );
            MenuUi.AddFloatingButton(
                host,
                font,
                I18n.GetOr(TextKey.MenuCredits, "Credits"),
                new Vector2(0f, -90f),
                ShowCredits
            );
            MenuUi.AddFloatingButton(
                host,
                font,
                I18n.GetOr(TextKey.MenuQuit, "Quit"),
                new Vector2(0f, -150f),
                MenuUi.QuitApp
            );
        }

        private void StartCampaign()
        {
            RunConfig.SetCampaign();
            AppNavigation.GoToGame();
        }

        private void ShowCredits()
        {
            ClearContent();
            MenuUi.AddFloatingLabel(
                host,
                font,
                I18n.GetOr(TextKey.MenuCreditsTitle, "Credits"),
                26,
                new Vector2(0f, 40f)
            );
            MenuUi.AddFloatingLabel(
                host,
                font,
                I18n.GetOr(TextKey.MenuCreditsBody, "TrejGun\nhello@trejgun.com"),
                18,
                new Vector2(0f, -10f)
            );
            MenuUi.AddFloatingButton(
                host,
                font,
                I18n.Get(TextKey.UiBack),
                new Vector2(0f, FooterY),
                ShowRoot
            );
        }

        private void ShowOpponentSelect()
        {
            ClearContent();
            var opponents = CreatureCatalog.Opponents;
            var chips = new List<RectTransform>(opponents.Count);
            for (var i = 0; i < opponents.Count; i++)
            {
                var kind = opponents[i].Kind;
                var frames = CreatureSheet.Portrait(kind);
                var button = MenuUi.AddFloatingChip(
                    host,
                    font,
                    I18n.Creature(kind),
                    frames,
                    frames.Length > 0 ? frames[0] : IconCatalog.Creature(kind),
                    Vector2.zero,
                    null,
                    MenuUi.ChipWidth,
                    124f,
                    FdrSheetLayout.BattleCellPixels
                );
                button.onClick.AddListener(() => SelectOpponent(kind, button));
                chips.Add(button.transform as RectTransform);
            }

            MenuUi.PlaceRow(chips, RowY, ChipGap);

            var back = MenuUi.AddFloatingButton(
                host,
                font,
                I18n.Get(TextKey.UiBack),
                Vector2.zero,
                ShowRoot
            );
            var next = MenuUi.AddFloatingButton(
                host,
                font,
                I18n.GetOr(TextKey.MenuNext, "Next"),
                Vector2.zero,
                ShowLoadout
            );
            MenuUi.PlacePair(
                back.transform as RectTransform,
                next.transform as RectTransform,
                FooterY
            );
        }

        private void SelectOpponent(CreatureKind kind, Button button)
        {
            if (selectedOpponentButton != null && selectedOpponentButton != button)
            {
                var previous = selectedOpponentButton.targetGraphic as Image;
                if (previous != null)
                {
                    previous.color = MenuUi.ChipColor;
                }
            }

            opponent = kind;
            selectedOpponentButton = button;
            var image = button != null ? button.targetGraphic as Image : null;
            if (image != null)
            {
                image.color = MenuUi.HighlightColor;
            }
        }

        private void ShowLoadout()
        {
            selectedOpponentButton = null;
            editingSlot = -1;
            MenuUi.ClearChildren(host);

            var chips = new List<RectTransform>(RunConfig.LoadoutSlots);
            for (var i = 0; i < RunConfig.LoadoutSlots; i++)
            {
                var slot = i;
                var substance = loadout[slot] ?? new Water();
                var button = MenuUi.AddFloatingChip(
                    host,
                    font,
                    substance.Label,
                    null,
                    substance.Icon,
                    Vector2.zero,
                    () => ShowSubstancePicker(slot)
                );
                chips.Add(button.transform as RectTransform);
            }

            MenuUi.PlaceRow(chips, RowY, ChipGap);

            var back = MenuUi.AddFloatingButton(
                host,
                font,
                I18n.Get(TextKey.UiBack),
                Vector2.zero,
                ShowOpponentSelect
            );
            var start = MenuUi.AddFloatingButton(
                host,
                font,
                I18n.GetOr(TextKey.MenuStartDuel, "Start duel"),
                Vector2.zero,
                StartDuel
            );
            MenuUi.PlacePair(
                back.transform as RectTransform,
                start.transform as RectTransform,
                FooterY
            );
        }

        private void ShowSubstancePicker(int slot)
        {
            editingSlot = slot;
            MenuUi.ClearChildren(host);

            var samples = SubstanceCatalog.Samples;
            var chips = new List<RectTransform>(samples.Count);
            var chipW = samples.Count > 5 ? 72f : MenuUi.ChipWidth;
            for (var i = 0; i < samples.Count; i++)
            {
                var sample = samples[i];
                var button = MenuUi.AddFloatingChip(
                    host,
                    font,
                    sample.Label,
                    null,
                    sample.Icon,
                    Vector2.zero,
                    () =>
                    {
                        if (editingSlot >= 0 && editingSlot < loadout.Length)
                        {
                            loadout[editingSlot] = SubstanceCatalog.Create(sample);
                        }

                        ShowLoadout();
                    },
                    chipW,
                    MenuUi.ChipHeight
                );
                chips.Add(button.transform as RectTransform);
            }

            MenuUi.PlaceRow(chips, RowY, 12f);
            MenuUi.AddFloatingButton(
                host,
                font,
                I18n.Get(TextKey.UiBack),
                new Vector2(0f, FooterY),
                ShowLoadout
            );
        }

        private void StartDuel()
        {
            RunConfig.SetDuel(opponent, loadout);
            AppNavigation.GoToGame();
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
