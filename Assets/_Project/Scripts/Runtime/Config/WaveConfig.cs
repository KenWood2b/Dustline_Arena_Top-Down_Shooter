using UnityEngine;

namespace DustlineArena.Runtime.Config
{
    [CreateAssetMenu(menuName = "Dustline Arena/Config/Wave", fileName = "WaveConfig")]
    public sealed class WaveConfig : ScriptableObject
    {
        [SerializeField, Min(0)] private int enemyCount = 8;
        [SerializeField, Min(0f)] private float spawnInterval = 0.6f;
        [SerializeField, Min(0f)] private float startDelay = 1f;
        [SerializeField] private GameObject enemyPrefab;

        public int EnemyCount => enemyCount;
        public float SpawnInterval => spawnInterval;
        public float StartDelay => startDelay;
        public GameObject EnemyPrefab => enemyPrefab;
    }
}
