using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using WolfMini.Core;
using WolfMini.Level;

namespace WolfMini.EditorTools
{
    /// <summary>
    /// Hand-authored world-space layout for the Full3D scene — designed directly
    /// in meters, not converted from the Wolf grid: rooms and corridors with
    /// arbitrary dimensions, varied passage widths, wall niches, open archways,
    /// chamfered (diagonal) corners and free-standing pillars. Walls are derived
    /// automatically from the floor plan: a rectangle side is walled wherever no
    /// other plan rectangle adjoins it from outside, so openings appear exactly
    /// where spaces touch.
    /// </summary>
    public static class WolfHandmadeLevelAuthor
    {
        public const string AssetPath = "Assets/WolfMini/Data/WolfHandmadeLevel.asset";

        private const float Eps = 0.001f;
        private const float MinWallLength = 0.01f;
        private const float Ceiling = WolfMiniConstants.WallHeight;

        // Wall styles (legacy wall values understood by the material library).
        private const int Blue = 8;
        private const int White = 1;
        private const int Prison = 5;

        private readonly struct PlanRect
        {
            public readonly Rect rect;
            public readonly int style;

            public PlanRect(float x, float z, float width, float depth, int style)
            {
                rect = new Rect(x, z, width, depth);
                this.style = style;
            }
        }

        [MenuItem("Tools/Wolf Full3D/Author Handmade Sector Level")]
        public static void AuthorLevel()
        {
            WolfSectorLevelDefinition definition = AssetDatabase.LoadAssetAtPath<WolfSectorLevelDefinition>(AssetPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<WolfSectorLevelDefinition>();
                AssetDatabase.CreateAsset(definition, AssetPath);
            }

            Populate(definition);

            EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssets();

            if (!definition.TryValidate(out List<string> errors))
            {
                foreach (string error in errors)
                {
                    Debug.LogError($"[WolfHandmade] {definition.name}: {error}", definition);
                }

                return;
            }

            Debug.Log($"[WolfHandmade] Authored '{definition.levelName}' into {AssetPath}: " +
                      $"{definition.sectors.Count} sector(s), {definition.walls.Count} wall segment(s), " +
                      $"{definition.doorways.Count} doorway(s), {definition.props.Count} prop(s), {definition.enemies.Count} enemy(ies).");
        }

