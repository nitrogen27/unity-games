using System.Collections.Generic;
using HelloWorldRoom;
using WolfMini.Core;
using WolfMini.Rendering;
using UnityEngine;

namespace WolfMini.Level
{
    /// <summary>
    /// Builds the level from a <see cref="WolfLevelDefinition"/> as chunked combined
    /// meshes (floor/ceiling/wall submeshes per chunk) instead of one cube per cell.
    /// Wall faces share a single atlas material with per-face UVs; floors and
    /// ceilings tile in fixed 2 m texture modules, which removes the giant-quad
    /// stretching and per-cube seams that showed up when looking at the ceiling.
    /// </summary>
    public sealed class WolfLevelMeshBuilder : MonoBehaviour
    {
        private const float Cell = WolfMiniConstants.CellSize;
        private const float TextureTileSize = WolfMiniConstants.WallTextureModule;
        private const float WorldScale = WolfMiniConstants.WorldScale;
        private const float DoorThickness = WolfMiniConstants.DoorThickness;
        private const float DoorTravel = WolfMiniConstants.DoorTravel;

        [SerializeField] private WolfLevelDefinition definition;
        [SerializeField] private WolfFull3DMaterialLibrary materialLibrary;
        [SerializeField] private bool buildOnStart = true;
        [SerializeField] private bool buildLights = true;
        // The target-look scene runs 48 realtime lamp lights in deferred.
        [SerializeField] private int maxRealtimeLightsPerFloor = 48;
        [SerializeField] private int chunkSizeCells = 16;

        private static readonly Vector2Int[] CardinalDirections =
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };

