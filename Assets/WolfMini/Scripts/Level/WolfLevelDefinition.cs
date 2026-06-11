using System;
using System.Collections.Generic;
using UnityEngine;

namespace WolfMini.Level
{
    /// <summary>
    /// True-3D level format. Each floor is a Wolf3D-style cell grid
    /// (walls/doors/statics/enemies) placed at its own world height. The floors
    /// list and vertical opening specs exist so later iterations can add storeys
    /// and height changes without another format migration; the current game uses
    /// a single floor and no openings.
    /// </summary>
    [CreateAssetMenu(fileName = "WolfLevelDefinition", menuName = "WolfMini/Wolf Level Definition")]
    public sealed class WolfLevelDefinition : ScriptableObject
    {
        public string levelName = "Wolf Level";
        public List<GridFloorSpec> floors = new List<GridFloorSpec>();
        public List<VerticalOpeningSpec> openings = new List<VerticalOpeningSpec>();
        public GridSpawnSpec playerSpawn = new GridSpawnSpec();

        public GridFloorSpec FindFloor(string id)
        {
            if (floors == null)
            {
                return null;
            }

            for (int i = 0; i < floors.Count; i++)
            {
                GridFloorSpec floor = floors[i];
                if (floor != null && floor.id == id)
                {
                    return floor;
                }
            }

            return null;
        }

