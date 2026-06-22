using System.Collections.Generic;
using UnityEngine;

namespace DustlineArena.Runtime.Weapons
{
    [DisallowMultipleComponent]
    public sealed class ProjectilePool : MonoBehaviour
    {
        private const int DefaultPrewarmCount = 32;
        private static readonly Dictionary<GameObject, ProjectilePool> SharedPools = new Dictionary<GameObject, ProjectilePool>();

        [SerializeField] private GameObject projectilePrefab;
        [SerializeField, Min(0)] private int prewarmCount = DefaultPrewarmCount;

        private readonly Queue<Projectile> available = new Queue<Projectile>();
        private bool prewarmed;

        public static ProjectilePool GetShared(GameObject prefab)
        {
            if (prefab == null)
            {
                return null;
            }

            if (SharedPools.TryGetValue(prefab, out ProjectilePool existing) && existing != null)
            {
                return existing;
            }

            GameObject poolObject = new GameObject($"ProjectilePool_{prefab.name}");
            poolObject.SetActive(false);
            DontDestroyOnLoad(poolObject);
            ProjectilePool pool = poolObject.AddComponent<ProjectilePool>();
            pool.projectilePrefab = prefab;
            poolObject.SetActive(true);
            SharedPools[prefab] = pool;
            return pool;
        }

        private void Awake()
        {
            Prewarm();
        }

        public Projectile Spawn(Vector3 position, Quaternion rotation)
        {
            Prewarm();

            Projectile projectile = available.Count > 0
                ? available.Dequeue()
                : CreateProjectile();

            if (projectile == null)
            {
                return null;
            }

            Transform projectileTransform = projectile.transform;
            projectileTransform.SetPositionAndRotation(position, rotation);
            projectile.gameObject.SetActive(true);
            projectile.OnSpawned(this);
            return projectile;
        }

        public void Despawn(Projectile projectile)
        {
            if (projectile == null)
            {
                return;
            }

            projectile.gameObject.SetActive(false);
            projectile.transform.SetParent(transform, false);
            available.Enqueue(projectile);
        }

        private void Prewarm()
        {
            if (prewarmed)
            {
                return;
            }

            prewarmed = true;
            for (int i = 0; i < prewarmCount; i++)
            {
                Projectile projectile = CreateProjectile();
                if (projectile != null)
                {
                    Despawn(projectile);
                }
            }
        }

        private Projectile CreateProjectile()
        {
            if (projectilePrefab == null)
            {
                return null;
            }

            GameObject instance = Instantiate(projectilePrefab, transform);
            instance.name = projectilePrefab.name;
            instance.SetActive(false);

            if (!instance.TryGetComponent(out Projectile projectile))
            {
                Destroy(instance);
                return null;
            }

            return projectile;
        }
    }
}
