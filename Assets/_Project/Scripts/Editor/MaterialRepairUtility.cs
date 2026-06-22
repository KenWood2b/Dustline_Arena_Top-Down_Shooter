using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DustlineArena.Editor
{
    public static class MaterialRepairUtility
    {
        private const string MaterialsFolder = "Assets/_Project/Materials";
        private const string ScenePath = "Assets/_Project/Scenes/Dustline_Arena_01.unity";
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player_Rigged.prefab";
        private const string EnemyPrefabPath = "Assets/_Project/Prefabs/Enemy_Chaser_Rigged.prefab";

        [MenuItem("Dustline Arena/Repair Scene Materials")]
        public static void RepairSceneMaterials()
        {
            UrpSetupUtility.EnsureUrpAssigned();

            Material floor = CreateMaterial("M_ArenaFloor_Concrete", new Color(0.28f, 0.31f, 0.32f), 0.12f, 0.18f);
            Material player = CreateMaterial("M_Player_Suit_Blue", new Color(0.12f, 0.42f, 0.86f), 0.08f, 0.28f);
            Material enemy = CreateMaterial("M_Enemy_Suit_Red", new Color(0.82f, 0.18f, 0.12f), 0.06f, 0.22f);
            Material weapon = CreateMaterial("M_Weapon_DarkMetal", new Color(0.12f, 0.13f, 0.14f), 0.25f, 0.42f);

            ApplyCharacterMaterials(PlayerPrefabPath, player, weapon);
            ApplyCharacterMaterials(EnemyPrefabPath, enemy, weapon);
            ApplySceneFloorMaterial(ScenePath, floor);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static Material CreateMaterial(string name, Color color, float metallic, float smoothness)
        {
            if (!AssetDatabase.IsValidFolder(MaterialsFolder))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Materials");
            }

            string path = $"{MaterialsFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = Shader.Find("Universal Render Pipeline/Lit") ?? material.shader;
            material.SetColor("_BaseColor", color);
            material.SetColor("_Color", color);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ApplyCharacterMaterials(string prefabPath, Material bodyMaterial, Material weaponMaterial)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null)
            {
                return;
            }

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (IsWeaponRenderer(renderer.transform))
                {
                    renderer.sharedMaterial = weaponMaterial;
                    EditorUtility.SetDirty(renderer);
                }
                else if (NeedsRepair(renderer.sharedMaterial))
                {
                    renderer.sharedMaterial = bodyMaterial;
                    EditorUtility.SetDirty(renderer);
                }
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static bool IsWeaponRenderer(Transform transform)
        {
            while (transform != null)
            {
                if (transform.name.Contains("Weapon") || transform.name.Contains("SMG") || transform.name.Contains("Rifle"))
                {
                    return true;
                }

                transform = transform.parent;
            }

            return false;
        }

        private static bool NeedsRepair(Material material)
        {
            return material == null || material.shader == null || material.shader.name == "Hidden/InternalErrorShader";
        }

        private static void ApplySceneFloorMaterial(string scenePath, Material floorMaterial)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer.gameObject.name == "Ground" || renderer.gameObject.name == "Arena Ground")
                    {
                        renderer.sharedMaterial = floorMaterial;
                        EditorUtility.SetDirty(renderer);
                    }
                }
            }

            EditorSceneManager.SaveScene(scene);
        }
    }
}
