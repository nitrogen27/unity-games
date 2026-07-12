using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using WolfMini.Core;
using WolfMini.Level;

namespace WolfMini.EditorTools
{
    /// <summary>
    /// One-time bridge from the legacy Wolf3D cell grid to the grid-free sector
    /// model: walkable cells are flood-filled into sectors and decomposed into
    /// maximal rectangles, solid-cell faces are merged into straight wall
    /// segments, and doors/statics/enemies become world-space specs. After the
    /// conversion the runtime no longer sees any 64x64 grid.
    /// </summary>
    public static class WolfSectorLevelConverter
    {
        public const string SectorAssetPath = "Assets/WolfMini/Data/WolfRepoLevel1Sector.asset";

        private const float Cell = WolfMiniConstants.CellSize;

        private enum Facing
        {
            East,
            West,
            North,
            South
        }

        [MenuItem("Tools/Wolf Full3D/Convert Repo Level To Sector Definition")]
        public static void ConvertRepoLevel()
        {
            var source = AssetDatabase.LoadAssetAtPath<WolfLevelDefinition>(WolfFull3DImporter.DefinitionAssetPath);
            if (source == null)
            {
                Debug.LogError("[WolfSector] Run 'Import Repo Level Into Definition' first.");
                return;
            }

            WolfSectorLevelDefinition target = AssetDatabase.LoadAssetAtPath<WolfSectorLevelDefinition>(SectorAssetPath);
            if (target == null)
            {
                target = ScriptableObject.CreateInstance<WolfSectorLevelDefinition>();
                AssetDatabase.CreateAsset(target, SectorAssetPath);
            }

            PopulateFromGrid(target, source);
            PrepareScissorStairRoom(target);
            AddStackedStoreysWithStairwells(target);

            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();

            if (!target.TryValidate(out List<string> errors))
            {
                foreach (string error in errors)
                {
                    Debug.LogError($"[WolfSector] {target.name}: {error}", target);
                }

                return;
            }

            Debug.Log($"[WolfSector] Converted '{target.levelName}' into {SectorAssetPath}: " +
                      $"{target.sectors.Count} sector(s), {target.walls.Count} wall segment(s), " +
                      $"{target.doorways.Count} doorway(s), {target.props.Count} prop(s), {target.enemies.Count} enemy(ies).");
        }

        /// <summary>Fills the sector definition from a grid definition. Testable without asset IO.</summary>
        public static void PopulateFromGrid(WolfSectorLevelDefinition target, WolfLevelDefinition source)
        {
            GridFloorSpec floor = source.FindFloor(source.playerSpawn.floorId) ?? source.floors[0];

            target.levelName = source.levelName;
            target.sectors.Clear();
            target.walls.Clear();
            target.doorways.Clear();
            target.props.Clear();
            target.enemies.Clear();
            target.stairwells.Clear();
            if (target.floorOpenings == null)
            {
                target.floorOpenings = new List<FloorOpeningSpec>();
            }

            target.floorOpenings.Clear();

            BuildSectors(target, floor);
            BuildWallSegments(target, floor);
            BuildDoorways(target, floor);
            BuildEntities(target, floor);

            Vector2Int spawn = source.playerSpawn.cell;
            target.playerSpawnPosition = new Vector3((spawn.x + 0.5f) * Cell, floor.y, (spawn.y + 0.5f) * Cell);
            target.playerSpawnYaw = 90f + source.playerSpawn.angle;
        }

