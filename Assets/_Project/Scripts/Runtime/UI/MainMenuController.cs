using System;
using DG.Tweening;
using DustlineArena.Runtime.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DustlineArena.Runtime.UI
{
    [DisallowMultipleComponent]
    public sealed class MainMenuController : MonoBehaviour
    {
        private const string FirstArenaScene = "Dustline_Arena_01";

        private CanvasGroup mainGroup;
        private RectTransform mainContent;
        private CanvasGroup settingsGroup;
        private RectTransform settingsPanel;
        private CanvasGroup missionGroup;
        private RectTransform missionPanel;
        private CanvasGroup loadingGroup;
        private Button playButton;
        private Button settingsButton;
        private Button quitButton;
        private Button backButton;
        private Button missionBackButton;
        private Button deployButton;
        private bool settingsOpen;
        private bool missionOpen;
        private bool loading;

        private void Awake()
        {
            Time.timeScale = 1f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            EnsureEventSystem();
            BuildInterface();
        }

        private void Start()
        {
            EventSystem.current?.SetSelectedGameObject(playButton.gameObject);
            mainGroup.alpha = 0f;
            mainContent.anchoredPosition += new Vector2(-34f, 0f);

            DOTween.Sequence()
                .SetUpdate(true)
                .Append(TweenCanvasAlpha(mainGroup, 1f, 0.28f))
                .Join(TweenAnchoredPosition(
                    mainContent,
                    mainContent.anchoredPosition + new Vector2(34f, 0f),
                    0.34f).SetEase(Ease.OutCubic));
        }

        private void Update()
        {
            if (loading || !Input.GetKeyDown(KeyCode.Escape))
            {
                return;
            }

            if (settingsOpen)
            {
                CloseSettings();
            }
            else if (missionOpen)
            {
                CloseMissionBrief();
            }
        }

        private void BuildInterface()
        {
            GameObject canvasObject = new GameObject(
                "Main_Menu_Canvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform root = canvasObject.GetComponent<RectTransform>();
            BuildBackground(root);
            BuildMainContent(root);
            BuildMissionBrief(root);
            BuildSettings(root);
            BuildLoadingOverlay(root);
        }

        private static void BuildBackground(RectTransform root)
        {
            RectTransform background = DustlineUiTheme.CreateRect(
                "Background",
                root,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
            DustlineUiTheme.AddImage(background, null, new Color(0.035f, 0.038f, 0.039f, 1f), false);

            RectTransform field = DustlineUiTheme.CreateRect(
                "Tactical_Field",
                background,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
            DustlineUiTheme.AddImage(field, null, new Color(0.065f, 0.06f, 0.052f, 1f), false);

            for (int i = 0; i < 11; i++)
            {
                float normalized = i / 10f;
                RectTransform line = DustlineUiTheme.CreateRect(
                    $"Grid_H_{i:00}",
                    field,
                    new Vector2(0f, normalized),
                    new Vector2(1f, normalized),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0f, 1f),
                    Vector2.zero);
                DustlineUiTheme.AddImage(line, null, new Color(0.62f, 0.52f, 0.36f, 0.08f), false);
            }

            for (int i = 0; i < 13; i++)
            {
                float normalized = i / 12f;
                RectTransform line = DustlineUiTheme.CreateRect(
                    $"Grid_V_{i:00}",
                    field,
                    new Vector2(normalized, 0f),
                    new Vector2(normalized, 1f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(1f, 0f),
                    Vector2.zero);
                DustlineUiTheme.AddImage(line, null, new Color(0.62f, 0.52f, 0.36f, 0.07f), false);
            }

            RectTransform topAccent = DustlineUiTheme.CreateRect(
                "Top_Accent",
                background,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(520f, 4f),
                new Vector2(0f, -42f));
            DustlineUiTheme.AddImage(topAccent, "AccentGold", DustlineUiTheme.Gold, true);
        }

        private void BuildMainContent(RectTransform root)
        {
            mainContent = DustlineUiTheme.CreateRect(
                "Main_Content",
                root,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
            mainGroup = mainContent.gameObject.AddComponent<CanvasGroup>();

            RectTransform terminalRect = DustlineUiTheme.CreateRect(
                "Terminal_Status",
                mainContent,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(640f, 30f),
                new Vector2(0f, 338f));
            DustlineUiTheme.AddText(
                terminalRect,
                "FIELD TERMINAL  //  ONLINE",
                18f,
                DustlineUiTheme.Muted,
                TextAlignmentOptions.Center,
                FontStyles.Bold);

            RectTransform titleRect = DustlineUiTheme.CreateRect(
                "Title",
                mainContent,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(760f, 190f),
                new Vector2(0f, 224f));
            TMP_Text title = DustlineUiTheme.AddText(
                titleRect,
                "DUSTLINE\n<color=#F2F2ED>ARENA</color>",
                78f,
                DustlineUiTheme.Gold,
                TextAlignmentOptions.Center,
                FontStyles.Bold);
            title.lineSpacing = -12f;

            RectTransform subtitleRect = DustlineUiTheme.CreateRect(
                "Subtitle",
                mainContent,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(640f, 32f),
                new Vector2(0f, 76f));
            DustlineUiTheme.AddText(
                subtitleRect,
                "TOP-DOWN SURVIVAL",
                22f,
                DustlineUiTheme.Text,
                TextAlignmentOptions.Center,
                FontStyles.Bold);

            RectTransform titleAccent = DustlineUiTheme.CreateRect(
                "Title_Accent",
                mainContent,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(96f, 3f),
                new Vector2(0f, 38f));
            DustlineUiTheme.AddImage(titleAccent, null, DustlineUiTheme.Gold, false);

            RectTransform buttonStack = DustlineUiTheme.CreateRect(
                "Button_Stack",
                mainContent,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(560f, 244f),
                new Vector2(0f, -128f));
            playButton = CreateMenuButton(buttonStack, "PLAY", Vector2.zero, OpenMissionBrief, 560f);
            settingsButton = CreateMenuButton(buttonStack, "SETTINGS", new Vector2(0f, -88f), OpenSettings, 560f);
            quitButton = CreateMenuButton(buttonStack, "QUIT", new Vector2(0f, -176f), QuitGame, 560f);

            RectTransform buildRect = DustlineUiTheme.CreateRect(
                "Build_Label",
                mainContent,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(640f, 28f),
                new Vector2(0f, 46f));
            DustlineUiTheme.AddText(
                buildRect,
                "DUSTLINE OPERATIONS NETWORK",
                15f,
                new Color(0.48f, 0.51f, 0.52f, 1f),
                TextAlignmentOptions.Center,
                FontStyles.Bold);
        }

        private void BuildMissionBrief(RectTransform root)
        {
            RectTransform overlay = DustlineUiTheme.CreateRect(
                "Mission_Brief_Screen",
                root,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
            DustlineUiTheme.AddImage(overlay, null, new Color(0.012f, 0.015f, 0.019f, 1f), false).raycastTarget = true;
            missionGroup = overlay.gameObject.AddComponent<CanvasGroup>();

            RectTransform panel = DustlineUiTheme.CreateRect(
                "Mission_Brief",
                overlay,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(860f, 880f),
                Vector2.zero);
            missionPanel = panel;
            DustlineUiTheme.AddImage(panel, "PanelDark", new Color(0.035f, 0.04f, 0.045f, 1f), true);

            RectTransform accent = DustlineUiTheme.CreateRect(
                "Accent",
                panel,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(-28f, 5f),
                new Vector2(0f, -14f));
            DustlineUiTheme.AddImage(accent, "AccentGold", DustlineUiTheme.Gold, true);

            AddPanelText(panel, "Header", "MISSION BRIEF", 30f, DustlineUiTheme.Gold, new Vector2(54f, -52f));
            AddPanelText(panel, "Sector", "SECTOR 01  /  FREIGHT YARD", 20f, DustlineUiTheme.Text, new Vector2(54f, -104f));
            AddPanelText(panel, "Threat", "THREAT LEVEL", 15f, DustlineUiTheme.Muted, new Vector2(54f, -154f));
            AddPanelText(panel, "ThreatValue", "ELEVATED", 24f, new Color(0.88f, 0.24f, 0.16f, 1f), new Vector2(54f, -182f));

            RectTransform radar = DustlineUiTheme.CreateRect(
                "Radar",
                panel,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(370f, 370f),
                new Vector2(0f, -24f));
            DustlineUiTheme.AddImage(radar, "PanelLight", new Color(0.09f, 0.10f, 0.10f, 0.86f), true);

            RectTransform horizontal = DustlineUiTheme.CreateRect(
                "Crosshair_H",
                radar,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(260f, 2f),
                Vector2.zero);
            DustlineUiTheme.AddImage(horizontal, null, new Color(1f, 0.72f, 0.16f, 0.48f), false);

            RectTransform vertical = DustlineUiTheme.CreateRect(
                "Crosshair_V",
                radar,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(2f, 260f),
                Vector2.zero);
            DustlineUiTheme.AddImage(vertical, null, new Color(1f, 0.72f, 0.16f, 0.48f), false);

            for (int i = 0; i < 5; i++)
            {
                float angle = i * 1.31f;
                Vector2 position = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (54f + i * 18f);
                RectTransform contact = DustlineUiTheme.CreateRect(
                    $"Contact_{i:00}",
                    radar,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(12f, 12f),
                    position);
                DustlineUiTheme.AddImage(
                    contact,
                    null,
                    i == 0 ? DustlineUiTheme.Gold : new Color(0.80f, 0.18f, 0.12f, 1f),
                    false);
            }

            AddPanelText(panel, "Intel", "5 ARENAS   //   4 WAVES EACH", 17f, DustlineUiTheme.Text, new Vector2(54f, -650f));
            AddPanelText(panel, "Directive", "DIRECTIVE: CLEAR THE DUSTLINE", 15f, DustlineUiTheme.Muted, new Vector2(54f, -684f));

            missionBackButton = CreateMenuButton(
                panel,
                "BACK",
                new Vector2(54f, -758f),
                CloseMissionBrief,
                350f,
                false);
            deployButton = CreateMenuButton(
                panel,
                "DEPLOY",
                new Vector2(456f, -758f),
                StartGame,
                350f);

            missionGroup.alpha = 0f;
            missionGroup.blocksRaycasts = false;
            missionGroup.interactable = false;
            overlay.gameObject.SetActive(false);
        }

        private void BuildSettings(RectTransform root)
        {
            RectTransform overlay = DustlineUiTheme.CreateRect(
                "Settings_Overlay",
                root,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
            DustlineUiTheme.AddImage(overlay, null, new Color(0.008f, 0.01f, 0.014f, 1f), false).raycastTarget = true;
            settingsGroup = overlay.gameObject.AddComponent<CanvasGroup>();

            settingsPanel = DustlineUiTheme.CreateRect(
                "Settings_Panel",
                overlay,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(760f, 820f),
                Vector2.zero);
            DustlineUiTheme.AddImage(settingsPanel, "PanelDark", DustlineUiTheme.Background, true);

            RectTransform accent = DustlineUiTheme.CreateRect(
                "Accent",
                settingsPanel,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(-32f, 5f),
                new Vector2(0f, -16f));
            DustlineUiTheme.AddImage(accent, "AccentGold", DustlineUiTheme.Gold, true);

            RectTransform titleRect = DustlineUiTheme.CreateRect(
                "Title",
                settingsPanel,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(-80f, 54f),
                new Vector2(0f, -62f));
            DustlineUiTheme.AddText(
                titleRect,
                "AUDIO SETTINGS",
                34f,
                DustlineUiTheme.Gold,
                TextAlignmentOptions.Left,
                FontStyles.Bold);

            GameAudio audio = GameAudio.Instance;
            CreateVolumeSlider(settingsPanel, "MASTER", audio.MasterVolume, new Vector2(0f, -184f), value => audio.MasterVolume = value);
            CreateVolumeSlider(settingsPanel, "MUSIC", audio.MusicVolume, new Vector2(0f, -294f), value => audio.MusicVolume = value);
            CreateVolumeSlider(settingsPanel, "SFX", audio.SfxVolume, new Vector2(0f, -404f), value => audio.SfxVolume = value);
            CreateVolumeSlider(settingsPanel, "UI", audio.UiVolume, new Vector2(0f, -514f), value => audio.UiVolume = value);

            backButton = CreateMenuButton(settingsPanel, "BACK", new Vector2(80f, -672f), CloseSettings, 600f, false);
            settingsGroup.alpha = 0f;
            settingsGroup.blocksRaycasts = false;
            settingsGroup.interactable = false;
            overlay.gameObject.SetActive(false);
        }

        private void BuildLoadingOverlay(RectTransform root)
        {
            RectTransform overlay = DustlineUiTheme.CreateRect(
                "Loading_Overlay",
                root,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
            DustlineUiTheme.AddImage(overlay, null, new Color(0.015f, 0.018f, 0.022f, 1f), false);
            loadingGroup = overlay.gameObject.AddComponent<CanvasGroup>();
            loadingGroup.alpha = 0f;
            loadingGroup.blocksRaycasts = false;
            loadingGroup.interactable = false;

            RectTransform labelRect = DustlineUiTheme.CreateRect(
                "Label",
                overlay,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(720f, 80f),
                Vector2.zero);
            DustlineUiTheme.AddText(
                labelRect,
                "DEPLOYING TO SECTOR 01",
                30f,
                DustlineUiTheme.Gold,
                TextAlignmentOptions.Center,
                FontStyles.Bold);
        }

        private Button CreateMenuButton(
            Transform parent,
            string label,
            Vector2 position,
            Action action,
            float width = 520f,
            bool playConfirmSound = true)
        {
            RectTransform rect = DustlineUiTheme.CreateRect(
                $"{label}_Button",
                parent,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(width, 68f),
                position);
            Image background = DustlineUiTheme.AddImage(
                rect,
                "PanelLight",
                new Color(0.075f, 0.085f, 0.095f, 0.94f),
                true);
            background.raycastTarget = true;

            Button button = rect.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = background;
            button.onClick.AddListener(() =>
            {
                if (playConfirmSound)
                {
                    GameAudio.PlayUiConfirm();
                }

                action?.Invoke();
            });

            RectTransform accentRect = DustlineUiTheme.CreateRect(
                "Accent",
                rect,
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(0f, 0.5f),
                new Vector2(5f, -14f),
                new Vector2(14f, 0f));
            Image accent = DustlineUiTheme.AddImage(accentRect, null, DustlineUiTheme.Gold, false);

            RectTransform labelRect = DustlineUiTheme.CreateRect(
                "Label",
                rect,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                new Vector2(-84f, 0f),
                new Vector2(24f, 0f));
            TMP_Text text = DustlineUiTheme.AddText(
                labelRect,
                label,
                23f,
                DustlineUiTheme.Text,
                TextAlignmentOptions.Left,
                FontStyles.Bold);

            MenuButtonFeedback feedback = rect.gameObject.AddComponent<MenuButtonFeedback>();
            feedback.Initialize(
                background,
                accent,
                text,
                background.color,
                new Color(0.13f, 0.14f, 0.15f, 1f));
            return button;
        }

        private static void CreateVolumeSlider(
            Transform parent,
            string label,
            float initialValue,
            Vector2 position,
            Action<float> onChanged)
        {
            RectTransform row = DustlineUiTheme.CreateRect(
                $"{label}_Row",
                parent,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(-96f, 72f),
                position);

            RectTransform labelRect = DustlineUiTheme.CreateRect(
                "Label",
                row,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(170f, 32f),
                new Vector2(0f, 14f));
            DustlineUiTheme.AddText(
                labelRect,
                label,
                18f,
                DustlineUiTheme.Text,
                TextAlignmentOptions.Left,
                FontStyles.Bold);

            RectTransform valueRect = DustlineUiTheme.CreateRect(
                "Value",
                row,
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(80f, 32f),
                new Vector2(0f, 14f));
            TMP_Text valueText = DustlineUiTheme.AddText(
                valueRect,
                $"{Mathf.RoundToInt(initialValue * 100f):000}",
                18f,
                DustlineUiTheme.Gold,
                TextAlignmentOptions.Right,
                FontStyles.Bold);

            RectTransform sliderRect = DustlineUiTheme.CreateRect(
                "Slider",
                row,
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 26f),
                new Vector2(0f, -16f));

            RectTransform trackRect = DustlineUiTheme.CreateRect(
                "Track",
                sliderRect,
                new Vector2(0f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-16f, 10f),
                Vector2.zero);
            DustlineUiTheme.AddImage(trackRect, null, new Color(0.015f, 0.018f, 0.02f, 1f), false);

            RectTransform fillArea = DustlineUiTheme.CreateRect(
                "Fill_Area",
                sliderRect,
                new Vector2(0f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-26f, 10f),
                Vector2.zero);
            RectTransform fillRect = DustlineUiTheme.CreateRect(
                "Fill",
                fillArea,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
            DustlineUiTheme.AddImage(fillRect, null, DustlineUiTheme.Gold, false);

            RectTransform handleArea = DustlineUiTheme.CreateRect(
                "Handle_Area",
                sliderRect,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                new Vector2(-26f, 0f),
                Vector2.zero);
            RectTransform handleRect = DustlineUiTheme.CreateRect(
                "Handle",
                handleArea,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(24f, 24f),
                Vector2.zero);
            Image handle = DustlineUiTheme.AddImage(handleRect, null, DustlineUiTheme.Text, false);
            handle.raycastTarget = true;

            Slider slider = sliderRect.gameObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.SetValueWithoutNotify(initialValue);
            slider.onValueChanged.AddListener(value =>
            {
                valueText.text = $"{Mathf.RoundToInt(value * 100f):000}";
                onChanged?.Invoke(value);
            });
        }

        private static void AddPanelText(
            Transform parent,
            string name,
            string value,
            float fontSize,
            Color color,
            Vector2 position)
        {
            RectTransform rect = DustlineUiTheme.CreateRect(
                name,
                parent,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(-84f, 32f),
                position);
            DustlineUiTheme.AddText(rect, value, fontSize, color, TextAlignmentOptions.Left, FontStyles.Bold);
        }

        private void OpenSettings()
        {
            if (settingsOpen || loading)
            {
                return;
            }

            settingsOpen = true;
            SetMainScreenVisible(false);
            settingsGroup.gameObject.SetActive(true);
            settingsGroup.blocksRaycasts = true;
            settingsGroup.interactable = true;
            settingsGroup.alpha = 1f;
            settingsPanel.localScale = Vector3.one * 0.96f;

            DOTween.Kill(settingsGroup);
            settingsPanel
                .DOScale(Vector3.one, 0.2f)
                .SetId(settingsGroup)
                .SetUpdate(true)
                .SetEase(Ease.OutCubic);

            EventSystem.current?.SetSelectedGameObject(backButton.gameObject);
        }

        private void CloseSettings()
        {
            if (!settingsOpen)
            {
                return;
            }

            GameAudio.PlayUiBack();
            settingsOpen = false;
            settingsGroup.blocksRaycasts = false;
            settingsGroup.interactable = false;
            DOTween.Kill(settingsGroup);
            settingsGroup.gameObject.SetActive(false);
            ShowMainScreen(settingsButton);
        }

        private void OpenMissionBrief()
        {
            if (missionOpen || loading)
            {
                return;
            }

            missionOpen = true;
            SetMainScreenVisible(false);
            missionGroup.gameObject.SetActive(true);
            missionGroup.alpha = 1f;
            missionGroup.blocksRaycasts = true;
            missionGroup.interactable = true;
            missionPanel.localScale = Vector3.one * 0.96f;

            DOTween.Kill(missionGroup);
            missionPanel
                .DOScale(Vector3.one, 0.2f)
                .SetId(missionGroup)
                .SetUpdate(true)
                .SetEase(Ease.OutCubic);

            EventSystem.current?.SetSelectedGameObject(deployButton.gameObject);
        }

        private void CloseMissionBrief()
        {
            if (!missionOpen)
            {
                return;
            }

            GameAudio.PlayUiBack();
            missionOpen = false;
            missionGroup.blocksRaycasts = false;
            missionGroup.interactable = false;
            DOTween.Kill(missionGroup);
            missionGroup.gameObject.SetActive(false);
            ShowMainScreen(playButton);
        }

        private void ShowMainScreen(Button selectedButton)
        {
            SetMainScreenVisible(true);
            mainGroup.alpha = 0f;
            DOTween.Kill(mainGroup);
            TweenCanvasAlpha(mainGroup, 1f, 0.16f)
                .SetId(mainGroup)
                .SetUpdate(true);
            EventSystem.current?.SetSelectedGameObject(selectedButton.gameObject);
        }

        private void SetMainScreenVisible(bool visible)
        {
            DOTween.Kill(mainGroup);
            mainGroup.alpha = visible ? 1f : 0f;
            mainGroup.blocksRaycasts = visible;
            mainGroup.interactable = visible;
        }

        private void StartGame()
        {
            if (loading)
            {
                return;
            }

            loading = true;
            SetMainButtonsInteractable(false);
            deployButton.interactable = false;
            missionBackButton.interactable = false;
            loadingGroup.blocksRaycasts = true;
            loadingGroup.alpha = 0f;
            DOTween.Kill(loadingGroup);
            TweenCanvasAlpha(loadingGroup, 1f, 0.26f)
                .SetUpdate(true)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => SceneManager.LoadScene(FirstArenaScene));
        }

        private void QuitGame()
        {
            if (loading)
            {
                return;
            }

            SetMainButtonsInteractable(false);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void SetMainButtonsInteractable(bool value)
        {
            playButton.interactable = value;
            settingsButton.interactable = value;
            quitButton.interactable = value;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(StandaloneInputModule));
        }

        private static Tween TweenCanvasAlpha(CanvasGroup group, float target, float duration)
        {
            return DOTween.To(
                () => group.alpha,
                value => group.alpha = value,
                target,
                duration);
        }

        private static Tween TweenAnchoredPosition(RectTransform rect, Vector2 target, float duration)
        {
            return DOTween.To(
                () => rect.anchoredPosition,
                value => rect.anchoredPosition = value,
                target,
                duration);
        }

        private void OnDestroy()
        {
            DOTween.Kill(mainGroup);
            DOTween.Kill(settingsGroup);
            DOTween.Kill(missionGroup);
            DOTween.Kill(loadingGroup);
        }
    }
}
