using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using WolfMini.EditorTools;
using WolfMini.Level;

namespace HelloWorldRoom.Editor.Tests
{
    public sealed class WolfFull3DLevelTests
    {
        private const string Level0Path = "Assets/Data/WolfRepo/level0.json";

        private WolfLevelDefinition definition;

        [SetUp]
        public void CreateDefinition()
        {
            definition = ScriptableObject.CreateInstance<WolfLevelDefinition>();
        }

        [TearDown]
        public void DestroyDefinition()
        {
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void ValidationRejectsWrongWallsArrayLength()
        {
            definition.floors.Add(new GridFloorSpec { id = "F1", width = 4, height = 4, walls = new int[3] });

            Assert.That(definition.TryValidate(out List<string> errors), Is.False);
            Assert.That(errors, Has.Some.Contains("walls array"));
        }

        [Test]
        public void ValidationRejectsDuplicateFloorIds()
        {
            definition.floors.Add(MakeEmptyFloor("F1", 0f));
            definition.floors.Add(MakeEmptyFloor("F1", -3f));

            Assert.That(definition.TryValidate(out List<string> errors), Is.False);
            Assert.That(errors, Has.Some.Contains("more than once"));
        }

        [Test]
        public void ValidationRejectsOutOfBoundsSpawn()
        {
            definition.floors.Add(MakeEmptyFloor("F1", 0f));
            definition.playerSpawn.floorId = "F1";
            definition.playerSpawn.cell = new Vector2Int(99, 99);

            Assert.That(definition.TryValidate(out List<string> errors), Is.False);
            Assert.That(errors, Has.Some.Contains("out of bounds"));
        }

        [Test]
        public void ValidationRejectsOpeningToMissingFloor()
        {
            definition.floors.Add(MakeEmptyFloor("F1", 0f));
            definition.playerSpawn.floorId = "F1";
            definition.openings.Add(new VerticalOpeningSpec { id = "O", upperFloorId = "F1", lowerFloorId = "Nope" });

            Assert.That(definition.TryValidate(out List<string> errors), Is.False);
            Assert.That(errors, Has.Some.Contains("missing lower floor"));
        }

        [Test]
        public void ImporterConvertsRepoLevelOneToOne()
        {
            WolfFull3DImporter.PopulateFromRepoJson(definition, File.ReadAllText(Level0Path));

            Assert.That(definition.floors, Has.Count.EqualTo(1));
            GridFloorSpec floor = definition.floors[0];
            Assert.That(floor.id, Is.EqualTo(WolfFull3DImporter.MainFloorId));
            Assert.That(floor.walls, Has.Length.EqualTo(64 * 64));
            Assert.That(floor.doors, Has.Count.EqualTo(22));
            Assert.That(floor.enemies, Has.Count.EqualTo(37));
            Assert.That(floor.statics, Is.Not.Empty);
            Assert.That(definition.playerSpawn.cell, Is.EqualTo(new Vector2Int(29, 6)));
            Assert.That(floor.IsWalkable(29, 6), Is.True, "Spawn cell must be walkable.");
            Assert.That(definition.TryValidate(out List<string> errors), Is.True, string.Join("\n", errors));
        }

        [Test]
        public void OpeningsCutFloorSlabAndLowerCeiling()
        {
            definition.floors.Add(MakeEmptyFloor("F1", 0f));
            definition.floors.Add(MakeEmptyFloor("B1", -3f));
            definition.playerSpawn.floorId = "F1";
            definition.openings.Add(new VerticalOpeningSpec
            {
                id = "O",
                upperFloorId = "F1",
                lowerFloorId = "B1",
                cells = new RectInt(2, 2, 1, 2)
            });

            Assert.That(definition.IsFloorSlabCut("F1", 2, 2), Is.True);
            Assert.That(definition.IsFloorSlabCut("F1", 2, 3), Is.True);
            Assert.That(definition.IsFloorSlabCut("F1", 2, 4), Is.False);
            Assert.That(definition.IsFloorSlabCut("B1", 2, 2), Is.False);
            Assert.That(definition.IsCeilingCut("B1", 2, 2), Is.True);
            Assert.That(definition.IsCeilingCut("F1", 2, 2), Is.False);
        }

        [Test]
        public void MeshBufferBuildsQuadWithSubmeshes()
        {
            var buffer = new WolfMeshBuffer(submeshCount: 2);
            buffer.AddQuad(1,
                new Vector3(0f, 0f, 0f),
                new Vector3(1f, 0f, 0f),
                new Vector3(1f, 1f, 0f),
                new Vector3(0f, 1f, 0f),
                Vector3.back,
                new Rect(0f, 0f, 1f, 1f));

            Mesh mesh = buffer.ToMesh("Test");
            Assert.That(mesh.vertexCount, Is.EqualTo(4));
            Assert.That(mesh.GetTriangles(0), Is.Empty);
            Assert.That(mesh.GetTriangles(1), Has.Length.EqualTo(6));
            Object.DestroyImmediate(mesh);
        }

        private static GridFloorSpec MakeEmptyFloor(string id, float y)
        {
            return new GridFloorSpec
            {
                id = id,
                y = y,
                width = 8,
                height = 8,
                walls = new int[64]
            };
        }
    }
}
