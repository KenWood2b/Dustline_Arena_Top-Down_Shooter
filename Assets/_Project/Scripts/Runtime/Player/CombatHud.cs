using DustlineArena.Runtime.Common;
using DustlineArena.Runtime.Audio;
using DustlineArena.Runtime.Health;
using DustlineArena.Runtime.Spawning;
using DustlineArena.Runtime.UI;
using DustlineArena.Runtime.Weapons;
using DustlineArena.Runtime.Camera;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DustlineArena.Runtime.Player
{
    [DisallowMultipleComponent]
    public sealed class CombatHud : MonoBehaviour
    {
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
        private TMP_Text healthDeltaValue;
        private TMP_Text bannerTitle;
        private TMP_Text bannerSubtitle;
        private TMP_Text clearTitle;
        private TMP_Text clearSubtitle;
        private TMP_Text clearCountdown;
        private TMP_Text pauseTitle;
        private TMP_Text pauseSubtitle;
        private TMP_Text pauseHint;
        private TMP_Text deathTitle;
        private TMP_Text deathSubtitle;
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
        private RectTransform clearRoot;
        private RectTransform deathRoot;
        private CanvasGroup crosshairGroup;
        private CanvasGroup hitMarkerGroup;
        private CanvasGroup healthDeltaGroup;
        private CanvasGroup bannerGroup;
        private CanvasGroup pauseGroup;
        private CanvasGroup clearGroup;
        private CanvasGroup deathGroup;
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
        private bool isPaused;
        private float prePauseTimeScale = 1f;
        private float levelCompleteEndTime;
        private string levelCompleteNextScene;
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
            if (isPaused)
            {
                isPaused = false;
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

                return;
            }

            if (isLevelComplete)
            {
                RefreshLevelCompleteOverlay();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SetPaused(!isPaused);
                return;
            }

            if (isPaused)
            {
                if (Input.GetKeyDown(KeyCode.R))
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

            GraphicRaycaster raycaster = canvasObject.GetComponent<GraphicRaycaster>();
            raycaster.enabled = false;

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
                "Label", dodgeRoot, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), new Vector2(128f, 20f), new Vector2(18f, -14f));
            dodgeLabel = DustlineUiTheme.AddText(labelRect, "DODGE  [SPACE]", 12f, DustlineUiTheme.Muted, TextAlignmentOptions.Left, FontStyles.Bold);

            RectTransform valueRect = DustlineUiTheme.CreateRect(
                "Value", dodgeRoot, Vector2.one, Vector2.one, Vector2.one, new Vector2(84f, 24f), new Vector2(-16f, -12f));
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
                "Wave", parent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(390f, 76f), new Vector2(0f, -24f));
            DustlineUiTheme.AddImage(panel, "PanelDark", DustlineUiTheme.Background, true);
            AddAccent(panel);

            RectTransform waveRect = DustlineUiTheme.CreateRect(
                "Wave_Value", panel, Vector2.zero, new Vector2(0.54f, 1f), new Vector2(0f, 0.5f), new Vector2(-18f, -12f), new Vector2(24f, -2f));
            waveValue = DustlineUiTheme.AddText(waveRect, "WAVE 1 / 1", 22f, DustlineUiTheme.Gold, TextAlignmentOptions.Left, FontStyles.Bold);

            RectTransform enemyRect = DustlineUiTheme.CreateRect(
                "Enemy_Value", panel, new Vector2(0.54f, 0f), Vector2.one, new Vector2(1f, 0.5f), new Vector2(-18f, -12f), new Vector2(-24f, -2f));
            enemyValue = DustlineUiTheme.AddText(enemyRect, "0 HOSTILES", 18f, DustlineUiTheme.Text, TextAlignmentOptions.Right, FontStyles.Bold);
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
                new Vector2(520f, 238f),
                Vector2.zero);
            DustlineUiTheme.AddImage(pausePanel, "PanelDark", DustlineUiTheme.Background, true);
            AddAccent(pausePanel);

            RectTransform pauseTitleRect = DustlineUiTheme.CreateRect(
                "Title",
                pausePanel,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(440f, 58f),
                new Vector2(0f, 54f));
            pauseTitle = DustlineUiTheme.AddText(pauseTitleRect, "PAUSED", 42f, DustlineUiTheme.Gold, TextAlignmentOptions.Center, FontStyles.Bold);

            RectTransform pauseSubtitleRect = DustlineUiTheme.CreateRect(
                "Subtitle",
                pausePanel,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(440f, 30f),
                new Vector2(0f, 6f));
            pauseSubtitle = DustlineUiTheme.AddText(pauseSubtitleRect, "TAKE A BREATH", 20f, DustlineUiTheme.Text, TextAlignmentOptions.Center, FontStyles.Bold);

            RectTransform pauseHintRect = DustlineUiTheme.CreateRect(
                "Hint",
                pausePanel,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(440f, 28f),
                new Vector2(0f, -56f));
            pauseHint = DustlineUiTheme.AddText(pauseHintRect, "ESC  RESUME     R  RESTART", 18f, DustlineUiTheme.Muted, TextAlignmentOptions.Center, FontStyles.Bold);

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

            RectTransform clearAccent = DustlineUiTheme.CreateRect(
                "Accent",
                clearRoot,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(360f, 5f),
                new Vector2(0f, 108f));
            DustlineUiTheme.AddImage(clearAccent, "AccentGold", DustlineUiTheme.Gold, true);

            RectTransform clearTitleRect = DustlineUiTheme.CreateRect(
                "Title",
                clearRoot,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(760f, 92f),
                new Vector2(0f, 48f));
            clearTitle = DustlineUiTheme.AddText(clearTitleRect, "AREA CLEARED", 64f, DustlineUiTheme.Gold, TextAlignmentOptions.Center, FontStyles.Bold);

            RectTransform clearSubtitleRect = DustlineUiTheme.CreateRect(
                "Subtitle",
                clearRoot,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(820f, 36f),
                new Vector2(0f, -22f));
            clearSubtitle = DustlineUiTheme.AddText(clearSubtitleRect, "ALL WAVES COMPLETE", 24f, DustlineUiTheme.Text, TextAlignmentOptions.Center, FontStyles.Bold);

            RectTransform clearCountdownRect = DustlineUiTheme.CreateRect(
                "Countdown",
                clearRoot,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(560f, 32f),
                new Vector2(0f, -72f));
            clearCountdown = DustlineUiTheme.AddText(clearCountdownRect, "DEPLOYING...", 22f, DustlineUiTheme.Muted, TextAlignmentOptions.Center, FontStyles.Bold);

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

            RectTransform deathTitleRect = DustlineUiTheme.CreateRect(
                "Title",
                deathRoot,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(520f, 76f),
                new Vector2(0f, 42f));
            deathTitle = DustlineUiTheme.AddText(deathTitleRect, "YOU DIED", 58f, DustlineUiTheme.Danger, TextAlignmentOptions.Center, FontStyles.Bold);

            RectTransform deathSubtitleRect = DustlineUiTheme.CreateRect(
                "Subtitle",
                deathRoot,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(640f, 32f),
                new Vector2(0f, -26f));
            deathSubtitle = DustlineUiTheme.AddText(deathSubtitleRect, "PRESS ENTER TO RESTART", 21f, DustlineUiTheme.Text, TextAlignmentOptions.Center, FontStyles.Bold);
        }

        private static void AddAccent(RectTransform panel)
        {
            RectTransform accent = DustlineUiTheme.CreateRect(
                "Accent", panel, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), new Vector2(-30f, 5f), new Vector2(0f, -3f));
            DustlineUiTheme.AddImage(accent, "AccentGold", DustlineUiTheme.Gold, true);
        }

        private void RefreshAll()
        {
            RefreshHealth();
            RefreshWeapon();
            RefreshGrenades();
            RefreshDodgeCooldown();
            RefreshWave();
        }

        private void OnHealthChanged(HealthChangedArgs args)
        {
            RefreshHealth();
            if (args.Delta < 0f)
            {
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
            RefreshHealth();
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
            string waveName = waveSpawner == null ? string.Empty : waveSpawner.GetWaveName(index);
            ShowBanner($"WAVE {index + 1}", string.IsNullOrWhiteSpace(waveName) ? "SURVIVE" : waveName.ToUpperInvariant());
        }

        private void OnWaveCompleted(int index)
        {
            RefreshWave();
            ShowBanner($"WAVE {index + 1} CLEARED", "BREATHE, THEN MOVE");
        }

        private void OnEnemyKilled(int totalKills)
        {
            RefreshWave();
        }

        private void OnAllWavesCompleted()
        {
            RefreshWave();
            ShowLevelCompleteOverlay();
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
            if (isGameOver || clearRoot == null || clearGroup == null || clearTitle == null || clearSubtitle == null || clearCountdown == null)
            {
                ShowBanner("AREA CLEARED", "ALL WAVES COMPLETE");
                return;
            }

            isLevelComplete = true;
            sceneTransition = sceneTransition == null && waveSpawner != null ? waveSpawner.GetComponent<SceneWaveTransition>() : sceneTransition;
            sceneTransition = sceneTransition == null ? FindObjectOfType<SceneWaveTransition>() : sceneTransition;

            bool hasNextScene = sceneTransition != null && sceneTransition.HasNextScene;
            levelCompleteNextScene = hasNextScene ? sceneTransition.NextSceneName : string.Empty;
            float delay = hasNextScene ? Mathf.Max(0f, sceneTransition.LoadDelay) : 0f;
            levelCompleteEndTime = Time.unscaledTime + delay;

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
            RefreshLevelCompleteOverlay();

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
        }

        private void RefreshLevelCompleteOverlay()
        {
            if (!isLevelComplete || clearCountdown == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(levelCompleteNextScene))
            {
                clearCountdown.text = "PRESS ENTER TO RESTART";
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
                {
                    RestartCurrentScene();
                }

                return;
            }

            float remaining = Mathf.Max(0f, levelCompleteEndTime - Time.unscaledTime);
            clearCountdown.text = remaining > 0.05f
                ? $"DEPLOYING IN {Mathf.CeilToInt(remaining)}"
                : "DEPLOYING...";
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
        }

        private void HidePauseOverlay()
        {
            if (pauseRoot == null || pauseGroup == null)
            {
                return;
            }

            DOTween.Kill(pauseRoot);
            DOTween.Kill(pauseGroup);
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
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void ShowDeathOverlay()
        {
            if (isGameOver)
            {
                return;
            }

            isGameOver = true;
            Cursor.visible = true;
            if (waveSpawner != null)
            {
                waveSpawner.StopWaves();
            }

            if (deathRoot == null || deathGroup == null)
            {
                return;
            }

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
