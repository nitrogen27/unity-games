using System;
using System.Collections.Generic;
using WolfMini.Core;
using UnityEngine;

namespace WolfMini.Level
{
    [CreateAssetMenu(fileName = "MiniLevelDefinition", menuName = "WolfMini/Mini Level Definition")]
    public sealed class MiniLevelDefinition : ScriptableObject
    {
        public string levelName = "WolfMini Two Floor Demo";
        public WolfMaterialLibrary materialLibrary;
        public List<FloorSpec> floors = new List<FloorSpec>();
        public List<DoorSpec> doors = new List<DoorSpec>();
        public List<StairSpec> stairs = new List<StairSpec>();
        public List<DecorSpec> decor = new List<DecorSpec>();
        public PlayerSpawnSpec playerSpawn = new PlayerSpawnSpec();

        public bool TryValidate(out List<string> errors)
        {
            errors = GetValidationErrors();
            return errors.Count == 0;
        }

        public bool IsValid()
        {
            return GetValidationErrors().Count == 0;
        }

        public List<string> GetValidationErrors()
        {
            var errors = new List<string>();
            var floorIds = BuildFloorIdSet(errors);

            ValidateFloorGeometry(errors);
            ValidateDoors(errors, floorIds);
            ValidateStairs(errors, floorIds);
            ValidateDecor(errors, floorIds);
            ValidatePlayerSpawn(errors, floorIds);

            return errors;
        }

        public FloorSpec FindFloor(string id)
        {
            if (floors == null)
            {
                return null;
            }

            for (int i = 0; i < floors.Count; i++)
            {
                FloorSpec floor = floors[i];
                if (floor != null && floor.id == id)
                {
                    return floor;
                }
            }

            return null;
        }

        public bool IsCellInAuthoredSpace(string floorId, int x, int z)
        {
            FloorSpec floor = FindFloor(floorId);
            return floor != null && IsCellInsideAnySpace(floor, x, z);
        }

