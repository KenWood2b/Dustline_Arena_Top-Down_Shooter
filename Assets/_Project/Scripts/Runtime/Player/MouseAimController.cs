using DustlineArena.Runtime.Config;
using DustlineArena.Runtime.Health;
using UnityEngine;

namespace DustlineArena.Runtime.Player
{
    [DisallowMultipleComponent]
    public sealed class MouseAimController : MonoBehaviour
    {
        [SerializeField] private PlayerMovementConfig config;
        [SerializeField] private UnityEngine.Camera targetCamera;
        [SerializeField] private Transform rotatedRoot;
        [SerializeField] private HealthComponent health;

        public Vector3 AimPoint { get; private set; }
        public Vector3 AimDirection { get; private set; } = Vector3.forward;

        public Vector3 GetAimDirectionFrom(Vector3 origin)
        {
            if (targetCamera != null && TryGetAimPointOnPlane(origin.y, out Vector3 point))
            {
                Vector3 direction = point - origin;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.0001f)
                {
                    return direction.normalized;
                }
            }

            Vector3 fallback = AimPoint - origin;
            fallback.y = 0f;
            return fallback.sqrMagnitude > 0.0001f ? fallback.normalized : AimDirection;
        }

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = UnityEngine.Camera.main;
            }

            if (rotatedRoot == null)
            {
                rotatedRoot = transform;
            }

            if (health == null)
            {
                health = GetComponent<HealthComponent>();
            }
        }

        private void Update()
        {
            if (Time.timeScale <= 0f)
            {
                return;
            }

            if (health != null && !health.IsAlive)
            {
                return;
            }

            if (targetCamera == null || rotatedRoot == null)
            {
                return;
            }

            if (!TryGetAimPointOnPlane(transform.position.y, out Vector3 point))
            {
                return;
            }

            AimPoint = point;
            Vector3 direction = AimPoint - rotatedRoot.position;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            AimDirection = direction.normalized;
            Quaternion targetRotation = Quaternion.LookRotation(AimDirection, Vector3.up);
            float rotationSpeed = config == null ? 720f : config.RotationSpeed;
            rotatedRoot.rotation = Quaternion.RotateTowards(rotatedRoot.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        private bool TryGetAimPointOnPlane(float height, out Vector3 point)
        {
            point = default;
            if (targetCamera == null)
            {
                return false;
            }

            Ray ray = targetCamera.ScreenPointToRay(Input.mousePosition);
            Plane aimPlane = new Plane(Vector3.up, new Vector3(0f, height, 0f));
            if (!aimPlane.Raycast(ray, out float distance))
            {
                return false;
            }

            point = ray.GetPoint(distance);
            return true;
        }
    }
}
