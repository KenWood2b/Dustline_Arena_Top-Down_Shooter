using System.Linq;
using DustlineArena.Runtime.Animation;
using DustlineArena.Runtime.Config;
using DustlineArena.Runtime.Weapons;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace DustlineArena.Editor
{
    public static class CharacterVisualUpgradeUtility
    {
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player_Rigged.prefab";
        private const string EnemyPrefabPath = "Assets/_Project/Prefabs/Enemy_Chaser_Rigged.prefab";
        internal const string PlayerVisualPath = "Assets/_Project/Art/ThirdParty/KayKitAdventurers/Ranger.fbx";
        private const string PlayerTexturePath = "Assets/_Project/Art/ThirdParty/KayKitAdventurers/ranger_texture.png";
        private const string PlayerKayKitMaterialPath = "Assets/_Project/Materials/M_KayKit_Ranger.mat";
        private const string EnemyVisualPath = "Assets/_Project/Art/ThirdParty/QuaterniusZombie/Zombie_Basic.fbx";
        private const string EnemyTextureFolderPath = "Assets/_Project/Art/ThirdParty/QuaterniusZombie/Textures";
        private const string EnemyTexturePath = EnemyTextureFolderPath + "/Zombie_Atlas.png";
        private const string EnemyZombieMaterialPath = "Assets/_Project/Materials/M_Zombie_Atlas.mat";
        private const string PlayerControllerPath = "Assets/_Project/Animation/Controllers/AC_Player.controller";
        private const string EnemyControllerPath = "Assets/_Project/Animation/Controllers/AC_Enemy_Chaser.controller";
        private const string PlayerMaterialPath = "Assets/_Project/Materials/M_Player_Suit_Blue.mat";
        private const string EnemyMaterialPath = "Assets/_Project/Materials/M_Enemy_Suit_Red.mat";
        private const string WeaponMaterialPath = "Assets/_Project/Materials/M_Weapon_DarkMetal.mat";
        private const string WeaponFolderPath = "Assets/_Project/Art/ThirdParty/TopDownShooterKit/Weapons";
        private const string ConfigFolderPath = "Assets/_Project/Configs";

        [InitializeOnLoadMethod]
        private static void ScheduleZombieEnemyInstall()
        {
            EditorApplication.delayCall += TryInstallZombieEnemy;
        }

        [MenuItem("Dustline Arena/Install Quaternius Zombie Enemies")]
        public static void InstallZombieEnemies()
        {
            ConfigureZombieImporter();
            Material zombieMaterial = CreateZombieMaterial();
            ConfigureZombieController();
            UpgradeVisual(EnemyPrefabPath, EnemyVisualPath, EnemyControllerPath, zombieMaterial, null, 1f, false);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Dustline Arena/Install KayKit Ranger Player")]
        public static void InstallKayKitRangerPlayer()
        {
            ConfigureRangerImporter();

            Material playerMaterial = CreateRangerMaterial();
            Material weaponMaterial = AssetDatabase.LoadAssetAtPath<Material>(WeaponMaterialPath);
            ConfigureCharacterMaterial(weaponMaterial, new Color(0.08f, 0.085f, 0.09f), 0.18f, 0.32f);

            UpgradeVisual(PlayerPrefabPath, PlayerVisualPath, PlayerControllerPath, playerMaterial, weaponMaterial, 1f, true);
            KeepPlayerLegsMovingWhileFiring();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Dustline Arena/Repair Ranger Weapon Rig")]
        public static void RepairRangerWeaponRig()
        {
            EnsureWeaponConfigs();
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            if (root == null)
            {
                return;
            }

            Transform visual = root.transform.Find("Visual");
            Transform weapon = root.transform.Find("Weapon_Rifle")
                ?? FindDeepChild(root.transform, "Weapon_Rifle");
            if (visual != null && weapon != null)
            {
                AttachWeaponToRightHand(weapon, visual);
                Material weaponMaterial = AssetDatabase.LoadAssetAtPath<Material>(WeaponMaterialPath);
                EnsureWeaponModels(weapon, weaponMaterial);

                ProjectileWeapon projectileWeapon = weapon.GetComponent<ProjectileWeapon>();
                if (projectileWeapon != null)
                {
                    SerializedObject serializedWeapon = new SerializedObject(projectileWeapon);
                    serializedWeapon.FindProperty("config").objectReferenceValue = null;
                    serializedWeapon.FindProperty("muzzleLocalPosition").vector3Value = new Vector3(0f, 0.2f, 1f);
                    serializedWeapon.FindProperty("leftHandGripLocalPosition").vector3Value = new Vector3(0f, 0.02f, 0.44f);
                    serializedWeapon.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            foreach (WeaponHandIK handIK in root.GetComponentsInChildren<WeaponHandIK>(true))
            {
                Object.DestroyImmediate(handIK);
            }

            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Dustline Arena/Upgrade Character Visuals")]
        public static void UpgradeCharacterVisuals()
        {
            Material playerMaterial = CreateRangerMaterial();
            Material weaponMaterial = AssetDatabase.LoadAssetAtPath<Material>(WeaponMaterialPath);

            ConfigureCharacterMaterial(playerMaterial, new Color(0.16f, 0.33f, 0.52f), 0.04f, 0.18f);
            ConfigureCharacterMaterial(weaponMaterial, new Color(0.08f, 0.085f, 0.09f), 0.18f, 0.32f);

            UpgradeVisual(PlayerPrefabPath, PlayerVisualPath, PlayerControllerPath, playerMaterial, weaponMaterial, 1f, true);
            Material zombieMaterial = CreateZombieMaterial();
            ConfigureZombieController();
            UpgradeVisual(EnemyPrefabPath, EnemyVisualPath, EnemyControllerPath, zombieMaterial, null, 1f, false);
            KeepPlayerLegsMovingWhileFiring();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void TryInstallZombieEnemy()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(EnemyVisualPath) == null)
            {
                return;
            }

            if (ConfigureZombieImporter())
            {
                EditorApplication.delayCall += TryInstallZombieEnemy;
                return;
            }

            Material zombieMaterial = CreateZombieMaterial();
            ConfigureZombieController();
            if (!EnemyUsesZombieVisual())
            {
                UpgradeVisual(EnemyPrefabPath, EnemyVisualPath, EnemyControllerPath, zombieMaterial, null, 1f, false);
            }
            else
            {
                ApplyMaterialToEnemyPrefab(zombieMaterial);
            }

            AssetDatabase.SaveAssets();
        }

        private static bool ConfigureZombieImporter()
        {
            ModelImporter importer = AssetImporter.GetAtPath(EnemyVisualPath) as ModelImporter;
            if (importer == null)
            {
                return false;
            }

            bool changed = false;
            if (importer.animationType != ModelImporterAnimationType.Generic)
            {
                importer.animationType = ModelImporterAnimationType.Generic;
                changed = true;
            }

            if (!importer.importAnimation)
            {
                importer.importAnimation = true;
                changed = true;
            }

            ModelImporterClipAnimation[] clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
            {
                clips = importer.defaultClipAnimations;
            }

            bool clipSettingsChanged = false;
            foreach (ModelImporterClipAnimation clip in clips)
            {
                string clipName = NormalizeClipName(clip.name);
                bool shouldLoop = clipName == "Idle"
                    || clipName == "Walk"
                    || clipName == "Run"
                    || clipName == "Run_Arms"
                    || clipName == "Crawl";
                if (clip.loopTime != shouldLoop)
                {
                    clip.loopTime = shouldLoop;
                    clipSettingsChanged = true;
                }
            }

            if (clipSettingsChanged)
            {
                importer.clipAnimations = clips;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }

            return changed;
        }

        private static Material CreateZombieMaterial()
        {
            EnsureAssetFolder(EnemyTextureFolderPath);

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(EnemyTexturePath);
            if (texture == null && AssetImporter.GetAtPath(EnemyVisualPath) is ModelImporter importer)
            {
                importer.ExtractTextures(EnemyTextureFolderPath);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                texture = AssetDatabase.LoadAssetAtPath<Texture2D>(EnemyTexturePath);
            }

            if (texture == null)
            {
                string textureGuid = AssetDatabase.FindAssets("Zombie_Atlas t:Texture2D", new[] { EnemyTextureFolderPath })
                    .FirstOrDefault();
                if (!string.IsNullOrEmpty(textureGuid))
                {
                    texture = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(textureGuid));
                }
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(EnemyZombieMaterialPath);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (material == null)
            {
                material = new Material(shader)
                {
                    name = "M_Zombie_Atlas"
                };
                AssetDatabase.CreateAsset(material, EnemyZombieMaterialPath);
            }
            else if (shader != null && material.shader != shader)
            {
                material.shader = shader;
            }

            material.color = Color.white;
            material.mainTexture = texture;
            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
            }

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", Color.white);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", 0f);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0.12f);
            }

            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ApplyMaterialToEnemyPrefab(Material material)
        {
            if (material == null)
            {
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(EnemyPrefabPath);
            if (root == null)
            {
                return;
            }

            Transform visual = root.transform.Find("Visual");
            if (visual != null)
            {
                ApplyMaterialToRenderers(visual.gameObject, material);
            }

            PrefabUtility.SaveAsPrefabAsset(root, EnemyPrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void EnsureAssetFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string parent = folderPath.Substring(0, folderPath.LastIndexOf('/'));
            EnsureAssetFolder(parent);
            AssetDatabase.CreateFolder(parent, folderPath.Substring(folderPath.LastIndexOf('/') + 1));
        }

        private static void ConfigureZombieController()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(EnemyControllerPath);
            if (controller == null)
            {
                return;
            }

            AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(EnemyVisualPath)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", System.StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (clips.Length == 0)
            {
                return;
            }

            foreach (AnimatorControllerLayer layer in controller.layers)
            {
                foreach (ChildAnimatorState childState in layer.stateMachine.states)
                {
                    AnimationClip replacement = childState.state.name switch
                    {
                        "Idle" => FindZombieClip(clips, "Idle"),
                        "Walk" => FindZombieClip(clips, "Walk", "Run", "Run_Arms"),
                        "Attack" => FindZombieClip(clips, "Idle_Attack", "Run_Attack", "Punch"),
                        "Hit" => FindZombieClip(clips, "HitReact"),
                        "Dead" => FindZombieClip(clips, "Death"),
                        _ => null
                    };

                    if (replacement != null && childState.state.motion != replacement)
                    {
                        childState.state.motion = replacement;
                        EditorUtility.SetDirty(childState.state);
                    }
                }
            }

            EditorUtility.SetDirty(controller);
        }

        private static AnimationClip FindZombieClip(AnimationClip[] clips, params string[] preferredNames)
        {
            foreach (string preferredName in preferredNames)
            {
                AnimationClip match = clips.FirstOrDefault(
                    clip => string.Equals(NormalizeClipName(clip.name), preferredName, System.StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static string NormalizeClipName(string clipName)
        {
            int separator = clipName.LastIndexOf('|');
            return separator >= 0 ? clipName.Substring(separator + 1) : clipName;
        }

        private static bool EnemyUsesZombieVisual()
        {
            GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
            Transform visual = enemyPrefab == null ? null : enemyPrefab.transform.Find("Visual");
            if (visual == null)
            {
                return false;
            }

            Object source = PrefabUtility.GetCorrespondingObjectFromSource(visual.gameObject);
            return source != null
                && string.Equals(AssetDatabase.GetAssetPath(source), EnemyVisualPath, System.StringComparison.OrdinalIgnoreCase);
        }

        private static void UpgradeVisual(string prefabPath, string visualPath, string controllerPath, Material bodyMaterial, Material weaponMaterial, float scale, bool attachWeaponToHand)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null)
            {
                return;
            }

            Transform oldVisual = root.transform.Find("Visual");
            if (oldVisual != null)
            {
                Object.DestroyImmediate(oldVisual.gameObject);
            }

            GameObject visualAsset = AssetDatabase.LoadAssetAtPath<GameObject>(visualPath);
            GameObject visual = visualAsset == null
                ? GameObject.CreatePrimitive(PrimitiveType.Capsule)
                : (GameObject)PrefabUtility.InstantiatePrefab(visualAsset);

            visual.name = "Visual";
            visual.transform.SetParent(root.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one * scale;
            ApplyMaterialToRenderers(visual, bodyMaterial);

            Animator animator = visual.GetComponentInChildren<Animator>();
            if (animator == null)
            {
                animator = visual.AddComponent<Animator>();
            }

            if (animator.avatar == null)
            {
                animator.avatar = AssetDatabase.LoadAllAssetsAtPath(visualPath)
                    .OfType<Avatar>()
                    .FirstOrDefault();
            }

            RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
            if (animator != null && controller != null)
            {
                animator.runtimeAnimatorController = controller;
            }

            Transform weapon = root.transform.Find("Weapon_Rifle");
            if (weapon != null)
            {
                EnsureWeaponModels(weapon, weaponMaterial);
                ApplyMaterialToRenderers(weapon.gameObject, weaponMaterial);
                EnsureFirePointMarker(weapon);

                if (attachWeaponToHand)
                {
                    AttachWeaponToRightHand(weapon, visual.transform);
                }
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void ApplyMaterialToRenderers(GameObject root, Material material)
        {
            if (root == null || material == null)
            {
                return;
            }

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterial = material;
                EditorUtility.SetDirty(renderer);
            }
        }

        private static void ConfigureCharacterMaterial(Material material, Color color, float metallic, float smoothness)
        {
            if (material == null)
            {
                return;
            }

            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", metallic);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }

            EditorUtility.SetDirty(material);
        }

        private static void AttachWeaponToRightHand(Transform weapon, Transform visualRoot)
        {
            Transform weaponSocket = FindDeepChild(visualRoot, "hand.r");
            Transform stableWeaponParent = weaponSocket != null
                ? weaponSocket
                : visualRoot.parent == null ? visualRoot : visualRoot.parent;

            weapon.SetParent(stableWeaponParent, false);
            weapon.localPosition = weaponSocket == null ? new Vector3(0.08f, 1.26f, 0.48f) : new Vector3(0f, 0.01f, 0.1f);
            weapon.localRotation = Quaternion.identity;
            weapon.localScale = Vector3.one;

            Transform muzzle = weapon.Find("FirePoint_Muzzle") ?? weapon.Find("Muzzle");
            if (muzzle != null)
            {
                muzzle.name = "FirePoint_Muzzle";
                muzzle.localPosition = new Vector3(0f, 0.2f, 1f);
                muzzle.localRotation = Quaternion.identity;
            }
        }

        private static void EnsureWeaponModels(Transform weapon, Material weaponMaterial)
        {
            string[] assetNames = { "AK", "SMG", "Pistol", "Shotgun", "Grenade" };
            for (int i = 0; i < assetNames.Length; i++)
            {
                string assetName = assetNames[i];
                string modelName = assetName + "_Model";
                Transform existing = weapon.Find(modelName);
                GameObject model;

                if (existing != null)
                {
                    model = existing.gameObject;
                }
                else
                {
                    GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(
                        $"{WeaponFolderPath}/{assetName}.fbx");
                    if (asset == null)
                    {
                        continue;
                    }

                    model = (GameObject)PrefabUtility.InstantiatePrefab(asset);
                    model.name = modelName;
                    model.transform.SetParent(weapon, false);
                }

                model.transform.localPosition = assetName == "Grenade"
                    ? new Vector3(0f, 0.02f, 0.08f)
                    : Vector3.zero;
                model.transform.localRotation = assetName == "Grenade"
                    ? Quaternion.Euler(-90f, 0f, 0f)
                    : Quaternion.Euler(0f, -90f, 0f) * Quaternion.Euler(-90f, 0f, 0f);
                model.transform.localScale = Vector3.one * (assetName == "Grenade" ? 20f : 100f);
                model.SetActive(false);
                ApplyMaterialToRenderers(model, weaponMaterial);
            }
        }

        private static WeaponConfig EnsureWeaponConfigs()
        {
            WeaponConfig source = AssetDatabase.LoadAssetAtPath<WeaponConfig>(
                $"{ConfigFolderPath}/PistolWeaponConfig.asset");
            GameObject projectile = source == null ? null : source.ProjectilePrefab;

            CreateOrUpdateWeaponConfig(
                "AKWeaponConfig",
                WeaponVisualId.AK,
                18f,
                7.5f,
                42f,
                1.8f,
                1.2f,
                1,
                0f,
                5.5f,
                projectile,
                null);
            CreateOrUpdateWeaponConfig(
                "SMGWeaponConfig",
                WeaponVisualId.SMG,
                11f,
                12f,
                38f,
                1.5f,
                3.5f,
                1,
                0f,
                5.5f,
                projectile,
                null);
            WeaponConfig pistol = CreateOrUpdateWeaponConfig(
                "PistolWeaponConfig",
                WeaponVisualId.Pistol,
                30f,
                2.5f,
                48f,
                2f,
                0.6f,
                1,
                0f,
                5.5f,
                projectile,
                null);
            CreateOrUpdateWeaponConfig(
                "ShotgunWeaponConfig",
                WeaponVisualId.Shotgun,
                10f,
                1.1f,
                34f,
                0.75f,
                8f,
                8,
                0f,
                5.5f,
                projectile,
                null);
            GameObject grenadeModel = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"{WeaponFolderPath}/Grenade.fbx");
            CreateOrUpdateWeaponConfig(
                "GrenadeWeaponConfig",
                WeaponVisualId.Grenade,
                85f,
                0.55f,
                9.5f,
                1.8f,
                0f,
                1,
                3.2f,
                5.5f,
                projectile,
                grenadeModel);
            return pistol;
        }

        private static WeaponConfig CreateOrUpdateWeaponConfig(
            string assetName,
            WeaponVisualId visual,
            float damage,
            float shotsPerSecond,
            float projectileSpeed,
            float projectileLifetime,
            float spreadAngle,
            int projectilesPerShot,
            float explosionRadius,
            float throwUpwardSpeed,
            GameObject projectile,
            GameObject thrownPrefab)
        {
            string path = $"{ConfigFolderPath}/{assetName}.asset";
            WeaponConfig config = AssetDatabase.LoadAssetAtPath<WeaponConfig>(path);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<WeaponConfig>();
                AssetDatabase.CreateAsset(config, path);
            }

            SerializedObject serializedConfig = new SerializedObject(config);
            serializedConfig.FindProperty("visual").enumValueIndex = (int)visual;
            serializedConfig.FindProperty("damage").floatValue = damage;
            serializedConfig.FindProperty("shotsPerSecond").floatValue = shotsPerSecond;
            serializedConfig.FindProperty("projectileSpeed").floatValue = projectileSpeed;
            serializedConfig.FindProperty("projectileLifetime").floatValue = projectileLifetime;
            serializedConfig.FindProperty("spreadAngle").floatValue = spreadAngle;
            serializedConfig.FindProperty("projectilesPerShot").intValue = projectilesPerShot;
            serializedConfig.FindProperty("explosionRadius").floatValue = explosionRadius;
            serializedConfig.FindProperty("throwUpwardSpeed").floatValue = throwUpwardSpeed;
            bool isGrenade = visual == WeaponVisualId.Grenade;
            serializedConfig.FindProperty("minThrowSpeed").floatValue = isGrenade ? 4.5f : 4f;
            serializedConfig.FindProperty("maxThrowSpeed").floatValue = isGrenade ? 13f : 12f;
            serializedConfig.FindProperty("minThrowUpwardSpeed").floatValue = isGrenade ? 3.4f : 3.5f;
            serializedConfig.FindProperty("maxThrowUpwardSpeed").floatValue = isGrenade ? 7.5f : 7f;
            serializedConfig.FindProperty("throwChargeDuration").floatValue = isGrenade ? 1.4f : 1.25f;
            if (projectile != null)
            {
                serializedConfig.FindProperty("projectilePrefab").objectReferenceValue = projectile;
            }
            serializedConfig.FindProperty("thrownPrefab").objectReferenceValue = thrownPrefab;

            serializedConfig.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            return config;
        }

        internal static bool IsKayKitRangerInstalled()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            if (root == null)
            {
                return false;
            }

            Transform visual = root.transform.Find("Visual");
            Object source = visual == null ? null : PrefabUtility.GetCorrespondingObjectFromSource(visual.gameObject);
            bool installed = source != null && AssetDatabase.GetAssetPath(source) == PlayerVisualPath;
            PrefabUtility.UnloadPrefabContents(root);
            return installed;
        }

        private static void ConfigureRangerImporter()
        {
            AssetDatabase.ImportAsset(PlayerVisualPath, ImportAssetOptions.ForceSynchronousImport);
            ModelImporter importer = AssetImporter.GetAtPath(PlayerVisualPath) as ModelImporter;
            if (importer == null)
            {
                return;
            }

            bool changed = importer.animationType != ModelImporterAnimationType.Human
                || importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel
                || importer.importAnimation;

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = false;

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        private static Material CreateRangerMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(PlayerKayKitMaterialPath);
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(PlayerTexturePath);

            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, PlayerKayKitMaterialPath);
            }

            material.mainTexture = texture;
            material.color = Color.white;
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", Color.white);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static Transform FindDeepChild(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    return child;
                }
            }

            return null;
        }

        private static void EnsureFirePointMarker(Transform weapon)
        {
            Transform muzzle = weapon.Find("FirePoint_Muzzle") ?? weapon.Find("Muzzle");
            if (muzzle == null)
            {
                GameObject muzzleObject = new GameObject("FirePoint_Muzzle");
                muzzleObject.transform.SetParent(weapon, false);
                muzzleObject.transform.localRotation = Quaternion.identity;
                muzzle = muzzleObject.transform;
            }

            muzzle.name = "FirePoint_Muzzle";
            muzzle.localPosition = new Vector3(0f, 0.2f, 1f);
            muzzle.localRotation = Quaternion.identity;
            muzzle.localScale = Vector3.one;

            if (muzzle.GetComponent<FirePointMarker>() == null)
            {
                muzzle.gameObject.AddComponent<FirePointMarker>();
            }
        }

        private static void KeepPlayerLegsMovingWhileFiring()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(PlayerControllerPath);
            if (controller == null || controller.layers.Length == 0)
            {
                return;
            }

            AnimatorControllerLayer[] layers = controller.layers;
            AnimatorControllerLayer baseLayer = layers[0];
            baseLayer.iKPass = true;
            layers[0] = baseLayer;
            controller.layers = layers;

            AnimatorStateMachine machine = baseLayer.stateMachine;
            foreach (AnimatorStateTransition transition in machine.anyStateTransitions)
            {
                if (transition.destinationState != null && transition.destinationState.name == "Fire")
                {
                    machine.RemoveAnyStateTransition(transition);
                    continue;
                }

                foreach (AnimatorCondition condition in transition.conditions)
                {
                    if (condition.parameter == "Fire")
                    {
                        machine.RemoveAnyStateTransition(transition);
                        break;
                    }
                }
            }

            EditorUtility.SetDirty(controller);
        }
    }

}