        /// <summary>
        /// Widens the dead-end room across the T-corridor from the spawn room
        /// (cells x 37..41, z 5..8) by one cell east so the classic scissor
        /// stairwell fits with a full-cell landing strip beyond the flights at
        /// both ends: the east wall moves out one cell and both long walls
        /// stretch to meet it. Also hangs a ceiling lamp over each end of the
        /// landing lane. Must run before the storey duplication so every
        /// storey inherits the bigger, lit room.
        /// </summary>
        public static void PrepareScissorStairRoom(WolfSectorLevelDefinition target)
        {
            const float eps = 0.01f;
            float oldEast = 41 * Cell;
            float newEast = 42 * Cell;
            float south = 5 * Cell;
            float north = 8 * Cell;

            SectorSpec room = null;
            int areaIndex = -1;
            var oldArea = new Rect(37 * Cell, south, 4 * Cell, 3 * Cell);
            foreach (SectorSpec sector in target.sectors)
            {
                for (int i = 0; i < sector.floorAreas.Count; i++)
                {
                    Rect area = sector.floorAreas[i];
                    if (Mathf.Abs(area.xMin - oldArea.xMin) < eps && Mathf.Abs(area.yMin - oldArea.yMin) < eps &&
                        Mathf.Abs(area.width - oldArea.width) < eps && Mathf.Abs(area.height - oldArea.height) < eps)
                    {
                        room = sector;
                        areaIndex = i;
                        break;
                    }
                }

                if (room != null)
                {
                    break;
                }
            }

            var eastWalls = new List<WallSegmentSpec>();
            var longWalls = new List<WallSegmentSpec>();
            foreach (WallSegmentSpec wall in target.walls)
            {
                bool horizontal = Mathf.Abs(wall.start.y - wall.end.y) < eps;
                if (!horizontal &&
                    Mathf.Abs(wall.start.x - oldEast) < eps && Mathf.Abs(wall.end.x - oldEast) < eps &&
                    wall.start.y > south - eps && wall.start.y < north + eps &&
                    wall.end.y > south - eps && wall.end.y < north + eps)
                {
                    eastWalls.Add(wall);
                    continue;
                }

                if (horizontal &&
                    (Mathf.Abs(wall.start.y - south) < eps || Mathf.Abs(wall.start.y - north) < eps) &&
                    (Mathf.Abs(wall.start.x - oldEast) < eps || Mathf.Abs(wall.end.x - oldEast) < eps) &&
                    Mathf.Min(wall.start.x, wall.end.x) > 37 * Cell - eps)
                {
                    longWalls.Add(wall);
                }
            }

            if (room == null || eastWalls.Count == 0 || longWalls.Count != 2)
            {
                Debug.LogError("[WolfSector] Scissor stair room not found where expected: " +
                               $"floor area {(room == null ? "missing" : "found")}, {eastWalls.Count} east wall segment(s), " +
                               $"{longWalls.Count} long wall end(s). Room left unchanged.");
                return;
            }

            room.floorAreas[areaIndex] = new Rect(oldArea.xMin, oldArea.yMin, 5 * Cell, oldArea.height);

            foreach (WallSegmentSpec wall in eastWalls)
            {
                wall.start = new Vector2(newEast, wall.start.y);
                wall.end = new Vector2(newEast, wall.end.y);
            }

            foreach (WallSegmentSpec wall in longWalls)
            {
                if (Mathf.Abs(wall.start.x - oldEast) < eps)
                {
                    wall.start = new Vector2(newEast, wall.start.y);
                }
                else
                {
                    wall.end = new Vector2(newEast, wall.end.y);
                }
            }

            // One lamp over each end of the landing lane: the doorside
            // landing and the far strip where the flights' mouths open.
            target.props.Add(new LevelPropSpec
            {
                position = new Vector3(37.5f * Cell, 0f, 6.5f * Cell),
                typeIndex = WolfLevelContent.CeilLightTypeIndex
            });
            target.props.Add(new LevelPropSpec
            {
                position = new Vector3(40.5f * Cell, 0f, 6.5f * Cell),
                typeIndex = WolfLevelContent.CeilLightTypeIndex
            });
        }

