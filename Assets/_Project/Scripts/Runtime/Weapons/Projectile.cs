using DustlineArena.Runtime.Common;
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
        private readonly RaycastHit[] hitBuffer = new RaycastHit[HitBufferSize];

        public void OnSpawned(ProjectilePool sourcePool)
        {
            pool = sourcePool;
            isDespawning = false;
            age = 0f;
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
            if (damageable != null && damageable.IsAlive)
            {
                damageable.TakeDamage(new DamageInfo(damage.Amount, damage.Source, sourceTeam, point, direction));
            }

            Despawn();
        }

        private void Despawn()
        {
            isDespawning = true;
            if (pool != null)
            {
                pool.Despawn(this);
                return;
            }

            Destroy(gameObject);
        }
    }
}
