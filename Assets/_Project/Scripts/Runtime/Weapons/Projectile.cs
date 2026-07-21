using DustlineArena.Runtime.Common;
using DustlineArena.Runtime.Feedback;
using UnityEngine;

namespace DustlineArena.Runtime.Weapons
{
    [RequireComponent(typeof(Collider))]
    [DisallowMultipleComponent]
    public sealed class Projectile : MonoBehaviour
    {
        private const int HitBufferSize = 16;
        private const float CastRadius = 0.08f;
        private const float CoverProbeHeight = 0.9f;
        private const float TrailTime = 0.055f;
        private const float TrailStartWidth = 0.09f;
        private const float TrailEndWidth = 0.015f;

        private static Material trailMaterial;
        private DamageInfo damage;
        private float speed;
        private float lifetime;
        private float age;
        private LayerMask hitMask;
        private Vector3 direction;
        private TeamId sourceTeam;
        private Transform sourceRoot;
        private ProjectilePool pool;
        private bool isDespawning;
        private TrailRenderer trail;
        private bool trailEnabled = true;
        private readonly RaycastHit[] hitBuffer = new RaycastHit[HitBufferSize];

        private void Awake()
        {
            EnsureTrail();
        }

        public void OnSpawned(ProjectilePool sourcePool)
        {
            pool = sourcePool;
            isDespawning = false;
            age = 0f;
            trailEnabled = !name.Contains("ShotgunPellet");
            EnsureTrail();
            if (trail != null)
            {
                trail.Clear();
                trail.emitting = trailEnabled;
            }
        }

        public void Initialize(DamageInfo damageInfo, Vector3 projectileDirection, float projectileSpeed, float projectileLifetime, LayerMask projectileHitMask)
        {
            damage = damageInfo;
            sourceTeam = damageInfo.SourceTeam;
            sourceRoot = damageInfo.Source == null ? null : damageInfo.Source.transform.root;
            direction = projectileDirection.normalized;
            speed = projectileSpeed;
            lifetime = projectileLifetime;
            hitMask = projectileHitMask;
            age = 0f;
            isDespawning = false;
        }

        private void Reset()
        {
            Collider projectileCollider = GetComponent<Collider>();
            projectileCollider.isTrigger = true;
        }

        private void Update()
        {
            if (isDespawning)
            {
                return;
            }

            float step = speed * Time.deltaTime;

            Vector3 castBottom = transform.position;
            castBottom.y = Mathf.Max(0.35f, transform.position.y - CoverProbeHeight);
            Vector3 castTop = transform.position + Vector3.up * 0.1f;
            int hitCount = Physics.CapsuleCastNonAlloc(
                castBottom,
                castTop,
                CastRadius,
                direction,
                hitBuffer,
                step,
                hitMask,
                QueryTriggerInteraction.Ignore);

            if (TryGetClosestValidHit(hitCount, out RaycastHit hit))
            {
                Hit(hit.collider, hit.point);
                return;
            }

            transform.position += direction * step;
            age += Time.deltaTime;

            if (age >= lifetime)
            {
                Despawn();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (ShouldIgnore(other))
            {
                return;
            }

            Hit(other, transform.position);
        }

        private bool TryGetClosestValidHit(int hitCount, out RaycastHit closestHit)
        {
            closestHit = default;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = hitBuffer[i];
                if (ShouldIgnore(hit.collider) || hit.distance >= closestDistance)
                {
                    continue;
                }

                closestDistance = hit.distance;
                closestHit = hit;
            }

            return closestDistance < float.MaxValue;
        }

        private bool ShouldIgnore(Collider other)
        {
            if (other == null)
            {
                return true;
            }

            if (sourceRoot != null && other.transform.root == sourceRoot)
            {
                return true;
            }

            TeamMember teamMember = other.GetComponentInParent<TeamMember>();
            return teamMember != null && teamMember.Team == sourceTeam;
        }

        private void Hit(Collider other, Vector3 point)
        {
            if (isDespawning)
            {
                return;
            }

            IDamageable damageable = other.GetComponentInParent<IDamageable>();
            bool hitDamageable = damageable != null && damageable.IsAlive;
            CombatVfx.SpawnImpact(point, direction, hitDamageable);
            if (hitDamageable)
            {
                damageable.TakeDamage(new DamageInfo(damage.Amount, damage.Source, sourceTeam, point, direction));
            }

            Despawn();
        }

        private void Despawn()
        {
            isDespawning = true;
            if (trail != null)
            {
                trail.emitting = false;
                trail.Clear();
            }

            if (pool != null)
            {
                pool.Despawn(this);
                return;
            }

            Destroy(gameObject);
        }

        private void EnsureTrail()
        {
            if (trail != null)
            {
                return;
            }

            trailEnabled = !name.Contains("ShotgunPellet");
            trail = GetComponent<TrailRenderer>();
            if (trail == null)
            {
                trail = gameObject.AddComponent<TrailRenderer>();
            }

            trail.time = trailEnabled ? TrailTime : 0f;
            trail.startWidth = trailEnabled ? TrailStartWidth : 0f;
            trail.endWidth = trailEnabled ? TrailEndWidth : 0f;
            trail.minVertexDistance = 0.02f;
            trail.numCornerVertices = 1;
            trail.numCapVertices = 1;
            trail.alignment = LineAlignment.View;
            trail.textureMode = LineTextureMode.Stretch;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.material = GetTrailMaterial();
            trail.startColor = new Color(1f, 0.86f, 0.28f, 0.95f);
            trail.endColor = new Color(1f, 0.62f, 0.12f, 0f);
            trail.emitting = trailEnabled;
        }

        private static Material GetTrailMaterial()
        {
            if (trailMaterial != null)
            {
                return trailMaterial;
            }

            Shader shader = Shader.Find("Sprites/Default")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Color");
            trailMaterial = new Material(shader)
            {
                name = "M_Runtime_ProjectileTrail",
                hideFlags = HideFlags.HideAndDontSave
            };
            trailMaterial.SetColor("_BaseColor", new Color(1f, 0.78f, 0.2f, 1f));
            trailMaterial.SetColor("_Color", new Color(1f, 0.78f, 0.2f, 1f));
            return trailMaterial;
        }
    }
}
