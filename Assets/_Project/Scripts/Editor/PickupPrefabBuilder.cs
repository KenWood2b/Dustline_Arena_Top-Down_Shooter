using DustlineArena.Runtime.Pickups;
using UnityEditor;
using UnityEngine;

namespace DustlineArena.Editor
{
    public static class PickupPrefabBuilder
    {
        private const string ResourcesFolder = "Assets/_Project/Resources";
        private const string PickupsFolder = ResourcesFolder + "/Pickups";
        private const string HealthKitSource = "Assets/Kabungus/HouseholdItems/Prefabs/HealthKit.prefab";
        private const string AmmoCaseSource = "Assets/Kabungus/HouseholdItems/Prefabs/AmmoCase.prefab";
        private const string AmmoCrateSource = "Assets/Kabungus/HouseholdItems/Prefabs/AmmoCrate.prefab";

        [InitializeOnLoadMethod]
        private static void EnsurePrefabsOnLoad()
        {
            EditorApplication.delayCall += BuildMissingPrefabs;
        }

        [MenuItem("Dustline Arena/Build Pickup Prefabs")]
        public static void BuildAllPrefabs()
        {
            EnsureFolders();
            BuildHealthPickup(force: true);
            BuildAmmoPickup(force: true);
            BuildLargeAmmoCache(force: true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void BuildMissingPrefabs()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            EnsureFolders();
            BuildHealthPickup(force: false);
            BuildAmmoPickup(force: false);
            BuildLargeAmmoCache(force: false);
            AssetDatabase.SaveAssets();
        }

        private static void BuildHealthPickup(bool force)
        {
            GameObject prefab = BuildPickupRoot(
                "HealthPickup",
                HealthKitSource,
                PickupsFolder + "/HealthPickup.prefab",
                new Vector3(0f, 0.42f, 0f),
                new Vector3(1.3f, 0.78f, 0.95f),
                Vector3.zero,
                Vector3.one,
                force);
            if (prefab == null)
            {
                return;
            }

            HealthPickup pickup = prefab.AddComponent<HealthPickup>();
            SerializedObject serialized = new SerializedObject(pickup);
            serialized.FindProperty("healAmount").floatValue = 25f;
            serialized.FindProperty("lifetime").floatValue = 28f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            SavePrefab(prefab, PickupsFolder + "/HealthPickup.prefab");
        }

        private static void BuildAmmoPickup(bool force)
        {
            GameObject prefab = BuildPickupRoot(
                "AmmoPickup",
                AmmoCaseSource,
                PickupsFolder + "/AmmoPickup.prefab",
                new Vector3(0f, 0.42f, 0f),
                new Vector3(1.15f, 0.78f, 0.9f),
                Vector3.zero,
                Vector3.one,
                force);
            if (prefab == null)
            {
                return;
            }

            AmmoPickup pickup = prefab.AddComponent<AmmoPickup>();
            SerializedObject serialized = new SerializedObject(pickup);
            serialized.FindProperty("amountOverride").intValue = 0;
            serialized.FindProperty("lifetime").floatValue = 25f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            SavePrefab(prefab, PickupsFolder + "/AmmoPickup.prefab");
        }

        private static void BuildLargeAmmoCache(bool force)
        {
            GameObject prefab = BuildPickupRoot(
                "LargeAmmoCache",
                AmmoCrateSource,
                PickupsFolder + "/LargeAmmoCache.prefab",
                new Vector3(0f, 0.52f, 0f),
                new Vector3(1.9f, 1f, 1.35f),
                Vector3.zero,
                Vector3.one,
                force);
            if (prefab == null)
            {
                return;
            }

            AmmoPickup pickup = prefab.AddComponent<AmmoPickup>();
            SerializedObject serialized = new SerializedObject(pickup);
            serialized.FindProperty("amountOverride").intValue = 120;
            serialized.FindProperty("lifetime").floatValue = 60f;
            serialized.FindProperty("rotationSpeed").floatValue = 35f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            SavePrefab(prefab, PickupsFolder + "/LargeAmmoCache.prefab");
        }

        private static GameObject BuildPickupRoot(
            string prefabName,
            string sourcePath,
            string outputPath,
            Vector3 colliderCenter,
            Vector3 colliderSize,
            Vector3 visualLocalPosition,
            Vector3 visualScale,
            bool force)
        {
            if (!force && AssetDatabase.LoadAssetAtPath<GameObject>(outputPath) != null)
            {
                return null;
            }

            GameObject sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (sourcePrefab == null)
            {
                Debug.LogWarning($"Dustline pickup source prefab is missing: {sourcePath}");
                return null;
            }

            GameObject root = new GameObject(prefabName);
            BoxCollider pickupCollider = root.AddComponent<BoxCollider>();
            pickupCollider.isTrigger = true;
            pickupCollider.center = colliderCenter;
            pickupCollider.size = colliderSize;

            GameObject visual = PrefabUtility.InstantiatePrefab(sourcePrefab, root.transform) as GameObject;
            if (visual == null)
            {
                visual = Object.Instantiate(sourcePrefab, root.transform);
            }

            visual.name = "Visual";
            visual.transform.localPosition = visualLocalPosition;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = visualScale;
            StripPhysicsFromVisual(visual);
            return root;
        }

        private static void StripPhysicsFromVisual(GameObject visual)
        {
            foreach (Collider collider in visual.GetComponentsInChildren<Collider>(includeInactive: true))
            {
                Object.DestroyImmediate(collider);
            }

            foreach (Rigidbody body in visual.GetComponentsInChildren<Rigidbody>(includeInactive: true))
            {
                Object.DestroyImmediate(body);
            }
        }

        private static void SavePrefab(GameObject root, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(ResourcesFolder))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Resources");
            }

            if (!AssetDatabase.IsValidFolder(PickupsFolder))
            {
                AssetDatabase.CreateFolder(ResourcesFolder, "Pickups");
            }
        }
    }
}
