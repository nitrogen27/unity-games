using System;
using System.Collections.Generic;
using UnityEngine;

namespace WolfMini.Level
{
    /// <summary>
    /// Grid-free, world-space level model for the Full3D pipeline. Geometry is
    /// described by sectors (floor/ceiling areas), merged wall segments and
    /// doorways, all in meters, so levels are no longer authored or stored as a
    /// Wolf3D cell grid. Storeys stack by giving sectors different floor
    /// heights; stairwell specs cut openings into a floor plane and run
    /// straight stairs down to the storey below.
    /// </summary>
    [CreateAssetMenu(fileName = "WolfSectorLevelDefinition", menuName = "WolfMini/Wolf Sector Level Definition")]
    public sealed class WolfSectorLevelDefinition : ScriptableObject
    {
        public string levelName = "Wolf Sector Level";
        public List<SectorSpec> sectors = new List<SectorSpec>();
        public List<WallSegmentSpec> walls = new List<WallSegmentSpec>();
        public List<DoorwaySpec> doorways = new List<DoorwaySpec>();
        public List<StairwellSpec> stairwells = new List<StairwellSpec>();
        public List<LevelPropSpec> props = new List<LevelPropSpec>();
        public List<LevelEnemySpec> enemies = new List<LevelEnemySpec>();
        public Vector3 playerSpawnPosition;
        public float playerSpawnYaw;

        public bool TryValidate(out List<string> errors)
        {
            errors = new List<string>();

            if (sectors == null || sectors.Count == 0)
            {
                errors.Add("Level has no sectors.");
            }
            else
            {
                for (int i = 0; i < sectors.Count; i++)
                {
                    SectorSpec sector = sectors[i];
                    if (sector == null)
                    {
                        errors.Add($"Sector at index {i} is missing.");
                        continue;
                    }

                    if (sector.ceilingHeight <= 0f)
                    {
                        errors.Add($"Sector '{sector.id}' ceiling height must be positive.");
                    }

                    if (sector.floorAreas == null || sector.floorAreas.Count == 0)
                    {
                        errors.Add($"Sector '{sector.id}' has no floor areas.");
                        continue;
                    }

                    foreach (Rect area in sector.floorAreas)
                    {
                        if (area.width <= 0f || area.height <= 0f)
                        {
                            errors.Add($"Sector '{sector.id}' has a degenerate floor area {area}.");
                            break;
                        }
                    }
                }
            }

            if (walls != null)
            {
                for (int i = 0; i < walls.Count; i++)
                {
                    WallSegmentSpec wall = walls[i];
                    if (wall == null)
                    {
                        continue;
                    }

                    if ((wall.end - wall.start).sqrMagnitude < 0.0001f)
                    {
                        errors.Add($"Wall segment at index {i} is degenerate.");
                    }

                    if (wall.height <= 0f)
                    {
                        errors.Add($"Wall segment at index {i} must have positive height.");
                    }
                }
            }

            if (doorways != null)
            {
                for (int i = 0; i < doorways.Count; i++)
                {
                    DoorwaySpec doorway = doorways[i];
                    if (doorway != null && (doorway.width <= 0f || doorway.height <= 0f))
                    {
                        errors.Add($"Doorway at index {i} must have positive size.");
                    }
                }
            }

            if (stairwells != null)
            {
                for (int i = 0; i < stairwells.Count; i++)
                {
                    StairwellSpec stairwell = stairwells[i];
                    if (stairwell == null)
                    {
                        continue;
                    }

                    if (stairwell.opening.width <= 0f || stairwell.opening.height <= 0f)
                    {
                        errors.Add($"Stairwell at index {i} has a degenerate opening {stairwell.opening}.");
                    }

                    if (stairwell.bottomY >= stairwell.topY)
                    {
                        errors.Add($"Stairwell at index {i} must descend: bottomY must lie below topY.");
                    }

                    if (stairwell.stepCount < 2)
                    {
                        errors.Add($"Stairwell at index {i} needs at least two steps.");
                    }
                }
            }

            if (sectors != null && sectors.Count > 0 && !ContainsPointOnFloor(playerSpawnPosition))
            {
                errors.Add($"Player spawn {playerSpawnPosition} is not inside any sector floor area.");
            }

            return errors.Count == 0;
        }