        /// <summary>
        /// The two extra storeys and the stairs that link them. Each storey
        /// is an exact copy of the whole authored floor — same rooms, walls,
        /// doorways, props and enemies — one storey height below and above the
        /// middle floor. The T-shaped start corridor becomes the hub: its west
        /// arm carries stairs down to the lower storey, its east arm stairs up
        /// to the top storey, so the T junction acts as the landing between
        /// them. A classic scissor stairwell in the widened room east of the
        /// corridor (see <see cref="PrepareScissorStairRoom"/>) links the same
        /// storeys in one room. Authored here because the repo grid carries no
        /// vertical data.
        /// </summary>
        public static void AddStackedStoreysWithStairwells(WolfSectorLevelDefinition target)
        {
            const int corridorStyle = 8; // blue stone, same as the corridor walls
            const int hallStyle = 2;     // tan stone, same as the big south hall walls
            const int stairStepCount = 13;
            float storey = WolfMiniConstants.WallHeight + WolfMiniConstants.FloorSlabThickness;
            float lowerY = -storey;
            float upperY = storey;

            int sectorCount = target.sectors.Count;
            int wallCount = target.walls.Count;
            int doorwayCount = target.doorways.Count;
            int propCount = target.props.Count;
            int enemyCount = target.enemies.Count;

            // The lower storey sits under the middle floor, so its walls close
            // the slab band; the top storey has nothing above it.
            DuplicateStorey(target, lowerY, "_L", WolfMiniConstants.FloorSlabThickness,
                sectorCount, wallCount, doorwayCount, propCount, enemyCount);
            DuplicateStorey(target, upperY, "_U", 0f,
                sectorCount, wallCount, doorwayCount, propCount, enemyCount);

            // The middle storey now carries the top storey: its walls run on up
            // to the top floor plane, closing that slab band sideways too.
            for (int i = 0; i < wallCount; i++)
            {
                target.walls[i].height += WolfMiniConstants.FloorSlabThickness;
            }

            // Stair hub in the T-bar of the start corridor (open cells
            // x 28..40, z 1..3). Both stairs take the middle cell row z 2,
            // leaving one-cell walk-around strips at z 1 and z 3, and both
            // entries sit flush with the T junction (x 33..35) so it reads as
            // the landing: step off west to descend, east to climb.

            // West arm: stairs down to the lower storey. Top entry at the
            // junction edge; the mouth opens west onto the arm's end cells.
            target.stairwells.Add(new StairwellSpec
            {
                opening = new Rect(30 * Cell, 2 * Cell, 3 * Cell, Cell),
                topY = 0f,
                bottomY = lowerY,
                alongZ = false,
                descendSign = -1,
                stepCount = stairStepCount,
                wallStyle = corridorStyle,
                treadUvScale = 4f
            });

            // East arm: stairs up to the top storey — authored as a stairwell
            // cut into the top storey's floor plane, its mouth opening west
            // toward the junction on the middle floor.
            target.stairwells.Add(new StairwellSpec
            {
                opening = new Rect(36 * Cell, 2 * Cell, 3 * Cell, Cell),
                topY = upperY,
                bottomY = 0f,
                alongZ = false,
                descendSign = -1,
                stepCount = stairStepCount,
                wallStyle = corridorStyle,
                treadUvScale = 4f
            });

            // Return staircase in the big south hall (cells x 27..41, z 27..33):
            // from the lower hall copy back up to the same hall on the middle
            // floor. It sits in the hall's north-west part (cells x 28..30,
            // row z 28), clear of the chandelier row at z 30, both guards and
            // the entrance path from the north door; the mouth opens east
            // toward the hall center, the stairs climb west.
            target.stairwells.Add(new StairwellSpec
            {
                opening = new Rect(28 * Cell, 28 * Cell, 3 * Cell, Cell),
                topY = 0f,
                bottomY = lowerY,
                alongZ = false,
                descendSign = 1,
                stepCount = stairStepCount,
                wallStyle = hallStyle,
                treadUvScale = 4f
            });

            // Classic scissor stairwell in the widened dead-end room across
            // the corridor from the spawn room (cells x 37..42, z 5..8, see
            // PrepareScissorStairRoom). Two straight full-storey flights run
            // head-to-head in the outer one-cell lanes while the middle lane
            // stays intact on every storey as the landing in front of the
            // door: climb the south flight out of the lower storey, U-turn
            // across the landing, and the north flight up to the top storey
            // starts right there. Full-cell strips at both ends carry the
            // stair entries past the flights.

            // South lane: lower flight. Its head tread lies flush with the
            // middle floor at the west end by the door; the mouth opens east
            // onto the lower storey's end strip.
            target.stairwells.Add(new StairwellSpec
            {
                opening = new Rect(38 * Cell, 5 * Cell, 3 * Cell, Cell),
                topY = 0f,
                bottomY = lowerY,
                alongZ = false,
                descendSign = 1,
                stepCount = stairStepCount,
                wallStyle = corridorStyle,
                treadUvScale = 4f,
                guardPitSides = true
            });

            // North lane: upper flight, mirrored head-to-head. Its mouth sits
            // beside the south flight's head on the middle floor; the head
            // tops out at the east end of the top storey.
            target.stairwells.Add(new StairwellSpec
            {
                opening = new Rect(38 * Cell, 7 * Cell, 3 * Cell, Cell),
                topY = upperY,
                bottomY = 0f,
                alongZ = false,
                descendSign = -1,
                stepCount = stairStepCount,
                wallStyle = corridorStyle,
                treadUvScale = 4f,
                guardPitSides = true
            });

            // Three-storey atrium in the big south hall: one void cuts the top
            // hall floor, the middle ceiling and the middle hall floor, so both
            // upper storeys become gallery rings around it, while the lower
            // hall floor stays intact so the player can look all the way down.
            target.floorOpenings.Add(new FloorOpeningSpec
            {
                opening = new Rect(31 * Cell, 28 * Cell, 8 * Cell, 5 * Cell),
                topY = upperY,
                bottomY = lowerY,
                wallStyle = hallStyle
            });
        }

