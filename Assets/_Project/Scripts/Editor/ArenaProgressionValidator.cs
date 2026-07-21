using System;
using System.Collections.Generic;
using DustlineArena.Runtime.Config;
using DustlineArena.Runtime.Navigation;
using DustlineArena.Runtime.Spawning;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DustlineArena.Editor
{
    public static class ArenaProgressionValidator
    {
        private const int SceneCount = 5;
        private const float TransitionDelay = 2f;
        private const string ScenePathFormat = "Assets/_Project/Scenes/Dustline_Arena_{0:00}.unity";
        private const string SceneNameFormat = "Dustline_Arena_{0:00}";

        [MenuItem("Dustline Arena/Validate Fix Arena Progression")]
        public static void ValidateFixArenaProgression()
        {
            EnsureBuildSettings();

            for (int i = 1; i <= SceneCount; i++)
            {
                ValidateScene(i);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Dustline Arena: arena progression validation complete.");
        }

        public static void ValidateFixArenaProgressionFromCommandLine()
        {
            ValidateFixArenaProgression();
        }

        private static void EnsureBuildSettings()
        {
            List<EditorBuildSettingsScene> orderedScenes = new List<EditorBuildSettingsScene>();
            HashSet<string> arenaScenePaths = new HashSet<string>();
            for (int i = 1; i <= SceneCount; i++)
            {
                string path = GetScenePath(i);
                arenaScenePaths.Add(path);
                orderedScenes.Add(new EditorBuildSettingsScene(path, true));
            }

            EditorBuildSettingsScene[] existingScenes = EditorBuildSettings.scenes;
            for (int i = 0; i < existingScenes.Length; i++)
            {
                EditorBuildSettingsScene scene = existingScenes[i];
                if (!arenaScenePaths.Contains(scene.path))
                {
                    orderedScenes.Add(scene);
                }
            }

            EditorBuildSettings.scenes = orderedScenes.ToArray();
        }

        private static void ValidateScene(int sceneIndex)
        {
            string scenePath = GetScenePath(sceneIndex);
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            if (sceneAsset == null)
            {
                Debug.LogError($"Dustline Arena: missing scene asset {scenePath}.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            WaveSpawner spawner = Object.FindObjectOfType<WaveSpawner>(true);
            if (spawner == null)
            {
                Debug.LogError($"Dustline Arena: {scene.name} has no WaveSpawner.");
                return;
            }

            ValidateWaveSpawner(scene.name, spawner);
            EnsureTransition(sceneIndex, spawner);
            ValidateSceneSupportObjects(scene.name);

            EditorUtility.SetDirty(spawner);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ValidateWaveSpawner(string sceneName, WaveSpawner spawner)
        {
            SerializedObject serializedSpawner = new SerializedObject(spawner);

            if (spawner.WaveCount <= 0)
            {
                AssignObjectArray(serializedSpawner.FindProperty("waves"), new[]
                {
                    LoadAsset<WaveConfig>("Assets/_Project/Configs/Wave_01.asset"),
                    LoadAsset<WaveConfig>("Assets/_Project/Configs/Wave_02.asset"),
                    LoadAsset<WaveConfig>("Assets/_Project/Configs/Wave_03.asset"),
                    LoadAsset<WaveConfig>("Assets/_Project/Configs/Wave_04.asset")
                });
                Debug.LogWarning($"Dustline Arena: {sceneName} WaveSpawner had no waves, reassigned default waves.", spawner);
            }

            SpawnPoint[] spawnPoints = Object.FindObjectsOfType<SpawnPoint>(true);
            if (spawnPoints.Length == 0)
            {
                Debug.LogError($"Dustline Arena: {sceneName} has no SpawnPoint objects.", spawner);
            }
            else if (spawnPoints.Length < 6)
            {
                Debug.LogWarning($"Dustline Arena: {sceneName} has only {spawnPoints.Length} spawn points.", spawner);
            }

            Array.Sort(spawnPoints, (left, right) => string.CompareOrdinal(left.name, right.name));
            AssignObjectArray(serializedSpawner.FindProperty("spawnPoints"), spawnPoints);

            RuntimeNavMeshBuilder navMeshBuilder = Object.FindObjectOfType<RuntimeNavMeshBuilder>(true);
            serializedSpawner.FindProperty("navMeshBuilder").objectReferenceValue = navMeshBuilder;

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            serializedSpawner.FindProperty("enemyTarget").objectReferenceValue =
                player == null ? null : player.transform;
            serializedSpawner.FindProperty("minimumSpawnDistance").floatValue = 15f;
            serializedSpawner.FindProperty("preferOffscreenSpawns").boolValue = true;
            serializedSpawner.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ValidateSceneSupportObjects(string sceneName)
        {
            RuntimeNavMeshBuilder navMeshBuilder = Object.FindObjectOfType<RuntimeNavMeshBuilder>(true);
            if (navMeshBuilder == null)
            {
                Debug.LogError($"Dustline Arena: {sceneName} has no RuntimeNavMeshBuilder.");
            }

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogError($"Dustline Arena: {sceneName} has no active Player-tagged object.");
            }
        }

        private static void EnsureTransition(int sceneIndex, WaveSpawner spawner)
        {
            SceneWaveTransition transition = spawner.GetComponent<SceneWaveTransition>();
            if (transition == null)
            {
                transition = spawner.gameObject.AddComponent<SceneWaveTransition>();
            }

            transition.Configure(spawner, GetNextSceneName(sceneIndex), TransitionDelay);
            EditorUtility.SetDirty(transition);
        }

        private static string GetNextSceneName(int sceneIndex)
        {
            return sceneIndex >= SceneCount ? string.Empty : string.Format(SceneNameFormat, sceneIndex + 1);
        }

        private static string GetScenePath(int sceneIndex)
        {
            return string.Format(ScenePathFormat, sceneIndex);
        }

        private static T LoadAsset<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                Debug.LogError($"Dustline Arena: missing asset {path}.");
            }

            return asset;
        }

        private static void AssignObjectArray<T>(SerializedProperty property, T[] values) where T : UnityEngine.Object
        {
            if (property == null)
            {
                return;
            }

            property.arraySize = values == null ? 0 : values.Length;
            for (int i = 0; i < property.arraySize; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }
    }
}
