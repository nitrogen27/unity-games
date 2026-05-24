using UnityEngine;

namespace WolfMini.Core
{
    [CreateAssetMenu(fileName = "WolfMaterialLibrary", menuName = "WolfMini/Rendering/Material Library")]
    public sealed class WolfMaterialLibrary : ScriptableObject
    {
        public const int DefaultBlueWallValue = 8;
        public const int DefaultUpperWallValue = DefaultBlueWallValue;
        public const int DefaultLowerWallValue = DefaultBlueWallValue;
        public const int DefaultDoorTileIndex = 98;
        public const int DefaultLockedDoorTileIndex = 100;
        public const int DefaultElevatorTileIndex = 102;

        private static readonly Color DefaultBlueWall = new Color32(24, 42, 132, 255);
        private static readonly Color DefaultDarkerBlueWall = DefaultBlueWall;
        private static readonly Color DefaultGrayFloor = new Color32(82, 82, 82, 255);
        private static readonly Color DefaultGrayCeiling = new Color32(112, 112, 112, 255);
        private static readonly Color DefaultTealDoor = new Color32(0, 126, 132, 255);
        private static readonly Color DefaultWoodTrim = new Color32(104, 57, 26, 255);

        [SerializeField] private Texture2D atlasTexture;
        [SerializeField] private int upperWallValue = DefaultUpperWallValue;
        [SerializeField] private int lowerWallValue = DefaultLowerWallValue;
        [SerializeField] private int doorTileIndex = DefaultDoorTileIndex;
        [SerializeField] private int lockedDoorTileIndex = DefaultLockedDoorTileIndex;
        [SerializeField] private int elevatorTileIndex = DefaultElevatorTileIndex;

        [Header("Material Asset Overrides")]
        [SerializeField] private Material blueWallMaterial;
        [SerializeField] private Material darkerBlueWallMaterial;

        [Header("Solid Fallbacks")]
        [SerializeField] private Color blueWallColor = DefaultBlueWall;
        [SerializeField] private Color darkerBlueWallColor = DefaultDarkerBlueWall;
        [SerializeField] private Color grayFloorColor = DefaultGrayFloor;
        [SerializeField] private Color grayCeilingColor = DefaultGrayCeiling;
        [SerializeField] private Color tealDoorColor = DefaultTealDoor;
        [SerializeField] private Color woodTrimColor = DefaultWoodTrim;

        private WolfAtlasMaterialCache materialCache;

        public Texture2D AtlasTexture
        {
            get => atlasTexture;
            set
            {
                atlasTexture = value;
                if (materialCache != null)
                {
                    materialCache.AtlasTexture = atlasTexture;
                }
            }
        }

        public int UpperWallValue
        {
            get => upperWallValue;
            set => upperWallValue = Mathf.Max(1, value);
        }

        public int LowerWallValue
        {
            get => lowerWallValue;
            set => lowerWallValue = Mathf.Max(1, value);
        }

        public int DoorTileIndex
        {
            get => doorTileIndex;
            set => doorTileIndex = AtlasTile.ClampIndex(value);
        }

        public int LockedDoorTileIndex
        {
            get => lockedDoorTileIndex;
            set => lockedDoorTileIndex = AtlasTile.ClampIndex(value);
        }

        public int ElevatorTileIndex
        {
            get => elevatorTileIndex;
            set => elevatorTileIndex = AtlasTile.ClampIndex(value);
        }

        public bool HasAtlasTexture => atlasTexture != null;

        public Material BlueWallMaterial
        {
            get => blueWallMaterial;
            set => blueWallMaterial = value;
        }

        public Material DarkerBlueWallMaterial
        {
            get => darkerBlueWallMaterial;
            set => darkerBlueWallMaterial = value;
        }

        public static (int lightIdx, int darkIdx) GetWallTileIndices(int wallValue)
        {
            int lightIdx = (wallValue - 1) * 2;
            return (lightIdx, lightIdx + 1);
        }

        public static void GetWallTileIndices(int wallValue, out int lightIdx, out int darkIdx)
        {
            (lightIdx, darkIdx) = GetWallTileIndices(wallValue);
        }

