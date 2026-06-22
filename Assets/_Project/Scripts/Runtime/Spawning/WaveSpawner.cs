using System;
using System.Collections;
using System.Collections.Generic;
using DustlineArena.Runtime.Config;
using DustlineArena.Runtime.Enemies;
using DustlineArena.Runtime.Health;
using UnityEngine;
using UnityEngine.AI;

namespace DustlineArena.Runtime.Spawning
{
    [DisallowMultipleComponent]
    public sealed class WaveSpawner : MonoBehaviour
    {
        [SerializeField] private WaveConfig[] waves;
        [SerializeField] private SpawnPoint[] spawnPoints;
        [SerializeField] private Transform enemyTarget;
        [SerializeField] private bool playOnStart = true;
        [SerializeField, Min(0f)] private float minimumSpawnDistance = 12f;
        [SerializeField] private bool preferOffscreenSpawns = true;
        [SerializeField, Min(10f)] private float lostEnemyDistance = 90f;
        [SerializeField, Min(0f)] private float lostEnemyFloorY = -5f;
        [SerializeField, Min(0.25f)] private float navMeshMissingGraceTime = 2.5f;

        private int waveIndex;
        private int lastSpawnPointIndex = -1;
        private Coroutine playRoutine;
        private bool isPlaying;
        private readonly HashSet<HealthComponent> aliveEnemies = new HashSet<HealthComponent>();
        private readonly Dictionary<HealthComponent, Action> deathHandlers = new Dictionary<HealthComponent, Action>();
        private readonly Dictionary<HealthComponent, Transform> enemyTransforms = new Dictionary<HealthComponent, Transform>();
        private readonly Dictionary<HealthComponent, EnemyPool> enemyPools = new Dictionary<HealthComponent, EnemyPool>();
        private readonly Dictionary<HealthComponent, float> navMeshMissingSince = new Dictionary<HealthComponent, float>();
        private readonly List<HealthComponent> staleEnemies = new List<HealthComponent>();
        private readonly List<int> spawnCandidates = new List<int>();
        private UnityEngine.Camera spawnCamera;

        public event Action<int> WaveStarted;
        public event Action<int> WaveCompleted;
        public event Action<int> EnemyKilled;
        public event Action AllWavesCompleted;

        public bool IsPlaying => isPlaying;
        public int CurrentWaveIndex => waveIndex;
        public int TotalKills { get; private set; }

        private void Start()
        {
            if (playOnStart)
            {
                StartWaves();
            }
        }

        private void OnDisable()
        {
            if (playRoutine != null)
            {
                StopCoroutine(playRoutine);
                playRoutine = null;
            }

            isPlaying = false;
            UnsubscribeFromEnemies();
        }

        public void SetTarget(Transform target)
        {
            enemyTarget = target;
        }

        public bool StartWaves()
        {
            if (IsPlaying)
            {
                return false;
            }

            isPlaying = true;
            TotalKills = 0;
            lastSpawnPointIndex = -1;
            playRoutine = StartCoroutine(PlayWaves());
            return true;
        }

        public IEnumerator PlayWaves()
        {
            if (waves == null || waves.Length == 0)
            {
                isPlaying = false;
                playRoutine = null;
                AllWavesCompleted?.Invoke();
                yield break;
            }

            try
            {
                for (waveIndex = 0; waveIndex < waves.Length; waveIndex++)
                {
                    WaveConfig wave = waves[waveIndex];
                    if (wave == null || wave.EnemyPrefab == null)
                    {
                        continue;
                    }

                    WaveStarted?.Invoke(waveIndex);
                    yield return new WaitForSeconds(wave.StartDelay);

                    for (int i = 0; i < wave.EnemyCount; i++)
                    {
                        SpawnEnemy(wave.EnemyPrefab);
                        if (i < wave.EnemyCount - 1 && wave.SpawnInterval > 0f)
                        {
                            yield return new WaitForSeconds(wave.SpawnInterval);
                        }
                    }

                    while (aliveEnemies.Count > 0)
                    {
                        RemoveStaleEnemies();
                        yield return null;
                    }

                    WaveCompleted?.Invoke(waveIndex);
                }

                AllWavesCompleted?.Invoke();
            }
            finally
            {
                isPlaying = false;
                playRoutine = null;
            }
        }

