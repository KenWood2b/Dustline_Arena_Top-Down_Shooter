using System;
using System.Collections;
using System.Collections.Generic;
using DustlineArena.Runtime.Config;
using DustlineArena.Runtime.Enemies;
using DustlineArena.Runtime.Health;
using DustlineArena.Runtime.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace DustlineArena.Runtime.Spawning
{
    [DisallowMultipleComponent]
    public sealed class WaveSpawner : MonoBehaviour
    {
        [SerializeField] private WaveConfig[] waves;
        [SerializeField] private SpawnPoint[] spawnPoints;
        [SerializeField] private Transform enemyTarget;
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private RuntimeNavMeshBuilder navMeshBuilder;
        [SerializeField, Min(0f)] private float minimumSpawnDistance = 12f;
        [SerializeField] private bool preferOffscreenSpawns = true;
        [SerializeField, Min(10f)] private float lostEnemyDistance = 90f;
        [SerializeField, Min(0f)] private float lostEnemyFloorY = -5f;
        [SerializeField, Min(0.25f)] private float navMeshMissingGraceTime = 2.5f;
        [SerializeField, Min(0f)] private float spawnScatterRadius = 3f;
        [SerializeField, Range(0f, 0.75f)] private float spawnIntervalJitter = 0.35f;
        [SerializeField] private bool scaleDifficultyByArenaIndex = true;
        [SerializeField, Min(0f)] private float enemyCountScalePerArena = 0.12f;
        [SerializeField, Min(0)] private int bonusEnemiesPerArena = 1;
        [SerializeField, Range(0.65f, 1f)] private float spawnIntervalScalePerArena = 0.92f;
        [SerializeField, Min(0.2f)] private float minimumScaledSpawnInterval = 0.32f;

#if UNITY_EDITOR
        [Header("Editor Stress Test")]
        [SerializeField] private bool useEditorStressSettings;
        [SerializeField, Range(1, 500)] private int editorEnemyCount = 64;
        [SerializeField, Min(0f)] private float editorSpawnInterval = 0.05f;
        [SerializeField] private bool editorSuppressEnemyAttacks = true;
#endif

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
        private int arenaDifficultyIndex = 1;

        public event Action<int> WaveStarted;
        public event Action<int> WaveCompleted;
        public event Action<int> EnemyKilled;
        public event Action AllWavesCompleted;

        public bool IsPlaying => isPlaying;
        public int CurrentWaveIndex => waveIndex;
        public int WaveCount => waves == null ? 0 : waves.Length;
        public string CurrentWaveName => GetWaveName(waveIndex);
        public int ActiveEnemyCount => aliveEnemies.Count;
        public int TotalKills { get; private set; }

        private void Awake()
        {
            SanitizeDifficultySettings();
            arenaDifficultyIndex = GetArenaDifficultyIndex(SceneManager.GetActiveScene().name);
        }

        private void Start()
        {
            if (navMeshBuilder == null)
            {
                navMeshBuilder = FindObjectOfType<RuntimeNavMeshBuilder>();
            }

            if (playOnStart)
            {
                StartWaves();
            }
        }

        private void OnDisable()
        {
            StopWaves();
        }

        public void SetTarget(Transform target)
        {
            enemyTarget = target;
        }

        public void SetSpawnPoints(SpawnPoint[] points)
        {
            spawnPoints = points;
            lastSpawnPointIndex = -1;
        }

        public string GetWaveName(int index)
        {
            if (waves == null || index < 0 || index >= waves.Length || waves[index] == null)
            {
                return string.Empty;
            }

            return waves[index].WaveName;
        }

        public bool StartWaves()
        {
            if (IsPlaying)
            {
                return false;
            }

            isPlaying = true;
            waveIndex = 0;
            TotalKills = 0;
            lastSpawnPointIndex = -1;
            playRoutine = StartCoroutine(PlayWaves());
            return true;
        }

        public void StopWaves()
        {
            if (playRoutine != null)
            {
                StopCoroutine(playRoutine);
                playRoutine = null;
            }

            isPlaying = false;
            ReleaseTrackedEnemies();
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
                yield return WaitForNavMesh();

                for (waveIndex = 0; waveIndex < waves.Length; waveIndex++)
                {
                    WaveConfig wave = waves[waveIndex];
                    if (wave == null || !wave.HasEnemyPrefab)
                    {
                        continue;
                    }

                    WaveStarted?.Invoke(waveIndex);
                    yield return new WaitForSeconds(wave.StartDelay);

                    int enemyCount = GetEnemyCount(wave);
                    float spawnInterval = GetSpawnInterval(wave);
                    for (int i = 0; i < enemyCount; i++)
                    {
                        GameObject enemyPrefab = wave.GetEnemyPrefab();
                        if (enemyPrefab == null || !SpawnEnemy(enemyPrefab))
                        {
                            Debug.LogWarning($"Dustline Arena: failed to spawn enemy {i + 1}/{enemyCount} for wave {waveIndex + 1}.", this);
                        }

                        if (i < enemyCount - 1 && spawnInterval > 0f)
                        {
                            float jitter = UnityEngine.Random.Range(1f - spawnIntervalJitter, 1f + spawnIntervalJitter);
                            yield return new WaitForSeconds(spawnInterval * jitter);
                        }
                    }

                    while (aliveEnemies.Count > 0)
                    {
                        RemoveStaleEnemies();
                        yield return null;
                    }

                    WaveCompleted?.Invoke(waveIndex);
                    if (wave.PostWaveDelay > 0f && waveIndex < waves.Length - 1)
                    {
                        yield return new WaitForSeconds(wave.PostWaveDelay);
                    }

                }

                AllWavesCompleted?.Invoke();
            }
            finally
            {
                isPlaying = false;
                playRoutine = null;
            }
        }

        private bool SpawnEnemy(GameObject enemyPrefab)
        {
            if (!TryGetSpawnPose(out Vector3 position, out Quaternion rotation))
            {
                return false;
            }

            EnemyPool pool = EnemyPool.GetShared(enemyPrefab);
            GameObject enemy = pool == null
                ? Instantiate(enemyPrefab, position, rotation)
                : pool.Spawn(position, rotation);
            if (enemy == null)
            {
                return false;
            }

            if (enemy.TryGetComponent(out NavMeshAgent agent) && agent.enabled)
            {
                agent.Warp(position);
            }

            if (enemyTarget != null && enemy.TryGetComponent(out EnemyBrain brain))
            {
                brain.SetTarget(enemyTarget);
#if UNITY_EDITOR
                brain.SuppressAttacks = useEditorStressSettings && editorSuppressEnemyAttacks;
#endif
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

            return true;
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
            bool wasTracked = aliveEnemies.Remove(health);
            if (wasTracked)
            {
                if (countKill)
                {
                    TotalKills++;
                    EnemyKilled?.Invoke(TotalKills);
                }
            }

            if (!wasTracked || !despawn || enemyObject == null)
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

        private void ReleaseTrackedEnemies()
        {
            staleEnemies.Clear();
            foreach (HealthComponent health in aliveEnemies)
            {
                staleEnemies.Add(health);
            }

            foreach (HealthComponent health in staleEnemies)
            {
                RemoveEnemy(health, false, true);
            }

            staleEnemies.Clear();
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

        private bool TryGetSpawnPose(out Vector3 position, out Quaternion rotation)
        {
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                int startIndex = lastSpawnPointIndex < 0
                    ? UnityEngine.Random.Range(0, spawnPoints.Length)
                    : (lastSpawnPointIndex + 1) % spawnPoints.Length;

                for (int i = 0; i < spawnPoints.Length; i++)
                {
                    int index = (startIndex + i) % spawnPoints.Length;
                    SpawnPoint spawnPoint = spawnPoints[index];
                    if (spawnPoint == null)
                    {
                        continue;
                    }

                    Vector3 candidate = FindScatteredNavMeshPosition(spawnPoint.Position);
                    if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 6f, NavMesh.AllAreas)
                        || !IsSpawnPositionUsable(hit.position))
                    {
                        continue;
                    }

                    position = hit.position;
                    rotation = spawnPoint.Rotation;
                    lastSpawnPointIndex = index;
                    return true;
                }
            }

            Vector3 origin = enemyTarget == null ? transform.position : enemyTarget.position;
            for (int i = 0; i < 16; i++)
            {
                Vector2 direction = UnityEngine.Random.insideUnitCircle.normalized;
                if (direction.sqrMagnitude < 0.01f)
                {
                    direction = Vector2.right;
                }

                float distance = Mathf.Max(minimumSpawnDistance, 10f) + UnityEngine.Random.Range(0f, 12f);
                Vector3 candidate = origin + new Vector3(direction.x, 0f, direction.y) * distance;
                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 8f, NavMesh.AllAreas)
                    && IsSpawnPositionUsable(hit.position))
                {
                    position = hit.position;
                    Vector3 lookDirection = origin - position;
                    lookDirection.y = 0f;
                    rotation = lookDirection.sqrMagnitude > 0.001f
                        ? Quaternion.LookRotation(lookDirection.normalized, Vector3.up)
                        : Quaternion.identity;
                    return true;
                }
            }

            position = default;
            rotation = Quaternion.identity;
            return false;
        }

        private bool IsSpawnPositionUsable(Vector3 spawnPosition)
        {
            if (enemyTarget == null)
            {
                return true;
            }

            Vector3 flatOffset = spawnPosition - enemyTarget.position;
            flatOffset.y = 0f;
            if (flatOffset.sqrMagnitude < minimumSpawnDistance * minimumSpawnDistance)
            {
                return false;
            }

            Vector3 destination = enemyTarget.position;
            if (NavMesh.SamplePosition(destination, out NavMeshHit targetHit, 4f, NavMesh.AllAreas))
            {
                destination = targetHit.position;
            }

            NavMeshPath path = new NavMeshPath();
            return NavMesh.CalculatePath(spawnPosition, destination, NavMesh.AllAreas, path)
                && path.status == NavMeshPathStatus.PathComplete;
        }

        private static Vector3 FindNearestNavMeshPosition(Vector3 position)
        {
            return NavMesh.SamplePosition(position, out NavMeshHit hit, 6f, NavMesh.AllAreas)
                ? hit.position
                : position;
        }

        private Vector3 FindScatteredNavMeshPosition(Vector3 center)
        {
            if (spawnScatterRadius <= 0f)
            {
                return FindNearestNavMeshPosition(center);
            }

            for (int attempt = 0; attempt < 10; attempt++)
            {
                Vector2 offset = UnityEngine.Random.insideUnitCircle * spawnScatterRadius;
                Vector3 candidate = center + new Vector3(offset.x, 0f, offset.y);
                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
                {
                    return hit.position;
                }
            }

            return FindNearestNavMeshPosition(center);
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

        private int GetEnemyCount(WaveConfig wave)
        {
#if UNITY_EDITOR
            if (useEditorStressSettings)
            {
                return Mathf.Clamp(editorEnemyCount, 1, 500);
            }
#endif
            int arenaStep = GetArenaDifficultyStep();
            int scaledCount = Mathf.RoundToInt(wave.EnemyCount * (1f + arenaStep * enemyCountScalePerArena));
            return Mathf.Max(1, scaledCount + arenaStep * bonusEnemiesPerArena);
        }

        private float GetSpawnInterval(WaveConfig wave)
        {
#if UNITY_EDITOR
            if (useEditorStressSettings)
            {
                return Mathf.Max(0f, editorSpawnInterval);
            }
#endif
            int arenaStep = GetArenaDifficultyStep();
            float scaledInterval = wave.SpawnInterval * Mathf.Pow(spawnIntervalScalePerArena, arenaStep);
            return Mathf.Max(minimumScaledSpawnInterval, scaledInterval);
        }

        private int GetArenaDifficultyStep()
        {
            return scaleDifficultyByArenaIndex ? Mathf.Max(0, arenaDifficultyIndex - 1) : 0;
        }

        private void SanitizeDifficultySettings()
        {
            enemyCountScalePerArena = Mathf.Max(0f, enemyCountScalePerArena);
            bonusEnemiesPerArena = Mathf.Max(0, bonusEnemiesPerArena);
            spawnIntervalScalePerArena = Mathf.Clamp(spawnIntervalScalePerArena <= 0f ? 0.92f : spawnIntervalScalePerArena, 0.65f, 1f);
            minimumScaledSpawnInterval = Mathf.Max(0.2f, minimumScaledSpawnInterval <= 0f ? 0.32f : minimumScaledSpawnInterval);
        }

        private static int GetArenaDifficultyIndex(string sceneName)
        {
            const string prefix = "Dustline_Arena_";
            if (string.IsNullOrWhiteSpace(sceneName) || !sceneName.StartsWith(prefix, StringComparison.Ordinal))
            {
                return 1;
            }

            string indexText = sceneName.Substring(prefix.Length);
            return int.TryParse(indexText, out int index) ? Mathf.Max(1, index) : 1;
        }

        private IEnumerator WaitForNavMesh()
        {
            if (navMeshBuilder == null)
            {
                yield break;
            }

            float deadline = Time.realtimeSinceStartup + 4f;
            while ((navMeshBuilder.IsBuilding || !navMeshBuilder.HasBuilt)
                && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            if (!navMeshBuilder.HasBuilt)
            {
                navMeshBuilder.Build();
            }
        }
    }
}
