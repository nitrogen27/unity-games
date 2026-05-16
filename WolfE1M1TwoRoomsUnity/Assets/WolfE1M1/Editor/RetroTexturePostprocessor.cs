using UnityEditor;
using UnityEngine;

namespace WolfE1M1.Editor
{
    public sealed class RetroTexturePostprocessor : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.Contains("/Resources/WolfE1M1/Textures/"))
            {
                return;
            }

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 4096;
            importer.sRGBTexture = true;
        }
    }
}
