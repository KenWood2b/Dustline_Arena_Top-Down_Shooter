using System;
using System.IO;
using DustlineArena.Runtime.Config;
using DustlineArena.Runtime.Navigation;
using DustlineArena.Runtime.Pickups;
using DustlineArena.Runtime.Spawning;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DustlineArena.Editor
{
    internal static class MinimalArenaMapBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Dustline_Arena_01.unity";
        private const string EnvironmentPath = "Assets/_Project/Art/ThirdParty/TopDownShooterKit/Environment";
        private const string PreviewPath = "Assets/_Project/Diagnostics/MinimalArenaPreview.png";
        private const string MapRootName = "Props_ExpandedArena_v2";
        private const float ArenaSize = 56f;
        private const float ArenaHalfSize = ArenaSize * 0.5f;

        public static void BuildFromCommandLine()
        {
            BuildMap(true);
        }

        [MenuItem("Dustline Arena/Rebuild Expanded Arena Map")]
        private static void RebuildFromMenu()
        {
            BuildMap(true);
        }

        private static void BuildMap(bool force)
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject arena = GameObject.Find("Arena");
            if (arena == null)
            {
                Debug.LogError("Dustline Arena: Arena root was not found.");
                return;
            }

            Transform existingMap = arena.transform.Find(MapRootName);
            if (!force && existingMap != null)
            {
                return;
            }

            RemoveExistingProps(arena.transform);

            GameObject mapRoot = new GameObject(MapRootName);
            mapRoot.transform.SetParent(arena.transform, false);

            Transform structures = CreateGroup(mapRoot.transform, "Structures");
            Transform cover = CreateGroup(mapRoot.transform, "Cover");
            Transform dressing = CreateGroup(mapRoot.transform, "Dressing");
            Transform perimeter = CreateGroup(mapRoot.transform, "Perimeter");

            RemoveLegacyWeaponRing(arena);
            ResizeGround(arena.transform);
            BuildStructures(structures);
            BuildCover(cover);
            BuildDressing(dressing);
            BuildPerimeter(perimeter);
            BuildSpawnAndRewardSystems(arena.transform);
            EnsureRuntimeNavMeshBuilder(arena.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            RenderPreview();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Dustline Arena: expanded arena map rebuilt.");
        }

        private static RuntimeNavMeshBuilder EnsureRuntimeNavMeshBuilder(Transform arena)
        {
            GameObject existing = GameObject.Find("Runtime NavMesh");
            GameObject navMeshObject = existing == null ? new GameObject("Runtime NavMesh") : existing;
            navMeshObject.transform.SetParent(arena, false);
            navMeshObject.transform.localPosition = Vector3.zero;

            RuntimeNavMeshBuilder builder = navMeshObject.GetComponent<RuntimeNavMeshBuilder>();
            if (builder == null)
            {
                builder = navMeshObject.AddComponent<RuntimeNavMeshBuilder>();
            }

            builder.Configure(arena, new Vector3(70f, 12f, 70f));
            return builder;
        }

        private static void BuildStructures(Transform parent)
        {
            Place(parent, "Container_Long_NW", "Container_Long.fbx", new Vector3(-20f, 0f, 16f), 90f);
            Place(parent, "Container_Long_SE", "Container_Long.fbx", new Vector3(20f, 0f, -16f), 90f);
            Place(parent, "Container_Long_NE", "Container_Long.fbx", new Vector3(18f, 0f, 21f), 0f);
            Place(parent, "Container_Long_SW", "Container_Long.fbx", new Vector3(-18f, 0f, -21f), 0f);
            Place(parent, "Container_Small_NE", "Container_Small.fbx", new Vector3(21f, 0f, 8f), 90f);
            Place(parent, "Container_Small_SW", "Container_Small.fbx", new Vector3(-21f, 0f, -8f), 90f);

            Place(parent, "TrashContainer_West", "TrashContainer.fbx", new Vector3(-20f, 0f, 5f), 90f);
            Place(parent, "TrashContainer_East", "TrashContainer.fbx", new Vector3(20f, 0f, -5f), -90f);
        }

        private static void BuildCover(Transform parent)
        {
            Place(parent, "Barrier_North_West", "Barrier_Fixed.fbx", new Vector3(-8f, 0f, 14f), 0f);
            Place(parent, "Barrier_North_East", "Barrier_Large.fbx", new Vector3(8f, 0f, 14f), 180f);
            Place(parent, "Barrier_South_West", "Barrier_Large.fbx", new Vector3(-8f, 0f, -14f), 0f);
            Place(parent, "Barrier_South_East", "Barrier_Fixed.fbx", new Vector3(8f, 0f, -14f), 180f);

            Place(parent, "Barrier_West_North", "Barrier_Large.fbx", new Vector3(-14f, 0f, 8f), 90f);
            Place(parent, "Barrier_West_South", "Barrier_Fixed.fbx", new Vector3(-14f, 0f, -8f), -90f);
            Place(parent, "Barrier_East_North", "Barrier_Fixed.fbx", new Vector3(14f, 0f, 8f), 90f);
            Place(parent, "Barrier_East_South", "Barrier_Large.fbx", new Vector3(14f, 0f, -8f), -90f);

            Place(parent, "SackTrench_NW", "SackTrench.fbx", new Vector3(-10f, 0f, 10f), 45f);
            Place(parent, "SackTrench_NE", "SackTrench.fbx", new Vector3(10f, 0f, 10f), -45f);
            Place(parent, "SackTrench_SW", "SackTrench.fbx", new Vector3(-10f, 0f, -10f), 135f);
            Place(parent, "SackTrench_SE", "SackTrench.fbx", new Vector3(10f, 0f, -10f), -135f);

            Place(parent, "Crate_West", "Crate.fbx", new Vector3(-18f, 0f, 0f), 12f);
            Place(parent, "Crate_East", "Crate.fbx", new Vector3(18f, 0f, 0f), -12f);
            Place(parent, "Crate_North", "Crate.fbx", new Vector3(0f, 0f, 18f), -8f);
            Place(parent, "Crate_South", "Crate.fbx", new Vector3(0f, 0f, -18f), 8f);

            Place(parent, "Barrier_Center_NW", "Barrier_Single.fbx", new Vector3(-5f, 0f, 4f), 35f);
            Place(parent, "Barrier_Center_NE", "Barrier_Single.fbx", new Vector3(5f, 0f, 4f), -35f);
            Place(parent, "Barrier_Center_SW", "Barrier_Single.fbx", new Vector3(-5f, 0f, -4f), 145f);
            Place(parent, "Barrier_Center_SE", "Barrier_Single.fbx", new Vector3(5f, 0f, -4f), -145f);
        }

        private static void BuildDressing(Transform parent)
        {
            Place(parent, "Pallet_NW", "Pallet.fbx", new Vector3(-23f, 0f, 22f), 20f, false);
            Place(parent, "Pallet_SE", "Pallet.fbx", new Vector3(23f, 0f, -22f), -20f, false);
            Place(parent, "Barrel_North", "ExplodingBarrel.fbx", new Vector3(3f, 0f, 22f), 0f);
            Place(parent, "Barrel_South", "ExplodingBarrel.fbx", new Vector3(-3f, 0f, -22f), 0f);

            Place(parent, "Cone_NE_A", "TrafficCone.fbx", new Vector3(23f, 0f, 14f), 0f, false);
            Place(parent, "Cone_NE_B", "TrafficCone.fbx", new Vector3(22f, 0f, 15f), 18f, false);
            Place(parent, "Cone_SW_A", "TrafficCone.fbx", new Vector3(-23f, 0f, -14f), 0f, false);
            Place(parent, "Cone_SW_B", "TrafficCone.fbx", new Vector3(-22f, 0f, -15f), -18f, false);
        }

        private static void ResizeGround(Transform arena)
        {
            Transform ground = arena.Find("Ground");
            if (ground != null)
            {
                ground.localScale = new Vector3(ArenaSize, 0.2f, ArenaSize);
            }
        }

        private static void RemoveLegacyWeaponRing(GameObject arena)
        {
            WeaponPickupRingSpawner legacySpawner = arena.GetComponent<WeaponPickupRingSpawner>();
            if (legacySpawner != null)
            {
                Object.DestroyImmediate(legacySpawner);
            }
        }

        private static void BuildPerimeter(Transform parent)
        {
            const float wallOffset = ArenaHalfSize - 0.5f;
            const float segmentSpacing = 4.2f;
            int segmentCount = Mathf.FloorToInt((ArenaSize - 4f) / segmentSpacing);

            for (int i = 0; i <= segmentCount; i++)
            {
                float coordinate = -ArenaHalfSize + 2f + i * segmentSpacing;
                Place(parent, $"Wall_North_{i:00}", "Barrier_Large.fbx", new Vector3(coordinate, 0f, wallOffset), 0f);
                Place(parent, $"Wall_South_{i:00}", "Barrier_Large.fbx", new Vector3(coordinate, 0f, -wallOffset), 180f);
                Place(parent, $"Wall_East_{i:00}", "Barrier_Large.fbx", new Vector3(wallOffset, 0f, coordinate), 90f);
                Place(parent, $"Wall_West_{i:00}", "Barrier_Large.fbx", new Vector3(-wallOffset, 0f, coordinate), -90f);
            }

            CreateSafetyWall(parent, "SafetyWall_North", new Vector3(0f, 1.5f, ArenaHalfSize), new Vector3(ArenaSize + 2f, 3f, 1f));
            CreateSafetyWall(parent, "SafetyWall_South", new Vector3(0f, 1.5f, -ArenaHalfSize), new Vector3(ArenaSize + 2f, 3f, 1f));
            CreateSafetyWall(parent, "SafetyWall_East", new Vector3(ArenaHalfSize, 1.5f, 0f), new Vector3(1f, 3f, ArenaSize + 2f));
            CreateSafetyWall(parent, "SafetyWall_West", new Vector3(-ArenaHalfSize, 1.5f, 0f), new Vector3(1f, 3f, ArenaSize + 2f));
        }

        private static void CreateSafetyWall(Transform parent, string name, Vector3 position, Vector3 size)
        {
            GameObject wall = new GameObject(name);
            wall.transform.SetParent(parent, false);
            wall.transform.localPosition = position;
            BoxCollider collider = wall.AddComponent<BoxCollider>();
            collider.size = size;
        }

        private static void BuildSpawnAndRewardSystems(Transform arena)
        {
            RemoveRootObject("Spawn Points");
            RemoveRootObject("Kill Reward System");

            GameObject spawnRoot = new GameObject("Spawn Points");
            Vector3[] spawnPositions =
            {
                new Vector3(-23f, 0.35f, -20f),
                new Vector3(0f, 0.35f, -24f),
                new Vector3(23f, 0.35f, -20f),
                new Vector3(24f, 0.35f, -6f),
                new Vector3(24f, 0.35f, 10f),
                new Vector3(18f, 0.35f, 23f),
                new Vector3(0f, 0.35f, 24f),
                new Vector3(-18f, 0.35f, 23f),
                new Vector3(-24f, 0.35f, 10f),
                new Vector3(-24f, 0.35f, -6f)
            };

            SpawnPoint[] spawnPoints = new SpawnPoint[spawnPositions.Length];
            for (int i = 0; i < spawnPositions.Length; i++)
            {
                GameObject point = new GameObject($"Spawn Point {i + 1:00}");
                point.transform.SetParent(spawnRoot.transform, false);
                point.transform.position = spawnPositions[i];
                point.transform.rotation = Quaternion.LookRotation(-spawnPositions[i].normalized, Vector3.up);
                spawnPoints[i] = point.AddComponent<SpawnPoint>();
            }

            WaveSpawner waveSpawner = Object.FindObjectOfType<WaveSpawner>();
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (waveSpawner != null)
            {
                SerializedObject serializedSpawner = new SerializedObject(waveSpawner);
                AssignObjectArray(serializedSpawner.FindProperty("spawnPoints"), spawnPoints);
                serializedSpawner.FindProperty("enemyTarget").objectReferenceValue =
                    player == null ? null : player.transform;
                serializedSpawner.FindProperty("minimumSpawnDistance").floatValue = 15f;
                serializedSpawner.FindProperty("preferOffscreenSpawns").boolValue = true;

                SerializedProperty waves = serializedSpawner.FindProperty("waves");
                Object wave = waves.arraySize > 0
                    ? waves.GetArrayElementAtIndex(0).objectReferenceValue
                    : null;
                if (wave != null)
                {
                    waves.arraySize = 4;
                    for (int i = 0; i < waves.arraySize; i++)
                    {
                        waves.GetArrayElementAtIndex(i).objectReferenceValue = wave;
                    }
                }

                serializedSpawner.ApplyModifiedPropertiesWithoutUndo();
            }

            GameObject rewardSystem = new GameObject("Kill Reward System");
            rewardSystem.transform.SetParent(arena, false);
            Transform rewardPointRoot = CreateGroup(rewardSystem.transform, "Reward Points");
            Vector3[] rewardPositions =
            {
                new Vector3(-8f, 0.05f, 0f),
                new Vector3(8f, 0.05f, 0f),
                new Vector3(0f, 0.05f, 8f),
                new Vector3(0f, 0.05f, -8f)
            };
            Transform[] rewardPoints = new Transform[rewardPositions.Length];
            for (int i = 0; i < rewardPositions.Length; i++)
            {
                rewardPoints[i] = CreateGroup(rewardPointRoot, $"Reward Point {i + 1}");
                rewardPoints[i].localPosition = rewardPositions[i];
            }

            KillRewardSpawner rewardSpawner = rewardSystem.AddComponent<KillRewardSpawner>();
            rewardSpawner.Configure(
                waveSpawner,
                new[] { 8, 20, 32 },
                new[]
                {
                    LoadAsset<WeaponConfig>("Assets/_Project/Configs/SMGWeaponConfig.asset"),
                    LoadAsset<WeaponConfig>("Assets/_Project/Configs/ShotgunWeaponConfig.asset"),
                    LoadAsset<WeaponConfig>("Assets/_Project/Configs/GrenadeWeaponConfig.asset")
                },
                new[]
                {
                    LoadAsset<GameObject>("Assets/_Project/Art/ThirdParty/TopDownShooterKit/Weapons/SMG.fbx"),
                    LoadAsset<GameObject>("Assets/_Project/Art/ThirdParty/TopDownShooterKit/Weapons/Shotgun.fbx"),
                    LoadAsset<GameObject>("Assets/_Project/Art/ThirdParty/TopDownShooterKit/Weapons/Grenade.fbx")
                },
                rewardPoints);
        }

        private static T LoadAsset<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new InvalidOperationException($"Missing asset: {path}");
            }

            return asset;
        }

        private static void AssignObjectArray<T>(SerializedProperty property, T[] objects) where T : Object
        {
            property.arraySize = objects.Length;
            for (int i = 0; i < objects.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = objects[i];
            }
        }

        private static void RemoveRootObject(string name)
        {
            GameObject existing = GameObject.Find(name);
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }
        }

        private static Transform CreateGroup(Transform parent, string name)
        {
            GameObject group = new GameObject(name);
            group.transform.SetParent(parent, false);
            return group.transform;
        }

        private static GameObject Place(
            Transform parent,
            string name,
            string assetName,
            Vector3 position,
            float yaw,
            bool blocking = true)
        {
            string path = $"{EnvironmentPath}/{assetName}";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Missing environment asset: {path}");
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = name;
            instance.transform.localPosition = position;
            instance.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            instance.transform.localScale = Vector3.one;
            NormalizeScaleAndGround(instance, GetTargetFootprint(assetName), parent.TransformPoint(position).y);

            if (blocking)
            {
                AddBoundsCollider(instance);
            }

            return instance;
        }

        private static float GetTargetFootprint(string assetName)
        {
            switch (assetName)
            {
                case "Container_Long.fbx":
                    return 7.5f;
                case "Container_Small.fbx":
                    return 4.5f;
                case "TrashContainer.fbx":
                    return 3f;
                case "Barrier_Fixed.fbx":
                case "Barrier_Large.fbx":
                    return 4f;
                case "Barrier_Single.fbx":
                    return 2.2f;
                case "SackTrench.fbx":
                    return 4.5f;
                case "Crate.fbx":
                    return 1.8f;
                case "Pallet.fbx":
                    return 2.5f;
                case "ExplodingBarrel.fbx":
                    return 1.2f;
                case "TrafficCone.fbx":
                    return 0.65f;
                default:
                    return 1f;
            }
        }

        private static void NormalizeScaleAndGround(GameObject target, float targetFootprint, float floorY)
        {
            if (!TryGetRendererBounds(target, out Bounds bounds))
            {
                return;
            }

            float currentFootprint = Mathf.Max(bounds.size.x, bounds.size.z);
            if (currentFootprint > Mathf.Epsilon)
            {
                float scale = targetFootprint / currentFootprint;
                target.transform.localScale *= scale;
            }

            if (TryGetRendererBounds(target, out bounds))
            {
                target.transform.position += Vector3.up * (floorY - bounds.min.y);
            }
        }

        private static bool TryGetRendererBounds(GameObject target, out Bounds bounds)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                bounds = default;
                return false;
            }

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return true;
        }

        private static void AddBoundsCollider(GameObject target)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return;
            }

            Bounds localBounds = new Bounds(
                target.transform.InverseTransformPoint(renderers[0].bounds.center),
                Vector3.zero);

            foreach (Renderer renderer in renderers)
            {
                Bounds bounds = renderer.bounds;
                Vector3 min = bounds.min;
                Vector3 max = bounds.max;
                for (int x = 0; x <= 1; x++)
                {
                    for (int y = 0; y <= 1; y++)
                    {
                        for (int z = 0; z <= 1; z++)
                        {
                            Vector3 corner = new Vector3(
                                x == 0 ? min.x : max.x,
                                y == 0 ? min.y : max.y,
                                z == 0 ? min.z : max.z);
                            localBounds.Encapsulate(target.transform.InverseTransformPoint(corner));
                        }
                    }
                }
            }

            BoxCollider collider = target.AddComponent<BoxCollider>();
            collider.center = localBounds.center;
            collider.size = localBounds.size;
        }

        private static void RemoveExistingProps(Transform arena)
        {
            for (int i = arena.childCount - 1; i >= 0; i--)
            {
                Transform child = arena.GetChild(i);
                if (child.name == "Props"
                    || child.name.StartsWith("Props_MinimalArena_", StringComparison.Ordinal)
                    || child.name.StartsWith("Props_ExpandedArena_", StringComparison.Ordinal)
                    || child.name == "Kill Reward System")
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }
        }

        private static void RenderPreview()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            const int width = 1280;
            const int height = 720;
            RenderTexture renderTexture = new RenderTexture(width, height, 24);
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;
            Vector3 previousPosition = camera.transform.position;
            Quaternion previousRotation = camera.transform.rotation;

            try
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                Vector3 focusPoint = player == null ? Vector3.zero : player.transform.position;
                camera.transform.position = focusPoint + new Vector3(0f, 42f, -28f);
                camera.transform.rotation = Quaternion.LookRotation(
                    focusPoint - camera.transform.position,
                    Vector3.up);
                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                camera.Render();
                texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                texture.Apply();

                string absolutePath = Path.GetFullPath(PreviewPath);
                Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
                File.WriteAllBytes(absolutePath, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                camera.transform.position = previousPosition;
                camera.transform.rotation = previousRotation;
                RenderTexture.active = previousActive;
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(renderTexture);
            }
        }
    }
}
