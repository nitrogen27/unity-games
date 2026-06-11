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
    /// ceilings tile one texture repeat per cell, which removes the giant-quad
    /// stretching and per-cube seams that showed up when looking at the ceiling.
    /// </summary>
    public sealed class WolfLevelMeshBuilder : MonoBehaviour
    {
        private const float Cell = WolfMiniConstants.CellSize;
        private const float DoorThickness = WolfMiniConstants.DoorThickness;
        private const float DoorTravel = WolfMiniConstants.DoorTravel;
        private const int StatSpriteOffset = 2;

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

        private static readonly StatInfo[] StatInfos =
        {
            new StatInfo("puddle", false, null),
            new StatInfo("greenBarrel", true, null),
            new StatInfo("tableChairs", true, null),
            new StatInfo("floorLamp", true, null),
            new StatInfo("chandelier", false, null),
            new StatInfo("hangedMan", true, null),
            new StatInfo("dogFood", false, "food"),
            new StatInfo("pillar", true, null),
            new StatInfo("tree", true, null),
            new StatInfo("skeleton", false, null),
            new StatInfo("sink", true, null),
            new StatInfo("plant", true, null),
            new StatInfo("urn", true, null),
            new StatInfo("bareTable", true, null),
            new StatInfo("ceilLight", false, null),
            new StatInfo("pans", false, null),
            new StatInfo("armor", true, null),
            new StatInfo("cage", true, null),
            new StatInfo("cageSkel", true, null),
            new StatInfo("bonesRelax", false, null),
            new StatInfo("key1", false, "key1"),
            new StatInfo("key2", false, "key2"),
            new StatInfo("stuff", true, null),
            new StatInfo("junk", false, null),
            new StatInfo("food", false, "food"),
            new StatInfo("firstaid", false, "health"),
            new StatInfo("clip", false, "ammo"),
            new StatInfo("machinegun", false, "machinegun"),
            new StatInfo("chaingun", false, "chaingun"),
            new StatInfo("cross", false, "cross"),
            new StatInfo("chalice", false, "chalice"),
            new StatInfo("bible", false, "bible"),
            new StatInfo("crown", false, "crown"),
            new StatInfo("oneUp", false, "oneup"),
            new StatInfo("gibs", false, null),
            new StatInfo("barrel", true, null),
            new StatInfo("well", true, null),
            new StatInfo("emptyWell", true, null),
            new StatInfo("gibs2", false, null),
            new StatInfo("flag", true, null),
            new StatInfo("callApogee", true, null),
            new StatInfo("junk2", false, null),
            new StatInfo("junk3", false, null),
            new StatInfo("junk4", false, null),
            new StatInfo("pots", false, null),
            new StatInfo("stove", true, null),
            new StatInfo("spears", true, null)
        };

        private static readonly Dictionary<string, EnemyInfo> EnemyInfos = new Dictionary<string, EnemyInfo>
        {
            ["guard"] = new EnemyInfo(1.55f, 2.08f, Color.white),
            ["officer"] = new EnemyInfo(1.55f, 2.08f, new Color(0.9f, 0.9f, 1.2f)),
            ["ss"] = new EnemyInfo(1.68f, 2.24f, new Color(0.5f, 0.5f, 0.5f)),
            ["dog"] = new EnemyInfo(1.30f, 1.05f, Color.white),
            ["mutant"] = new EnemyInfo(1.68f, 2.24f, new Color(0.4f, 0.8f, 0.3f)),
            ["boss"] = new EnemyInfo(2.30f, 2.78f, new Color(1.2f, 0.8f, 0.8f))
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
                        Rect cellUV = new Rect(x, z, 1f, 1f);
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
                    if (materialLibrary.TryGetWallOverride(wallValue, out Material overrideMaterial))
                    {
                        if (!overrideSubmeshes.TryGetValue(overrideMaterial, out submesh))
                        {
                            submesh = 3 + overrideMaterials.Count;
                            overrideSubmeshes[overrideMaterial] = submesh;
                            overrideMaterials.Add(overrideMaterial);
                        }

                        tileUV = new Rect(0f, 0f, 1f, 1f);
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

                        AddWallFace(buffer, submesh, direction, x0, x1, z0, z1, floorY, ceilingY, tileUV);
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
            if (direction.x > 0)
            {
                buffer.AddQuad(submesh,
                    new Vector3(x1, y0, z0), new Vector3(x1, y0, z1),
                    new Vector3(x1, y1, z1), new Vector3(x1, y1, z0),
                    Vector3.right, uv);
            }
            else if (direction.x < 0)
            {
                buffer.AddQuad(submesh,
                    new Vector3(x0, y0, z1), new Vector3(x0, y0, z0),
                    new Vector3(x0, y1, z0), new Vector3(x0, y1, z1),
                    Vector3.left, uv);
            }
            else if (direction.y > 0)
            {
                buffer.AddQuad(submesh,
                    new Vector3(x1, y0, z1), new Vector3(x0, y0, z1),
                    new Vector3(x0, y1, z1), new Vector3(x1, y1, z1),
                    Vector3.forward, uv);
            }
            else
            {
                buffer.AddQuad(submesh,
                    new Vector3(x0, y0, z0), new Vector3(x1, y0, z0),
                    new Vector3(x1, y1, z0), new Vector3(x0, y1, z0),
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

                Vector3 size = doorSpec.vertical
                    ? new Vector3(DoorThickness, floor.ceilingHeight, Cell)
                    : new Vector3(Cell, floor.ceilingHeight, DoorThickness);

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
                door.transform.position = CellCenter(doorSpec.x, doorSpec.y, floor.y + floor.ceilingHeight * 0.5f);

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
                door.AddComponent<WolfDoor>().Configure(slideOffset, 2.4f, 3.5f, locked);
            }
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
                if (staticSpec == null || staticSpec.typeIndex < 0 || staticSpec.typeIndex >= StatInfos.Length)
                {
                    continue;
                }

                StatInfo info = StatInfos[staticSpec.typeIndex];
                Vector3 basePosition = CellCenter(staticSpec.x, staticSpec.y, floor.y);

                // ceilLight (14) and chandelier (4) become lamp fixtures + point lights.
                if (staticSpec.typeIndex == 14 || staticSpec.typeIndex == 4)
                {
                    bool warm = staticSpec.typeIndex == 4;
                    bool withLight = buildLights && lightsBudget > 0;
                    if (withLight)
                    {
                        lightsBudget--;
                    }

                    AddCeilingLamp(parent, info.name, basePosition, floor, warm, withLight);
                    continue;
                }

                // 15 (pans) is skipped to match the reference builder.
                if (staticSpec.typeIndex == 15)
                {
                    continue;
                }

                float scale = info.pickupType == null ? 1.0f : GetPickupScale(info.pickupType);
                Material material = materialLibrary.GetStaticMaterial(staticSpec.typeIndex, StatSpriteOffset);
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

                EnemyInfo info = EnemyInfos.TryGetValue(enemySpec.type, out EnemyInfo found)
                    ? found
                    : EnemyInfos["guard"];
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

        // Lamp light colors and intensities ported from WolfDynamicLightingSetup
        // (chandeliers warm and stronger, ceiling lights cooler).
        private static readonly Color WarmLampColor = new Color(1.0f, 0.72f, 0.36f);
        private static readonly Color CoolLampColor = new Color(1.0f, 0.84f, 0.62f);

        private void AddCeilingLamp(Transform parent, string name, Vector3 basePosition, GridFloorSpec floor, bool warm, bool withLight)
        {
            float ceilingY = floor.y + floor.ceilingHeight;

            GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cap.name = $"{name} cap {basePosition.x:0},{basePosition.z:0}";
            cap.transform.position = new Vector3(basePosition.x, ceilingY - 0.05f, basePosition.z);
            cap.transform.localScale = new Vector3(0.48f, 0.05f, 0.48f);
            cap.transform.SetParent(parent, true);
            Material capMaterial = materialLibrary.LampCapMaterial != null
                ? materialLibrary.LampCapMaterial
                : warm ? materialLibrary.LampWarmCap : materialLibrary.LampGreenCap;
            cap.GetComponent<Renderer>().sharedMaterial = capMaterial;
            DestroySafely(cap.GetComponent<Collider>());

            GameObject bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulb.name = $"{name} bulb {basePosition.x:0},{basePosition.z:0}";
            bulb.transform.position = new Vector3(basePosition.x, ceilingY - 0.16f, basePosition.z);
            bulb.transform.localScale = Vector3.one * 0.20f;
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
            lightObject.transform.position = new Vector3(basePosition.x, ceilingY - 0.22f, basePosition.z);
            lightObject.transform.SetParent(parent, true);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = warm ? WarmLampColor : CoolLampColor;
            // The target-look scene baked 2.0/1.7 with GI; these run realtime in
            // deferred, so slightly lower values give a comparable exposure.
            light.intensity = warm ? 1.4f : 1.15f;
            light.range = warm ? 16f : 13f;
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
            float diameter = warm ? 4.8f : 3.8f;
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

        private static float GetPickupScale(string pickupType)
        {
            return pickupType switch
            {
                "ammo" => 0.66f,
                "food" => 0.72f,
                "health" => 0.78f,
                "key1" => 0.72f,
                "key2" => 0.72f,
                _ => 0.82f
            };
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

        private readonly struct StatInfo
        {
            public readonly string name;
            public readonly bool blocking;
            public readonly string pickupType;

            public StatInfo(string name, bool blocking, string pickupType)
            {
                this.name = name;
                this.blocking = blocking;
                this.pickupType = pickupType;
            }
        }

        private readonly struct EnemyInfo
        {
            public readonly float width;
            public readonly float height;
            public readonly Color tint;

            public EnemyInfo(float width, float height, Color tint)
            {
                this.width = width;
                this.height = height;
                this.tint = tint;
            }
        }
    }

    /// <summary>Accumulates quads into vertex/uv/normal lists with per-submesh triangle indices.</summary>
    public sealed class WolfMeshBuffer
    {
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Vector3> normals = new List<Vector3>();
        private readonly List<Vector2> uvs = new List<Vector2>();
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