        /// <summary>Builds the handmade layout into the definition. Testable without asset IO.</summary>
        public static void Populate(WolfSectorLevelDefinition definition)
        {
            definition.levelName = "Handmade Bunker";
            definition.sectors.Clear();
            definition.walls.Clear();
            definition.doorways.Clear();
            definition.props.Clear();
            definition.enemies.Clear();

            // Floor plan in meters. Nothing here is a multiple of the old grid:
            // room sizes like 9.7x6.3, passages 1.6/1.7/2.2/2.5 wide, niches
            // 0.7-0.75 deep.
            var plan = new List<PlanRect>
            {
                new PlanRect(0.00f, 0.00f, 9.70f, 6.30f, Blue),     // spawn hall
                new PlanRect(4.05f, -0.75f, 1.50f, 0.75f, White),   // south wall niche
                new PlanRect(4.15f, 6.30f, 1.70f, 0.50f, White),    // door 1 reveal
                new PlanRect(4.15f, 6.80f, 1.70f, 5.20f, White),    // north corridor (1.7 wide)
                new PlanRect(1.90f, 12.00f, 10.60f, 6.50f, Blue),   // guard hall
                new PlanRect(12.50f, 13.90f, 3.40f, 2.50f, White),  // east corridor (2.5 wide archway)
                new PlanRect(15.90f, 14.05f, 0.50f, 2.20f, White),  // door 2 reveal
                new PlanRect(16.40f, 11.90f, 5.20f, 6.30f, White),  // storage room
                new PlanRect(21.60f, 13.40f, 0.70f, 1.20f, Blue),   // storage niche north
                new PlanRect(21.60f, 16.00f, 0.70f, 1.20f, Blue),   // storage niche south
                new PlanRect(9.70f, 2.30f, 4.10f, 1.60f, White),    // south corridor (1.6 wide)
                new PlanRect(13.80f, 2.30f, 0.50f, 1.60f, White),   // door 3 reveal
                new PlanRect(14.30f, 0.90f, 4.30f, 4.40f, Prison)   // holding cell
            };

            int sectorIndex = 0;
            foreach (PlanRect planRect in plan)
            {
                definition.sectors.Add(new SectorSpec
                {
                    id = $"S{++sectorIndex}",
                    floorY = 0f,
                    ceilingHeight = Ceiling,
                    floorAreas = new List<Rect> { planRect.rect }
                });
            }

            for (int i = 0; i < plan.Count; i++)
            {
                AddRectWalls(definition.walls, plan, i);
            }

            // Free-standing square pillars in the guard hall (0.7 x 0.7).
            AddPillar(definition.walls, new Vector2(5.40f, 15.20f), 0.35f, Blue);
            AddPillar(definition.walls, new Vector2(9.00f, 15.20f), 0.35f, Blue);

            // Chamfered corners — diagonal walls the Wolf grid could never hold.
            AddDiagonal(definition.walls, new Vector2(0.00f, 5.20f), new Vector2(1.10f, 6.30f), Blue);   // spawn hall NW
            AddDiagonal(definition.walls, new Vector2(8.60f, 6.30f), new Vector2(9.70f, 5.20f), Blue);   // spawn hall NE
            AddDiagonal(definition.walls, new Vector2(1.90f, 17.40f), new Vector2(3.00f, 18.50f), Blue); // guard hall NW
            AddDiagonal(definition.walls, new Vector2(11.40f, 18.50f), new Vector2(12.50f, 17.40f), Blue); // guard hall NE

            definition.doorways.Add(MakeDoor(new Vector2(5.00f, 6.55f), alongZ: false, width: 1.70f, type: "normal"));
            definition.doorways.Add(MakeDoor(new Vector2(16.15f, 15.15f), alongZ: true, width: 2.20f, type: "normal"));
            definition.doorways.Add(MakeDoor(new Vector2(14.05f, 3.10f), alongZ: true, width: 1.60f, type: "normal"));

            AddProp(definition, 14, 2.60f, 3.20f);   // ceiling light, spawn hall
            AddProp(definition, 14, 7.00f, 3.20f);   // ceiling light, spawn hall
            AddProp(definition, 35, 1.00f, 5.60f);   // barrel
            AddProp(definition, 11, 9.00f, 0.70f);   // plant
            AddProp(definition, 25, 4.80f, -0.40f);  // first aid in the niche
            AddProp(definition, 14, 5.00f, 9.40f);   // ceiling light, north corridor
            AddProp(definition, 4, 7.20f, 15.20f);   // chandelier, guard hall
            AddProp(definition, 2, 3.40f, 13.40f);   // table and chairs
            AddProp(definition, 26, 11.60f, 13.00f); // ammo clip
            AddProp(definition, 6, 10.80f, 17.30f);  // dog food
            AddProp(definition, 14, 14.20f, 15.15f); // ceiling light, east corridor
            AddProp(definition, 4, 19.00f, 15.05f);  // chandelier, storage
            AddProp(definition, 13, 18.00f, 13.00f); // bare table
            AddProp(definition, 26, 19.60f, 17.20f); // ammo clip
            AddProp(definition, 29, 21.95f, 14.00f); // cross in niche
            AddProp(definition, 30, 21.95f, 16.60f); // chalice in niche
            AddProp(definition, 14, 11.70f, 3.10f);  // ceiling light, south corridor
            AddProp(definition, 14, 16.45f, 3.10f);  // ceiling light, holding cell
            AddProp(definition, 9, 15.50f, 1.60f);   // skeleton
            AddProp(definition, 27, 17.60f, 4.40f);  // machine gun
            AddProp(definition, 24, 15.20f, 4.50f);  // food

            AddEnemy(definition, "guard", 4.20f, 16.60f, 180f);
            AddEnemy(definition, "guard", 10.00f, 14.20f, 270f);
            AddEnemy(definition, "dog", 7.20f, 17.60f, 180f);
            AddEnemy(definition, "guard", 19.80f, 12.80f, 90f);

            definition.playerSpawnPosition = new Vector3(4.80f, 0f, 2.20f);
            definition.playerSpawnYaw = 0f;
        }

        /// <summary>
        /// Walls for one plan rectangle: each side is walled on the intervals
        /// where no other rectangle adjoins it from outside.
        /// </summary>
        private static void AddRectWalls(List<WallSegmentSpec> walls, List<PlanRect> plan, int index)
        {
            Rect r = plan[index].rect;
            int style = plan[index].style;
            AddSideWalls(walls, plan, index, vertical: true, plane: r.xMin, from: r.yMin, to: r.yMax, outsideSign: -1, style);
            AddSideWalls(walls, plan, index, vertical: true, plane: r.xMax, from: r.yMin, to: r.yMax, outsideSign: +1, style);
            AddSideWalls(walls, plan, index, vertical: false, plane: r.yMin, from: r.xMin, to: r.xMax, outsideSign: -1, style);
            AddSideWalls(walls, plan, index, vertical: false, plane: r.yMax, from: r.xMin, to: r.xMax, outsideSign: +1, style);
        }

