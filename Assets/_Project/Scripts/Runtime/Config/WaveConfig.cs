using UnityEngine;

namespace DustlineArena.Runtime.Config
{
    [CreateAssetMenu(menuName = "Dustline Arena/Config/Wave", fileName = "WaveConfig")]
    public sealed class WaveConfig : ScriptableObject
    {
        [SerializeField] private string waveName = "Wave";
        [SerializeField, Min(0)] private int enemyCount = 8;
        [SerializeField, Min(0f)] private float spawnInterval = 0.6f;
        [SerializeField, Min(0f)] private float startDelay = 1f;
        [SerializeField, Min(0f)] private float postWaveDelay = 1.5f;
        [SerializeField] private GameObject enemyPrefab;

        public string WaveName => string.IsNullOrWhiteSpace(waveName) ? name : waveName;
        public int EnemyCount => enemyCount;
        public float SpawnInterval => spawnInterval;
        public float StartDelay => startDelay;
        public float PostWaveDelay => postWaveDelay;
        public GameObject EnemyPrefab => enemyPrefab;
    }
}
