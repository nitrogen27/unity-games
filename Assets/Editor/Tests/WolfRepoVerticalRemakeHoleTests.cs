using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HelloWorldRoom.Editor.Tests
{
    public sealed class WolfRepoVerticalRemakeHoleTests
    {
        [SetUp]
        public void BuildScene()
        {
            Assert.That(EditorApplication.isPlayingOrWillChangePlaymode, Is.False, "Run this EditMode hole test after leaving Play Mode.");
            WolfRepoLevelBuilder.BuildVerticalRemake();
            Physics.SyncTransforms();
        }

        [Test]
        public void UpperStairSideVoidsArePhysicallyClosed()
        {
            var failures = WolfRepoVerticalRemakeHoleValidator.FindUpperStairHoleFailures();

            if (failures.Count > 0)
            {
                Assert.Fail(WolfRepoVerticalRemakeHoleValidator.FormatFailures(failures));
            }
        }

        [Test]
        public void VerticalRemakeHasNoLegacyHighWallBandObjects()
        {
            Assert.That(GameObject.Find("Upper Band 00,00 Type 1"), Is.Null);
            Assert.That(GameObject.Find("Over-Door 28,52"), Is.Null);
        }
    }
}
