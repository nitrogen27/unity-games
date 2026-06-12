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
        public void StartCorridorStairwellDescendsToAMatchingLowerStorey()
        {
            int sectorsBefore = sector.sectors.Count;
            WolfSectorLevelConverter.AddStartCorridorStairwell(sector);

            Assert.That(sector.TryValidate(out List<string> errors), Is.True, string.Join("\n", errors));
            Assert.That(sector.stairwells, Has.Count.EqualTo(1));

            StairwellSpec stairwell = sector.stairwells[0];
            Assert.That(stairwell.topY, Is.EqualTo(0f).Within(0.001f));
            Assert.That(stairwell.bottomY, Is.EqualTo(-WolfMiniConstants.WallHeight).Within(0.001f));

            // The opening leaves a one-cell walk-around strip on both sides of
            // the start corridor (cells x 33..35).
            float cell = WolfMiniConstants.CellSize;
            Assert.That(stairwell.opening.xMin, Is.EqualTo(34 * cell).Within(0.001f));
            Assert.That(stairwell.opening.xMax, Is.EqualTo(35 * cell).Within(0.001f));

            // Steps stay climbable for the character controller, both ways.
            float riser = (stairwell.topY - stairwell.bottomY) / stairwell.stepCount;
            Assert.That(riser, Is.LessThanOrEqualTo(WolfMiniConstants.PlayerStepOffset));

            // The lower storey mirrors the corridor above (same three-cell
            // width) and the stairs land on it: its floor area starts where
            // the opening ends.
            SectorSpec hall = sector.sectors[sectorsBefore];
            Assert.That(hall.floorY, Is.EqualTo(stairwell.bottomY).Within(0.001f));
            Assert.That(hall.ceilingHeight, Is.EqualTo(WolfMiniConstants.WallHeight).Within(0.001f));
            Rect hallArea = hall.floorAreas[0];
            Assert.That(hallArea.yMin, Is.EqualTo(stairwell.opening.yMax).Within(0.001f));
            Assert.That(hallArea.xMin, Is.EqualTo(33 * cell).Within(0.001f));
            Assert.That(hallArea.width, Is.EqualTo(3 * cell).Within(0.001f));
        }
    }
}
