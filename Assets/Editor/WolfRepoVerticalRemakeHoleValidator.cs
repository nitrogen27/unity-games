using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace HelloWorldRoom.Editor
{
    public static class WolfRepoVerticalRemakeHoleValidator
    {
        private const float CapsuleRadius = 0.27f;
        private const float CapsuleBottom = 0.32f;
        private const float CapsuleTop = 1.14f;
        private const float SupportTolerance = 0.18f;

        [MenuItem("Tools/Wolf Repo/Validate Vertical Remake Holes")]
        public static void ValidateFromMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[WolfRepoHoleTest] Unity is in Play Mode. Stopping Play Mode; run validation again after it exits.");
                EditorApplication.isPlaying = false;
                return;
            }

            WolfRepoLevelBuilder.BuildVerticalRemake();
            Physics.SyncTransforms();

            List<string> failures = FindUpperStairHoleFailures();
            if (failures.Count == 0)
            {
                Debug.Log("[WolfRepoHoleTest] PASS: no unsupported walkable points or open facade leaks found around the upper stair.");
                return;
            }

            string report = FormatFailures(failures);
            Debug.LogError(report);
            throw new InvalidOperationException(report);
        }

        public static List<string> FindUpperStairHoleFailures()
        {
            var failures = new List<string>();

            ProbeSupportZone("sealed lower stair hub floor", 50.55f, 91.45f, 48.55f, 73.45f, 0.0f, failures);
            ProbeSupportZone("sealed upper deck", 63.55f, 91.45f, 48.55f, 73.45f, 3.0f, failures);

            ProbeFacadeZone("sealed west north wall", 50.00f, 48.55f, 57.10f, 0.30f, 5.70f, failures);
            ProbeFacadeZone("sealed west south wall", 50.00f, 60.90f, 73.45f, 0.30f, 5.70f, failures);
            ProbeFacadeZone("sealed west stair header", 50.00f, 57.90f, 60.10f, 3.20f, 5.70f, failures);
            ProbeFacadeZone("sealed landing north face", 63.20f, 48.55f, 57.10f, 0.30f, 5.70f, failures);
            ProbeFacadeZone("sealed landing south face", 63.20f, 60.90f, 73.45f, 0.30f, 5.70f, failures);
            ProbeFacadeZone("sealed landing header", 63.20f, 57.90f, 60.10f, 3.20f, 5.70f, failures);
            ProbeSideWallZone("sealed stair north corridor wall", 50.55f, 62.85f, 57.55f, 0.30f, 5.70f, failures);
            ProbeSideWallZone("sealed stair south corridor wall", 50.55f, 62.85f, 60.45f, 0.30f, 5.70f, failures);

            return failures;
        }

        public static string FormatFailures(List<string> failures)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"[WolfRepoHoleTest] FAIL: found {failures.Count} upper stair support/facade leak points.");
            int limit = Mathf.Min(failures.Count, 48);
            for (int i = 0; i < limit; i++)
            {
                builder.AppendLine(failures[i]);
            }

            if (failures.Count > limit)
            {
                builder.AppendLine($"...and {failures.Count - limit} more.");
            }

            return builder.ToString();
        }

        private static void ProbeSupportZone(string label, float minX, float maxX, float minZ, float maxZ, float expectedY, List<string> failures)
        {
            const float step = 0.45f;
            for (float x = minX; x <= maxX; x += step)
            {
                for (float z = minZ; z <= maxZ; z += step)
                {
                    if (IsControllerBlocked(x, z, expectedY))
                    {
                        continue;
                    }

                    if (!HasSupportNearHeight(x, z, expectedY, out string hitName, out float hitY))
                    {
                        failures.Add($"{label}: x={x:0.00}, z={z:0.00}, expectedY={expectedY:0.00}, nearestSupport={hitName}@{hitY:0.00}");
                    }
                }
            }
        }

        private static void ProbeFacadeZone(string label, float x, float minZ, float maxZ, float minY, float maxY, List<string> failures)
        {
            const float zStep = 0.45f;
            const float yStep = 0.45f;
            Vector3 halfExtents = new Vector3(0.13f, 0.13f, 0.13f);

            for (float z = minZ; z <= maxZ; z += zStep)
            {
                for (float y = minY; y <= maxY; y += yStep)
                {
                    Vector3 center = new Vector3(x, y, z);
                    if (!Physics.CheckBox(center, halfExtents, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                    {
                        failures.Add($"{label}: missing facade coverage at x={x:0.00}, y={y:0.00}, z={z:0.00}");
                    }
                }
            }
        }

        private static void ProbeSideWallZone(string label, float minX, float maxX, float z, float minY, float maxY, List<string> failures)
        {
            const float xStep = 0.45f;
            const float yStep = 0.45f;
            Vector3 halfExtents = new Vector3(0.13f, 0.13f, 0.13f);

            for (float x = minX; x <= maxX; x += xStep)
            {
                for (float y = minY; y <= maxY; y += yStep)
                {
                    Vector3 center = new Vector3(x, y, z);
                    if (!Physics.CheckBox(center, halfExtents, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                    {
                        failures.Add($"{label}: missing side-wall coverage at x={x:0.00}, y={y:0.00}, z={z:0.00}");
                    }
                }
            }
        }

        private static bool IsControllerBlocked(float x, float z, float floorY)
        {
            Vector3 p1 = new Vector3(x, floorY + CapsuleBottom, z);
            Vector3 p2 = new Vector3(x, floorY + CapsuleTop, z);
            return Physics.CheckCapsule(p1, p2, CapsuleRadius, ~0, QueryTriggerInteraction.Ignore);
        }

        private static bool HasSupportNearHeight(float x, float z, float expectedY, out string hitName, out float hitY)
        {
            hitName = "<none>";
            hitY = float.NegativeInfinity;

            Vector3 origin = new Vector3(x, expectedY + 0.85f, z);
            RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 2.2f, ~0, QueryTriggerInteraction.Ignore);
            foreach (RaycastHit hit in hits)
            {
                if (hit.normal.y < 0.65f)
                {
                    continue;
                }

                if (hit.point.y > hitY)
                {
                    hitY = hit.point.y;
                    hitName = hit.collider != null ? hit.collider.gameObject.name : "<unknown>";
                }

                if (Mathf.Abs(hit.point.y - expectedY) <= SupportTolerance)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
