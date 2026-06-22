using DustlineArena.Runtime.Animation;
using DustlineArena.Runtime.Camera;
using DustlineArena.Runtime.Common;
using DustlineArena.Runtime.Config;
using DustlineArena.Runtime.Enemies;
using DustlineArena.Runtime.Health;
using DustlineArena.Runtime.Navigation;
using DustlineArena.Runtime.Pickups;
using DustlineArena.Runtime.Player;
using DustlineArena.Runtime.Spawning;
using DustlineArena.Runtime.Weapons;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace DustlineArena.Editor
{
    public static class DustlinePrototypeBuilder
    {
        private const string Root = "Assets/_Project";
        private const string Art = Root + "/Art/ThirdParty/TopDownShooterKit";
        private const string Configs = Root + "/Configs";
        private const string Controllers = Root + "/Animation/Controllers";
        private const string Prefabs = Root + "/Prefabs";
        private const string Scenes = Root + "/Scenes";
        private const string Materials = Root + "/Materials";

        private const string PlayerCharacterPrefab = "Assets/_Project/Art/ThirdParty/KayKitAdventurers/Ranger.fbx";
        private const string EnemyCharacterPrefab = "Assets/_Project/Art/ThirdParty/QuaterniusZombie/Zombie_Basic.fbx";
        private const string EnemyZombieMaterialPath = "Assets/_Project/Materials/M_Zombie_Atlas.mat";
        private const string GroundMaterialPath = "Assets/Synty/PolygonGeneric/Materials/Generic_Dirt.mat";

        [MenuItem("Dustline Arena/Build Prototype Assets")]
        public static void BuildPrototypeAssets()
        {
            EnsureFolders();
            UrpSetupUtility.EnsureUrpAssigned();

            Material groundMaterial = AssetDatabase.LoadAssetAtPath<Material>(GroundMaterialPath);
            if (groundMaterial == null)
            {
                groundMaterial = CreateMaterial("M_Ground_Dust", new Color(0.48f, 0.43f, 0.36f));
            }
            Material projectileMaterial = CreateMaterial("M_Projectile", new Color(1f, 0.82f, 0.18f));

            PlayerMovementConfig playerMovement = CreateAsset<PlayerMovementConfig>(Configs + "/PlayerMovementConfig.asset");
            EnemyConfig enemyConfig = CreateAsset<EnemyConfig>(Configs + "/EnemyConfig.asset");
            WeaponConfig pistolConfig = CreateAsset<WeaponConfig>(Configs + "/PistolWeaponConfig.asset");

            GameObject projectilePrefab = CreateProjectilePrefab(projectileMaterial);
            AssignObject(pistolConfig, "projectilePrefab", projectilePrefab);
            AssignEnum(pistolConfig, "visual", WeaponVisualId.Pistol);
            AssignFloat(pistolConfig, "damage", 30f);
            AssignFloat(pistolConfig, "shotsPerSecond", 2.5f);
            AssignFloat(pistolConfig, "projectileSpeed", 48f);
            AssignFloat(pistolConfig, "projectileLifetime", 2f);
            AssignFloat(pistolConfig, "spreadAngle", 0.6f);

            AnimatorController playerController = CreatePlayerAnimatorController();
            AnimatorController enemyController = CreateEnemyAnimatorController();

            GameObject playerPrefab = CreatePlayerPrefab(playerMovement, pistolConfig, playerController);
            GameObject enemyPrefab = CreateEnemyPrefab(enemyConfig, enemyController);

            WaveConfig waveConfig = CreateAsset<WaveConfig>(Configs + "/Wave_01.asset");
            AssignObject(waveConfig, "enemyPrefab", enemyPrefab);

            CreatePrototypeScene(playerPrefab, waveConfig, pistolConfig, groundMaterial);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void EnsureFolders()
        {
            CreateFolder("Assets", "_Project");
            CreateFolder(Root, "Animation");
            CreateFolder(Root + "/Animation", "Controllers");
            CreateFolder(Root, "Configs");
            CreateFolder(Root, "Prefabs");
            CreateFolder(Root, "Scenes");
            CreateFolder(Root, "Materials");
        }

        private static void CreateFolder(string parent, string folder)
        {
            string path = parent + "/" + folder;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, folder);
            }
        }

        private static T CreateAsset<T>(string path) where T : ScriptableObject
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                return existing;
            }

            T asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static Material CreateMaterial(string name, Color color)
        {
            string path = $"{Materials}/{name}.mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material material = new Material(shader)
            {
                color = color
            };

            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static AnimatorController CreatePlayerAnimatorController()
        {
            string path = Controllers + "/AC_Player.controller";
            AnimatorController existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (existing != null)
            {
                return existing;
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            AnimatorControllerLayer[] layers = controller.layers;
            AnimatorControllerLayer baseLayer = layers[0];
            baseLayer.iKPass = true;
            layers[0] = baseLayer;
            controller.layers = layers;
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Fire", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Dead", AnimatorControllerParameterType.Bool);

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState idle = machine.AddState("Idle", new Vector3(260f, 120f, 0f));
            AnimatorState run = machine.AddState("Run Rifle", new Vector3(520f, 120f, 0f));
            AnimatorState fire = machine.AddState("Fire", new Vector3(520f, -40f, 0f));
            AnimatorState hit = machine.AddState("Hit", new Vector3(260f, -160f, 0f));
            AnimatorState dead = machine.AddState("Dead", new Vector3(780f, -160f, 0f));

            idle.motion = LoadClip("Assets/KayKit/Characters/Animations/Animations/Rig_Medium/Combat Ranged/Ranged_2H_Aiming.anim");
            run.motion = LoadClip("Assets/KayKit/Characters/Animations/Animations/Rig_Medium/Movement Advanced/Running_HoldingRifle.anim");
            fire.motion = LoadClip("Assets/KayKit/Characters/Animations/Animations/Rig_Medium/Combat Ranged/Ranged_2H_Shoot.anim");
            hit.motion = LoadClip("Assets/KayKit/Characters/Animations/Animations/Rig_Medium/General/Hit_A.anim");
            dead.motion = LoadClip("Assets/KayKit/Characters/Animations/Animations/Rig_Medium/General/Death_A.anim");

            machine.defaultState = idle;
            AddFloatTransition(idle, run, "Speed", AnimatorConditionMode.Greater, 0.1f);
            AddFloatTransition(run, idle, "Speed", AnimatorConditionMode.Less, 0.1f);
            AddTriggerTransition(machine, hit, "Hit");
            AddBoolTransition(machine, dead, "Dead", true);
            AddExitTransition(hit, idle, 0.85f);

            return controller;
        }

        private static AnimatorController CreateEnemyAnimatorController()
        {
            string path = Controllers + "/AC_Enemy_Chaser.controller";
            AnimatorController existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (existing != null)
            {
                return existing;
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Fire", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Dead", AnimatorControllerParameterType.Bool);

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState idle = machine.AddState("Idle", new Vector3(260f, 120f, 0f));
            AnimatorState walk = machine.AddState("Walk", new Vector3(520f, 120f, 0f));
            AnimatorState attack = machine.AddState("Attack", new Vector3(520f, -40f, 0f));
            AnimatorState hit = machine.AddState("Hit", new Vector3(260f, -160f, 0f));
            AnimatorState dead = machine.AddState("Dead", new Vector3(780f, -160f, 0f));

            idle.motion = LoadClip("Assets/KayKit/Characters/Animations/Animations/Rig_Medium/General/Idle_B.anim");
            walk.motion = LoadClip("Assets/KayKit/Characters/Animations/Animations/Rig_Medium/Movement Basic/Walking_A.anim");
            attack.motion = LoadClip("Assets/KayKit/Characters/Animations/Animations/Rig_Medium/Combat Melee/Melee_Unarmed_Attack_Punch_A.anim");
            hit.motion = LoadClip("Assets/KayKit/Characters/Animations/Animations/Rig_Medium/General/Hit_B.anim");
            dead.motion = LoadClip("Assets/KayKit/Characters/Animations/Animations/Rig_Medium/General/Death_B.anim");

            machine.defaultState = idle;
            AddFloatTransition(idle, walk, "Speed", AnimatorConditionMode.Greater, 0.1f);
            AddFloatTransition(walk, idle, "Speed", AnimatorConditionMode.Less, 0.1f);
            AddTriggerTransition(machine, attack, "Fire");
            AddTriggerTransition(machine, hit, "Hit");
            AddBoolTransition(machine, dead, "Dead", true);
            AddExitTransition(attack, idle, 0.85f);
            AddExitTransition(hit, idle, 0.85f);

            return controller;
        }

        private static AnimationClip LoadClip(string path)
        {
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        }

        private static GameObject CreateProjectilePrefab(Material material)
        {
            string path = Prefabs + "/Projectile_Bullet.prefab";
            GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = "Projectile_Bullet";
            projectile.transform.localScale = new Vector3(0.18f, 0.18f, 0.18f);
            projectile.GetComponent<Collider>().isTrigger = true;
            projectile.GetComponent<MeshRenderer>().sharedMaterial = material;
            projectile.AddComponent<Projectile>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(projectile, path);
            Object.DestroyImmediate(projectile);
            return prefab;
        }

        private static GameObject CreatePlayerPrefab(PlayerMovementConfig movementConfig, WeaponConfig weaponConfig, AnimatorController animatorController)
        {
            GameObject player = new GameObject("Player_Rigged");
            player.tag = "Player";

            Rigidbody body = player.AddComponent<Rigidbody>();
            body.mass = 1.4f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            CapsuleCollider collider = player.AddComponent<CapsuleCollider>();
            collider.height = 2f;
            collider.radius = 0.45f;
            collider.center = Vector3.up;

            TeamMember team = player.AddComponent<TeamMember>();
            HealthComponent health = player.AddComponent<HealthComponent>();
            PlayerInputReader input = player.AddComponent<PlayerInputReader>();
            PlayerMotor motor = player.AddComponent<PlayerMotor>();
            MouseAimController aim = player.AddComponent<MouseAimController>();
            PlayerWeaponController weaponController = player.AddComponent<PlayerWeaponController>();

            AssignEnum(team, "team", TeamId.Player);
            AssignObject(motor, "config", movementConfig);
            AssignObject(motor, "inputReader", input);
            AssignObject(aim, "config", movementConfig);

            GameObject visual = InstantiateModel(PlayerCharacterPrefab, player.transform, "Visual");
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            Animator animator = visual.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.runtimeAnimatorController = animatorController;
            }

            ProjectileWeapon weapon = CreateWeaponRig(player.transform, weaponConfig, team);
            AssignObject(weaponController, "inputReader", input);
            AssignObject(weaponController, "aimController", aim);
            AssignObject(weaponController, "weapon", weapon);

            CharacterAnimationDriver animationDriver = player.AddComponent<CharacterAnimationDriver>();
            AssignObject(animationDriver, "animator", animator);
            AssignObject(animationDriver, "trackedBody", body);
            AssignObject(animationDriver, "health", health);
            AssignObject(animationDriver, "weapon", weapon);
            AssignFloat(animationDriver, "maxSpeed", 8f);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(player, Prefabs + "/Player_Rigged.prefab");
            Object.DestroyImmediate(player);
            return prefab;
        }

        private static ProjectileWeapon CreateWeaponRig(Transform parent, WeaponConfig weaponConfig, TeamMember ownerTeam)
        {
            GameObject weaponRoot = new GameObject("Weapon_Rifle");
            weaponRoot.transform.SetParent(parent);
            weaponRoot.transform.localPosition = new Vector3(0.08f, 1.26f, 0.48f);
            weaponRoot.transform.localRotation = Quaternion.identity;
            weaponRoot.transform.localScale = Vector3.one;

            GameObject rifle = InstantiateModel(Art + "/Weapons/AK.fbx", weaponRoot.transform, "AK_Model");
            rifle.transform.localPosition = Vector3.zero;
            rifle.transform.localRotation =
                Quaternion.Euler(0f, -90f, 0f) * Quaternion.Euler(-90f, 0f, 0f);
            rifle.transform.localScale = Vector3.one * 100f;
            rifle.SetActive(weaponConfig != null);

            GameObject muzzle = new GameObject("FirePoint_Muzzle");
            muzzle.transform.SetParent(weaponRoot.transform);
            muzzle.transform.localPosition = new Vector3(0f, 0.2f, 1f);
            muzzle.transform.localRotation = Quaternion.identity;
            muzzle.AddComponent<FirePointMarker>();

            ProjectileWeapon weapon = weaponRoot.AddComponent<ProjectileWeapon>();
            AssignObject(weapon, "config", weaponConfig);
            AssignObject(weapon, "muzzle", muzzle.transform);
            AssignObject(weapon, "ownerTeam", ownerTeam);
            return weapon;
        }

        private static GameObject CreateEnemyPrefab(EnemyConfig config, AnimatorController animatorController)
        {
            GameObject enemy = new GameObject("Enemy_Chaser_Rigged");

            Rigidbody body = enemy.AddComponent<Rigidbody>();
            body.mass = 1.1f;
            body.useGravity = false;
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            CapsuleCollider collider = enemy.AddComponent<CapsuleCollider>();
            collider.height = 2f;
            collider.radius = 0.42f;
            collider.center = Vector3.up;

            TeamMember team = enemy.AddComponent<TeamMember>();
            HealthComponent health = enemy.AddComponent<HealthComponent>();
            NavMeshAgent agent = enemy.AddComponent<NavMeshAgent>();
            agent.speed = 4.5f;
            agent.acceleration = 28f;
            agent.angularSpeed = 720f;
            agent.radius = 0.42f;
            agent.height = 2f;
            agent.stoppingDistance = 1.15f;
            agent.autoBraking = true;
            agent.updateRotation = false;
            EnemyBrain brain = enemy.AddComponent<EnemyBrain>();

            AssignEnum(team, "team", TeamId.Enemy);
            AssignBool(health, "destroyOnDeath", true);
            AssignObject(brain, "config", config);

            GameObject visual = InstantiateModel(EnemyCharacterPrefab, enemy.transform, "Visual");
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
            Material zombieMaterial = AssetDatabase.LoadAssetAtPath<Material>(EnemyZombieMaterialPath);
            if (zombieMaterial != null)
            {
                foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.sharedMaterial = zombieMaterial;
                }
            }

            Animator animator = visual.GetComponentInChildren<Animator>();
            if (animator == null)
            {
                animator = visual.AddComponent<Animator>();
            }

            foreach (Object subAsset in AssetDatabase.LoadAllAssetsAtPath(EnemyCharacterPrefab))
            {
                if (subAsset is Avatar avatar)
                {
                    animator.avatar = avatar;
                    break;
                }
            }

            if (animator != null)
            {
                animator.runtimeAnimatorController = animatorController;
            }

            CharacterAnimationDriver animationDriver = enemy.AddComponent<CharacterAnimationDriver>();
            AssignObject(animationDriver, "animator", animator);
            AssignObject(animationDriver, "trackedBody", body);
            AssignObject(animationDriver, "health", health);
            AssignObject(animationDriver, "enemyBrain", brain);
            AssignObject(animationDriver, "navMeshAgent", agent);
            AssignFloat(animationDriver, "maxSpeed", 4.5f);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(enemy, Prefabs + "/Enemy_Chaser_Rigged.prefab");
            Object.DestroyImmediate(enemy);
            return prefab;
        }

        private static GameObject InstantiateModel(string path, Transform parent, string name)
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null)
            {
                GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fallback.name = name;
                fallback.transform.SetParent(parent);
                return fallback;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            instance.name = name;
            instance.transform.SetParent(parent);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            return instance;
        }

        private static void CreatePrototypeScene(GameObject playerPrefab, WaveConfig waveConfig, WeaponConfig weaponConfig, Material groundMaterial)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Dustline_Arena_01";

            GameObject lighting = new GameObject("Lighting");
            GameObject light = new GameObject("Directional Light");
            light.transform.SetParent(lighting.transform);
            Light lightComponent = light.AddComponent<Light>();
            lightComponent.type = LightType.Directional;
            lightComponent.intensity = 1.2f;
            light.transform.rotation = Quaternion.Euler(55f, -35f, 0f);

            GameObject arena = new GameObject("Arena");
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.SetParent(arena.transform);
            ground.transform.localPosition = new Vector3(0f, -0.1f, 0f);
            ground.transform.localScale = new Vector3(120f, 0.2f, 120f);
            ground.GetComponent<MeshRenderer>().sharedMaterial = groundMaterial;
            ground.GetComponent<BoxCollider>().size = new Vector3(56f / 120f, 1f, 56f / 120f);

            BuildArenaProps(arena.transform);
            EnsureRuntimeNavMeshBuilder(arena.transform);

            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.name = "Player";
            player.transform.position = new Vector3(0f, 0.1f, 0f);

            GameObject cameraObject = new GameObject("Main Camera");
            UnityEngine.Camera camera = cameraObject.AddComponent<UnityEngine.Camera>();
            camera.tag = "MainCamera";
            camera.fieldOfView = 42f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 180f;
            TopDownCameraFollow cameraFollow = cameraObject.AddComponent<TopDownCameraFollow>();
            cameraObject.transform.position = new Vector3(0f, 19f, -13f);
            AssignObject(cameraFollow, "target", player.transform);

            GameObject spawnerObject = new GameObject("Wave Spawner");
            WaveSpawner spawner = spawnerObject.AddComponent<WaveSpawner>();
            AssignBool(spawner, "playOnStart", true);
            AssignObject(spawner, "enemyTarget", player.transform);
            AssignArray(spawner, "waves", new Object[] { waveConfig });

            SpawnPoint[] points = CreateSpawnPoints();
            AssignArray(spawner, "spawnPoints", points);
            spawnerObject.SetActive(true);

            CreateHealthPickup(new Vector3(4f, 0.4f, -4f));
            CreateHealthPickup(new Vector3(-5f, 0.4f, 5f));
            CreateWeaponPickupRing(arena.transform);

            EditorSceneManager.SaveScene(scene, Scenes + "/Dustline_Arena_01.unity");
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

        private static void BuildArenaProps(Transform parent)
        {
            GameObject props = new GameObject("Props");
            props.transform.SetParent(parent);

            PlaceProp(props.transform, "Container_Long", Art + "/Environment/Container_Long.fbx", new Vector3(-13f, 0f, -8f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
            PlaceProp(props.transform, "Container_Small", Art + "/Environment/Container_Small.fbx", new Vector3(12f, 0f, 7f), Quaternion.Euler(0f, -90f, 0f), Vector3.one);
            PlaceProp(props.transform, "Barrier_Fixed_A", Art + "/Environment/Barrier_Fixed.fbx", new Vector3(-5f, 0f, -11f), Quaternion.identity, Vector3.one);
            PlaceProp(props.transform, "Barrier_Fixed_B", Art + "/Environment/Barrier_Fixed.fbx", new Vector3(5f, 0f, 11f), Quaternion.Euler(0f, 180f, 0f), Vector3.one);
            PlaceProp(props.transform, "Barrier_Large_A", Art + "/Environment/Barrier_Large.fbx", new Vector3(-11f, 0f, 5f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
            PlaceProp(props.transform, "Barrier_Large_B", Art + "/Environment/Barrier_Large.fbx", new Vector3(11f, 0f, -5f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
            PlaceProp(props.transform, "SackTrench_A", Art + "/Environment/SackTrench.fbx", new Vector3(-2f, 0f, 8f), Quaternion.Euler(0f, 25f, 0f), Vector3.one);
            PlaceProp(props.transform, "SackTrench_B", Art + "/Environment/SackTrench.fbx", new Vector3(3f, 0f, -8f), Quaternion.Euler(0f, -20f, 0f), Vector3.one);
            PlaceProp(props.transform, "Crate_A", Art + "/Environment/Crate.fbx", new Vector3(-7f, 0f, 1f), Quaternion.Euler(0f, 14f, 0f), Vector3.one);
            PlaceProp(props.transform, "Crate_B", Art + "/Environment/Crate.fbx", new Vector3(8f, 0f, -1f), Quaternion.Euler(0f, -18f, 0f), Vector3.one);
            PlaceProp(props.transform, "ExplodingBarrel", Art + "/Environment/ExplodingBarrel.fbx", new Vector3(0f, 0f, 10f), Quaternion.identity, Vector3.one);
            PlaceProp(props.transform, "TrashContainer", Art + "/Environment/TrashContainer.fbx", new Vector3(0f, 0f, -13f), Quaternion.Euler(0f, 180f, 0f), Vector3.one);
            PlaceProp(props.transform, "Pallet_A", Art + "/Environment/Pallet.fbx", new Vector3(-10f, 0f, -1f), Quaternion.Euler(0f, 40f, 0f), Vector3.one);
            PlaceProp(props.transform, "TrafficCone_A", Art + "/Environment/TrafficCone.fbx", new Vector3(6f, 0f, 3f), Quaternion.identity, Vector3.one);
            PlaceProp(props.transform, "TrafficCone_B", Art + "/Environment/TrafficCone.fbx", new Vector3(7f, 0f, 4f), Quaternion.identity, Vector3.one);
        }

        private static GameObject PlaceProp(Transform parent, string name, string path, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            GameObject prop = InstantiateModel(path, parent, name);
            prop.transform.position = position;
            prop.transform.rotation = rotation;
            prop.transform.localScale = scale;
            AddBoundsCollider(prop, false);
            if (path.EndsWith("/SackTrench.fbx"))
            {
                BoxCollider collider = prop.GetComponent<BoxCollider>();
                collider.size = new Vector3(collider.size.x * 0.91f, collider.size.y, collider.size.z * 0.4f);
            }

            return prop;
        }

        private static SpawnPoint[] CreateSpawnPoints()
        {
            GameObject root = new GameObject("Spawn Points");
            Vector3[] positions =
            {
                new Vector3(-16f, 0.35f, -16f),
                new Vector3(16f, 0.35f, -16f),
                new Vector3(-16f, 0.35f, 16f),
                new Vector3(16f, 0.35f, 16f),
                new Vector3(0f, 0.35f, 17f),
                new Vector3(0f, 0.35f, -17f)
            };

            SpawnPoint[] points = new SpawnPoint[positions.Length];
            for (int i = 0; i < points.Length; i++)
            {
                GameObject point = new GameObject($"Spawn Point {i + 1}");
                point.transform.SetParent(root.transform);
                point.transform.position = positions[i];
                points[i] = point.AddComponent<SpawnPoint>();
            }

            return points;
        }

        private static void CreateHealthPickup(Vector3 position)
        {
            GameObject pickup = InstantiateModel(Art + "/Environment/Health.fbx", null, "Health Pickup");
            pickup.transform.position = position;
            pickup.transform.localScale = Vector3.one * 1.2f;
            BoxCollider collider = pickup.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = Vector3.one * 1.2f;
            pickup.AddComponent<HealthPickup>();
        }

        private static void CreateWeaponPickupRing(Transform parent)
        {
            WeaponConfig[] configs =
            {
                AssetDatabase.LoadAssetAtPath<WeaponConfig>(Configs + "/AKWeaponConfig.asset"),
                AssetDatabase.LoadAssetAtPath<WeaponConfig>(Configs + "/SMGWeaponConfig.asset"),
                AssetDatabase.LoadAssetAtPath<WeaponConfig>(Configs + "/PistolWeaponConfig.asset"),
                AssetDatabase.LoadAssetAtPath<WeaponConfig>(Configs + "/ShotgunWeaponConfig.asset"),
                AssetDatabase.LoadAssetAtPath<WeaponConfig>(Configs + "/GrenadeWeaponConfig.asset")
            };
            GameObject[] models =
            {
                AssetDatabase.LoadAssetAtPath<GameObject>(Art + "/Weapons/AK.fbx"),
                AssetDatabase.LoadAssetAtPath<GameObject>(Art + "/Weapons/SMG.fbx"),
                AssetDatabase.LoadAssetAtPath<GameObject>(Art + "/Weapons/Pistol.fbx"),
                AssetDatabase.LoadAssetAtPath<GameObject>(Art + "/Weapons/Shotgun.fbx"),
                AssetDatabase.LoadAssetAtPath<GameObject>(Art + "/Weapons/Grenade.fbx")
            };

            WeaponPickupRingSpawner ringSpawner = parent.gameObject.AddComponent<WeaponPickupRingSpawner>();
            ringSpawner.Configure(configs, models);
        }

        private static void AddBoundsCollider(GameObject target, bool trigger)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            BoxCollider collider = target.AddComponent<BoxCollider>();
            collider.isTrigger = trigger;
            collider.center = target.transform.InverseTransformPoint(bounds.center);
            collider.size = bounds.size;
        }

        private static void AddFloatTransition(AnimatorState from, AnimatorState to, string parameter, AnimatorConditionMode mode, float threshold)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = 0.08f;
            transition.AddCondition(mode, threshold, parameter);
        }

        private static void AddTriggerTransition(AnimatorStateMachine machine, AnimatorState to, string parameter)
        {
            AnimatorStateTransition transition = machine.AddAnyStateTransition(to);
            transition.hasExitTime = false;
            transition.canTransitionToSelf = false;
            transition.duration = 0.04f;
            transition.AddCondition(AnimatorConditionMode.If, 0f, parameter);
        }

        private static void AddBoolTransition(AnimatorStateMachine machine, AnimatorState to, string parameter, bool value)
        {
            AnimatorStateTransition transition = machine.AddAnyStateTransition(to);
            transition.hasExitTime = false;
            transition.canTransitionToSelf = false;
            transition.duration = 0.05f;
            transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, parameter);
        }

        private static void AddExitTransition(AnimatorState from, AnimatorState to, float exitTime)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = true;
            transition.exitTime = exitTime;
            transition.duration = 0.08f;
        }

        private static void AssignObject(Object target, string propertyName, Object value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyName).objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignEnum(Object target, string propertyName, TeamId value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyName).enumValueIndex = (int)value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignEnum(Object target, string propertyName, WeaponVisualId value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyName).enumValueIndex = (int)value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignBool(Object target, string propertyName, bool value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyName).boolValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignFloat(Object target, string propertyName, float value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyName).floatValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignArray(Object target, string propertyName, Object[] values)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty array = serializedObject.FindProperty(propertyName);
            array.arraySize = values.Length;

            for (int i = 0; i < values.Length; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