        private HashSet<string> BuildFloorIdSet(List<string> errors)
        {
            var ids = new HashSet<string>();
            if (floors == null)
            {
                errors.Add("Floors list is missing.");
                return ids;
            }

            for (int i = 0; i < floors.Count; i++)
            {
                FloorSpec floor = floors[i];
                if (floor == null)
                {
                    errors.Add($"Floor at index {i} is missing.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(floor.id))
                {
                    errors.Add($"Floor at index {i} has an empty id.");
                    continue;
                }

                if (!ids.Add(floor.id))
                {
                    errors.Add($"Floor id '{floor.id}' is used more than once.");
                }
            }

            return ids;
        }

        private void ValidateFloorGeometry(List<string> errors)
        {
            if (floors == null)
            {
                return;
            }

            foreach (FloorSpec floor in floors)
            {
                if (floor == null)
                {
                    continue;
                }

                ValidateRooms(errors, floor);
                ValidateCorridors(errors, floor);
            }
        }

        private static void ValidateRooms(List<string> errors, FloorSpec floor)
        {
            if (floor.rooms == null)
            {
                errors.Add($"Floor '{floor.id}' rooms list is missing.");
                return;
            }

            for (int i = 0; i < floor.rooms.Count; i++)
            {
                RoomRect room = floor.rooms[i];
                if (room == null)
                {
                    errors.Add($"Floor '{floor.id}' room at index {i} is missing.");
                    continue;
                }

                if (room.widthCells <= 0 || room.depthCells <= 0)
                {
                    errors.Add($"Room '{room.id}' on floor '{floor.id}' must have positive size.");
                }
            }
        }

        private static void ValidateCorridors(List<string> errors, FloorSpec floor)
        {
            if (floor.corridors == null)
            {
                errors.Add($"Floor '{floor.id}' corridors list is missing.");
                return;
            }

            for (int i = 0; i < floor.corridors.Count; i++)
            {
                CorridorRect corridor = floor.corridors[i];
                if (corridor == null)
                {
                    errors.Add($"Floor '{floor.id}' corridor at index {i} is missing.");
                    continue;
                }

                if (corridor.widthCells <= 0 || corridor.depthCells <= 0)
                {
                    errors.Add($"Corridor '{corridor.id}' on floor '{floor.id}' must have positive size.");
                }
            }
        }

        private void ValidateDoors(List<string> errors, HashSet<string> floorIds)
        {
            if (doors == null)
            {
                errors.Add("Doors list is missing.");
                return;
            }

            for (int i = 0; i < doors.Count; i++)
            {
                DoorSpec door = doors[i];
                if (door == null)
                {
                    errors.Add($"Door at index {i} is missing.");
                    continue;
                }

                if (!floorIds.Contains(door.floorId))
                {
                    errors.Add($"Door '{door.id}' references missing floor '{door.floorId}'.");
                    continue;
                }

                FloorSpec floor = FindFloor(door.floorId);
                if (floor != null && !IsCellInsideOrAdjacentToAnySpace(floor, door.cell.x, door.cell.y))
                {
                    errors.Add($"Door '{door.id}' on floor '{door.floorId}' is not inside or adjacent to any room/corridor.");
                }
            }
        }

        private void ValidateStairs(List<string> errors, HashSet<string> floorIds)
        {
            if (stairs == null)
            {
                errors.Add("Stairs list is missing.");
                return;
            }

            for (int i = 0; i < stairs.Count; i++)
            {
                StairSpec stair = stairs[i];
                if (stair == null)
                {
                    errors.Add($"Stair at index {i} is missing.");
                    continue;
                }

                if (!floorIds.Contains(stair.upperFloorId))
                {
                    errors.Add($"Stair '{stair.id}' references missing upper floor '{stair.upperFloorId}'.");
                }

                if (!floorIds.Contains(stair.lowerFloorId))
                {
                    errors.Add($"Stair '{stair.id}' references missing lower floor '{stair.lowerFloorId}'.");
                }

                if (stair.upperOpeningCells.width <= 0 || stair.upperOpeningCells.height <= 0)
                {
                    errors.Add($"Stair '{stair.id}' upper opening must have positive size.");
                }

                if (stair.lowerLandingCells.width <= 0 || stair.lowerLandingCells.height <= 0)
                {
                    errors.Add($"Stair '{stair.id}' lower landing must have positive size.");
                }
            }
        }

        private void ValidateDecor(List<string> errors, HashSet<string> floorIds)
        {
            if (decor == null)
            {
                errors.Add("Decor list is missing.");
                return;
            }

            for (int i = 0; i < decor.Count; i++)
            {
                DecorSpec item = decor[i];
                if (item == null)
                {
                    errors.Add($"Decor at index {i} is missing.");
                    continue;
                }

                if (!floorIds.Contains(item.floorId))
                {
                    errors.Add($"Decor '{item.id}' references missing floor '{item.floorId}'.");
                }
            }
        }

        private void ValidatePlayerSpawn(List<string> errors, HashSet<string> floorIds)
        {
            if (playerSpawn == null)
            {
                errors.Add("Player spawn is missing.");
                return;
            }

            if (!floorIds.Contains(playerSpawn.floorId))
            {
                errors.Add($"Player spawn references missing floor '{playerSpawn.floorId}'.");
            }
        }

        private static bool IsCellInsideAnySpace(FloorSpec floor, int x, int z)
        {
            if (floor.rooms != null)
            {
                foreach (RoomRect room in floor.rooms)
                {
                    if (room != null && room.ContainsCell(x, z))
                    {
                        return true;
                    }
                }
            }

            if (floor.corridors != null)
            {
                foreach (CorridorRect corridor in floor.corridors)
                {
                    if (corridor != null && corridor.ContainsCell(x, z))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsCellInsideOrAdjacentToAnySpace(FloorSpec floor, int x, int z)
        {
            if (floor.rooms != null)
            {
                foreach (RoomRect room in floor.rooms)
                {
                    if (room != null && room.ContainsOrEdgeAdjacentToCell(x, z))
                    {
                        return true;
                    }
                }
            }

            if (floor.corridors != null)
            {
                foreach (CorridorRect corridor in floor.corridors)
                {
                    if (corridor != null && corridor.ContainsOrEdgeAdjacentToCell(x, z))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }

    [Serializable]
    public sealed class FloorSpec
    {
        public string id = "Floor";
        public float y;
        public List<RoomRect> rooms = new List<RoomRect>();
        public List<CorridorRect> corridors = new List<CorridorRect>();
        public int wallValue = 8;
        public Color floorColor = new Color32(82, 82, 82, 255);
        public Color ceilingColor = new Color32(76, 76, 76, 255);
    }

    [Serializable]
    public sealed class RoomRect
    {
        public string id = "Room";
        public int x;
        public int z;
        public int widthCells = 1;
        public int depthCells = 1;
        public bool hasCeiling = true;
        public bool hasFloor = true;

        public bool ContainsCell(int cellX, int cellZ)
        {
            return widthCells > 0
                && depthCells > 0
                && cellX >= x
                && cellX < x + widthCells
                && cellZ >= z
                && cellZ < z + depthCells;
        }

        public bool ContainsOrEdgeAdjacentToCell(int cellX, int cellZ)
        {
            if (widthCells <= 0 || depthCells <= 0)
            {
                return false;
            }

            return ContainsCell(cellX, cellZ)
                || (cellZ >= z && cellZ < z + depthCells && (cellX == x - 1 || cellX == x + widthCells))
                || (cellX >= x && cellX < x + widthCells && (cellZ == z - 1 || cellZ == z + depthCells));
        }
    }

    [Serializable]
    public sealed class CorridorRect
    {
        public string id = "Corridor";
        public int x;
        public int z;
        public int widthCells = 1;
        public int depthCells = 1;

        public bool ContainsCell(int cellX, int cellZ)
        {
            return widthCells > 0
                && depthCells > 0
                && cellX >= x
                && cellX < x + widthCells
                && cellZ >= z
                && cellZ < z + depthCells;
        }

        public bool ContainsOrEdgeAdjacentToCell(int cellX, int cellZ)
        {
            if (widthCells <= 0 || depthCells <= 0)
            {
                return false;
            }

            return ContainsCell(cellX, cellZ)
                || (cellZ >= z && cellZ < z + depthCells && (cellX == x - 1 || cellX == x + widthCells))
                || (cellX >= x && cellX < x + widthCells && (cellZ == z - 1 || cellZ == z + depthCells));
        }
    }

    [Serializable]
    public sealed class DoorSpec
    {
        public string id = "Door";
        public string floorId = "Upper";
        public Vector2Int cell;
        public DoorOrientation orientation = DoorOrientation.NorthSouth;
        public string connectsA;
        public string connectsB;
        public DoorType type = DoorType.Normal;
        public bool startsOpen;
    }

    [Serializable]
    public sealed class StairSpec
    {
        public string id = "Stair";
        public string upperFloorId = "Upper";
        public string lowerFloorId = "Lower";
        public RectInt upperOpeningCells = new RectInt(0, 0, 2, 3);
        public RectInt lowerLandingCells = new RectInt(0, 0, 5, 5);
        public StairDirection direction = StairDirection.South;
        public bool addRailings = true;
        public bool addSideWalls = true;
    }

    [Serializable]
    public sealed class DecorSpec
    {
        public string id = "Decor";
        public string floorId = "Upper";
        public DecorType type = DecorType.CeilingLight;
        public Vector2 cell;
        public float rotationY;
        public bool blocking;
    }

    [Serializable]
    public sealed class PlayerSpawnSpec
    {
        public string floorId = "Upper";
        public Vector2 cell;
        public float rotationY;
    }

    public enum DoorOrientation
    {
        NorthSouth,
        EastWest
    }

    public enum DoorType
    {
        Normal,
        LockedGold,
        LockedSilver,
        Elevator
    }

    public enum StairDirection
    {
        North,
        South,
        East,
        West
    }

    public enum DecorType
    {
        CeilingLight,
        Chandelier,
        Barrel,
        Table,
        Plant,
        Column,
        Railing,
        Sign
    }
}
