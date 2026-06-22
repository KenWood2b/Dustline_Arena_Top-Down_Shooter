using DustlineArena.Runtime.Config;
using DustlineArena.Runtime.Weapons;
using DustlineArena.Runtime.Health;
using UnityEngine;

namespace DustlineArena.Runtime.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerWeaponController : MonoBehaviour
    {
        private const int TrajectoryHitBufferSize = 32;

        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private MouseAimController aimController;
        [SerializeField] private ProjectileWeapon weapon;
        [SerializeField] private HealthComponent health;
        [SerializeField, Min(8)] private int trajectorySegments = 24;
        [SerializeField, Min(0.02f)] private float trajectoryStep = 0.08f;

        private bool chargingGrenade;
        private float grenadeChargeStartedAt;
        private LineRenderer trajectoryRenderer;
        private readonly RaycastHit[] trajectoryHitBuffer = new RaycastHit[TrajectoryHitBufferSize];

        public float GrenadeCharge01 => chargingGrenade ? GetGrenadeCharge() : 0f;

        private void Awake()
        {
            if (inputReader == null)
            {
                inputReader = GetComponent<PlayerInputReader>();
            }

            if (aimController == null)
            {
                aimController = GetComponent<MouseAimController>();
            }

            if (weapon == null)
            {
                weapon = GetComponentInChildren<ProjectileWeapon>();
            }

            if (health == null)
            {
                health = GetComponent<HealthComponent>();
            }
        }

        private void Update()
        {
            if (health != null && !health.IsAlive)
            {
                CancelGrenadeCharge();
                return;
            }

            if (inputReader == null || aimController == null || weapon == null)
            {
                return;
            }

            if (weapon.IsGrenadeEquipped)
            {
                UpdateGrenadeInput();
                return;
            }

            CancelGrenadeCharge();
            bool fireRequested = weapon.Config.Visual == WeaponVisualId.Pistol
                ? inputReader.FirePressed
                : inputReader.FireHeld;
            if (fireRequested)
            {
                weapon.TryFire(aimController.AimDirection);
            }
        }

        private void UpdateGrenadeInput()
        {
            if (inputReader.FirePressed && weapon.CanFire)
            {
                chargingGrenade = true;
                grenadeChargeStartedAt = Time.time;
                EnsureTrajectoryRenderer();
            }

            if (!chargingGrenade)
            {
                SetTrajectoryVisible(false);
                return;
            }

            float power = GetGrenadeCharge();
            DrawGrenadeTrajectory(power);

            if (inputReader.FireReleased)
            {
                weapon.TryFire(aimController.AimDirection, power);
                CancelGrenadeCharge();
            }
        }

        private float GetGrenadeCharge()
        {
            float duration = Mathf.Max(0.05f, weapon.GrenadeChargeDuration);
            return Mathf.Clamp01((Time.time - grenadeChargeStartedAt) / duration);
        }

        private void DrawGrenadeTrajectory(float power)
        {
            EnsureTrajectoryRenderer();
            SetTrajectoryVisible(true);

            Vector3 start = weapon.Muzzle.position;
            Vector3 velocity = weapon.GetGrenadeThrowVelocity(aimController.AimDirection, power);
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
            if (distance <= 0.0001f)
            {
                closestHit = default;
                return false;
            }

            int hitCount = Physics.RaycastNonAlloc(
                start,
                delta / distance,
                trajectoryHitBuffer,
                distance,
                ~0,
                QueryTriggerInteraction.Ignore);
            float closestDistance = float.MaxValue;
            closestHit = default;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = trajectoryHitBuffer[i];
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    continue;
                }

                if (hit.distance >= closestDistance)
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
            trajectoryRenderer.material = new Material(shader)
            {
                color = Color.white
            };
            trajectoryRenderer.enabled = false;
        }

        private void CancelGrenadeCharge()
        {
            chargingGrenade = false;
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
