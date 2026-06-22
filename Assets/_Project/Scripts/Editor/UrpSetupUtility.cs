using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DustlineArena.Editor
{
    public static class UrpSetupUtility
    {
        private const string SettingsFolder = "Assets/_Project/Settings";
        private const string RendererPath = SettingsFolder + "/Dustline_UniversalRenderer.asset";
        private const string PipelinePath = SettingsFolder + "/Dustline_URP.asset";

        [MenuItem("Dustline Arena/Setup URP")]
        public static void SetupUrp()
        {
            EnsureUrpAssigned();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static UniversalRenderPipelineAsset EnsureUrpAssigned()
        {
            EnsureSettingsFolder();

            UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, RendererPath);
            }

            UniversalRenderPipelineAsset pipelineAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipelineAsset == null)
            {
                pipelineAsset = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(pipelineAsset, PipelinePath);
            }

            GraphicsSettings.defaultRenderPipeline = pipelineAsset;

            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipelineAsset;
            }

            EditorUtility.SetDirty(pipelineAsset);
            EditorUtility.SetDirty(rendererData);
            return pipelineAsset;
        }

        private static void EnsureSettingsFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project"))
            {
                AssetDatabase.CreateFolder("Assets", "_Project");
            }

            if (!AssetDatabase.IsValidFolder(SettingsFolder))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Settings");
            }
        }
    }
}
