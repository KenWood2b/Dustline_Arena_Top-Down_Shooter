using System;
using DustlineArena.Runtime.Common;
using DustlineArena.Runtime.Config;
using DustlineArena.Runtime.Health;
using DustlineArena.Runtime.Weapons;
using UnityEngine;

namespace DustlineArena.Runtime.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerGrenadeController : MonoBehaviour
    {
        private const int TrajectoryHitBufferSize = 32;

        [SerializeField] private GrenadeConfig config;
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private MouseAimController aimController;
        [SerializeField] private ProjectileWeapon weapon;
        [SerializeField] private TeamMember ownerTeam;
        [SerializeField] private HealthComponent health;
        [SerializeField, Min(8)] private int trajectorySegments = 24;
        [SerializeField, Min(0.02f)] private float trajectoryStep = 0.08f;

        private readonly RaycastHit[] trajectoryHitBuffer = new RaycastHit[TrajectoryHitBufferSize];
        private bool charging;
        private float chargeStartedAt;
        private float nextThrowTime;
        private int grenadeCount;
        private LineRenderer trajectoryRenderer;
        private Material trajectoryMaterial;

        public event Action GrenadeCountChanged;
        public event Action Thrown;

        public GrenadeConfig Config => config;
        public int GrenadeCount => grenadeCount;
        public int MaximumGrenades => config == null ? 0 : config.MaximumCount;
        public bool IsCharging => charging;
        public float Charge01 => charging ? GetCharge() : 0f;
        public float Cooldown01 => config == null || config.ThrowCooldown <= 0f
            ? 0f
            : Mathf.Clamp01((nextThrowTime - Time.time) / config.ThrowCooldown);

        private void Awake()
        {
            inputReader = inputReader == null ? GetComponent<PlayerInputReader>() : inputReader;
            aimController = aimController == null ? GetComponent<MouseAimController>() : aimController;
            weapon = weapon == null ? GetComponentInChildren<ProjectileWeapon>() : weapon;
            ownerTeam = ownerTeam == null ? GetComponent<TeamMember>() : ownerTeam;
            health = health == null ? GetComponent<HealthComponent>() : health;
            grenadeCount = config == null ? 0 : config.StartingCount;
        }

        private void OnDisable()
        {
            CancelCharge();
        }

        private void OnDestroy()
        {
            if (trajectoryMaterial != null)
            {
                Destroy(trajectoryMaterial);
            }
        }

        private void Update()
        {
            if (Time.timeScale <= 0f)
            {
                return;
            }

            if (config == null || inputReader == null || aimController == null || weapon == null)
            {
                return;
            }

            if (health != null && !health.IsAlive)
            {
                CancelCharge();
                return;
            }

            if (!charging && inputReader.GrenadePressed && CanStartCharge())
            {
                charging = true;
                chargeStartedAt = Time.time;
                EnsureTrajectoryRenderer();
                weapon.SetVisualSuppressed(true);
            }

            if (!charging)
            {
                SetTrajectoryVisible(false);
                return;
            }

            float power = GetCharge();
            DrawTrajectory(power);
            if (inputReader.GrenadeReleased)
            {
                Throw(power);
                CancelCharge();
            }
        }

        public bool TryAddGrenades(int amount)
        {
            if (config == null || amount <= 0 || grenadeCount >= config.MaximumCount)
            {
                return false;
            }

            grenadeCount = Mathf.Min(config.MaximumCount, grenadeCount + amount);
            GrenadeCountChanged?.Invoke();
            return true;
        }

        private bool CanStartCharge()
        {
            return grenadeCount > 0 && Time.time >= nextThrowTime && config.ThrownPrefab != null;
        }

        private float GetCharge()
        {
            return Mathf.Clamp01((Time.time - chargeStartedAt) / Mathf.Max(0.05f, config.ChargeDuration));
        }

        private Vector3 GetThrowVelocity(Vector3 direction, float power)
        {
            float charge = Mathf.Clamp01(power);
            float forwardSpeed = Mathf.Lerp(config.MinThrowSpeed, config.MaxThrowSpeed, charge);
            float upwardSpeed = Mathf.Lerp(config.MinThrowUpwardSpeed, config.MaxThrowUpwardSpeed, charge);
            return direction.normalized * forwardSpeed + Vector3.up * upwardSpeed;
        }

        private void Throw(float power)
        {
            if (!CanStartCharge())
            {
                return;
            }

            Vector3 origin = weapon.Muzzle.position;
            Vector3 direction = aimController.GetAimDirectionFrom(origin);
            GameObject grenadeRoot = new GameObject("Thrown_Grenade");
            grenadeRoot.transform.SetPositionAndRotation(origin, Quaternion.LookRotation(direction, Vector3.up));

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

            TeamId team = ownerTeam == null ? TeamId.Player : ownerTeam.Team;
            DamageInfo damage = new DamageInfo(config.Damage, gameObject, team, origin, direction);
            GrenadeProjectile grenade = grenadeRoot.AddComponent<GrenadeProjectile>();
            grenade.Initialize(
                damage,
                grenadeBody,
                GetThrowVelocity(direction, power),
                config.FuseTime,
                config.ExplosionRadius,
                config.HitMask);

            grenadeCount--;
            nextThrowTime = Time.time + config.ThrowCooldown;
            GrenadeCountChanged?.Invoke();
            Thrown?.Invoke();
        }

        private void DrawTrajectory(float power)
        {
            EnsureTrajectoryRenderer();
            SetTrajectoryVisible(true);

            Vector3 start = weapon.Muzzle.position;
            Vector3 velocity = GetThrowVelocity(aimController.GetAimDirectionFrom(start), power);
            Vector3 previous = start;
            int pointCount = Mathf.Max(2, trajectorySegments);
            trajectoryRenderer.positionCount = pointCount;
            trajectoryRenderer.SetPosition(0, start);

            for (int i = 1; i < pointCount; i++)
            {
                float time = i * trajectoryStep;
                Vector3 point = start + velocity * time + Physics.gravity * (0.5f * time * time);
                if (TryGetTrajectoryHit(previous, point, out RaycastHit hit))
                {
                    trajectoryRenderer.SetPosition(i, hit.point);
                    trajectoryRenderer.positionCount = i + 1;
                    break;
                }

                trajectoryRenderer.SetPosition(i, point);
                previous = point;
            }
        }

        private bool TryGetTrajectoryHit(Vector3 start, Vector3 end, out RaycastHit closestHit)
        {
            Vector3 delta = end - start;
            float distance = delta.magnitude;
            closestHit = default;
            if (distance <= 0.0001f)
            {
                return false;
            }

            int hitCount = Physics.RaycastNonAlloc(start, delta / distance, trajectoryHitBuffer, distance, ~0, QueryTriggerInteraction.Ignore);
            float closestDistance = float.MaxValue;
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = trajectoryHitBuffer[i];
                if (hit.transform.root == transform.root || hit.distance >= closestDistance)
                {
                    continue;
                }

                closestDistance = hit.distance;
                closestHit = hit;
            }

            return closestDistance < float.MaxValue;
        }

        private void EnsureTrajectoryRenderer()
        {
            if (trajectoryRenderer != null)
            {
                return;
            }

            GameObject trajectoryObject = new GameObject("Grenade_Trajectory");
            trajectoryObject.transform.SetParent(transform, false);
            trajectoryRenderer = trajectoryObject.AddComponent<LineRenderer>();
            trajectoryRenderer.useWorldSpace = true;
            trajectoryRenderer.widthMultiplier = 0.045f;
            trajectoryRenderer.numCapVertices = 4;
            trajectoryRenderer.textureMode = LineTextureMode.Stretch;
            trajectoryRenderer.startColor = new Color(1f, 0.85f, 0.2f, 0.85f);
            trajectoryRenderer.endColor = new Color(1f, 0.35f, 0.04f, 0.25f);

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Sprites/Default")
                ?? Shader.Find("Unlit/Color");
            if (shader != null)
            {
                trajectoryMaterial = new Material(shader) { color = Color.white };
                trajectoryRenderer.sharedMaterial = trajectoryMaterial;
            }

            trajectoryRenderer.enabled = false;
        }

        private void CancelCharge()
        {
            if (charging && weapon != null)
            {
                weapon.SetVisualSuppressed(false);
            }

            charging = false;
            SetTrajectoryVisible(false);
        }

        private void SetTrajectoryVisible(bool visible)
        {
            if (trajectoryRenderer != null)
            {
                trajectoryRenderer.enabled = visible;
            }
        }
    }
}
