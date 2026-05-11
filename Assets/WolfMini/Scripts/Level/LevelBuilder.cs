using System.Collections.Generic;
using WolfMini.Core;
using UnityEngine;

namespace WolfMini.Level
{
    public sealed class LevelBuilder : MonoBehaviour
    {
        private const float SlabThickness = 0.08f;
        private const float WallThickness = 0.12f;
        private const float RailingHeight = 0.85f;

        [SerializeField] private MiniLevelDefinition levelDefinition;
        [SerializeField] private WolfMaterialLibrary materialLibrary;
        [SerializeField] private bool buildOnStart = true;
        [SerializeField] private bool includeStairsAndRailings = true;

        private WolfAtlasMaterialCache fallbackMaterials;

        public MiniLevelDefinition LevelDefinition
        {
            get => levelDefinition;
            set => levelDefinition = value;
        }

        public WolfMaterialLibrary MaterialLibrary
        {
            get => materialLibrary;
            set => materialLibrary = value;
        }

        public bool BuildOnStart
        {
            get => buildOnStart;
            set => buildOnStart = value;
        }

        private WolfAtlasMaterialCache FallbackMaterials
        {
            get
            {
                fallbackMaterials ??= new WolfAtlasMaterialCache();
                return fallbackMaterials;
            }
        }

        private void Start()
        {
            if (buildOnStart)
            {
                Build();
            }
        }

        [ContextMenu("Rebuild Level")]
        public void Build()
        {
            Build(levelDefinition);
        }

        public void Build(MiniLevelDefinition definition)
        {
            if (definition == null)
            {
                Debug.LogWarning("[WolfMini] LevelBuilder has no MiniLevelDefinition assigned.", this);
                return;
            }

            levelDefinition = definition;
            Transform root = ResetGeneratedRoot();

            foreach (FloorSpec floor in definition.floors)
            {
                if (floor == null)
                {
                    continue;
                }

                BuildFloor(root, floor);
            }

            if (includeStairsAndRailings)
            {
                BuildStairs(root);
            }
        }

        private Transform ResetGeneratedRoot()
        {
            GeneratedLevelRoot existing = GetComponentInChildren<GeneratedLevelRoot>(true);
            if (existing != null)
            {
                DestroyObject(existing.gameObject);
            }

            GameObject root = new GameObject("Generated Level");
            root.transform.SetParent(transform, false);
            root.AddComponent<GeneratedLevelRoot>();
            return root.transform;
        }

        private void BuildFloor(Transform parent, FloorSpec floor)
        {
            GameObject floorRoot = new GameObject($"{floor.id} Floor");
            floorRoot.transform.SetParent(parent, false);

            Dictionary<Vector2Int, CellFlags> cells = CollectCells(floor);
            foreach (KeyValuePair<Vector2Int, CellFlags> pair in cells)
            {
                Vector2Int cell = pair.Key;
                CellFlags flags = pair.Value;
                bool insideUpperOpening = IsInsideUpperStairOpening(floor.id, cell);

                if (flags.hasFloor && !insideUpperOpening)
                {
                    CreateCellSlab(
                        floorRoot.transform,
                        $"{floor.id}_Floor_{cell.x}_{cell.y}",
                        floor.y - SlabThickness * 0.5f,
                        cell,
                        GetFloorMaterial(floor));
                }

                if (flags.hasCeiling)
                {
                    CreateCellSlab(
                        floorRoot.transform,
                        $"{floor.id}_Ceiling_{cell.x}_{cell.y}",
                        floor.y + WolfMiniConstants.WallHeight + SlabThickness * 0.5f,
                        cell,
                        GetCeilingMaterial(floor));
                }
            }

            BuildWalls(floorRoot.transform, floor, cells);
        }

        private Dictionary<Vector2Int, CellFlags> CollectCells(FloorSpec floor)
        {
            var cells = new Dictionary<Vector2Int, CellFlags>();

            if (floor.rooms != null)
            {
                foreach (RoomRect room in floor.rooms)
                {
                    if (room == null)
                    {
                        continue;
                    }

                    AddRectCells(cells, room.x, room.z, room.widthCells, room.depthCells, room.hasFloor, room.hasCeiling);
                }
            }

            if (floor.corridors != null)
            {
                foreach (CorridorRect corridor in floor.corridors)
                {
                    if (corridor == null)
                    {
                        continue;
                    }

                    AddRectCells(cells, corridor.x, corridor.z, corridor.widthCells, corridor.depthCells, true, true);
                }
            }

            return cells;
        }

