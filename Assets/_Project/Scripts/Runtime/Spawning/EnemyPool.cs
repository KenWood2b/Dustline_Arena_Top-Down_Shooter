using System.Collections.Generic;
using DustlineArena.Runtime.Health;
using DustlineArena.Runtime.Pickups;
using UnityEngine;
using UnityEngine.AI;

namespace DustlineArena.Runtime.Spawning
{
    [DisallowMultipleComponent]
    public sealed class EnemyPool : MonoBehaviour
    {
        private const int DefaultPrewarmCount = 0;
        private static readonly Dictionary<GameObject, EnemyPool> SharedPools = new Dictionary<GameObject, EnemyPool>();

        [SerializeField] private GameObject enemyPrefab;
        [SerializeField, Min(0)] private int prewarmCount = DefaultPrewarmCount;

        private readonly Queue<GameObject> available = new Queue<GameObject>();
        private bool prewarmed;

        public static EnemyPool GetShared(GameObject prefab)
        {
            if (prefab == null)
            {
                return null;
            }

            if (SharedPools.TryGetValue(prefab, out EnemyPool existing) && existing != null)
            {
                return existing;
            }

            GameObject poolObject = new GameObject($"EnemyPool_{prefab.name}");
            poolObject.SetActive(false);
            DontDestroyOnLoad(poolObject);
            EnemyPool pool = poolObject.AddComponent<EnemyPool>();
            pool.enemyPrefab = prefab;
            poolObject.SetActive(true);
            SharedPools[prefab] = pool;
            return pool;
        }

        private void Awake()
        {
            Prewarm();
        }

        public GameObject Spawn(Vector3 position, Quaternion rotation)
        {
            Prewarm();

            GameObject enemy = available.Count > 0
                ? available.Dequeue()
                : CreateEnemy(position, rotation);
            if (enemy == null)
            {
                return null;
            }

            Transform enemyTransform = enemy.transform;
            enemyTransform.SetParent(null, true);
            enemyTransform.SetPositionAndRotation(position, rotation);
            enemy.SetActive(true);
            ResetEnemy(enemy, position);
            enemy.SetActive(true);
            return enemy;
        }

        public void Despawn(GameObject enemy)
        {
            if (enemy == null)
            {
                return;
            }

            enemy.SetActive(false);
            enemy.transform.SetParent(transform, false);
            available.Enqueue(enemy);
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
                GameObject enemy = CreateEnemy(transform.position, transform.rotation);
                if (enemy != null)
                {
                    Despawn(enemy);
                }
            }
        }

        private GameObject CreateEnemy(Vector3 position, Quaternion rotation)
        {
            if (enemyPrefab == null)
            {
                return null;
            }

            GameObject instance = Instantiate(enemyPrefab, position, rotation);
            instance.name = enemyPrefab.name;
            instance.SetActive(false);
            if (instance.TryGetComponent(out HealthComponent health))
            {
                health.DestroyOnDeath = false;
            }

            if (!instance.TryGetComponent(out EnemyAmmoDropper _))
            {
                instance.AddComponent<EnemyAmmoDropper>();
            }

            return instance;
        }

        private static void ResetEnemy(GameObject enemy, Vector3 position)
        {
            foreach (Renderer renderer in enemy.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = true;
            }

            foreach (Collider collider in enemy.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = true;
            }

            if (enemy.TryGetComponent(out HealthComponent health))
            {
                health.DestroyOnDeath = false;
                health.ResetHealth();
            }

            if (enemy.TryGetComponent(out Rigidbody body) && !body.isKinematic)
            {
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            if (enemy.TryGetComponent(out NavMeshAgent agent))
            {
                agent.enabled = true;
                if (NavMesh.SamplePosition(position, out NavMeshHit hit, 4f, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);
                }

                if (agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                    agent.ResetPath();
                }
            }

            Animator animator = enemy.GetComponentInChildren<Animator>(true);
            if (animator != null && enemy.activeInHierarchy)
            {
                animator.Rebind();
                animator.Update(0f);
            }
        }
    }
}
