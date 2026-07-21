using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DustlineArena.Editor
{
    public static class EnvironmentMaterialUpgradeUtility
    {
        private const string ScenePathFormat = "Assets/_Project/Scenes/Dustline_Arena_{0:00}.unity";
        private const string SyntyMaterialsPath = "Assets/Synty/PolygonGeneric/Materials";

        private struct MaterialProfile
        {
            public string GroundSurface;
            public string Foliage;
            public string Flowers;
            public string Rock;
            public string Ground;
            public string Metal;
            public string Wood;
            public string Paper;
            public string Default;
        }

        [MenuItem("Dustline Arena/Repair Native Environment Materials")]
        public static void RepairNativeEnvironmentMaterials()
        {
            UrpSetupUtility.EnsureUrpAssigned();
            ConvertSyntyMaterialsToUrp();
            RepairArenaSceneMaterials();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Dustline Arena: native environment materials repaired and converted for URP.");
        }

        public static void RepairNativeEnvironmentMaterialsFromCommandLine()
        {
            RepairNativeEnvironmentMaterials();
        }

        private static void ConvertSyntyMaterialsToUrp()
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null)
            {
                Debug.LogWarning("Dustline Arena: URP Lit shader was not found, environment materials were not converted.");
                return;
            }

            string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { SyntyMaterialsPath });
            foreach (string guid in materialGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    continue;
                }

                ConvertMaterialToUrpLit(material, urpLit);
            }
        }

        private static void ConvertMaterialToUrpLit(Material material, Shader urpLit)
        {
            Texture albedo = GetTexture(material, "_BaseMap", "_MainTex", "_Albedo_Map");
            Texture normal = GetTexture(material, "_BumpMap", "_Normal_Map");
            Texture emission = GetTexture(material, "_EmissionMap", "_Emission_Map");
            Color baseColor = GetColor(material, "_BaseColor", "_Color");
            Color emissionColor = GetColor(material, "_EmissionColor", "_Emission_Color");
            float metallic = GetFloat(material, 0f, "_Metallic");
            float smoothness = GetFloat(material, 0.2f, "_Smoothness", "_Glossiness");
            bool alphaClip = GetFloat(material, 0f, "_AlphaClip", "_Alpha_Clip_Threshold") > 0f ||
                             material.IsKeywordEnabled("_ALPHATEST_ON");

            material.shader = urpLit;
            SetTexture(material, albedo, "_BaseMap", "_MainTex");
            SetTexture(material, normal, "_BumpMap");
            SetTexture(material, emission, "_EmissionMap");
            SetColor(material, baseColor, "_BaseColor", "_Color");
            SetColor(material, emissionColor, "_EmissionColor");
            SetFloat(material, metallic, "_Metallic");
            SetFloat(material, smoothness, "_Smoothness");
            SetFloat(material, 0f, "_Surface");
            SetFloat(material, alphaClip ? 1f : 0f, "_AlphaClip");
            SetFloat(material, 0.5f, "_Cutoff");

            if (normal != null)
            {
                material.EnableKeyword("_NORMALMAP");
            }

            if (emission != null)
            {
                material.EnableKeyword("_EMISSION");
            }

            if (alphaClip)
            {
                material.EnableKeyword("_ALPHATEST_ON");
                material.SetOverrideTag("RenderType", "TransparentCutout");
            }

            EditorUtility.SetDirty(material);
        }

        private static void RepairArenaSceneMaterials()
        {
            for (int i = 1; i <= 5; i++)
            {
                string scenePath = string.Format(ScenePathFormat, i);
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                MaterialProfile profile = GetMaterialProfile(i);

                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                    {
                        if (renderer.gameObject.name == "Ground" || renderer.gameObject.name == "Arena Ground")
                        {
                            Material groundMaterial = LoadNativeSyntyMaterialByName(profile.GroundSurface);
                            if (groundMaterial != null)
                            {
                                AssignRendererMaterial(renderer, groundMaterial);
                            }

                            continue;
                        }

                        string propName = GetGeneratedPropName(renderer.transform);
                        if (string.IsNullOrEmpty(propName))
                        {
                            continue;
                        }

                        Material material = LoadNativeSyntyMaterial(propName, profile);
                        if (material == null)
                        {
                            continue;
                        }

                        AssignRendererMaterial(renderer, material);
                    }
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        private static string GetGeneratedPropName(Transform transform)
        {
            Transform current = transform;
            string propName = string.Empty;
            while (current != null)
            {
                if (IsGeneratedMapRoot(current.name))
                {
                    return propName;
                }

                propName = current.name;
                current = current.parent;
            }

            return string.Empty;
        }

        private static bool IsGeneratedMapRoot(string name)
        {
            return name.StartsWith("Props_ArenaSceneLayout_", StringComparison.Ordinal) ||
                   name.StartsWith("Props_ExpandedArena_", StringComparison.Ordinal) ||
                   name.StartsWith("Props_MinimalArena_", StringComparison.Ordinal) ||
                   name == "Props";
        }

        private static Material LoadNativeSyntyMaterial(string propName, MaterialProfile profile)
        {
            string materialName;
            if (ContainsAny(propName, "Ground_Grass", "River_Grass", "Slope_Grass", "Road", "Ground_Dirt", "River_Dirt", "Ground_Edge", "Slope_Dirt"))
            {
                materialName = profile.Ground;
            }
            else if (ContainsAny(propName, "Bush", "Grass", "Fern", "Flowers"))
            {
                materialName = ContainsAny(propName, "Flowers")
                    ? profile.Flowers
                    : profile.Foliage;
            }
            else if (ContainsAny(propName, "Dirt_Cliff"))
            {
                materialName = profile.Ground;
            }
            else if (ContainsAny(propName, "Cliff", "Rock", "Ruin"))
            {
                materialName = profile.Rock;
            }
            else if (ContainsAny(propName, "Pipe", "Light", "Beam"))
            {
                materialName = profile.Metal;
            }
            else if (ContainsAny(propName, "Barrel_Metal"))
            {
                materialName = profile.Metal;
            }
            else if (ContainsAny(propName, "Barrel_Wood"))
            {
                materialName = profile.Wood;
            }
            else if (ContainsAny(propName, "Crate", "Box", "Pallet", "Plank"))
            {
                materialName = profile.Wood;
            }
            else if (ContainsAny(propName, "Paper"))
            {
                materialName = profile.Paper;
            }
            else
            {
                materialName = profile.Default;
            }

            return LoadNativeSyntyMaterialByName(materialName);
        }

        private static Material LoadNativeSyntyMaterialByName(string materialName)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>($"{SyntyMaterialsPath}/{materialName}.mat");
            if (material != null)
            {
                return material;
            }

            return AssetDatabase.LoadAssetAtPath<Material>($"{SyntyMaterialsPath}/Alts/{materialName}.mat");
        }

        private static MaterialProfile GetMaterialProfile(int sceneIndex)
        {
            switch (sceneIndex)
            {
                case 2:
                    return new MaterialProfile
                    {
                        GroundSurface = "Generic_Road",
                        Foliage = "Generic_Ivy",
                        Flowers = "Generic_02_C",
                        Rock = "Generic_Concrete",
                        Ground = "Generic_Road",
                        Metal = "Generic_Concrete",
                        Wood = "Generic_04_B",
                        Paper = "Generic_04_A",
                        Default = "Generic_Road"
                    };
                case 3:
                    return new MaterialProfile
                    {
                        GroundSurface = "Generic_Dirt",
                        Foliage = "Generic_Ivy",
                        Flowers = "Generic_03_C",
                        Rock = "Generic_Rock",
                        Ground = "Generic_Dirt",
                        Metal = "Generic_Brick",
                        Wood = "Generic_Wood",
                        Paper = "Generic_01_A",
                        Default = "Generic_Brick"
                    };
                case 4:
                    return new MaterialProfile
                    {
                        GroundSurface = "Generic_Triplanar_Grass_01",
                        Foliage = "Generic_Triplanar_Grass_01",
                        Flowers = "Generic_03_A",
                        Rock = "Generic_Rock",
                        Ground = "Generic_Dirt",
                        Metal = "Generic_Concrete",
                        Wood = "Generic_Wood",
                        Paper = "Generic_01_C",
                        Default = "Generic_Dirt"
                    };
                case 5:
                    return new MaterialProfile
                    {
                        GroundSurface = "Generic_Concrete",
                        Foliage = "Generic_Ivy",
                        Flowers = "Generic_04_C",
                        Rock = "Generic_Plaster",
                        Ground = "Generic_Road",
                        Metal = "Generic_Concrete",
                        Wood = "Generic_04_B",
                        Paper = "Generic_04_A",
                        Default = "Generic_Plaster"
                    };
                default:
                    return new MaterialProfile
                    {
                        GroundSurface = "Generic_Dirt",
                        Foliage = "Generic_Grass",
                        Flowers = "Generic_02_A",
                        Rock = "Generic_Rock",
                        Ground = "Generic_Dirt",
                        Metal = "Generic_Concrete",
                        Wood = "Generic_Wood",
                        Paper = "Generic_01_A",
                        Default = "Generic_Concrete"
                    };
            }
        }

        private static void AssignRendererMaterial(Renderer renderer, Material material)
        {
            Material[] materials = renderer.sharedMaterials;
            if (materials == null || materials.Length == 0)
            {
                renderer.sharedMaterial = material;
                EditorUtility.SetDirty(renderer);
                return;
            }

            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = material;
            }

            renderer.sharedMaterials = materials;
            EditorUtility.SetDirty(renderer);
        }

        private static bool ContainsAny(string value, params string[] fragments)
        {
            for (int i = 0; i < fragments.Length; i++)
            {
                if (value.Contains(fragments[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static Texture GetTexture(Material material, params string[] properties)
        {
            for (int i = 0; i < properties.Length; i++)
            {
                if (material.HasProperty(properties[i]))
                {
                    Texture texture = material.GetTexture(properties[i]);
                    if (texture != null)
                    {
                        return texture;
                    }
                }
            }

            return null;
        }

        private static Color GetColor(Material material, params string[] properties)
        {
            for (int i = 0; i < properties.Length; i++)
            {
                if (material.HasProperty(properties[i]))
                {
                    return material.GetColor(properties[i]);
                }
            }

            return Color.white;
        }

        private static float GetFloat(Material material, float fallback, params string[] properties)
        {
            for (int i = 0; i < properties.Length; i++)
            {
                if (material.HasProperty(properties[i]))
                {
                    return material.GetFloat(properties[i]);
                }
            }

            return fallback;
        }

        private static void SetTexture(Material material, Texture texture, params string[] properties)
        {
            if (texture == null)
            {
                return;
            }

            for (int i = 0; i < properties.Length; i++)
            {
                if (material.HasProperty(properties[i]))
                {
                    material.SetTexture(properties[i], texture);
                }
            }
        }

        private static void SetColor(Material material, Color color, params string[] properties)
        {
            for (int i = 0; i < properties.Length; i++)
            {
                if (material.HasProperty(properties[i]))
                {
                    material.SetColor(properties[i], color);
                }
            }
        }

        private static void SetFloat(Material material, float value, params string[] properties)
        {
            for (int i = 0; i < properties.Length; i++)
            {
                if (material.HasProperty(properties[i]))
                {
                    material.SetFloat(properties[i], value);
                }
            }
        }
    }
}