        public AtlasTile GetUpperWallTile(bool darkSide = false)
        {
            return GetWallTile(upperWallValue, darkSide);
        }

        public AtlasTile GetLowerWallTile(bool darkSide = false)
        {
            return GetWallTile(lowerWallValue, darkSide);
        }

        public AtlasTile GetWallTile(int wallValue, bool darkSide = false)
        {
            (int lightIdx, _) = GetWallTileIndices(wallValue);
            return AtlasTile.FromIndex(lightIdx);
        }

        public Material GetUpperWallMaterial(bool darkSide = false)
        {
            return GetWallMaterial(upperWallValue, darkSide);
        }

        public Material GetLowerWallMaterial(bool darkSide = false)
        {
            return GetWallMaterial(lowerWallValue, darkSide);
        }

        public Material GetWallMaterial(int wallValue, bool darkSide = false)
        {
            (int lightIdx, _) = GetWallTileIndices(wallValue);
            return GetAtlasOrMaterialOrFallback(
                lightIdx,
                GetWallMaterialOverride(wallValue),
                "BlueWall",
                blueWallColor);
        }

        public Material GetDoorMaterial()
        {
            return GetAtlasOrFallback(doorTileIndex, "TealDoor", tealDoorColor);
        }

        public Material GetLockedDoorMaterial()
        {
            return GetAtlasOrFallback(lockedDoorTileIndex, "LockedTealDoor", tealDoorColor);
        }

        public Material GetElevatorMaterial()
        {
            return GetAtlasOrFallback(elevatorTileIndex, "ElevatorTealDoor", tealDoorColor);
        }

        public Material GetFloorMaterial()
        {
            return Cache.GetSolidMaterial("GrayFloor", grayFloorColor);
        }

        public Material GetCeilingMaterial()
        {
            return Cache.GetSolidMaterial("GrayCeiling", grayCeilingColor);
        }

        public Material GetWoodTrimMaterial()
        {
            return Cache.GetSolidMaterial("WoodTrim", woodTrimColor);
        }

        public void ClearRuntimeCache()
        {
            if (materialCache == null)
            {
                return;
            }

            materialCache.Clear();
            materialCache = null;
        }

        private WolfAtlasMaterialCache Cache
        {
            get
            {
                if (materialCache == null)
                {
                    materialCache = new WolfAtlasMaterialCache(atlasTexture);
                }
                else if (materialCache.AtlasTexture != atlasTexture)
                {
                    materialCache.AtlasTexture = atlasTexture;
                }

                return materialCache;
            }
        }

        private Material GetAtlasOrMaterialOrFallback(int tileIndex, Material materialOverride, string fallbackName, Color fallbackColor)
        {
            if (atlasTexture != null)
            {
                return Cache.GetTileMaterial(tileIndex);
            }

            if (materialOverride != null)
            {
                return materialOverride;
            }

            return Cache.GetSolidMaterial(fallbackName, fallbackColor);
        }

        private Material GetAtlasOrFallback(int tileIndex, string fallbackName, Color fallbackColor)
        {
            return GetAtlasOrMaterialOrFallback(tileIndex, null, fallbackName, fallbackColor);
        }

        private Material GetWallMaterialOverride(int wallValue)
        {
            if (wallValue != DefaultBlueWallValue)
            {
                return null;
            }

            return blueWallMaterial != null ? blueWallMaterial : darkerBlueWallMaterial;
        }

        private void OnValidate()
        {
            upperWallValue = Mathf.Max(1, upperWallValue);
            lowerWallValue = Mathf.Max(1, lowerWallValue);
            doorTileIndex = AtlasTile.ClampIndex(doorTileIndex);
            lockedDoorTileIndex = AtlasTile.ClampIndex(lockedDoorTileIndex);
            elevatorTileIndex = AtlasTile.ClampIndex(elevatorTileIndex);
            WolfAtlasMaterialCache.ConfigurePixelTexture(atlasTexture);
        }

        private void OnDisable()
        {
            ClearRuntimeCache();
        }
    }
}
