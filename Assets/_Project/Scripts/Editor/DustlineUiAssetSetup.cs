using UnityEditor;
using UnityEngine;

namespace DustlineArena.Editor
{
    [InitializeOnLoad]
    internal static class DustlineUiAssetSetup
    {
        private const string UiRoot = "Assets/_Project/UI/Resources/DustlineUI/";

        static DustlineUiAssetSetup()
        {
            EditorApplication.delayCall += ConfigureTextures;
        }

        private static void ConfigureTextures()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { UiRoot.TrimEnd('/') });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(path) is TextureImporter importer))
                {
                    continue;
                }

                Vector4 border = GetBorder(path);
                bool changed = importer.textureType != TextureImporterType.Sprite
                    || importer.spriteImportMode != SpriteImportMode.Single
                    || importer.mipmapEnabled
                    || !importer.alphaIsTransparency
                    || importer.wrapMode != TextureWrapMode.Clamp
                    || importer.spriteBorder != border;
                if (!changed)
                {
                    continue;
                }

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.maxTextureSize = 1024;
                importer.spritePixelsPerUnit = 100f;
                importer.spriteBorder = border;
                importer.SaveAndReimport();
            }
        }

        private static Vector4 GetBorder(string path)
        {
            if (path.EndsWith("GrenadeRing.png"))
            {
                return Vector4.zero;
            }

            if (path.EndsWith("HealthFill.png") || path.EndsWith("BarFull.png"))
            {
                return new Vector4(24f, 24f, 24f, 24f);
            }

            return new Vector4(28f, 28f, 28f, 28f);
        }
    }
}
