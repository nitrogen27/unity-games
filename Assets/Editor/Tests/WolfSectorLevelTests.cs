using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using WolfMini.Core;
using WolfMini.EditorTools;
using WolfMini.Level;

namespace HelloWorldRoom.Editor.Tests
{
    public sealed class WolfSectorLevelTests
    {
        private const string Level0Path = "Assets/Data/WolfRepo/level0.json";

        private WolfLevelDefinition grid;
        private WolfSectorLevelDefinition sector;

        [SetUp]
        public void Convert()
        {
            grid = ScriptableObject.CreateInstance<WolfLevelDefinition>();
            sector = ScriptableObject.CreateInstance<WolfSectorLevelDefinition>();
            WolfFull3DImporter.PopulateFromRepoJson(grid, File.ReadAllText(Level0Path));
            WolfSectorLevelConverter.PopulateFromGrid(sector, grid);
        }

        [TearDown]
        public void Destroy()
        {
            Object.DestroyImmediate(grid);
            Object.DestroyImmediate(sector);
        }

        [Test]
        public void ConversionPreservesContentAndValidates()
        {
            GridFloorSpec floor = grid.floors[0];
            Assert.That(sector.doorways, Has.Count.EqualTo(floor.doors.Count));
            Assert.That(sector.props, Has.Count.EqualTo(floor.statics.Count));
            Assert.That(sector.enemies, Has.Count.EqualTo(floor.enemies.Count));
            Assert.That(sector.TryValidate(out List<string> errors), Is.True, string.Join("\n", errors));

            foreach (DoorwaySpec doorway in sector.doorways)
            {
                // Square floor-to-ceiling opening: nothing to patch above the door.
                Assert.That(doorway.height, Is.EqualTo(WolfMiniConstants.WallHeight).Within(0.001f));
                Assert.That(doorway.width, Is.EqualTo(doorway.height).Within(0.001f));
                Assert.That(doorway.ceilingHeight, Is.EqualTo(WolfMiniConstants.WallHeight).Within(0.001f));
            }
        }

        [Test]
        public void SectorAreasCoverExactlyTheWalkableCells()
        {
            GridFloorSpec floor = grid.floors[0];
            int emptyCells = 0;
            for (int z = 0; z < floor.height; z++)
            {
                for (int x = 0; x < floor.width; x++)
                {
                    if (floor.WallAt(x, z) == 0)
                    {
                        emptyCells++;
                    }
                }
            }

            float cellArea = WolfMiniConstants.CellSize * WolfMiniConstants.CellSize;
            float coveredArea = 0f;
            foreach (SectorSpec spec in sector.sectors)
            {
                foreach (Rect area in spec.floorAreas)
                {
                    coveredArea += area.width * area.height;
                }
            }

            Assert.That(coveredArea, Is.EqualTo(emptyCells * cellArea).Within(0.01f));
        }

        [Test]
        public void WallSegmentsAreMergedRuns()
        {
            GridFloorSpec floor = grid.floors[0];
            int rawFaces = 0;
            for (int z = 0; z < floor.height; z++)
            {
                for (int x = 0; x < floor.width; x++)
                {
                    if (floor.WallAt(x, z) <= 0)
                    {
                        continue;
                    }

                    // Faces toward door cells are not emitted: the widened
                    // doorway provides its own jambs.
                    if (floor.WallAt(x + 1, z) == 0) rawFaces++;
                    if (floor.WallAt(x - 1, z) == 0) rawFaces++;
                    if (floor.WallAt(x, z + 1) == 0) rawFaces++;
                    if (floor.WallAt(x, z - 1) == 0) rawFaces++;
                }
            }

            Assert.That(sector.walls, Is.Not.Empty);
            Assert.That(sector.walls.Count, Is.LessThan(rawFaces), "Collinear faces must merge into segments.");

            bool anyMerged = false;
            float totalLength = 0f;
            foreach (WallSegmentSpec wall in sector.walls)
            {
                float length = (wall.end - wall.start).magnitude;
                totalLength += length;
                Assert.That(length, Is.GreaterThanOrEqualTo(WolfMiniConstants.CellSize - 0.001f));
                if (length > WolfMiniConstants.CellSize + 0.001f)
                {
                    anyMerged = true;
                }
            }

            Assert.That(anyMerged, Is.True, "At least one wall run should span multiple cells.");
            Assert.That(totalLength, Is.EqualTo(rawFaces * WolfMiniConstants.CellSize).Within(0.01f),
                "Merging must preserve total wall length.");
        }

