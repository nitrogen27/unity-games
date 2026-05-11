using System;
using UnityEditor;
using UnityEngine;

namespace WolfMini.EditorTools
{
    public sealed class WolfMiniPixelImportPostprocessor : AssetPostprocessor
    {
        private const string ArtRoot = "Assets/WolfMini/Art";
        private const string TextureRoot = ArtRoot + "/Textures/";
        private const string SpriteRoot = ArtRoot + "/Sprites/";
        private const string UiRoot = ArtRoot + "/UI/";
        private const int SpritePixelsPerUnit = 64;

        private void OnPreprocessTexture()
        {
            TextureImporter textureImporter = assetImporter as TextureImporter;
            if (textureImporter == null)
            {
                return;
            }

            if (!IsWolfMiniPixelTexture(assetPath))
            {
                return;
            }

            ApplyPixelSettings(textureImporter, assetPath);
        }

        [MenuItem("WolfMini/Apply Pixel Import Settings")]
        private static void ApplyPixelImportSettings()
        {
            string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { ArtRoot });
            string[] texturePaths = Array.ConvertAll(textureGuids, AssetDatabase.GUIDToAssetPath);
            Array.Sort(texturePaths, StringComparer.Ordinal);

            int reimportedCount = 0;

            try
            {
                AssetDatabase.StartAssetEditing();

                foreach (string texturePath in texturePaths)
                {
                    if (!IsWolfMiniPixelTexture(texturePath))
                    {
                        continue;
                    }

                    AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate);
                    reimportedCount++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            Debug.Log($"WolfMini pixel import settings applied to {reimportedCount} texture asset(s).");
        }

        private static void ApplyPixelSettings(TextureImporter textureImporter, string assetPath)
        {
            bool isSpriteOrUi = IsSpriteOrUiTexture(assetPath);

            textureImporter.filterMode = FilterMode.Point;
            textureImporter.mipmapEnabled = false;
            textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
            textureImporter.wrapMode = TextureWrapMode.Clamp;

            if (!isSpriteOrUi)
            {
                return;
            }

            textureImporter.textureType = TextureImporterType.Sprite;
            textureImporter.spritePixelsPerUnit = SpritePixelsPerUnit;
            textureImporter.alphaIsTransparency = true;
        }

        private static bool IsWolfMiniPixelTexture(string assetPath)
        {
            return assetPath.StartsWith(TextureRoot, StringComparison.Ordinal)
                || IsSpriteOrUiTexture(assetPath);
        }

        private static bool IsSpriteOrUiTexture(string assetPath)
        {
            return assetPath.StartsWith(SpriteRoot, StringComparison.Ordinal)
                || assetPath.StartsWith(UiRoot, StringComparison.Ordinal);
        }
    }
}
