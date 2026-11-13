using System.Collections;
using DustlineArena.Runtime.Common;
using DustlineArena.Runtime.Audio;
using DustlineArena.Runtime.Health;
using DustlineArena.Runtime.Progression;
using DustlineArena.Runtime.Spawning;
using DustlineArena.Runtime.UI;
using DustlineArena.Runtime.Weapons;
using DustlineArena.Runtime.Camera;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DustlineArena.Runtime.Player
{
    [DisallowMultipleComponent]
    public sealed class CombatHud : MonoBehaviour
    {
        private const string MainMenuSceneName = "Dustline_MainMenu";
        private const float LevelCompleteOverlayDelay = 5f;

        [SerializeField] private HealthComponent health;
        [SerializeField] private ProjectileWeapon weapon;
        [SerializeField] private PlayerGrenadeController grenades;
        [SerializeField] private PlayerMotor playerMotor;
        [SerializeField] private WaveSpawner waveSpawner;
        [SerializeField] private SceneWaveTransition sceneTransition;
        [SerializeField] private MouseAimController aimController;
        [SerializeField] private UnityEngine.Camera targetCamera;
        [SerializeField] private TopDownCameraFollow cameraFollow;
        [SerializeField] private bool hideSystemCursor = true;
        [SerializeField, Range(0.65f, 1f)] private float weaponHudScale = 0.86f;
        [SerializeField, Min(5f)] private float crosshairBaseGap = 8f;
        [SerializeField, Min(0f)] private float crosshairBusyGap = 16f;
        [SerializeField, Min(0f)] private float crosshairFireBloomMax = 14f;
        [SerializeField, Min(0f)] private float crosshairFireBloomDecay = 42f;

        private Image healthFill;
        private Image reloadFill;
        private Image grenadeChargeFill;
        private Image dodgeCooldownFill;
        private Image crosshairDot;
        private Image damageFlash;
        private TMP_Text healthValue;
        private TMP_Text weaponName;
        private TMP_Text magazineValue;
        private TMP_Text reserveValue;
        private TMP_Text reloadValue;
        private TMP_Text grenadeValue;
        private TMP_Text grenadeLabel;
        private TMP_Text dodgeValue;
        private TMP_Text dodgeLabel;
        private TMP_Text waveValue;
        private TMP_Text enemyValue;
        private TMP_Text missionValue;
        private TMP_Text previousArenaValue;
        private TMP_Text healthDeltaValue;
        private TMP_Text bannerTitle;
        private TMP_Text bannerSubtitle;
        private TMP_Text clearTitle;
        private TMP_Text clearSubtitle;
        private TMP_Text clearStats;
        private TMP_Text clearHint;
        private TMP_Text pauseTitle;
        private TMP_Text pauseSubtitle;
        private TMP_Text pauseHint;
        private TMP_Text deathTitle;
        private TMP_Text deathSubtitle;
        private TMP_Text deathStats;
        private RectTransform reloadGroup;
        private RectTransform dodgeRoot;
        private RectTransform healthDeltaRect;
        private RectTransform iconStock;
        private RectTransform iconReceiver;
        private RectTransform iconBarrel;
        private RectTransform iconGrip;
        private RectTransform iconMagazine;
        private RectTransform crosshairRoot;
        private RectTransform crosshairTop;
        private RectTransform crosshairBottom;
        private RectTransform crosshairLeft;
        private RectTransform crosshairRight;
        private RectTransform hitMarkerRoot;
        private RectTransform bannerRoot;
        private RectTransform pauseRoot;
        private RectTransform pauseMainContent;
        private RectTransform pauseSettingsContent;
        private RectTransform clearRoot;
        private RectTransform deathRoot;
        private CanvasGroup crosshairGroup;
        private CanvasGroup hitMarkerGroup;
        private CanvasGroup healthDeltaGroup;
        private CanvasGroup bannerGroup;
        private CanvasGroup pauseGroup;
        private CanvasGroup clearGroup;
        private CanvasGroup deathGroup;
        private GraphicRaycaster hudRaycaster;
        private Button clearNextButton;
        private Button clearMenuButton;
        private Button pauseResumeButton;
        private Button pauseSettingsButton;
        private Button pauseSettingsBackButton;
        private Button deathRestartButton;
        private Button deathMenuButton;
        private Slider pauseMasterSlider;
        private GameObject hudRoot;
        private float nextWorldRefresh;
        private float lastHealthNormalized = -1f;
        private int lastMagazine = int.MinValue;
        private int lastReserve = int.MinValue;
        private int lastGrenadeCount = int.MinValue;
        private int lastActiveEnemies = int.MinValue;
        private int lastWaveIndex = int.MinValue;
        private bool lastDodgeReady = true;
        private float crosshairFireBloom;
        private bool isGameOver;
        private bool isLevelComplete;
        private bool isLevelCompletePending;
        private bool isPaused;
        private bool pauseSettingsOpen;
        private float prePauseTimeScale = 1f;
        private float arenaStartTime;
        private float arenaElapsedTime;
        private float arenaDamageTaken;
        private int arenaKills;
        private int arenaIndex;
        private string levelCompleteNextScene;
        private Coroutine levelCompleteRoutine;
        private bool cursorWasVisible;

        private void Awake()
        {
            SanitizeCrosshairSettings();
            health = health == null ? GetComponent<HealthComponent>() : health;
            weapon = weapon == null ? GetComponentInChildren<ProjectileWeapon>() : weapon;
            grenades = grenades == null ? GetComponent<PlayerGrenadeController>() : grenades;
            playerMotor = playerMotor == null ? GetComponent<PlayerMotor>() : playerMotor;
            waveSpawner = waveSpawner == null ? FindObjectOfType<WaveSpawner>() : waveSpawner;
            sceneTransition = sceneTransition == null && waveSpawner != null ? waveSpawner.GetComponent<SceneWaveTransition>() : sceneTransition;
            sceneTransition = sceneTransition == null ? FindObjectOfType<SceneWaveTransition>() : sceneTransition;
            aimController = aimController == null ? GetComponent<MouseAimController>() : aimController;
            targetCamera = targetCamera == null ? UnityEngine.Camera.main : targetCamera;
            cameraFollow = cameraFollow == null && targetCamera != null ? targetCamera.GetComponent<TopDownCameraFollow>() : cameraFollow;
            cameraFollow = cameraFollow == null ? FindObjectOfType<TopDownCameraFollow>() : cameraFollow;
            arenaIndex = OperationProgress.GetArenaIndex(SceneManager.GetActiveScene().name);
            OperationProgress.EnsureOperation();
            arenaStartTime = Time.time;
            BuildHud();
        }

        private void OnEnable()
        {
            cursorWasVisible = Cursor.visible;
            if (hideSystemCursor)
            {
                Cursor.visible = false;
            }

            if (health != null)
            {
                health.Changed += OnHealthChanged;
                health.Died += OnPlayerDied;
            }

            HealthComponent.AnyDamaged += OnAnyDamaged;

            if (weapon != null)
            {
                weapon.AmmoChanged += RefreshWeapon;
                weapon.ReloadStarted += OnReloadStarted;
                weapon.ReloadCompleted += OnReloadCompleted;
                weapon.Fired += OnWeaponFired;
            }

            if (grenades != null)
            {
                grenades.GrenadeCountChanged += RefreshGrenades;
            }

            if (waveSpawner != null)
            {
                waveSpawner.WaveStarted += OnWaveStarted;
                waveSpawner.WaveCompleted += OnWaveCompleted;
                waveSpawner.EnemyKilled += OnEnemyKilled;
                waveSpawner.AllWavesCompleted += OnAllWavesCompleted;
            }

            RefreshAll();
        }

        private void Start()
        {
            // Other player components may initialize after this HUD's OnEnable.
            RefreshAll();
        }

        private void OnDisable()
        {
            CancelPendingLevelComplete();

            if (isPaused || isLevelComplete || isGameOver)
            {
                isPaused = false;
                isLevelComplete = false;
                isGameOver = false;
                Time.timeScale = prePauseTimeScale <= 0f ? 1f : prePauseTimeScale;
                GameAudio.SetGameplayPaused(false);
            }

            Cursor.visible = cursorWasVisible;
            KillHudTweens();

            if (health != null)
            {
                health.Changed -= OnHealthChanged;
                health.Died -= OnPlayerDied;
            }

            HealthComponent.AnyDamaged -= OnAnyDamaged;

            if (weapon != null)
            {
                weapon.AmmoChanged -= RefreshWeapon;
                weapon.ReloadStarted -= OnReloadStarted;
                weapon.ReloadCompleted -= OnReloadCompleted;
                weapon.Fired -= OnWeaponFired;
            }

            if (grenades != null)
            {
                grenades.GrenadeCountChanged -= RefreshGrenades;
            }

            if (waveSpawner != null)
            {
                waveSpawner.WaveStarted -= OnWaveStarted;
                waveSpawner.WaveCompleted -= OnWaveCompleted;
                waveSpawner.EnemyKilled -= OnEnemyKilled;
                waveSpawner.AllWavesCompleted -= OnAllWavesCompleted;
            }
        }

        private void OnDestroy()
        {
            KillHudTweens();
            if (hudRoot != null)
            {
                Destroy(hudRoot);
            }
        }

        private void Update()
        {
            if (isGameOver)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
                {
                    RestartCurrentScene();
                }
                else if (Input.GetKeyDown(KeyCode.Escape))
                {
                    LoadMainMenu();
                }

                return;
            }

            if (isLevelComplete)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
                {
                    ContinueAfterArena();
                }
                else if (Input.GetKeyDown(KeyCode.Escape))
                {
                    ReturnToMainMenu();
                }

                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (isPaused && pauseSettingsOpen)
                {
                    ShowPauseMainMenu(true);
                    return;
                }

                SetPaused(!isPaused);
                return;
            }

            if (isPaused)
            {
                if (!pauseSettingsOpen && Input.GetKeyDown(KeyCode.R))
                {
                    RestartCurrentScene();
                }

                return;
            }

            RefreshReload();
            RefreshDodgeCooldown();

            if (grenadeChargeFill != null)
            {
                RefreshGrenadeCharge();
            }

            if (Time.unscaledTime >= nextWorldRefresh)
            {
                nextWorldRefresh = Time.unscaledTime + 0.15f;
                RefreshWave();
            }

            RefreshCrosshair();
        }

        private void BuildHud()
        {
            GameObject canvasObject = new GameObject(
                "Combat_HUD",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            hudRoot = canvasObject;

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvas.pixelPerfect = true;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            hudRaycaster = canvasObject.GetComponent<GraphicRaycaster>();
            hudRaycaster.enabled = false;

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            RectTransform safeArea = DustlineUiTheme.CreateRect(
                "Safe_Area", canvasRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            BuildVitals(safeArea);
            BuildWeapon(safeArea);
            BuildDodge(safeArea);
            BuildWave(safeArea);
            BuildCrosshair(safeArea);
            BuildDamageFlash(safeArea);
            BuildGameStateOverlay(safeArea);
        }

        private void BuildVitals(RectTransform parent)
        {
            RectTransform panel = DustlineUiTheme.CreateRect(
                "Vitals", parent, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(420f, 132f), new Vector2(28f, 28f));
            DustlineUiTheme.AddImage(panel, "PanelDark", DustlineUiTheme.Background, true);
            AddAccent(panel);

            RectTransform titleRect = DustlineUiTheme.CreateRect(
                "Title", panel, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, 1f), new Vector2(-44f, 28f), new Vector2(24f, -18f));
            DustlineUiTheme.AddText(titleRect, "VITALS", 18f, DustlineUiTheme.Gold, TextAlignmentOptions.Left, FontStyles.Bold);

            RectTransform valueRect = DustlineUiTheme.CreateRect(
                "Value", panel, new Vector2(1f, 1f), Vector2.one, Vector2.one, new Vector2(170f, 34f), new Vector2(-22f, -16f));
            healthValue = DustlineUiTheme.AddText(valueRect, "100 / 100", 24f, DustlineUiTheme.Text, TextAlignmentOptions.Right, FontStyles.Bold);

            healthDeltaRect = DustlineUiTheme.CreateRect(
                "Health_Delta", panel, new Vector2(1f, 1f), Vector2.one, Vector2.one, new Vector2(112f, 24f), new Vector2(-24f, -49f));
            healthDeltaValue = DustlineUiTheme.AddText(healthDeltaRect, "+0", 18f, new Color(0.46f, 1f, 0.55f, 1f), TextAlignmentOptions.Right, FontStyles.Bold);
            healthDeltaGroup = healthDeltaRect.gameObject.AddComponent<CanvasGroup>();
            healthDeltaGroup.alpha = 0f;
            healthDeltaGroup.blocksRaycasts = false;
            healthDeltaGroup.interactable = false;

            RectTransform barBackground = DustlineUiTheme.CreateRect(
                "Health_Background", panel, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(-44f, 34f), new Vector2(0f, 22f));
            DustlineUiTheme.AddImage(barBackground, "PanelDark", new Color(0.015f, 0.018f, 0.022f, 1f), true);

            RectTransform fillRect = DustlineUiTheme.CreateRect(
                "Health_Fill", barBackground, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-8f, -8f), Vector2.zero);
            healthFill = DustlineUiTheme.AddImage(fillRect, "HealthFill", Color.white, false);
            healthFill.type = Image.Type.Filled;
            healthFill.fillMethod = Image.FillMethod.Horizontal;
            healthFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        }

        private void BuildWeapon(RectTransform parent)
        {
            RectTransform panel = DustlineUiTheme.CreateRect(
                "Weapon", parent, Vector2.one, Vector2.one, Vector2.one, new Vector2(570f, 190f), new Vector2(-28f, -28f));
            panel.localScale = Vector3.one * weaponHudScale;
            DustlineUiTheme.AddImage(panel, "PanelDark", DustlineUiTheme.Background, true);
            AddAccent(panel);

            RectTransform nameRect = DustlineUiTheme.CreateRect(
                "Weapon_Name", panel, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, 1f), new Vector2(-285f, 34f), new Vector2(24f, -17f));
            weaponName = DustlineUiTheme.AddText(nameRect, "NO WEAPON", 23f, DustlineUiTheme.Gold, TextAlignmentOptions.Left, FontStyles.Bold);

            RectTransform iconFrame = DustlineUiTheme.CreateRect(
                "Weapon_Icon", panel, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(270f, 82f), new Vector2(18f, 62f));
            DustlineUiTheme.AddImage(iconFrame, "PanelDark", new Color(0.015f, 0.018f, 0.022f, 0.72f), true);
            iconStock = CreateIconPart("Stock", iconFrame);
            iconReceiver = CreateIconPart("Receiver", iconFrame);
            iconBarrel = CreateIconPart("Barrel", iconFrame);
            iconGrip = CreateIconPart("Grip", iconFrame);
            iconMagazine = CreateIconPart("Magazine", iconFrame);

            RectTransform magazineLabelRect = DustlineUiTheme.CreateRect(
                "Magazine_Label", panel, new Vector2(1f, 1f), Vector2.one, Vector2.one, new Vector2(112f, 20f), new Vector2(-150f, -18f));
            DustlineUiTheme.AddText(magazineLabelRect, "MAGAZINE", 12f, DustlineUiTheme.Muted, TextAlignmentOptions.Center, FontStyles.Bold);

            RectTransform ammoRect = DustlineUiTheme.CreateRect(
                "Magazine", panel, Vector2.one, Vector2.one, Vector2.one, new Vector2(112f, 62f), new Vector2(-150f, -38f));
            magazineValue = DustlineUiTheme.AddText(ammoRect, "--", 48f, DustlineUiTheme.Text, TextAlignmentOptions.Center, FontStyles.Bold);

            RectTransform divider = DustlineUiTheme.CreateRect(
                "Ammo_Divider", panel, Vector2.one, Vector2.one, Vector2.one, new Vector2(3f, 70f), new Vector2(-136f, -34f));
            DustlineUiTheme.AddImage(divider, null, new Color(DustlineUiTheme.Gold.r, DustlineUiTheme.Gold.g, DustlineUiTheme.Gold.b, 0.65f), false);

            RectTransform reserveLabelRect = DustlineUiTheme.CreateRect(
                "Reserve_Label", panel, Vector2.one, Vector2.one, Vector2.one, new Vector2(112f, 20f), new Vector2(-20f, -18f));
            DustlineUiTheme.AddText(reserveLabelRect, "RESERVE", 12f, DustlineUiTheme.Muted, TextAlignmentOptions.Center, FontStyles.Bold);

            RectTransform reserveRect = DustlineUiTheme.CreateRect(
                "Reserve", panel, Vector2.one, Vector2.one, Vector2.one, new Vector2(112f, 54f), new Vector2(-20f, -41f));
            reserveValue = DustlineUiTheme.AddText(reserveRect, "---", 30f, DustlineUiTheme.Text, TextAlignmentOptions.Center, FontStyles.Bold);

            reloadGroup = DustlineUiTheme.CreateRect(
                "Reload", panel, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(300f, 40f), new Vector2(20f, 16f));
            RectTransform reloadBackground = DustlineUiTheme.CreateRect(
                "Background", reloadGroup, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), Vector2.zero);
            DustlineUiTheme.AddImage(reloadBackground, "PanelDark", new Color(0.015f, 0.018f, 0.022f, 1f), true);
            RectTransform reloadFillRect = DustlineUiTheme.CreateRect(
                "Fill", reloadBackground, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-4f, -4f), Vector2.zero);
            reloadFill = DustlineUiTheme.AddImage(reloadFillRect, "AccentGold", DustlineUiTheme.Gold, false);
            reloadFill.type = Image.Type.Filled;
            reloadFill.fillMethod = Image.FillMethod.Horizontal;
            RectTransform reloadTextRect = DustlineUiTheme.CreateRect(
                "Label", reloadGroup, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, 1f), new Vector2(0f, 24f), Vector2.zero);
            reloadValue = DustlineUiTheme.AddText(reloadTextRect, "R  RELOAD", 14f, DustlineUiTheme.Muted, TextAlignmentOptions.Left, FontStyles.Bold);

            RectTransform grenadePanel = DustlineUiTheme.CreateRect(
                "Grenades", panel, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(218f, 40f), new Vector2(332f, 16f));
            DustlineUiTheme.AddImage(grenadePanel, "PanelDark", DustlineUiTheme.BackgroundSoft, true);
            RectTransform grenadeTextRect = DustlineUiTheme.CreateRect(
                "Count", grenadePanel, Vector2.zero, Vector2.one, new Vector2(1f, 0.5f), new Vector2(-76f, 0f), new Vector2(-8f, 0f));
            grenadeValue = DustlineUiTheme.AddText(grenadeTextRect, "0 / 0", 18f, DustlineUiTheme.Text, TextAlignmentOptions.Right, FontStyles.Bold);
            RectTransform grenadeLabelRect = DustlineUiTheme.CreateRect(
                "Label", grenadePanel, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), new Vector2(-116f, 0f), new Vector2(46f, 0f));
            grenadeLabel = DustlineUiTheme.AddText(grenadeLabelRect, "GRENADE  [G]", 12f, DustlineUiTheme.Muted, TextAlignmentOptions.Left, FontStyles.Bold);
            RectTransform chargeRingRect = DustlineUiTheme.CreateRect(
                "Charge_Ring", grenadePanel, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30f, 30f), new Vector2(8f, 0f));
            grenadeChargeFill = DustlineUiTheme.AddImage(chargeRingRect, "GrenadeRing", Color.white, false);
            grenadeChargeFill.color = new Color(DustlineUiTheme.Gold.r, DustlineUiTheme.Gold.g, DustlineUiTheme.Gold.b, 0.35f);
            grenadeChargeFill.type = Image.Type.Filled;
            grenadeChargeFill.fillMethod = Image.FillMethod.Radial360;
            grenadeChargeFill.fillOrigin = (int)Image.Origin360.Top;
            grenadeChargeFill.fillClockwise = true;
            grenadeChargeFill.fillAmount = 0f;
            grenadeChargeFill.raycastTarget = false;
            chargeRingRect.SetAsFirstSibling();
        }

        private void BuildDodge(RectTransform parent)
        {
            dodgeRoot = DustlineUiTheme.CreateRect(
                "Dodge", parent, Vector2.one, Vector2.one, Vector2.one, new Vector2(250f, 46f), new Vector2(-28f, -202f));
            dodgeRoot.localScale = Vector3.one * weaponHudScale;
            DustlineUiTheme.AddImage(dodgeRoot, "PanelDark", DustlineUiTheme.BackgroundSoft, true);
            AddAccent(dodgeRoot);

            RectTransform labelRect = DustlineUiTheme.CreateRect(
                "Label", dodgeRoot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(128f, 20f), new Vector2(18f, -9f));
            dodgeLabel = DustlineUiTheme.AddText(labelRect, "DODGE  [SPACE]", 12f, DustlineUiTheme.Muted, TextAlignmentOptions.Left, FontStyles.Bold);

            RectTransform valueRect = DustlineUiTheme.CreateRect(
                "Value", dodgeRoot, Vector2.one, Vector2.one, Vector2.one, new Vector2(84f, 20f), new Vector2(-16f, -9f));
            dodgeValue = DustlineUiTheme.AddText(valueRect, "READY", 16f, DustlineUiTheme.Gold, TextAlignmentOptions.Right, FontStyles.Bold);

            RectTransform barBackground = DustlineUiTheme.CreateRect(
                "Cooldown_Background", dodgeRoot, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(-34f, 8f), new Vector2(0f, 8f));
            DustlineUiTheme.AddImage(barBackground, "PanelDark", new Color(0.015f, 0.018f, 0.022f, 1f), true);

            RectTransform fillRect = DustlineUiTheme.CreateRect(
                "Cooldown_Fill", barBackground, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-4f, -4f), Vector2.zero);
            dodgeCooldownFill = DustlineUiTheme.AddImage(fillRect, "AccentGold", DustlineUiTheme.Gold, false);
            dodgeCooldownFill.type = Image.Type.Filled;
            dodgeCooldownFill.fillMethod = Image.FillMethod.Horizontal;
            dodgeCooldownFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            dodgeCooldownFill.fillAmount = 1f;
        }

        private static RectTransform CreateIconPart(string name, RectTransform parent)
        {
            RectTransform part = DustlineUiTheme.CreateRect(
                name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            DustlineUiTheme.AddImage(part, null, DustlineUiTheme.Gold, false);
            return part;
        }

        private static void SetIconPart(RectTransform part, Vector2 size, Vector2 position, float rotation, bool visible = true)
        {
            part.gameObject.SetActive(visible);
            part.sizeDelta = size;
            part.anchoredPosition = position;
            part.localRotation = Quaternion.Euler(0f, 0f, rotation);
        }

        private void ConfigureWeaponIcon(Config.WeaponVisualId visual)
        {
            switch (visual)
            {
                case Config.WeaponVisualId.Pistol:
                    SetIconPart(iconStock, Vector2.zero, Vector2.zero, 0f, false);
                    SetIconPart(iconReceiver, new Vector2(74f, 16f), new Vector2(4f, 11f), 0f);
                    SetIconPart(iconBarrel, new Vector2(45f, 8f), new Vector2(59f, 13f), 0f);
                    SetIconPart(iconGrip, new Vector2(20f, 42f), new Vector2(-7f, -17f), -12f);
                    SetIconPart(iconMagazine, Vector2.zero, Vector2.zero, 0f, false);
                    break;
                case Config.WeaponVisualId.SMG:
                    SetIconPart(iconStock, new Vector2(54f, 9f), new Vector2(-78f, 8f), -4f);
                    SetIconPart(iconReceiver, new Vector2(84f, 24f), new Vector2(-20f, 6f), 0f);
                    SetIconPart(iconBarrel, new Vector2(49f, 9f), new Vector2(47f, 9f), 0f);
                    SetIconPart(iconGrip, new Vector2(17f, 34f), new Vector2(-21f, -22f), -10f);
                    SetIconPart(iconMagazine, new Vector2(17f, 38f), new Vector2(18f, -22f), -5f);
                    break;
                case Config.WeaponVisualId.Shotgun:
                    SetIconPart(iconStock, new Vector2(62f, 22f), new Vector2(-80f, 2f), -5f);
                    SetIconPart(iconReceiver, new Vector2(52f, 18f), new Vector2(-27f, 7f), 0f);
                    SetIconPart(iconBarrel, new Vector2(118f, 8f), new Vector2(58f, 11f), 0f);
                    SetIconPart(iconGrip, new Vector2(18f, 32f), new Vector2(-26f, -20f), -13f);
                    SetIconPart(iconMagazine, Vector2.zero, Vector2.zero, 0f, false);
                    break;
                default:
                    SetIconPart(iconStock, new Vector2(58f, 17f), new Vector2(-82f, 4f), -6f);
                    SetIconPart(iconReceiver, new Vector2(82f, 20f), new Vector2(-24f, 7f), 0f);
                    SetIconPart(iconBarrel, new Vector2(90f, 7f), new Vector2(63f, 11f), 0f);
                    SetIconPart(iconGrip, new Vector2(17f, 34f), new Vector2(-25f, -21f), -14f);
                    SetIconPart(iconMagazine, new Vector2(19f, 38f), new Vector2(14f, -22f), -12f);
                    break;
            }
        }

        private void SetWeaponIconVisible(bool visible)
        {
            iconStock.gameObject.SetActive(visible);
            iconReceiver.gameObject.SetActive(visible);
            iconBarrel.gameObject.SetActive(visible);
            iconGrip.gameObject.SetActive(visible);
            iconMagazine.gameObject.SetActive(visible);
        }

        private void BuildWave(RectTransform parent)
        {
            RectTransform panel = DustlineUiTheme.CreateRect(
                "Mission_Progress", parent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(470f, 130f), new Vector2(0f, -24f));
            DustlineUiTheme.AddImage(panel, "PanelDark", DustlineUiTheme.Background, true);
            AddAccent(panel);

            RectTransform missionRect = DustlineUiTheme.CreateRect(
                "Mission_Value", panel, new Vector2(0f, 0.66f), Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-44f, -8f), Vector2.zero);
            missionValue = DustlineUiTheme.AddText(missionRect, "ARENA 01 / 05  //  FREIGHT YARD", 16f, DustlineUiTheme.Gold, TextAlignmentOptions.Center, FontStyles.Bold);

            RectTransform waveRect = DustlineUiTheme.CreateRect(
                "Wave_Value", panel, new Vector2(0f, 0.31f), new Vector2(0.54f, 0.66f), new Vector2(0f, 0.5f), new Vector2(-18f, -8f), new Vector2(24f, 0f));
            waveValue = DustlineUiTheme.AddText(waveRect, "WAVE 1 / 1", 21f, DustlineUiTheme.Text, TextAlignmentOptions.Left, FontStyles.Bold);

            RectTransform enemyRect = DustlineUiTheme.CreateRect(
                "Enemy_Value", panel, new Vector2(0.54f, 0.31f), new Vector2(1f, 0.66f), new Vector2(1f, 0.5f), new Vector2(-18f, -8f), new Vector2(-24f, 0f));
            enemyValue = DustlineUiTheme.AddText(enemyRect, "0 HOSTILES", 18f, DustlineUiTheme.Text, TextAlignmentOptions.Right, FontStyles.Bold);

            RectTransform previousRect = DustlineUiTheme.CreateRect(
                "Previous_Arena", panel, Vector2.zero, new Vector2(1f, 0.31f), new Vector2(0.5f, 0.5f), new Vector2(-44f, -6f), Vector2.zero);
            previousArenaValue = DustlineUiTheme.AddText(previousRect, "OPERATION START", 13f, DustlineUiTheme.Muted, TextAlignmentOptions.Center, FontStyles.Bold);
        }

        private void BuildCrosshair(RectTransform parent)
        {
            crosshairRoot = DustlineUiTheme.CreateRect(
                "Crosshair", parent, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(86f, 86f), Vector2.zero);
            crosshairGroup = crosshairRoot.gameObject.AddComponent<CanvasGroup>();
            crosshairGroup.blocksRaycasts = false;
            crosshairGroup.interactable = false;
            crosshairGroup.alpha = 0.95f;

            crosshairTop = CreateCrosshairLine("Top", crosshairRoot, new Vector2(2.5f, 7f));
            crosshairBottom = CreateCrosshairLine("Bottom", crosshairRoot, new Vector2(2.5f, 7f));
            crosshairLeft = CreateCrosshairLine("Left", crosshairRoot, new Vector2(7f, 2.5f));
            crosshairRight = CreateCrosshairLine("Right", crosshairRoot, new Vector2(7f, 2.5f));

            RectTransform dotRect = DustlineUiTheme.CreateRect(
                "Dot", crosshairRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(5f, 5f), Vector2.zero);
            crosshairDot = DustlineUiTheme.AddImage(dotRect, null, DustlineUiTheme.Text, false);
            crosshairDot.raycastTarget = false;

            SetCrosshairGap(crosshairBaseGap);
            BuildHitMarker(crosshairRoot);
        }

        private static RectTransform CreateCrosshairLine(string name, RectTransform parent, Vector2 size)
        {
            RectTransform line = DustlineUiTheme.CreateRect(
                name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), size, Vector2.zero);
            Image image = DustlineUiTheme.AddImage(line, null, DustlineUiTheme.Gold, false);
            image.raycastTarget = false;
            return line;
        }

        private void SanitizeCrosshairSettings()
        {
            crosshairBaseGap = Mathf.Clamp(crosshairBaseGap, 6f, 10f);
            crosshairBusyGap = Mathf.Clamp(crosshairBusyGap, crosshairBaseGap + 3f, 18f);
            crosshairFireBloomMax = Mathf.Clamp(crosshairFireBloomMax, 4f, 16f);
            crosshairFireBloomDecay = Mathf.Clamp(crosshairFireBloomDecay, 34f, 80f);
        }

        private void BuildHitMarker(RectTransform parent)
        {
            hitMarkerRoot = DustlineUiTheme.CreateRect(
                "Hit_Marker",
                parent,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(44f, 44f),
                Vector2.zero);
            hitMarkerGroup = hitMarkerRoot.gameObject.AddComponent<CanvasGroup>();
            hitMarkerGroup.alpha = 0f;
            hitMarkerGroup.blocksRaycasts = false;
            hitMarkerGroup.interactable = false;

            CreateHitMarkerLine("Top_Left", hitMarkerRoot, new Vector2(-12f, 12f), -45f);
            CreateHitMarkerLine("Top_Right", hitMarkerRoot, new Vector2(12f, 12f), 45f);
            CreateHitMarkerLine("Bottom_Left", hitMarkerRoot, new Vector2(-12f, -12f), 45f);
            CreateHitMarkerLine("Bottom_Right", hitMarkerRoot, new Vector2(12f, -12f), -45f);
        }

        private static void CreateHitMarkerLine(string name, RectTransform parent, Vector2 position, float rotation)
        {
            RectTransform line = DustlineUiTheme.CreateRect(
                name,
                parent,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(4f, 18f),
                position);
            line.localRotation = Quaternion.Euler(0f, 0f, rotation);
            Image image = DustlineUiTheme.AddImage(line, null, Color.white, false);
            image.raycastTarget = false;
        }

        private void BuildDamageFlash(RectTransform parent)
        {
            RectTransform flashRect = DustlineUiTheme.CreateRect(
                "Damage_Flash",
                parent,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
            damageFlash = DustlineUiTheme.AddImage(flashRect, null, new Color(1f, 0.05f, 0.02f, 0f), false);
            damageFlash.raycastTarget = false;
            flashRect.SetAsFirstSibling();
        }

        private void BuildGameStateOverlay(RectTransform parent)
        {
            bannerRoot = DustlineUiTheme.CreateRect(
                "Wave_Banner",
                parent,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(620f, 132f),
                new Vector2(0f, 92f));
            bannerGroup = bannerRoot.gameObject.AddComponent<CanvasGroup>();
            bannerGroup.alpha = 0f;
            bannerGroup.blocksRaycasts = false;
            bannerGroup.interactable = false;

            RectTransform bannerTitleRect = DustlineUiTheme.CreateRect(
                "Title",
                bannerRoot,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                new Vector2(-16f, -50f),
                new Vector2(0f, 18f));
            bannerTitle = DustlineUiTheme.AddText(bannerTitleRect, "WAVE", 54f, DustlineUiTheme.Gold, TextAlignmentOptions.Center, FontStyles.Bold);

            RectTransform bannerSubtitleRect = DustlineUiTheme.CreateRect(
                "Subtitle",
                bannerRoot,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                new Vector2(-16f, -88f),
                new Vector2(0f, -42f));
            bannerSubtitle = DustlineUiTheme.AddText(bannerSubtitleRect, "SURVIVE", 18f, DustlineUiTheme.Text, TextAlignmentOptions.Center, FontStyles.Bold);

            pauseRoot = DustlineUiTheme.CreateRect(
                "Pause_Overlay",
                parent,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
            DustlineUiTheme.AddImage(pauseRoot, null, new Color(0.012f, 0.014f, 0.018f, 0.68f), false);
            pauseGroup = pauseRoot.gameObject.AddComponent<CanvasGroup>();
            pauseGroup.alpha = 0f;
            pauseGroup.blocksRaycasts = false;
            pauseGroup.interactable = false;

            RectTransform pausePanel = DustlineUiTheme.CreateRect(
                "Panel",
                pauseRoot,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(640f, 720f),
                Vector2.zero);
            DustlineUiTheme.AddImage(pausePanel, "PanelDark", DustlineUiTheme.Background, true);
            AddAccent(pausePanel);

            pauseMainContent = DustlineUiTheme.CreateRect(
                "Main",
                pausePanel,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);

            RectTransform pauseTitleRect = DustlineUiTheme.CreateRect(
                "Title",
                pauseMainContent,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(540f, 64f),
                new Vector2(0f, 270f));
            pauseTitle = DustlineUiTheme.AddText(pauseTitleRect, "PAUSED", 46f, DustlineUiTheme.Gold, TextAlignmentOptions.Center, FontStyles.Bold);

            RectTransform pauseSubtitleRect = DustlineUiTheme.CreateRect(
                "Subtitle",
                pauseMainContent,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(540f, 30f),
                new Vector2(0f, 220f));
            pauseSubtitle = DustlineUiTheme.AddText(pauseSubtitleRect, "TAKE A BREATH", 20f, DustlineUiTheme.Text, TextAlignmentOptions.Center, FontStyles.Bold);

            pauseResumeButton = CreateClearButton(pauseMainContent, "RESUME", new Vector2(0f, 124f), () => SetPaused(false), 480f);
            pauseSettingsButton = CreateClearButton(pauseMainContent, "SETTINGS", new Vector2(0f, 42f), OpenPauseSettings, 480f);
            CreateClearButton(pauseMainContent, "RESTART ARENA", new Vector2(0f, -40f), RestartCurrentScene, 480f);
            CreateClearButton(pauseMainContent, "MAIN MENU", new Vector2(0f, -122f), LoadMainMenu, 480f);

            RectTransform pauseHintRect = DustlineUiTheme.CreateRect(
                "Hint",
                pauseMainContent,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(540f, 28f),
                new Vector2(0f, -272f));
            pauseHint = DustlineUiTheme.AddText(pauseHintRect, "ESC  RESUME     R  RESTART", 17f, DustlineUiTheme.Muted, TextAlignmentOptions.Center, FontStyles.Bold);

            pauseSettingsContent = DustlineUiTheme.CreateRect(
                "Audio_Settings",
                pausePanel,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);

            RectTransform settingsTitleRect = DustlineUiTheme.CreateRect(
                "Title",
                pauseSettingsContent,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(540f, 64f),
                new Vector2(0f, 270f));
            DustlineUiTheme.AddText(settingsTitleRect, "AUDIO SETTINGS", 40f, DustlineUiTheme.Gold, TextAlignmentOptions.Center, FontStyles.Bold);

            RectTransform settingsSubtitleRect = DustlineUiTheme.CreateRect(
                "Subtitle",
                pauseSettingsContent,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(540f, 30f),
                new Vector2(0f, 220f));
            DustlineUiTheme.AddText(settingsSubtitleRect, "CHANGES ARE SAVED AUTOMATICALLY", 16f, DustlineUiTheme.Muted, TextAlignmentOptions.Center, FontStyles.Bold);

            GameAudio audio = GameAudio.Instance;
            pauseMasterSlider = CreatePauseVolumeSlider(pauseSettingsContent, "MASTER", audio.MasterVolume, new Vector2(0f, 124f), value => audio.MasterVolume = value);
            CreatePauseVolumeSlider(pauseSettingsContent, "MUSIC", audio.MusicVolume, new Vector2(0f, 38f), value => audio.MusicVolume = value);
            CreatePauseVolumeSlider(pauseSettingsContent, "SFX", audio.SfxVolume, new Vector2(0f, -48f), value => audio.SfxVolume = value);
            CreatePauseVolumeSlider(pauseSettingsContent, "UI", audio.UiVolume, new Vector2(0f, -134f), value => audio.UiVolume = value);
            pauseSettingsBackButton = CreateClearButton(pauseSettingsContent, "BACK", new Vector2(0f, -250f), () => ShowPauseMainMenu(true), 480f, false);
            pauseSettingsContent.gameObject.SetActive(false);

            clearRoot = DustlineUiTheme.CreateRect(
                "Area_Clear_Overlay",
                parent,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
            DustlineUiTheme.AddImage(clearRoot, null, new Color(0.012f, 0.016f, 0.018f, 0.78f), false);
            clearGroup = clearRoot.gameObject.AddComponent<CanvasGroup>();
            clearGroup.alpha = 0f;
            clearGroup.blocksRaycasts = false;
            clearGroup.interactable = false;

            RectTransform clearPanel = DustlineUiTheme.CreateRect(
                "Panel",
                clearRoot,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(780f, 620f),
                Vector2.zero);
            DustlineUiTheme.AddImage(clearPanel, "PanelDark", DustlineUiTheme.Background, true);
            AddAccent(clearPanel);

            RectTransform clearTitleRect = DustlineUiTheme.CreateRect(
                "Title",
                clearPanel,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(700f, 72f),
                new Vector2(0f, 218f));
            clearTitle = DustlineUiTheme.AddText(clearTitleRect, "AREA CLEARED", 52f, DustlineUiTheme.Gold, TextAlignmentOptions.Center, FontStyles.Bold);

            RectTransform clearSubtitleRect = DustlineUiTheme.CreateRect(
                "Subtitle",
                clearPanel,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(700f, 34f),
                new Vector2(0f, 158f));
            clearSubtitle = DustlineUiTheme.AddText(clearSubtitleRect, "ALL WAVES COMPLETE", 24f, DustlineUiTheme.Text, TextAlignmentOptions.Center, FontStyles.Bold);

            RectTransform statsLabelRect = DustlineUiTheme.CreateRect(
                "Stats_Label",
                clearPanel,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(650f, 26f),
                new Vector2(0f, 94f));
            DustlineUiTheme.AddText(statsLabelRect, "AFTER ACTION REPORT", 17f, DustlineUiTheme.Muted, TextAlignmentOptions.Center, FontStyles.Bold);

            RectTransform clearStatsRect = DustlineUiTheme.CreateRect(
                "Stats",
                clearPanel,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(650f, 72f),
                new Vector2(0f, 40f));
            clearStats = DustlineUiTheme.AddText(clearStatsRect, "HOSTILES  0     DAMAGE  0     TIME  00:00", 22f, DustlineUiTheme.Text, TextAlignmentOptions.Center, FontStyles.Bold);

            clearNextButton = CreateClearButton(clearPanel, "NEXT ARENA", new Vector2(-166f, -86f), ContinueAfterArena);
            clearMenuButton = CreateClearButton(clearPanel, "MAIN MENU", new Vector2(166f, -86f), ReturnToMainMenu);

            RectTransform clearHintRect = DustlineUiTheme.CreateRect(
                "Hint",
                clearPanel,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(650f, 28f),
                new Vector2(0f, -190f));
            clearHint = DustlineUiTheme.AddText(clearHintRect, "ENTER  CONTINUE     ESC  MAIN MENU", 17f, DustlineUiTheme.Muted, TextAlignmentOptions.Center, FontStyles.Bold);

            deathRoot = DustlineUiTheme.CreateRect(
                "Death_Overlay",
                parent,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
            DustlineUiTheme.AddImage(deathRoot, null, new Color(0.02f, 0.015f, 0.012f, 0.72f), false);
            deathGroup = deathRoot.gameObject.AddComponent<CanvasGroup>();
            deathGroup.alpha = 0f;
            deathGroup.blocksRaycasts = false;
            deathGroup.interactable = false;

            RectTransform deathPanel = DustlineUiTheme.CreateRect(
                "Panel",
                deathRoot,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(760f, 590f),
                Vector2.zero);
            DustlineUiTheme.AddImage(deathPanel, "PanelDark", DustlineUiTheme.Background, true);
            AddAccent(deathPanel);

            RectTransform deathTitleRect = DustlineUiTheme.CreateRect(
                "Title",
                deathPanel,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(680f, 76f),
                new Vector2(0f, 206f));
            deathTitle = DustlineUiTheme.AddText(deathTitleRect, "OPERATION FAILED", 52f, DustlineUiTheme.Danger, TextAlignmentOptions.Center, FontStyles.Bold);

            RectTransform deathSubtitleRect = DustlineUiTheme.CreateRect(
                "Subtitle",
                deathPanel,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(680f, 32f),
                new Vector2(0f, 150f));
            deathSubtitle = DustlineUiTheme.AddText(deathSubtitleRect, "THE DUSTLINE CLAIMED ANOTHER RUN", 20f, DustlineUiTheme.Text, TextAlignmentOptions.Center, FontStyles.Bold);

            RectTransform deathStatsLabelRect = DustlineUiTheme.CreateRect(
                "Stats_Label",
                deathPanel,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(650f, 28f),
                new Vector2(0f, 82f));
            DustlineUiTheme.AddText(deathStatsLabelRect, "COMBAT REPORT", 17f, DustlineUiTheme.Muted, TextAlignmentOptions.Center, FontStyles.Bold);

            RectTransform deathStatsRect = DustlineUiTheme.CreateRect(
                "Stats",
                deathPanel,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(650f, 68f),
                new Vector2(0f, 28f));
            deathStats = DustlineUiTheme.AddText(deathStatsRect, "ARENA 01 / 05   WAVE 1 / 4   HOSTILES 00   TIME 00:00", 19f, DustlineUiTheme.Text, TextAlignmentOptions.Center, FontStyles.Bold);

            deathRestartButton = CreateClearButton(deathPanel, "RETRY ARENA", new Vector2(-166f, -96f), RestartCurrentScene);
            deathMenuButton = CreateClearButton(deathPanel, "MAIN MENU", new Vector2(166f, -96f), LoadMainMenu);

            RectTransform deathHintRect = DustlineUiTheme.CreateRect(
                "Hint",
                deathPanel,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(650f, 28f),
                new Vector2(0f, -206f));
            DustlineUiTheme.AddText(deathHintRect, "ENTER  RETRY     ESC  MAIN MENU", 17f, DustlineUiTheme.Muted, TextAlignmentOptions.Center, FontStyles.Bold);
        }

        private static void AddAccent(RectTransform panel)
        {
            RectTransform accent = DustlineUiTheme.CreateRect(
                "Accent", panel, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), new Vector2(-30f, 5f), new Vector2(0f, -3f));
            DustlineUiTheme.AddImage(accent, "AccentGold", DustlineUiTheme.Gold, true);
        }

        private static Button CreateClearButton(
            Transform parent,
            string label,
            Vector2 position,
            UnityEngine.Events.UnityAction action,
            float width = 300f,
            bool playConfirmSound = true)
        {
            RectTransform rect = DustlineUiTheme.CreateRect(
                $"{label.Replace(' ', '_')}_Button",
                parent,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(width, 68f),
                position);
            Color idleColor = new Color(0.075f, 0.085f, 0.095f, 0.98f);
            Image background = DustlineUiTheme.AddImage(rect, "PanelLight", idleColor, true);
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
                new Vector2(-60f, 0f),
                new Vector2(20f, 0f));
            TMP_Text text = DustlineUiTheme.AddText(labelRect, label, 21f, DustlineUiTheme.Text, TextAlignmentOptions.Center, FontStyles.Bold);

            MenuButtonFeedback feedback = rect.gameObject.AddComponent<MenuButtonFeedback>();
            feedback.Initialize(background, accent, text, idleColor, new Color(0.13f, 0.14f, 0.15f, 1f));
            return button;
        }

        private static Slider CreatePauseVolumeSlider(
            Transform parent,
            string label,
            float initialValue,
            Vector2 position,
            System.Action<float> onChanged)
        {
            RectTransform row = DustlineUiTheme.CreateRect(
                $"{label}_Row",
                parent,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(500f, 72f),
                position);

            RectTransform labelRect = DustlineUiTheme.CreateRect(
                "Label",
                row,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(180f, 28f),
                Vector2.zero);
            DustlineUiTheme.AddText(labelRect, label, 18f, DustlineUiTheme.Text, TextAlignmentOptions.Left, FontStyles.Bold);

            RectTransform valueRect = DustlineUiTheme.CreateRect(
                "Value",
                row,
                Vector2.one,
                Vector2.one,
                Vector2.one,
                new Vector2(80f, 28f),
                Vector2.zero);
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
                new Vector2(0.5f, 0f),
                new Vector2(0f, 28f),
                new Vector2(0f, 4f));

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
            return slider;
        }

        private void RefreshAll()
        {
            RefreshHealth();
            RefreshWeapon();
            RefreshGrenades();
            RefreshDodgeCooldown();
            RefreshMissionProgress();
            RefreshWave();
        }

        private void OnHealthChanged(HealthChangedArgs args)
        {
            RefreshHealth();
            if (args.Delta < 0f)
            {
                arenaDamageTaken += -args.Delta;
                PunchText(healthValue, new Vector3(0.16f, 0.16f, 0f), 0.16f);
                ShowDamageFlash(-args.Delta);
            }
            else if (args.Delta > 0f)
            {
                ShowHealthGain(args.Delta);
            }
        }

        private void OnPlayerDied()
        {
            CancelPendingLevelComplete();
            RefreshHealth();
            GameAudio.PlayGameOver();
            ShowDeathOverlay();
        }

        private void RefreshHealth()
        {
            float current = health == null ? 0f : health.Current;
            float maximum = health == null ? 0f : health.Max;
            float normalized = maximum <= 0f ? 0f : current / maximum;
            if (healthFill != null)
            {
                bool animate = lastHealthNormalized >= 0f && !Mathf.Approximately(lastHealthNormalized, normalized);
                healthFill.DOKill();
                if (animate)
                {
                    DOTween.To(() => healthFill.fillAmount, value => healthFill.fillAmount = value, normalized, 0.18f)
                        .SetEase(Ease.OutQuad)
                        .SetUpdate(true)
                        .SetTarget(this);
                }
                else
                {
                    healthFill.fillAmount = normalized;
                }

                healthFill.color = GetHealthColor(normalized);
            }

            if (healthValue != null)
            {
                healthValue.text = $"{current:0} / {maximum:0}";
                healthValue.color = normalized <= 0.25f ? DustlineUiTheme.Danger : DustlineUiTheme.Text;
            }

            lastHealthNormalized = normalized;
        }

        private void ShowHealthGain(float amount)
        {
            Color healColor = new Color(0.46f, 1f, 0.55f, 1f);
            PunchText(healthValue, new Vector3(0.12f, 0.12f, 0f), 0.16f);

            if (healthFill != null)
            {
                DOTween.Kill(healthFill);
                healthFill.color = healColor;
                DOTween.To(() => healthFill.color, value => healthFill.color = value, GetHealthColor(lastHealthNormalized), 0.34f)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true)
                    .SetTarget(healthFill);
            }

            if (healthValue != null)
            {
                DOTween.Kill(healthValue);
                healthValue.color = healColor;
                DOTween.To(() => healthValue.color, value => healthValue.color = value, lastHealthNormalized <= 0.25f ? DustlineUiTheme.Danger : DustlineUiTheme.Text, 0.28f)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true)
                    .SetTarget(healthValue);
            }

            if (healthDeltaRect == null || healthDeltaGroup == null || healthDeltaValue == null)
            {
                return;
            }

            Vector2 startPosition = new Vector2(-24f, -49f);
            healthDeltaRect.anchoredPosition = startPosition;
            healthDeltaRect.localScale = Vector3.one;
            healthDeltaGroup.alpha = 0f;
            healthDeltaValue.text = $"+{Mathf.CeilToInt(amount)}";
            healthDeltaValue.color = healColor;

            DOTween.Kill(healthDeltaRect);
            DOTween.Kill(healthDeltaGroup);

            Sequence sequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetTarget(healthDeltaRect);

            sequence.Append(DOTween.To(() => healthDeltaGroup.alpha, value => healthDeltaGroup.alpha = value, 1f, 0.08f)
                .SetEase(Ease.OutQuad));
            sequence.Join(DOTween.To(() => healthDeltaRect.localScale, value => healthDeltaRect.localScale = value, Vector3.one * 1.12f, 0.12f)
                .SetEase(Ease.OutBack));
            sequence.AppendInterval(0.1f);
            sequence.Join(DOTween.To(() => healthDeltaRect.anchoredPosition, value => healthDeltaRect.anchoredPosition = value, startPosition + new Vector2(0f, 18f), 0.42f)
                .SetEase(Ease.OutCubic));
            sequence.Join(DOTween.To(() => healthDeltaGroup.alpha, value => healthDeltaGroup.alpha = value, 0f, 0.34f)
                .SetEase(Ease.InQuad));
        }

        private static Color GetHealthColor(float normalized)
        {
            return normalized <= 0.25f ? DustlineUiTheme.Danger : Color.white;
        }

        private static Color GetAmmoColor(int ammoInMagazine, int magazineSize)
        {
            if (ammoInMagazine <= 0)
            {
                return DustlineUiTheme.Danger;
            }

            int lowAmmoThreshold = Mathf.Max(1, Mathf.CeilToInt(magazineSize * 0.25f));
            return ammoInMagazine <= lowAmmoThreshold ? DustlineUiTheme.Gold : DustlineUiTheme.Text;
        }

        private void RefreshWeapon()
        {
            if (weaponName == null)
            {
                return;
            }

            if (weapon == null || weapon.Config == null)
            {
                weaponName.text = "NO WEAPON";
                magazineValue.text = "--";
                reserveValue.text = "---";
                SetWeaponIconVisible(false);
                return;
            }

            weaponName.text = weapon.Config.Visual.ToString().ToUpperInvariant();
            ConfigureWeaponIcon(weapon.Config.Visual);
            int currentMagazine = weapon.AmmoInMagazine;
            int currentReserve = weapon.ReserveAmmo;
            ResetTextScale(magazineValue);
            magazineValue.text = currentMagazine.ToString("00");
            reserveValue.text = currentReserve.ToString("000");
            magazineValue.color = GetAmmoColor(currentMagazine, weapon.Config.MagazineSize);

            if (lastReserve != int.MinValue && lastReserve != currentReserve)
            {
                PunchText(reserveValue, new Vector3(0.1f, 0.1f, 0f), 0.14f);
            }

            lastMagazine = currentMagazine;
            lastReserve = currentReserve;
            RefreshReload();
        }

        private void RefreshReload()
        {
            if (reloadGroup == null)
            {
                return;
            }

            bool reloading = weapon != null && weapon.IsReloading;
            reloadFill.fillAmount = reloading ? weapon.ReloadProgress01 : 0f;
            if (reloading)
            {
                reloadValue.text = weapon.UsesIncrementalReload
                    ? $"LOADING SHELL  {weapon.ReloadProgress01 * 100f:0}%"
                    : $"RELOADING  {weapon.ReloadProgress01 * 100f:0}%";
                reloadValue.color = DustlineUiTheme.Gold;
            }
            else if (weapon != null && weapon.AmmoInMagazine == 0 && weapon.ReserveAmmo > 0)
            {
                reloadValue.text = "EMPTY  -  PRESS R";
                reloadValue.color = DustlineUiTheme.Danger;
            }
            else if (weapon != null && weapon.CanReload)
            {
                reloadValue.text = "R  -  RELOAD";
                reloadValue.color = DustlineUiTheme.Muted;
            }
            else
            {
                reloadValue.text = "MAGAZINE READY";
                reloadValue.color = DustlineUiTheme.Muted;
            }
        }

        private void RefreshGrenades()
        {
            int current = grenades == null ? 0 : grenades.GrenadeCount;
            int maximum = grenades == null ? 0 : grenades.MaximumGrenades;
            if (grenadeValue != null)
            {
                grenadeValue.text = $"{current} / {maximum}";
                grenadeValue.color = current > 0 ? DustlineUiTheme.Text : DustlineUiTheme.Danger;
                grenadeValue.alpha = maximum > 0 ? 1f : 0.55f;
                if (lastGrenadeCount != int.MinValue && lastGrenadeCount != current)
                {
                    PunchText(grenadeValue, new Vector3(0.14f, 0.14f, 0f), 0.16f);
                }
            }

            lastGrenadeCount = current;
            RefreshGrenadeCharge();
        }

        private void RefreshGrenadeCharge()
        {
            if (grenadeChargeFill == null)
            {
                return;
            }

            float cooldown = grenades == null ? 0f : grenades.Cooldown01;
            bool charging = grenades != null && grenades.IsCharging;
            bool coolingDown = !charging && cooldown > 0f;

            grenadeChargeFill.fillAmount = charging ? grenades.Charge01 : cooldown;
            grenadeChargeFill.color = charging
                ? new Color(DustlineUiTheme.Gold.r, DustlineUiTheme.Gold.g, DustlineUiTheme.Gold.b, 0.55f)
                : coolingDown
                    ? new Color(DustlineUiTheme.Danger.r, DustlineUiTheme.Danger.g, DustlineUiTheme.Danger.b, 0.48f)
                    : new Color(DustlineUiTheme.Gold.r, DustlineUiTheme.Gold.g, DustlineUiTheme.Gold.b, 0.20f);

            if (grenadeLabel != null)
            {
                grenadeLabel.text = coolingDown ? "COOLDOWN" : "GRENADE  [G]";
                grenadeLabel.color = coolingDown ? DustlineUiTheme.Danger : DustlineUiTheme.Muted;
            }
        }

        private void RefreshDodgeCooldown()
        {
            if (dodgeRoot == null || dodgeCooldownFill == null)
            {
                return;
            }

            if (playerMotor == null)
            {
                dodgeCooldownFill.fillAmount = 0f;
                if (dodgeValue != null)
                {
                    dodgeValue.text = "--";
                    dodgeValue.color = DustlineUiTheme.Muted;
                }

                return;
            }

            float progress = playerMotor.DodgeCooldownProgress01;
            bool ready = playerMotor.IsDodgeReady;
            dodgeCooldownFill.fillAmount = progress;
            dodgeCooldownFill.color = ready
                ? DustlineUiTheme.Gold
                : new Color(DustlineUiTheme.Danger.r, DustlineUiTheme.Danger.g, DustlineUiTheme.Danger.b, 0.86f);

            if (dodgeValue != null)
            {
                dodgeValue.text = ready ? "READY" : $"{progress * 100f:0}%";
                dodgeValue.color = ready ? DustlineUiTheme.Gold : DustlineUiTheme.Danger;

                if (!lastDodgeReady && ready)
                {
                    PunchText(dodgeValue, new Vector3(0.14f, 0.14f, 0f), 0.16f);
                }
            }

            if (dodgeLabel != null)
            {
                dodgeLabel.color = ready ? DustlineUiTheme.Muted : DustlineUiTheme.Danger;
            }

            lastDodgeReady = ready;
        }

        private void OnWaveStarted(int index)
        {
            RefreshWave();
            GameAudio.PlayWaveStarted(index);
            string waveName = waveSpawner == null ? string.Empty : waveSpawner.GetWaveName(index);
            ShowBanner($"WAVE {index + 1}", string.IsNullOrWhiteSpace(waveName) ? "SURVIVE" : waveName.ToUpperInvariant());
        }

        private void OnWaveCompleted(int index)
        {
            RefreshWave();
            if (waveSpawner == null || index < waveSpawner.WaveCount - 1)
            {
                GameAudio.PlayWaveCleared();
            }

            ShowBanner($"WAVE {index + 1} CLEARED", "BREATHE, THEN MOVE");
        }

        private void OnEnemyKilled(int totalKills)
        {
            arenaKills = Mathf.Max(arenaKills, totalKills);
            RefreshWave();
        }

        private void OnAllWavesCompleted()
        {
            RefreshWave();
            if (isGameOver || isLevelComplete || isLevelCompletePending)
            {
                return;
            }

            GameAudio.PlayArenaCleared();
            ShowBanner("AREA SECURED", "STAND BY FOR EXTRACTION");
            isLevelCompletePending = true;
            levelCompleteRoutine = StartCoroutine(ShowLevelCompleteAfterDelay());
        }

        private IEnumerator ShowLevelCompleteAfterDelay()
        {
            yield return new WaitForSeconds(LevelCompleteOverlayDelay);
            levelCompleteRoutine = null;
            isLevelCompletePending = false;

            if (!isGameOver)
            {
                ShowLevelCompleteOverlay();
            }
        }

        private void CancelPendingLevelComplete()
        {
            if (levelCompleteRoutine != null)
            {
                StopCoroutine(levelCompleteRoutine);
                levelCompleteRoutine = null;
            }

            isLevelCompletePending = false;
        }

        private void OnAnyDamaged(HealthComponent target, DamageInfo damage)
        {
            if (target == null || damage.SourceTeam != TeamId.Player)
            {
                return;
            }

            TeamMember targetTeamMember = target.GetComponentInParent<TeamMember>();
            if (targetTeamMember == null || targetTeamMember.Team != TeamId.Enemy)
            {
                return;
            }

            ShowHitMarker(!target.IsAlive);
        }

        private void RefreshWave()
        {
            if (waveValue == null || enemyValue == null)
            {
                return;
            }

            if (waveSpawner == null)
            {
                waveValue.text = "WAVE --";
                enemyValue.text = "-- HOSTILES";
                return;
            }

            int count = waveSpawner.WaveCount;
            int current = count <= 0 ? 0 : Mathf.Clamp(waveSpawner.CurrentWaveIndex + 1, 1, count);
            waveValue.text = count <= 0 ? "WAVE --" : $"WAVE {current} / {count}";
            enemyValue.text = $"{waveSpawner.ActiveEnemyCount} HOSTILES";
            if (lastWaveIndex != int.MinValue && lastWaveIndex != current)
            {
                PunchText(waveValue, new Vector3(0.1f, 0.1f, 0f), 0.16f);
            }

            if (lastActiveEnemies != int.MinValue && lastActiveEnemies != waveSpawner.ActiveEnemyCount)
            {
                PunchText(enemyValue, new Vector3(0.08f, 0.08f, 0f), 0.14f);
            }

            lastWaveIndex = current;
            lastActiveEnemies = waveSpawner.ActiveEnemyCount;
        }

        private void RefreshMissionProgress()
        {
            if (missionValue != null)
            {
                missionValue.text = $"ARENA {arenaIndex:00} / {OperationProgress.TotalArenas:00}  //  {OperationProgress.GetSectorName(arenaIndex)}";
            }

            if (previousArenaValue == null)
            {
                return;
            }

            if (OperationProgress.TryGetArenaResult(arenaIndex - 1, out ArenaRunResult previous))
            {
                previousArenaValue.text = $"LAST: ARENA {previous.ArenaIndex:00}  //  {previous.Kills:00} HOSTILES  //  {FormatElapsedTime(previous.Duration)}";
                return;
            }

            previousArenaValue.text = "OPERATION START  //  5 SECTORS";
        }

        private void OnWeaponFired()
        {
            if (cameraFollow != null)
            {
                cameraFollow.AddShake(GetWeaponShakeStrength());
            }

            if (crosshairRoot == null)
            {
                return;
            }

            float bloom = GetWeaponCrosshairBloom();
            crosshairFireBloom = Mathf.Min(GetWeaponCrosshairBloomMax(), crosshairFireBloom + bloom);
            Vector2 punch = GetWeaponCrosshairPunch();
            if (punch.sqrMagnitude > 0.0001f)
            {
                crosshairRoot.DOKill();
                crosshairRoot.localScale = Vector3.one;
                crosshairRoot.DOPunchScale(new Vector3(punch.x, punch.y, 0f), 0.11f, 6, 0.35f)
                    .SetUpdate(true)
                    .SetTarget(this);
            }
        }

        private float GetWeaponShakeStrength()
        {
            if (weapon == null || weapon.Config == null)
            {
                return 0.035f;
            }

            switch (weapon.Config.Visual)
            {
                case Config.WeaponVisualId.Pistol:
                    return 0.055f;
                case Config.WeaponVisualId.SMG:
                    return 0.035f;
                case Config.WeaponVisualId.Shotgun:
                    return 0.16f;
                default:
                    return 0.075f;
            }
        }

        private float GetWeaponCrosshairBloom()
        {
            if (weapon == null || weapon.Config == null)
            {
                return 8f;
            }

            switch (weapon.Config.Visual)
            {
                case Config.WeaponVisualId.Pistol:
                    return 3f;
                case Config.WeaponVisualId.SMG:
                    return 0.8f;
                case Config.WeaponVisualId.Shotgun:
                    return 10f;
                default:
                    return 2f;
            }
        }

        private float GetWeaponCrosshairBloomMax()
        {
            if (weapon == null || weapon.Config == null)
            {
                return Mathf.Min(crosshairFireBloomMax, 14f);
            }

            switch (weapon.Config.Visual)
            {
                case Config.WeaponVisualId.SMG:
                    return Mathf.Min(crosshairFireBloomMax, 3f);
                case Config.WeaponVisualId.AK:
                    return Mathf.Min(crosshairFireBloomMax, 5f);
                case Config.WeaponVisualId.Pistol:
                    return Mathf.Min(crosshairFireBloomMax, 6f);
                case Config.WeaponVisualId.Shotgun:
                    return crosshairFireBloomMax;
                default:
                    return Mathf.Min(crosshairFireBloomMax, 5f);
            }
        }

        private Vector2 GetWeaponCrosshairPunch()
        {
            if (weapon == null || weapon.Config == null)
            {
                return new Vector2(0.04f, 0.04f);
            }

            switch (weapon.Config.Visual)
            {
                case Config.WeaponVisualId.SMG:
                    return Vector2.zero;
                case Config.WeaponVisualId.AK:
                    return new Vector2(0.04f, 0.04f);
                case Config.WeaponVisualId.Pistol:
                    return new Vector2(0.06f, 0.06f);
                case Config.WeaponVisualId.Shotgun:
                    return new Vector2(0.16f, 0.16f);
                default:
                    return new Vector2(0.05f, 0.05f);
            }
        }

        private void ShowHitMarker(bool lethal)
        {
            if (hitMarkerRoot == null || hitMarkerGroup == null)
            {
                return;
            }

            Color markerColor = lethal ? DustlineUiTheme.Gold : Color.white;
            foreach (Graphic graphic in hitMarkerRoot.GetComponentsInChildren<Graphic>(true))
            {
                graphic.color = markerColor;
            }

            DOTween.Kill(hitMarkerRoot);
            DOTween.Kill(hitMarkerGroup);

            hitMarkerRoot.localScale = Vector3.one * (lethal ? 0.92f : 0.78f);
            hitMarkerGroup.alpha = 1f;

            Sequence sequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetTarget(hitMarkerRoot);

            sequence.Append(DOTween.To(() => hitMarkerRoot.localScale, value => hitMarkerRoot.localScale = value, Vector3.one * (lethal ? 1.22f : 1.08f), 0.08f)
                .SetEase(Ease.OutBack));
            sequence.Join(DOTween.To(() => hitMarkerGroup.alpha, value => hitMarkerGroup.alpha = value, 0f, lethal ? 0.28f : 0.18f)
                .SetEase(Ease.InQuad));
        }

        private void ShowBanner(string title, string subtitle)
        {
            if (bannerRoot == null || bannerGroup == null || bannerTitle == null || bannerSubtitle == null || isGameOver)
            {
                return;
            }

            bannerTitle.text = title;
            bannerSubtitle.text = subtitle;
            bannerRoot.localScale = Vector3.one * 0.88f;
            bannerGroup.alpha = 0f;

            DOTween.Kill(bannerRoot);
            DOTween.Kill(bannerGroup);

            Sequence sequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetTarget(bannerRoot);

            sequence.Append(DOTween.To(() => bannerGroup.alpha, value => bannerGroup.alpha = value, 1f, 0.16f)
                .SetEase(Ease.OutQuad));
            sequence.Join(DOTween.To(() => bannerRoot.localScale, value => bannerRoot.localScale = value, Vector3.one, 0.22f)
                .SetEase(Ease.OutBack));
            sequence.AppendInterval(1.1f);
            sequence.Append(DOTween.To(() => bannerGroup.alpha, value => bannerGroup.alpha = value, 0f, 0.28f)
                .SetEase(Ease.InQuad));
        }

        private void ShowLevelCompleteOverlay()
        {
            if (isGameOver || isLevelComplete || clearRoot == null || clearGroup == null || clearTitle == null || clearSubtitle == null)
            {
                ShowBanner("AREA CLEARED", "ALL WAVES COMPLETE");
                return;
            }

            isLevelComplete = true;
            arenaElapsedTime = Mathf.Max(0f, Time.time - arenaStartTime);
            OperationProgress.RecordArenaResult(arenaIndex, arenaKills, arenaDamageTaken, arenaElapsedTime);
            sceneTransition = sceneTransition == null && waveSpawner != null ? waveSpawner.GetComponent<SceneWaveTransition>() : sceneTransition;
            sceneTransition = sceneTransition == null ? FindObjectOfType<SceneWaveTransition>() : sceneTransition;

            bool hasNextScene = sceneTransition != null && sceneTransition.HasNextScene;
            levelCompleteNextScene = hasNextScene ? sceneTransition.NextSceneName : string.Empty;

            prePauseTimeScale = Time.timeScale <= 0f ? 1f : Time.timeScale;
            Time.timeScale = 0f;
            GameAudio.SetGameplayPaused(true);
            Cursor.visible = true;
            EnsureEventSystem();
            if (hudRaycaster != null)
            {
                hudRaycaster.enabled = true;
            }

            if (crosshairGroup != null)
            {
                crosshairGroup.alpha = 0f;
            }

            DOTween.Kill(bannerRoot);
            DOTween.Kill(bannerGroup);
            if (bannerGroup != null)
            {
                bannerGroup.alpha = 0f;
            }

            clearTitle.text = hasNextScene ? "AREA CLEARED" : "RUN COMPLETE";
            clearSubtitle.text = hasNextScene
                ? $"NEXT: {FormatSceneName(levelCompleteNextScene)}"
                : "ALL AREAS SECURED";
            if (clearStats != null)
            {
                OperationTotals totals = OperationProgress.GetTotals();
                clearStats.text =
                    $"SECTOR {arenaIndex:00}   HOSTILES {arenaKills:00}   DAMAGE {arenaDamageTaken:0}   TIME {FormatElapsedTime(arenaElapsedTime)}\n" +
                    $"OPERATION   {totals.ArenasCleared} / {OperationProgress.TotalArenas} CLEARED   HOSTILES {totals.Kills:00}   TIME {FormatElapsedTime(totals.Duration)}";
            }

            if (clearHint != null)
            {
                clearHint.text = hasNextScene
                    ? "ENTER  NEXT ARENA     ESC  MAIN MENU"
                    : "ENTER  COMPLETE OPERATION";
            }

            if (clearNextButton != null)
            {
                clearNextButton.gameObject.SetActive(hasNextScene);
            }

            if (clearMenuButton != null)
            {
                clearMenuButton.GetComponent<RectTransform>().anchoredPosition = hasNextScene
                    ? new Vector2(166f, -86f)
                    : new Vector2(0f, -86f);
            }

            clearGroup.blocksRaycasts = true;
            clearGroup.interactable = true;

            DOTween.Kill(clearRoot);
            DOTween.Kill(clearGroup);
            clearRoot.localScale = Vector3.one * 1.03f;
            clearGroup.alpha = 0f;

            Sequence sequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetTarget(clearRoot);
            sequence.Append(DOTween.To(() => clearGroup.alpha, value => clearGroup.alpha = value, 1f, 0.24f)
                .SetEase(Ease.OutQuad));
            sequence.Join(DOTween.To(() => clearRoot.localScale, value => clearRoot.localScale = value, Vector3.one, 0.34f)
                .SetEase(Ease.OutCubic));

            EventSystem.current?.SetSelectedGameObject(hasNextScene
                ? clearNextButton?.gameObject
                : clearMenuButton?.gameObject);
        }

        private void ContinueAfterArena()
        {
            if (!isLevelComplete)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(levelCompleteNextScene))
            {
                ReturnToMainMenu();
                return;
            }

            PrepareForSceneLoad(false);
            if (sceneTransition == null || !sceneTransition.TryLoadNextScene())
            {
                SceneManager.LoadScene(levelCompleteNextScene);
            }
        }

        private void ReturnToMainMenu()
        {
            if (!isLevelComplete)
            {
                return;
            }

            PrepareForSceneLoad(true);
            SceneManager.LoadScene(MainMenuSceneName);
        }

        private void PrepareForSceneLoad(bool showCursor)
        {
            isLevelComplete = false;
            Time.timeScale = 1f;
            GameAudio.SetGameplayPaused(false);
            Cursor.visible = showCursor || !hideSystemCursor;

            if (clearGroup != null)
            {
                clearGroup.blocksRaycasts = false;
                clearGroup.interactable = false;
            }
        }

        private static string FormatElapsedTime(float seconds)
        {
            int totalSeconds = Mathf.Max(0, Mathf.RoundToInt(seconds));
            return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }
        }

        private static string FormatSceneName(string sceneName)
        {
            const string prefix = "Dustline_Arena_";
            if (!string.IsNullOrWhiteSpace(sceneName) && sceneName.StartsWith(prefix, System.StringComparison.Ordinal))
            {
                return $"ARENA {sceneName.Substring(prefix.Length)}";
            }

            return string.IsNullOrWhiteSpace(sceneName)
                ? "NEXT AREA"
                : sceneName.Replace('_', ' ').ToUpperInvariant();
        }

        private void SetPaused(bool paused)
        {
            if (isPaused == paused || isGameOver || isLevelComplete)
            {
                return;
            }

            isPaused = paused;
            if (paused)
            {
                prePauseTimeScale = Time.timeScale <= 0f ? 1f : Time.timeScale;
                Time.timeScale = 0f;
                GameAudio.SetGameplayPaused(true);
                Cursor.visible = true;
                ShowPauseOverlay();
                return;
            }

            pauseSettingsOpen = false;
            Time.timeScale = prePauseTimeScale <= 0f ? 1f : prePauseTimeScale;
            GameAudio.SetGameplayPaused(false);
            if (hideSystemCursor)
            {
                Cursor.visible = false;
            }

            HidePauseOverlay();
        }

        private void ShowPauseOverlay()
        {
            if (pauseRoot == null || pauseGroup == null)
            {
                return;
            }

            if (pauseTitle != null)
            {
                pauseTitle.text = "PAUSED";
            }

            if (pauseSubtitle != null)
            {
                pauseSubtitle.text = "TAKE A BREATH";
            }

            if (pauseHint != null)
            {
                pauseHint.text = "ESC  RESUME     R  RESTART";
            }

            pauseGroup.blocksRaycasts = true;
            pauseGroup.interactable = true;
            if (hudRaycaster != null)
            {
                hudRaycaster.enabled = true;
            }

            EnsureEventSystem();
            ShowPauseMainMenu(false);

            DOTween.Kill(pauseRoot);
            DOTween.Kill(pauseGroup);
            pauseRoot.localScale = Vector3.one * 1.025f;
            pauseGroup.alpha = 0f;

            if (crosshairGroup != null)
            {
                crosshairGroup.alpha = 0f;
            }

            Sequence sequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetTarget(pauseRoot);
            sequence.Append(DOTween.To(() => pauseGroup.alpha, value => pauseGroup.alpha = value, 1f, 0.16f)
                .SetEase(Ease.OutQuad));
            sequence.Join(DOTween.To(() => pauseRoot.localScale, value => pauseRoot.localScale = value, Vector3.one, 0.22f)
                .SetEase(Ease.OutCubic));

            EventSystem.current?.SetSelectedGameObject(pauseResumeButton?.gameObject);
        }

        private void OpenPauseSettings()
        {
            if (!isPaused || pauseSettingsOpen || pauseMainContent == null || pauseSettingsContent == null)
            {
                return;
            }

            pauseSettingsOpen = true;
            pauseMainContent.gameObject.SetActive(false);
            pauseSettingsContent.gameObject.SetActive(true);
            pauseSettingsContent.localScale = Vector3.one * 0.97f;
            pauseSettingsContent
                .DOScale(Vector3.one, 0.16f)
                .SetUpdate(true)
                .SetEase(Ease.OutCubic);
            EventSystem.current?.SetSelectedGameObject(pauseMasterSlider?.gameObject);
        }

        private void ShowPauseMainMenu(bool playBackSound)
        {
            if (pauseMainContent == null || pauseSettingsContent == null)
            {
                return;
            }

            if (playBackSound)
            {
                GameAudio.PlayUiBack();
            }

            pauseSettingsOpen = false;
            DOTween.Kill(pauseSettingsContent);
            pauseSettingsContent.gameObject.SetActive(false);
            pauseMainContent.gameObject.SetActive(true);
            EventSystem.current?.SetSelectedGameObject(pauseSettingsButton?.gameObject ?? pauseResumeButton?.gameObject);
        }

        private void HidePauseOverlay()
        {
            if (pauseRoot == null || pauseGroup == null)
            {
                return;
            }

            DOTween.Kill(pauseRoot);
            DOTween.Kill(pauseGroup);
            pauseGroup.blocksRaycasts = false;
            pauseGroup.interactable = false;
            if (hudRaycaster != null)
            {
                hudRaycaster.enabled = false;
            }

            if (pauseSettingsContent != null)
            {
                pauseSettingsContent.gameObject.SetActive(false);
            }

            if (pauseMainContent != null)
            {
                pauseMainContent.gameObject.SetActive(true);
            }

            DOTween.To(() => pauseGroup.alpha, value => pauseGroup.alpha = value, 0f, 0.12f)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .SetTarget(pauseGroup);

            if (crosshairGroup != null)
            {
                crosshairGroup.alpha = 0.95f;
            }
        }

        private static void RestartCurrentScene()
        {
            Time.timeScale = 1f;
            GameAudio.SetGameplayPaused(false);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void LoadMainMenu()
        {
            CancelPendingLevelComplete();
            isPaused = false;
            isLevelComplete = false;
            isGameOver = false;
            pauseSettingsOpen = false;
            Time.timeScale = 1f;
            GameAudio.SetGameplayPaused(false);
            Cursor.visible = true;
            SceneManager.LoadScene(MainMenuSceneName);
        }

        private void ShowDeathOverlay()
        {
            if (isGameOver)
            {
                return;
            }

            isGameOver = true;
            prePauseTimeScale = Time.timeScale <= 0f ? 1f : Time.timeScale;
            Time.timeScale = 0f;
            GameAudio.SetGameplayPaused(true);
            Cursor.visible = true;
            if (waveSpawner != null)
            {
                waveSpawner.StopWaves();
            }

            if (deathRoot == null || deathGroup == null)
            {
                return;
            }

            arenaElapsedTime = Mathf.Max(0f, Time.time - arenaStartTime);
            if (deathStats != null)
            {
                int currentWave = waveSpawner == null ? 1 : Mathf.Clamp(waveSpawner.CurrentWaveIndex + 1, 1, Mathf.Max(1, waveSpawner.WaveCount));
                int waveCount = waveSpawner == null ? 1 : Mathf.Max(1, waveSpawner.WaveCount);
                deathStats.text = $"ARENA {arenaIndex:00} / {OperationProgress.TotalArenas:00}   WAVE {currentWave} / {waveCount}   HOSTILES {arenaKills:00}   TIME {FormatElapsedTime(arenaElapsedTime)}";
            }

            deathGroup.blocksRaycasts = true;
            deathGroup.interactable = true;
            if (hudRaycaster != null)
            {
                hudRaycaster.enabled = true;
            }

            if (crosshairGroup != null)
            {
                crosshairGroup.alpha = 0f;
            }

            EnsureEventSystem();

            DOTween.Kill(deathRoot);
            DOTween.Kill(deathGroup);
            deathRoot.localScale = Vector3.one * 1.04f;
            deathGroup.alpha = 0f;

            Sequence sequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetTarget(deathRoot);

            sequence.Append(DOTween.To(() => deathGroup.alpha, value => deathGroup.alpha = value, 1f, 0.28f)
                .SetEase(Ease.OutQuad));
            sequence.Join(DOTween.To(() => deathRoot.localScale, value => deathRoot.localScale = value, Vector3.one, 0.32f)
                .SetEase(Ease.OutCubic));

            PunchText(deathSubtitle, new Vector3(0.06f, 0.06f, 0f), 0.24f);
            EventSystem.current?.SetSelectedGameObject(deathRestartButton?.gameObject);
        }

        private void ShowDamageFlash(float amount)
        {
            if (damageFlash == null)
            {
                return;
            }

            float maximum = health == null ? 100f : Mathf.Max(1f, health.Max);
            float intensity = Mathf.Clamp01(amount / maximum);
            Color flashColor = new Color(1f, 0.06f, 0.02f, Mathf.Lerp(0.10f, 0.28f, intensity));
            if (cameraFollow != null)
            {
                cameraFollow.AddShake(Mathf.Lerp(0.09f, 0.26f, intensity));
            }

            DOTween.Kill(damageFlash);
            damageFlash.color = flashColor;
            DOTween.To(() => damageFlash.color, value => damageFlash.color = value, new Color(flashColor.r, flashColor.g, flashColor.b, 0f), 0.32f)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .SetTarget(damageFlash);
        }

        private void OnReloadStarted()
        {
            RefreshWeapon();
            PunchText(reloadValue, new Vector3(0.08f, 0.08f, 0f), 0.14f);
        }

        private void OnReloadCompleted()
        {
            RefreshWeapon();
            ResetTextScale(magazineValue);
        }

        private void RefreshCrosshair()
        {
            if (crosshairRoot == null)
            {
                return;
            }

            if (targetCamera == null)
            {
                targetCamera = UnityEngine.Camera.main;
            }

            Vector3 screenPosition = Input.mousePosition;
            if (aimController != null && targetCamera != null)
            {
                Vector3 aimScreenPosition = targetCamera.WorldToScreenPoint(aimController.AimPoint);
                if (aimScreenPosition.z > 0f)
                {
                    screenPosition = aimScreenPosition;
                }
            }

            crosshairRoot.position = screenPosition;

            if (crosshairFireBloom > 0.01f)
            {
                float decay = 1f - Mathf.Exp(-crosshairFireBloomDecay * Time.unscaledDeltaTime);
                crosshairFireBloom = Mathf.Lerp(crosshairFireBloom, 0f, decay);
            }
            else
            {
                crosshairFireBloom = 0f;
            }

            bool busy = weapon != null && (weapon.IsReloading || weapon.AmmoInMagazine == 0);
            float targetGap = (busy ? crosshairBusyGap : crosshairBaseGap) + crosshairFireBloom;
            float currentGap = crosshairTop == null ? targetGap : crosshairTop.anchoredPosition.y;
            SetCrosshairGap(Mathf.Lerp(currentGap, targetGap, Time.unscaledDeltaTime * 14f));
            SetCrosshairColor(GetCrosshairColor(busy));
        }

        private void SetCrosshairGap(float gap)
        {
            if (crosshairTop == null)
            {
                return;
            }

            crosshairTop.anchoredPosition = new Vector2(0f, gap);
            crosshairBottom.anchoredPosition = new Vector2(0f, -gap);
            crosshairLeft.anchoredPosition = new Vector2(-gap, 0f);
            crosshairRight.anchoredPosition = new Vector2(gap, 0f);
        }

        private void SetCrosshairColor(Color color)
        {
            SetGraphicColor(crosshairTop, color);
            SetGraphicColor(crosshairBottom, color);
            SetGraphicColor(crosshairLeft, color);
            SetGraphicColor(crosshairRight, color);
            if (crosshairDot != null)
            {
                Color dotColor = IsCrosshairBusy()
                    ? new Color(1f, 0.28f, 0.18f, 1f)
                    : Color.Lerp(DustlineUiTheme.Text, color, 0.35f);
                crosshairDot.color = dotColor;
            }
        }

        private Color GetCrosshairColor(bool busy)
        {
            return busy
                ? new Color(1f, 0.92f, 0.76f, 1f)
                : DustlineUiTheme.Gold;
        }

        private bool IsCrosshairBusy()
        {
            return weapon != null && (weapon.IsReloading || weapon.AmmoInMagazine == 0);
        }

        private static void SetGraphicColor(RectTransform rect, Color color)
        {
            if (rect != null && rect.TryGetComponent(out Graphic graphic))
            {
                graphic.color = color;
            }
        }

        private void PunchText(TMP_Text text, Vector3 punch, float duration)
        {
            if (text == null)
            {
                return;
            }

            text.rectTransform.DOKill();
            text.rectTransform.localScale = Vector3.one;
            text.rectTransform.DOPunchScale(punch, duration, 8, 0.6f)
                .SetUpdate(true)
                .SetTarget(this);
        }

        private static void ResetTextScale(TMP_Text text)
        {
            if (text == null)
            {
                return;
            }

            text.rectTransform.DOKill();
            text.rectTransform.localScale = Vector3.one;
        }

        private void KillHudTweens()
        {
            DOTween.Kill(this);
            if (clearRoot != null)
            {
                DOTween.Kill(clearRoot);
            }

            if (pauseRoot != null)
            {
                DOTween.Kill(pauseRoot);
            }

            if (clearGroup != null)
            {
                DOTween.Kill(clearGroup);
            }

            if (pauseGroup != null)
            {
                DOTween.Kill(pauseGroup);
            }

            if (healthFill != null)
            {
                healthFill.DOKill();
            }
        }
    }
}
