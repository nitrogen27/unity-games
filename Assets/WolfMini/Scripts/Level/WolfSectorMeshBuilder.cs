using System.Collections.Generic;
using HelloWorldRoom;
using WolfMini.Core;
using WolfMini.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace WolfMini.Level
{
    /// <summary>
    /// Builds a level from a grid-free <see cref="WolfSectorLevelDefinition"/>:
    /// one combined mesh where each floor/ceiling rectangle and each merged wall
    /// segment is a single quad (atlas-windowed styles fall back to per-tile
    /// quads because an atlas window cannot wrap), plus door slabs, lintels,
    /// billboards and lamp lights as separate objects.
    /// </summary>
    public sealed class WolfSectorMeshBuilder : MonoBehaviour
    {
        private const float Cell = WolfMiniConstants.CellSize;
        private const float Module = WolfMiniConstants.WallTextureModule;
        private const float DoorThickness = WolfMiniConstants.DoorThickness;
        private const float WorldScale = WolfMiniConstants.WorldScale;

        [SerializeField] private WolfSectorLevelDefinition definition;
        [SerializeField] private WolfFull3DMaterialLibrary materialLibrary;
        [SerializeField] private bool buildOnStart = true;
        [SerializeField] private bool buildLights = true;
        // Covers both storeys: lamps use target-look realtime light rigs.
        [SerializeField] private int maxRealtimeLights = 96;
        [SerializeField] private bool buildReflectionProbes = true;

        // Guard rails are human-scale furniture (like doors and actors), not
        // world-scaled architecture: ~1 m of guard height reads right against
        // the 1.75 m soldiers on the galleries.
        public const float RailingInset = 0.11f;
        public const float RailingGuardHeight = 0.98f;
        public const float RailingTopRailWidth = 0.10f;
        private const float RailingPostHeight = 1.06f;
        private const float RailingPostSize = 0.08f;
        private const float RailingPostCapSize = 0.14f;
        private const float RailingPostCapHeight = 0.045f;
        private const float RailingPostSpacing = 1.35f;
        private const float RailingTopRailHeight = 0.07f;
        private const float RailingBarSize = 0.034f;
        private static readonly float[] RailingBarHeights = { 0.23f, 0.46f, 0.69f };

        private const int LampLightingMask = ~0;
        private const float PrimaryShadowStrength = 0.64f;
        // Environment reflection probes: local receiver renderers keep floor
        // and ceiling bounds small enough for Unity to bind nearby box-projected
        // probes. Probe boxes hug the room interior — a box that pokes through
        // a wall projects this room's cubemap onto the neighbour's floor, which
        // reads as reflections leaking through the wall. Only rooms of a few
        // cells get a probe — tiny closets would explode the probe count
        // without adding visible reflections.
        private const float ReflectionProbePadding = 0.15f * WorldScale;
        private const float ReflectionProbeBlendDistance = 0.25f * WorldScale;
        private const float MinimumReflectionProbeSide = 2.5f * Cell;
        private const int SectorReflectionProbeResolution = 128;
        private const int AtriumReflectionProbeResolution = 256;
        private const float LocalProbeReceiverOffset = 0.006f * WorldScale;
        private const float ReflectionBoundaryProbeHeight = 0.42f * WorldScale;
        private const float ReflectionBoundaryInset = 0.18f * WorldScale;
        private const float MinimumReflectionSize = 0.55f * WorldScale;

        public WolfSectorLevelDefinition Definition
        {
            get => definition;
            set => definition = value;
        }

        public WolfFull3DMaterialLibrary MaterialLibrary
        {
            get => materialLibrary;
            set => materialLibrary = value;
        }

        public bool BuildOnStart
        {
            get => buildOnStart;
            set => buildOnStart = value;
        }

        private void Start()
        {
            if (buildOnStart)
            {
                Build();
            }
        }

        [ContextMenu("Rebuild Level")]
        public void Build()
        {
            if (definition == null)
            {
                Debug.LogWarning("[WolfSector] WolfSectorMeshBuilder has no definition assigned.", this);
                return;
            }

            if (materialLibrary == null)
            {
                Debug.LogWarning("[WolfSector] WolfSectorMeshBuilder has no material library assigned.", this);
                return;
            }

            if (!definition.TryValidate(out List<string> errors))
            {
                foreach (string error in errors)
                {
                    Debug.LogError($"[WolfSector] {definition.name}: {error}", definition);
                }

                return;
            }

            ApplyCinematicRenderSettings();

            Transform root = ResetGeneratedRoot();
            BuildLevelGeometry(root);
            BuildLocalProbeSurfaceReceivers(root);
            BuildRailings(root);
            BuildDoorways(root);
            Physics.SyncTransforms();
            BuildProps(root);
            BuildChandeliers(root);
            BuildFloorOpeningLighting(root);
            BuildEnemies(root);
            ApplyLightingQuality();
            BuildReflectionProbes(root);
        }

        private Transform ResetGeneratedRoot()
        {
            GeneratedLevelRoot existing = GetComponentInChildren<GeneratedLevelRoot>(true);
            if (existing != null)
            {
                DestroySafely(existing.gameObject);
            }

            GameObject root = new GameObject("Generated Sector Level");
            root.transform.SetParent(transform, false);
            root.AddComponent<GeneratedLevelRoot>();
            return root.transform;
        }

        private static Cubemap indoorEnvironmentReflection;

        private static Cubemap GetIndoorEnvironmentReflection()
        {
            if (indoorEnvironmentReflection != null)
            {
                return indoorEnvironmentReflection;
            }

            indoorEnvironmentReflection = new Cubemap(4, TextureFormat.RGBA32, false)
            {
                name = "Wolf indoor environment reflection",
                hideFlags = HideFlags.DontSave
            };
            var shade = new Color(0.032f, 0.03f, 0.027f);
            var pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = shade;
            }

            for (int face = 0; face < 6; face++)
            {
                indoorEnvironmentReflection.SetPixels(pixels, (CubemapFace)face);
            }

            indoorEnvironmentReflection.Apply();
            return indoorEnvironmentReflection;
        }

        private static void ApplyCinematicRenderSettings()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.310f, 0.290f, 0.250f);
            RenderSettings.ambientEquatorColor = new Color(0.185f, 0.195f, 0.220f);
            RenderSettings.ambientGroundColor = new Color(0.215f, 0.220f, 0.235f);
            RenderSettings.ambientIntensity = 0.74f;
            RenderSettings.reflectionIntensity = 0.86f;
            RenderSettings.reflectionBounces = 1;
            // Indoors there is no sky: surfaces outside every probe volume must
            // reflect darkness. The default procedural skybox fallback paints
            // straight-edged white "daylight" bands on floors at probe borders.
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = GetIndoorEnvironmentReflection();
            RenderSettings.defaultReflectionResolution = AtriumReflectionProbeResolution;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0009f;
            RenderSettings.fogColor = new Color(0.070f, 0.080f, 0.092f);

            foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude))
            {
                if (light.type != LightType.Directional || light.gameObject.name != "Full3D Directional Light")
                {
                    continue;
                }

                // The bunker is fully indoors: a directional light reads as a
                // white daylight wash from nowhere. Only lamp fixtures emit.
                light.enabled = false;
            }
        }

        private void BuildLevelGeometry(Transform parent)
        {
            var buffer = new WolfMeshBuffer(submeshCount: 3);
            const int floorSubmesh = 0;
            const int ceilingSubmesh = 1;
            const int wallSubmesh = 2;

            var overrideSubmeshes = new Dictionary<Material, int>();
            var overrideMaterials = new List<Material>();

            foreach (SectorSpec sector in definition.sectors)
            {
                float ceilingY = sector.floorY + sector.ceilingHeight;
                foreach (Rect area in sector.floorAreas)
                {
                    // Vertical openings punch through the horizontal planes
                    // they cross: stair shafts, and simple atrium voids that
                    // leave the lower floor intact.
                    foreach (Rect piece in SubtractVerticalOpenings(area, sector.floorY))
                    {
                        AddFloorQuad(buffer, floorSubmesh, piece, sector.floorY, WolfMiniConstants.FloorTextureModule);
                    }

                    foreach (Rect piece in SubtractVerticalOpenings(area, ceilingY))
                    {
                        AddCeilingQuad(buffer, ceilingSubmesh, piece, ceilingY);
                    }
                }
            }

            foreach (StairwellSpec stairwell in definition.stairwells)
            {
                if (stairwell != null)
                {
                    AddStairwellGeometry(buffer, floorSubmesh, wallSubmesh, overrideSubmeshes, overrideMaterials, stairwell);
                }
            }

            if (definition.floorOpenings != null)
            {
                foreach (FloorOpeningSpec opening in definition.floorOpenings)
                {
                    if (opening != null)
                    {
                        AddFloorOpeningGeometry(buffer, wallSubmesh, overrideSubmeshes, overrideMaterials, opening);
                    }
                }
            }

            // Doorway openings are wider than the door cell, so wall runs on
            // the opening planes are clipped before emission.
            List<OpeningCut> cuts = BuildOpeningCuts();
            foreach (WallSegmentSpec wall in definition.walls)
            {
                foreach (WallSegmentSpec piece in ClipWallByOpenings(wall, cuts))
                {
                    EmitWall(buffer, wallSubmesh, overrideSubmeshes, overrideMaterials, piece);
                }
            }

            foreach (DoorwaySpec doorway in definition.doorways)
            {
                AddDoorwayGeometry(buffer, floorSubmesh, ceilingSubmesh, wallSubmesh, overrideSubmeshes, overrideMaterials, doorway);
            }

            var materials = new List<Material>
            {
                materialLibrary.FloorMaterial,
                materialLibrary.CeilingMaterial,
                materialLibrary.WallAtlasMaterial
            };
            materials.AddRange(overrideMaterials);
            while (materials.Count > buffer.SubmeshCount)
            {
                materials.RemoveAt(materials.Count - 1);
            }

            GameObject geometry = new GameObject("Level Geometry");
            geometry.transform.SetParent(parent, false);
            geometry.isStatic = true;

            Mesh mesh = buffer.ToMesh($"{definition.levelName}_Geometry");
            geometry.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = geometry.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = materials.ToArray();
            // Probe binding is per renderer: this mesh spans the whole level,
            // so any probe Unity picked for it (the atrium one wins on
            // importance) would be reflected by every wall and floor of every
            // room — reflections through walls. The per-room receiver quads
            // carry the probe reflections instead.
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.BlendProbes;
            renderer.receiveShadows = true;
            geometry.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        /// <summary>
        /// Small renderer bounds make Unity bind nearby reflection probes to
        /// glossy horizontal surfaces instead of treating the whole level as
        /// one giant probe receiver.
        /// </summary>
        private void BuildLocalProbeSurfaceReceivers(Transform parent)
        {
            if (!buildReflectionProbes)
            {
                return;
            }

            GameObject group = new GameObject("Local PBR Reflection Receivers");
            group.transform.SetParent(parent, false);

            int index = 1;
            foreach (SectorSpec sector in definition.sectors)
            {
                // A receiver in a sector without its own probe would bind to a
                // neighbouring room's probe and paint that room's reflections
                // on this side of the shared wall (brightest under doors).
                if (!TryGetSectorProbeArea(sector, out _))
                {
                    continue;
                }

                float ceilingY = sector.floorY + sector.ceilingHeight;
                foreach (Rect area in sector.floorAreas)
                {
                    foreach (Rect piece in SubtractVerticalOpenings(area, sector.floorY))
                    {
                        CreateHorizontalReflectionReceiver(group.transform, $"floor receiver {index:000}", piece, sector.floorY + LocalProbeReceiverOffset, false);
                        index++;
                    }

                    foreach (Rect piece in SubtractVerticalOpenings(area, ceilingY))
                    {
                        CreateHorizontalReflectionReceiver(group.transform, $"ceiling receiver {index:000}", piece, ceilingY - LocalProbeReceiverOffset, true);
                        index++;
                    }
                }
            }
        }

        private void CreateHorizontalReflectionReceiver(Transform parent, string name, Rect area, float y, bool ceiling)
        {
            if (area.width <= 0.05f || area.height <= 0.05f)
            {
                return;
            }

            Material material = ceiling ? materialLibrary.CeilingMaterial : materialLibrary.FloorMaterial;
            if (material == null)
            {
                return;
            }

            var buffer = new WolfMeshBuffer(submeshCount: 1);
            if (ceiling)
            {
                AddCeilingQuad(buffer, 0, area, y);
            }
            else
            {
                AddFloorQuad(buffer, 0, area, y, WolfMiniConstants.FloorTextureModule);
            }

            GameObject receiver = new GameObject(name);
            receiver.transform.SetParent(parent, false);

            Mesh mesh = buffer.ToMesh($"{definition.levelName}_{name}");
            receiver.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = receiver.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.BlendProbesAndSkybox;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.BlendProbes;
        }

        /// <summary>Floor and ceiling quads over one world-space rectangle, with continuous world-anchored module UVs.</summary>
        private static void AddHorizontalQuads(WolfMeshBuffer buffer, int floorSubmesh, int ceilingSubmesh, Rect area, float floorY, float ceilingY)
        {
            AddFloorQuad(buffer, floorSubmesh, area, floorY, WolfMiniConstants.FloorTextureModule);
            AddCeilingQuad(buffer, ceilingSubmesh, area, ceilingY);
        }

        private static void AddFloorQuad(WolfMeshBuffer buffer, int floorSubmesh, Rect area, float floorY, float module)
        {
            Rect uv = new Rect(area.x / module, area.y / module, area.width / module, area.height / module);
            buffer.AddQuad(floorSubmesh,
                new Vector3(area.xMin, floorY, area.yMin),
                new Vector3(area.xMax, floorY, area.yMin),
                new Vector3(area.xMax, floorY, area.yMax),
                new Vector3(area.xMin, floorY, area.yMax),
                Vector3.up, uv);
        }

        private static void AddCeilingQuad(WolfMeshBuffer buffer, int ceilingSubmesh, Rect area, float ceilingY)
        {
            const float floorModule = WolfMiniConstants.FloorTextureModule;
            Rect uv = new Rect(area.x / floorModule, area.y / floorModule, area.width / floorModule, area.height / floorModule);
            buffer.AddQuad(ceilingSubmesh,
                new Vector3(area.xMax, ceilingY, area.yMin),
                new Vector3(area.xMin, ceilingY, area.yMin),
                new Vector3(area.xMin, ceilingY, area.yMax),
                new Vector3(area.xMax, ceilingY, area.yMax),
                Vector3.down, uv);
        }

        /// <summary>
        /// Pieces of a horizontal rectangle at <paramref name="planeY"/> left
        /// after cutting vertical openings out of it. Stairwells consume the
        /// lower floor inside their shaft; floor openings leave that floor
        /// intact and only cut the planes above it.
        /// </summary>
        private List<Rect> SubtractVerticalOpenings(Rect area, float planeY)
        {
            const float eps = 0.001f;
            var pieces = new List<Rect> { area };
            foreach (StairwellSpec stairwell in definition.stairwells)
            {
                if (stairwell == null || planeY > stairwell.topY + eps || planeY < stairwell.bottomY - eps)
                {
                    continue;
                }

                pieces = SubtractRect(pieces, stairwell.opening);
            }

            if (definition.floorOpenings == null)
            {
                return pieces;
            }

            foreach (FloorOpeningSpec opening in definition.floorOpenings)
            {
                if (opening == null || planeY > opening.topY + eps || planeY <= opening.bottomY + eps)
                {
                    continue;
                }

                pieces = SubtractRect(pieces, opening.opening);
            }

            return pieces;
        }

        private static List<Rect> SubtractRect(List<Rect> pieces, Rect hole)
        {
            const float eps = 0.001f;
            var result = new List<Rect>();
            foreach (Rect piece in pieces)
            {
                bool overlaps = hole.xMin < piece.xMax - eps && hole.xMax > piece.xMin + eps &&
                                hole.yMin < piece.yMax - eps && hole.yMax > piece.yMin + eps;
                if (!overlaps)
                {
                    result.Add(piece);
                    continue;
                }

                float bandMin = Mathf.Max(piece.yMin, hole.yMin);
                float bandMax = Mathf.Min(piece.yMax, hole.yMax);
                if (hole.yMin > piece.yMin + eps)
                {
                    result.Add(Rect.MinMaxRect(piece.xMin, piece.yMin, piece.xMax, hole.yMin));
                }

                if (hole.yMax < piece.yMax - eps)
                {
                    result.Add(Rect.MinMaxRect(piece.xMin, hole.yMax, piece.xMax, piece.yMax));
                }

                if (hole.xMin > piece.xMin + eps)
                {
                    result.Add(Rect.MinMaxRect(piece.xMin, bandMin, hole.xMin, bandMax));
                }

                if (hole.xMax < piece.xMax - eps)
                {
                    result.Add(Rect.MinMaxRect(hole.xMax, bandMin, piece.xMax, bandMax));
                }
            }

            return result;
        }

        /// <summary>
        /// Staircase down through a floor opening: the shaft's side walls use
        /// the wall style (so the pit reads as cut into the same walls), treads
        /// and risers use the floor material with tiles scaled down by
        /// <see cref="StairwellSpec.treadUvScale"/>. The upper tread is flush
        /// with the upper floor, and the lower mouth gets a full-depth starter
        /// step one riser above the lower floor.
        /// </summary>
        private void AddStairwellGeometry(
            WolfMeshBuffer buffer,
            int floorSubmesh,
            int wallSubmesh,
            Dictionary<Material, int> overrideSubmeshes,
            List<Material> overrideMaterials,
            StairwellSpec stairwell)
        {
            Rect opening = stairwell.opening;
            float depth = stairwell.topY - stairwell.bottomY;
            int steps = Mathf.Max(2, stairwell.stepCount);
            float run = stairwell.alongZ ? opening.height : opening.width;
            float tread = run / steps;
            float riser = depth / steps;
            float module = WolfMiniConstants.FloorTextureModule / Mathf.Max(1f, stairwell.treadUvScale);
            int sign = stairwell.descendSign >= 0 ? 1 : -1;

            foreach (WallSegmentSpec shaftWall in CreateShaftWalls(stairwell))
            {
                EmitWall(buffer, wallSubmesh, overrideSubmeshes, overrideMaterials, shaftWall);
            }

            // Slab edges at both transverse cuts: the side shaft walls already
            // close the long edges, while these two-sided faces give the upper
            // floor/lower ceiling band visible wall-material thickness at both
            // stair ends.
            foreach (WallSegmentSpec slabEdge in CreateStairwellSlabEdges(stairwell))
            {
                EmitWall(buffer, wallSubmesh, overrideSubmeshes, overrideMaterials, slabEdge);
            }

            float topEdge = stairwell.alongZ
                ? (sign > 0 ? opening.yMin : opening.yMax)
                : (sign > 0 ? opening.xMin : opening.xMax);

            for (int step = 0; step < steps; step++)
            {
                float near = topEdge + sign * step * tread;
                float far = near + sign * tread;
                float treadSurfaceY = stairwell.topY - step * riser;

                Rect treadRect = stairwell.alongZ
                    ? Rect.MinMaxRect(opening.xMin, Mathf.Min(near, far), opening.xMax, Mathf.Max(near, far))
                    : Rect.MinMaxRect(Mathf.Min(near, far), opening.yMin, Mathf.Max(near, far), opening.yMax);
                AddFloorQuad(buffer, floorSubmesh, treadRect, treadSurfaceY, module);

                // Riser at the step's high edge, facing down the descent. The
                // upper tread is already flush with the upper floor, while every
                // following tread steps down by one uniform riser.
                if (step == 0)
                {
                    continue;
                }

                AddStairRiser(
                    buffer,
                    floorSubmesh,
                    stairwell,
                    opening,
                    sign,
                    near,
                    treadSurfaceY,
                    treadSurfaceY + riser,
                    module);
            }

            float mouthPlane = topEdge + sign * run;
            AddStairRiser(
                buffer,
                floorSubmesh,
                stairwell,
                opening,
                sign,
                mouthPlane,
                stairwell.bottomY,
                stairwell.bottomY + riser,
                module);
        }

        private static void AddStairRiser(
            WolfMeshBuffer buffer,
            int floorSubmesh,
            StairwellSpec stairwell,
            Rect opening,
            int sign,
            float riserPlane,
            float riserBottom,
            float riserTop,
            float module)
        {
            Vector2 origin;
            Vector2 direction;
            Vector3 normal;
            if (stairwell.alongZ)
            {
                normal = new Vector3(0f, 0f, sign);
                direction = new Vector2(-sign, 0f);
                origin = new Vector2(sign > 0 ? opening.xMax : opening.xMin, riserPlane);
            }
            else
            {
                normal = new Vector3(sign, 0f, 0f);
                direction = new Vector2(0f, sign);
                origin = new Vector2(riserPlane, sign > 0 ? opening.yMin : opening.yMax);
            }

            float width = stairwell.alongZ ? opening.width : opening.height;
            float height = riserTop - riserBottom;
            Rect riserUV = new Rect(Vector2.Dot(origin, direction) / module, riserBottom / module, width / module, height / module);
            AddWallQuad(buffer, floorSubmesh, origin, direction, normal, 0f, width, riserBottom, riserTop, riserUV);
        }

        /// <summary>
        /// Walls of the stair shaft as one-brick-thick boxes: interior faces
        /// flush with the opening for the shaft seen from above, exterior
        /// faces one brick out and end caps at the mouth corners so the
        /// enclosure reads as solid masonry from the storey below. The high
        /// end gets a capped head wall; the low end stays open as the mouth.
        /// </summary>
        private static IEnumerable<WallSegmentSpec> CreateShaftWalls(StairwellSpec stairwell)
        {
            Rect o = stairwell.opening;
            int sign = stairwell.descendSign >= 0 ? 1 : -1;
            float t = Mathf.Max(0.01f, stairwell.wallThickness);

            if (stairwell.alongZ)
            {
                // Interior faces flush with the opening.
                yield return MakeShaftWall(stairwell, new Vector2(o.xMin, o.yMin), new Vector2(o.xMin, o.yMax));
                yield return MakeShaftWall(stairwell, new Vector2(o.xMax, o.yMax), new Vector2(o.xMax, o.yMin));
                // Exterior faces one brick out.
                yield return MakeShaftWall(stairwell, new Vector2(o.xMin - t, o.yMax), new Vector2(o.xMin - t, o.yMin));
                yield return MakeShaftWall(stairwell, new Vector2(o.xMax + t, o.yMin), new Vector2(o.xMax + t, o.yMax));

                float zHead = sign > 0 ? o.yMin : o.yMax;
                float zHeadOut = zHead - sign * t;
                float zMouth = sign > 0 ? o.yMax : o.yMin;
                if (sign > 0)
                {
                    // End caps at the mouth corners, facing out of the mouth.
                    yield return MakeShaftWall(stairwell, new Vector2(o.xMin, zMouth), new Vector2(o.xMin - t, zMouth));
                    yield return MakeShaftWall(stairwell, new Vector2(o.xMax + t, zMouth), new Vector2(o.xMax, zMouth));
                    // Head wall outer face and its end caps.
                    yield return MakeShaftWall(stairwell, new Vector2(o.xMin - t, zHeadOut), new Vector2(o.xMax + t, zHeadOut));
                    yield return MakeShaftWall(stairwell, new Vector2(o.xMin - t, zHead), new Vector2(o.xMin - t, zHeadOut));
                    yield return MakeShaftWall(stairwell, new Vector2(o.xMax + t, zHeadOut), new Vector2(o.xMax + t, zHead));
                }
                else
                {
                    yield return MakeShaftWall(stairwell, new Vector2(o.xMin - t, zMouth), new Vector2(o.xMin, zMouth));
                    yield return MakeShaftWall(stairwell, new Vector2(o.xMax, zMouth), new Vector2(o.xMax + t, zMouth));
                    yield return MakeShaftWall(stairwell, new Vector2(o.xMax + t, zHeadOut), new Vector2(o.xMin - t, zHeadOut));
                    yield return MakeShaftWall(stairwell, new Vector2(o.xMin - t, zHeadOut), new Vector2(o.xMin - t, zHead));
                    yield return MakeShaftWall(stairwell, new Vector2(o.xMax + t, zHead), new Vector2(o.xMax + t, zHeadOut));
                }
            }
            else
            {
                yield return MakeShaftWall(stairwell, new Vector2(o.xMax, o.yMin), new Vector2(o.xMin, o.yMin));
                yield return MakeShaftWall(stairwell, new Vector2(o.xMin, o.yMax), new Vector2(o.xMax, o.yMax));
                yield return MakeShaftWall(stairwell, new Vector2(o.xMin, o.yMin - t), new Vector2(o.xMax, o.yMin - t));
                yield return MakeShaftWall(stairwell, new Vector2(o.xMax, o.yMax + t), new Vector2(o.xMin, o.yMax + t));

                float xHead = sign > 0 ? o.xMin : o.xMax;
                float xHeadOut = xHead - sign * t;
                float xMouth = sign > 0 ? o.xMax : o.xMin;
                if (sign > 0)
                {
                    yield return MakeShaftWall(stairwell, new Vector2(xMouth, o.yMin - t), new Vector2(xMouth, o.yMin));
                    yield return MakeShaftWall(stairwell, new Vector2(xMouth, o.yMax), new Vector2(xMouth, o.yMax + t));
                    yield return MakeShaftWall(stairwell, new Vector2(xHeadOut, o.yMax + t), new Vector2(xHeadOut, o.yMin - t));
                    yield return MakeShaftWall(stairwell, new Vector2(xHeadOut, o.yMin - t), new Vector2(xHead, o.yMin - t));
                    yield return MakeShaftWall(stairwell, new Vector2(xHead, o.yMax + t), new Vector2(xHeadOut, o.yMax + t));
                }
                else
                {
                    yield return MakeShaftWall(stairwell, new Vector2(xMouth, o.yMin), new Vector2(xMouth, o.yMin - t));
                    yield return MakeShaftWall(stairwell, new Vector2(xMouth, o.yMax + t), new Vector2(xMouth, o.yMax));
                    yield return MakeShaftWall(stairwell, new Vector2(xHeadOut, o.yMin - t), new Vector2(xHeadOut, o.yMax + t));
                    yield return MakeShaftWall(stairwell, new Vector2(xHead, o.yMin - t), new Vector2(xHeadOut, o.yMin - t));
                    yield return MakeShaftWall(stairwell, new Vector2(xHeadOut, o.yMax + t), new Vector2(xHead, o.yMax + t));
                }
            }
        }

        private void AddFloorOpeningGeometry(
            WolfMeshBuffer buffer,
            int wallSubmesh,
            Dictionary<Material, int> overrideSubmeshes,
            List<Material> overrideMaterials,
            FloorOpeningSpec opening)
        {
            foreach (WallSegmentSpec slabEdge in CreateFloorOpeningSlabEdges(opening))
            {
                EmitWall(buffer, wallSubmesh, overrideSubmeshes, overrideMaterials, slabEdge);
            }
        }

        /// <summary>
        /// Metal guard rails on every gallery tier: around the atrium voids
        /// (clipped at adjacent stair mouths, like the slab bands) and along
        /// the pit sides of stairwells that descend out of such a gallery.
        /// One combined mesh with the rail material; the mesh doubles as the
        /// collider so the player cannot walk off a gallery edge while shots
        /// still pass between the bars.
        /// </summary>
        private void BuildRailings(Transform parent)
        {
            var buffer = new WolfMeshBuffer(submeshCount: 1);
            var postAnchors = new HashSet<Vector3Int>();

            if (definition.floorOpenings != null)
            {
                foreach (FloorOpeningSpec opening in definition.floorOpenings)
                {
                    if (opening == null)
                    {
                        continue;
                    }

                    Rect o = opening.opening;
                    foreach (float plane in FloorPlanesCutByOpening(opening))
                    {
                        AddOpeningEdgeRailings(buffer, postAnchors, o, new Vector2(o.xMin, o.yMin), new Vector2(o.xMax, o.yMin), plane);
                        AddOpeningEdgeRailings(buffer, postAnchors, o, new Vector2(o.xMax, o.yMin), new Vector2(o.xMax, o.yMax), plane);
                        AddOpeningEdgeRailings(buffer, postAnchors, o, new Vector2(o.xMax, o.yMax), new Vector2(o.xMin, o.yMax), plane);
                        AddOpeningEdgeRailings(buffer, postAnchors, o, new Vector2(o.xMin, o.yMax), new Vector2(o.xMin, o.yMin), plane);
                    }
                }
            }

            AddStairwellPitRailings(buffer, postAnchors);

            if (buffer.IsEmpty)
            {
                return;
            }

            GameObject railings = new GameObject("Atrium Railings");
            railings.transform.SetParent(parent, false);
            railings.isStatic = true;

            Mesh mesh = buffer.ToMesh($"{definition.levelName}_Railings");
            railings.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = railings.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = materialLibrary.RailMaterial;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.BlendProbesAndSkybox;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.BlendProbes;
            renderer.receiveShadows = true;
            railings.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        private void AddOpeningEdgeRailings(WolfMeshBuffer buffer, HashSet<Vector3Int> postAnchors, Rect opening, Vector2 start, Vector2 end, float plane)
        {
            foreach (EdgeSegment segment in ClipOpeningEdgeByAdjacentStairwells(opening, start, end, plane))
            {
                AddRailingRun(buffer, postAnchors, segment.start, segment.end, plane);
            }
        }

        /// <summary>
        /// Guard rails along both long sides of a stair pit cut into a gallery
        /// floor next to an atrium void. The head end stays open as the stair
        /// entry; the mouth end borders the void where the gallery has no
        /// floor to stand on, so no rail is needed there.
        /// </summary>
        private void AddStairwellPitRailings(WolfMeshBuffer buffer, HashSet<Vector3Int> postAnchors)
        {
            if (definition.stairwells == null)
            {
                return;
            }

            foreach (StairwellSpec stairwell in definition.stairwells)
            {
                if (stairwell == null || !StairwellBordersFloorOpening(stairwell))
                {
                    continue;
                }

                Rect o = stairwell.opening;
                if (stairwell.alongZ)
                {
                    AddPitEdgeRailings(buffer, postAnchors, o, new Vector2(o.xMin, o.yMax), new Vector2(o.xMin, o.yMin), stairwell.topY);
                    AddPitEdgeRailings(buffer, postAnchors, o, new Vector2(o.xMax, o.yMin), new Vector2(o.xMax, o.yMax), stairwell.topY);
                }
                else
                {
                    AddPitEdgeRailings(buffer, postAnchors, o, new Vector2(o.xMin, o.yMin), new Vector2(o.xMax, o.yMin), stairwell.topY);
                    AddPitEdgeRailings(buffer, postAnchors, o, new Vector2(o.xMax, o.yMax), new Vector2(o.xMin, o.yMax), stairwell.topY);
                }
            }
        }

        private void AddPitEdgeRailings(WolfMeshBuffer buffer, HashSet<Vector3Int> postAnchors, Rect pit, Vector2 start, Vector2 end, float plane)
        {
            foreach (EdgeSegment segment in ClipStairwellEdgeByAdjacentFloorOpenings(pit, start, end, plane))
            {
                AddRailingRun(buffer, postAnchors, segment.start, segment.end, plane);
            }
        }

        private bool StairwellBordersFloorOpening(StairwellSpec stairwell)
        {
            const float eps = 0.001f;
            if (definition.floorOpenings == null)
            {
                return false;
            }

            foreach (FloorOpeningSpec opening in definition.floorOpenings)
            {
                if (opening != null &&
                    stairwell.topY <= opening.topY + eps &&
                    stairwell.topY > opening.bottomY + eps &&
                    RectanglesTouchOnEdge(stairwell.opening, opening.opening, eps))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// One straight railing run standing on the gallery floor just inside
        /// the guarded edge: posts with caps, a solid top rail and three thin
        /// bars. The edge runs with the same winding as the slab edges, so the
        /// gallery side is Cross(up, end - start). Post anchors are de-duplicated
        /// across runs because collinear void and pit runs share their end posts.
        /// </summary>
        private static void AddRailingRun(WolfMeshBuffer buffer, HashSet<Vector3Int> postAnchors, Vector2 start, Vector2 end, float floorY)
        {
            Vector2 delta = end - start;
            float length = delta.magnitude;
            if (length < RailingPostSize * 2f)
            {
                return;
            }

            Vector2 direction = delta / length;
            Vector2 lineStart = start + new Vector2(direction.y, -direction.x) * RailingInset;

            Vector3 along = new Vector3(direction.x, 0f, direction.y);
            Vector3 side = Vector3.Cross(Vector3.up, along);
            Vector3 origin = new Vector3(lineStart.x, floorY, lineStart.y);
            Vector3 center = origin + along * (length * 0.5f);

            AddRailingBox(buffer, center + Vector3.up * (RailingGuardHeight - RailingTopRailHeight * 0.5f),
                along, side, length, RailingTopRailWidth, RailingTopRailHeight);
            foreach (float barHeight in RailingBarHeights)
            {
                AddRailingBox(buffer, center + Vector3.up * barHeight, along, side, length, RailingBarSize, RailingBarSize);
            }

            int posts = Mathf.Max(2, Mathf.CeilToInt(length / RailingPostSpacing) + 1);
            for (int i = 0; i < posts; i++)
            {
                float t = i / (float)(posts - 1);
                Vector3 basePosition = origin + along * (length * t);
                var anchor = new Vector3Int(
                    Mathf.RoundToInt(basePosition.x * 1000f),
                    Mathf.RoundToInt(basePosition.y * 1000f),
                    Mathf.RoundToInt(basePosition.z * 1000f));
                if (!postAnchors.Add(anchor))
                {
                    continue;
                }

                AddRailingBox(buffer, basePosition + Vector3.up * (RailingPostHeight * 0.5f),
                    along, side, RailingPostSize, RailingPostSize, RailingPostHeight);
                AddRailingBox(buffer, basePosition + Vector3.up * (RailingPostHeight + RailingPostCapHeight * 0.5f),
                    along, side, RailingPostCapSize, RailingPostCapSize, RailingPostCapHeight);
            }
        }

        /// <summary>Axis box from an orthonormal (along, side, up) frame: long faces, end caps, top and bottom.</summary>
        private static void AddRailingBox(WolfMeshBuffer buffer, Vector3 center, Vector3 along, Vector3 side, float length, float width, float height)
        {
            const int submesh = 0;
            Vector3 a = along * (length * 0.5f);
            Vector3 s = side * (width * 0.5f);
            Vector3 h = Vector3.up * (height * 0.5f);

            Rect sideUV = new Rect(0f, 0f, length, height);
            Rect capUV = new Rect(0f, 0f, width, height);
            Rect flatUV = new Rect(0f, 0f, length, width);

            buffer.AddQuad(submesh, center - a + s - h, center + a + s - h, center + a + s + h, center - a + s + h, side, sideUV);
            buffer.AddQuad(submesh, center + a - s - h, center - a - s - h, center - a - s + h, center + a - s + h, -side, sideUV);
            buffer.AddQuad(submesh, center + a + s - h, center + a - s - h, center + a - s + h, center + a + s + h, along, capUV);
            buffer.AddQuad(submesh, center - a - s - h, center - a + s - h, center - a + s + h, center - a - s + h, -along, capUV);
            buffer.AddQuad(submesh, center - a + s + h, center + a + s + h, center + a - s + h, center - a - s + h, Vector3.up, flatUV);
            buffer.AddQuad(submesh, center + a + s - h, center - a + s - h, center - a - s - h, center + a - s - h, Vector3.down, flatUV);
        }

        /// <summary>
        /// Two-sided slab faces around a rectangular gallery/atrium opening,
        /// one band per floor plane the void cuts, so a multi-storey opening
        /// shows the slab on every gallery tier. Adjacent stair openings remove
        /// the shared edge span, so no band hangs across a stair mouth.
        /// </summary>
        private IEnumerable<WallSegmentSpec> CreateFloorOpeningSlabEdges(FloorOpeningSpec spec)
        {
            Rect opening = spec.opening;
            int style = spec.wallStyle;
            const float height = WolfMiniConstants.FloorSlabThickness;

            foreach (float plane in FloorPlanesCutByOpening(spec))
            {
                float baseY = plane - WolfMiniConstants.FloorSlabThickness;

                foreach (EdgeSegment segment in ClipOpeningEdgeByAdjacentStairwells(opening, new Vector2(opening.xMin, opening.yMin), new Vector2(opening.xMax, opening.yMin), plane))
                {
                    foreach (WallSegmentSpec edge in CreateDoubleSidedSlabEdge(segment.start, segment.end, baseY, height, style))
                    {
                        yield return edge;
                    }
                }

                foreach (EdgeSegment segment in ClipOpeningEdgeByAdjacentStairwells(opening, new Vector2(opening.xMax, opening.yMin), new Vector2(opening.xMax, opening.yMax), plane))
                {
                    foreach (WallSegmentSpec edge in CreateDoubleSidedSlabEdge(segment.start, segment.end, baseY, height, style))
                    {
                        yield return edge;
                    }
                }

                foreach (EdgeSegment segment in ClipOpeningEdgeByAdjacentStairwells(opening, new Vector2(opening.xMax, opening.yMax), new Vector2(opening.xMin, opening.yMax), plane))
                {
                    foreach (WallSegmentSpec edge in CreateDoubleSidedSlabEdge(segment.start, segment.end, baseY, height, style))
                    {
                        yield return edge;
                    }
                }

                foreach (EdgeSegment segment in ClipOpeningEdgeByAdjacentStairwells(opening, new Vector2(opening.xMin, opening.yMax), new Vector2(opening.xMin, opening.yMin), plane))
                {
                    foreach (WallSegmentSpec edge in CreateDoubleSidedSlabEdge(segment.start, segment.end, baseY, height, style))
                    {
                        yield return edge;
                    }
                }
            }
        }

        /// <summary>
        /// Distinct sector floor planes the opening punches through, highest
        /// first: every storey floor above the opening's own (intact) bottom
        /// floor whose sector overlaps the void footprint.
        /// </summary>
        private List<float> FloorPlanesCutByOpening(FloorOpeningSpec spec)
        {
            const float eps = 0.001f;
            var planes = new List<float>();
            foreach (SectorSpec sector in definition.sectors)
            {
                if (sector?.floorAreas == null ||
                    sector.floorY > spec.topY + eps ||
                    sector.floorY <= spec.bottomY + eps ||
                    planes.Exists(p => Mathf.Abs(p - sector.floorY) <= eps))
                {
                    continue;
                }

                foreach (Rect area in sector.floorAreas)
                {
                    if (area.Overlaps(spec.opening))
                    {
                        planes.Add(sector.floorY);
                        break;
                    }
                }
            }

            planes.Sort((a, b) => b.CompareTo(a));
            return planes;
        }

        private IEnumerable<EdgeSegment> ClipOpeningEdgeByAdjacentStairwells(Rect opening, Vector2 start, Vector2 end, float topY)
        {
            const float eps = 0.001f;
            bool alongX = Mathf.Abs(start.y - end.y) <= eps;
            bool alongZ = Mathf.Abs(start.x - end.x) <= eps;
            if ((!alongX && !alongZ) || definition.stairwells == null)
            {
                yield return new EdgeSegment(start, end);
                yield break;
            }

            float edgeMin = alongX ? Mathf.Min(start.x, end.x) : Mathf.Min(start.y, end.y);
            float edgeMax = alongX ? Mathf.Max(start.x, end.x) : Mathf.Max(start.y, end.y);
            var blocked = new List<Vector2>();

            foreach (StairwellSpec stairwell in definition.stairwells)
            {
                // Only stair shafts that cut through this floor plane can
                // interrupt the band along it.
                if (stairwell == null || topY > stairwell.topY + eps || topY < stairwell.bottomY - eps)
                {
                    continue;
                }

                Rect stair = stairwell.opening;
                if (!RectanglesTouchOnEdge(opening, stair, eps))
                {
                    continue;
                }

                if (alongX)
                {
                    float z = start.y;
                    if (Mathf.Abs(stair.yMax - z) > eps && Mathf.Abs(stair.yMin - z) > eps)
                    {
                        continue;
                    }

                    AddBlockedInterval(blocked, edgeMin, edgeMax, stair.xMin, stair.xMax, eps);
                }
                else
                {
                    float x = start.x;
                    if (Mathf.Abs(stair.xMax - x) > eps && Mathf.Abs(stair.xMin - x) > eps)
                    {
                        continue;
                    }

                    AddBlockedInterval(blocked, edgeMin, edgeMax, stair.yMin, stair.yMax, eps);
                }
            }

            if (blocked.Count == 0)
            {
                yield return new EdgeSegment(start, end);
                yield break;
            }

            blocked.Sort((a, b) => a.x.CompareTo(b.x));
            float cursor = edgeMin;
            foreach (Vector2 interval in blocked)
            {
                if (interval.x > cursor + eps)
                {
                    yield return MakeEdgeSegment(start, end, alongX, cursor, interval.x);
                }

                cursor = Mathf.Max(cursor, interval.y);
            }

            if (cursor < edgeMax - eps)
            {
                yield return MakeEdgeSegment(start, end, alongX, cursor, edgeMax);
            }
        }

        private IEnumerable<EdgeSegment> ClipStairwellEdgeByAdjacentFloorOpenings(Rect stairwell, Vector2 start, Vector2 end, float topY)
        {
            const float eps = 0.001f;
            bool alongX = Mathf.Abs(start.y - end.y) <= eps;
            bool alongZ = Mathf.Abs(start.x - end.x) <= eps;
            if ((!alongX && !alongZ) || definition.floorOpenings == null)
            {
                yield return new EdgeSegment(start, end);
                yield break;
            }

            float edgeMin = alongX ? Mathf.Min(start.x, end.x) : Mathf.Min(start.y, end.y);
            float edgeMax = alongX ? Mathf.Max(start.x, end.x) : Mathf.Max(start.y, end.y);
            var blocked = new List<Vector2>();

            foreach (FloorOpeningSpec opening in definition.floorOpenings)
            {
                // Only voids that cut through this floor plane can swallow
                // the band along it.
                if (opening == null || topY > opening.topY + eps || topY <= opening.bottomY + eps)
                {
                    continue;
                }

                Rect floorOpening = opening.opening;
                if (!RectanglesTouchOnEdge(stairwell, floorOpening, eps))
                {
                    continue;
                }

                if (alongX)
                {
                    float z = start.y;
                    if (Mathf.Abs(floorOpening.yMax - z) > eps && Mathf.Abs(floorOpening.yMin - z) > eps)
                    {
                        continue;
                    }

                    AddBlockedInterval(blocked, edgeMin, edgeMax, floorOpening.xMin, floorOpening.xMax, eps);
                }
                else
                {
                    float x = start.x;
                    if (Mathf.Abs(floorOpening.xMax - x) > eps && Mathf.Abs(floorOpening.xMin - x) > eps)
                    {
                        continue;
                    }

                    AddBlockedInterval(blocked, edgeMin, edgeMax, floorOpening.yMin, floorOpening.yMax, eps);
                }
            }

            if (blocked.Count == 0)
            {
                yield return new EdgeSegment(start, end);
                yield break;
            }

            blocked.Sort((a, b) => a.x.CompareTo(b.x));
            float cursor = edgeMin;
            foreach (Vector2 interval in blocked)
            {
                if (interval.x > cursor + eps)
                {
                    yield return MakeEdgeSegment(start, end, alongX, cursor, interval.x);
                }

                cursor = Mathf.Max(cursor, interval.y);
            }

            if (cursor < edgeMax - eps)
            {
                yield return MakeEdgeSegment(start, end, alongX, cursor, edgeMax);
            }
        }

        private static bool RectanglesTouchOnEdge(Rect a, Rect b, float eps)
        {
            bool touchesVertical = Mathf.Abs(a.xMin - b.xMax) <= eps || Mathf.Abs(a.xMax - b.xMin) <= eps;
            bool overlapsZ = Mathf.Min(a.yMax, b.yMax) > Mathf.Max(a.yMin, b.yMin) + eps;
            bool touchesHorizontal = Mathf.Abs(a.yMin - b.yMax) <= eps || Mathf.Abs(a.yMax - b.yMin) <= eps;
            bool overlapsX = Mathf.Min(a.xMax, b.xMax) > Mathf.Max(a.xMin, b.xMin) + eps;
            return (touchesVertical && overlapsZ) || (touchesHorizontal && overlapsX);
        }

        private static void AddBlockedInterval(List<Vector2> blocked, float edgeMin, float edgeMax, float min, float max, float eps)
        {
            float clippedMin = Mathf.Max(edgeMin, min);
            float clippedMax = Mathf.Min(edgeMax, max);
            if (clippedMax > clippedMin + eps)
            {
                blocked.Add(new Vector2(clippedMin, clippedMax));
            }
        }

        private static EdgeSegment MakeEdgeSegment(Vector2 start, Vector2 end, bool alongX, float min, float max)
        {
            if (alongX)
            {
                bool forward = start.x <= end.x;
                float z = start.y;
                return forward
                    ? new EdgeSegment(new Vector2(min, z), new Vector2(max, z))
                    : new EdgeSegment(new Vector2(max, z), new Vector2(min, z));
            }

            bool zForward = start.y <= end.y;
            float x = start.x;
            return zForward
                ? new EdgeSegment(new Vector2(x, min), new Vector2(x, max))
                : new EdgeSegment(new Vector2(x, max), new Vector2(x, min));
        }

        /// <summary>
        /// Two-sided slab faces on the two transverse sides of the stair opening.
        /// The long sides are already covered by the full-height shaft walls, so
        /// emitting only these end bands avoids duplicate faces along the sides.
        /// </summary>
        private IEnumerable<WallSegmentSpec> CreateStairwellSlabEdges(StairwellSpec stairwell)
        {
            Rect o = stairwell.opening;
            float baseY = stairwell.topY - WolfMiniConstants.FloorSlabThickness;
            const float height = WolfMiniConstants.FloorSlabThickness;

            if (stairwell.alongZ)
            {
                foreach (EdgeSegment segment in ClipStairwellEdgeByAdjacentFloorOpenings(o, new Vector2(o.xMin, o.yMin), new Vector2(o.xMax, o.yMin), stairwell.topY))
                {
                    foreach (WallSegmentSpec edge in CreateDoubleSidedSlabEdge(segment.start, segment.end, baseY, height, stairwell.wallStyle))
                    {
                        yield return edge;
                    }
                }

                foreach (EdgeSegment segment in ClipStairwellEdgeByAdjacentFloorOpenings(o, new Vector2(o.xMax, o.yMax), new Vector2(o.xMin, o.yMax), stairwell.topY))
                {
                    foreach (WallSegmentSpec edge in CreateDoubleSidedSlabEdge(segment.start, segment.end, baseY, height, stairwell.wallStyle))
                    {
                        yield return edge;
                    }
                }
            }
            else
            {
                foreach (EdgeSegment segment in ClipStairwellEdgeByAdjacentFloorOpenings(o, new Vector2(o.xMin, o.yMax), new Vector2(o.xMin, o.yMin), stairwell.topY))
                {
                    foreach (WallSegmentSpec edge in CreateDoubleSidedSlabEdge(segment.start, segment.end, baseY, height, stairwell.wallStyle))
                    {
                        yield return edge;
                    }
                }

                foreach (EdgeSegment segment in ClipStairwellEdgeByAdjacentFloorOpenings(o, new Vector2(o.xMax, o.yMin), new Vector2(o.xMax, o.yMax), stairwell.topY))
                {
                    foreach (WallSegmentSpec edge in CreateDoubleSidedSlabEdge(segment.start, segment.end, baseY, height, stairwell.wallStyle))
                    {
                        yield return edge;
                    }
                }
            }
        }

        private static IEnumerable<WallSegmentSpec> CreateDoubleSidedSlabEdge(Vector2 start, Vector2 end, float baseY, float height, int style)
        {
            yield return MakeSlabEdge(start, end, baseY, height, style);
            yield return MakeSlabEdge(end, start, baseY, height, style);
        }

        private static WallSegmentSpec MakeSlabEdge(Vector2 start, Vector2 end, float baseY, float height, int style)
        {
            return new WallSegmentSpec
            {
                start = start,
                end = end,
                baseY = baseY,
                height = height,
                style = style
            };
        }

        private readonly struct EdgeSegment
        {
            public readonly Vector2 start;
            public readonly Vector2 end;

            public EdgeSegment(Vector2 start, Vector2 end)
            {
                this.start = start;
                this.end = end;
            }
        }

        private static WallSegmentSpec MakeShaftWall(StairwellSpec stairwell, Vector2 start, Vector2 end)
        {
            return new WallSegmentSpec
            {
                start = start,
                end = end,
                baseY = stairwell.bottomY,
                height = stairwell.topY - stairwell.bottomY,
                style = stairwell.wallStyle
            };
        }

        private void EmitWall(WolfMeshBuffer buffer, int wallSubmesh, Dictionary<Material, int> overrideSubmeshes, List<Material> overrideMaterials, WallSegmentSpec wall)
        {
            if (materialLibrary.TryGetWallOverride(wall.style, out Material overrideMaterial, out bool overrideTiles))
            {
                int submesh = GetOverrideSubmesh(overrideSubmeshes, overrideMaterials, overrideMaterial);
                if (overrideTiles)
                {
                    AddRepeatingWall(buffer, submesh, wall);
                    return;
                }

                // Clamped feature texture (prison front): square panel at the
                // bottom, trim band up to the ceiling — never a second copy of
                // the picture.
                Material trim = materialLibrary.DoorJambMaterial;
                if (trim != null)
                {
                    int trimSubmesh = GetOverrideSubmesh(overrideSubmeshes, overrideMaterials, trim);
                    AddFeatureWall(buffer, submesh, trimSubmesh, trimIsAtlas: false, Vector2.zero, wall);
                }
                else
                {
                    Rect jambWindow = WolfFull3DMaterialLibrary.GetAtlasTileUV(
                        WolfFull3DMaterialLibrary.DoorJambTile, WolfFull3DMaterialLibrary.WallAtlasSize);
                    AddFeatureWall(buffer, submesh, wallSubmesh, trimIsAtlas: true, jambWindow.min, wall);
                }

                return;
            }

            Rect window = WolfFull3DMaterialLibrary.GetWallTileUV(wall.style);
            AddAtlasWall(buffer, wallSubmesh, wall, window.min);
        }

        private readonly struct OpeningCut
        {
            public readonly bool wallAlongX;
            public readonly float plane;
            public readonly float min;
            public readonly float max;
            public readonly float yMin;
            public readonly float yMax;

            public OpeningCut(bool wallAlongX, float plane, float min, float max, float yMin, float yMax)
            {
                this.wallAlongX = wallAlongX;
                this.plane = plane;
                this.min = min;
                this.max = max;
                this.yMin = yMin;
                this.yMax = yMax;
            }
        }

        /// <summary>
        /// Cut intervals on the room-facing wall planes of each doorway whose
        /// opening is wider than the door cell.
        /// </summary>
        private List<OpeningCut> BuildOpeningCuts()
        {
            var cuts = new List<OpeningCut>();
            foreach (DoorwaySpec doorway in definition.doorways)
            {
                if (doorway == null || doorway.width <= Cell + 0.001f)
                {
                    continue;
                }

                float half = doorway.width * 0.5f;
                float cellHalf = Cell * 0.5f;
                float y0 = doorway.baseY;
                float y1 = doorway.baseY + doorway.ceilingHeight;
                Vector2 c = doorway.center;
                if (doorway.alongZ)
                {
                    cuts.Add(new OpeningCut(false, c.x - cellHalf, c.y - half, c.y + half, y0, y1));
                    cuts.Add(new OpeningCut(false, c.x + cellHalf, c.y - half, c.y + half, y0, y1));
                }
                else
                {
                    cuts.Add(new OpeningCut(true, c.y - cellHalf, c.x - half, c.x + half, y0, y1));
                    cuts.Add(new OpeningCut(true, c.y + cellHalf, c.x - half, c.x + half, y0, y1));
                }
            }

            return cuts;
        }

        private static IEnumerable<WallSegmentSpec> ClipWallByOpenings(WallSegmentSpec wall, List<OpeningCut> cuts)
        {
            const float eps = 0.001f;
            bool alongX = Mathf.Abs(wall.start.y - wall.end.y) < eps;
            bool alongZ = Mathf.Abs(wall.start.x - wall.end.x) < eps;
            if (alongX == alongZ)
            {
                // Diagonal or degenerate: doorways never sit on those.
                yield return wall;
                yield break;
            }

            float plane = alongX ? wall.start.y : wall.start.x;
            float a = alongX ? wall.start.x : wall.start.y;
            float b = alongX ? wall.end.x : wall.end.y;

            var intervals = new List<Vector2> { new Vector2(Mathf.Min(a, b), Mathf.Max(a, b)) };
            foreach (OpeningCut cut in cuts)
            {
                if (cut.wallAlongX != alongX || Mathf.Abs(cut.plane - plane) > eps)
                {
                    continue;
                }

                // A doorway only cuts walls of its own storey: a wall band
                // below or above the opening's vertical range stays intact.
                if (wall.baseY + wall.height <= cut.yMin + eps || wall.baseY >= cut.yMax - eps)
                {
                    continue;
                }

                var next = new List<Vector2>();
                foreach (Vector2 interval in intervals)
                {
                    if (cut.max <= interval.x + eps || cut.min >= interval.y - eps)
                    {
                        next.Add(interval);
                        continue;
                    }

                    if (cut.min > interval.x + eps)
                    {
                        next.Add(new Vector2(interval.x, cut.min));
                    }

                    if (cut.max < interval.y - eps)
                    {
                        next.Add(new Vector2(cut.max, interval.y));
                    }
                }

                intervals = next;
            }

            bool ascending = b >= a;
            foreach (Vector2 interval in intervals)
            {
                float pieceA = ascending ? interval.x : interval.y;
                float pieceB = ascending ? interval.y : interval.x;
                yield return new WallSegmentSpec
                {
                    start = alongX ? new Vector2(pieceA, plane) : new Vector2(plane, pieceA),
                    end = alongX ? new Vector2(pieceB, plane) : new Vector2(plane, pieceB),
                    baseY = wall.baseY,
                    height = wall.height,
                    style = wall.style
                };
            }
        }

        /// <summary>
        /// Threshold floor, full-height ceiling and the two jambs of a widened
        /// doorway. Narrow (handmade) doorways skip this: their reveals are part
        /// of the floor plan.
        /// </summary>
        private void AddDoorwayGeometry(
            WolfMeshBuffer buffer,
            int floorSubmesh,
            int ceilingSubmesh,
            int wallSubmesh,
            Dictionary<Material, int> overrideSubmeshes,
            List<Material> overrideMaterials,
            DoorwaySpec doorway)
        {
            if (doorway == null || doorway.width <= Cell + 0.001f)
            {
                return;
            }

            float half = doorway.width * 0.5f;
            float cellHalf = Cell * 0.5f;
            Vector2 c = doorway.center;

            Rect area = doorway.alongZ
                ? new Rect(c.x - cellHalf, c.y - half, Cell, doorway.width)
                : new Rect(c.x - half, c.y - cellHalf, doorway.width, Cell);
            AddHorizontalQuads(buffer, floorSubmesh, ceilingSubmesh, area, doorway.baseY, doorway.baseY + doorway.ceilingHeight);

            WallSegmentSpec firstJamb;
            WallSegmentSpec secondJamb;
            if (doorway.alongZ)
            {
                firstJamb = MakeJamb(new Vector2(c.x + cellHalf, c.y - half), new Vector2(c.x - cellHalf, c.y - half), doorway);
                secondJamb = MakeJamb(new Vector2(c.x - cellHalf, c.y + half), new Vector2(c.x + cellHalf, c.y + half), doorway);
            }
            else
            {
                firstJamb = MakeJamb(new Vector2(c.x - half, c.y - cellHalf), new Vector2(c.x - half, c.y + cellHalf), doorway);
                secondJamb = MakeJamb(new Vector2(c.x + half, c.y + cellHalf), new Vector2(c.x + half, c.y - cellHalf), doorway);
            }

            EmitWall(buffer, wallSubmesh, overrideSubmeshes, overrideMaterials, firstJamb);
            EmitWall(buffer, wallSubmesh, overrideSubmeshes, overrideMaterials, secondJamb);
        }

        private static WallSegmentSpec MakeJamb(Vector2 start, Vector2 end, DoorwaySpec doorway)
        {
            return new WallSegmentSpec
            {
                start = start,
                end = end,
                baseY = doorway.baseY,
                height = doorway.ceilingHeight,
                style = doorway.headerStyle
            };
        }

        private static int GetOverrideSubmesh(Dictionary<Material, int> overrideSubmeshes, List<Material> overrideMaterials, Material material)
        {
            if (!overrideSubmeshes.TryGetValue(material, out int submesh))
            {
                submesh = 3 + overrideMaterials.Count;
                overrideSubmeshes[material] = submesh;
                overrideMaterials.Add(material);
            }

            return submesh;
        }

        private static bool TryGetSegmentFrame(WallSegmentSpec wall, out Vector2 direction, out float length, out Vector3 normal)
        {
            direction = wall.end - wall.start;
            length = direction.magnitude;
            if (length < 0.001f || wall.height <= 0f)
            {
                normal = Vector3.zero;
                return false;
            }

            direction /= length;
            normal = Vector3.Cross(Vector3.up, new Vector3(direction.x, 0f, direction.y)).normalized;
            return true;
        }

        /// <summary>
        /// Continuous module UVs: U is anchored to world distance along the
        /// segment axis, while V is anchored to the local storey height. Lower
        /// walls can extend into the floor slab for sealing, but their visible
        /// room-height section still starts at the bottom of the wall texture
        /// instead of showing an extra repeated strip at floor level.
        /// </summary>
        private static Rect ModuleUV(Vector2 start, Vector2 direction, float length, float baseY, float height)
        {
            float u0 = Vector2.Dot(start, direction) / Module;
            float v0 = StoreyLocalY(baseY) / Module;
            return new Rect(u0, v0, length / Module, height / Module);
        }

        private static float StoreyLocalY(float worldY)
        {
            float storeyHeight = WolfMiniConstants.WallHeight + WolfMiniConstants.FloorSlabThickness;
            float localY = Mathf.Repeat(worldY, storeyHeight);
            return localY >= storeyHeight - 0.001f ? 0f : localY;
        }

        /// <summary>Seamless repeat material: one continuous quad per wall run.</summary>
        private static void AddRepeatingWall(WolfMeshBuffer buffer, int submesh, WallSegmentSpec wall)
        {
            if (!TryGetSegmentFrame(wall, out Vector2 direction, out float length, out Vector3 normal))
            {
                return;
            }

            Rect uv = ModuleUV(wall.start, direction, length, wall.baseY, wall.height);
            AddWallQuad(buffer, submesh, wall.start, direction, normal, 0f, length, wall.baseY, wall.baseY + wall.height, uv);
        }

        /// <summary>
        /// Atlas wall: one continuous quad per run; the WolfMini/AtlasRepeat
        /// shader wraps the module UVs inside the atlas window carried in
        /// TEXCOORD1, so the repeat never bleeds into neighboring tiles.
        /// </summary>
        private static void AddAtlasWall(WolfMeshBuffer buffer, int submesh, WallSegmentSpec wall, Vector2 window)
        {
            if (!TryGetSegmentFrame(wall, out Vector2 direction, out float length, out Vector3 normal))
            {
                return;
            }

            Rect uv = ModuleUV(wall.start, direction, length, wall.baseY, wall.height);
            AddWallQuad(buffer, submesh, wall.start, direction, normal, 0f, length, wall.baseY, wall.baseY + wall.height, uv, window);
        }

        /// <summary>
        /// Clamped feature texture (prison front): one square panel per cell of
        /// length at natural proportions, plus a trim band from the panel top to
        /// the ceiling instead of a duplicated picture.
        /// </summary>
        private static void AddFeatureWall(WolfMeshBuffer buffer, int panelSubmesh, int trimSubmesh, bool trimIsAtlas, Vector2 trimWindow, WallSegmentSpec wall)
        {
            if (!TryGetSegmentFrame(wall, out Vector2 direction, out float length, out Vector3 normal))
            {
                return;
            }

            float panelHeight = Mathf.Min(wall.height, Cell);
            for (float along = 0f; along < length - 0.001f; along += Cell)
            {
                float pieceEnd = Mathf.Min(along + Cell, length);
                float widthFraction = (pieceEnd - along) / Cell;
                Rect panelUV = new Rect(0f, 0f, widthFraction, 1f);
                AddWallQuad(buffer, panelSubmesh, wall.start, direction, normal, along, pieceEnd, wall.baseY, wall.baseY + panelHeight, panelUV);
            }

            float bandHeight = wall.height - panelHeight;
            if (bandHeight < 0.01f)
            {
                return;
            }

            Rect bandUV = ModuleUV(wall.start, direction, length, wall.baseY + panelHeight, bandHeight);
            if (trimIsAtlas)
            {
                AddWallQuad(buffer, trimSubmesh, wall.start, direction, normal, 0f, length, wall.baseY + panelHeight, wall.baseY + wall.height, bandUV, trimWindow);
            }
            else
            {
                AddWallQuad(buffer, trimSubmesh, wall.start, direction, normal, 0f, length, wall.baseY + panelHeight, wall.baseY + wall.height, bandUV);
            }
        }

        private static void AddWallQuad(
            WolfMeshBuffer buffer,
            int submesh,
            Vector2 origin,
            Vector2 direction,
            Vector3 normal,
            float fromDistance,
            float toDistance,
            float y0,
            float y1,
            Rect uv)
        {
            AddWallQuad(buffer, submesh, origin, direction, normal, fromDistance, toDistance, y0, y1, uv, Vector2.zero);
        }

        private static void AddWallQuad(
            WolfMeshBuffer buffer,
            int submesh,
            Vector2 origin,
            Vector2 direction,
            Vector3 normal,
            float fromDistance,
            float toDistance,
            float y0,
            float y1,
            Rect uv,
            Vector2 atlasWindow)
        {
            Vector2 from = origin + direction * fromDistance;
            Vector2 to = origin + direction * toDistance;

            buffer.AddQuad(submesh,
                new Vector3(from.x, y0, from.y),
                new Vector3(to.x, y0, to.y),
                new Vector3(to.x, y1, to.y),
                new Vector3(from.x, y1, from.y),
                normal, uv, atlasWindow);
        }

        private void BuildDoorways(Transform parent)
        {
            foreach (DoorwaySpec doorway in definition.doorways)
            {
                if (doorway == null)
                {
                    continue;
                }

                float doorHeight = Mathf.Min(doorway.height, doorway.ceilingHeight);

                Material faceMaterial = materialLibrary.DoorFaceMaterial;
                Material jambMaterial = materialLibrary.DoorJambMaterial;
                int faceTile = doorway.type switch
                {
                    "elevator" => WolfFull3DMaterialLibrary.ElevatorDoorTile,
                    "gold" => WolfFull3DMaterialLibrary.LockedDoorTile,
                    "silver" => WolfFull3DMaterialLibrary.LockedDoorTile,
                    _ => WolfFull3DMaterialLibrary.NormalDoorTile
                };
                Rect faceUV = faceMaterial != null
                    ? new Rect(0f, 0f, 1f, 1f)
                    : WolfFull3DMaterialLibrary.GetAtlasTileUV(faceTile, WolfFull3DMaterialLibrary.WallAtlasSize);
                Rect jambUV = jambMaterial != null
                    ? new Rect(0f, 0f, 1f, 1f)
                    : WolfFull3DMaterialLibrary.GetAtlasTileUV(WolfFull3DMaterialLibrary.DoorJambTile, WolfFull3DMaterialLibrary.WallAtlasSize);

                // One whole slab, original Wolf style. It retracts toward the
                // side the converter verified has two solid cells in a row, so
                // the open slab hides completely inside the wall.
                Vector3 size = doorway.alongZ
                    ? new Vector3(DoorThickness, doorHeight, doorway.width)
                    : new Vector3(doorway.width, doorHeight, DoorThickness);

                GameObject door = new GameObject($"Door {doorway.type} {doorway.center.x:0},{doorway.center.y:0}");
                door.transform.SetParent(parent, false);
                door.transform.position = new Vector3(doorway.center.x, doorway.baseY + doorHeight * 0.5f, doorway.center.y);

                Mesh mesh = CreateDoorBoxMesh(size, doorway.alongZ, faceUV, jambUV);
                door.AddComponent<MeshFilter>().sharedMesh = mesh;
                MeshRenderer renderer = door.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = new[]
                {
                    faceMaterial != null ? faceMaterial : materialLibrary.WallAtlasMaterial,
                    jambMaterial != null ? jambMaterial : materialLibrary.WallAtlasMaterial
                };
                renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.BlendProbesAndSkybox;
                renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.BlendProbes;
                renderer.receiveShadows = true;
                BoxCollider collider = door.AddComponent<BoxCollider>();
                collider.size = size;

                int slideSign = doorway.slideSign >= 0 ? 1 : -1;
                float travel = doorway.width - 0.02f;
                Vector3 slideOffset = doorway.alongZ
                    ? new Vector3(0f, 0f, slideSign * travel)
                    : new Vector3(slideSign * travel, 0f, 0f);
                bool locked = doorway.type == "gold" || doorway.type == "silver";
                door.AddComponent<WolfDoor>().Configure(slideOffset, 2.4f * WorldScale, 3.5f, locked);
            }
        }

        private static Mesh CreateDoorBoxMesh(Vector3 size, bool alongZ, Rect faceUV, Rect jambUV)
        {
            // Submesh 0 = the two large door faces, submesh 1 = jamb/top/bottom.
            var buffer = new WolfMeshBuffer(submeshCount: 2);
            Vector3 h = size * 0.5f;

            int eastWestSubmesh = alongZ ? 0 : 1;
            int northSouthSubmesh = alongZ ? 1 : 0;
            Rect eastWest = alongZ ? faceUV : jambUV;
            Rect northSouth = alongZ ? jambUV : faceUV;

            buffer.AddQuad(eastWestSubmesh, new Vector3(h.x, -h.y, -h.z), new Vector3(h.x, -h.y, h.z), new Vector3(h.x, h.y, h.z), new Vector3(h.x, h.y, -h.z), Vector3.right, eastWest);
            buffer.AddQuad(eastWestSubmesh, new Vector3(-h.x, -h.y, h.z), new Vector3(-h.x, -h.y, -h.z), new Vector3(-h.x, h.y, -h.z), new Vector3(-h.x, h.y, h.z), Vector3.left, eastWest);
            buffer.AddQuad(northSouthSubmesh, new Vector3(h.x, -h.y, h.z), new Vector3(-h.x, -h.y, h.z), new Vector3(-h.x, h.y, h.z), new Vector3(h.x, h.y, h.z), Vector3.forward, northSouth);
            buffer.AddQuad(northSouthSubmesh, new Vector3(-h.x, -h.y, -h.z), new Vector3(h.x, -h.y, -h.z), new Vector3(h.x, h.y, -h.z), new Vector3(-h.x, h.y, -h.z), Vector3.back, northSouth);
            buffer.AddQuad(1, new Vector3(-h.x, h.y, -h.z), new Vector3(h.x, h.y, -h.z), new Vector3(h.x, h.y, h.z), new Vector3(-h.x, h.y, h.z), Vector3.up, jambUV);
            buffer.AddQuad(1, new Vector3(h.x, -h.y, -h.z), new Vector3(-h.x, -h.y, -h.z), new Vector3(-h.x, -h.y, h.z), new Vector3(h.x, -h.y, h.z), Vector3.down, jambUV);

            return buffer.ToMesh("DoorBox");
        }

        private void BuildProps(Transform parent)
        {
            int lightsBudget = maxRealtimeLights;

            foreach (LevelPropSpec prop in definition.props)
            {
                if (prop == null || prop.typeIndex < 0 || prop.typeIndex >= WolfLevelContent.StatInfos.Length)
                {
                    continue;
                }

                WolfLevelContent.StatInfo info = WolfLevelContent.StatInfos[prop.typeIndex];

                if (prop.typeIndex == WolfLevelContent.CeilLightTypeIndex || prop.typeIndex == WolfLevelContent.ChandelierTypeIndex)
                {
                    if (IsCeilingPropInsideFloorOpening(prop) || IsCeilingPropInsideStairwell(prop))
                    {
                        continue;
                    }

                    bool warm = prop.typeIndex == WolfLevelContent.ChandelierTypeIndex;
                    bool withLight = buildLights && lightsBudget > 0;
                    if (withLight)
                    {
                        lightsBudget--;
                    }

                    AddCeilingLamp(parent, info.name, prop.position, warm, withLight);
                    continue;
                }

                if (prop.typeIndex == WolfLevelContent.PansTypeIndex)
                {
                    continue;
                }

                float scale = WolfLevelContent.GetStaticScale(info.pickupType);
                Material material = materialLibrary.GetStaticMaterial(prop.typeIndex, WolfLevelContent.SpriteOffset);
                CreateBillboard(
                    parent,
                    $"{info.name} {prop.position.x:0},{prop.position.z:0}",
                    new Vector3(prop.position.x, prop.position.y + 0.02f + scale * 0.6f, prop.position.z),
                    new Vector2(scale, scale * 1.2f),
                    material,
                    info.blocking,
                    prop.position.y);
            }
        }

        private bool IsCeilingPropInsideFloorOpening(LevelPropSpec prop)
        {
            if (definition.floorOpenings == null)
            {
                return false;
            }

            const float eps = 0.001f;
            Vector2 point = new Vector2(prop.position.x, prop.position.z);
            foreach (FloorOpeningSpec opening in definition.floorOpenings)
            {
                if (opening == null || prop.position.y > opening.topY + eps || prop.position.y < opening.bottomY - eps)
                {
                    continue;
                }

                Rect rect = opening.opening;
                if (point.x >= rect.xMin - eps && point.x <= rect.xMax + eps &&
                    point.y >= rect.yMin - eps && point.y <= rect.yMax + eps)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Ceiling lamps of storeys crossed by a stair shaft are dropped: the
        /// shaft cuts their ceiling plane, so they would float inside the
        /// staircase, and the storey at the shaft's top would hang its lamp
        /// over the open pit.
        /// </summary>
        private bool IsCeilingPropInsideStairwell(LevelPropSpec prop)
        {
            if (definition.stairwells == null)
            {
                return false;
            }

            const float eps = 0.001f;
            Vector2 point = new Vector2(prop.position.x, prop.position.z);
            foreach (StairwellSpec stairwell in definition.stairwells)
            {
                if (stairwell == null || prop.position.y > stairwell.topY + eps || prop.position.y < stairwell.bottomY - eps)
                {
                    continue;
                }

                Rect rect = stairwell.opening;
                if (point.x >= rect.xMin - eps && point.x <= rect.xMax + eps &&
                    point.y >= rect.yMin - eps && point.y <= rect.yMax + eps)
                {
                    return true;
                }
            }

            return false;
        }

        private void BuildEnemies(Transform parent)
        {
            foreach (LevelEnemySpec enemy in definition.enemies)
            {
                if (enemy == null)
                {
                    continue;
                }

                WolfLevelContent.EnemyInfo info = WolfLevelContent.EnemyInfos.TryGetValue(enemy.type, out WolfLevelContent.EnemyInfo found)
                    ? found
                    : WolfLevelContent.EnemyInfos["guard"];
                Material material = materialLibrary.GetEnemyMaterial(enemy.type, info.tint);
                CreateBillboard(
                    parent,
                    $"{enemy.type} {enemy.position.x:0},{enemy.position.z:0}",
                    new Vector3(enemy.position.x, enemy.position.y + info.height * 0.5f, enemy.position.z),
                    new Vector2(info.width, info.height),
                    material,
                    true,
                    enemy.position.y);
            }
        }

        /// <summary>
        /// Ceiling height above a world position. With stacked storeys several
        /// sectors can cover the same XZ point, so the position belongs to the
        /// one with the highest floor at or below it.
        /// </summary>
        private float CeilingYAt(Vector3 position)
        {
            var point = new Vector2(position.x, position.z);
            SectorSpec best = null;
            foreach (SectorSpec sector in definition.sectors)
            {
                if (sector.floorY > position.y + 0.001f || (best != null && sector.floorY <= best.floorY))
                {
                    continue;
                }

                foreach (Rect area in sector.floorAreas)
                {
                    if (area.Contains(point))
                    {
                        best = sector;
                        break;
                    }
                }
            }

            return best != null ? best.floorY + best.ceilingHeight : position.y + WolfMiniConstants.WallHeight;
        }

        private void AddCeilingLamp(Transform parent, string name, Vector3 basePosition, bool warm, bool withLight)
        {
            float ceilingY = CeilingYAt(basePosition);

            GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cap.name = $"{name} cap {basePosition.x:0},{basePosition.z:0}";
            cap.transform.position = new Vector3(basePosition.x, ceilingY - 0.05f * WorldScale, basePosition.z);
            cap.transform.localScale = new Vector3(0.48f, 0.05f, 0.48f) * WorldScale;
            cap.transform.SetParent(parent, true);
            Material capMaterial = materialLibrary.LampCapMaterial != null
                ? materialLibrary.LampCapMaterial
                : warm ? materialLibrary.LampWarmCap : materialLibrary.LampGreenCap;
            Renderer capRenderer = cap.GetComponent<Renderer>();
            capRenderer.sharedMaterial = capMaterial;
            capRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.BlendProbesAndSkybox;
            capRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.BlendProbes;
            DestroySafely(cap.GetComponent<Collider>());

            GameObject bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulb.name = $"{name} bulb {basePosition.x:0},{basePosition.z:0}";
            bulb.transform.position = new Vector3(basePosition.x, ceilingY - 0.16f * WorldScale, basePosition.z);
            bulb.transform.localScale = Vector3.one * 0.20f * WorldScale;
            bulb.transform.SetParent(parent, true);
            Material bulbMaterial = materialLibrary.LampGlowMaterial != null
                ? materialLibrary.LampGlowMaterial
                : warm ? materialLibrary.LampWarmBulb : materialLibrary.LampGreenBulb;
            Renderer bulbRenderer = bulb.GetComponent<Renderer>();
            bulbRenderer.sharedMaterial = bulbMaterial;
            bulbRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.BlendProbesAndSkybox;
            bulbRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.BlendProbes;
            DestroySafely(bulb.GetComponent<Collider>());

            AddCeilingSpill(parent, name, basePosition, ceilingY, warm);
            AddLampSurfaceReflections(parent, name, basePosition, ceilingY, warm);

            if (!withLight)
            {
                return;
            }

            AddLampLightRig(parent, name, basePosition, ceilingY, warm);
        }

        private void AddCeilingSpill(Transform parent, string name, Vector3 basePosition, float ceilingY, bool warm)
        {
            Material material = warm ? materialLibrary.CeilingSpillWarmMaterial : materialLibrary.CeilingSpillCoolMaterial;
            if (material == null)
            {
                return;
            }

            GameObject spill = GameObject.CreatePrimitive(PrimitiveType.Quad);
            spill.name = $"{name} ceiling spill {basePosition.x:0},{basePosition.z:0}";
            spill.transform.SetParent(parent, true);
            spill.transform.position = new Vector3(basePosition.x, ceilingY - 0.02f, basePosition.z);
            spill.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            float diameter = (warm ? 3.8f : 3.2f) * WorldScale;
            spill.transform.localScale = new Vector3(diameter, diameter, 1f);

            Renderer renderer = spill.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.receiveShadows = false;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            DestroySafely(spill.GetComponent<Collider>());
        }

        private void AddLampSurfaceReflections(Transform parent, string name, Vector3 basePosition, float ceilingY, bool warm)
        {
            Vector3 anchor = new Vector3(basePosition.x, ceilingY - 0.22f * WorldScale, basePosition.z);
            Material floorMaterial = warm ? materialLibrary.LampFloorReflectionWarmMaterial : materialLibrary.LampFloorReflectionCoolMaterial;
            Material wallMaterial = warm ? materialLibrary.LampWallReflectionWarmMaterial : materialLibrary.LampWallReflectionCoolMaterial;
            string suffix = $"{basePosition.x:0},{basePosition.z:0}";

            if (TryFindSceneSurface(anchor, Vector3.down, 24f * WorldScale, SurfaceTarget.Floor, out RaycastHit floorHit))
            {
                float floorWidth = (warm ? 3.8f : 3.3f) * WorldScale;
                float floorHeight = (warm ? 2.7f : 2.35f) * WorldScale;
                CreateBoundedFloorReflectionQuad(
                    parent,
                    $"{name} floor reflection {suffix}",
                    floorHit.point + floorHit.normal * 0.026f,
                    floorHit.normal,
                    floorMaterial,
                    floorWidth,
                    floorHeight);
            }

            Vector3 wallOrigin = anchor - Vector3.up * 0.34f * WorldScale;
            Vector3[] directions = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
            float maxDistance = (warm ? 7.2f : 6.4f) * WorldScale;
            float wallWidth = (warm ? 4.0f : 3.5f) * WorldScale;
            float wallHeight = (warm ? 1.9f : 1.65f) * WorldScale;
            for (int i = 0; i < directions.Length; i++)
            {
                if (TryFindSceneSurface(wallOrigin, directions[i], maxDistance, SurfaceTarget.Wall, out RaycastHit wallHit))
                {
                    CreateSurfaceReflectionQuad(
                        parent,
                        $"{name} wall reflection {suffix}",
                        wallHit.point + wallHit.normal * 0.024f,
                        wallHit.normal,
                        Vector3.up,
                        wallMaterial,
                        wallWidth,
                        wallHeight);
                }
            }
        }

        private void BuildFloorOpeningLighting(Transform parent)
        {
            if (!buildLights || definition.floorOpenings == null || definition.floorOpenings.Count == 0)
            {
                return;
            }

            GameObject group = new GameObject("Atrium Lighting");
            group.transform.SetParent(parent, false);

            int index = 1;
            foreach (FloorOpeningSpec opening in definition.floorOpenings)
            {
                if (opening == null)
                {
                    continue;
                }

                Rect rect = opening.opening;
                Vector3 center = new Vector3(rect.center.x, 0f, rect.center.y);
                float width = Mathf.Max(Cell, rect.width * 0.34f);
                float depth = Mathf.Max(Cell, rect.height * 0.34f);
                float radius = Mathf.Max(rect.width, rect.height) * 0.58f;
                Color warm = Color.Lerp(WolfLevelContent.WarmLampColor, Color.white, 0.46f);
                Color cool = Color.Lerp(WolfLevelContent.CoolLampColor, Color.white, 0.58f);

                CreateBoundedFloorReflectionQuad(
                    group.transform,
                    $"atrium floor reflection {index:00}",
                    new Vector3(center.x, opening.bottomY + 0.028f, center.z),
                    Vector3.up,
                    materialLibrary.LampFloorReflectionWarmMaterial,
                    width,
                    depth);

                Light lower = CreatePointLight(
                    group.transform,
                    $"atrium lower fill {index:00}",
                    new Vector3(center.x, opening.bottomY + 1.25f * WorldScale, center.z),
                    warm,
                    0.34f,
                    radius,
                    LightShadows.Soft);
                lower.shadowStrength = 0.24f;
                lower.shadowResolution = UnityEngine.Rendering.LightShadowResolution.Medium;

                CreatePointLight(
                    group.transform,
                    $"atrium vertical glow {index:00}",
                    new Vector3(center.x, Mathf.Lerp(opening.bottomY, opening.topY, 0.52f), center.z),
                    Color.Lerp(warm, cool, 0.35f),
                    0.12f,
                    radius * 0.82f,
                    LightShadows.None);

                Light upper = CreatePointLight(
                    group.transform,
                    $"atrium upper fill {index:00}",
                    new Vector3(center.x, opening.topY + 1.35f * WorldScale, center.z),
                    cool,
                    0.18f,
                    radius * 0.68f,
                    LightShadows.Soft);
                upper.shadowStrength = 0.16f;
                upper.shadowResolution = UnityEngine.Rendering.LightShadowResolution.Medium;
                index++;
            }
        }

        private void CreateBoundedFloorReflectionQuad(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 normal,
            Material material,
            float width,
            float height)
        {
            Vector3 safeNormal = normal.sqrMagnitude > 0.001f ? normal.normalized : Vector3.up;
            Vector3 xAxis = Vector3.ProjectOnPlane(Vector3.right, safeNormal);
            if (xAxis.sqrMagnitude < 0.001f)
            {
                xAxis = Vector3.ProjectOnPlane(Vector3.forward, safeNormal);
            }

            xAxis.Normalize();
            Vector3 zAxis = Vector3.Cross(safeNormal, xAxis).normalized;
            float positiveX = FindReflectionBoundaryExtent(position, safeNormal, xAxis, width * 0.5f);
            float negativeX = FindReflectionBoundaryExtent(position, safeNormal, -xAxis, width * 0.5f);
            float positiveZ = FindReflectionBoundaryExtent(position, safeNormal, zAxis, height * 0.5f);
            float negativeZ = FindReflectionBoundaryExtent(position, safeNormal, -zAxis, height * 0.5f);
            float isolatedWidth = positiveX + negativeX;
            float isolatedHeight = positiveZ + negativeZ;
            if (isolatedWidth < MinimumReflectionSize || isolatedHeight < MinimumReflectionSize)
            {
                return;
            }

            Vector3 isolatedCenter = position +
                xAxis * ((positiveX - negativeX) * 0.5f) +
                zAxis * ((positiveZ - negativeZ) * 0.5f);
            CreateSurfaceReflectionQuad(parent, name, isolatedCenter, safeNormal, zAxis, material, isolatedWidth, isolatedHeight);
        }

        private float FindReflectionBoundaryExtent(Vector3 center, Vector3 normal, Vector3 direction, float requestedExtent)
        {
            Vector3 origin = center + normal * ReflectionBoundaryProbeHeight;
            float probeDistance = requestedExtent + ReflectionBoundaryInset + 0.08f * WorldScale;
            if (!TryFindSceneSurface(origin, direction.normalized, probeDistance, SurfaceTarget.Wall, out RaycastHit hit))
            {
                return requestedExtent;
            }

            return Mathf.Clamp(hit.distance - ReflectionBoundaryInset, MinimumReflectionSize * 0.5f, requestedExtent);
        }

        private bool TryFindSceneSurface(Vector3 origin, Vector3 direction, float maxDistance, SurfaceTarget target, out RaycastHit bestHit)
        {
            RaycastHit[] hits = Physics.RaycastAll(origin, direction, maxDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            bestHit = default;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                if (hit.collider == null || hit.distance >= bestDistance || ShouldSkipReflectionSurface(hit.transform))
                {
                    continue;
                }

                float upDot = Vector3.Dot(hit.normal.normalized, Vector3.up);
                if (target == SurfaceTarget.Floor && upDot < 0.55f)
                {
                    continue;
                }

                if (target == SurfaceTarget.Wall && Mathf.Abs(upDot) > 0.35f)
                {
                    continue;
                }

                bestHit = hit;
                bestDistance = hit.distance;
            }

            return bestDistance < float.PositiveInfinity;
        }

        private static bool ShouldSkipReflectionSurface(Transform transform)
        {
            if (transform == null)
            {
                return true;
            }

            string objectName = transform.gameObject.name;
            return transform.GetComponentInParent<WolfDoor>() != null ||
                objectName.StartsWith("Door ", System.StringComparison.Ordinal) ||
                objectName.Contains(" Door ", System.StringComparison.Ordinal) ||
                objectName.StartsWith("ceilLight", System.StringComparison.Ordinal) ||
                objectName.StartsWith("chandelier", System.StringComparison.Ordinal) ||
                objectName.Contains("reflection", System.StringComparison.OrdinalIgnoreCase) ||
                objectName.Contains("spill", System.StringComparison.OrdinalIgnoreCase);
        }

        private void CreateSurfaceReflectionQuad(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 normal,
            Vector3 localUpAxis,
            Material material,
            float width,
            float height)
        {
            if (material == null)
            {
                return;
            }

            GameObject reflection = GameObject.CreatePrimitive(PrimitiveType.Quad);
            reflection.name = name;
            reflection.transform.SetParent(parent, true);
            reflection.transform.position = position;
            Vector3 safeNormal = normal.sqrMagnitude > 0.001f ? normal.normalized : Vector3.forward;
            Vector3 safeUp = Vector3.ProjectOnPlane(localUpAxis, safeNormal);
            if (safeUp.sqrMagnitude < 0.001f)
            {
                safeUp = Vector3.ProjectOnPlane(Vector3.up, safeNormal);
            }
            if (safeUp.sqrMagnitude < 0.001f)
            {
                safeUp = Vector3.right;
            }

            reflection.transform.rotation = Quaternion.LookRotation(safeNormal, safeUp.normalized);
            reflection.transform.localScale = new Vector3(width, height, 1f);
            Renderer renderer = reflection.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.receiveShadows = false;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            DestroySafely(reflection.GetComponent<Collider>());
        }

        private void AddLampLightRig(Transform parent, string name, Vector3 basePosition, float ceilingY, bool warm)
        {
            // A coincident unshadowed component approximates first-bounce light
            // from the visible bulb without introducing a sourceless fill.
            float primaryIntensity = warm ? 2.15f : 1.90f;
            float bounceIntensity = warm ? 0.32f : 0.29f;
            Color color = warm
                ? WolfLevelContent.WarmLampColor
                : Color.Lerp(WolfLevelContent.CoolLampColor, WolfLevelContent.WarmLampColor, 0.18f);
            Vector3 anchor = new Vector3(basePosition.x, ceilingY - 0.16f * WorldScale, basePosition.z);
            Vector3 lightPosition = anchor - Vector3.up * 0.06f * WorldScale;
            Color lightColor = Color.Lerp(color, Color.white, 0.08f);
            float lightRange = (warm ? 10.0f : 9.2f) * WorldScale;
            string suffix = $"{basePosition.x:0},{basePosition.z:0}";

            Light primary = CreatePointLight(
                parent,
                $"{name} light {suffix}",
                lightPosition,
                lightColor,
                primaryIntensity,
                lightRange,
                LightShadows.Soft);
            primary.shadowStrength = PrimaryShadowStrength;
            primary.shadowBias = 0.035f;
            primary.shadowNormalBias = 0.24f;
            primary.shadowNearPlane = 0.12f;
            primary.shadowResolution = UnityEngine.Rendering.LightShadowResolution.High;

            CreatePointLight(
                parent,
                $"{name} ceiling bounce {suffix}",
                lightPosition,
                lightColor,
                bounceIntensity,
                lightRange,
                LightShadows.None);
        }

        /// <summary>
        /// Hangs a two-tier candle chandelier over every void wide enough to
        /// read as an atrium. The void drops all storey ceiling lamps, so the
        /// chandelier is the single light source of the open space.
        /// </summary>
        private void BuildChandeliers(Transform parent)
        {
            if (definition.floorOpenings == null)
            {
                return;
            }

            foreach (FloorOpeningSpec opening in definition.floorOpenings)
            {
                if (opening == null || Mathf.Min(opening.opening.width, opening.opening.height) < 4f * WorldScale)
                {
                    continue;
                }

                AddAtriumChandelier(parent, opening);
            }
        }

        private void AddAtriumChandelier(Transform parent, FloorOpeningSpec opening)
        {
            Vector2 center = opening.opening.center;
            float ceilingY = CeilingYAt(new Vector3(center.x, opening.topY + 0.1f, center.y));

            GameObject root = new GameObject($"Atrium chandelier {center.x:0},{center.y:0}");
            root.transform.SetParent(parent, true);
            root.transform.position = new Vector3(center.x, ceilingY, center.y);
            Transform anchor = root.transform;

            Material metal = materialLibrary.RailMaterial;
            Material darkMetal = materialLibrary.LampCapMaterial != null ? materialLibrary.LampCapMaterial : metal;
            Material glow = materialLibrary.LampGlowMaterial != null ? materialLibrary.LampGlowMaterial : materialLibrary.LampWarmBulb;

            // The big ring targets the top gallery's eye band; the ring radius
            // shrinks for narrower voids so candles never overhang the slabs.
            float upperRadius = Mathf.Min(1.5f * WorldScale, 0.16f * Mathf.Min(opening.opening.width, opening.opening.height));
            float lowerRadius = upperRadius * 0.62f;
            float hubY = ceilingY - 0.75f * WorldScale;
            float upperRingY = hubY - 0.52f * WorldScale;
            float lowerRingY = upperRingY - 0.58f * WorldScale;
            float bowlY = lowerRingY - 0.22f * WorldScale;
            Vector3 centerAt(float y) => new Vector3(center.x, y, center.y);

            CreateChandelierPart(PrimitiveType.Cylinder, "Chandelier mount", anchor, centerAt(ceilingY - 0.045f), new Vector3(0.76f, 0.045f, 0.76f), darkMetal, true);
            CreateChandelierRod(anchor, "Chandelier stem", centerAt(ceilingY), centerAt(hubY), 0.05f, metal);
            CreateChandelierPart(PrimitiveType.Cylinder, "Chandelier hub", anchor, centerAt(hubY), new Vector3(0.34f, 0.17f, 0.34f), darkMetal, true);
            // The bowl dish hangs straight under the point light: it stays a
            // non-caster so the light keeps its pool on the atrium floor.
            CreateChandelierRod(anchor, "Chandelier drop rod", centerAt(hubY), centerAt(bowlY), 0.035f, metal);
            CreateChandelierPart(PrimitiveType.Cylinder, "Chandelier bowl dish", anchor, centerAt(bowlY), new Vector3(1.0f, 0.10f, 1.0f), darkMetal, false);
            CreateChandelierPart(PrimitiveType.Sphere, "Chandelier bowl lens", anchor, centerAt(bowlY - 0.10f), new Vector3(0.86f, 0.30f, 0.86f), glow, false);

            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * 2f / 8f;
                var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                CreateChandelierRod(
                    anchor,
                    $"Chandelier suspension {i:00}",
                    centerAt(hubY - 0.05f) + direction * 0.28f,
                    centerAt(upperRingY + 0.08f) + direction * (upperRadius - 0.06f),
                    0.03f,
                    metal);
                CreateChandelierRod(
                    anchor,
                    $"Chandelier band strut {i:00}",
                    centerAt(upperRingY - 0.08f) + direction * upperRadius,
                    centerAt(upperRingY - 0.34f) + direction * upperRadius,
                    0.025f,
                    metal);
            }

            for (int i = 0; i < 6; i++)
            {
                float angle = (i + 0.5f) * Mathf.PI * 2f / 6f;
                var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                CreateChandelierRod(
                    anchor,
                    $"Chandelier tier rod {i:00}",
                    centerAt(upperRingY - 0.30f) + direction * (upperRadius * 0.86f),
                    centerAt(lowerRingY + 0.06f) + direction * (lowerRadius * 0.98f),
                    0.028f,
                    metal);
            }

            CreateChandelierRing(anchor, "Chandelier upper band", centerAt(upperRingY), upperRadius, 0.16f, 0.20f, 28, darkMetal);
            CreateChandelierRing(anchor, "Chandelier upper trim band", centerAt(upperRingY - 0.34f), upperRadius, 0.08f, 0.12f, 28, metal);
            CreateChandelierRing(anchor, "Chandelier lower band", centerAt(lowerRingY), lowerRadius, 0.13f, 0.16f, 22, darkMetal);
            CreateChandelierCandles(anchor, "Chandelier upper", centerAt(upperRingY + 0.08f), upperRadius, 16, 0.46f, darkMetal, glow);
            CreateChandelierCandles(anchor, "Chandelier lower", centerAt(lowerRingY + 0.065f), lowerRadius, 10, 0.40f, darkMetal, glow);

            if (!buildLights)
            {
                return;
            }

            Vector3 lightPosition = centerAt((upperRingY + lowerRingY) * 0.5f);
            Color lightColor = Color.Lerp(WolfLevelContent.WarmLampColor, Color.white, 0.08f);
            float lightRange = 10.5f * WorldScale;

            Light primary = CreatePointLight(
                anchor,
                $"Atrium chandelier light {center.x:0},{center.y:0}",
                lightPosition,
                lightColor,
                1.85f,
                lightRange,
                LightShadows.Soft);
            primary.shadowStrength = PrimaryShadowStrength;
            primary.shadowBias = 0.035f;
            primary.shadowNormalBias = 0.24f;
            primary.shadowNearPlane = 0.12f;
            primary.shadowResolution = UnityEngine.Rendering.LightShadowResolution.High;

            CreatePointLight(
                anchor,
                $"Atrium chandelier ceiling bounce {center.x:0},{center.y:0}",
                lightPosition,
                lightColor,
                0.36f,
                lightRange,
                LightShadows.None);
        }

        private void CreateChandelierRing(Transform anchor, string name, Vector3 center, float radius, float height, float thickness, int segments, Material material)
        {
            float segmentLength = 2f * Mathf.PI * radius / segments + thickness * 0.4f;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                Vector3 position = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                var tangent = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                GameObject segment = CreateChandelierPart(PrimitiveType.Cube, $"{name} {i:00}", anchor, position, new Vector3(thickness, height, segmentLength), material, true);
                segment.transform.rotation = Quaternion.LookRotation(tangent, Vector3.up);
            }
        }

        private void CreateChandelierCandles(Transform anchor, string name, Vector3 ringTop, float radius, int count, float tubeHeight, Material cup, Material glow)
        {
            for (int i = 0; i < count; i++)
            {
                // Half-step offset keeps candles clear of the suspension rods.
                float angle = (i + 0.5f) * Mathf.PI * 2f / count;
                Vector3 basePosition = ringTop + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                CreateChandelierPart(PrimitiveType.Cylinder, $"{name} cup {i:00}", anchor, basePosition + Vector3.up * 0.05f, new Vector3(0.13f, 0.05f, 0.13f), cup, true);
                CreateChandelierPart(PrimitiveType.Cylinder, $"{name} candle {i:00}", anchor, basePosition + Vector3.up * (0.10f + tubeHeight * 0.5f), new Vector3(0.095f, tubeHeight * 0.5f, 0.095f), glow, false);
            }
        }

        private void CreateChandelierRod(Transform anchor, string name, Vector3 from, Vector3 to, float radius, Material material)
        {
            Vector3 delta = to - from;
            GameObject rod = CreateChandelierPart(PrimitiveType.Cylinder, name, anchor, (from + to) * 0.5f, new Vector3(radius * 2f, delta.magnitude * 0.5f, radius * 2f), material, true);
            rod.transform.rotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
        }

        private GameObject CreateChandelierPart(PrimitiveType type, string name, Transform anchor, Vector3 position, Vector3 scale, Material material, bool castShadows)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(anchor, true);
            part.transform.position = position;
            part.transform.localScale = scale;
            Renderer renderer = part.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.BlendProbesAndSkybox;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.BlendProbes;
            if (!castShadows)
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            DestroySafely(part.GetComponent<Collider>());
            return part;
        }

        private Light CreatePointLight(Transform parent, string name, Vector3 position, Color color, float intensity, float range, LightShadows shadows)
        {
            GameObject lightObject = new GameObject(name);
            lightObject.transform.SetParent(parent, true);
            lightObject.transform.position = position;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            ConfigureGeneratedLight(light, color, intensity, range, shadows);
            return light;
        }

        private static void ConfigureGeneratedLight(Light light, Color color, float intensity, float range, LightShadows shadows)
        {
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.bounceIntensity = 0f;
            light.shadows = shadows;
            light.cullingMask = LampLightingMask;
            light.renderMode = LightRenderMode.ForcePixel;
            light.lightmapBakeType = LightmapBakeType.Realtime;
            if (shadows != LightShadows.None)
            {
                light.shadowBias = 0.035f;
                light.shadowNormalBias = 0.24f;
                light.shadowNearPlane = 0.12f;
                light.shadowResolution = UnityEngine.Rendering.LightShadowResolution.High;
            }
        }

        private void ApplyLightingQuality()
        {
            if (!buildLights || !Application.isPlaying)
            {
                return;
            }

            QualitySettings.pixelLightCount = Mathf.Max(QualitySettings.pixelLightCount, maxRealtimeLights * 8);
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
            QualitySettings.shadowProjection = ShadowProjection.StableFit;
            QualitySettings.shadowDistance = Mathf.Max(QualitySettings.shadowDistance, 42f * WorldScale);
            QualitySettings.realtimeReflectionProbes = true;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            RenderSettings.reflectionIntensity = Mathf.Max(RenderSettings.reflectionIntensity, 0.86f);
            RenderSettings.defaultReflectionResolution = Mathf.Max(RenderSettings.defaultReflectionResolution, AtriumReflectionProbeResolution);
        }

        /// <summary>
        /// One box-projected probe per sector room plus one tall probe per
        /// atrium volume. Rendered once after the geometry and lamp rigs exist;
        /// the glossy floor, railings and doors then reflect the actual
        /// surroundings instead of only the flat ambient.
        /// </summary>
        private void BuildReflectionProbes(Transform parent)
        {
            if (!buildReflectionProbes)
            {
                return;
            }

            GameObject group = new GameObject("Reflection Probes");
            group.transform.SetParent(parent, false);

            int index = 1;
            foreach (SectorSpec sector in definition.sectors)
            {
                // One probe per sector, anchored at its largest floor area.
                if (!TryGetSectorProbeArea(sector, out Rect best))
                {
                    continue;
                }

                Vector3 center = new Vector3(best.center.x, sector.floorY + sector.ceilingHeight * 0.5f, best.center.y);
                Vector3 size = new Vector3(
                    best.width + ReflectionProbePadding,
                    sector.ceilingHeight + ReflectionProbePadding,
                    best.height + ReflectionProbePadding);
                CreateReflectionProbe(group.transform, $"sector reflection probe {index:00}", center, size, 1, SectorReflectionProbeResolution);
                index++;
            }

            if (definition.floorOpenings == null)
            {
                return;
            }

            int atriumIndex = 1;
            foreach (FloorOpeningSpec opening in definition.floorOpenings)
            {
                if (opening == null)
                {
                    continue;
                }

                Rect rect = opening.opening;
                float topCeiling = CeilingYAt(new Vector3(rect.center.x, opening.topY + 0.1f, rect.center.y));
                // The probe hugs the open shaft: reaching over the galleries
                // would replace their own room reflections with the shaft's
                // cubemap, drawing a bright seam across the gallery floors.
                // Half a cell still catches the railings along the edge.
                float galleryMargin = 0.5f * Cell;
                Vector3 center = new Vector3(rect.center.x, (opening.bottomY + topCeiling) * 0.5f, rect.center.y);
                Vector3 size = new Vector3(
                    rect.width + galleryMargin * 2f,
                    topCeiling - opening.bottomY + ReflectionProbePadding,
                    rect.height + galleryMargin * 2f);
                CreateReflectionProbe(group.transform, $"atrium reflection probe {atriumIndex:00}", center, size, 2, AtriumReflectionProbeResolution);
                atriumIndex++;
            }
        }

        private static bool TryGetSectorProbeArea(SectorSpec sector, out Rect area)
        {
            area = default;
            float bestArea = 0f;
            foreach (Rect candidate in sector.floorAreas)
            {
                float squareMeters = candidate.width * candidate.height;
                if (squareMeters > bestArea)
                {
                    bestArea = squareMeters;
                    area = candidate;
                }
            }

            return bestArea > 0f && (area.width >= MinimumReflectionProbeSide || area.height >= MinimumReflectionProbeSide);
        }

        private void CreateReflectionProbe(Transform parent, string name, Vector3 center, Vector3 size, int importance, int resolution)
        {
            GameObject probeObject = new GameObject(name);
            probeObject.transform.SetParent(parent, true);
            probeObject.transform.position = center;

            ReflectionProbe probe = probeObject.AddComponent<ReflectionProbe>();
            probe.boxProjection = true;
            probe.center = Vector3.zero;
            probe.size = size;
            probe.resolution = resolution;
            probe.hdr = true;
            // Above 1 the boosted reflections read as a wet floor and blow out
            // under the lamps; 1 keeps them at the captured brightness.
            probe.intensity = 1f;
            probe.importance = importance;
            probe.blendDistance = ReflectionProbeBlendDistance;
            probe.shadowDistance = 26f * WorldScale;
            probe.nearClipPlane = 0.05f;
            probe.farClipPlane = 180f * WorldScale;
            probe.cullingMask = ~0;

            if (Application.isPlaying)
            {
                // The player loop renders the queued probe on the next frames,
                // with proper specular convolution for glossy surfaces.
                probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
                probe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.ViaScripting;
                probe.timeSlicingMode = UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.NoTimeSlicing;
                probe.RenderProbe();
            }
            else
            {
                // Edit-mode rebuilds (capture tools) have no player loop, so a
                // queued realtime probe would stay black; render synchronously.
                probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Custom;
                probe.customBakedTexture = RenderProbeCubemap(name, center, resolution);
            }
        }

        // Camera orientation per cubemap face; the rendered face image is
        // rotated 180 degrees (Array.Reverse) to match cubemap storage.
        private static readonly (CubemapFace face, Vector3 euler)[] CubemapFaceRotations =
        {
            (CubemapFace.PositiveX, new Vector3(0f, 90f, 0f)),
            (CubemapFace.NegativeX, new Vector3(0f, 270f, 0f)),
            (CubemapFace.PositiveY, new Vector3(270f, 0f, 0f)),
            (CubemapFace.NegativeY, new Vector3(90f, 0f, 0f)),
            (CubemapFace.PositiveZ, Vector3.zero),
            (CubemapFace.NegativeZ, new Vector3(0f, 180f, 0f))
        };

        /// <summary>
        /// Renders the six probe faces with plain offscreen camera renders.
        /// Camera.RenderToCubemap corrupts every later offscreen render of the
        /// editor session on Metal, so the faces are read back one by one.
        /// </summary>
        private Texture RenderProbeCubemap(string name, Vector3 center, int size)
        {
            var cubemap = new Cubemap(size, TextureFormat.RGBA32, true)
            {
                name = $"{name} cubemap",
                hideFlags = HideFlags.DontSave
            };

            GameObject rigObject = new GameObject($"{name} render rig");
            var renderTexture = new RenderTexture(size, size, 24);
            var faceTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            try
            {
                rigObject.transform.position = center;
                Camera camera = rigObject.AddComponent<Camera>();
                camera.enabled = false;
                camera.fieldOfView = 90f;
                camera.aspect = 1f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color32(56, 56, 56, 255);
                camera.nearClipPlane = 0.05f;
                camera.farClipPlane = 160f;
                camera.targetTexture = renderTexture;

                foreach ((CubemapFace face, Vector3 euler) in CubemapFaceRotations)
                {
                    rigObject.transform.rotation = Quaternion.Euler(euler);
                    camera.Render();

                    RenderTexture.active = renderTexture;
                    faceTexture.ReadPixels(new Rect(0, 0, size, size), 0, 0);
                    Color[] pixels = faceTexture.GetPixels();
                    System.Array.Reverse(pixels);
                    cubemap.SetPixels(pixels, face);
                }

                cubemap.Apply(true);
            }
            finally
            {
                RenderTexture.active = null;
                DestroySafely(rigObject);
                DestroySafely(faceTexture);
                renderTexture.Release();
                DestroySafely(renderTexture);
            }

            return cubemap;
        }

        private void CreateBillboard(Transform parent, string name, Vector3 position, Vector2 scale, Material material, bool blocking, float floorY)
        {
            GameObject billboard = GameObject.CreatePrimitive(PrimitiveType.Quad);
            billboard.name = name;
            billboard.transform.position = position;
            billboard.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            billboard.transform.SetParent(parent, true);
            billboard.GetComponent<Renderer>().sharedMaterial = material;
            billboard.AddComponent<WolfBillboard>();
            DestroySafely(billboard.GetComponent<Collider>());

            if (blocking)
            {
                GameObject colliderObject = new GameObject($"{name} Collider");
                colliderObject.transform.position = new Vector3(position.x, floorY + 0.6f, position.z);
                colliderObject.transform.SetParent(parent, true);
                CapsuleCollider capsule = colliderObject.AddComponent<CapsuleCollider>();
                capsule.radius = 0.30f;
                capsule.height = 1.2f;
            }
        }

        private static void DestroySafely(Object value)
        {
            if (value == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(value);
            }
            else
            {
                DestroyImmediate(value);
            }
        }

        private enum SurfaceTarget
        {
            Floor,
            Wall
        }
    }
}
