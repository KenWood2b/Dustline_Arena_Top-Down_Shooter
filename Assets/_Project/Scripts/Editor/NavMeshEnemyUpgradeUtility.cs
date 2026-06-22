using DustlineArena.Runtime.Animation;
using DustlineArena.Runtime.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace DustlineArena.Editor
{
    public static class NavMeshEnemyUpgradeUtility
    {
        private const string EnemyPrefabPath = "Assets/_Project/Prefabs/Enemy_Chaser_Rigged.prefab";
        private const string ScenePath = "Assets/_Project/Scenes/Dustline_Arena_01.unity";

        [MenuItem("Dustline Arena/Upgrade Enemies To NavMesh")]
        public static void UpgradeEnemiesToNavMesh()
        {
            UpgradeEnemyPrefab();
            UpgradeScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Dustline Arena: enemies upgraded to NavMesh movement.");
        }

        private static void UpgradeEnemyPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(EnemyPrefabPath);

            Rigidbody body = root.GetComponent<Rigidbody>();
            if (body == null)
            {
                body = root.AddComponent<Rigidbody>();
            }

            body.mass = 1.1f;
            body.useGravity = false;
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            NavMeshAgent agent = root.GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                agent = root.AddComponent<NavMeshAgent>();
            }

            agent.speed = 4.5f;
            agent.acceleration = 28f;
            agent.angularSpeed = 720f;
            agent.radius = 0.42f;
            agent.height = 2f;
            agent.baseOffset = 0f;
            agent.stoppingDistance = 1.15f;
            agent.autoBraking = true;
            agent.updateRotation = false;

            CharacterAnimationDriver animationDriver = root.GetComponent<CharacterAnimationDriver>();
            if (animationDriver != null)
            {
                SerializedObject serializedDriver = new SerializedObject(animationDriver);
                serializedDriver.FindProperty("trackedBody").objectReferenceValue = body;
                serializedDriver.FindProperty("navMeshAgent").objectReferenceValue = agent;
                serializedDriver.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.SaveAsPrefabAsset(root, EnemyPrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void UpgradeScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject arena = GameObject.Find("Arena");
            if (arena == null)
            {
                Debug.LogWarning("Dustline Arena: Arena root was not found, Runtime NavMesh was not added.");
                return;
            }

            GameObject existing = GameObject.Find("Runtime NavMesh");
            GameObject navMeshObject = existing == null ? new GameObject("Runtime NavMesh") : existing;
            navMeshObject.transform.SetParent(arena.transform, false);
            navMeshObject.transform.localPosition = Vector3.zero;

            RuntimeNavMeshBuilder builder = navMeshObject.GetComponent<RuntimeNavMeshBuilder>();
            if (builder == null)
            {
                builder = navMeshObject.AddComponent<RuntimeNavMeshBuilder>();
            }

            builder.Configure(arena.transform, new Vector3(70f, 12f, 70f));
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