        /// <summary>True when the XZ position lies inside a sector floor area or a doorway threshold.</summary>
        public bool ContainsPointOnFloor(Vector3 position)
        {
            var point = new Vector2(position.x, position.z);

            if (sectors != null)
            {
                foreach (SectorSpec sector in sectors)
                {
                    if (sector?.floorAreas == null)
                    {
                        continue;
                    }

                    foreach (Rect area in sector.floorAreas)
                    {
                        if (area.Contains(point))
                        {
                            return true;
                        }
                    }
                }
            }

            if (doorways != null)
            {
                foreach (DoorwaySpec doorway in doorways)
                {
                    if (doorway == null)
                    {
                        continue;
                    }

                    float half = doorway.width * 0.5f;
                    if (Mathf.Abs(point.x - doorway.center.x) <= half && Mathf.Abs(point.y - doorway.center.y) <= half)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }

    /// <summary>A room or corridor: floor/ceiling coverage as world-space XZ rectangles.</summary>
    [Serializable]
    public sealed class SectorSpec
    {
        public string id = "S1";
        public float floorY;
        public float ceilingHeight = 2f;

        /// <summary>World-space XZ rectangles (Rect.y is the Z coordinate).</summary>
        public List<Rect> floorAreas = new List<Rect>();

        public Color floorColor = new Color32(112, 112, 112, 255);
        public Color ceilingColor = new Color32(56, 56, 56, 255);
    }

    /// <summary>
    /// One straight wall run. The visible face points toward
    /// Vector3.Cross(Vector3.up, end - start), i.e. the walkable side lies on
    /// that side of the segment.
    /// </summary>
    [Serializable]
    public sealed class WallSegmentSpec
    {
        public Vector2 start;
        public Vector2 end;
        public float baseY;
        public float height = 2f;

        /// <summary>Material style; keeps the legacy wall value so the material library lookup applies.</summary>
        public int style = 1;
    }

    [Serializable]
    public sealed class DoorwaySpec
    {
        /// <summary>World XZ center of the doorway threshold.</summary>
        public Vector2 center;
        public float baseY;

        /// <summary>Ceiling height above the threshold, for the lintel band over the door.</summary>
        public float ceilingHeight = 2f;

        /// <summary>True when the door slab runs along the Z axis.</summary>
        public bool alongZ;
        public float width = 2f;
        public float height = 2f;
        public string type = "normal";

        /// <summary>
        /// Wall style of the doorway jambs; matches the flanking walls so the
        /// opening reads as cut into the same wall.
        /// </summary>
        public int headerStyle = 1;

        /// <summary>
        /// Direction (+1/-1 along the slab axis) the single slab retracts into;
        /// the converter picks a side with enough solid wall to hide the slab.
        /// </summary>
        public int slideSign = 1;
    }

    /// <summary>
    /// Rectangular opening cut into a floor plane with a straight staircase
    /// descending to the storey below. The shaft's side walls carry a wall
    /// style so the opening reads as cut into the same walls; treads and
    /// risers use the floor material with scaled-down tiles.
    /// </summary>
    [Serializable]
    public sealed class StairwellSpec
    {
        /// <summary>World-space XZ rectangle cut out of the floor at <see cref="topY"/> (Rect.y is the Z coordinate).</summary>
        public Rect opening;

        /// <summary>Floor plane the opening is cut into.</summary>
        public float topY;

        /// <summary>Floor the stairs land on.</summary>
        public float bottomY = -2f;

        /// <summary>True when the stairs run along the Z axis.</summary>
        public bool alongZ = true;

        /// <summary>+1 descends toward the positive run axis, -1 toward the negative one.</summary>
        public int descendSign = 1;

        public int stepCount = 16;

        /// <summary>Wall style of the shaft side walls; matches the surrounding room walls.</summary>
        public int wallStyle = 1;

        /// <summary>How many times smaller the floor tiles on the steps are than on the room floor.</summary>
        public float treadUvScale = 4f;
    }

    [Serializable]
    public sealed class LevelPropSpec
    {
        /// <summary>World position at floor level.</summary>
        public Vector3 position;
        public int typeIndex;
        public string typeName = "";
    }

    [Serializable]
    public sealed class LevelEnemySpec
    {
        /// <summary>World position at floor level.</summary>
        public Vector3 position;
        public string type = "guard";
        public float yaw;
        public bool patrol;
        public int difficulty;
    }
}
