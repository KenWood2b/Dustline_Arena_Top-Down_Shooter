using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DustlineArena.Runtime.Spawning
{
    [DisallowMultipleComponent]
    public sealed class SceneWaveTransition : MonoBehaviour
    {
        private const string ArenaScenePrefix = "Dustline_Arena_";
        private const int LastArenaSceneIndex = 5;

        [SerializeField] private WaveSpawner waveSpawner;
        [SerializeField, Min(0f)] private float loadDelay = 2f;
        [SerializeField] private string nextSceneName;

        private bool loading;

        public string NextSceneName => nextSceneName;

        public void Configure(WaveSpawner spawner, string sceneName, float delay)
        {
            waveSpawner = spawner;
            nextSceneName = sceneName;
            loadDelay = Mathf.Max(0f, delay);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureForLoadedScene()
        {
            WaveSpawner spawner = UnityEngine.Object.FindObjectOfType<WaveSpawner>();
            if (spawner == null || spawner.GetComponent<SceneWaveTransition>() != null)
            {
                return;
            }

            SceneWaveTransition transition = spawner.gameObject.AddComponent<SceneWaveTransition>();
            transition.waveSpawner = spawner;
            transition.nextSceneName = GetConventionNextSceneName(SceneManager.GetActiveScene().name);
        }

        private void Awake()
        {
            waveSpawner = waveSpawner == null ? GetComponent<WaveSpawner>() : waveSpawner;
            if (string.IsNullOrWhiteSpace(nextSceneName))
            {
                nextSceneName = GetConventionNextSceneName(SceneManager.GetActiveScene().name);
            }
        }

        private void OnEnable()
        {
            if (waveSpawner != null)
            {
                waveSpawner.AllWavesCompleted += OnAllWavesCompleted;
            }
        }

        private void OnDisable()
        {
            if (waveSpawner != null)
            {
                waveSpawner.AllWavesCompleted -= OnAllWavesCompleted;
            }
        }

        private void OnAllWavesCompleted()
        {
            if (loading || string.IsNullOrWhiteSpace(nextSceneName))
            {
                return;
            }

            StartCoroutine(LoadNextScene());
        }

        private IEnumerator LoadNextScene()
        {
            loading = true;
            if (loadDelay > 0f)
            {
                yield return new WaitForSeconds(loadDelay);
            }

            SceneManager.LoadScene(nextSceneName);
        }

        private static string GetConventionNextSceneName(string currentSceneName)
        {
            if (string.IsNullOrWhiteSpace(currentSceneName) ||
                !currentSceneName.StartsWith(ArenaScenePrefix, StringComparison.Ordinal))
            {
                return string.Empty;
            }

            string indexText = currentSceneName.Substring(ArenaScenePrefix.Length);
            if (!int.TryParse(indexText, out int currentIndex) ||
                currentIndex < 1 ||
                currentIndex >= LastArenaSceneIndex)
            {
                return string.Empty;
            }

            return $"{ArenaScenePrefix}{currentIndex + 1:00}";
        }
    }
}
