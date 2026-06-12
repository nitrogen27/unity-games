using System.Collections.Generic;
using HelloWorldRoom;
using WolfMini.Core;
using WolfMini.Rendering;
using UnityEngine;

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
        // Covers both storeys: all lamps are shadowless point lights.
        [SerializeField] private int maxRealtimeLights = 96;

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

            Transform root = ResetGeneratedRoot();
            BuildLevelGeometry(root);
            BuildDoorways(root);
            BuildProps(root);
            BuildEnemies(root);
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
                    // Stairwell openings punch through every horizontal plane
                    // they sit on: the upper sector's floor and, when a lower
                    // sector reaches up to the same plane, its ceiling.
                    foreach (Rect piece in SubtractStairwellOpenings(area, sector.floorY))
                    {
                        AddFloorQuad(buffer, floorSubmesh, piece, sector.floorY, WolfMiniConstants.FloorTextureModule);
                    }

                    foreach (Rect piece in SubtractStairwellOpenings(area, ceilingY))
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
            geometry.AddComponent<MeshRenderer>().sharedMaterials = materials.ToArray();
            geometry.AddComponent<MeshCollider>().sharedMesh = mesh;
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

        /// <summary>Pieces of a horizontal rectangle at <paramref name="planeY"/> left after cutting out the stairwell openings on that plane.</summary>
        private List<Rect> SubtractStairwellOpenings(Rect area, float planeY)
        {
            var pieces = new List<Rect> { area };
            foreach (StairwellSpec stairwell in definition.stairwells)
            {
                if (stairwell == null || Mathf.Abs(stairwell.topY - planeY) > 0.001f)
                {
                    continue;
                }

                pieces = SubtractRect(pieces, stairwell.opening);
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
        /// <see cref="StairwellSpec.treadUvScale"/>. The slope is sealed: first
        /// riser starts at the upper floor edge, the last tread lands on the
        /// lower floor.
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

            for (int step = 0; step < steps; step++)
            {
                float topEdge = stairwell.alongZ
                    ? (sign > 0 ? opening.yMin : opening.yMax)
                    : (sign > 0 ? opening.xMin : opening.xMax);
                float near = topEdge + sign * step * tread;
                float far = near + sign * tread;
                float treadY = stairwell.topY - (step + 1) * riser;

                Rect treadRect = stairwell.alongZ
                    ? Rect.MinMaxRect(opening.xMin, Mathf.Min(near, far), opening.xMax, Mathf.Max(near, far))
                    : Rect.MinMaxRect(Mathf.Min(near, far), opening.yMin, Mathf.Max(near, far), opening.yMax);
                AddFloorQuad(buffer, floorSubmesh, treadRect, treadY, module);

                // Riser at the step's high edge, facing down the descent.
                Vector2 origin;
                Vector2 direction;
                Vector3 normal;
                if (stairwell.alongZ)
                {
                    normal = new Vector3(0f, 0f, sign);
                    direction = new Vector2(-sign, 0f);
                    origin = new Vector2(sign > 0 ? opening.xMax : opening.xMin, near);
                }
                else
                {
                    normal = new Vector3(sign, 0f, 0f);
                    direction = new Vector2(0f, sign);
                    origin = new Vector2(near, sign > 0 ? opening.yMin : opening.yMax);
                }

                float width = stairwell.alongZ ? opening.width : opening.height;
                float riserTop = stairwell.topY - step * riser;
                Rect riserUV = new Rect(Vector2.Dot(origin, direction) / module, treadY / module, width / module, riser / module);
                AddWallQuad(buffer, floorSubmesh, origin, direction, normal, 0f, width, treadY, riserTop, riserUV);
            }
        }

        /// <summary>
        /// Walls of the stair shaft. The side planes carry faces both ways:
        /// inward for the open shaft seen from above, outward so the enclosure
        /// reads as a solid block when the storey below runs past it. The high
        /// end gets an outward head wall; the low end stays open as the mouth.
        /// </summary>
        private static IEnumerable<WallSegmentSpec> CreateShaftWalls(StairwellSpec stairwell)
        {
            Rect opening = stairwell.opening;
            int sign = stairwell.descendSign >= 0 ? 1 : -1;
            var c00 = new Vector2(opening.xMin, opening.yMin);
            var c01 = new Vector2(opening.xMin, opening.yMax);
            var c10 = new Vector2(opening.xMax, opening.yMin);
            var c11 = new Vector2(opening.xMax, opening.yMax);

            if (stairwell.alongZ)
            {
                yield return MakeShaftWall(stairwell, c00, c01);
                yield return MakeShaftWall(stairwell, c01, c00);
                yield return MakeShaftWall(stairwell, c11, c10);
                yield return MakeShaftWall(stairwell, c10, c11);
                yield return sign > 0 ? MakeShaftWall(stairwell, c00, c10) : MakeShaftWall(stairwell, c11, c01);
            }
            else
            {
                yield return MakeShaftWall(stairwell, c10, c00);
                yield return MakeShaftWall(stairwell, c00, c10);
                yield return MakeShaftWall(stairwell, c01, c11);
                yield return MakeShaftWall(stairwell, c11, c01);
                yield return sign > 0 ? MakeShaftWall(stairwell, c01, c00) : MakeShaftWall(stairwell, c10, c11);
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
        /// Continuous module UVs anchored to world coordinates: U is the world
        /// distance along the segment axis, V is the world height, both divided
        /// by the texture module. Collinear runs and stacked bands stay
        /// phase-aligned, so no seam can appear inside or between segments.
        /// </summary>
        private static Rect ModuleUV(Vector2 start, Vector2 direction, float length, float baseY, float height)
        {
            float u0 = Vector2.Dot(start, direction) / Module;
            float v0 = baseY / Module;
            return new Rect(u0, v0, length / Module, height / Module);
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
                door.AddComponent<MeshRenderer>().sharedMaterials = new[]
                {
                    faceMaterial != null ? faceMaterial : materialLibrary.WallAtlasMaterial,
                    jambMaterial != null ? jambMaterial : materialLibrary.WallAtlasMaterial
                };
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
            cap.GetComponent<Renderer>().sharedMaterial = capMaterial;
            DestroySafely(cap.GetComponent<Collider>());

            GameObject bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulb.name = $"{name} bulb {basePosition.x:0},{basePosition.z:0}";
            bulb.transform.position = new Vector3(basePosition.x, ceilingY - 0.16f * WorldScale, basePosition.z);
            bulb.transform.localScale = Vector3.one * 0.20f * WorldScale;
            bulb.transform.SetParent(parent, true);
            Material bulbMaterial = materialLibrary.LampGlowMaterial != null
                ? materialLibrary.LampGlowMaterial
                : warm ? materialLibrary.LampWarmBulb : materialLibrary.LampGreenBulb;
            bulb.GetComponent<Renderer>().sharedMaterial = bulbMaterial;
            DestroySafely(bulb.GetComponent<Collider>());

            AddCeilingSpill(parent, name, basePosition, ceilingY, warm);

            if (!withLight)
            {
                return;
            }

            GameObject lightObject = new GameObject($"{name} light {basePosition.x:0},{basePosition.z:0}");
            lightObject.transform.position = new Vector3(basePosition.x, ceilingY - 0.22f * WorldScale, basePosition.z);
            lightObject.transform.SetParent(parent, true);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = warm ? WolfLevelContent.WarmLampColor : WolfLevelContent.CoolLampColor;
            light.intensity = warm ? 1.4f : 1.15f;
            light.range = (warm ? 16f : 13f) * WorldScale;
            light.bounceIntensity = 0.15f;
            light.shadows = LightShadows.None;
        }

        /// <summary>Soft light pool on the ceiling around the lamp, as in the target-look scene.</summary>
        private void AddCeilingSpill(Transform parent, string name, Vector3 basePosition, float ceilingY, bool warm)
        {
            Material spillMaterial = warm ? materialLibrary.CeilingSpillWarmMaterial : materialLibrary.CeilingSpillCoolMaterial;
            if (spillMaterial == null)
            {
                return;
            }

            GameObject spill = GameObject.CreatePrimitive(PrimitiveType.Quad);
            spill.name = $"{name} ceiling spill {basePosition.x:0},{basePosition.z:0}";
            spill.transform.SetParent(parent, true);
            spill.transform.position = new Vector3(basePosition.x, ceilingY - 0.02f, basePosition.z);
            spill.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            float diameter = (warm ? 4.8f : 3.8f) * WorldScale;
            spill.transform.localScale = new Vector3(diameter, diameter, 1f);

            Renderer renderer = spill.GetComponent<Renderer>();
            renderer.sharedMaterial = spillMaterial;
            renderer.receiveShadows = false;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            DestroySafely(spill.GetComponent<Collider>());
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
    }
}