        private static void AddRectCells(
            Dictionary<Vector2Int, CellFlags> cells,
            int x,
            int z,
            int width,
            int depth,
            bool hasFloor,
            bool hasCeiling)
        {
            for (int ix = x; ix < x + width; ix++)
            {
                for (int iz = z; iz < z + depth; iz++)
                {
                    var key = new Vector2Int(ix, iz);
                    cells.TryGetValue(key, out CellFlags flags);
                    flags.hasFloor |= hasFloor;
                    flags.hasCeiling |= hasCeiling;
                    cells[key] = flags;
                }
            }
        }

        private void BuildWalls(Transform parent, FloorSpec floor, Dictionary<Vector2Int, CellFlags> cells)
        {
            Material lightWall = GetWallMaterial(floor.wallValue, false);
            Material darkWall = GetWallMaterial(floor.wallValue, true);
            Vector2Int[] directions =
            {
                new Vector2Int(0, 1),
                new Vector2Int(1, 0),
                new Vector2Int(0, -1),
                new Vector2Int(-1, 0)
            };

            foreach (Vector2Int cell in cells.Keys)
            {
                for (int i = 0; i < directions.Length; i++)
                {
                    Vector2Int direction = directions[i];
                    Vector2Int neighbor = cell + direction;
                    if (cells.ContainsKey(neighbor))
                    {
                        continue;
                    }

                    bool eastWestEdge = direction.x != 0;
                    Material material = eastWestEdge ? darkWall : lightWall;
                    CreateWall(parent, $"{floor.id}_Wall_{cell.x}_{cell.y}_{i}", floor.y, cell, direction, material);
                }
            }
        }

        private void BuildStairs(Transform parent)
        {
            if (levelDefinition.stairs == null)
            {
                return;
            }

            foreach (StairSpec stair in levelDefinition.stairs)
            {
                if (stair == null)
                {
                    continue;
                }

                FloorSpec upper = levelDefinition.FindFloor(stair.upperFloorId);
                FloorSpec lower = levelDefinition.FindFloor(stair.lowerFloorId);
                if (upper == null || lower == null)
                {
                    continue;
                }

                GameObject stairRoot = new GameObject($"{stair.id} Stairs");
                stairRoot.transform.SetParent(parent, false);

                CreateStairSteps(stairRoot.transform, stair, upper.y, lower.y);

                if (stair.addRailings)
                {
                    CreateUpperOpeningRailings(stairRoot.transform, stair, upper.y);
                }

                if (stair.addSideWalls)
                {
                    CreateStairSideTrims(stairRoot.transform, stair, upper.y, lower.y);
                }
            }
        }

        private void CreateStairSteps(Transform parent, StairSpec stair, float upperY, float lowerY)
        {
            const int stepCount = 12;
            float totalHeight = upperY - lowerY;
            float rise = totalHeight / stepCount;
            float run = WolfMiniConstants.CellSize * 0.42f;
            float width = GetStairWidth(stair) - 0.2f;
            Vector3 direction = DirectionToVector(stair.direction);
            Vector3 right = new Vector3(direction.z, 0f, -direction.x);
            Vector3 start = OpeningCenter(stair.upperOpeningCells);
            start -= direction * (run * 0.5f);

            for (int i = 0; i < stepCount; i++)
            {
                float topY = upperY - rise * (i + 1);
                float height = Mathf.Max(0.08f, topY - lowerY);
                Vector3 center = start + direction * (run * (i + 0.5f));
                center.y = lowerY + height * 0.5f;

                Vector3 scale = Mathf.Abs(direction.z) > 0f
                    ? new Vector3(width, height, run)
                    : new Vector3(run, height, width);

                CreateCube(parent, $"Step_{i:00}", center, scale, GetSolidMaterial("StairStone", new Color32(74, 74, 74, 255)));
            }

            Vector3 landingCenter = RectCenter(stair.lowerLandingCells);
            landingCenter.y = lowerY - SlabThickness * 0.5f;
            CreateCube(
                parent,
                "Lower Landing Patch",
                landingCenter,
                new Vector3(stair.lowerLandingCells.width * WolfMiniConstants.CellSize, SlabThickness, stair.lowerLandingCells.height * WolfMiniConstants.CellSize),
                GetSolidMaterial("LowerLanding", new Color32(76, 76, 76, 255)));

            _ = right;
        }