        /// <summary>
        /// Clones the first counts of sectors, walls, doorways, props and
        /// enemies (the authored middle storey) shifted by <paramref name="offsetY"/>.
        /// A storey that sits under another one passes the slab thickness as
        /// <paramref name="extraWallHeight"/>: its walls then run on up to the
        /// floor plane above, closing the slab band sideways so nothing lit
        /// leaks through seam cracks between the storeys.
        /// </summary>
        private static void DuplicateStorey(
            WolfSectorLevelDefinition target,
            float offsetY,
            string idSuffix,
            float extraWallHeight,
            int sectorCount,
            int wallCount,
            int doorwayCount,
            int propCount,
            int enemyCount)
        {
            for (int i = 0; i < sectorCount; i++)
            {
                SectorSpec sector = target.sectors[i];
                target.sectors.Add(new SectorSpec
                {
                    id = $"{sector.id}{idSuffix}",
                    floorY = sector.floorY + offsetY,
                    ceilingHeight = sector.ceilingHeight,
                    floorColor = sector.floorColor,
                    ceilingColor = sector.ceilingColor,
                    floorAreas = new List<Rect>(sector.floorAreas)
                });
            }

            for (int i = 0; i < wallCount; i++)
            {
                WallSegmentSpec wall = target.walls[i];
                target.walls.Add(new WallSegmentSpec
                {
                    start = wall.start,
                    end = wall.end,
                    baseY = wall.baseY + offsetY,
                    height = wall.height + extraWallHeight,
                    style = wall.style
                });
            }

            for (int i = 0; i < doorwayCount; i++)
            {
                DoorwaySpec doorway = target.doorways[i];
                target.doorways.Add(new DoorwaySpec
                {
                    center = doorway.center,
                    baseY = doorway.baseY + offsetY,
                    ceilingHeight = doorway.ceilingHeight,
                    alongZ = doorway.alongZ,
                    width = doorway.width,
                    height = doorway.height,
                    type = doorway.type,
                    headerStyle = doorway.headerStyle,
                    slideSign = doorway.slideSign
                });
            }

            for (int i = 0; i < propCount; i++)
            {
                LevelPropSpec prop = target.props[i];
                target.props.Add(new LevelPropSpec
                {
                    position = prop.position + Vector3.up * offsetY,
                    typeIndex = prop.typeIndex,
                    typeName = prop.typeName
                });
            }

            for (int i = 0; i < enemyCount; i++)
            {
                LevelEnemySpec enemy = target.enemies[i];
                target.enemies.Add(new LevelEnemySpec
                {
                    position = enemy.position + Vector3.up * offsetY,
                    type = enemy.type,
                    yaw = enemy.yaw,
                    patrol = enemy.patrol,
                    difficulty = enemy.difficulty
                });
            }
        }

        /// <summary>
        /// Flood-fills empty cells into sectors of maximal rectangles. Door
        /// cells are excluded: the doorway owns its threshold floor and the
        /// lowered soffit so the door fills its whole opening.
        /// </summary>
        private static void BuildSectors(WolfSectorLevelDefinition target, GridFloorSpec floor)
        {
            bool[] visited = new bool[floor.width * floor.height];
            int sectorIndex = 0;

            for (int z = 0; z < floor.height; z++)
            {
                for (int x = 0; x < floor.width; x++)
                {
                    if (visited[z * floor.width + x] || floor.WallAt(x, z) != 0)
                    {
                        continue;
                    }

                    HashSet<Vector2Int> region = FloodFill(floor, visited, x, z);
                    var sector = new SectorSpec
                    {
                        id = $"S{++sectorIndex}",
                        floorY = floor.y,
                        ceilingHeight = floor.ceilingHeight,
                        floorColor = floor.floorColor,
                        ceilingColor = floor.ceilingColor,
                        floorAreas = DecomposeIntoRects(region)
                    };
                    target.sectors.Add(sector);
                }
            }
        }

