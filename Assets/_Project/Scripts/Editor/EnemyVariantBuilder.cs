using DustlineArena.Runtime.Animation;
using DustlineArena.Runtime.Config;
using DustlineArena.Runtime.Enemies;
using DustlineArena.Runtime.Health;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace DustlineArena.Editor
{
    public static class EnemyVariantBuilder
    {
        private const string BaseEnemyPrefabPath = "Assets/_Project/Prefabs/Enemy_Chaser_Rigged.prefab";
        private const string ConfigFolderPath = "Assets/_Project/Configs";
        private const string PrefabFolderPath = "Assets/_Project/Prefabs";
        private const string MaterialFolderPath = "Assets/_Project/Materials";
        private const string BaseZombieMaterialPath = "Assets/_Project/Materials/M_Zombie_Atlas.mat";

        private static readonly VariantSpec[] Variants =
        {
            new VariantSpec(
                "Runner",
                $"{ConfigFolderPath}/EnemyConfig_Runner.asset",
                $"{PrefabFolderPath}/Enemy_Chaser_Runner.prefab",
                $"{MaterialFolderPath}/M_Zombie_Runner.mat",
                new Color(0.36f, 0.95f, 0.18f),
                0.86f,
                55f,
                9.8f,
                96f,
                1.2f,
                7f,
                0.5f,
                true,
                1.48f,
                0.55f,
                1.45f,
                3f,
                0.34f,
                1.72f),
            new VariantSpec(
                "Bruiser",
                $"{ConfigFolderPath}/EnemyConfig_Bruiser.asset",
                $"{PrefabFolderPath}/Enemy_Chaser_Bruiser.prefab",
                $"{MaterialFolderPath}/M_Zombie_Bruiser.mat",
                new Color(0.55f, 0.12f, 0.16f),
                1.11f,
                210f,
                3.9f,
                22f,
                1.45f,
                18f,
                0.72f,
                false,
                1.15f,
                0.35f,
                2.2f,
                4.5f,
                0.48f,
                2.12f),
            new VariantSpec(
                "Tank",
                $"{ConfigFolderPath}/EnemyConfig_Tank.asset",
                $"{PrefabFolderPath}/Enemy_Chaser_Tank.prefab",
                $"{MaterialFolderPath}/M_Zombie_Tank.mat",
                new Color(0.38f, 0.48f, 0.58f),
                1.32f,
                420f,
                2.65f,
                16f,
                1.7f,
                35f,
                1.25f,
                false,
                1.1f,
                0.3f,
                2.6f,
                5f,
                0.62f,
                2.45f)
        };

        [MenuItem("Dustline Arena/Build Enemy Variants")]
        public static void BuildEnemyVariants()
        {
            GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BaseEnemyPrefabPath);
            if (basePrefab == null)
            {
                Debug.LogError($"Dustline Arena: base enemy prefab not found at {BaseEnemyPrefabPath}.");
                return;
            }

            foreach (VariantSpec variant in Variants)
            {
                EnemyConfig config = CreateOrUpdateConfig(variant);
                Material material = CreateOrUpdateMaterial(variant);
                CreateOrUpdatePrefab(variant, config, material);
            }

            ConfigureWaveEnemyMixes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Dustline Arena: enemy variants built and wave mixes updated.");
        }

        public static void BuildEnemyVariantsFromCommandLine()
        {
            BuildEnemyVariants();
        }

        private static EnemyConfig CreateOrUpdateConfig(VariantSpec variant)
        {
            EnemyConfig config = AssetDatabase.LoadAssetAtPath<EnemyConfig>(variant.ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<EnemyConfig>();
                AssetDatabase.CreateAsset(config, variant.ConfigPath);
            }

            SerializedObject serializedConfig = new SerializedObject(config);
            serializedConfig.FindProperty("moveSpeed").floatValue = variant.MoveSpeed;
            serializedConfig.FindProperty("acceleration").floatValue = variant.Acceleration;
            serializedConfig.FindProperty("attackRange").floatValue = variant.AttackRange;
            serializedConfig.FindProperty("attackDamage").floatValue = variant.AttackDamage;
            serializedConfig.FindProperty("attackCooldown").floatValue = variant.AttackCooldown;
            serializedConfig.FindProperty("useChaseBurst").boolValue = variant.UseChaseBurst;
            serializedConfig.FindProperty("chaseBurstSpeedMultiplier").floatValue = variant.ChaseBurstSpeedMultiplier;
            serializedConfig.FindProperty("chaseBurstDuration").floatValue = variant.ChaseBurstDuration;
            serializedConfig.FindProperty("chaseBurstCooldown").floatValue = variant.ChaseBurstCooldown;
            serializedConfig.FindProperty("chaseBurstMinDistance").floatValue = variant.ChaseBurstMinDistance;
            serializedConfig.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            return config;
        }

        private static Material CreateOrUpdateMaterial(VariantSpec variant)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(variant.MaterialPath);
            if (material == null)
            {
                Material source = AssetDatabase.LoadAssetAtPath<Material>(BaseZombieMaterialPath);
                material = source == null ? new Material(Shader.Find("Universal Render Pipeline/Lit")) : new Material(source);
                AssetDatabase.CreateAsset(material, variant.MaterialPath);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", variant.Tint);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", variant.Tint);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void CreateOrUpdatePrefab(VariantSpec variant, EnemyConfig config, Material material)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(variant.PrefabPath) == null
                && !AssetDatabase.CopyAsset(BaseEnemyPrefabPath, variant.PrefabPath))
            {
                Debug.LogError($"Dustline Arena: failed to create {variant.Name} prefab.");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(variant.PrefabPath);
            if (root == null)
            {
                return;
            }

            root.name = $"Enemy_Chaser_{variant.Name}";
            root.transform.localScale = Vector3.one * variant.Scale;
            ConfigureComponent(root.GetComponent<EnemyBrain>(), "config", config);
            ConfigureComponent(root.GetComponent<HealthComponent>(), "maxHealth", variant.MaxHealth);

            if (root.TryGetComponent(out NavMeshAgent agent))
            {
                agent.radius = variant.AgentRadius;
                agent.height = variant.AgentHeight;
                agent.speed = variant.MoveSpeed;
                agent.acceleration = variant.Acceleration;
                agent.stoppingDistance = variant.AttackRange * 0.85f;
            }

            if (root.TryGetComponent(out CapsuleCollider collider))
            {
                collider.radius = variant.AgentRadius;
                collider.height = variant.AgentHeight;
                collider.center = new Vector3(0f, variant.AgentHeight * 0.5f, 0f);
            }

            CharacterAnimationDriver animationDriver = root.GetComponent<CharacterAnimationDriver>();
            ConfigureComponent(animationDriver, "maxSpeed", variant.MoveSpeed);

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = material;
                }

                renderer.sharedMaterials = materials;
            }

            PrefabUtility.SaveAsPrefabAsset(root, variant.PrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void ConfigureWaveEnemyMixes()
        {
            GameObject basic = AssetDatabase.LoadAssetAtPath<GameObject>(BaseEnemyPrefabPath);
            GameObject runner = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolderPath}/Enemy_Chaser_Runner.prefab");
            GameObject bruiser = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolderPath}/Enemy_Chaser_Bruiser.prefab");
            GameObject tank = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolderPath}/Enemy_Chaser_Tank.prefab");

            ConfigureWave($"{ConfigFolderPath}/Wave_01.asset", basic, new[] { basic, runner }, new[] { 5.5f, 1.1f });
            ConfigureWave($"{ConfigFolderPath}/Wave_02.asset", basic, new[] { basic, runner, bruiser }, new[] { 4.5f, 2.4f, 1.1f });
            ConfigureWave($"{ConfigFolderPath}/Wave_03.asset", basic, new[] { basic, runner, bruiser, tank }, new[] { 3.4f, 2.7f, 1.9f, 0.65f });
            ConfigureWave($"{ConfigFolderPath}/Wave_04.asset", basic, new[] { basic, runner, bruiser, tank }, new[] { 2.4f, 2.6f, 2.5f, 1.15f });
        }

        private static void ConfigureWave(string path, GameObject fallbackPrefab, GameObject[] prefabs, float[] weights)
        {
            WaveConfig wave = AssetDatabase.LoadAssetAtPath<WaveConfig>(path);
            if (wave == null)
            {
                return;
            }

            SerializedObject serializedWave = new SerializedObject(wave);
            serializedWave.FindProperty("enemyPrefab").objectReferenceValue = fallbackPrefab;
            SerializedProperty options = serializedWave.FindProperty("enemyPrefabs");
            options.arraySize = prefabs.Length;
            for (int i = 0; i < prefabs.Length; i++)
            {
                SerializedProperty option = options.GetArrayElementAtIndex(i);
                option.FindPropertyRelative("prefab").objectReferenceValue = prefabs[i];
                option.FindPropertyRelative("weight").floatValue = weights[i];
            }

            serializedWave.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(wave);
        }

        private static void ConfigureComponent(Object component, string propertyName, Object value)
        {
            if (component == null)
            {
                return;
            }

            SerializedObject serializedObject = new SerializedObject(component);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                return;
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureComponent(Object component, string propertyName, float value)
        {
            if (component == null)
            {
                return;
            }

            SerializedObject serializedObject = new SerializedObject(component);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                return;
            }

            property.floatValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private readonly struct VariantSpec
        {
            public readonly string Name;
            public readonly string ConfigPath;
            public readonly string PrefabPath;
            public readonly string MaterialPath;
            public readonly Color Tint;
            public readonly float Scale;
            public readonly float MaxHealth;
            public readonly float MoveSpeed;
            public readonly float Acceleration;
            public readonly float AttackRange;
            public readonly float AttackDamage;
            public readonly float AttackCooldown;
            public readonly bool UseChaseBurst;
            public readonly float ChaseBurstSpeedMultiplier;
            public readonly float ChaseBurstDuration;
            public readonly float ChaseBurstCooldown;
            public readonly float ChaseBurstMinDistance;
            public readonly float AgentRadius;
            public readonly float AgentHeight;

            public VariantSpec(
                string name,
                string configPath,
                string prefabPath,
                string materialPath,
                Color tint,
                float scale,
                float maxHealth,
                float moveSpeed,
                float acceleration,
                float attackRange,
                float attackDamage,
                float attackCooldown,
                bool useChaseBurst,
                float chaseBurstSpeedMultiplier,
                float chaseBurstDuration,
                float chaseBurstCooldown,
                float chaseBurstMinDistance,
                float agentRadius,
                float agentHeight)
            {
                Name = name;
                ConfigPath = configPath;
                PrefabPath = prefabPath;
                MaterialPath = materialPath;
                Tint = tint;
                Scale = scale;
                MaxHealth = maxHealth;
                MoveSpeed = moveSpeed;
                Acceleration = acceleration;
                AttackRange = attackRange;
                AttackDamage = attackDamage;
                AttackCooldown = attackCooldown;
                UseChaseBurst = useChaseBurst;
                ChaseBurstSpeedMultiplier = chaseBurstSpeedMultiplier;
                ChaseBurstDuration = chaseBurstDuration;
                ChaseBurstCooldown = chaseBurstCooldown;
                ChaseBurstMinDistance = chaseBurstMinDistance;
                AgentRadius = agentRadius;
                AgentHeight = agentHeight;
            }
        }
    }
}
