using System;
using System.Collections.Generic;
using DustlineArena.Runtime.Navigation;
using DustlineArena.Runtime.Spawning;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DustlineArena.Editor
{
    public static class ArenaSceneSmokeTestUtility
    {
        private const int SceneCount = 5;
        private const string ScenePathFormat = "Assets/_Project/Scenes/Dustline_Arena_{0:00}.unity";

        private static readonly string[] BlockingFragments =
        {
            "Container",
            "Trash",
            "Barrier",
            "Sack",
            "Crate",
            "Barrel",
            "Cliff",
            "Rock",
            "Pipe",
            "BoxScatter",
            "CardboardScatter",
            "BoxStack",
            "Planks",
            "Pallet",
            "Wall_"
        };

        private static readonly string[] PassThroughFragments =
        {
            "RoadPatch",
            "RoadWarning",
            "ParkingPatch",
            "RoadCrossing",
            "RoadSmall",
            "Papers",
            "Grass",
            "Flowers",
            "Bush",
            "Fern",
            "Pebble",
            "DirtScuff",
            "DirtPatch",
            "Cone",
            "RoofLight",
            "WallLight"
        };

        [MenuItem("Dustline Arena/Run Arena Smoke Tests")]
        public static void RunFromMenu()
        {
            RunAllScenes(true);
        }

        public static void RunFromCommandLine()
        {
            bool passed = RunAllScenes(false);
            EditorApplication.Exit(passed ? 0 : 1);
        }

        private static bool RunAllScenes(bool throwOnFailure)
        {
            List<string> failures = new List<string>();
            for (int i = 1; i <= SceneCount; i++)
            {
                ValidateScene(i, failures);
            }

            if (failures.Count == 0)
            {
                Debug.Log("Dustline Arena smoke test passed.");
                return true;
            }

            string message = "Dustline Arena smoke test failed:\n" + string.Join("\n", failures);
            if (throwOnFailure)
            {
                throw new InvalidOperationException(message);
            }

            Debug.LogError(message);
            return false;
        }

        private static void ValidateScene(int sceneIndex, List<string> failures)
        {
            string scenePath = string.Format(ScenePathFormat, sceneIndex);
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            WaveSpawner spawner = Object.FindObjectOfType<WaveSpawner>(true);
            RuntimeNavMeshBuilder navMeshBuilder = Object.FindObjectOfType<RuntimeNavMeshBuilder>(true);
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (spawner == null)
            {
                failures.Add($"{scene.name}: missing WaveSpawner.");
                return;
            }

            if (spawner.WaveCount < 1)
            {
                failures.Add($"{scene.name}: WaveSpawner has no waves.");
            }

            if (player == null)
            {
                failures.Add($"{scene.name}: missing Player-tagged object.");
            }

            ValidateNavMesh(scene, navMeshBuilder, player, failures);
            ValidateColliders(scene, failures);
        }

        private static void ValidateNavMesh(
            Scene scene,
            RuntimeNavMeshBuilder navMeshBuilder,
            GameObject player,
            List<string> failures)
        {
            if (navMeshBuilder == null)
            {
                failures.Add($"{scene.name}: missing RuntimeNavMeshBuilder.");
                return;
            }

            NavMesh.RemoveAllNavMeshData();
            navMeshBuilder.Build();
            if (!navMeshBuilder.HasBuilt)
            {
                failures.Add($"{scene.name}: RuntimeNavMeshBuilder did not build.");
                return;
            }

            if (player == null)
            {
                return;
            }

            if (!NavMesh.SamplePosition(player.transform.position, out NavMeshHit playerHit, 8f, NavMesh.AllAreas))
            {
                failures.Add($"{scene.name}: player is not near NavMesh.");
                return;
            }

            SpawnPoint[] spawnPoints = Object.FindObjectsOfType<SpawnPoint>(true);
            if (spawnPoints.Length < 6)
            {
                failures.Add($"{scene.name}: has only {spawnPoints.Length} spawn points.");
            }

            for (int i = 0; i < spawnPoints.Length; i++)
            {
                SpawnPoint spawnPoint = spawnPoints[i];
                if (spawnPoint == null)
                {
                    continue;
                }

                if (!NavMesh.SamplePosition(spawnPoint.Position, out NavMeshHit spawnHit, 8f, NavMesh.AllAreas))
                {
                    failures.Add($"{scene.name}: {spawnPoint.name} is not near NavMesh.");
                    continue;
                }

                NavMeshPath path = new NavMeshPath();
                if (!NavMesh.CalculatePath(spawnHit.position, playerHit.position, NavMesh.AllAreas, path)
                    || path.status != NavMeshPathStatus.PathComplete)
                {
                    failures.Add($"{scene.name}: {spawnPoint.name} has no complete path to player.");
                }
            }
        }

        private static void ValidateColliders(Scene scene, List<string> failures)
        {
            Transform layoutRoot = FindLayoutRoot();
            if (layoutRoot == null)
            {
                failures.Add($"{scene.name}: missing generated arena layout root.");
                return;
            }

            foreach (Transform section in layoutRoot)
            {
                ValidatePlacedObjects(scene, section, failures);
            }
        }

        private static void ValidatePlacedObjects(Scene scene, Transform section, List<string> failures)
        {
            foreach (Transform child in section)
            {
                string name = child.name;
                Collider[] colliders = child.GetComponentsInChildren<Collider>(true);
                int solidColliderCount = CountSolidColliders(colliders);

                if (ContainsAny(name, BlockingFragments))
                {
                    if (solidColliderCount == 0)
                    {
                        failures.Add($"{scene.name}: {name} should block movement but has no solid collider.");
                        continue;
                    }

                    ValidateColliderFootprint(scene, child.gameObject, failures);
                }
                else if (ContainsAny(name, PassThroughFragments) && solidColliderCount > 0)
                {
                    failures.Add($"{scene.name}: {name} should be pass-through but has {solidColliderCount} solid collider(s).");
                }
            }
        }

        private static void ValidateColliderFootprint(Scene scene, GameObject target, List<string> failures)
        {
            if (!TryGetBounds(target.GetComponentsInChildren<Renderer>(true), out Bounds rendererBounds)
                || !TryGetBounds(target.GetComponentsInChildren<Collider>(true), out Bounds colliderBounds))
            {
                return;
            }

            float rendererFootprint = Mathf.Max(rendererBounds.size.x, rendererBounds.size.z);
            float colliderFootprint = Mathf.Max(colliderBounds.size.x, colliderBounds.size.z);
            if (colliderFootprint > rendererFootprint * 1.35f + 0.35f)
            {
                failures.Add(
                    $"{scene.name}: {target.name} collider footprint {colliderFootprint:0.00} is too large for renderer {rendererFootprint:0.00}.");
            }

            Vector3 centerOffset = colliderBounds.center - rendererBounds.center;
            centerOffset.y = 0f;
            if (centerOffset.magnitude > Mathf.Max(0.65f, rendererFootprint * 0.18f))
            {
                failures.Add(
                    $"{scene.name}: {target.name} collider center is offset {centerOffset.magnitude:0.00} from renderer center.");
            }
        }

        private static Transform FindLayoutRoot()
        {
            GameObject arena = GameObject.Find("Arena");
            if (arena == null)
            {
                return null;
            }

            foreach (Transform child in arena.transform)
            {
                if (child.name.StartsWith("Props_ArenaSceneLayout_", StringComparison.Ordinal))
                {
                    return child;
                }
            }

            return null;
        }

        private static int CountSolidColliders(Collider[] colliders)
        {
            int count = 0;
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null && colliders[i].enabled && !colliders[i].isTrigger)
                {
                    count++;
                }
            }

            return count;
        }

        private static bool TryGetBounds(Renderer[] renderers, out Bounds bounds)
        {
            bounds = default;
            bool hasBounds = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderers[i].bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }

            return hasBounds;
        }

        private static bool TryGetBounds(Collider[] colliders, out Bounds bounds)
        {
            bounds = default;
            bool hasBounds = false;
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] == null || colliders[i].isTrigger)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = colliders[i].bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(colliders[i].bounds);
                }
            }

            return hasBounds;
        }

        private static bool ContainsAny(string value, string[] fragments)
        {
            for (int i = 0; i < fragments.Length; i++)
            {
                if (value.IndexOf(fragments[i], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