        /// <summary>Cells where the floor slab of <paramref name="floorId"/> is cut (future stair/shaft mouths).</summary>
        public bool IsFloorSlabCut(string floorId, int x, int z)
        {
            if (openings == null)
            {
                return false;
            }

            foreach (VerticalOpeningSpec opening in openings)
            {
                if (opening != null && opening.upperFloorId == floorId && opening.cells.Contains(new Vector2Int(x, z)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Cells where the ceiling of <paramref name="floorId"/> is cut (the storey above opens here).</summary>
        public bool IsCeilingCut(string floorId, int x, int z)
        {
            if (openings == null)
            {
                return false;
            }

            foreach (VerticalOpeningSpec opening in openings)
            {
                if (opening != null && opening.lowerFloorId == floorId && opening.cells.Contains(new Vector2Int(x, z)))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryValidate(out List<string> errors)
        {
            errors = GetValidationErrors();
            return errors.Count == 0;
        }

        public List<string> GetValidationErrors()
        {
            var errors = new List<string>();
            HashSet<string> floorIds = ValidateFloors(errors);
            ValidateOpenings(errors, floorIds);
            ValidateSpawn(errors, floorIds);
            return errors;
        }

        private HashSet<string> ValidateFloors(List<string> errors)
        {
            var ids = new HashSet<string>();
            if (floors == null || floors.Count == 0)
            {
                errors.Add("Level has no floors.");
                return ids;
            }

            for (int i = 0; i < floors.Count; i++)
            {
                GridFloorSpec floor = floors[i];
                if (floor == null)
                {
                    errors.Add($"Floor at index {i} is missing.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(floor.id))
                {
                    errors.Add($"Floor at index {i} has an empty id.");
                }
                else if (!ids.Add(floor.id))
                {
                    errors.Add($"Floor id '{floor.id}' is used more than once.");
                }

                if (floor.width <= 0 || floor.height <= 0)
                {
                    errors.Add($"Floor '{floor.id}' must have positive grid size.");
                }
                else if (floor.walls == null || floor.walls.Length != floor.width * floor.height)
                {
                    errors.Add($"Floor '{floor.id}' walls array must contain {floor.width * floor.height} entries.");
                }

                if (floor.ceilingHeight <= 0f)
                {
                    errors.Add($"Floor '{floor.id}' ceiling height must be positive.");
                }

                ValidateFloorEntities(errors, floor);
            }

            return ids;
        }

        private static void ValidateFloorEntities(List<string> errors, GridFloorSpec floor)
        {
            if (floor.doors != null)
            {
                foreach (GridDoorSpec door in floor.doors)
                {
                    if (door != null && !floor.InBounds(door.x, door.y))
                    {
                        errors.Add($"Door at ({door.x},{door.y}) on floor '{floor.id}' is out of bounds.");
                    }
                }
            }

            if (floor.statics != null)
            {
                foreach (GridStaticSpec item in floor.statics)
                {
                    if (item != null && !floor.InBounds(item.x, item.y))
                    {
                        errors.Add($"Static at ({item.x},{item.y}) on floor '{floor.id}' is out of bounds.");
                    }
                }
            }

            if (floor.enemies != null)
            {
                foreach (GridEnemySpec enemy in floor.enemies)
                {
                    if (enemy != null && !floor.InBounds(enemy.x, enemy.y))
                    {
                        errors.Add($"Enemy at ({enemy.x},{enemy.y}) on floor '{floor.id}' is out of bounds.");
                    }
                }
            }
        }

        private void ValidateOpenings(List<string> errors, HashSet<string> floorIds)
        {
            if (openings == null)
            {
                return;
            }

            foreach (VerticalOpeningSpec opening in openings)
            {
                if (opening == null)
                {
                    continue;
                }

                if (!floorIds.Contains(opening.upperFloorId))
                {
                    errors.Add($"Opening '{opening.id}' references missing upper floor '{opening.upperFloorId}'.");
                }

                if (!floorIds.Contains(opening.lowerFloorId))
                {
                    errors.Add($"Opening '{opening.id}' references missing lower floor '{opening.lowerFloorId}'.");
                }

                if (opening.cells.width <= 0 || opening.cells.height <= 0)
                {
                    errors.Add($"Opening '{opening.id}' must cover at least one cell.");
                }
            }
        }

        private void ValidateSpawn(List<string> errors, HashSet<string> floorIds)
        {
            if (playerSpawn == null)
            {
                errors.Add("Player spawn is missing.");
                return;
            }

            if (!floorIds.Contains(playerSpawn.floorId))
            {
                errors.Add($"Player spawn references missing floor '{playerSpawn.floorId}'.");
                return;
            }

            GridFloorSpec floor = FindFloor(playerSpawn.floorId);
            if (floor != null && !floor.InBounds(playerSpawn.cell.x, playerSpawn.cell.y))
            {
                errors.Add($"Player spawn ({playerSpawn.cell.x},{playerSpawn.cell.y}) is out of bounds on floor '{playerSpawn.floorId}'.");
            }
        }
    }

    [Serializable]
    public sealed class GridFloorSpec
    {
        public string id = "F1";
        public float y;
        public float ceilingHeight = 2f;
        public int width = 64;
        public int height = 64;

        /// <summary>Row-major grid (z * width + x): 0 empty, &gt;0 wall tile value, -1 door cell.</summary>
        public int[] walls = Array.Empty<int>();

        public List<GridDoorSpec> doors = new List<GridDoorSpec>();
        public List<GridStaticSpec> statics = new List<GridStaticSpec>();
        public List<GridEnemySpec> enemies = new List<GridEnemySpec>();
        public Color floorColor = new Color32(112, 112, 112, 255);
        public Color ceilingColor = new Color32(56, 56, 56, 255);

        public bool InBounds(int x, int z)
        {
            return x >= 0 && x < width && z >= 0 && z < height;
        }

        public int WallAt(int x, int z)
        {
            return InBounds(x, z) ? walls[z * width + x] : 1;
        }

        /// <summary>Empty or door cell — a cell the player can occupy or see through.</summary>
        public bool IsWalkable(int x, int z)
        {
            return InBounds(x, z) && walls[z * width + x] <= 0;
        }
    }

    [Serializable]
    public sealed class GridDoorSpec
    {
        public int x;
        public int y;
        public string type = "normal";
        public bool vertical;
    }

    [Serializable]
    public sealed class GridStaticSpec
    {
        public int x;
        public int y;
        public int typeIndex;
        public string typeName = "";
    }

    [Serializable]
    public sealed class GridEnemySpec
    {
        public int x;
        public int y;
        public string type = "guard";
        public int dir;
        public bool patrol;
        public int difficulty;
    }

    [Serializable]
    public sealed class VerticalOpeningSpec
    {
        public string id = "Opening";
        public string upperFloorId = "F1";
        public string lowerFloorId = "B1";
        public RectInt cells = new RectInt(0, 0, 1, 1);
    }

    [Serializable]
    public sealed class GridSpawnSpec
    {
        public string floorId = "F1";
        public Vector2Int cell;
        public int angle;
    }
}