        private static HashSet<Vector2Int> FloodFill(GridFloorSpec floor, bool[] visited, int startX, int startZ)
        {
            var region = new HashSet<Vector2Int>();
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(new Vector2Int(startX, startZ));
            visited[startZ * floor.width + startX] = true;

            while (queue.Count > 0)
            {
                Vector2Int cell = queue.Dequeue();
                region.Add(cell);

                foreach (Vector2Int step in new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down })
                {
                    Vector2Int next = cell + step;
                    if (!floor.InBounds(next.x, next.y) || visited[next.y * floor.width + next.x])
                    {
                        continue;
                    }

                    if (floor.WallAt(next.x, next.y) != 0)
                    {
                        continue;
                    }

                    visited[next.y * floor.width + next.x] = true;
                    queue.Enqueue(next);
                }
            }

            return region;
        }

        /// <summary>Greedy decomposition of a cell region into world-space rectangles.</summary>
        private static List<Rect> DecomposeIntoRects(HashSet<Vector2Int> region)
        {
            var rects = new List<Rect>();
            var covered = new HashSet<Vector2Int>();

            var cells = new List<Vector2Int>(region);
            cells.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));

            foreach (Vector2Int seed in cells)
            {
                if (covered.Contains(seed))
                {
                    continue;
                }

                int width = 1;
                while (region.Contains(new Vector2Int(seed.x + width, seed.y)) &&
                       !covered.Contains(new Vector2Int(seed.x + width, seed.y)))
                {
                    width++;
                }

                int height = 1;
                bool rowFits = true;
                while (rowFits)
                {
                    for (int dx = 0; dx < width; dx++)
                    {
                        var probe = new Vector2Int(seed.x + dx, seed.y + height);
                        if (!region.Contains(probe) || covered.Contains(probe))
                        {
                            rowFits = false;
                            break;
                        }
                    }

                    if (rowFits)
                    {
                        height++;
                    }
                }

                for (int dz = 0; dz < height; dz++)
                {
                    for (int dx = 0; dx < width; dx++)
                    {
                        covered.Add(new Vector2Int(seed.x + dx, seed.y + dz));
                    }
                }

                rects.Add(new Rect(seed.x * Cell, seed.y * Cell, width * Cell, height * Cell));
            }

            return rects;
        }

        /// <summary>Solid-cell faces toward walkable cells, merged into straight runs per plane and style.</summary>
        private static void BuildWallSegments(WolfSectorLevelDefinition target, GridFloorSpec floor)
        {
            // Key: facing + plane line (in cells) + wall style; value: run coordinates.
            var runs = new Dictionary<(Facing facing, int plane, int style), List<int>>();

            for (int z = 0; z < floor.height; z++)
            {
                for (int x = 0; x < floor.width; x++)
                {
                    int style = floor.WallAt(x, z);
                    if (style <= 0)
                    {
                        continue;
                    }

                    // Faces toward door cells are skipped: the widened
                    // doorway emits its own jambs inside the flanking walls.
                    if (floor.WallAt(x + 1, z) == 0)
                    {
                        AddRun(runs, Facing.East, x + 1, style, z);
                    }

                    if (floor.WallAt(x - 1, z) == 0)
                    {
                        AddRun(runs, Facing.West, x, style, z);
                    }

                    if (floor.WallAt(x, z + 1) == 0)
                    {
                        AddRun(runs, Facing.North, z + 1, style, x);
                    }

                    if (floor.WallAt(x, z - 1) == 0)
                    {
                        AddRun(runs, Facing.South, z, style, x);
                    }
                }
            }

            foreach (KeyValuePair<(Facing facing, int plane, int style), List<int>> entry in runs)
            {
                entry.Value.Sort();
                int rangeStart = entry.Value[0];
                int previous = rangeStart;

                for (int i = 1; i <= entry.Value.Count; i++)
                {
                    if (i < entry.Value.Count && entry.Value[i] == previous + 1)
                    {
                        previous = entry.Value[i];
                        continue;
                    }

                    target.walls.Add(CreateSegment(entry.Key.facing, entry.Key.plane, entry.Key.style, rangeStart, previous, floor));

                    if (i < entry.Value.Count)
                    {
                        rangeStart = entry.Value[i];
                        previous = rangeStart;
                    }
                }
            }
        }

        private static void AddRun(Dictionary<(Facing, int, int), List<int>> runs, Facing facing, int plane, int style, int runCoord)
        {
            (Facing, int, int) key = (facing, plane, style);
            if (!runs.TryGetValue(key, out List<int> list))
            {
                list = new List<int>();
                runs[key] = list;
            }

            list.Add(runCoord);
        }

        /// <summary>
        /// Builds the world segment so its visible face (up x direction) points
        /// toward the walkable side.
        /// </summary>
        private static WallSegmentSpec CreateSegment(Facing facing, int plane, int style, int runStart, int runEnd, GridFloorSpec floor)
        {
            float planeWorld = plane * Cell;
            float from = runStart * Cell;
            float to = (runEnd + 1) * Cell;

            Vector2 start;
            Vector2 end;
            switch (facing)
            {
                case Facing.East:
                    start = new Vector2(planeWorld, from);
                    end = new Vector2(planeWorld, to);
                    break;
                case Facing.West:
                    start = new Vector2(planeWorld, to);
                    end = new Vector2(planeWorld, from);
                    break;
                case Facing.North:
                    start = new Vector2(to, planeWorld);
                    end = new Vector2(from, planeWorld);
                    break;
                default:
                    start = new Vector2(from, planeWorld);
                    end = new Vector2(to, planeWorld);
                    break;
            }

            return new WallSegmentSpec
            {
                start = start,
                end = end,
                baseY = floor.y,
                height = floor.ceilingHeight,
                style = style
            };
        }

        private static void BuildDoorways(WolfSectorLevelDefinition target, GridFloorSpec floor)
        {
            if (floor.doors == null)
            {
                return;
            }

            foreach (GridDoorSpec door in floor.doors)
            {
                if (door == null)
                {
                    continue;
                }

                // Vertical doors are flanked by solid cells along Z, the
                // others along X; the jambs copy that wall style.
                int headerStyle = door.vertical
                    ? Mathf.Max(floor.WallAt(door.x, door.y - 1), floor.WallAt(door.x, door.y + 1))
                    : Mathf.Max(floor.WallAt(door.x - 1, door.y), floor.WallAt(door.x + 1, door.y));

                // The full-height slab is wider than one flank cell, so it must
                // retract toward a side with two solid cells in a row.
                bool positiveSideSolid = door.vertical
                    ? floor.WallAt(door.x, door.y + 1) > 0 && floor.WallAt(door.x, door.y + 2) > 0
                    : floor.WallAt(door.x + 1, door.y) > 0 && floor.WallAt(door.x + 2, door.y) > 0;
                int slideSign = positiveSideSolid ? 1 : -1;

                // Square floor-to-ceiling opening: the doorway is widened
                // beyond the cell so the door slab is ceilingHeight x
                // ceilingHeight with nothing to patch above it.
                target.doorways.Add(new DoorwaySpec
                {
                    center = new Vector2((door.x + 0.5f) * Cell, (door.y + 0.5f) * Cell),
                    baseY = floor.y,
                    ceilingHeight = floor.ceilingHeight,
                    alongZ = door.vertical,
                    width = floor.ceilingHeight,
                    height = floor.ceilingHeight,
                    type = door.type,
                    headerStyle = headerStyle > 0 ? headerStyle : 1,
                    slideSign = slideSign
                });
            }
        }

        private static void BuildEntities(WolfSectorLevelDefinition target, GridFloorSpec floor)
        {
            if (floor.statics != null)
            {
                foreach (GridStaticSpec item in floor.statics)
                {
                    if (item == null)
                    {
                        continue;
                    }

                    target.props.Add(new LevelPropSpec
                    {
                        position = new Vector3((item.x + 0.5f) * Cell, floor.y, (item.y + 0.5f) * Cell),
                        typeIndex = item.typeIndex,
                        typeName = item.typeName
                    });
                }
            }

            if (floor.enemies != null)
            {
                foreach (GridEnemySpec enemy in floor.enemies)
                {
                    if (enemy == null)
                    {
                        continue;
                    }

                    target.enemies.Add(new LevelEnemySpec
                    {
                        position = new Vector3((enemy.x + 0.5f) * Cell, floor.y, (enemy.y + 0.5f) * Cell),
                        type = enemy.type,
                        yaw = enemy.dir * 90f,
                        patrol = enemy.patrol,
                        difficulty = enemy.difficulty
                    });
                }
            }
        }
    }
}