        private void CreateUpperOpeningRailings(Transform parent, StairSpec stair, float upperY)
        {
            RectInt rect = stair.upperOpeningCells;
            float xMin = rect.xMin * WolfMiniConstants.CellSize;
            float xMax = rect.xMax * WolfMiniConstants.CellSize;
            float zMin = rect.yMin * WolfMiniConstants.CellSize;
            float zMax = rect.yMax * WolfMiniConstants.CellSize;
            float y = upperY + RailingHeight;
            float railThickness = 0.12f;
            Material rail = GetWoodTrimMaterial();

            bool skipNorth = stair.direction == StairDirection.South;
            bool skipSouth = stair.direction == StairDirection.North;
            bool skipWest = stair.direction == StairDirection.East;
            bool skipEast = stair.direction == StairDirection.West;

            if (!skipNorth)
            {
                CreateCube(parent, "Railing North", new Vector3((xMin + xMax) * 0.5f, y, zMin), new Vector3(xMax - xMin, railThickness, railThickness), rail);
            }

            if (!skipSouth)
            {
                CreateCube(parent, "Railing South", new Vector3((xMin + xMax) * 0.5f, y, zMax), new Vector3(xMax - xMin, railThickness, railThickness), rail);
            }

            if (!skipWest)
            {
                CreateCube(parent, "Railing West", new Vector3(xMin, y, (zMin + zMax) * 0.5f), new Vector3(railThickness, railThickness, zMax - zMin), rail);
            }

            if (!skipEast)
            {
                CreateCube(parent, "Railing East", new Vector3(xMax, y, (zMin + zMax) * 0.5f), new Vector3(railThickness, railThickness, zMax - zMin), rail);
            }
        }

        private void CreateStairSideTrims(Transform parent, StairSpec stair, float upperY, float lowerY)
        {
            RectInt rect = stair.upperOpeningCells;
            Vector3 center = OpeningCenter(rect);
            Vector3 direction = DirectionToVector(stair.direction);
            Vector3 side = new Vector3(direction.z, 0f, -direction.x);
            float length = WolfMiniConstants.CellSize * Mathf.Max(rect.width, rect.height);
            float height = Mathf.Abs(upperY - lowerY);
            float y = lowerY + height * 0.5f;
            float sideOffset = GetStairWidth(stair) * 0.5f;
            Material material = GetWallMaterial(8, true);

            Vector3 scale = Mathf.Abs(direction.z) > 0f
                ? new Vector3(WallThickness, height, length)
                : new Vector3(length, height, WallThickness);

            CreateCube(parent, "Stair Side Trim A", new Vector3(center.x, y, center.z) + side * sideOffset, scale, material);
            CreateCube(parent, "Stair Side Trim B", new Vector3(center.x, y, center.z) - side * sideOffset, scale, material);
        }

        private bool IsInsideUpperStairOpening(string floorId, Vector2Int cell)
        {
            if (levelDefinition == null || levelDefinition.stairs == null)
            {
                return false;
            }

            foreach (StairSpec stair in levelDefinition.stairs)
            {
                if (stair == null || stair.upperFloorId != floorId)
                {
                    continue;
                }

                if (stair.upperOpeningCells.Contains(cell))
                {
                    return true;
                }
            }

            return false;
        }

        private void CreateCellSlab(Transform parent, string name, float centerY, Vector2Int cell, Material material)
        {
            Vector3 center = CellCenter(cell, centerY);
            CreateCube(parent, name, center, new Vector3(WolfMiniConstants.CellSize, SlabThickness, WolfMiniConstants.CellSize), material);
        }

