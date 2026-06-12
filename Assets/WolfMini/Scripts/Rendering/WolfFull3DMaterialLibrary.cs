using System.Collections.Generic;
using UnityEngine;

namespace WolfMini.Rendering
{
    /// <summary>
    /// Single material source for the true-3D level pipeline. Wall faces share one
    /// atlas material (UVs are written into the generated mesh), sprites share one
    /// transparent atlas material per sheet, floors/ceilings use the target-look
    /// materials when assigned and fall back to flat colors otherwise.
    /// </summary>
    [CreateAssetMenu(fileName = "WolfFull3DMaterialLibrary", menuName = "WolfMini/Full3D Material Library")]
    public sealed class WolfFull3DMaterialLibrary : ScriptableObject
    {
        public const int WallAtlasSize = 16;
        public const int SpriteAtlasSize = 16;
        public const int NormalDoorTile = 98;
        public const int DoorJambTile = 100;
        public const int ElevatorDoorTile = 102;
        public const int LockedDoorTile = 104;

        [Header("Atlases")]
        [SerializeField] private Texture2D wallsAtlas;
        [SerializeField] private Texture2D spritesAtlas;
        [SerializeField] private Texture2D guardSheet;
        [SerializeField] private Texture2D dogSheet;

        [Header("Surface materials")]
        [SerializeField] private Material wallAtlasMaterial;
        [SerializeField] private Material floorMaterial;
        [SerializeField] private Material ceilingMaterial;

        [Header("Target look overrides")]
        [SerializeField] private List<WallMaterialOverride> wallOverrides = new List<WallMaterialOverride>();
        [SerializeField] private Material doorFaceMaterial;
        [SerializeField] private Material doorJambMaterial;
        [SerializeField] private Material lampCapMaterial;
        [SerializeField] private Material lampGlowMaterial;
        [SerializeField] private Material ceilingSpillWarmMaterial;
        [SerializeField] private Material ceilingSpillCoolMaterial;

        [Header("Lamp materials")]
        [SerializeField] private Material lampWarmCap;
        [SerializeField] private Material lampGreenCap;
        [SerializeField] private Material lampWarmBulb;
        [SerializeField] private Material lampGreenBulb;

        private readonly Dictionary<int, Material> staticMaterials = new Dictionary<int, Material>();
        private readonly Dictionary<string, Material> enemyMaterials = new Dictionary<string, Material>();
        private readonly Dictionary<string, Material> fallbackColorMaterials = new Dictionary<string, Material>();

        public Texture2D WallsAtlas { get => wallsAtlas; set => wallsAtlas = value; }
        public Texture2D SpritesAtlas { get => spritesAtlas; set => spritesAtlas = value; }
        public Texture2D GuardSheet { get => guardSheet; set => guardSheet = value; }
        public Texture2D DogSheet { get => dogSheet; set => dogSheet = value; }

        public Material WallAtlasMaterial
        {
            get => wallAtlasMaterial != null ? wallAtlasMaterial : GetFallback("WallAtlas", new Color32(24, 42, 132, 255), wallsAtlas);
            set => wallAtlasMaterial = value;
        }

        public Material FloorMaterial
        {
            get => floorMaterial != null ? floorMaterial : GetFallback("Floor", new Color32(112, 112, 112, 255));
            set => floorMaterial = value;
        }

        public Material CeilingMaterial
        {
            get => ceilingMaterial != null ? ceilingMaterial : GetFallback("Ceiling", new Color32(56, 56, 56, 255));
            set => ceilingMaterial = value;
        }

        public Material LampWarmCap
        {
            get => lampWarmCap != null ? lampWarmCap : GetFallback("LampWarmCap", new Color(0.65f, 0.48f, 0.16f));
            set => lampWarmCap = value;
        }

        public Material LampGreenCap
        {
            get => lampGreenCap != null ? lampGreenCap : GetFallback("LampGreenCap", new Color(0.12f, 0.50f, 0.34f));
            set => lampGreenCap = value;
        }

        public Material LampWarmBulb
        {
            get => lampWarmBulb != null ? lampWarmBulb : GetEmissiveFallback("LampWarmBulb", new Color(1f, 0.85f, 0.45f));
            set => lampWarmBulb = value;
        }

        public Material LampGreenBulb
        {
            get => lampGreenBulb != null ? lampGreenBulb : GetEmissiveFallback("LampGreenBulb", new Color(0.36f, 1f, 0.46f));
            set => lampGreenBulb = value;
        }

        public Material DoorFaceMaterial { get => doorFaceMaterial; set => doorFaceMaterial = value; }
        public Material DoorJambMaterial { get => doorJambMaterial; set => doorJambMaterial = value; }
        public Material LampCapMaterial { get => lampCapMaterial; set => lampCapMaterial = value; }
        public Material LampGlowMaterial { get => lampGlowMaterial; set => lampGlowMaterial = value; }
        public Material CeilingSpillWarmMaterial { get => ceilingSpillWarmMaterial; set => ceilingSpillWarmMaterial = value; }
        public Material CeilingSpillCoolMaterial { get => ceilingSpillCoolMaterial; set => ceilingSpillCoolMaterial = value; }

