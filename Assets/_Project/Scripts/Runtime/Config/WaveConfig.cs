using System;
using UnityEngine;

namespace DustlineArena.Runtime.Config
{
    [CreateAssetMenu(menuName = "Dustline Arena/Config/Wave", fileName = "WaveConfig")]
    public sealed class WaveConfig : ScriptableObject
    {
        [Serializable]
        public struct WeightedEnemyPrefab
        {
            [SerializeField] private GameObject prefab;
            [SerializeField, Min(0f)] private float weight;

            public GameObject Prefab => prefab;
            public float Weight => Mathf.Max(0f, weight);
        }

        [SerializeField] private string waveName = "Wave";
        [SerializeField, Min(0)] private int enemyCount = 8;
        [SerializeField, Min(0f)] private float spawnInterval = 0.6f;
        [SerializeField, Min(0f)] private float startDelay = 1f;
        [SerializeField, Min(0f)] private float postWaveDelay = 1.5f;
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private WeightedEnemyPrefab[] enemyPrefabs = Array.Empty<WeightedEnemyPrefab>();

        public string WaveName => string.IsNullOrWhiteSpace(waveName) ? name : waveName;
        public int EnemyCount => enemyCount;
        public float SpawnInterval => spawnInterval;
        public float StartDelay => startDelay;
        public float PostWaveDelay => postWaveDelay;
        public GameObject EnemyPrefab => GetPrimaryEnemyPrefab();
        public bool HasEnemyPrefab => GetPrimaryEnemyPrefab() != null;

        public GameObject GetEnemyPrefab()
        {
            if (enemyPrefabs == null || enemyPrefabs.Length == 0)
            {
                return enemyPrefab;
            }

            float totalWeight = 0f;
            for (int i = 0; i < enemyPrefabs.Length; i++)
            {
                if (enemyPrefabs[i].Prefab != null)
                {
                    totalWeight += enemyPrefabs[i].Weight;
                }
            }

            if (totalWeight <= 0f)
            {
                return GetPrimaryEnemyPrefab();
            }

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            float accumulated = 0f;
            for (int i = 0; i < enemyPrefabs.Length; i++)
            {
                WeightedEnemyPrefab option = enemyPrefabs[i];
                if (option.Prefab == null || option.Weight <= 0f)
                {
                    continue;
                }

                accumulated += option.Weight;
                if (roll <= accumulated)
                {
                    return option.Prefab;
                }
            }

            return GetPrimaryEnemyPrefab();
        }

        private GameObject GetPrimaryEnemyPrefab()
        {
            if (enemyPrefab != null)
            {
                return enemyPrefab;
            }

            if (enemyPrefabs == null)
            {
                return null;
            }

            for (int i = 0; i < enemyPrefabs.Length; i++)
            {
                if (enemyPrefabs[i].Prefab != null)
                {
                    return enemyPrefabs[i].Prefab;
                }
            }

            return null;
        }
    }
}
