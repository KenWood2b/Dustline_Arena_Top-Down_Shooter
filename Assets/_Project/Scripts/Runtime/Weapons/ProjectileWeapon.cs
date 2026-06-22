using DustlineArena.Runtime.Common;
using DustlineArena.Runtime.Config;
using DustlineArena.Runtime.Health;
using System;
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
        private Transform rightHandGrip;
        private Transform leftHandGrip;
        private Animator ownerAnimator;
        private Transform animatedRightHand;
        private Transform weaponSocket;
        private int lastPoseRefreshFrame = -1;

        public event Action Fired;

        public WeaponConfig Config => config;
        public Transform Muzzle => muzzle;
        public Transform RightHandGrip => rightHandGrip == null ? transform : rightHandGrip;
        public Transform LeftHandGrip => leftHandGrip == null ? transform : leftHandGrip;
        public Transform OwnerRoot => ownerTeam == null ? transform.root : ownerTeam.transform;
        public bool IsGrenadeEquipped => config != null && config.Visual == WeaponVisualId.Grenade;
        public float GrenadeChargeDuration => config == null ? 1f : config.ThrowChargeDuration;
        public bool UsesLeftHand => config != null && GetVisualProfile().usesLeftHand;
        public bool CanFire =>
            config != null
            && Time.time >= nextFireTime
            && (config.Visual == WeaponVisualId.Grenade
                ? config.ThrownPrefab != null
                : config.ProjectilePrefab != null);

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

            CacheAnimatedRightHand();

            if (normalizePlayerWeaponPose && ownerTeam != null && ownerTeam.Team == TeamId.Player)
            {
                NormalizePlayerWeaponPose();
                EnsurePlayerWeaponAssetModel();
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
            if (ownerHealth != null)
            {
                ownerHealth.Died -= HideOnOwnerDeath;
            }
        }

        public void Equip(WeaponConfig weaponConfig)
        {
            if (weaponConfig == null)
            {
                return;
            }

            config = weaponConfig;
            nextFireTime = 0f;

            if (normalizePlayerWeaponPose && ownerTeam != null && ownerTeam.Team == TeamId.Player)
            {
                NormalizePlayerWeaponPose();
                EnsurePlayerWeaponAssetModel();
            }
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
            return TryFire(direction, 1f);
        }

        public bool TryFire(Vector3 direction, float grenadePower)
        {
            if (!CanFire)
            {
                return false;
            }

            nextFireTime = Time.time + 1f / config.ShotsPerSecond;

            Vector3 normalizedDirection = direction.normalized;
            if (config.Visual == WeaponVisualId.Grenade)
            {
                ThrowGrenade(normalizedDirection, grenadePower);
                Fired?.Invoke();
                return true;
            }

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

            Fired?.Invoke();
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

        public Vector3 GetGrenadeThrowVelocity(Vector3 direction, float power)
        {
            if (!IsGrenadeEquipped)
            {
                return Vector3.zero;
            }

            float charge = Mathf.Clamp01(power);
            float forwardSpeed = Mathf.Lerp(config.MinThrowSpeed, config.MaxThrowSpeed, charge);
            float upwardSpeed = Mathf.Lerp(
                config.MinThrowUpwardSpeed,
                config.MaxThrowUpwardSpeed,
                charge);
            return direction.normalized * forwardSpeed + Vector3.up * upwardSpeed;
        }

        private void ThrowGrenade(Vector3 direction, float power)
        {
            GameObject grenadeRoot = new GameObject("Thrown_Grenade");
            grenadeRoot.transform.position = muzzle.position;
            grenadeRoot.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

            GameObject visual = Instantiate(config.ThrownPrefab, grenadeRoot.transform);
            visual.name = "Grenade_Visual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            visual.transform.localScale = Vector3.one * 38f;

            SphereCollider grenadeCollider = grenadeRoot.AddComponent<SphereCollider>();
            grenadeCollider.radius = 0.22f;

            Rigidbody grenadeBody = grenadeRoot.AddComponent<Rigidbody>();
            grenadeBody.mass = 0.45f;
            grenadeBody.drag = 0.05f;
            grenadeBody.angularDrag = 0.05f;
            grenadeBody.interpolation = RigidbodyInterpolation.Interpolate;
            grenadeBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            TeamId team = ownerTeam == null ? TeamId.Neutral : ownerTeam.Team;
            DamageInfo damage = new DamageInfo(config.Damage, gameObject, team, muzzle.position, direction);
            GrenadeProjectile grenade = grenadeRoot.AddComponent<GrenadeProjectile>();
            grenade.Initialize(
                damage,
                grenadeBody,
                GetGrenadeThrowVelocity(direction, power),
                config.ProjectileLifetime,
                config.ExplosionRadius,
                config.HitMask);
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
                transform.localPosition = GetPlayerWeaponLocalPosition();
                transform.localRotation = Quaternion.Euler(playerWeaponLocalEuler);
                return;
            }

            if (!keepPlayerWeaponLevel || ownerTeam == null)
            {
                transform.localPosition = socketLocalPosition;
                transform.localRotation = Quaternion.Euler(socketLocalEuler);
                return;
            }

            Transform ownerRoot = ownerTeam.transform;
            transform.position = weaponSocket.position
                + ownerRoot.right * socketLocalPosition.x
                + ownerRoot.up * socketLocalPosition.y
                + ownerRoot.forward * socketLocalPosition.z;
            transform.rotation = ownerRoot.rotation * Quaternion.Euler(socketLocalEuler);
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
            SetWeaponVisible(false);
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
    }
}