        /// <summary>Target-look material for a wall value (e.g. blue stone, white stone), if assigned.</summary>
        public bool TryGetWallOverride(int wallValue, out Material material, out bool tileVertically)
        {
            if (wallOverrides != null)
            {
                foreach (WallMaterialOverride entry in wallOverrides)
                {
                    if (entry != null && entry.wallValue == wallValue && entry.material != null)
                    {
                        material = entry.material;
                        tileVertically = entry.tileVertically;
                        return true;
                    }
                }
            }

            material = null;
            tileVertically = true;
            return false;
        }

        public void SetWallOverride(int wallValue, Material material, bool tileVertically = true)
        {
            wallOverrides ??= new List<WallMaterialOverride>();
            foreach (WallMaterialOverride entry in wallOverrides)
            {
                if (entry != null && entry.wallValue == wallValue)
                {
                    entry.material = material;
                    entry.tileVertically = tileVertically;
                    return;
                }
            }

            wallOverrides.Add(new WallMaterialOverride { wallValue = wallValue, material = material, tileVertically = tileVertically });
        }

        /// <summary>UV rect of a wall value's light-side tile inside the walls atlas.</summary>
        public static Rect GetWallTileUV(int wallValue, bool darkSide = false)
        {
            int tileIndex = (wallValue - 1) * 2 + (darkSide ? 1 : 0);
            return GetAtlasTileUV(tileIndex, WallAtlasSize);
        }

        public static Rect GetAtlasTileUV(int tileIndex, int atlasSize)
        {
            int col = tileIndex % atlasSize;
            int row = tileIndex / atlasSize;
            float size = 1f / atlasSize;
            return new Rect(col * size, 1f - (row + 1) * size, size, size);
        }

        /// <summary>Shared sprite material for one static type (atlas window via texture offset).</summary>
        public Material GetStaticMaterial(int typeIndex, int spriteOffset)
        {
            if (staticMaterials.TryGetValue(typeIndex, out Material cached) && cached != null)
            {
                return cached;
            }

            int texIdx = typeIndex + spriteOffset;
            Material material = CreateSpriteWindowMaterial($"Static_{typeIndex:00}", spritesAtlas, SpriteAtlasSize,
                texIdx % SpriteAtlasSize, texIdx / SpriteAtlasSize, Color.white);
            staticMaterials[typeIndex] = material;
            return material;
        }

        public Material GetEnemyMaterial(string type, Color tint)
        {
            string key = $"{type}:{tint}";
            if (enemyMaterials.TryGetValue(key, out Material cached) && cached != null)
            {
                return cached;
            }

            Material material = type == "dog"
                ? CreateStripWindowMaterial("Enemy_Dog", dogSheet, 39, 0, tint)
                : CreateSpriteWindowMaterial($"Enemy_{type}", guardSheet, 8, 0, 0, tint);
            enemyMaterials[key] = material;
            return material;
        }

        private static Material CreateSpriteWindowMaterial(string label, Texture2D texture, int atlasSize, int col, int row, Color tint)
        {
            Material material = CreateTransparentMaterial(label, texture, tint);
            material.mainTextureScale = new Vector2(1f / atlasSize, 1f / atlasSize);
            material.mainTextureOffset = new Vector2(col / (float)atlasSize, 1f - (row + 1f) / atlasSize);
            return material;
        }

        private static Material CreateStripWindowMaterial(string label, Texture2D texture, int columns, int col, Color tint)
        {
            Material material = CreateTransparentMaterial(label, texture, tint);
            material.mainTextureScale = new Vector2(1f / columns, 1f);
            material.mainTextureOffset = new Vector2(col / (float)columns, 0f);
            return material;
        }

        private static Material CreateTransparentMaterial(string label, Texture2D texture, Color tint)
        {
            Shader shader = Shader.Find("Unlit/Transparent") ?? Shader.Find("Standard");
            Material material = new Material(shader)
            {
                name = $"WolfFull3D_{label}",
                mainTexture = texture,
                color = tint,
                hideFlags = HideFlags.DontSave
            };
            return material;
        }

        private Material GetFallback(string label, Color color, Texture2D texture = null)
        {
            if (fallbackColorMaterials.TryGetValue(label, out Material cached) && cached != null)
            {
                return cached;
            }

            Shader shader = texture != null
                ? Shader.Find("Unlit/Texture") ?? Shader.Find("Standard")
                : Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            Material material = new Material(shader)
            {
                name = $"WolfFull3D_Fallback_{label}",
                color = color,
                hideFlags = HideFlags.DontSave
            };
            if (texture != null)
            {
                material.mainTexture = texture;
            }

            fallbackColorMaterials[label] = material;
            return material;
        }

        private Material GetEmissiveFallback(string label, Color color)
        {
            Material material = GetFallback(label, color);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 1.7f);
            }

            return material;
        }
    }

    [System.Serializable]
    public sealed class WallMaterialOverride
    {
        public int wallValue;
        public Material material;
        // True keeps texel density stable when wall geometry gets taller.
        // False is reserved for deliberate one-off feature panels.
        public bool tileVertically = true;
    }
}