        private void SpawnEnemy(GameObject enemyPrefab)
        {
            SpawnPoint spawnPoint = GetSpawnPoint();
            Vector3 position = spawnPoint == null ? transform.position : spawnPoint.Position;
            Quaternion rotation = spawnPoint == null ? Quaternion.identity : spawnPoint.Rotation;
            position = FindNearestNavMeshPosition(position);

            EnemyPool pool = EnemyPool.GetShared(enemyPrefab);
            GameObject enemy = pool == null
                ? Instantiate(enemyPrefab, position, rotation)
                : pool.Spawn(position, rotation);
            if (enemy == null)
            {
                return;
            }

            if (enemy.TryGetComponent(out NavMeshAgent agent) && agent.enabled && NavMesh.SamplePosition(position, out NavMeshHit hit, 2.5f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }

            if (enemyTarget != null && enemy.TryGetComponent(out EnemyBrain brain))
            {
                brain.SetTarget(enemyTarget);
            }

            HealthComponent health = enemy.GetComponentInParent<HealthComponent>();
            if (health != null && aliveEnemies.Add(health))
            {
                Action deathHandler = () => OnEnemyDied(health);
                deathHandlers.Add(health, deathHandler);
                enemyTransforms[health] = enemy.transform;
                if (pool != null)
                {
                    enemyPools[health] = pool;
                }

                health.Died += deathHandler;
            }
        }

        private void OnEnemyDied(HealthComponent health)
        {
            RemoveEnemy(health, true, true);
        }

        private void RemoveEnemy(HealthComponent health, bool countKill, bool despawn)
        {
            GameObject enemyObject = health == null ? null : health.gameObject;
            EnemyPool pool = null;
            if (!ReferenceEquals(health, null))
            {
                enemyPools.TryGetValue(health, out pool);
            }

            if (health != null && deathHandlers.TryGetValue(health, out Action deathHandler))
            {
                health.Died -= deathHandler;
            }

            deathHandlers.Remove(health);
            enemyTransforms.Remove(health);
            enemyPools.Remove(health);
            navMeshMissingSince.Remove(health);
            if (aliveEnemies.Remove(health))
            {
                if (countKill)
                {
                    TotalKills++;
                    EnemyKilled?.Invoke(TotalKills);
                }
            }

            if (!despawn || enemyObject == null)
            {
                return;
            }

            if (pool != null)
            {
                pool.Despawn(enemyObject);
                return;
            }

            Destroy(enemyObject);
        }

        private void RemoveStaleEnemies()
        {
            staleEnemies.Clear();
            foreach (HealthComponent health in aliveEnemies)
            {
                if (IsEnemyStale(health))
                {
                    staleEnemies.Add(health);
                }
            }

            foreach (HealthComponent health in staleEnemies)
            {
                RemoveEnemy(health, false, true);
            }
        }

        private bool IsEnemyStale(HealthComponent health)
        {
            if (health == null || !health.IsAlive || !health.gameObject.activeInHierarchy)
            {
                return true;
            }

            if (!enemyTransforms.TryGetValue(health, out Transform enemyTransform) || enemyTransform == null)
            {
                return true;
            }

            Vector3 position = enemyTransform.position;
            if (position.y < lostEnemyFloorY)
            {
                return true;
            }

            Vector3 origin = enemyTarget == null ? transform.position : enemyTarget.position;
            Vector3 flatOffset = position - origin;
            flatOffset.y = 0f;
            if (flatOffset.sqrMagnitude > lostEnemyDistance * lostEnemyDistance)
            {
                return true;
            }

            if (health.TryGetComponent(out NavMeshAgent agent) && agent.enabled && !agent.isOnNavMesh)
            {
                if (!navMeshMissingSince.TryGetValue(health, out float missingSince))
                {
                    navMeshMissingSince[health] = Time.time;
                    return false;
                }

                return Time.time - missingSince >= navMeshMissingGraceTime;
            }

            navMeshMissingSince.Remove(health);
            return false;
        }

        private void UnsubscribeFromEnemies()
        {
            foreach (KeyValuePair<HealthComponent, Action> pair in deathHandlers)
            {
                if (pair.Key != null)
                {
                    pair.Key.Died -= pair.Value;
                }
            }

            deathHandlers.Clear();
            aliveEnemies.Clear();
            enemyTransforms.Clear();
            enemyPools.Clear();
            navMeshMissingSince.Clear();
            staleEnemies.Clear();
        }

        private SpawnPoint GetSpawnPoint()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                return null;
            }

            int fallbackIndex = -1;
            float fallbackDistanceSqr = float.MinValue;
            float minimumSpawnDistanceSqr = minimumSpawnDistance * minimumSpawnDistance;
            spawnCandidates.Clear();
            if (spawnCamera == null)
            {
                spawnCamera = UnityEngine.Camera.main;
            }

            for (int i = 0; i < spawnPoints.Length; i++)
            {
                SpawnPoint point = spawnPoints[i];
                if (point == null || i == lastSpawnPointIndex)
                {
                    continue;
                }

                float distanceSqr = enemyTarget == null
                    ? float.MaxValue
                    : (point.Position - enemyTarget.position).sqrMagnitude;
                if (distanceSqr > fallbackDistanceSqr)
                {
                    fallbackDistanceSqr = distanceSqr;
                    fallbackIndex = i;
                }

                if (distanceSqr < minimumSpawnDistanceSqr)
                {
                    continue;
                }

                if (preferOffscreenSpawns && spawnCamera != null && IsVisible(spawnCamera, point.Position))
                {
                    continue;
                }

                spawnCandidates.Add(i);
            }

            int selectedIndex = spawnCandidates.Count > 0
                ? spawnCandidates[UnityEngine.Random.Range(0, spawnCandidates.Count)]
                : fallbackIndex;
            if (selectedIndex < 0)
            {
                selectedIndex = UnityEngine.Random.Range(0, spawnPoints.Length);
            }

            lastSpawnPointIndex = selectedIndex;
            return spawnPoints[selectedIndex];
        }

        private static Vector3 FindNearestNavMeshPosition(Vector3 position)
        {
            return NavMesh.SamplePosition(position, out NavMeshHit hit, 6f, NavMesh.AllAreas)
                ? hit.position
                : position;
        }

        private static bool IsVisible(UnityEngine.Camera camera, Vector3 position)
        {
            Vector3 viewport = camera.WorldToViewportPoint(position);
            return viewport.z > 0f
                && viewport.x > -0.08f
                && viewport.x < 1.08f
                && viewport.y > -0.08f
                && viewport.y < 1.08f;
        }
    }
}