        public WolfLevelDefinition Definition
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
                Debug.LogWarning("[WolfFull3D] WolfLevelMeshBuilder has no definition assigned.", this);
                return;
            }

            if (materialLibrary == null)
            {
                Debug.LogWarning("[WolfFull3D] WolfLevelMeshBuilder has no material library assigned.", this);
                return;
            }

            if (!definition.TryValidate(out List<string> errors))
            {
                foreach (string error in errors)
                {
                    Debug.LogError($"[WolfFull3D] {definition.name}: {error}", definition);
                }

                return;
            }

            Transform root = ResetGeneratedRoot();

            foreach (GridFloorSpec floor in definition.floors)
            {
                GameObject floorRoot = new GameObject($"{floor.id} Floor");
                floorRoot.transform.SetParent(root, false);

                BuildFloorChunks(floorRoot.transform, floor);
                BuildDoors(floorRoot.transform, floor);
                BuildStatics(floorRoot.transform, floor);
                BuildEnemies(floorRoot.transform, floor);
            }
        }

        private Transform ResetGeneratedRoot()
        {
            GeneratedLevelRoot existing = GetComponentInChildren<GeneratedLevelRoot>(true);
            if (existing != null)
            {
                DestroySafely(existing.gameObject);
            }

            GameObject root = new GameObject("Generated Full3D Level");
            root.transform.SetParent(transform, false);
            root.AddComponent<GeneratedLevelRoot>();
            return root.transform;
        }

        private void BuildFloorChunks(Transform parent, GridFloorSpec floor)
        {
            int chunk = Mathf.Max(4, chunkSizeCells);
            for (int chunkZ = 0; chunkZ < floor.height; chunkZ += chunk)
            {
                for (int chunkX = 0; chunkX < floor.width; chunkX += chunk)
                {
                    BuildChunk(parent, floor, chunkX, chunkZ,
                        Mathf.Min(chunk, floor.width - chunkX),
                        Mathf.Min(chunk, floor.height - chunkZ));
                }
            }
        }

        private void BuildChunk(Transform parent, GridFloorSpec floor, int originX, int originZ, int cellsX, int cellsZ)
        {
            var buffer = new WolfMeshBuffer(submeshCount: 3);
            const int floorSubmesh = 0;
            const int ceilingSubmesh = 1;
            const int wallSubmesh = 2;

            // Wall values with a target-look override material get their own
            // submesh with 0..1 UVs per face; the rest share the atlas submesh.
            var overrideSubmeshes = new Dictionary<Material, int>();
            var overrideMaterials = new List<Material>();

            float floorY = floor.y;
            float ceilingY = floor.y + floor.ceilingHeight;

            for (int z = originZ; z < originZ + cellsZ; z++)
            {
                for (int x = originX; x < originX + cellsX; x++)
                {
                    int wallValue = floor.WallAt(x, z);
                    float x0 = x * Cell;
                    float x1 = (x + 1) * Cell;
                    float z0 = z * Cell;
                    float z1 = (z + 1) * Cell;

                    if (wallValue <= 0)
                    {
                        Rect cellUV = new Rect(x0 / TextureTileSize, z0 / TextureTileSize, Cell / TextureTileSize, Cell / TextureTileSize);
                        if (!definition.IsFloorSlabCut(floor.id, x, z))
                        {
                            buffer.AddQuad(floorSubmesh,
                                new Vector3(x0, floorY, z0),
                                new Vector3(x1, floorY, z0),
                                new Vector3(x1, floorY, z1),
                                new Vector3(x0, floorY, z1),
                                Vector3.up, cellUV);
                        }

                        if (!definition.IsCeilingCut(floor.id, x, z))
                        {
                            buffer.AddQuad(ceilingSubmesh,
                                new Vector3(x1, ceilingY, z0),
                                new Vector3(x0, ceilingY, z0),
                                new Vector3(x0, ceilingY, z1),
                                new Vector3(x1, ceilingY, z1),
                                Vector3.down, cellUV);
                        }

                        continue;
                    }

                    int submesh = wallSubmesh;
                    Rect tileUV;
                    bool atlasWindow = true;
                    bool tileVertically = true;
                    if (materialLibrary.TryGetWallOverride(wallValue, out Material overrideMaterial, out bool overrideTiles))
                    {
                        if (!overrideSubmeshes.TryGetValue(overrideMaterial, out submesh))
                        {
                            submesh = 3 + overrideMaterials.Count;
                            overrideSubmeshes[overrideMaterial] = submesh;
                            overrideMaterials.Add(overrideMaterial);
                        }

                        tileUV = new Rect(0f, 0f, 1f, 1f);
                        atlasWindow = false;
                        tileVertically = overrideTiles;
                    }
                    else
                    {
                        tileUV = WolfFull3DMaterialLibrary.GetWallTileUV(wallValue);
                    }

                    foreach (Vector2Int direction in CardinalDirections)
                    {
                        if (!floor.IsWalkable(x + direction.x, z + direction.y))
                        {
                            continue;
                        }

                        AddWallColumn(buffer, submesh, direction, x0, x1, z0, z1, floorY, ceilingY, tileUV, atlasWindow, tileVertically);
                    }
                }
            }

            if (buffer.IsEmpty)
            {
                return;
            }

            GameObject chunkObject = new GameObject($"{floor.id} Chunk {originX:00}_{originZ:00}");
            chunkObject.transform.SetParent(parent, false);
            chunkObject.isStatic = true;

            var submeshMaterials = new List<Material>
            {
                materialLibrary.FloorMaterial,
                materialLibrary.CeilingMaterial,
                materialLibrary.WallAtlasMaterial
            };
            submeshMaterials.AddRange(overrideMaterials);
            while (submeshMaterials.Count > buffer.SubmeshCount)
            {
                submeshMaterials.RemoveAt(submeshMaterials.Count - 1);
            }

            Mesh mesh = buffer.ToMesh($"{floor.id}_Chunk_{originX:00}_{originZ:00}");
            chunkObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            chunkObject.AddComponent<MeshRenderer>().sharedMaterials = submeshMaterials.ToArray();
            chunkObject.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        /// <summary>
        /// Emits one wall face, keeping texel density at one texture repeat per
        /// fixed 2 m texture module. Atlas windows cannot wrap, so they are emitted as
        /// stacked quads (full tiles plus a proportional partial slice); override
        /// materials with repeat wrap use a single quad with UVs scaled to the
        /// wall dimensions; clamped feature walls are sliced into modules too.
        /// </summary>
        private static void AddWallColumn(
            WolfMeshBuffer buffer,
            int submesh,
            Vector2Int direction,
            float x0,
            float x1,
            float z0,
            float z1,
            float y0,
            float y1,
            Rect uv,
            bool atlasWindow,
            bool tileVertically)
        {
            if (!atlasWindow)
            {
                if (tileVertically)
                {
                    Rect repeated = new Rect(uv.x, uv.y, uv.width * Cell / TextureTileSize, uv.height * (y1 - y0) / TextureTileSize);
                    AddWallFace(buffer, submesh, direction, x0, x1, z0, z1, y0, y1, repeated);
                    return;
                }

                AddSlicedWallFace(buffer, submesh, direction, x0, x1, z0, z1, y0, y1, uv);
                return;
            }

            AddSlicedWallFace(buffer, submesh, direction, x0, x1, z0, z1, y0, y1, uv);
        }

        private static void AddSlicedWallFace(
            WolfMeshBuffer buffer,
            int submesh,
            Vector2Int direction,
            float x0,
            float x1,
            float z0,
            float z1,
            float y0,
            float y1,
            Rect uv)
        {
            for (float along = 0f; along < Cell - 0.001f; along += TextureTileSize)
            {
                float pieceEnd = Mathf.Min(along + TextureTileSize, Cell);
                float widthFraction = (pieceEnd - along) / TextureTileSize;
                for (float y = y0; y < y1 - 0.001f; y += TextureTileSize)
                {
                    float segmentTop = Mathf.Min(y + TextureTileSize, y1);
                    float heightFraction = (segmentTop - y) / TextureTileSize;
                    Rect segmentUV = new Rect(uv.xMin, uv.yMin, uv.width * widthFraction, uv.height * heightFraction);
                    AddWallFaceSlice(buffer, submesh, direction, x0, x1, z0, z1, along, pieceEnd, y, segmentTop, segmentUV);
                }
            }
        }

        private static void AddWallFace(
            WolfMeshBuffer buffer,
            int submesh,
            Vector2Int direction,
            float x0,
            float x1,
            float z0,
            float z1,
            float y0,
            float y1,
            Rect uv)
        {
            AddWallFaceSlice(buffer, submesh, direction, x0, x1, z0, z1, 0f, Cell, y0, y1, uv);
        }

        private static void AddWallFaceSlice(
            WolfMeshBuffer buffer,
            int submesh,
            Vector2Int direction,
            float x0,
            float x1,
            float z0,
            float z1,
            float fromDistance,
            float toDistance,
            float y0,
            float y1,
            Rect uv)
        {
            if (direction.x > 0)
            {
                buffer.AddQuad(submesh,
                    new Vector3(x1, y0, z0 + fromDistance), new Vector3(x1, y0, z0 + toDistance),
                    new Vector3(x1, y1, z0 + toDistance), new Vector3(x1, y1, z0 + fromDistance),
                    Vector3.right, uv);
            }
            else if (direction.x < 0)
            {
                buffer.AddQuad(submesh,
                    new Vector3(x0, y0, z1 - fromDistance), new Vector3(x0, y0, z1 - toDistance),
                    new Vector3(x0, y1, z1 - toDistance), new Vector3(x0, y1, z1 - fromDistance),
                    Vector3.left, uv);
            }
            else if (direction.y > 0)
            {
                buffer.AddQuad(submesh,
                    new Vector3(x1 - fromDistance, y0, z1), new Vector3(x1 - toDistance, y0, z1),
                    new Vector3(x1 - toDistance, y1, z1), new Vector3(x1 - fromDistance, y1, z1),
                    Vector3.forward, uv);
            }
            else
            {
                buffer.AddQuad(submesh,
                    new Vector3(x0 + fromDistance, y0, z0), new Vector3(x0 + toDistance, y0, z0),
                    new Vector3(x0 + toDistance, y1, z0), new Vector3(x0 + fromDistance, y1, z0),
                    Vector3.back, uv);
            }
        }

        private void BuildDoors(Transform parent, GridFloorSpec floor)
        {
            if (floor.doors == null)
            {
                return;
            }

            foreach (GridDoorSpec doorSpec in floor.doors)
            {
                if (doorSpec == null)
                {
                    continue;
                }

                // Doors are real door-sized slabs; the band between the door top
                // and the ceiling is filled by a static lintel below.
                float doorHeight = Mathf.Min(WolfMiniConstants.DoorHeight, floor.ceilingHeight);
                Vector3 size = doorSpec.vertical
                    ? new Vector3(DoorThickness, doorHeight, Cell)
                    : new Vector3(Cell, doorHeight, DoorThickness);

                int faceTile = doorSpec.type switch
                {
                    "elevator" => WolfFull3DMaterialLibrary.ElevatorDoorTile,
                    "gold" => WolfFull3DMaterialLibrary.LockedDoorTile,
                    "silver" => WolfFull3DMaterialLibrary.LockedDoorTile,
                    _ => WolfFull3DMaterialLibrary.NormalDoorTile
                };

                // Target-look door materials use 0..1 UVs; without them the faces
                // fall back to the matching window of the walls atlas.
                Material faceMaterial = materialLibrary.DoorFaceMaterial;
                Material jambMaterial = materialLibrary.DoorJambMaterial;
                Rect faceUV = faceMaterial != null
                    ? new Rect(0f, 0f, 1f, 1f)
                    : WolfFull3DMaterialLibrary.GetAtlasTileUV(faceTile, WolfFull3DMaterialLibrary.WallAtlasSize);
                Rect jambUV = jambMaterial != null
                    ? new Rect(0f, 0f, 1f, 1f)
                    : WolfFull3DMaterialLibrary.GetAtlasTileUV(WolfFull3DMaterialLibrary.DoorJambTile, WolfFull3DMaterialLibrary.WallAtlasSize);

                GameObject door = new GameObject($"Door {doorSpec.type} {doorSpec.x:00},{doorSpec.y:00}");
                door.transform.SetParent(parent, false);
                door.transform.position = CellCenter(doorSpec.x, doorSpec.y, floor.y + doorHeight * 0.5f);

                Mesh mesh = CreateDoorBoxMesh(size, doorSpec.vertical, faceUV, jambUV);
                door.AddComponent<MeshFilter>().sharedMesh = mesh;
                door.AddComponent<MeshRenderer>().sharedMaterials = new[]
                {
                    faceMaterial != null ? faceMaterial : materialLibrary.WallAtlasMaterial,
                    jambMaterial != null ? jambMaterial : materialLibrary.WallAtlasMaterial
                };
                BoxCollider collider = door.AddComponent<BoxCollider>();
                collider.size = size;

                Vector3 slideOffset = doorSpec.vertical
                    ? new Vector3(0f, 0f, DoorTravel)
                    : new Vector3(DoorTravel, 0f, 0f);
                bool locked = doorSpec.type == "gold" || doorSpec.type == "silver";
                door.AddComponent<WolfDoor>().Configure(slideOffset, 2.4f * WorldScale, 3.5f, locked);

                BuildDoorLintel(parent, floor, doorSpec, doorHeight, jambUV,
                    jambMaterial != null ? jambMaterial : materialLibrary.WallAtlasMaterial);
            }
        }

        /// <summary>Static band filling the doorway between the door top and the ceiling.</summary>
        private static void BuildDoorLintel(Transform parent, GridFloorSpec floor, GridDoorSpec doorSpec, float doorHeight, Rect jambUV, Material material)
        {
            float lintelHeight = floor.ceilingHeight - doorHeight;
            if (lintelHeight < 0.01f)
            {
                return;
            }

            Vector3 size = doorSpec.vertical
                ? new Vector3(DoorThickness, lintelHeight, Cell)
                : new Vector3(Cell, lintelHeight, DoorThickness);

            GameObject lintel = new GameObject($"Door lintel {doorSpec.x:00},{doorSpec.y:00}");
            lintel.transform.SetParent(parent, false);
            lintel.transform.position = CellCenter(doorSpec.x, doorSpec.y, floor.y + doorHeight + lintelHeight * 0.5f);
            lintel.isStatic = true;

            Mesh mesh = CreateDoorBoxMesh(size, doorSpec.vertical, jambUV, jambUV);
            lintel.AddComponent<MeshFilter>().sharedMesh = mesh;
            lintel.AddComponent<MeshRenderer>().sharedMaterials = new[] { material, material };
            BoxCollider collider = lintel.AddComponent<BoxCollider>();
            collider.size = size;
        }

        private static Mesh CreateDoorBoxMesh(Vector3 size, bool vertical, Rect faceUV, Rect jambUV)
        {
            // Submesh 0 = the two large door faces, submesh 1 = jamb/top/bottom,
            // so the face and jamb can use different target-look materials.
            var buffer = new WolfMeshBuffer(submeshCount: 2);
            Vector3 h = size * 0.5f;

            int eastWestSubmesh = vertical ? 0 : 1;
            int northSouthSubmesh = vertical ? 1 : 0;
            Rect eastWest = vertical ? faceUV : jambUV;
            Rect northSouth = vertical ? jambUV : faceUV;

            buffer.AddQuad(eastWestSubmesh, new Vector3(h.x, -h.y, -h.z), new Vector3(h.x, -h.y, h.z), new Vector3(h.x, h.y, h.z), new Vector3(h.x, h.y, -h.z), Vector3.right, eastWest);
            buffer.AddQuad(eastWestSubmesh, new Vector3(-h.x, -h.y, h.z), new Vector3(-h.x, -h.y, -h.z), new Vector3(-h.x, h.y, -h.z), new Vector3(-h.x, h.y, h.z), Vector3.left, eastWest);
            buffer.AddQuad(northSouthSubmesh, new Vector3(h.x, -h.y, h.z), new Vector3(-h.x, -h.y, h.z), new Vector3(-h.x, h.y, h.z), new Vector3(h.x, h.y, h.z), Vector3.forward, northSouth);
            buffer.AddQuad(northSouthSubmesh, new Vector3(-h.x, -h.y, -h.z), new Vector3(h.x, -h.y, -h.z), new Vector3(h.x, h.y, -h.z), new Vector3(-h.x, h.y, -h.z), Vector3.back, northSouth);
            buffer.AddQuad(1, new Vector3(-h.x, h.y, -h.z), new Vector3(h.x, h.y, -h.z), new Vector3(h.x, h.y, h.z), new Vector3(-h.x, h.y, h.z), Vector3.up, jambUV);
            buffer.AddQuad(1, new Vector3(h.x, -h.y, -h.z), new Vector3(-h.x, -h.y, -h.z), new Vector3(-h.x, -h.y, h.z), new Vector3(h.x, -h.y, h.z), Vector3.down, jambUV);

            return buffer.ToMesh("DoorBox");
        }

        private void BuildStatics(Transform parent, GridFloorSpec floor)
        {
            if (floor.statics == null)
            {
                return;
            }

            int lightsBudget = maxRealtimeLightsPerFloor;

            foreach (GridStaticSpec staticSpec in floor.statics)
            {
                if (staticSpec == null || staticSpec.typeIndex < 0 || staticSpec.typeIndex >= WolfLevelContent.StatInfos.Length)
                {
                    continue;
                }

                WolfLevelContent.StatInfo info = WolfLevelContent.StatInfos[staticSpec.typeIndex];
                Vector3 basePosition = CellCenter(staticSpec.x, staticSpec.y, floor.y);

                // Ceiling lights and chandeliers become lamp fixtures + point lights.
                if (staticSpec.typeIndex == WolfLevelContent.CeilLightTypeIndex || staticSpec.typeIndex == WolfLevelContent.ChandelierTypeIndex)
                {
                    bool warm = staticSpec.typeIndex == WolfLevelContent.ChandelierTypeIndex;
                    bool withLight = buildLights && lightsBudget > 0;
                    if (withLight)
                    {
                        lightsBudget--;
                    }

                    AddCeilingLamp(parent, info.name, basePosition, floor, warm, withLight);
                    continue;
                }

                // pans are skipped to match the reference builder.
                if (staticSpec.typeIndex == WolfLevelContent.PansTypeIndex)
                {
                    continue;
                }

                float scale = WolfLevelContent.GetStaticScale(info.pickupType);
                Material material = materialLibrary.GetStaticMaterial(staticSpec.typeIndex, WolfLevelContent.SpriteOffset);
                CreateBillboard(
                    parent,
                    $"{info.name} {staticSpec.x:00},{staticSpec.y:00}",
                    new Vector3(basePosition.x, floor.y + 0.02f + scale * 0.6f, basePosition.z),
                    new Vector2(scale, scale * 1.2f),
                    material,
                    info.blocking,
                    floor.y);
            }
        }

        private void BuildEnemies(Transform parent, GridFloorSpec floor)
        {
            if (floor.enemies == null)
            {
                return;
            }

            foreach (GridEnemySpec enemySpec in floor.enemies)
            {
                if (enemySpec == null)
                {
                    continue;
                }

                WolfLevelContent.EnemyInfo info = WolfLevelContent.EnemyInfos.TryGetValue(enemySpec.type, out WolfLevelContent.EnemyInfo found)
                    ? found
                    : WolfLevelContent.EnemyInfos["guard"];
                Vector3 basePosition = CellCenter(enemySpec.x, enemySpec.y, floor.y);
                Material material = materialLibrary.GetEnemyMaterial(enemySpec.type, info.tint);
                CreateBillboard(
                    parent,
                    $"{enemySpec.type} {enemySpec.x:00},{enemySpec.y:00}",
                    new Vector3(basePosition.x, floor.y + info.height * 0.5f, basePosition.z),
                    new Vector2(info.width, info.height),
                    material,
                    true,
                    floor.y);
            }
        }

        private void AddCeilingLamp(Transform parent, string name, Vector3 basePosition, GridFloorSpec floor, bool warm, bool withLight)
        {
            float ceilingY = floor.y + floor.ceilingHeight;

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
            // The target-look scene baked 2.0/1.7 with GI; these run realtime in
            // deferred, so slightly lower values give a comparable exposure.
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

        private static Vector3 CellCenter(int x, int z, float y)
        {
            return new Vector3((x + 0.5f) * Cell, y, (z + 0.5f) * Cell);
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

    /// <summary>Accumulates quads into vertex/uv/normal lists with per-submesh triangle indices.</summary>
    public sealed class WolfMeshBuffer
    {
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Vector3> normals = new List<Vector3>();
        private readonly List<Vector2> uvs = new List<Vector2>();
        // TEXCOORD1: atlas-window origin consumed by the WolfMini/AtlasRepeat
        // shader; zero for quads rendered with plain materials.
        private readonly List<Vector2> atlasWindows = new List<Vector2>();
        private readonly List<List<int>> submeshTriangles = new List<List<int>>();

        public WolfMeshBuffer(int submeshCount)
        {
            EnsureSubmesh(submeshCount - 1);
        }

        public bool IsEmpty => vertices.Count == 0;

        public int SubmeshCount => submeshTriangles.Count;

        private void EnsureSubmesh(int submesh)
        {
            while (submeshTriangles.Count <= submesh)
            {
                submeshTriangles.Add(new List<int>());
            }
        }

        /// <summary>
        /// Adds a quad. Corners are given as bottom-left, bottom-right, top-right,
        /// top-left as seen from the front (normal side); UV rect maps accordingly.
        /// </summary>
        public void AddQuad(int submesh, Vector3 bl, Vector3 br, Vector3 tr, Vector3 tl, Vector3 normal, Rect uv)
        {
            AddQuad(submesh, bl, br, tr, tl, normal, uv, Vector2.zero);
        }

        /// <summary>Adds a quad carrying an atlas-window origin in TEXCOORD1 for the atlas-repeat shader.</summary>
        public void AddQuad(int submesh, Vector3 bl, Vector3 br, Vector3 tr, Vector3 tl, Vector3 normal, Rect uv, Vector2 atlasWindow)
        {
            EnsureSubmesh(submesh);
            int baseIndex = vertices.Count;
            vertices.Add(bl);
            vertices.Add(br);
            vertices.Add(tr);
            vertices.Add(tl);

            for (int i = 0; i < 4; i++)
            {
                normals.Add(normal);
            }

            uvs.Add(new Vector2(uv.xMin, uv.yMin));
            uvs.Add(new Vector2(uv.xMax, uv.yMin));
            uvs.Add(new Vector2(uv.xMax, uv.yMax));
            uvs.Add(new Vector2(uv.xMin, uv.yMax));

            for (int i = 0; i < 4; i++)
            {
                atlasWindows.Add(atlasWindow);
            }

            List<int> triangles = submeshTriangles[submesh];
            triangles.Add(baseIndex);
            triangles.Add(baseIndex + 3);
            triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex);
            triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex + 1);
        }

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh
            {
                name = name,
                indexFormat = vertices.Count > 65000
                    ? UnityEngine.Rendering.IndexFormat.UInt32
                    : UnityEngine.Rendering.IndexFormat.UInt16
            };

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetUVs(1, atlasWindows);
            mesh.subMeshCount = submeshTriangles.Count;
            for (int i = 0; i < submeshTriangles.Count; i++)
            {
                mesh.SetTriangles(submeshTriangles[i], i);
            }

            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
