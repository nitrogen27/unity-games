using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using WolfMini.Core;
using WolfMini.EditorTools;
using WolfMini.Level;

namespace HelloWorldRoom.Editor.Tests
{
    public sealed class WolfHandmadeLevelTests
    {
        private WolfSectorLevelDefinition definition;

        [SetUp]
        public void Author()
        {
            definition = ScriptableObject.CreateInstance<WolfSectorLevelDefinition>();
            WolfHandmadeLevelAuthor.Populate(definition);
        }

        [TearDown]
        public void Destroy()
        {
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void HandmadeLevelValidates()
        {
            Assert.That(definition.TryValidate(out List<string> errors), Is.True, string.Join("\n", errors));
            Assert.That(definition.sectors, Is.Not.Empty);
            Assert.That(definition.walls, Is.Not.Empty);
            Assert.That(definition.doorways, Is.Not.Empty);
            Assert.That(definition.props, Is.Not.Empty);
            Assert.That(definition.enemies, Is.Not.Empty);
            Assert.That(definition.ContainsPointOnFloor(definition.playerSpawnPosition), Is.True);
        }

        [Test]
        public void LayoutIsNotGridShaped()
        {
            float cell = WolfMiniConstants.CellSize;

            bool anyDiagonal = false;
            bool anyOffGridLength = false;
            foreach (WallSegmentSpec wall in definition.walls)
            {
                Vector2 d = wall.end - wall.start;
                if (Mathf.Abs(d.x) > 0.001f && Mathf.Abs(d.y) > 0.001f)
                {
                    anyDiagonal = true;
                }

                float length = d.magnitude;
                float remainder = Mathf.Abs(length - Mathf.Round(length / cell) * cell);
                if (remainder > 0.05f)
                {
                    anyOffGridLength = true;
                }
            }

            Assert.That(anyDiagonal, Is.True, "Handmade layout must contain diagonal walls.");
            Assert.That(anyOffGridLength, Is.True, "Handmade layout must contain wall lengths off the 2 m grid.");

            bool anyOffGridDoor = false;
            foreach (DoorwaySpec doorway in definition.doorways)
            {
                if (Mathf.Abs(doorway.width - cell) > 0.05f)
                {
                    anyOffGridDoor = true;
                }
            }

            Assert.That(anyOffGridDoor, Is.True, "Handmade layout must contain non-grid doorway widths.");
        }

        [Test]
        public void EveryPropAndEnemyStandsOnAFloor()
        {
            foreach (LevelPropSpec prop in definition.props)
            {
                Assert.That(definition.ContainsPointOnFloor(prop.position), Is.True,
                    $"Prop '{prop.typeName}' at {prop.position} is outside the floor plan.");
            }

            foreach (LevelEnemySpec enemy in definition.enemies)
            {
                Assert.That(definition.ContainsPointOnFloor(enemy.position), Is.True,
                    $"Enemy '{enemy.type}' at {enemy.position} is outside the floor plan.");
            }
        }
    }
}
