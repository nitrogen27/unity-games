using System;
using System.Collections.Generic;
using System.IO;
using HelloWorldRoom;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace HelloWorldRoom.Editor
{
    public static class WolfRepoLevelBuilder
    {
        private const string ScenePath = "Assets/Scenes/WolfRepoLevel1.unity";
        private const string VerticalRemakeScenePath = "Assets/Scenes/WolfRepoLevel1VerticalRemake.unity";
        private const string DataFolder = "Assets/Data/WolfRepo";
        private const string Level0Path = DataFolder + "/level0.json";
        private const string TextureFolder = "Assets/Textures/WolfRepo";
        private const string MaterialFolder = "Assets/Materials/WolfRepo";

        private const int MapSize = 64;
        private const int AtlasSize = 16;
        private const int StatSpriteOffset = 2;
        private const float Cell = 2f;
        private const float WallHeight = 2f;
        private const float EyeHeight = 0.9f;
        private const float DoorThickness = 0.15f;
        private const float DoorTravel = Cell - 0.02f;

        private static readonly StatInfo[] StatInfos =
        {
            new("puddle", false, null, 0, 0),
            new("greenBarrel", true, null, 2, 1),
            new("tableChairs", true, null, 13, 1),
            new("floorLamp", true, null, 3, 1),
            new("chandelier", false, null, 4, 1),
            new("hangedMan", true, null, 5, 1),
            new("dogFood", false, "food", 10, 3),
            new("pillar", true, null, 8, 1),
            new("tree", true, null, 9, 1),
            new("skeleton", false, null, 10, 1),
            new("sink", true, null, 11, 1),
            new("plant", true, null, 11, 1),
            new("urn", true, null, 12, 1),
            new("bareTable", true, null, 13, 1),
            new("ceilLight", false, null, 0, 2),
            new("pans", false, null, 1, 2),
            new("armor", true, null, 2, 2),
            new("cage", true, null, 3, 2),
            new("cageSkel", true, null, 3, 2),
            new("bonesRelax", false, null, 4, 3),
            new("key1", false, "key1", 5, 3),
            new("key2", false, "key2", 6, 3),
            new("stuff", true, null, 7, 3),
            new("junk", false, null, 4, 3),
            new("food", false, "food", 10, 3),
            new("firstaid", false, "health", 11, 3),
            new("clip", false, "ammo", 12, 3),
            new("machinegun", false, "machinegun", 13, 3),
            new("chaingun", false, "chaingun", 7, 3),
            new("cross", false, "cross", 14, 3),
            new("chalice", false, "chalice", 15, 3),
            new("bible", false, "bible", 0, 4),
            new("crown", false, "crown", 1, 4),
            new("oneUp", false, "oneup", 2, 4),
            new("gibs", false, null, 7, 4),
            new("barrel", true, null, 4, 4),
            new("well", true, null, 5, 4),
            new("emptyWell", true, null, 6, 4),
            new("gibs2", false, null, 7, 4),
            new("flag", true, null, 8, 4),
            new("callApogee", true, null, 0, 5),
            new("junk2", false, null, 4, 3),
            new("junk3", false, null, 4, 3),
            new("junk4", false, null, 4, 3),
            new("pots", false, null, 1, 2),
            new("stove", true, null, 15, 5),
            new("spears", true, null, 0, 5)
        };

        private static readonly Dictionary<string, EnemyInfo> EnemyInfos = new()
        {
            ["guard"] = new EnemyInfo(1.55f, 2.08f, Color.white),
            ["officer"] = new EnemyInfo(1.55f, 2.08f, new Color(0.9f, 0.9f, 1.2f)),
            ["ss"] = new EnemyInfo(1.68f, 2.24f, new Color(0.5f, 0.5f, 0.5f)),
            ["dog"] = new EnemyInfo(1.30f, 1.05f, Color.white),
            ["mutant"] = new EnemyInfo(1.68f, 2.24f, new Color(0.4f, 0.8f, 0.3f)),
            ["boss"] = new EnemyInfo(2.30f, 2.78f, new Color(1.2f, 0.8f, 0.8f))
        };

        [MenuItem("Tools/Wolf Repo/Recreate Level 1 One To One")]
        public static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[WolfRepo] Unity is in Play Mode. Stopping Play Mode; run this menu item again after it exits.");
                EditorApplication.isPlaying = false;
                return;
            }

            EnsureFolders();
            AssetDatabase.Refresh();
            ConfigureImportedTextures();

            WolfRepoLevel level = LoadLevel();
            WolfRepoAssets assets = LoadAssets();
            WolfRepoMaterials materials = new WolfRepoMaterials(assets);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "WolfRepoLevel1";

            ConfigureRenderSettings(level);

            GameObject root = new GameObject("Wolf3D Repo Level 1 - One To One");
            GameObject levelRoot = new GameObject("Level Geometry");
            GameObject doorRoot = new GameObject("Doors");
            GameObject staticsRoot = new GameObject("Statics");
            GameObject enemiesRoot = new GameObject("Enemies");
            levelRoot.transform.SetParent(root.transform);
            doorRoot.transform.SetParent(root.transform);
            staticsRoot.transform.SetParent(root.transform);
            enemiesRoot.transform.SetParent(root.transform);

            BuildFloorAndCeiling(level, materials, levelRoot.transform);
            BuildWalls(level, materials, levelRoot.transform);
            BuildDoors(level, materials, doorRoot.transform);
            BuildStatics(level, materials, staticsRoot.transform);
            BuildEnemies(level, materials, enemiesRoot.transform);
            CreatePlayer(level, materials);
            CreateHud(assets);
            CreateLights();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[WolfRepo] Built '{level.name}' from repo data: {level.walls.Length} tiles, {level.doors.Length} doors, {level.enemies.Length} enemies, {level.statics.Length} statics. Scene saved at {ScenePath}.");
        }

        [MenuItem("Tools/Wolf Repo/Recreate Level 1 Vertical Remake")]
        public static void BuildVerticalRemake()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[WolfRepoRemake] Unity is in Play Mode. Stopping Play Mode; run this menu item again after it exits.");
                EditorApplication.isPlaying = false;
                return;
            }

            EnsureFolders();
            AssetDatabase.Refresh();
            ConfigureImportedTextures();

            WolfRepoLevel level = LoadLevel();
            WolfRepoAssets assets = LoadAssets();
            WolfRepoMaterials materials = new WolfRepoMaterials(assets);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "WolfRepoLevel1VerticalRemake";

            ConfigureRenderSettings(level);
            RenderSettings.ambientLight = new Color(0.42f, 0.42f, 0.45f);

            GameObject root = new GameObject("Wolf3D Repo Level 1 - Vertical Remake");
            GameObject levelRoot = new GameObject("Level Geometry");
            GameObject doorRoot = new GameObject("Doors");
            GameObject staticsRoot = new GameObject("Statics");
            GameObject enemiesRoot = new GameObject("Enemies");
            GameObject verticalRoot = new GameObject("Vertical Remake Layers");
            levelRoot.transform.SetParent(root.transform);
            doorRoot.transform.SetParent(root.transform);
            staticsRoot.transform.SetParent(root.transform);
            enemiesRoot.transform.SetParent(root.transform);
            verticalRoot.transform.SetParent(root.transform);

            BuildRemakeFloorAndCeiling(level, materials, levelRoot.transform);
            BuildWalls(level, materials, levelRoot.transform, true);
            BuildDoors(level, materials, doorRoot.transform, true);
            BuildStatics(level, materials, staticsRoot.transform, true);
            BuildEnemies(level, materials, enemiesRoot.transform, true);
            BuildVerticalRemakeLayers(level, materials, verticalRoot.transform);
            CreatePlayer(level, materials);
            CreateHud(assets);
            CreateLights();
            CreateRemakeLights();

            EditorSceneManager.SaveScene(scene, VerticalRemakeScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(VerticalRemakeScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[WolfRepoRemake] Built vertical remake with basement, upper gallery, floor opening and stair links. Scene saved at {VerticalRemakeScenePath}.");
        }

        private static WolfRepoLevel LoadLevel()
        {
            TextAsset levelAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(Level0Path);
            if (levelAsset == null)
            {
                throw new InvalidOperationException($"Missing {Level0Path}. Copy/export repo maps before building.");
            }

            WolfRepoLevel level = JsonUtility.FromJson<WolfRepoLevel>(levelAsset.text);
            if (level == null || level.walls == null || level.walls.Length != MapSize * MapSize)
            {
                throw new InvalidOperationException($"Invalid Wolf repo level data in {Level0Path}.");
            }

            return level;
        }

        private static void BuildFloorAndCeiling(WolfRepoLevel level, WolfRepoMaterials materials, Transform parent)
        {
            Vector3 center = new Vector3(MapSize * Cell * 0.5f, 0f, MapSize * Cell * 0.5f);
            Vector3 size = new Vector3(MapSize * Cell, 0.06f, MapSize * Cell);

            CreatePrimitiveBox("Floor", center + Vector3.down * 0.03f, size, materials.FloorColor, parent, true);
            CreatePrimitiveBox("Ceiling", center + Vector3.up * (WallHeight + 0.03f), size, materials.CeilingColor, parent, false);
        }

        private static void BuildRemakeFloorAndCeiling(WolfRepoLevel level, WolfRepoMaterials materials, Transform parent)
        {
            int floorTiles = 0;
            int ceilingTiles = 0;
            for (int y = 0; y < MapSize; y++)
            {
                for (int x = 0; x < MapSize; x++)
                {
                    if (level.walls[y * MapSize + x] > 0 || IsVerticalRemakeReservedCell(x, y))
                    {
                        continue;
                    }

                    CreatePrimitiveBox(
                        $"Floor Tile {x:00},{y:00}",
                        GridToWorld(x, y, -0.03f),
                        new Vector3(Cell, 0.06f, Cell),
                        materials.FloorColor,
                        parent,
                        true);
                    CreatePrimitiveBox(
                        $"Ceiling Tile {x:00},{y:00}",
                        GridToWorld(x, y, WallHeight + 0.03f),
                        new Vector3(Cell, 0.06f, Cell),
                        materials.CeilingColor,
                        parent,
                        true);
                    floorTiles++;
                    ceilingTiles++;
                }
            }

            Vector3 center = new Vector3(MapSize * Cell * 0.5f, 0f, MapSize * Cell * 0.5f);
            CreatePrimitiveBox(
                "High Remake Ceiling",
                center + Vector3.up * 6.05f,
                new Vector3(MapSize * Cell, 0.10f, MapSize * Cell),
                materials.CeilingColor,
                parent,
                false);

            Debug.Log($"[WolfRepoRemake] Spawned {floorTiles} floor tiles and {ceilingTiles} low ceiling tiles with vertical openings.");
        }

        private static void BuildWalls(
            WolfRepoLevel level,
            WolfRepoMaterials materials,
            Transform parent,
            bool excludeVerticalReserved = false)
        {
            int sourceWallCells = 0;
            int count = 0;
            for (int y = 0; y < MapSize; y++)
            {
                for (int x = 0; x < MapSize; x++)
                {
                    int wallValue = level.walls[y * MapSize + x];
                    if (wallValue <= 0)
                    {
                        continue;
                    }

                    if (excludeVerticalReserved && IsVerticalRemakeReservedCell(x, y))
                    {
                        continue;
                    }

                    sourceWallCells++;
                    if (!HasExposedSide(level.walls, x, y, excludeVerticalReserved))
                    {
                        continue;
                    }

                    Vector3 position = GridToWorld(x, y, WallHeight * 0.5f);
                    Mesh wallMesh = CreateWallCellMesh(level.walls, x, y, excludeVerticalReserved);
                    GameObject wall = CreateMeshBox($"Wall {x:00},{y:00} Type {wallValue}", position, wallMesh, materials.GetWallSideMaterials(wallValue), parent);
                    BoxCollider collider = wall.AddComponent<BoxCollider>();
                    collider.size = new Vector3(Cell, WallHeight, Cell);
                    wall.isStatic = true;
                    count++;
                }
            }

            Debug.Log($"[WolfRepo] Spawned {count} exposed wall cells from {sourceWallCells} source wall cells.");
        }

        private static void BuildDoors(
            WolfRepoLevel level,
            WolfRepoMaterials materials,
            Transform parent,
            bool excludeVerticalReserved = false)
        {
            Mesh verticalMesh = CreateBoxMesh(new Vector3(DoorThickness, WallHeight, Cell));
            Mesh horizontalMesh = CreateBoxMesh(new Vector3(Cell, WallHeight, DoorThickness));
            int doorCount = 0;

            foreach (WolfDoorData doorData in level.doors)
            {
                if (excludeVerticalReserved && IsVerticalRemakeReservedCell(doorData.x, doorData.y))
                {
                    continue;
                }

                Vector3 position = GridToWorld(doorData.x, doorData.y, WallHeight * 0.5f);
                bool locked = doorData.type == "gold" || doorData.type == "silver";
                Material[] doorMats = materials.GetDoorMaterials(doorData.type, doorData.vertical);
                Mesh mesh = doorData.vertical ? verticalMesh : horizontalMesh;
                GameObject door = CreateMeshBox($"Door {doorData.type} {doorData.x:00},{doorData.y:00}", position, mesh, doorMats, parent);
                BoxCollider collider = door.AddComponent<BoxCollider>();
                collider.size = doorData.vertical
                    ? new Vector3(DoorThickness, WallHeight, Cell)
                    : new Vector3(Cell, WallHeight, DoorThickness);

                WolfDoor wolfDoor = door.AddComponent<WolfDoor>();
                wolfDoor.Configure(doorData.vertical ? Vector3.forward * DoorTravel : Vector3.right * DoorTravel, 2.7f, 5f, locked);
                doorCount++;
            }

            Debug.Log($"[WolfRepo] Spawned {doorCount} doors.");
        }

        private static void BuildStatics(WolfRepoLevel level, WolfRepoMaterials materials, Transform parent, bool excludeVerticalReserved = false)
        {
            foreach (WolfStaticData staticData in level.statics)
            {
                if (excludeVerticalReserved && IsVerticalRemakeReservedCell(staticData.x, staticData.y))
                {
                    continue;
                }

                if (staticData.typeName == "exit")
                {
                    CreateExitMarker(staticData, parent);
                    continue;
                }

                if (staticData.typeIndex < 0 || staticData.typeIndex >= StatInfos.Length)
                {
                    continue;
                }

                StatInfo info = StatInfos[staticData.typeIndex];
                Vector3 position = GridToWorld(staticData.x, staticData.y, 0f);

                if (staticData.typeIndex == 14 || staticData.typeIndex == 4)
                {
                    AddCeilingLamp(info.name, position, staticData.typeIndex == 4, materials, parent);
                    continue;
                }

                if (staticData.typeIndex == 15)
                {
                    continue;
                }

                float scale = info.pickupType == null ? 1.0f : GetPickupScale(info.pickupType);
                Material material = materials.GetStaticMaterial(staticData.typeIndex, info);
                CreateBillboard(
                    $"{info.name} {staticData.x:00},{staticData.y:00}",
                    new Vector3(position.x, 0.02f + scale * 0.6f, position.z),
                    new Vector2(scale, scale * 1.2f),
                    material,
                    parent,
                    info.blocking);
            }

            Debug.Log($"[WolfRepo] Spawned {level.statics.Length} static entries.");
        }

        private static void BuildEnemies(WolfRepoLevel level, WolfRepoMaterials materials, Transform parent, bool excludeVerticalReserved = false)
        {
            foreach (WolfEnemyData enemyData in level.enemies)
            {
                if (excludeVerticalReserved && IsVerticalRemakeReservedCell(enemyData.x, enemyData.y))
                {
                    continue;
                }

                EnemyInfo info = EnemyInfos.TryGetValue(enemyData.type, out EnemyInfo found)
                    ? found
                    : EnemyInfos["guard"];
                Vector3 basePosition = GridToWorld(enemyData.x, enemyData.y, 0f);
                Material material = materials.GetEnemyMaterial(enemyData.type, info.tint);
                CreateBillboard(
                    $"{enemyData.type} {enemyData.x:00},{enemyData.y:00}",
                    new Vector3(basePosition.x, info.height * 0.5f, basePosition.z),
                    new Vector2(info.width, info.height),
                    material,
                    parent,
                    true);
            }

            Debug.Log($"[WolfRepo] Spawned {level.enemies.Length} enemies.");
        }

        private static void CreatePlayer(WolfRepoLevel level, WolfRepoMaterials materials)
        {
            GameObject player = new GameObject("Wolf Repo Player");
            player.transform.position = GridToWorld(level.spawnX, level.spawnY, 0f);
            player.transform.rotation = Quaternion.Euler(0f, 90f + level.spawnAngle, 0f);

            CharacterController characterController = player.AddComponent<CharacterController>();
            characterController.height = 1.45f;
            characterController.radius = 0.30f;
            characterController.center = new Vector3(0f, 0.72f, 0f);
            characterController.stepOffset = 0.22f;
            characterController.slopeLimit = 50f;

            SimpleFirstPersonController controller = player.AddComponent<SimpleFirstPersonController>();
            WolfPlayerInteractor interactor = player.AddComponent<WolfPlayerInteractor>();

            GameObject cameraObject = new GameObject("First Person Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(player.transform);
            cameraObject.transform.localPosition = new Vector3(0f, EyeHeight, 0f);
            cameraObject.transform.localRotation = Quaternion.identity;

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 75f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 200f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = materials.CeilingColor.color;
            cameraObject.AddComponent<AudioListener>();

            Light playerLight = cameraObject.AddComponent<Light>();
            playerLight.type = LightType.Point;
            playerLight.color = new Color(1f, 0.93f, 0.82f);
            playerLight.intensity = 0.35f;
            playerLight.range = 10f;

            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("playerCamera").objectReferenceValue = camera;
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject serializedInteractor = new SerializedObject(interactor);
            serializedInteractor.FindProperty("playerCamera").objectReferenceValue = camera;
            serializedInteractor.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateHud(WolfRepoAssets assets)
        {
            GameObject hud = new GameObject("Wolf Repo HUD");
            WolfHud wolfHud = hud.AddComponent<WolfHud>();

            SerializedObject serializedHud = new SerializedObject(wolfHud);
            serializedHud.FindProperty("faceTexture").objectReferenceValue = assets.Bj;
            serializedHud.FindProperty("weaponTexture").objectReferenceValue = assets.Attack;
            serializedHud.FindProperty("keyTexture").objectReferenceValue = assets.HudKeys;
            serializedHud.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateLights()
        {
            GameObject directional = new GameObject("Repo Directional Light");
            Light light = directional.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = Color.white;
            light.intensity = 0.30f;
            directional.transform.rotation = Quaternion.Euler(55f, -45f, 0f);
        }

        private static void CreateRemakeLights()
        {
            // Basement — bright central ceiling lamp + four warm wall sconces + a cool
            // back fill so the room reads as enclosed and lit, not a scary pit.
            CreatePointLight("Basement Ceiling Light",   new Vector3(69f,   -0.40f, 22f),    new Color(1.00f, 0.92f, 0.78f), 4.5f, 12f);
            CreatePointLight("Basement Sconce West",     new Vector3(63.5f, -1.00f, 22f),    new Color(1.00f, 0.78f, 0.42f), 1.6f, 6f);
            CreatePointLight("Basement Sconce East",     new Vector3(74.5f, -1.00f, 22f),    new Color(1.00f, 0.78f, 0.42f), 1.6f, 6f);
            CreatePointLight("Basement Sconce North",    new Vector3(69f,   -1.00f, 27.4f),  new Color(1.00f, 0.78f, 0.42f), 1.6f, 6f);
            CreatePointLight("Basement Sconce South",    new Vector3(69f,   -1.00f, 16.6f),  new Color(1.00f, 0.86f, 0.55f), 1.8f, 7f);
            CreatePointLight("Basement Cool Fill",       new Vector3(69f,   -2.20f, 22f),    new Color(0.55f, 0.70f, 1.00f), 0.9f, 9f);

            // Down-stair lighting — top landing + bottom landing.
            CreatePointLight("Down Stair Top Light",     new Vector3(69f,    0.10f,  9.6f),  new Color(1.00f, 0.86f, 0.55f), 2.0f, 6f);
            CreatePointLight("Down Stair Bottom Light",  new Vector3(69f,   -2.65f, 16.2f),  new Color(1.00f, 0.86f, 0.55f), 2.2f, 7f);

            // Up-stair lighting — top of the gallery stair + bottom near main hall.
            CreatePointLight("Up Stair Bottom Light",    new Vector3(54.7f,  0.20f, 59f),    new Color(1.00f, 0.86f, 0.55f), 2.0f, 6f);
            CreatePointLight("Up Stair Top Light",       new Vector3(63.0f,  3.20f, 59f),    new Color(1.00f, 0.86f, 0.55f), 2.0f, 7f);

            // Upper gallery — kept cool blue accent + a neutral lift so the deck
            // isn't too dark relative to the warmly-lit stair.
            CreatePointLight("Upper Gallery Blue Light", new Vector3(77f,    4.80f, 62f),    new Color(0.45f, 0.62f, 1.00f), 2.4f, 14f);
            CreatePointLight("Upper Gallery Fill",       new Vector3(70f,    3.80f, 62f),    new Color(1.00f, 0.95f, 0.85f), 1.4f, 12f);
        }

        private static void CreatePointLight(string name, Vector3 position, Color color, float intensity, float range)
        {
            GameObject lightObject = new GameObject(name);
            lightObject.transform.position = position;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
        }

        private static void BuildVerticalRemakeLayers(WolfRepoLevel level, WolfRepoMaterials materials, Transform parent)
        {
            Material upperWall = materials.GetWallSideMaterials(8)[2];

            BuildBasementLayer(parent, materials);
            BuildUpperGallery(parent, materials, upperWall, materials.StairRail);
            BuildStairRuns(parent, materials);
        }

        private static void BuildBasementLayer(Transform parent, WolfRepoMaterials materials)
        {
            const float floorY = -3.05f;
            const float wallCenterY = -1.55f;
            const float wallHeight = 3.0f;
            const float ceilingY = -0.10f;   // flat ceiling just under main-level floor
            const float centerX = 69f;
            const float centerZ = 22f;

            CreatePrimitiveBox("Basement Floor",   new Vector3(centerX, floorY,   centerZ), new Vector3(12f, 0.10f, 12f), materials.FloorColor,   parent, true);
            CreatePrimitiveBox("Basement Ceiling", new Vector3(centerX, ceilingY, centerZ), new Vector3(12f, 0.10f, 12f), materials.CeilingColor, parent, false);

            CreatePrimitiveBox("Basement Back Wall Left",   new Vector3(65.5f, wallCenterY, 28f), new Vector3(5f, wallHeight, 0.24f), materials.BasementWall, parent, true);
            CreatePrimitiveBox("Basement Back Wall Right",  new Vector3(72.5f, wallCenterY, 28f), new Vector3(5f, wallHeight, 0.24f), materials.BasementWall, parent, true);
            CreatePrimitiveBox("Basement Left Wall",        new Vector3(63f,     wallCenterY, centerZ), new Vector3(0.24f, wallHeight, 12f), materials.BasementWall, parent, true);
            CreatePrimitiveBox("Basement Right Wall",       new Vector3(75f,     wallCenterY, centerZ), new Vector3(0.24f, wallHeight, 12f), materials.BasementWall, parent, true);
            CreatePrimitiveBox("Basement Front Wall Left",  new Vector3(64.5f,   wallCenterY, 16f), new Vector3(3f, wallHeight, 0.24f), materials.BasementWall, parent, true);
            CreatePrimitiveBox("Basement Front Wall Right", new Vector3(73.5f,   wallCenterY, 16f), new Vector3(3f, wallHeight, 0.24f), materials.BasementWall, parent, true);
            CreatePrimitiveBox("Basement North Door Header", new Vector3(centerX, -0.50f, 28f), new Vector3(2.1f, 0.90f, 0.24f), materials.BasementWall, parent, true);
            CreateBasementDoor("Basement Door North", new Vector3(centerX, -2f, 28f), false, materials, parent);
            CreatePrimitiveBox("Basement North Door Threshold", new Vector3(centerX, floorY + 0.07f, 28f), new Vector3(2.15f, 0.06f, 0.48f), materials.StairStone, parent, true);

            // A small blue-stone room chain behind the basement door keeps the lower
            // level in the same visual language as the original map rooms.
            CreatePrimitiveBox("Basement North Room Floor",   new Vector3(centerX, floorY,   32f), new Vector3(8f, 0.10f, 8f), materials.FloorColor,   parent, true);
            CreatePrimitiveBox("Basement North Room Ceiling", new Vector3(centerX, ceilingY, 32f), new Vector3(8f, 0.10f, 8f), materials.CeilingColor, parent, false);
            CreatePrimitiveBox("Basement North Room West Wall",       new Vector3(65f, wallCenterY, 32f),   new Vector3(0.24f, wallHeight, 8f), materials.BasementWall, parent, true);
            CreatePrimitiveBox("Basement North Room Back Wall",       new Vector3(centerX, wallCenterY, 36f), new Vector3(8f, wallHeight, 0.24f), materials.BasementWall, parent, true);
            CreatePrimitiveBox("Basement North Room East Wall South", new Vector3(73f, wallCenterY, 29.5f), new Vector3(0.24f, wallHeight, 3f), materials.BasementWall, parent, true);
            CreatePrimitiveBox("Basement North Room East Wall North", new Vector3(73f, wallCenterY, 34.5f), new Vector3(0.24f, wallHeight, 3f), materials.BasementWall, parent, true);
            CreatePrimitiveBox("Basement East Door Header", new Vector3(73f, -0.50f, 32f), new Vector3(0.24f, 0.90f, 2.1f), materials.BasementWall, parent, true);
            CreateBasementDoor("Basement Door East", new Vector3(73f, -2f, 32f), true, materials, parent);
            CreatePrimitiveBox("Basement East Door Threshold", new Vector3(73f, floorY + 0.07f, 32f), new Vector3(0.48f, 0.06f, 2.15f), materials.StairStone, parent, true);

            CreatePrimitiveBox("Basement East Room Floor",   new Vector3(77f, floorY,   32f), new Vector3(8f, 0.10f, 6f), materials.FloorColor,   parent, true);
            CreatePrimitiveBox("Basement East Room Ceiling", new Vector3(77f, ceilingY, 32f), new Vector3(8f, 0.10f, 6f), materials.CeilingColor, parent, false);
            CreatePrimitiveBox("Basement East Room Front Wall", new Vector3(77f, wallCenterY, 29f), new Vector3(8f, wallHeight, 0.24f), materials.BasementWall, parent, true);
            CreatePrimitiveBox("Basement East Room Back Wall",  new Vector3(77f, wallCenterY, 35f), new Vector3(8f, wallHeight, 0.24f), materials.BasementWall, parent, true);
            CreatePrimitiveBox("Basement East Room East Wall",  new Vector3(81f, wallCenterY, 32f), new Vector3(0.24f, wallHeight, 6f), materials.BasementWall, parent, true);

            // Cyan trim borrows the elevator/door accent color from the original set.
            const float trimY = -2.20f;
            const float trimT = 0.06f;
            CreatePrimitiveBox("Basement Trim Back Left",   new Vector3(65.5f, trimY, 27.92f), new Vector3(4.7f, 0.10f, trimT), materials.StairTrim, parent, false);
            CreatePrimitiveBox("Basement Trim Back Right",  new Vector3(72.5f, trimY, 27.92f), new Vector3(4.7f, 0.10f, trimT), materials.StairTrim, parent, false);
            CreatePrimitiveBox("Basement Trim Left",        new Vector3(63.08f,  trimY, centerZ), new Vector3(trimT, 0.10f, 11.6f), materials.StairTrim, parent, false);
            CreatePrimitiveBox("Basement Trim Right",       new Vector3(74.92f,  trimY, centerZ), new Vector3(trimT, 0.10f, 11.6f), materials.StairTrim, parent, false);
            CreatePrimitiveBox("Basement Trim Front Left",  new Vector3(64.5f,   trimY, 16.08f), new Vector3(3f,    0.10f, trimT), materials.StairTrim, parent, false);
            CreatePrimitiveBox("Basement Trim Front Right", new Vector3(73.5f,   trimY, 16.08f), new Vector3(3f,    0.10f, trimT), materials.StairTrim, parent, false);
            CreatePrimitiveBox("Basement North Room Back Trim", new Vector3(centerX, trimY, 35.92f), new Vector3(7.6f, 0.10f, trimT), materials.StairTrim, parent, false);
            CreatePrimitiveBox("Basement East Room East Trim",  new Vector3(80.92f, trimY, 32f),     new Vector3(trimT, 0.10f, 5.6f), materials.StairTrim, parent, false);

            CreatePrimitiveBox("Basement Floor Plate", new Vector3(centerX, floorY + 0.06f, 22f), new Vector3(2.6f, 0.04f, 2.6f), materials.StairRiser, parent, false);

            CreateBasementCeilingLamp(parent, materials, new Vector3(centerX, ceilingY - 0.08f, centerZ), "Main");
            CreateBasementCeilingLamp(parent, materials, new Vector3(centerX, ceilingY - 0.08f, 32f), "North Room");
            CreateBasementCeilingLamp(parent, materials, new Vector3(77f, ceilingY - 0.08f, 32f), "East Room");
            CreateBasementWallSconces(parent, materials, wallCenterY);
            CreateHoleTrim(parent, materials);
        }

        private static void CreateBasementDoor(string name, Vector3 position, bool vertical, WolfRepoMaterials materials, Transform parent)
        {
            Vector3 size = vertical
                ? new Vector3(DoorThickness, WallHeight, Cell)
                : new Vector3(Cell, WallHeight, DoorThickness);
            Mesh mesh = CreateBoxMesh(size);
            GameObject door = CreateMeshBox(name, position, mesh, materials.GetDoorMaterials("normal", vertical), parent);
            BoxCollider collider = door.AddComponent<BoxCollider>();
            collider.size = size;

            WolfDoor wolfDoor = door.AddComponent<WolfDoor>();
            wolfDoor.Configure(vertical ? Vector3.forward * DoorTravel : Vector3.right * DoorTravel, 2.7f, 5f, false);
        }

        private static void CreateBasementCeilingLamp(Transform parent, WolfRepoMaterials materials, Vector3 capPos, string label)
        {
            GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cap.name = $"Basement {label} Ceiling Lamp Cap";
            cap.transform.position = capPos;
            cap.transform.localScale = new Vector3(0.55f, 0.05f, 0.55f);
            cap.transform.SetParent(parent, true);
            cap.GetComponent<Renderer>().sharedMaterial = materials.LampWarmCap;
            UnityEngine.Object.DestroyImmediate(cap.GetComponent<Collider>());

            GameObject bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulb.name = $"Basement {label} Ceiling Lamp Bulb";
            bulb.transform.position = capPos + Vector3.down * 0.18f;
            bulb.transform.localScale = Vector3.one * 0.28f;
            bulb.transform.SetParent(parent, true);
            bulb.GetComponent<Renderer>().sharedMaterial = materials.StairLampBulb;
            UnityEngine.Object.DestroyImmediate(bulb.GetComponent<Collider>());
        }

        private static void CreateBasementWallSconces(Transform parent, WolfRepoMaterials materials, float wallCenterY)
        {
            // Four wall sconces — small bright bulbs near the ceiling on each wall mid-span.
            Vector3[] positions =
            {
                new Vector3(63.18f, wallCenterY + 0.55f, 22f),
                new Vector3(74.82f, wallCenterY + 0.55f, 22f),
                new Vector3(66f,    wallCenterY + 0.55f, 27.82f),
                new Vector3(72f,    wallCenterY + 0.55f, 27.82f),
                new Vector3(69f,    wallCenterY + 0.55f, 16.18f),
                new Vector3(69f,    wallCenterY + 0.55f, 35.82f),
                new Vector3(80.82f, wallCenterY + 0.55f, 32f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                GameObject sconce = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sconce.name = $"Basement Sconce {i + 1}";
                sconce.transform.position = positions[i];
                sconce.transform.localScale = Vector3.one * 0.22f;
                sconce.transform.SetParent(parent, true);
                sconce.GetComponent<Renderer>().sharedMaterial = materials.StairLampBulb;
                UnityEngine.Object.DestroyImmediate(sconce.GetComponent<Collider>());
            }
        }

        private static void BuildUpperGallery(Transform parent, WolfRepoMaterials materials, Material wall, Material rail)
        {
            const float minX = 50f;
            const float maxX = 92f;
            const float minZ = 48f;
            const float maxZ = 74f;
            const float centerX = (minX + maxX) * 0.5f;
            const float centerZ = (minZ + maxZ) * 0.5f;
            const float width = maxX - minX;
            const float depth = maxZ - minZ;
            const float wallHeight = 6f;
            const float wallY = wallHeight * 0.5f;
            const float deckTopY = 3.0f;
            const float deckBottomY = WallHeight + 0.04f;
            const float deckThickness = deckTopY - deckBottomY;
            const float deckCenterY = deckBottomY + deckThickness * 0.5f;
            const float stairCenterZ = 59f;
            const float stairNorthZ = 57.55f;
            const float stairSouthZ = 60.45f;
            const float deckMinX = 63.2f;
            float deckWidth = maxX - deckMinX;
            float deckCenterX = deckMinX + deckWidth * 0.5f;

            CreateSlabBox("Sealed Upper Stair Hub Lower Floor", new Vector3(centerX, -0.03f, centerZ), new Vector3(width, 0.06f, depth), materials.FloorColor, materials.CeilingColor, materials.BasementWall, parent, true);
            CreateSlabBox("Sealed Upper Stair Hub Ceiling", new Vector3(centerX, 6.04f, centerZ), new Vector3(width, 0.10f, depth), materials.CeilingColor, materials.CeilingColor, materials.BasementWall, parent, false);

            CreatePrimitiveBox("Sealed Upper Stair Hub North Wall", new Vector3(centerX, wallY, minZ), new Vector3(width, wallHeight, 0.24f), materials.BasementWall, parent, true);
            CreatePrimitiveBox("Sealed Upper Stair Hub South Wall", new Vector3(centerX, wallY, maxZ), new Vector3(width, wallHeight, 0.24f), materials.BasementWall, parent, true);

            float westNorthDepth = stairNorthZ - minZ;
            float westSouthDepth = maxZ - stairSouthZ;
            CreatePrimitiveBox("Sealed Upper Stair Hub West North Wall", new Vector3(minX, wallY, minZ + westNorthDepth * 0.5f), new Vector3(0.26f, wallHeight, westNorthDepth), materials.BasementWall, parent, true);
            CreatePrimitiveBox("Sealed Upper Stair Hub West South Wall", new Vector3(minX, wallY, stairSouthZ + westSouthDepth * 0.5f), new Vector3(0.26f, wallHeight, westSouthDepth), materials.BasementWall, parent, true);
            CreatePrimitiveBox("Sealed Upper Stair Hub West Header", new Vector3(minX, 4.50f, stairCenterZ), new Vector3(0.26f, 3.00f, stairSouthZ - stairNorthZ), materials.BasementWall, parent, true);

            CreatePrimitiveBox("Sealed Upper Stair Hub East North Wall", new Vector3(maxX, wallY, 53.2f), new Vector3(0.26f, wallHeight, 10.4f), materials.BasementWall, parent, true);
            CreatePrimitiveBox("Sealed Upper Stair Hub East South Wall", new Vector3(maxX, wallY, 68.8f), new Vector3(0.26f, wallHeight, 10.4f), materials.BasementWall, parent, true);
            CreatePrimitiveBox("Sealed Upper Stair Hub East Header", new Vector3(maxX, 4.50f, stairCenterZ), new Vector3(0.26f, 3.00f, 2.60f), materials.BasementWall, parent, true);
            CreateBasementDoor("Sealed Upper Stair Hub East Door", new Vector3(maxX, WallHeight * 0.5f, stairCenterZ), true, materials, parent);

            float stairWallCenterX = (minX + deckMinX) * 0.5f;
            float stairWallLength = deckMinX - minX;
            CreatePrimitiveBox("Sealed Upper Stair North Corridor Wall", new Vector3(stairWallCenterX, wallY, stairNorthZ), new Vector3(stairWallLength, wallHeight, 0.26f), materials.BasementWall, parent, true);
            CreatePrimitiveBox("Sealed Upper Stair South Corridor Wall", new Vector3(stairWallCenterX, wallY, stairSouthZ), new Vector3(stairWallLength, wallHeight, 0.26f), materials.BasementWall, parent, true);

            float landingNorthDepth = stairNorthZ - minZ;
            float landingSouthDepth = maxZ - stairSouthZ;
            CreatePrimitiveBox("Sealed Upper Stair Landing North Face", new Vector3(deckMinX, wallY, minZ + landingNorthDepth * 0.5f), new Vector3(0.26f, wallHeight, landingNorthDepth), materials.BasementWall, parent, true);
            CreatePrimitiveBox("Sealed Upper Stair Landing South Face", new Vector3(deckMinX, wallY, stairSouthZ + landingSouthDepth * 0.5f), new Vector3(0.26f, wallHeight, landingSouthDepth), materials.BasementWall, parent, true);
            CreatePrimitiveBox("Sealed Upper Stair Landing Header", new Vector3(deckMinX, 4.50f, stairCenterZ), new Vector3(0.26f, 3.00f, stairSouthZ - stairNorthZ), materials.BasementWall, parent, true);

            CreateSlabBox("Sealed Upper Stair Hub Upper Deck", new Vector3(deckCenterX, deckCenterY, centerZ), new Vector3(deckWidth, deckThickness, depth), materials.FloorColor, materials.CeilingColor, materials.BasementWall, parent, true);
            CreatePrimitiveBox("Sealed Upper Deck North Rail", new Vector3(deckCenterX, 3.55f, stairNorthZ), new Vector3(deckWidth - 0.4f, 1.1f, 0.16f), rail, parent, true);
            CreatePrimitiveBox("Sealed Upper Deck South Rail", new Vector3(deckCenterX, 3.55f, stairSouthZ), new Vector3(deckWidth - 0.4f, 1.1f, 0.16f), rail, parent, true);

            Mesh panelMesh = CreateBoxMesh(new Vector3(0.08f, 1.55f, 1.25f));
            CreateMeshBox("Sealed Upper Stair Left Cyan Panel", new Vector3(49.86f, 1.45f, 54.5f), panelMesh, materials.GetDoorMaterials("elevator", true), parent);
            CreateMeshBox("Sealed Upper Stair Right Cyan Panel", new Vector3(49.86f, 1.45f, 63.5f), panelMesh, materials.GetDoorMaterials("elevator", true), parent);
            CreatePrimitiveBox("Sealed Upper Stair Left Switch", new Vector3(49.80f, 0.86f, 56.55f), new Vector3(0.03f, 0.34f, 0.18f), materials.StairTrim, parent, false);
            CreatePrimitiveBox("Sealed Upper Stair Right Switch", new Vector3(49.80f, 0.86f, 61.45f), new Vector3(0.03f, 0.34f, 0.18f), materials.StairTrim, parent, false);

            CreateStairBulb(parent, materials, new Vector3(71f, 5.72f, 61f), 0.20f);
            CreateStairBulb(parent, materials, new Vector3(58f, 5.72f, 59f), 0.18f);
        }

        private static void CreateLowerStairVoidBlockers(Transform parent, WolfRepoMaterials materials)
        {
            const float minZ = 48f;
            const float maxZ = 74f;
            const float openingNorthZ = 57.55f;
            const float openingSouthZ = 60.45f;
            const float centerZ = 59f;
            const float wallHeight = 6f;
            const float wallY = wallHeight * 0.5f;
            const float facadeThickness = 0.26f;
            const float frontX = 54.25f;
            const float backX = 65.00f;
            const float maxX = 92f;

            CreateLowStairFacade("Upper Stair Lower Front", frontX, parent, materials.BasementWall, minZ, maxZ, centerZ, openingNorthZ, openingSouthZ, facadeThickness, wallHeight, wallY);
            CreateLowStairFacade("Upper Stair Lower Back", backX, parent, materials.BasementWall, minZ, maxZ, centerZ, openingNorthZ, openingSouthZ, facadeThickness, wallHeight, wallY);

            float sideCenterX = (backX + maxX) * 0.5f;
            float sideLength = maxX - backX;
            CreatePrimitiveBox(
                "Upper Stair Lower North Void Blocker",
                new Vector3(sideCenterX, WallHeight * 0.5f, openingNorthZ),
                new Vector3(sideLength, WallHeight, 0.20f),
                materials.BasementWall,
                parent,
                true);
            CreatePrimitiveBox(
                "Upper Stair Lower South Void Blocker",
                new Vector3(sideCenterX, WallHeight * 0.5f, openingSouthZ),
                new Vector3(sideLength, WallHeight, 0.20f),
                materials.BasementWall,
                parent,
                true);
        }

        private static void CreateLowStairFacade(
            string prefix,
            float x,
            Transform parent,
            Material wall,
            float minZ,
            float maxZ,
            float centerZ,
            float openingNorthZ,
            float openingSouthZ,
            float thickness,
            float wallHeight,
            float wallY)
        {
            float northDepth = openingNorthZ - minZ;
            float southDepth = maxZ - openingSouthZ;
            CreatePrimitiveBox(
                $"{prefix} North Wall",
                new Vector3(x, wallY, minZ + northDepth * 0.5f),
                new Vector3(thickness, wallHeight, northDepth),
                wall,
                parent,
                true);
            CreatePrimitiveBox(
                $"{prefix} South Wall",
                new Vector3(x, wallY, openingSouthZ + southDepth * 0.5f),
                new Vector3(thickness, wallHeight, southDepth),
                wall,
                parent,
                true);
            CreatePrimitiveBox(
                $"{prefix} Header",
                new Vector3(x, 4.55f, centerZ),
                new Vector3(thickness, 2.90f, openingSouthZ - openingNorthZ),
                wall,
                parent,
                true);
        }

        private static void BuildStairRuns(Transform parent, WolfRepoMaterials materials)
        {
            CreateDownStairs(parent, materials);
            CreateUpStairs(parent, materials);
        }

        private static void CreateDownStairs(Transform parent, WolfRepoMaterials materials)
        {
            // Slim open stair: every tread is exposed, with an explicit dark riser
            // face so the descent reads from the top of the opening.
            const int steps = 18;
            const float totalDrop = 3f;
            const float stepDrop = totalDrop / steps;          // 0.1667 m per step
            const float run = 0.32f;
            const float startZ = 10.18f;
            const float centerX = 69f;
            const float width = 1.46f;

            for (int i = 1; i <= steps; i++)
            {
                float topY = -i * stepDrop;
                float centerY = topY - stepDrop * 0.5f;
                float centerZ = startZ + (i - 0.5f) * run;
                CreatePrimitiveBox(
                    $"Basement Stair Down {i:00}",
                    new Vector3(centerX, centerY, centerZ),
                    new Vector3(width, stepDrop + 0.01f, run + 0.01f),
                    materials.StairStone,
                    parent,
                    true);
                CreatePrimitiveBox(
                    $"Basement Stair Nose {i:00}",
                    new Vector3(centerX, topY + 0.012f, centerZ - run * 0.42f),
                    new Vector3(width + 0.08f, 0.025f, 0.035f),
                    materials.StairRiser,
                    parent,
                    false);
                CreatePrimitiveBox(
                    $"Basement Stair Riser Face {i:00}",
                    new Vector3(centerX, topY - stepDrop * 0.5f, centerZ - run * 0.5f - 0.012f),
                    new Vector3(width + 0.04f, stepDrop + 0.015f, 0.035f),
                    materials.StairRiser,
                    parent,
                    false);
            }

            float stringerCenterZ = startZ + steps * run * 0.5f;
            float stringerLength = steps * run + 0.04f;

            // Side shaft walls — widened to 0.27 so they bridge from the hole edge
            // (x=68 / x=70) all the way to the step's outer edge (x=68.27 / 69.73),
            // leaving zero gap for the player's CharacterController to slip through.
            // Top is raised to y=0 (corridor floor level) so walking from corridor
            // onto the wall top is a flat transition, then a clean 0.17 stepDown
            // onto the first stair cap is well within stepOffset (0.22).
            CreatePrimitiveBox("Basement Stair West Shaft Wall", new Vector3(68.135f, -1.50f, stringerCenterZ), new Vector3(0.27f, 3.00f, stringerLength + 0.04f), materials.BasementWall, parent, true);
            CreatePrimitiveBox("Basement Stair East Shaft Wall", new Vector3(69.865f, -1.50f, stringerCenterZ), new Vector3(0.27f, 3.00f, stringerLength + 0.04f), materials.BasementWall, parent, true);
            // Front shaft wall — closes the corridor-side of the shaft below floor
            // level only (y=-3 to y=0), so the player still walks onto the staircase
            // unobstructed but doesn't see void through the front edge of the hole.
            CreatePrimitiveBox("Basement Stair Front Shaft Wall", new Vector3(centerX, -1.50f, 10.08f), new Vector3(2.0f, 3.00f, 0.20f), materials.BasementWall, parent, true);

            // Hand rails along both sides — slim, dark, follow the stair slope at
            // a constant offset so they read as a single straight line.
            CreatePrimitiveBox("Basement Stair Left Rail Top",  new Vector3(centerX - 0.78f, -0.55f, stringerCenterZ), new Vector3(0.06f, 0.06f, stringerLength), materials.StairRail, parent, false);
            CreatePrimitiveBox("Basement Stair Right Rail Top", new Vector3(centerX + 0.78f, -0.55f, stringerCenterZ), new Vector3(0.06f, 0.06f, stringerLength), materials.StairRail, parent, false);
            CreatePrimitiveBox("Basement Stair Left Rail Mid",  new Vector3(centerX - 0.78f, -0.95f, stringerCenterZ), new Vector3(0.04f, 0.04f, stringerLength), materials.StairRail, parent, false);
            CreatePrimitiveBox("Basement Stair Right Rail Mid", new Vector3(centerX + 0.78f, -0.95f, stringerCenterZ), new Vector3(0.04f, 0.04f, stringerLength), materials.StairRail, parent, false);

            // Warm bulb at the top of the stairs and another at the landing.
            CreateStairBulb(parent, materials, new Vector3(centerX, 0.28f, startZ - 0.10f), 0.16f);
            CreateStairBulb(parent, materials, new Vector3(centerX, -2.78f, startZ + steps * run + 0.10f), 0.18f);

            CreatePrimitiveBox("Basement Stair Top Stone Threshold", new Vector3(centerX, 0.045f, startZ - 0.20f), new Vector3(width + 0.34f, 0.09f, 0.24f), materials.StairRiser, parent, true);
            CreatePrimitiveBox("Basement Stair Bottom Landing", new Vector3(centerX, -2.99f, startZ + steps * run + 0.46f), new Vector3(width + 0.30f, 0.08f, 0.90f), materials.StairStone, parent, true);
            CreatePrimitiveBox("Basement Stair Entry West Stone Pier", new Vector3(centerX - 0.98f, 0.18f, startZ - 0.05f), new Vector3(0.18f, 0.36f, 0.44f), materials.BasementWall, parent, true);
            CreatePrimitiveBox("Basement Stair Entry East Stone Pier", new Vector3(centerX + 0.98f, 0.18f, startZ - 0.05f), new Vector3(0.18f, 0.36f, 0.44f), materials.BasementWall, parent, true);
        }

        private static void CreateUpStairs(Transform parent, WolfRepoMaterials materials)
        {
            const int steps = 18;
            const float totalRise = 3f;
            const float stepRise = totalRise / steps;
            const float run = 0.43f;
            const float startX = 54.9f;
            const float centerZ = 59f;
            const float width = 2.05f;

            for (int i = 1; i <= steps; i++)
            {
                float topY = i * stepRise;
                float centerY = topY - stepRise * 0.5f;
                float centerX = startX + (i - 0.5f) * run;
                CreatePrimitiveBox(
                    $"Upper Gallery Stair Up {i:00}",
                    new Vector3(centerX, centerY, centerZ),
                    new Vector3(run + 0.01f, stepRise + 0.01f, width),
                    materials.StairStone,
                    parent,
                    true);
            }

            float stringerCenterX = startX + steps * run * 0.5f;
            float stringerLength = steps * run + 0.04f;
            CreatePrimitiveBox(
                "Upper Stair Entrance Stone Threshold",
                new Vector3(startX - 0.26f, 0.045f, centerZ),
                new Vector3(0.68f, 0.09f, width + 0.46f),
                materials.StairStone,
                parent,
                true);
            CreatePrimitiveBox(
                "Upper Stair Top Landing Stone",
                new Vector3(startX + steps * run + 0.28f, 2.99f, centerZ),
                new Vector3(0.72f, 0.08f, width + 0.42f),
                materials.StairStone,
                parent,
                true);
            CreatePrimitiveBox("Upper Stair North Handrail Top", new Vector3(stringerCenterX, 2.05f, centerZ - 0.72f), new Vector3(stringerLength, 0.06f, 0.06f), materials.WoodDark, parent, false);
            CreatePrimitiveBox("Upper Stair South Handrail Top", new Vector3(stringerCenterX, 2.05f, centerZ + 0.72f), new Vector3(stringerLength, 0.06f, 0.06f), materials.WoodDark, parent, false);
            CreatePrimitiveBox("Upper Stair North Handrail Mid", new Vector3(stringerCenterX, 1.65f, centerZ - 0.72f), new Vector3(stringerLength, 0.04f, 0.04f), materials.Wood, parent, false);
            CreatePrimitiveBox("Upper Stair South Handrail Mid", new Vector3(stringerCenterX, 1.65f, centerZ + 0.72f), new Vector3(stringerLength, 0.04f, 0.04f), materials.Wood, parent, false);

            CreateStairBulb(parent, materials, new Vector3(startX - 0.10f, 0.32f, centerZ), 0.16f);
            CreateStairBulb(parent, materials, new Vector3(startX + steps * run + 0.10f, 3.30f, centerZ), 0.18f);
        }

        private static void CreateStairBulb(Transform parent, WolfRepoMaterials materials, Vector3 position, float scale)
        {
            GameObject bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulb.name = $"Stair Lamp {position.x:0}_{position.y:0}_{position.z:0}";
            bulb.transform.position = position;
            bulb.transform.localScale = Vector3.one * scale;
            bulb.transform.SetParent(parent, true);
            bulb.GetComponent<Renderer>().sharedMaterial = materials.StairLampBulb;
            UnityEngine.Object.DestroyImmediate(bulb.GetComponent<Collider>());
        }

        private static void CreateHoleTrim(Transform parent, WolfRepoMaterials materials)
        {
            // Hole bounds. Stair entry on the front side (zFront), so the balustrade
            // closes the west, east and back sides; the front gets a floor lip only.
            const float xWest = 68f;
            const float xEast = 70f;
            const float zFront = 10f;
            const float zBack = 16f;
            const float zCenter = (zFront + zBack) * 0.5f;
            const float zSpan = zBack - zFront;
            const float postCenterY = 0.50f;
            const float postHeight = 1.00f;
            const float topRailY = 1.00f;

            // Floor-level lip — wraps the four edges of the opening at corridor height.
            CreatePrimitiveBox("Hole Lip North", new Vector3(69f, -0.02f, zFront),  new Vector3(2.20f, 0.04f, 0.16f), materials.WoodDark, parent, false);
            CreatePrimitiveBox("Hole Lip South", new Vector3(69f, -0.02f, zBack),   new Vector3(2.20f, 0.04f, 0.16f), materials.WoodDark, parent, false);
            CreatePrimitiveBox("Hole Lip West",  new Vector3(xWest, -0.02f, zCenter), new Vector3(0.16f, 0.04f, zSpan), materials.WoodDark, parent, false);
            CreatePrimitiveBox("Hole Lip East",  new Vector3(xEast, -0.02f, zCenter), new Vector3(0.16f, 0.04f, zSpan), materials.WoodDark, parent, false);

            // Four chunky corner posts — the visual anchor of the balustrade.
            Vector3[] cornerPositions =
            {
                new Vector3(xWest, postCenterY, zFront),
                new Vector3(xEast, postCenterY, zFront),
                new Vector3(xWest, postCenterY, zBack),
                new Vector3(xEast, postCenterY, zBack)
            };
            for (int i = 0; i < cornerPositions.Length; i++)
            {
                CreatePrimitiveBox($"Hole Corner Post {i + 1}", cornerPositions[i], new Vector3(0.20f, postHeight, 0.20f), materials.WoodDark, parent, true);
                // Brass cap on each post.
                CreatePrimitiveBox($"Hole Corner Cap {i + 1}", cornerPositions[i] + Vector3.up * (postHeight * 0.5f + 0.05f), new Vector3(0.24f, 0.06f, 0.24f), materials.Brass, parent, false);
            }

            // Six balusters per long side — slim wood spindles between the corner posts.
            const int balusterCount = 6;
            const float balusterSpacing = zSpan / (balusterCount + 1);
            for (int i = 1; i <= balusterCount; i++)
            {
                float z = zFront + i * balusterSpacing;
                CreatePrimitiveBox($"Hole West Baluster {i:00}", new Vector3(xWest, 0.40f, z), new Vector3(0.06f, 0.80f, 0.06f), materials.Wood, parent, false);
                CreatePrimitiveBox($"Hole East Baluster {i:00}", new Vector3(xEast, 0.40f, z), new Vector3(0.06f, 0.80f, 0.06f), materials.Wood, parent, false);
            }

            // Top handrails on three sides (front is open for the stair entry).
            CreatePrimitiveBox("Hole West Handrail", new Vector3(xWest, topRailY, zCenter), new Vector3(0.10f, 0.08f, zSpan + 0.20f), materials.WoodDark, parent, false);
            CreatePrimitiveBox("Hole East Handrail", new Vector3(xEast, topRailY, zCenter), new Vector3(0.10f, 0.08f, zSpan + 0.20f), materials.WoodDark, parent, false);
            CreatePrimitiveBox("Hole Back Handrail", new Vector3(69f,  topRailY, zBack),    new Vector3(2.20f, 0.08f, 0.10f), materials.WoodDark, parent, false);

            // Hanging chandelier above the opening centre.
            CreateOpeningChandelier(parent, materials, new Vector3(69f, 0f, zCenter));
        }

        private static void CreateOpeningChandelier(Transform parent, WolfRepoMaterials materials, Vector3 footprint)
        {
            const float chainTopY = 5.95f;     // just below the high remake ceiling at y=6.05
            const float chainBottomY = 3.00f;
            float chainCenterY = (chainTopY + chainBottomY) * 0.5f;
            float chainHalfHeight = (chainTopY - chainBottomY) * 0.5f;

            GameObject chain = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            chain.name = "Chandelier Chain";
            chain.transform.position = new Vector3(footprint.x, chainCenterY, footprint.z);
            chain.transform.localScale = new Vector3(0.04f, chainHalfHeight, 0.04f);
            chain.transform.SetParent(parent, true);
            chain.GetComponent<Renderer>().sharedMaterial = materials.WoodDark;
            UnityEngine.Object.DestroyImmediate(chain.GetComponent<Collider>());

            GameObject hub = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hub.name = "Chandelier Hub";
            hub.transform.position = new Vector3(footprint.x, chainBottomY, footprint.z);
            hub.transform.localScale = Vector3.one * 0.30f;
            hub.transform.SetParent(parent, true);
            hub.GetComponent<Renderer>().sharedMaterial = materials.Brass;
            UnityEngine.Object.DestroyImmediate(hub.GetComponent<Collider>());

            const int bulbCount = 6;
            const float bulbRadius = 0.40f;
            for (int i = 0; i < bulbCount; i++)
            {
                float angle = i * (2f * Mathf.PI / bulbCount);
                float dx = Mathf.Cos(angle) * bulbRadius;
                float dz = Mathf.Sin(angle) * bulbRadius;

                // Brass arm reaching from hub out to each bulb.
                Vector3 armCenter = new Vector3(footprint.x + dx * 0.5f, chainBottomY, footprint.z + dz * 0.5f);
                CreatePrimitiveBox(
                    $"Chandelier Arm {i + 1}",
                    armCenter,
                    new Vector3(Mathf.Abs(dx) > Mathf.Abs(dz) ? bulbRadius : 0.04f, 0.04f, Mathf.Abs(dz) > Mathf.Abs(dx) ? bulbRadius : 0.04f),
                    materials.Brass,
                    parent,
                    false);

                GameObject bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bulb.name = $"Chandelier Bulb {i + 1}";
                bulb.transform.position = new Vector3(footprint.x + dx, chainBottomY - 0.05f, footprint.z + dz);
                bulb.transform.localScale = Vector3.one * 0.14f;
                bulb.transform.SetParent(parent, true);
                bulb.GetComponent<Renderer>().sharedMaterial = materials.StairLampBulb;
                UnityEngine.Object.DestroyImmediate(bulb.GetComponent<Collider>());
            }

            GameObject lightObj = new GameObject("Chandelier Light");
            lightObj.transform.position = new Vector3(footprint.x, chainBottomY - 0.50f, footprint.z);
            lightObj.transform.SetParent(parent, true);
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1.00f, 0.86f, 0.55f);
            light.intensity = 3.0f;
            light.range = 12f;
        }

        private static GameObject CreateMeshBox(string name, Vector3 position, Mesh mesh, Material[] materials, Transform parent)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.position = position;
            gameObject.transform.SetParent(parent, true);
            MeshFilter filter = gameObject.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            return gameObject;
        }

        private static GameObject CreatePrimitiveBox(string name, Vector3 position, Vector3 scale, Material material, Transform parent, bool withCollider)
        {
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gameObject.name = name;
            gameObject.transform.position = position;
            gameObject.transform.localScale = scale;
            gameObject.transform.SetParent(parent, true);
            gameObject.isStatic = true;
            gameObject.GetComponent<Renderer>().sharedMaterial = material;
            if (!withCollider)
            {
                UnityEngine.Object.DestroyImmediate(gameObject.GetComponent<Collider>());
            }
            return gameObject;
        }

        private static GameObject CreateSlabBox(
            string name,
            Vector3 position,
            Vector3 size,
            Material top,
            Material bottom,
            Material side,
            Transform parent,
            bool withCollider)
        {
            Material[] materials = { side, side, top, bottom, side, side };
            GameObject gameObject = CreateMeshBox(name, position, CreateBoxMesh(size), materials, parent);
            gameObject.isStatic = true;
            if (withCollider)
            {
                BoxCollider collider = gameObject.AddComponent<BoxCollider>();
                collider.size = size;
            }

            return gameObject;
        }

        private static void CreateBillboard(string name, Vector3 position, Vector2 scale, Material material, Transform parent, bool blocking)
        {
            GameObject billboard = GameObject.CreatePrimitive(PrimitiveType.Quad);
            billboard.name = name;
            billboard.transform.position = position;
            billboard.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            billboard.transform.SetParent(parent, true);
            billboard.GetComponent<Renderer>().sharedMaterial = material;
            billboard.AddComponent<WolfBillboard>();

            Collider meshCollider = billboard.GetComponent<Collider>();
            if (meshCollider != null)
            {
                UnityEngine.Object.DestroyImmediate(meshCollider);
            }

            if (blocking)
            {
                GameObject colliderObject = new GameObject($"{name} Collider");
                colliderObject.transform.position = new Vector3(position.x, 0.6f, position.z);
                colliderObject.transform.SetParent(parent, true);
                CapsuleCollider collider = colliderObject.AddComponent<CapsuleCollider>();
                collider.radius = 0.30f;
                collider.height = 1.2f;
            }
        }

        private static void AddCeilingLamp(string name, Vector3 basePosition, bool warm, WolfRepoMaterials materials, Transform parent)
        {
            GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cap.name = $"{name} cap {basePosition.x:0},{basePosition.z:0}";
            cap.transform.position = new Vector3(basePosition.x, WallHeight - 0.05f, basePosition.z);
            cap.transform.localScale = new Vector3(0.48f, 0.05f, 0.48f);
            cap.transform.SetParent(parent, true);
            cap.GetComponent<Renderer>().sharedMaterial = warm ? materials.LampWarmCap : materials.LampGreenCap;

            GameObject bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulb.name = $"{name} bulb {basePosition.x:0},{basePosition.z:0}";
            bulb.transform.position = new Vector3(basePosition.x, WallHeight - 0.16f, basePosition.z);
            bulb.transform.localScale = Vector3.one * 0.20f;
            bulb.transform.SetParent(parent, true);
            bulb.GetComponent<Renderer>().sharedMaterial = warm ? materials.LampWarmBulb : materials.LampGreenBulb;

            Collider capCollider = cap.GetComponent<Collider>();
            if (capCollider != null) UnityEngine.Object.DestroyImmediate(capCollider);
            Collider bulbCollider = bulb.GetComponent<Collider>();
            if (bulbCollider != null) UnityEngine.Object.DestroyImmediate(bulbCollider);

            GameObject lightObject = new GameObject($"{name} light {basePosition.x:0},{basePosition.z:0}");
            lightObject.transform.position = new Vector3(basePosition.x, WallHeight - 0.22f, basePosition.z);
            lightObject.transform.SetParent(parent, true);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = warm ? new Color(1f, 0.87f, 0.61f) : new Color(0.62f, 1f, 0.56f);
            light.intensity = warm ? 0.55f : 0.65f;
            light.range = warm ? 7f : 8f;
        }

        private static void CreateExitMarker(WolfStaticData staticData, Transform parent)
        {
            GameObject marker = new GameObject($"Exit Marker {staticData.x:00},{staticData.y:00}");
            marker.transform.position = GridToWorld(staticData.x, staticData.y, 0.5f);
            marker.transform.SetParent(parent, true);
            BoxCollider trigger = marker.AddComponent<BoxCollider>();
            trigger.size = Vector3.one;
            trigger.isTrigger = true;
        }

        // Closed box mesh used for doors and other slabs. Submesh order matches three.js
        // BoxGeometry material slots so the [+x, -x, +y, -y, +z, -z] convention
        // carries straight over: 0=East, 1=West, 2=Top, 3=Bottom, 4=North, 5=South.
        // Normals are HARDCODED to point outward; do not derive them from the
        // cross product of the listed vertices — the original code did, and the
        // resulting inward normals on the +Y/-Y caps caused the wall texture to
        // render in place of the floor whenever the camera looked downward (the
        // back face of the cap showed up because back-face culling judged it as
        // a front face). With correct outward normals, top and bottom caps are
        // back-facing from any in-room camera position and get culled cleanly,
        // so they cost nothing visually but protect against any future case
        // where a player camera would otherwise peek through the open ends.
        private static Mesh CreateBoxMesh(Vector3 size)
        {
            float hx = size.x * 0.5f;
            float hy = size.y * 0.5f;
            float hz = size.z * 0.5f;
            // Triangle winding (CW vs CCW) is what determines visibility under
            // Unity's default Cull Back, NOT the per-vertex normals. The original
            // top/bottom orderings ran CW when viewed from inside the room (so the
            // bottom face was front-facing from above and rendered the wall texture
            // on top of the floor). The two cap faces below are wound so their
            // outward side is the front-face — i.e., the bottom is only visible
            // from underneath the cube, the top only from above the cube.
            Vector3[][] faces =
            {
                new[] { new Vector3( hx, -hy,  hz), new Vector3( hx, -hy, -hz), new Vector3( hx,  hy, -hz), new Vector3( hx,  hy,  hz) }, // 0: East  (+X)
                new[] { new Vector3(-hx, -hy, -hz), new Vector3(-hx, -hy,  hz), new Vector3(-hx,  hy,  hz), new Vector3(-hx,  hy, -hz) }, // 1: West  (-X)
                new[] { new Vector3(-hx,  hy,  hz), new Vector3( hx,  hy,  hz), new Vector3( hx,  hy, -hz), new Vector3(-hx,  hy, -hz) }, // 2: Top   (+Y) — CW from above
                new[] { new Vector3(-hx, -hy, -hz), new Vector3( hx, -hy, -hz), new Vector3( hx, -hy,  hz), new Vector3(-hx, -hy,  hz) }, // 3: Bot   (-Y) — CW from below
                new[] { new Vector3(-hx, -hy,  hz), new Vector3( hx, -hy,  hz), new Vector3( hx,  hy,  hz), new Vector3(-hx,  hy,  hz) }, // 4: North (+Z)
                new[] { new Vector3( hx, -hy, -hz), new Vector3(-hx, -hy, -hz), new Vector3(-hx,  hy, -hz), new Vector3( hx,  hy, -hz) }  // 5: South (-Z)
            };

            Vector3[] faceNormals =
            {
                new Vector3( 1f,  0f,  0f), // East
                new Vector3(-1f,  0f,  0f), // West
                new Vector3( 0f,  1f,  0f), // Top
                new Vector3( 0f, -1f,  0f), // Bottom
                new Vector3( 0f,  0f,  1f), // North
                new Vector3( 0f,  0f, -1f)  // South
            };

            int faceCount = faces.Length;
            Vector3[] vertices = new Vector3[faceCount * 4];
            Vector2[] uvs = new Vector2[faceCount * 4];
            Vector3[] normals = new Vector3[faceCount * 4];
            int[][] triangles = new int[faceCount][];
            Vector2[] faceUvs = { new(0f, 0f), new(1f, 0f), new(1f, 1f), new(0f, 1f) };

            for (int face = 0; face < faceCount; face++)
            {
                int start = face * 4;
                Vector3 normal = faceNormals[face];
                for (int i = 0; i < 4; i++)
                {
                    vertices[start + i] = faces[face][i];
                    uvs[start + i] = faceUvs[i];
                    normals[start + i] = normal;
                }

                triangles[face] = new[] { start, start + 1, start + 2, start, start + 2, start + 3 };
            }

            Mesh mesh = new Mesh
            {
                name = $"WolfRepoBox_{size.x:0.00}_{size.y:0.00}_{size.z:0.00}",
                vertices = vertices,
                uv = uvs,
                normals = normals,
                subMeshCount = faceCount
            };

            for (int face = 0; face < faceCount; face++)
            {
                mesh.SetTriangles(triangles[face], face);
            }

            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateWallCellMesh(int[] walls, int x, int y, bool excludeVerticalReserved = false)
        {
            const float hx = Cell * 0.5f;
            const float hy = WallHeight * 0.5f;
            const float hz = Cell * 0.5f;

            List<Vector3> vertices = new();
            List<Vector2> uvs = new();
            List<Vector3> normals = new();
            List<int>[] triangles =
            {
                new List<int>(),
                new List<int>(),
                new List<int>(),
                new List<int>()
            };

            if (!IsSolidWall(walls, x + 1, y, excludeVerticalReserved))
            {
                AddFace(vertices, uvs, normals, triangles[0], Vector3.right,
                    new Vector3(hx, -hy, hz), new Vector3(hx, -hy, -hz), new Vector3(hx, hy, -hz), new Vector3(hx, hy, hz));
            }

            if (!IsSolidWall(walls, x - 1, y, excludeVerticalReserved))
            {
                AddFace(vertices, uvs, normals, triangles[1], Vector3.left,
                    new Vector3(-hx, -hy, -hz), new Vector3(-hx, -hy, hz), new Vector3(-hx, hy, hz), new Vector3(-hx, hy, -hz));
            }

            if (!IsSolidWall(walls, x, y + 1, excludeVerticalReserved))
            {
                AddFace(vertices, uvs, normals, triangles[2], Vector3.forward,
                    new Vector3(-hx, -hy, hz), new Vector3(hx, -hy, hz), new Vector3(hx, hy, hz), new Vector3(-hx, hy, hz));
            }

            if (!IsSolidWall(walls, x, y - 1, excludeVerticalReserved))
            {
                AddFace(vertices, uvs, normals, triangles[3], Vector3.back,
                    new Vector3(hx, -hy, -hz), new Vector3(-hx, -hy, -hz), new Vector3(-hx, hy, -hz), new Vector3(hx, hy, -hz));
            }

            Mesh mesh = new Mesh
            {
                name = $"WolfRepoWallCell_{x:00}_{y:00}",
                vertices = vertices.ToArray(),
                uv = uvs.ToArray(),
                normals = normals.ToArray(),
                subMeshCount = 4
            };

            for (int i = 0; i < triangles.Length; i++)
            {
                mesh.SetTriangles(triangles[i], i);
            }

            mesh.RecalculateBounds();
            return mesh;
        }

        private static bool IsSolidWall(int[] walls, int x, int y, bool excludeVerticalReserved = false)
        {
            if (x < 0 || x >= MapSize || y < 0 || y >= MapSize)
            {
                return false;
            }

            if (excludeVerticalReserved && IsVerticalRemakeReservedCell(x, y))
            {
                return false;
            }

            return walls[y * MapSize + x] > 0;
        }

        private static bool HasExposedSide(int[] walls, int x, int y, bool excludeVerticalReserved = false)
        {
            return !IsSolidWall(walls, x + 1, y, excludeVerticalReserved)
                || !IsSolidWall(walls, x - 1, y, excludeVerticalReserved)
                || !IsSolidWall(walls, x, y + 1, excludeVerticalReserved)
                || !IsSolidWall(walls, x, y - 1, excludeVerticalReserved);
        }

        private static bool IsBasementHoleCell(int x, int y)
        {
            return x == 34 && y >= 5 && y <= 7;
        }

        private static bool IsVerticalRemakeReservedCell(int x, int y)
        {
            return IsBasementHoleCell(x, y) || IsUpperStairHubCell(x, y);
        }

        private static bool IsUpperStairHubCell(int x, int y)
        {
            return x >= 25 && x <= 45 && y >= 24 && y <= 36;
        }

        private static void AddFace(
            List<Vector3> vertices,
            List<Vector2> uvs,
            List<Vector3> normals,
            List<int> triangles,
            Vector3 normal,
            Vector3 v0,
            Vector3 v1,
            Vector3 v2,
            Vector3 v3)
        {
            int start = vertices.Count;
            vertices.Add(v0);
            vertices.Add(v1);
            vertices.Add(v2);
            vertices.Add(v3);
            uvs.Add(new Vector2(0f, 0f));
            uvs.Add(new Vector2(1f, 0f));
            uvs.Add(new Vector2(1f, 1f));
            uvs.Add(new Vector2(0f, 1f));
            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
            triangles.Add(start);
            triangles.Add(start + 2);
            triangles.Add(start + 3);
        }

        private static Vector3 GridToWorld(int x, int y, float worldY)
        {
            return new Vector3(x * Cell + Cell * 0.5f, worldY, y * Cell + Cell * 0.5f);
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

        private static void ConfigureRenderSettings(WolfRepoLevel level)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.55f);
            RenderSettings.fog = false;
            RenderSettings.skybox = null;
            DynamicGI.UpdateEnvironment();
        }

        private static void ConfigureImportedTextures()
        {
            ConfigureTexture("walls.png", false, TextureWrapMode.Clamp, FilterMode.Point);
            ConfigureTexture("sprites.png", true, TextureWrapMode.Clamp, FilterMode.Point);
            ConfigureTexture("guard.png", true, TextureWrapMode.Clamp, FilterMode.Point);
            ConfigureTexture("guard_soft.png", true, TextureWrapMode.Clamp, FilterMode.Point);
            ConfigureTexture("dog.png", true, TextureWrapMode.Clamp, FilterMode.Point);
            ConfigureTexture("attack.png", true, TextureWrapMode.Clamp, FilterMode.Point);
            ConfigureTexture("bj.png", true, TextureWrapMode.Clamp, FilterMode.Point);
            ConfigureTexture("hudbg.png", true, TextureWrapMode.Clamp, FilterMode.Point);
            ConfigureTexture("hudkeys.png", true, TextureWrapMode.Clamp, FilterMode.Point);
            ConfigureTexture("hudnumbers.png", true, TextureWrapMode.Clamp, FilterMode.Point);
            ConfigureTexture("hudweapons.png", true, TextureWrapMode.Clamp, FilterMode.Point);
        }

        private static void ConfigureTexture(string fileName, bool alpha, TextureWrapMode wrapMode, FilterMode filterMode)
        {
            string path = $"{TextureFolder}/{fileName}";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = alpha ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
            importer.mipmapEnabled = false;
            importer.wrapMode = wrapMode;
            importer.filterMode = filterMode;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static WolfRepoAssets LoadAssets()
        {
            return new WolfRepoAssets
            {
                Walls = LoadTexture("walls.png"),
                Sprites = LoadTexture("sprites.png"),
                Guard = LoadTexture("guard.png"),
                Dog = LoadTexture("dog.png"),
                Attack = LoadTexture("attack.png"),
                Bj = LoadTexture("bj.png"),
                HudKeys = LoadTexture("hudkeys.png")
            };
        }

        private static Texture2D LoadTexture(string fileName)
        {
            string path = $"{TextureFolder}/{fileName}";
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
            {
                throw new InvalidOperationException($"Missing texture: {path}");
            }
            return texture;
        }

        private static void EnsureFolders()
        {
            CreateFolderIfMissing("Assets", "Scenes");
            CreateFolderIfMissing("Assets", "Materials");
            CreateFolderIfMissing("Assets/Materials", "WolfRepo");
            CreateFolderIfMissing("Assets", "Data");
            CreateFolderIfMissing("Assets/Data", "WolfRepo");
            CreateFolderIfMissing("Assets", "Textures");
            CreateFolderIfMissing("Assets/Textures", "WolfRepo");
        }

        private static void CreateFolderIfMissing(string parent, string child)
        {
            string path = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static Texture2D WriteAndImportTexture(
            string name,
            Func<Texture2D> build,
            TextureWrapMode wrapMode = TextureWrapMode.Repeat,
            FilterMode filterMode = FilterMode.Bilinear,
            TextureImporterCompression compression = TextureImporterCompression.Compressed)
        {
            string assetPath = $"{TextureFolder}/{name}.png";
            string absPath = ToAbsoluteAssetPath(assetPath);
            Texture2D texture = build();
            File.WriteAllBytes(absPath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(assetPath) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = true;
                importer.alphaSource = TextureImporterAlphaSource.None;
                importer.mipmapEnabled = true;
                importer.wrapMode = wrapMode;
                importer.filterMode = filterMode;
                importer.anisoLevel = 4;
                importer.maxTextureSize = 1024;
                importer.textureCompression = compression;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        private static Texture2D BuildBasementBlueWallTex()
        {
            return BuildHdWallTileTexture(14, 512, 1.04f, 1.14f, 0.08f);
        }

        private static Texture2D BuildBasementFloorStoneTex()
        {
            return BuildHdWallTileTexture(68, 512, 0.78f, 0.95f, 0.04f);
        }

        private static Texture2D BuildBasementCeilingStoneTex()
        {
            return BuildHdWallTileTexture(69, 512, 0.58f, 0.90f, 0.03f);
        }

        private static Texture2D BuildStairStoneTex()
        {
            return BuildStepTexture(68, true);
        }

        private static Texture2D BuildStairRiserTex()
        {
            return BuildStepTexture(68, false);
        }

        private static Texture2D BuildStepTexture(int tileIndex, bool tread)
        {
            const int size = 1024;
            Texture2D texture = BuildHdWallTileTexture(tileIndex, size, tread ? 1.08f : 0.82f, tread ? 1.20f : 1.10f, tread ? 0.025f : 0.015f);
            Color[] pixels = texture.GetPixels();

            for (int y = 0; y < size; y++)
            {
                float v = y / (size - 1f);
                for (int x = 0; x < size; x++)
                {
                    float u = x / (size - 1f);
                    Color c = pixels[y * size + x];
                    float sideShade = Mathf.Lerp(0.80f, 1f, Mathf.SmoothStep(0f, 0.14f, Mathf.Min(u, 1f - u)));
                    float shade;

                    if (tread)
                    {
                        float frontShadow = 1f - 0.34f * (1f - Mathf.SmoothStep(0f, 0.28f, v));
                        float backFalloff = 1f - 0.10f * Mathf.SmoothStep(0.82f, 1f, v);
                        float bevelHighlight = 1f + 0.20f * Mathf.Exp(-Mathf.Pow((v - 0.30f) * 24f, 2f));
                        float groutShade = Mathf.Abs(Mathf.Repeat((u * 3.0f) + (v * 0.35f), 1f) - 0.5f) < 0.018f ? 0.88f : 1f;
                        shade = frontShadow * backFalloff * bevelHighlight * groutShade * sideShade;
                    }
                    else
                    {
                        float topHighlight = 1f + 0.26f * (1f - Mathf.SmoothStep(0f, 0.16f, v));
                        float lowerShadow = 1f - 0.42f * Mathf.SmoothStep(0.42f, 1f, v);
                        float horizontalJoint = Mathf.Abs(Mathf.Repeat(v * 2.0f, 1f) - 0.5f) < 0.030f ? 0.78f : 1f;
                        shade = topHighlight * lowerShadow * horizontalJoint * sideShade;
                    }

                    c *= shade;
                    c.a = 1f;
                    pixels[y * size + x] = ClampColor(c);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(true);
            return texture;
        }

        private static Texture2D BuildHdWallTileTexture(int tileIndex, int outputSize, float brightness, float contrast, float detailAmount)
        {
            Texture2D source = LoadReadableWallsTexture();
            int tileSize = source.width / AtlasSize;
            int tileX = tileIndex % AtlasSize;
            int tileYFromTop = tileIndex / AtlasSize;
            int sourceX0 = tileX * tileSize;
            int sourceY0 = source.height - (tileYFromTop + 1) * tileSize;
            Texture2D texture = NewTexture(outputSize);
            Color[] pixels = new Color[outputSize * outputSize];

            for (int y = 0; y < outputSize; y++)
            {
                float localY = (y + 0.5f) * tileSize / outputSize;
                int sy = Mathf.Clamp(Mathf.FloorToInt(localY), 0, tileSize - 1);
                float fy = localY - Mathf.Floor(localY);
                for (int x = 0; x < outputSize; x++)
                {
                    float localX = (x + 0.5f) * tileSize / outputSize;
                    int sx = Mathf.Clamp(Mathf.FloorToInt(localX), 0, tileSize - 1);
                    float fx = localX - Mathf.Floor(localX);

                    Color baseColor = source.GetPixel(sourceX0 + sx, sourceY0 + sy);
                    float baseLum = Luminance(baseColor);
                    float neighborLum =
                        Luminance(SampleTilePixel(source, sourceX0, sourceY0, tileSize, sx - 1, sy)) +
                        Luminance(SampleTilePixel(source, sourceX0, sourceY0, tileSize, sx + 1, sy)) +
                        Luminance(SampleTilePixel(source, sourceX0, sourceY0, tileSize, sx, sy - 1)) +
                        Luminance(SampleTilePixel(source, sourceX0, sourceY0, tileSize, sx, sy + 1));
                    neighborLum *= 0.25f;

                    float edge = Mathf.Clamp01(Mathf.Abs(baseLum - neighborLum) * 5.5f);
                    float pixelCenter = 1f - Mathf.Max(Mathf.Abs(fx - 0.5f), Mathf.Abs(fy - 0.5f)) * 2f;
                    float blockBoundary = Mathf.Max(
                        fx < 0.12f || fx > 0.88f ? 1f : 0f,
                        fy < 0.12f || fy > 0.88f ? 1f : 0f);
                    float detail = (Mathf.PerlinNoise((x + tileIndex * 53f) / 17f, (y + tileIndex * 97f) / 19f) - 0.5f) * detailAmount;
                    float shade = brightness + detail + pixelCenter * 0.07f - edge * 0.08f - blockBoundary * 0.035f;
                    Color c = ApplyContrast(baseColor, contrast) * shade;
                    c.a = 1f;
                    pixels[y * outputSize + x] = ClampColor(c);
                }
            }

            UnityEngine.Object.DestroyImmediate(source);
            texture.SetPixels(pixels);
            texture.Apply(true);
            return texture;
        }

        private static Texture2D LoadReadableWallsTexture()
        {
            string path = ToAbsoluteAssetPath($"{TextureFolder}/walls.png");
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(path)))
            {
                throw new InvalidOperationException($"Could not decode source wall texture: {path}");
            }

            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Point;
            return texture;
        }

        private static Color SampleTilePixel(Texture2D source, int x0, int y0, int tileSize, int x, int y)
        {
            x = Mathf.Clamp(x, 0, tileSize - 1);
            y = Mathf.Clamp(y, 0, tileSize - 1);
            return source.GetPixel(x0 + x, y0 + y);
        }

        private static Color ApplyContrast(Color color, float contrast)
        {
            return new Color(
                (color.r - 0.5f) * contrast + 0.5f,
                (color.g - 0.5f) * contrast + 0.5f,
                (color.b - 0.5f) * contrast + 0.5f,
                color.a);
        }

        private static float Luminance(Color color)
        {
            return color.r * 0.2126f + color.g * 0.7152f + color.b * 0.0722f;
        }

        private static string ToAbsoluteAssetPath(string assetPath)
        {
            string relative = assetPath.StartsWith("Assets/", StringComparison.Ordinal)
                ? assetPath.Substring("Assets/".Length)
                : assetPath;
            return Path.Combine(Application.dataPath, relative.Replace('/', Path.DirectorySeparatorChar));
        }

        private static Texture2D NewTexture(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Bilinear;
            return texture;
        }

        private static Color ClampColor(Color color)
        {
            return new Color(
                Mathf.Clamp01(color.r),
                Mathf.Clamp01(color.g),
                Mathf.Clamp01(color.b),
                Mathf.Clamp01(color.a));
        }

        private static int Mod(int value, int divisor)
        {
            int result = value % divisor;
            return result < 0 ? result + divisor : result;
        }

        private sealed class WolfRepoMaterials
        {
            private readonly WolfRepoAssets assets;
            private readonly Dictionary<int, Material[]> wallMaterials = new();
            private readonly Dictionary<int, Material> wallTileMaterials = new();
            private readonly Dictionary<int, Material> staticMaterials = new();
            private readonly Dictionary<string, Material> enemyMaterials = new();
            private readonly Dictionary<string, Material[]> doorMaterials = new();

            public readonly Material FloorColor;
            public readonly Material CeilingColor;
            public readonly Material LampWarmCap;
            public readonly Material LampGreenCap;
            public readonly Material LampWarmBulb;
            public readonly Material LampGreenBulb;
            public readonly Material BasementWall;
            public readonly Material BasementFloor;
            public readonly Material BasementCeiling;
            public readonly Material StairStone;
            public readonly Material StairRiser;
            public readonly Material StairRail;
            public readonly Material StairTrim;
            public readonly Material StairLampBulb;
            public readonly Material Wood;
            public readonly Material WoodDark;
            public readonly Material Brass;

            public WolfRepoMaterials(WolfRepoAssets assets)
            {
                this.assets = assets;
                FloorColor = CreateColorMaterial("Mat_WolfRepo_FloorColor", new Color(112f / 255f, 112f / 255f, 112f / 255f));
                CeilingColor = CreateColorMaterial("Mat_WolfRepo_CeilingColor", new Color(56f / 255f, 56f / 255f, 56f / 255f));
                LampWarmCap = CreateColorMaterial("Mat_WolfRepo_LampWarmCap", new Color(0.65f, 0.48f, 0.16f));
                LampGreenCap = CreateColorMaterial("Mat_WolfRepo_LampGreenCap", new Color(0.12f, 0.50f, 0.34f));
                LampWarmBulb = CreateColorMaterial("Mat_WolfRepo_LampWarmBulb", new Color(1f, 0.85f, 0.45f), true);
                LampGreenBulb = CreateColorMaterial("Mat_WolfRepo_LampGreenBulb", new Color(0.36f, 1f, 0.46f), true);

                Texture2D basementWallTex = WriteAndImportTexture("Tex_WolfRepo_BasementBlueWall", BuildBasementBlueWallTex);
                Texture2D basementFloorTex = WriteAndImportTexture("Tex_WolfRepo_BasementFloorStone", BuildBasementFloorStoneTex);
                Texture2D basementCeilingTex = WriteAndImportTexture("Tex_WolfRepo_BasementCeilingStone", BuildBasementCeilingStoneTex);
                Texture2D stairStoneTex = WriteAndImportTexture("Tex_WolfRepo_StairStoneHD", BuildStairStoneTex, TextureWrapMode.Repeat, FilterMode.Point, TextureImporterCompression.Uncompressed);
                Texture2D stairRiserTex = WriteAndImportTexture("Tex_WolfRepo_StairRiserHD", BuildStairRiserTex, TextureWrapMode.Repeat, FilterMode.Point, TextureImporterCompression.Uncompressed);

                BasementWall    = CreateTexturedMaterial("Mat_WolfRepo_BasementWall",    basementWallTex,    Color.white, new Vector2(2.2f, 1.8f), 0.02f, 0.20f);
                BasementFloor   = CreateTexturedMaterial("Mat_WolfRepo_BasementFloor",   basementFloorTex,   Color.white, new Vector2(3.4f, 3.4f), 0.02f, 0.16f);
                BasementCeiling = CreateTexturedMaterial("Mat_WolfRepo_BasementCeiling", basementCeilingTex, Color.white, new Vector2(2.4f, 2.4f), 0.00f, 0.10f);
                StairStone      = CreateTexturedMaterial("Mat_WolfRepo_StairStone",      stairStoneTex,      Color.white, Vector2.one, 0.02f, 0.22f);
                StairRiser      = CreateTexturedMaterial("Mat_WolfRepo_StairRiser",      stairRiserTex,      Color.white, Vector2.one, 0.02f, 0.18f);
                StairRail       = CreateColorMaterial("Mat_WolfRepo_StairRail",       new Color(0.02f, 0.03f, 0.08f));
                StairTrim       = CreateColorMaterial("Mat_WolfRepo_StairTrim",       new Color(0.00f, 0.72f, 0.86f));
                StairLampBulb   = CreateColorMaterial("Mat_WolfRepo_StairLampBulb",   new Color(1.00f, 0.86f, 0.55f), true);
                Wood            = CreateColorMaterial("Mat_WolfRepo_Wood",            new Color(0.46f, 0.30f, 0.16f));
                WoodDark        = CreateColorMaterial("Mat_WolfRepo_WoodDark",        new Color(0.26f, 0.17f, 0.10f));
                Brass           = CreateColorMaterial("Mat_WolfRepo_Brass",           new Color(0.82f, 0.65f, 0.28f));
            }

            public Material[] GetWallMaterials(int wallValue)
            {
                if (wallMaterials.TryGetValue(wallValue, out Material[] cached))
                {
                    return cached;
                }

                int lightIdx = (wallValue - 1) * 2;
                int darkIdx = lightIdx + 1;
                Material light = GetWallTileMaterial(lightIdx);
                Material dark = GetWallTileMaterial(darkIdx);
                // Submesh order matches three.js BoxGeometry: [+x, -x, +y, -y, +z, -z]
                // = [East, West, Top, Bottom, North, South]. Wolf3D convention:
                // dark on E/W (vertical pillars), light on top/bottom/N/S — same as
                // the three.js port (`[darkMat, darkMat, lightMat, lightMat, lightMat, lightMat]`).
                Material[] materials = { dark, dark, light, light, light, light };
                wallMaterials[wallValue] = materials;
                return materials;
            }

            public Material[] GetWallSideMaterials(int wallValue)
            {
                Material[] six = GetWallMaterials(wallValue);
                return new[] { six[0], six[1], six[4], six[5] };
            }

            public Material[] GetDoorMaterials(string type, bool vertical)
            {
                string key = $"{type}:{vertical}";
                if (doorMaterials.TryGetValue(key, out Material[] cached))
                {
                    return cached;
                }

                int faceIdx = type switch
                {
                    "elevator" => 102,
                    "gold" => 104,
                    "silver" => 104,
                    _ => 98
                };
                Material face = GetWallTileMaterial(faceIdx);
                Material side = GetWallTileMaterial(100);
                // 6-submesh order: [East, West, Top, Bottom, North, South].
                // Vertical door (thin in X): face on E/W; jamb on top/bottom/N/S.
                // Horizontal door (thin in Z): face on N/S; jamb on E/W/top/bottom.
                Material[] materials = vertical
                    ? new[] { face, face, side, side, side, side }
                    : new[] { side, side, side, side, face, face };
                doorMaterials[key] = materials;
                return materials;
            }

            public Material GetStaticMaterial(int typeIndex, StatInfo info)
            {
                if (staticMaterials.TryGetValue(typeIndex, out Material cached))
                {
                    return cached;
                }

                int texIdx = typeIndex + StatSpriteOffset;
                int col = texIdx % AtlasSize;
                int row = texIdx / AtlasSize;
                Material material = CreateAtlasMaterial($"Mat_WolfRepo_Static_{typeIndex:00}_{info.name}", assets.Sprites, AtlasSize, col, row, Color.white, true);
                staticMaterials[typeIndex] = material;
                return material;
            }

            public Material GetEnemyMaterial(string type, Color tint)
            {
                string key = $"{type}:{tint}";
                if (enemyMaterials.TryGetValue(key, out Material cached))
                {
                    return cached;
                }

                Material material;
                if (type == "dog")
                {
                    material = CreateAtlasMaterial("Mat_WolfRepo_Enemy_Dog", assets.Dog, 39, 0, 0, tint, true, horizontalStrip: true);
                }
                else
                {
                    material = CreateAtlasMaterial($"Mat_WolfRepo_Enemy_{type}", assets.Guard, 8, 0, 0, tint, true);
                }

                enemyMaterials[key] = material;
                return material;
            }

            private Material GetWallTileMaterial(int tileIndex)
            {
                if (wallTileMaterials.TryGetValue(tileIndex, out Material cached))
                {
                    return cached;
                }

                int col = tileIndex % AtlasSize;
                int row = tileIndex / AtlasSize;
                Material material = CreateAtlasMaterial($"Mat_WolfRepo_WallTile_{tileIndex:000}", assets.Walls, AtlasSize, col, row, Color.white, false);
                wallTileMaterials[tileIndex] = material;
                return material;
            }
        }

        private static Material CreateAtlasMaterial(
            string name,
            Texture2D texture,
            int atlasSize,
            int col,
            int row,
            Color tint,
            bool transparent,
            bool horizontalStrip = false)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            // Use unlit shaders for everything — walls, doors, sprites. Lambert was
            // tried earlier but per-vertex shading creates visible creases at every
            // cube boundary, which read as "perpendicular wall pieces" along long
            // flat walls. Wolf3D's renderer is also flat-shaded; Unlit matches.
            Shader targetShader = Shader.Find(transparent ? "Unlit/Transparent" : "Unlit/Texture")
                                  ?? Shader.Find("Standard");
            if (material == null)
            {
                material = new Material(targetShader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = targetShader;
            material.mainTexture = texture;
            material.color = tint;
            if (horizontalStrip)
            {
                material.mainTextureScale = new Vector2(1f / atlasSize, 1f);
                material.mainTextureOffset = new Vector2(col / (float)atlasSize, 0f);
            }
            else
            {
                material.mainTextureScale = new Vector2(1f / atlasSize, 1f / atlasSize);
                material.mainTextureOffset = new Vector2(col / (float)atlasSize, 1f - (row + 1f) / atlasSize);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateTexturedMaterial(string name, Texture2D texture, Color color, Vector2 tiling, float metallic, float gloss)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Unlit/Texture") ?? Shader.Find("Standard");
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.color = color;
            material.mainTexture = texture;
            material.mainTextureScale = tiling;
            material.mainTextureOffset = Vector2.zero;

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", metallic);
            }

            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", gloss);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateColorMaterial(string name, Color color, bool emissive = false)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            if (emissive && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 1.7f);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        [Serializable]
        private sealed class WolfRepoLevel
        {
            public string name;
            public string music;
            public string parTime;
            public int[] ceiling;
            public int[] floor;
            public int[] walls;
            public WolfDoorData[] doors;
            public WolfEnemyData[] enemies;
            public WolfStaticData[] statics;
            public int spawnX;
            public int spawnY;
            public int spawnAngle;
        }

        [Serializable]
        private sealed class WolfDoorData
        {
            public int x;
            public int y;
            public string type;
            public bool vertical;
        }

        [Serializable]
        private sealed class WolfEnemyData
        {
            public int x;
            public int y;
            public string type;
            public int dir;
            public bool patrol;
            public int difficulty;
        }

        [Serializable]
        private sealed class WolfStaticData
        {
            public int x;
            public int y;
            public int typeIndex;
            public string typeName;
        }

        private sealed class WolfRepoAssets
        {
            public Texture2D Walls;
            public Texture2D Sprites;
            public Texture2D Guard;
            public Texture2D Dog;
            public Texture2D Attack;
            public Texture2D Bj;
            public Texture2D HudKeys;
        }

        private readonly struct StatInfo
        {
            public readonly string name;
            public readonly bool blocking;
            public readonly string pickupType;
            public readonly int tileX;
            public readonly int tileY;

            public StatInfo(string name, bool blocking, string pickupType, int tileX, int tileY)
            {
                this.name = name;
                this.blocking = blocking;
                this.pickupType = pickupType;
                this.tileX = tileX;
                this.tileY = tileY;
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
}
