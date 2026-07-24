using DustlineArena.Runtime.Common;
using DustlineArena.Runtime.Config;
using DustlineArena.Runtime.Audio;
using DustlineArena.Runtime.Feedback;
using DustlineArena.Runtime.Health;
using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DustlineArena.Runtime.Weapons
{
    [DisallowMultipleComponent]
    public sealed class ProjectileWeapon : MonoBehaviour
    {
        private const float PlayerRifleAssetScale = 100f;
        private static readonly string[] WeaponModelNames =
        {
            "AK_Model",
            "SMG_Model",
            "Pistol_Model",
            "Shotgun_Model",
            "Grenade_Model"
        };

        [SerializeField] private WeaponConfig config;
        [SerializeField] private Transform muzzle;
        [SerializeField] private ProjectilePool projectilePool;
        [SerializeField] private TeamMember ownerTeam;
        [SerializeField] private HealthComponent ownerHealth;
        [SerializeField] private bool normalizePlayerWeaponPose = true;
        [SerializeField] private bool attachToWeaponSocket = true;
        [SerializeField] private string weaponSocketName = "hand.r";
        [SerializeField] private bool keepPlayerWeaponLevel = true;
        [SerializeField] private Vector3 socketLocalPosition = new Vector3(0f, 0.01f, 0.1f);
        [SerializeField] private Vector3 socketLocalEuler = Vector3.zero;
        [SerializeField] private bool followAnimatedRightHand;
        [SerializeField] private Vector3 playerWeaponLocalPosition = new Vector3(0.08f, 1.26f, 0.48f);
        [SerializeField] private Vector3 rightHandFollowOffset = new Vector3(0.02f, -0.03f, 0.16f);
        [SerializeField] private Vector3 playerWeaponLocalEuler = Vector3.zero;
        [SerializeField] private Vector3 assetModelLocalPosition = Vector3.zero;
        [SerializeField] private Vector3 assetModelLocalEuler = new Vector3(-90f, -90f, 0f);
        [SerializeField] private Vector3 assetModelLocalScale = Vector3.one * PlayerRifleAssetScale;
        [SerializeField] private Vector3 rightHandGripLocalPosition = new Vector3(0.02f, -0.02f, 0.12f);
        [SerializeField] private Vector3 leftHandGripLocalPosition = new Vector3(0f, 0.02f, 0.44f);
        [SerializeField] private Vector3 muzzleLocalPosition = new Vector3(0f, 0.2f, 1f);

        private float nextFireTime;
        private float reloadCompleteTime;
        private bool isReloading;
        private AmmoState currentAmmo;
        private readonly Dictionary<WeaponConfig, AmmoState> ammoStates = new Dictionary<WeaponConfig, AmmoState>();
        private Transform rightHandGrip;
        private Transform leftHandGrip;
        private Animator ownerAnimator;
        private Transform animatedRightHand;
        private Transform weaponSocket;
        private int lastPoseRefreshFrame = -1;
        private Vector3 recoilLocalOffset;
        private Vector3 recoilLocalEuler;

        public event Action Fired;
        public event Action AmmoChanged;
        public event Action ReloadStarted;
        public event Action ReloadCompleted;
        public static event Action<ProjectileWeapon> AnyFired;

        public WeaponConfig Config => config;
        public Transform Muzzle => muzzle;
        public Transform RightHandGrip => rightHandGrip == null ? transform : rightHandGrip;
        public Transform LeftHandGrip => leftHandGrip == null ? transform : leftHandGrip;
        public Transform OwnerRoot => ownerTeam == null ? transform.root : ownerTeam.transform;
        public bool UsesLeftHand => config != null && GetVisualProfile().usesLeftHand;
        public int AmmoInMagazine => currentAmmo == null ? 0 : currentAmmo.Magazine;
        public int ReserveAmmo => currentAmmo == null ? 0 : currentAmmo.Reserve;
        public int TotalAmmo => AmmoInMagazine + ReserveAmmo;
        public bool IsReloading => isReloading;
        public bool UsesIncrementalReload => config != null && config.Visual == WeaponVisualId.Shotgun;
        public float ReloadProgress01 => !isReloading || config == null
            ? 0f
            : 1f - Mathf.Clamp01((reloadCompleteTime - Time.time) / GetReloadStepDuration());
        public bool CanReload => config != null
            && currentAmmo != null
            && !isReloading
            && currentAmmo.Magazine < config.MagazineSize
            && currentAmmo.Reserve > 0;
        public bool CanFire =>
            config != null
            && currentAmmo != null
            && (!isReloading || UsesIncrementalReload)
            && currentAmmo.Magazine > 0
            && Time.time >= nextFireTime
            && config.ProjectilePrefab != null;

        public void SetVisualSuppressed(bool suppressed)
        {
            if (ownerHealth != null && !ownerHealth.IsAlive)
            {
                SetWeaponVisible(false);
                return;
            }

            SetWeaponVisible(!suppressed);
        }

        private void OnValidate()
        {
            SanitizeSerializedDefaults();
        }

        private void Awake()
        {
            SanitizeSerializedDefaults();

            if (muzzle == null)
            {
                muzzle = transform;
            }

            if (ownerTeam == null)
            {
                ownerTeam = GetComponentInParent<TeamMember>();
            }

            if (ownerHealth == null)
            {
                ownerHealth = GetComponentInParent<HealthComponent>();
            }

            currentAmmo = GetOrCreateAmmoState(config);

            CacheAnimatedRightHand();

            if (normalizePlayerWeaponPose && ownerTeam != null && ownerTeam.Team == TeamId.Player)
            {
                NormalizePlayerWeaponPose();
                EnsurePlayerWeaponAssetModel();
                SetWeaponVisible(true);
            }
        }

        private void OnEnable()
        {
            if (ownerHealth != null)
            {
                ownerHealth.Died += HideOnOwnerDeath;
            }
        }

        private void OnDisable()
        {
            DOTween.Kill(this);
            recoilLocalOffset = Vector3.zero;
            recoilLocalEuler = Vector3.zero;
            CancelReload();
            if (ownerHealth != null)
            {
                ownerHealth.Died -= HideOnOwnerDeath;
            }
        }

        public void Equip(WeaponConfig weaponConfig)
        {
            if (weaponConfig == null || weaponConfig.Visual == WeaponVisualId.Grenade)
            {
                return;
            }

            if (config == weaponConfig && currentAmmo != null)
            {
                AddAmmo(weaponConfig.AmmoPerPickup);
                return;
            }

            CancelReload();
            config = weaponConfig;
            currentAmmo = GetOrCreateAmmoState(config);
            nextFireTime = 0f;
            AmmoChanged?.Invoke();

            if (normalizePlayerWeaponPose && ownerTeam != null && ownerTeam.Team == TeamId.Player)
            {
                NormalizePlayerWeaponPose();
                EnsurePlayerWeaponAssetModel();
            }
        }

        private void Update()
        {
            if (isReloading && Time.time >= reloadCompleteTime)
            {
                CompleteReloadStep();
            }
        }

        public bool BeginReload()
        {
            if (!CanReload)
            {
                return false;
            }

            isReloading = true;
            reloadCompleteTime = Time.time + GetReloadStepDuration();
            ReloadStarted?.Invoke();
            GameAudio.PlayReloadStart(muzzle == null ? transform.position : muzzle.position);
            return true;
        }

        public bool AddAmmo(int amount)
        {
            if (config == null || currentAmmo == null || amount <= 0 || currentAmmo.Reserve >= config.MaxReserveAmmo)
            {
                return false;
            }

            int previous = currentAmmo.Reserve;
            currentAmmo.Reserve = Mathf.Min(config.MaxReserveAmmo, currentAmmo.Reserve + amount);
            if (currentAmmo.Reserve == previous)
            {
                return false;
            }

            AmmoChanged?.Invoke();
            return true;
        }

        private void LateUpdate()
        {
            if (!normalizePlayerWeaponPose || ownerTeam == null || ownerTeam.Team != TeamId.Player)
            {
                return;
            }

            if (ownerHealth != null && !ownerHealth.IsAlive)
            {
                SetWeaponVisible(false);
                return;
            }

            RefreshPlayerWeaponPose();
        }

        public void RefreshPlayerWeaponPose()
        {
            if (ownerTeam == null || ownerTeam.Team != TeamId.Player)
            {
                return;
            }

            if (lastPoseRefreshFrame == Time.frameCount)
            {
                return;
            }

            lastPoseRefreshFrame = Time.frameCount;
            Transform desiredParent = GetWeaponParent();
            if (transform.parent != desiredParent)
            {
                transform.SetParent(desiredParent, false);
            }

            bool usingSocket = desiredParent != ownerTeam.transform;
            ApplyWeaponRootPose(usingSocket);
            transform.localScale = Vector3.one;
        }

        public bool TryFire(Vector3 direction)
        {
            if (!CanFire)
            {
                return false;
            }

            if (isReloading && UsesIncrementalReload)
            {
                CancelReload();
            }

            nextFireTime = Time.time + 1f / config.ShotsPerSecond;
            currentAmmo.Magazine--;
            AmmoChanged?.Invoke();

            Vector3 normalizedDirection = SanitizeFireDirection(direction);
            TeamId team = ownerTeam == null ? TeamId.Neutral : ownerTeam.Team;
            int projectileCount = Mathf.Max(1, config.ProjectilesPerShot);
            for (int i = 0; i < projectileCount; i++)
            {
                Vector3 fireDirection = ApplySpread(normalizedDirection, config.SpreadAngle);
                Projectile projectile = SpawnProjectile(fireDirection);
                if (projectile == null)
                {
                    continue;
                }

                DamageInfo damage = new DamageInfo(config.Damage, gameObject, team, muzzle.position, fireDirection);
                projectile.Initialize(damage, fireDirection, config.ProjectileSpeed, config.ProjectileLifetime, config.HitMask);
            }

            ShellCasingFx.Spawn(muzzle.position, normalizedDirection, config.Visual);
            CombatVfx.SpawnMuzzleFlash(muzzle, normalizedDirection, config.Visual);
            GameAudio.PlayWeaponFire(config.Visual, muzzle.position);
            PlayRecoilKick();
            Fired?.Invoke();
            AnyFired?.Invoke(this);
            return true;
        }

        private Projectile SpawnProjectile(Vector3 fireDirection)
        {
            Quaternion rotation = Quaternion.LookRotation(fireDirection, Vector3.up);
            ProjectilePool pool = projectilePool != null
                ? projectilePool
                : ProjectilePool.GetShared(config.ProjectilePrefab);
            Projectile projectile = pool == null ? null : pool.Spawn(muzzle.position, rotation);
            if (projectile != null)
            {
                return projectile;
            }

            GameObject projectileInstance = Instantiate(config.ProjectilePrefab, muzzle.position, rotation);
            return projectileInstance.TryGetComponent(out Projectile fallbackProjectile)
                ? fallbackProjectile
                : null;
        }

        private AmmoState GetOrCreateAmmoState(WeaponConfig weaponConfig)
        {
            if (weaponConfig == null)
            {
                return null;
            }

            if (ammoStates.TryGetValue(weaponConfig, out AmmoState state))
            {
                return state;
            }

            state = new AmmoState
            {
                Magazine = weaponConfig.MagazineSize,
                Reserve = weaponConfig.StartingReserveAmmo
            };
            ammoStates.Add(weaponConfig, state);
            return state;
        }

        private void CompleteReloadStep()
        {
            if (config == null || currentAmmo == null)
            {
                isReloading = false;
                return;
            }

            int needed = Mathf.Max(0, config.MagazineSize - currentAmmo.Magazine);
            int loaded = UsesIncrementalReload ? Mathf.Min(1, currentAmmo.Reserve, needed) : Mathf.Min(needed, currentAmmo.Reserve);
            currentAmmo.Magazine += loaded;
            currentAmmo.Reserve -= loaded;
            AmmoChanged?.Invoke();

            if (UsesIncrementalReload && currentAmmo.Magazine < config.MagazineSize && currentAmmo.Reserve > 0)
            {
                reloadCompleteTime = Time.time + GetReloadStepDuration();
                return;
            }

            isReloading = false;
            reloadCompleteTime = 0f;
            ReloadCompleted?.Invoke();
            GameAudio.PlayReloadComplete(muzzle == null ? transform.position : muzzle.position);
        }

        private void CancelReload()
        {
            isReloading = false;
            reloadCompleteTime = 0f;
        }

        private float GetReloadStepDuration()
        {
            if (config == null)
            {
                return 0.05f;
            }

            if (!UsesIncrementalReload)
            {
                return config.ReloadDuration;
            }

            return Mathf.Max(0.05f, config.ReloadDuration / Mathf.Max(1, config.MagazineSize));
        }

        private void NormalizePlayerWeaponPose()
        {
            Transform desiredParent = GetWeaponParent();
            transform.SetParent(desiredParent, false);
            bool usingSocket = desiredParent != ownerTeam.transform;
            ApplyWeaponRootPose(usingSocket);
            transform.localScale = Vector3.one;
            EnsureHandGripTargets();

            if (muzzle == null)
            {
                GameObject muzzleObject = new GameObject("FirePoint_Muzzle");
                muzzleObject.transform.SetParent(transform, false);
                muzzle = muzzleObject.transform;
            }

            muzzle.name = "FirePoint_Muzzle";
            muzzle.SetParent(transform, false);
            muzzle.localPosition = muzzleLocalPosition;
            muzzle.localRotation = Quaternion.identity;
            muzzle.localScale = Vector3.one;
        }

        private void ApplyWeaponRootPose(bool usingSocket)
        {
            if (!usingSocket)
            {
                transform.localPosition = GetPlayerWeaponLocalPosition() + recoilLocalOffset;
                transform.localRotation = Quaternion.Euler(playerWeaponLocalEuler) * Quaternion.Euler(recoilLocalEuler);
                return;
            }

            if (!keepPlayerWeaponLevel || ownerTeam == null)
            {
                transform.localPosition = socketLocalPosition + recoilLocalOffset;
                transform.localRotation = Quaternion.Euler(socketLocalEuler) * Quaternion.Euler(recoilLocalEuler);
                return;
            }

            Transform ownerRoot = ownerTeam.transform;
            transform.position = weaponSocket.position
                + ownerRoot.right * (socketLocalPosition.x + recoilLocalOffset.x)
                + ownerRoot.up * (socketLocalPosition.y + recoilLocalOffset.y)
                + ownerRoot.forward * (socketLocalPosition.z + recoilLocalOffset.z);
            transform.rotation = ownerRoot.rotation * Quaternion.Euler(socketLocalEuler) * Quaternion.Euler(recoilLocalEuler);
        }

        private void PlayRecoilKick()
        {
            if (ownerTeam == null || ownerTeam.Team != TeamId.Player)
            {
                return;
            }

            WeaponFeelProfile profile = GetFeelProfile();
            DOTween.Kill(this);

            recoilLocalOffset = new Vector3(
                UnityEngine.Random.Range(-profile.sideKick, profile.sideKick),
                profile.upKick,
                -profile.backKick);
            recoilLocalEuler = new Vector3(
                -profile.pitchKick,
                UnityEngine.Random.Range(-profile.yawKick, profile.yawKick),
                UnityEngine.Random.Range(-profile.rollKick, profile.rollKick));

            RefreshPlayerWeaponPose();

            Sequence sequence = DOTween.Sequence()
                .SetTarget(this);
            sequence.AppendInterval(profile.holdTime);
            sequence.Append(DOTween.To(() => recoilLocalOffset, value => recoilLocalOffset = value, Vector3.zero, profile.returnTime)
                .SetEase(Ease.OutCubic));
            sequence.Join(DOTween.To(() => recoilLocalEuler, value => recoilLocalEuler = value, Vector3.zero, profile.returnTime)
                .SetEase(Ease.OutCubic));
        }

        private WeaponFeelProfile GetFeelProfile()
        {
            if (config == null)
            {
                return WeaponFeelProfile.Default;
            }

            switch (config.Visual)
            {
                case WeaponVisualId.Pistol:
                    return new WeaponFeelProfile(0.055f, 0.012f, 0.01f, 4.5f, 0.45f, 0.9f, 0.015f, 0.105f);
                case WeaponVisualId.SMG:
                    return new WeaponFeelProfile(0.035f, 0.008f, 0.008f, 2.7f, 0.65f, 0.65f, 0.005f, 0.075f);
                case WeaponVisualId.Shotgun:
                    return new WeaponFeelProfile(0.105f, 0.025f, 0.018f, 7.5f, 0.8f, 1.25f, 0.025f, 0.16f);
                default:
                    return new WeaponFeelProfile(0.065f, 0.014f, 0.012f, 4.2f, 0.55f, 0.9f, 0.01f, 0.1f);
            }
        }

        private void EnsurePlayerWeaponAssetModel()
        {
            SanitizeSerializedDefaults();

            if (config == null)
            {
                foreach (string modelName in WeaponModelNames)
                {
                    Transform unequippedModel = transform.Find(modelName);
                    if (unequippedModel != null)
                    {
                        unequippedModel.gameObject.SetActive(false);
                    }
                }

                return;
            }

            WeaponVisualProfile profile = GetVisualProfile();
            for (int i = 0; i < WeaponModelNames.Length; i++)
            {
                Transform model = transform.Find(WeaponModelNames[i]);
                if (model == null)
                {
                    continue;
                }

                bool active = i == (int)profile.visual;
                model.gameObject.SetActive(active);
                if (!active)
                {
                    continue;
                }

                model.localPosition = profile.modelPosition;
                model.localRotation = Quaternion.Euler(profile.modelEuler);
                model.localScale = Vector3.one * profile.modelScale;

                foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.enabled = true;
                }
            }

            muzzleLocalPosition = profile.muzzlePosition;
            leftHandGripLocalPosition = profile.leftHandGrip;
            if (muzzle != null)
            {
                muzzle.localPosition = muzzleLocalPosition;
            }

            if (leftHandGrip != null)
            {
                leftHandGrip.localPosition = leftHandGripLocalPosition;
            }
        }

        private WeaponVisualProfile GetVisualProfile()
        {
            WeaponVisualId visual = config == null ? WeaponVisualId.AK : config.Visual;
            switch (visual)
            {
                case WeaponVisualId.SMG:
                    return new WeaponVisualProfile(
                        visual,
                        Vector3.zero,
                        new Vector3(-90f, -90f, 0f),
                        100f,
                        new Vector3(0f, 0.15f, 0.98f),
                        new Vector3(0f, 0.02f, 0.36f),
                        true);
                case WeaponVisualId.Pistol:
                    return new WeaponVisualProfile(
                        visual,
                        Vector3.zero,
                        new Vector3(-90f, -90f, 0f),
                        100f,
                        new Vector3(0f, 0.16f, 0.66f),
                        Vector3.zero,
                        false);
                case WeaponVisualId.Shotgun:
                    return new WeaponVisualProfile(
                        visual,
                        Vector3.zero,
                        new Vector3(-90f, -90f, 0f),
                        100f,
                        new Vector3(0f, 0.11f, 1.22f),
                        new Vector3(0f, 0.02f, 0.54f),
                        true);
                case WeaponVisualId.Grenade:
                    return new WeaponVisualProfile(
                        visual,
                        new Vector3(0f, 0.02f, 0.08f),
                        new Vector3(-90f, 0f, 0f),
                        20f,
                        new Vector3(0f, 0.08f, 0.18f),
                        Vector3.zero,
                        false);
                default:
                    return new WeaponVisualProfile(
                        WeaponVisualId.AK,
                        Vector3.zero,
                        new Vector3(-90f, -90f, 0f),
                        100f,
                        new Vector3(0f, 0.2f, 1f),
                        new Vector3(0f, 0.02f, 0.44f),
                        true);
            }
        }

        private void HideOnOwnerDeath()
        {
            CancelReload();
            SetWeaponVisible(false);
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
        }

        private void SetWeaponVisible(bool visible)
        {
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = visible;
            }

            foreach (FirePointMarker marker in GetComponentsInChildren<FirePointMarker>(true))
            {
                marker.enabled = visible;
                marker.gameObject.SetActive(visible);
            }
        }

        private Vector3 GetPlayerWeaponLocalPosition()
        {
            if (!followAnimatedRightHand || ownerTeam == null)
            {
                return playerWeaponLocalPosition;
            }

            CacheAnimatedRightHand();
            if (animatedRightHand == null)
            {
                return playerWeaponLocalPosition;
            }

            return ownerTeam.transform.InverseTransformPoint(animatedRightHand.position) + rightHandFollowOffset;
        }

        private Transform GetWeaponParent()
        {
            if (!attachToWeaponSocket || ownerTeam == null)
            {
                return ownerTeam == null ? transform.parent : ownerTeam.transform;
            }

            if (weaponSocket == null)
            {
                weaponSocket = FindDeepChild(ownerTeam.transform, weaponSocketName);
            }

            return weaponSocket == null ? ownerTeam.transform : weaponSocket;
        }

        private void CacheAnimatedRightHand()
        {
            if (animatedRightHand != null || ownerTeam == null)
            {
                return;
            }

            if (ownerAnimator == null)
            {
                ownerAnimator = ownerTeam.GetComponentInChildren<Animator>();
            }

            if (ownerAnimator != null && ownerAnimator.isHuman)
            {
                animatedRightHand = ownerAnimator.GetBoneTransform(HumanBodyBones.RightHand);
            }

            if (animatedRightHand == null)
            {
                animatedRightHand = FindDeepChild(ownerTeam.transform, "Hand_R")
                    ?? FindDeepChild(ownerTeam.transform, "RightHand")
                    ?? FindDeepChild(ownerTeam.transform, "mixamorig:RightHand");
            }
        }

        private void EnsureHandGripTargets()
        {
            rightHandGrip = EnsureChildTransform("RightHandGrip", rightHandGripLocalPosition);
            leftHandGrip = EnsureChildTransform("LeftHandGrip", leftHandGripLocalPosition);
        }

        private Transform EnsureChildTransform(string childName, Vector3 localPosition)
        {
            Transform child = transform.Find(childName);
            if (child == null)
            {
                child = new GameObject(childName).transform;
                child.SetParent(transform, false);
            }

            child.localPosition = localPosition;
            child.localRotation = Quaternion.identity;
            child.localScale = Vector3.one;
            return child;
        }

        private void SanitizeSerializedDefaults()
        {
            if (string.IsNullOrWhiteSpace(weaponSocketName) || weaponSocketName == "handslot.r")
            {
                weaponSocketName = "hand.r";
            }

            if (assetModelLocalEuler == new Vector3(0f, -90f, 0f)
                || assetModelLocalEuler == new Vector3(0f, -90f, -6f)
                || assetModelLocalEuler == new Vector3(0f, 90f, -6f))
            {
                assetModelLocalEuler = new Vector3(-90f, -90f, 0f);
            }

            if (assetModelLocalScale.x < PlayerRifleAssetScale
                || assetModelLocalScale.y < PlayerRifleAssetScale
                || assetModelLocalScale.z < PlayerRifleAssetScale)
            {
                assetModelLocalScale = Vector3.one * PlayerRifleAssetScale;
            }

            if (muzzleLocalPosition == Vector3.zero)
            {
                muzzleLocalPosition = new Vector3(0f, 0.2f, 1f);
            }

            if (leftHandGripLocalPosition == Vector3.zero)
            {
                leftHandGripLocalPosition = new Vector3(0f, 0.02f, 0.44f);
            }

            if (rightHandGripLocalPosition == Vector3.zero)
            {
                rightHandGripLocalPosition = new Vector3(0.02f, -0.02f, 0.12f);
            }
        }

        private static Transform FindDeepChild(Transform root, string childName)
        {
            if (root == null)
            {
                return null;
            }

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == childName)
                {
                    return child;
                }
            }

            return null;
        }

        private static Vector3 ApplySpread(Vector3 direction, float spreadAngle)
        {
            if (spreadAngle <= 0f)
            {
                return direction;
            }

            float yaw = UnityEngine.Random.Range(-spreadAngle, spreadAngle);
            return Quaternion.AngleAxis(yaw, Vector3.up) * direction;
        }

        private Vector3 SanitizeFireDirection(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
            {
                return direction.normalized;
            }

            Vector3 fallback = ownerTeam == null ? transform.forward : ownerTeam.transform.forward;
            fallback.y = 0f;
            return fallback.sqrMagnitude > 0.0001f ? fallback.normalized : Vector3.forward;
        }

        private sealed class AmmoState
        {
            public int Magazine;
            public int Reserve;
        }

        private readonly struct WeaponVisualProfile
        {
            public readonly WeaponVisualId visual;
            public readonly Vector3 modelPosition;
            public readonly Vector3 modelEuler;
            public readonly float modelScale;
            public readonly Vector3 muzzlePosition;
            public readonly Vector3 leftHandGrip;
            public readonly bool usesLeftHand;

            public WeaponVisualProfile(
                WeaponVisualId visual,
                Vector3 modelPosition,
                Vector3 modelEuler,
                float modelScale,
                Vector3 muzzlePosition,
                Vector3 leftHandGrip,
                bool usesLeftHand)
            {
                this.visual = visual;
                this.modelPosition = modelPosition;
                this.modelEuler = modelEuler;
                this.modelScale = modelScale;
                this.muzzlePosition = muzzlePosition;
                this.leftHandGrip = leftHandGrip;
                this.usesLeftHand = usesLeftHand;
            }
        }

        private readonly struct WeaponFeelProfile
        {
            public static readonly WeaponFeelProfile Default = new WeaponFeelProfile(0.055f, 0.012f, 0.01f, 4f, 0.5f, 0.8f, 0.01f, 0.1f);

            public readonly float backKick;
            public readonly float upKick;
            public readonly float sideKick;
            public readonly float pitchKick;
            public readonly float yawKick;
            public readonly float rollKick;
            public readonly float holdTime;
            public readonly float returnTime;

            public WeaponFeelProfile(
                float backKick,
                float upKick,
                float sideKick,
                float pitchKick,
                float yawKick,
                float rollKick,
                float holdTime,
                float returnTime)
            {
                this.backKick = backKick;
                this.upKick = upKick;
                this.sideKick = sideKick;
                this.pitchKick = pitchKick;
                this.yawKick = yawKick;
                this.rollKick = rollKick;
                this.holdTime = holdTime;
                this.returnTime = returnTime;
            }
        }
    }
}
