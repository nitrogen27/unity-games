using System.Collections.Generic;
using UnityEngine;

namespace WolfMini.Core
{
    public sealed class WolfAtlasMaterialCache
    {
        private readonly Dictionary<int, Material> tileMaterials = new Dictionary<int, Material>();
        private readonly Dictionary<string, Material> solidMaterials = new Dictionary<string, Material>();
        private readonly bool preferUnlit;
        private Texture atlasTexture;

        public WolfAtlasMaterialCache(Texture atlasTexture = null, bool preferUnlit = true)
        {
            this.preferUnlit = preferUnlit;
            AtlasTexture = atlasTexture;
        }

        public Texture AtlasTexture
        {
            get => atlasTexture;
            set
            {
                if (atlasTexture == value)
                {
                    return;
                }

                ClearTileMaterials();
                atlasTexture = value;
                ConfigurePixelTexture(atlasTexture);
            }
        }

        public bool HasAtlasTexture => atlasTexture != null;

        public Material GetTileMaterial(int tileIndex)
        {
            if (atlasTexture == null)
            {
                return null;
            }

            int index = AtlasTile.ClampIndex(tileIndex);
            if (tileMaterials.TryGetValue(index, out Material material) && material != null)
            {
                return material;
            }

            material = CreateAtlasMaterial(atlasTexture, AtlasTile.FromIndex(index), preferUnlit);
            tileMaterials[index] = material;
            return material;
        }

        public Material GetSolidMaterial(string key, Color color)
        {
            string cacheKey = $"{key}:{ColorUtility.ToHtmlStringRGBA(color)}";
            if (solidMaterials.TryGetValue(cacheKey, out Material material) && material != null)
            {
                return material;
            }

            material = CreateSolidMaterial(key, color, preferUnlit);
            solidMaterials[cacheKey] = material;
            return material;
        }

        public void Clear()
        {
            ClearTileMaterials();
            ClearSolidMaterials();
        }

        public static Material CreateAtlasMaterial(Texture texture, AtlasTile tile, bool preferUnlit = true)
        {
            Material material = CreateMaterial($"WolfMini_AtlasTile_{tile.Index:000}", true, preferUnlit);
            ConfigurePixelTexture(texture);
            ApplyTexture(material, texture, tile.Offset, tile.Repeat);
            ApplyColor(material, Color.white);
            return material;
        }

        public static Material CreateSolidMaterial(string name, Color color, bool preferUnlit = true)
        {
            Material material = CreateMaterial($"WolfMini_{name}", false, preferUnlit);
            ApplyColor(material, color);
            return material;
        }

        public static void ConfigurePixelTexture(Texture texture)
        {
            if (texture == null)
            {
                return;
            }

            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.anisoLevel = 0;
        }

        private void ClearTileMaterials()
        {
            foreach (Material material in tileMaterials.Values)
            {
                DestroyMaterial(material);
            }

            tileMaterials.Clear();
        }

        private void ClearSolidMaterials()
        {
            foreach (Material material in solidMaterials.Values)
            {
                DestroyMaterial(material);
            }

            solidMaterials.Clear();
        }

        private static Material CreateMaterial(string name, bool textured, bool preferUnlit)
        {
            Shader shader = FindShader(textured, preferUnlit);
            Material material = new Material(shader)
            {
                name = name,
                hideFlags = HideFlags.DontSave
            };

            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", 0f);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0f);
            }

            if (material.HasProperty("_SpecColor"))
            {
                material.SetColor("_SpecColor", Color.black);
            }

            return material;
        }

        private static Shader FindShader(bool textured, bool preferUnlit)
        {
            if (preferUnlit)
            {
                Shader shader = FindFirstShader(textured
                    ? new[] { "Unlit/Texture", "Universal Render Pipeline/Unlit", "Legacy Shaders/Diffuse", "Diffuse", "Standard" }
                    : new[] { "Unlit/Color", "Universal Render Pipeline/Unlit", "Legacy Shaders/Diffuse", "Diffuse", "Standard" });

                if (shader != null)
                {
                    return shader;
                }
            }

            return FindFirstShader("Legacy Shaders/Diffuse", "Diffuse", "Standard", "Sprites/Default");
        }

        private static Shader FindFirstShader(params string[] shaderNames)
        {
            for (int i = 0; i < shaderNames.Length; i++)
            {
                Shader shader = Shader.Find(shaderNames[i]);
                if (shader != null)
                {
                    return shader;
                }
            }

            return Shader.Find("Standard");
        }

        private static void ApplyTexture(Material material, Texture texture, Vector2 offset, Vector2 repeat)
        {
            SetTexture(material, "_MainTex", texture, offset, repeat);
            SetTexture(material, "_BaseMap", texture, offset, repeat);
        }

        private static void SetTexture(Material material, string propertyName, Texture texture, Vector2 offset, Vector2 repeat)
        {
            if (!material.HasProperty(propertyName))
            {
                return;
            }

            material.SetTexture(propertyName, texture);
            material.SetTextureOffset(propertyName, offset);
            material.SetTextureScale(propertyName, repeat);
        }

        private static void ApplyColor(Material material, Color color)
        {
            SetColor(material, "_Color", color);
            SetColor(material, "_BaseColor", color);
        }

        private static void SetColor(Material material, string propertyName, Color color)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetColor(propertyName, color);
            }
        }

        private static void DestroyMaterial(Material material)
        {
            if (material == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(material);
            }
            else
            {
                Object.DestroyImmediate(material);
            }
        }
    }
}