        private static void AddSideWalls(
            List<WallSegmentSpec> walls,
            List<PlanRect> plan,
            int index,
            bool vertical,
            float plane,
            float from,
            float to,
            int outsideSign,
            int style)
        {
            float probe = plane + outsideSign * Eps;

            var covers = new List<Vector2>();
            for (int i = 0; i < plan.Count; i++)
            {
                if (i == index)
                {
                    continue;
                }

                Rect other = plan[i].rect;
                bool coversProbe = vertical
                    ? other.xMin < probe && other.xMax > probe
                    : other.yMin < probe && other.yMax > probe;
                if (!coversProbe)
                {
                    continue;
                }

                float a = vertical ? other.yMin : other.xMin;
                float b = vertical ? other.yMax : other.xMax;
                a = Mathf.Max(a, from);
                b = Mathf.Min(b, to);
                if (b - a > Eps)
                {
                    covers.Add(new Vector2(a, b));
                }
            }

            foreach (Vector2 interval in SubtractIntervals(from, to, covers))
            {
                walls.Add(CreateAxisSegment(vertical, plane, interval.x, interval.y, outsideSign, style));
            }
        }

        private static List<Vector2> SubtractIntervals(float from, float to, List<Vector2> covers)
        {
            covers.Sort((p, q) => p.x.CompareTo(q.x));

            var result = new List<Vector2>();
            float cursor = from;
            foreach (Vector2 cover in covers)
            {
                if (cover.x > cursor + MinWallLength)
                {
                    result.Add(new Vector2(cursor, Mathf.Min(cover.x, to)));
                }

                cursor = Mathf.Max(cursor, cover.y);
                if (cursor >= to)
                {
                    return result;
                }
            }

            if (to - cursor > MinWallLength)
            {
                result.Add(new Vector2(cursor, to));
            }

            return result;
        }

        /// <summary>
        /// Axis-aligned segment ordered so the visible face
        /// (up x direction) points back into the rectangle.
        /// </summary>
        private static WallSegmentSpec CreateAxisSegment(bool vertical, float plane, float from, float to, int outsideSign, int style)
        {
            Vector2 start;
            Vector2 end;
            if (vertical)
            {
                // Wall along Z; room on +x side when this is the west side.
                start = outsideSign < 0 ? new Vector2(plane, from) : new Vector2(plane, to);
                end = outsideSign < 0 ? new Vector2(plane, to) : new Vector2(plane, from);
            }
            else
            {
                // Wall along X; room on +z side when this is the south side.
                start = outsideSign < 0 ? new Vector2(to, plane) : new Vector2(from, plane);
                end = outsideSign < 0 ? new Vector2(from, plane) : new Vector2(to, plane);
            }

            return new WallSegmentSpec
            {
                start = start,
                end = end,
                baseY = 0f,
                height = Ceiling,
                style = style
            };
        }

        /// <summary>Free-standing square pillar: four wall faces pointing outward.</summary>
        private static void AddPillar(List<WallSegmentSpec> walls, Vector2 center, float halfSize, int style)
        {
            float x0 = center.x - halfSize;
            float x1 = center.x + halfSize;
            float z0 = center.y - halfSize;
            float z1 = center.y + halfSize;

            walls.Add(MakeSegment(new Vector2(x0, z1), new Vector2(x0, z0), style)); // west face, normal -x
            walls.Add(MakeSegment(new Vector2(x1, z0), new Vector2(x1, z1), style)); // east face, normal +x
            walls.Add(MakeSegment(new Vector2(x0, z0), new Vector2(x1, z0), style)); // south face, normal -z
            walls.Add(MakeSegment(new Vector2(x1, z1), new Vector2(x0, z1), style)); // north face, normal +z
        }

        /// <summary>Diagonal wall; order start->end so up x direction faces the room.</summary>
        private static void AddDiagonal(List<WallSegmentSpec> walls, Vector2 start, Vector2 end, int style)
        {
            walls.Add(MakeSegment(start, end, style));
        }

        private static WallSegmentSpec MakeSegment(Vector2 start, Vector2 end, int style)
        {
            return new WallSegmentSpec
            {
                start = start,
                end = end,
                baseY = 0f,
                height = Ceiling,
                style = style
            };
        }

        private static DoorwaySpec MakeDoor(Vector2 center, bool alongZ, float width, string type)
        {
            return new DoorwaySpec
            {
                center = center,
                baseY = 0f,
                ceilingHeight = Ceiling,
                alongZ = alongZ,
                width = width,
                height = Mathf.Min(WolfMiniConstants.DoorHeight, Ceiling),
                type = type
            };
        }

        private static void AddProp(WolfSectorLevelDefinition definition, int typeIndex, float x, float z)
        {
            definition.props.Add(new LevelPropSpec
            {
                position = new Vector3(x, 0f, z),
                typeIndex = typeIndex,
                typeName = WolfLevelContent.StatInfos[typeIndex].name
            });
        }

        private static void AddEnemy(WolfSectorLevelDefinition definition, string type, float x, float z, float yaw)
        {
            definition.enemies.Add(new LevelEnemySpec
            {
                position = new Vector3(x, 0f, z),
                type = type,
                yaw = yaw
            });
        }
    }
}