        private void CreateWall(Transform parent, string name, float floorY, Vector2Int cell, Vector2Int direction, Material material)
        {
            float cellSize = WolfMiniConstants.CellSize;
            Vector3 center = CellCenter(cell, floorY + WolfMiniConstants.WallHeight * 0.5f);
            Vector3 scale;

            if (direction.x > 0)
            {
                center.x += cellSize * 0.5f;
                scale = new Vector3(WallThickness, WolfMiniConstants.WallHeight, cellSize + WallThickness);
            }
            else if (direction.x < 0)
            {
                center.x -= cellSize * 0.5f;
                scale = new Vector3(WallThickness, WolfMiniConstants.WallHeight, cellSize + WallThickness);
            }
            else if (direction.y > 0)
            {
                center.z += cellSize * 0.5f;
                scale = new Vector3(cellSize + WallThickness, WolfMiniConstants.WallHeight, WallThickness);
            }
            else
            {
                center.z -= cellSize * 0.5f;
                scale = new Vector3(cellSize + WallThickness, WolfMiniConstants.WallHeight, WallThickness);
            }

            CreateCube(parent, name, center, scale, material);
        }

        private Material GetWallMaterial(int wallValue, bool darkSide)
        {
            if (materialLibrary != null)
            {
                return materialLibrary.GetWallMaterial(wallValue, darkSide);
            }

            Color color = darkSide ? new Color32(11, 22, 78, 255) : new Color32(24, 42, 132, 255);
            return FallbackMaterials.GetSolidMaterial(darkSide ? "DarkerBlueWall" : "BlueWall", color);
        }

        private Material GetFloorMaterial(FloorSpec floor)
        {
            if (materialLibrary != null)
            {
                return materialLibrary.GetFloorMaterial();
            }

            return FallbackMaterials.GetSolidMaterial($"{floor.id}_Floor", floor.floorColor);
        }

        private Material GetCeilingMaterial(FloorSpec floor)
        {
            if (materialLibrary != null)
            {
                return materialLibrary.GetCeilingMaterial();
            }

            return FallbackMaterials.GetSolidMaterial($"{floor.id}_Ceiling", floor.ceilingColor);
        }

        private Material GetWoodTrimMaterial()
        {
            return materialLibrary != null
                ? materialLibrary.GetWoodTrimMaterial()
                : FallbackMaterials.GetSolidMaterial("WoodTrim", new Color32(104, 57, 26, 255));
        }

        private Material GetSolidMaterial(string name, Color color)
        {
            return FallbackMaterials.GetSolidMaterial(name, color);
        }

        private static Vector3 CellCenter(Vector2Int cell, float y)
        {
            float cellSize = WolfMiniConstants.CellSize;
            return new Vector3((cell.x + 0.5f) * cellSize, y, (cell.y + 0.5f) * cellSize);
        }

        private static Vector3 RectCenter(RectInt rect)
        {
            float cellSize = WolfMiniConstants.CellSize;
            return new Vector3((rect.x + rect.width * 0.5f) * cellSize, 0f, (rect.y + rect.height * 0.5f) * cellSize);
        }

        private static Vector3 OpeningCenter(RectInt rect)
        {
            return RectCenter(rect);
        }

        private static Vector3 DirectionToVector(StairDirection direction)
        {
            return direction switch
            {
                StairDirection.North => Vector3.back,
                StairDirection.South => Vector3.forward,
                StairDirection.East => Vector3.right,
                StairDirection.West => Vector3.left,
                _ => Vector3.forward
            };
        }

        private static float GetStairWidth(StairSpec stair)
        {
            return (stair.direction == StairDirection.North || stair.direction == StairDirection.South)
                ? stair.upperOpeningCells.width * WolfMiniConstants.CellSize
                : stair.upperOpeningCells.height * WolfMiniConstants.CellSize;
        }

        private static GameObject CreateCube(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.position = position;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            return cube;
        }

        private static void DestroyObject(Object value)
        {
            if (value == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(value);
            }
            else
            {
                DestroyImmediate(value);
            }
        }

        private struct CellFlags
        {
            public bool hasFloor;
            public bool hasCeiling;
        }
    }
}