        [Test]
        public void SpawnLandsInsideASector()
        {
            Assert.That(sector.ContainsPointOnFloor(sector.playerSpawnPosition), Is.True);
        }

        [Test]
        public void LowerStoreyExactlyMirrorsTheUpperFloor()
        {
            int sectors = sector.sectors.Count;
            int walls = sector.walls.Count;
            int doorways = sector.doorways.Count;
            int props = sector.props.Count;
            int enemies = sector.enemies.Count;

            WolfSectorLevelConverter.AddLowerStoreyWithStairwell(sector);

            Assert.That(sector.TryValidate(out List<string> errors), Is.True, string.Join("\n", errors));

            // Two ways between the storeys: down in the start corridor, back
            // up in the big south hall.
            Assert.That(sector.stairwells, Has.Count.EqualTo(2));

            // The whole floor is duplicated: same rooms, walls, doors and content.
            Assert.That(sector.sectors, Has.Count.EqualTo(sectors * 2));
            Assert.That(sector.walls, Has.Count.EqualTo(walls * 2));
            Assert.That(sector.doorways, Has.Count.EqualTo(doorways * 2));
            Assert.That(sector.props, Has.Count.EqualTo(props * 2));
            Assert.That(sector.enemies, Has.Count.EqualTo(enemies * 2));

            // Storeys are separated by a slab so nothing of the lower storey
            // touches the upper floor plane.
            float storey = WolfMiniConstants.WallHeight + WolfMiniConstants.FloorSlabThickness;
            for (int i = 0; i < sectors; i++)
            {
                SectorSpec upper = sector.sectors[i];
                SectorSpec lower = sector.sectors[sectors + i];
                Assert.That(lower.floorY, Is.EqualTo(upper.floorY - storey).Within(0.001f));
                Assert.That(lower.ceilingHeight, Is.EqualTo(upper.ceilingHeight).Within(0.001f));
                Assert.That(lower.floorAreas, Is.EqualTo(upper.floorAreas));
            }

            for (int i = 0; i < doorways; i++)
            {
                DoorwaySpec upper = sector.doorways[i];
                DoorwaySpec lower = sector.doorways[doorways + i];
                Assert.That(lower.baseY, Is.EqualTo(upper.baseY - storey).Within(0.001f));
                Assert.That(lower.center, Is.EqualTo(upper.center));
                Assert.That(lower.type, Is.EqualTo(upper.type));
            }

            foreach (StairwellSpec stairwell in sector.stairwells)
            {
                Assert.That(stairwell.topY, Is.EqualTo(0f).Within(0.001f));
                Assert.That(stairwell.bottomY, Is.EqualTo(-storey).Within(0.001f));

                // Steps stay climbable for the character controller, both ways.
                float riser = (stairwell.topY - stairwell.bottomY) / stairwell.stepCount;
                Assert.That(riser, Is.LessThanOrEqualTo(WolfMiniConstants.PlayerStepOffset));
            }

            float cell = WolfMiniConstants.CellSize;

            // The corridor opening leaves a one-cell walk-around strip on both
            // sides of the start corridor (cells x 33..35).
            Rect corridorOpening = sector.stairwells[0].opening;
            Assert.That(corridorOpening.xMin, Is.EqualTo(34 * cell).Within(0.001f));
            Assert.That(corridorOpening.xMax, Is.EqualTo(35 * cell).Within(0.001f));

            // The hall staircase stays inside the big south hall (cells
            // x 27..41, z 27..33), clear of the chandelier row at z 30.
            Rect hallOpening = sector.stairwells[1].opening;
            Assert.That(hallOpening.xMin, Is.GreaterThanOrEqualTo(28 * cell - 0.001f));
            Assert.That(hallOpening.xMax, Is.LessThanOrEqualTo(41 * cell + 0.001f));
            Assert.That(hallOpening.yMin, Is.GreaterThanOrEqualTo(28 * cell - 0.001f));
            Assert.That(hallOpening.yMax, Is.LessThanOrEqualTo(30 * cell + 0.001f));
        }
    }
}
