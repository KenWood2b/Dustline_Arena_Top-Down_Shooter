using DustlineArena.Runtime.Common;
using DustlineArena.Runtime.Config;
using DustlineArena.Runtime.Health;
using UnityEngine;

namespace DustlineArena.Runtime.Player
{
    [DisallowMultipleComponent]
    public sealed class MouseAimController : MonoBehaviour
    {
        private const int AimHitBufferSize = 16;

        [SerializeField] private PlayerMovementConfig config;
        [SerializeField] private UnityEngine.Camera targetCamera;
        [SerializeField] private Transform rotatedRoot;
        [SerializeField] private HealthComponent health;
        [SerializeField, Min(0.1f)] private float minAimDistanceFromPlayer = 0.75f;
        [SerializeField, Min(1f)] private float minScreenAimDistance = 6f;
        [SerializeField, Min(10f)] private float aimRaycastDistance = 120f;

        private readonly RaycastHit[] aimHitBuffer = new RaycastHit[AimHitBufferSize];
        public Vector3 AimPoint { get; private set; }
        public Vector3 AimDirection { get; private set; } = Vector3.forward;

        public Vector3 GetAimDirectionFrom(Vector3 origin)
        {
            if (TryGetEnemyAimPointFromWorld(out Vector3 enemyPoint))
            {
                return GetDirectionToPoint(origin, enemyPoint);
            }

            if (TryGetAimPointOnPlane(transform.position.y, out Vector3 point))
            {
                return GetStableAimDirection(origin, point);
            }

            return GetStableAimDirection(origin, AimPoint);
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

            if (direction.sqrMagnitude < minAimDistanceFromPlayer * minAimDistanceFromPlayer
                && !TryGetScreenAimDirection(out direction))
            {
                return;
            }

            AimDirection = direction.normalized;
            Quaternion targetRotation = Quaternion.LookRotation(AimDirection, Vector3.up);
            float rotationSpeed = config == null ? 720f : config.RotationSpeed;
            rotatedRoot.rotation = Quaternion.RotateTowards(rotatedRoot.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        private bool TryGetEnemyAimPointFromWorld(out Vector3 point)
        {
            point = default;
            if (targetCamera == null)
            {
                return false;
            }

            Ray ray = targetCamera.ScreenPointToRay(Input.mousePosition);
            int hitCount = Physics.RaycastNonAlloc(
                ray,
                aimHitBuffer,
                aimRaycastDistance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            RaycastHit closestEnemyHit = default;
            float closestDistance = float.MaxValue;
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = aimHitBuffer[i];
                Collider hitCollider = hit.collider;
                if (hitCollider == null || hitCollider.transform.root == transform.root || hit.distance >= closestDistance)
                {
                    continue;
                }

                TeamMember teamMember = hitCollider.GetComponentInParent<TeamMember>();
                if (teamMember == null || teamMember.Team != TeamId.Enemy)
                {
                    continue;
                }

                closestEnemyHit = hit;
                closestDistance = hit.distance;
            }

            if (closestDistance == float.MaxValue)
            {
                return false;
            }

            point = closestEnemyHit.collider.bounds.center;
            return true;
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

        private Vector3 GetStableAimDirection(Vector3 origin, Vector3 point)
        {
            Vector3 rootPosition = rotatedRoot == null ? transform.position : rotatedRoot.position;
            Vector3 playerToPoint = point - rootPosition;
            playerToPoint.y = 0f;
            if (playerToPoint.sqrMagnitude < minAimDistanceFromPlayer * minAimDistanceFromPlayer)
            {
                if (TryGetScreenAimDirection(out Vector3 screenDirection))
                {
                    return screenDirection;
                }

                return GetCurrentPlanarAimDirection();
            }

            Vector3 direction = point - origin;
            direction.y = 0f;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : GetCurrentPlanarAimDirection();
        }

        private Vector3 GetCurrentPlanarAimDirection()
        {
            Vector3 direction = AimDirection;
            if (direction.sqrMagnitude < 0.0001f && rotatedRoot != null)
            {
                direction = rotatedRoot.forward;
            }

            direction.y = 0f;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
        }

        private Vector3 GetDirectionToPoint(Vector3 origin, Vector3 point)
        {
            Vector3 direction = point - origin;
            direction.y = 0f;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : GetCurrentPlanarAimDirection();
        }

        private bool TryGetScreenAimDirection(out Vector3 direction)
        {
            direction = default;
            if (targetCamera == null)
            {
                return false;
            }

            Vector3 rootPosition = rotatedRoot == null ? transform.position : rotatedRoot.position;
            Vector3 rootScreenPosition = targetCamera.WorldToScreenPoint(rootPosition);
            if (rootScreenPosition.z <= 0f)
            {
                return false;
            }

            Vector2 screenDelta = (Vector2)Input.mousePosition - new Vector2(rootScreenPosition.x, rootScreenPosition.y);
            if (screenDelta.sqrMagnitude < minScreenAimDistance * minScreenAimDistance)
            {
                return false;
            }

            Vector3 screenRight = targetCamera.transform.right;
            Vector3 screenUp = targetCamera.transform.up;
            screenRight.y = 0f;
            screenUp.y = 0f;

            if (screenRight.sqrMagnitude < 0.0001f || screenUp.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            direction = screenRight.normalized * screenDelta.x + screenUp.normalized * screenDelta.y;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            direction.Normalize();
            return true;
        }
    }
}
