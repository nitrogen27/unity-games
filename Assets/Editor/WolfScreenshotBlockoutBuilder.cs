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
    public static class WolfScreenshotBlockoutBuilder
    {
        private const string ScenePath = "Assets/Scenes/WolfScreenshotBlockout.unity";
        private const string MaterialFolder = "Assets/Materials/Blockout";
        private const string GeneratedTextureFolder = "Assets/Textures/BlockoutWolf3D";
        private const string WolfRepoTextureFolder = "Assets/Textures/WolfRepo";
        private const string WolfRepoLevelPath = "Assets/Data/WolfRepo/level0.json";
        private const int WolfAtlasSize = 16;
        private const int MapSize = 64;
        private const float Cell = 2.0f;
        private const float DoorThickness = 0.14f;

        private const float WallHeight = 3.0f;
        private const float WallThickness = 0.32f;
        private const float RailHeight = 1.08f;
        private const float RailThickness = 0.18f;
        private const float FloorThickness = 0.22f;
        private const float CeilingThickness = 0.08f;
        private const float StoneBaseHeight = 1.18f;
        private const float UpperY = 4.0f;
        private const float TextureWorldUnits = 1.45f;

        private static BlockoutMaterials activeMaterials;

        [MenuItem("Tools/Wolf Blockout/Create Screenshot Space Map")]
        public static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                Debug.LogWarning("[WolfBlockout] Stopped Play Mode. Run the builder again after Play Mode exits.");
                return;
            }

            EnsureFolders();

            BlockoutMaterials materials = CreateMaterials();
            activeMaterials = materials;
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "WolfScreenshotBlockout";

            ConfigureRenderSettings();

            GameObject root = new GameObject("Wolf Screenshot Blockout");
            BuildCopiedWolfLocations(root.transform, materials);
            CreatePlayer();
            CreateHud();
            CreateLighting();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[WolfBlockout] Built two-floor screenshot blockout. Scene saved at " + ScenePath + ". Press Play: WASD/mouse, Shift, Space.");
        }

        private static void BuildCopiedWolfLocations(Transform parent, BlockoutMaterials materials)
        {
            SourceLevel level = LoadSourceLevel();
            GameObject hallRoot = CreateChild("01 Screenshot Wolf3D Hall And Stairs", parent);
            GameObject copiedRoot = CreateChild("02 Standard Wolf3D Room Copies", parent);

            BuildScreenshotReferenceHall(hallRoot.transform, materials);

            CopyWolfModule(level, "Lower West Standard Stone Room Copy x00 y15", 0, 15, 16, 14, -36, -6, 0f, copiedRoot.transform, materials);
            CopyWolfModule(level, "Lower East Standard Wood Room Copy x26 y35", 26, 35, 16, 14, 20, -6, 0f, copiedRoot.transform, materials);
            CopyWolfModule(level, "Upper Back Standard Blue Hall Copy x47 y16", 47, 16, 17, 12, -8, 11, UpperY, copiedRoot.transform, materials);
        }

        private static void BuildScreenshotReferenceHall(Transform parent, BlockoutMaterials materials)
        {
            GameObject floors = CreateChild("Wolf3D Hall Floors", parent);
            GameObject walls = CreateChild("Wolf3D Stone Base Wood Walls", parent);
            GameObject rails = CreateChild("Wolf3D Balcony Rails", parent);
            GameObject stairs = CreateChild("Wolf3D Twin Staircases", parent);
            GameObject decor = CreateChild("Wolf3D Hall Props", parent);

            Floor("Lower Main Hall Floor", -15f, 15f, -16f, 4.2f, 0f, materials.Floor, floors.transform);
            Floor("Lower Entry Porch Floor", -5.2f, 5.2f, -20f, -16f, 0f, materials.Floor, floors.transform);
            Floor("Lower Under Gallery Room Floor", -4.9f, 4.9f, 4.2f, 12.5f, 0f, materials.Floor, floors.transform);
            Floor("Lower Left Side Hall Floor", -27f, -15f, -12f, 5f, 0f, materials.Floor, floors.transform);
            Floor("Lower Right Side Hall Floor", 15f, 27f, -12f, 5f, 0f, materials.Floor, floors.transform);
            Floor("Upper Rear Gallery Floor", -15f, 15f, 2.2f, 12.5f, UpperY, materials.Floor, floors.transform);
            Floor("Upper Rear Connector Floor", -27f, 27f, 12.5f, 22f, UpperY, materials.Floor, floors.transform);
            Floor("Upper Left Gallery Floor", -27f, -15f, -6f, 12.5f, UpperY, materials.Floor, floors.transform);
            Floor("Upper Right Gallery Floor", 15f, 27f, -6f, 12.5f, UpperY, materials.Floor, floors.transform);

            WallZBaseUpper("Lower South Wall Left", -16f, -15f, -2.4f, 0f, WallHeight, materials.Wood, walls.transform);
            WallZBaseUpper("Lower South Wall Right", -16f, 2.4f, 15f, 0f, WallHeight, materials.Wood, walls.transform);
            WallZ("Lower Porch Front Rail Left", -20f, -5.2f, -1.8f, 0f, RailHeight, materials.StoneBase, walls.transform, RailThickness);
            WallZ("Lower Porch Front Rail Right", -20f, 1.8f, 5.2f, 0f, RailHeight, materials.StoneBase, walls.transform, RailThickness);
            WallX("Lower Porch West Rail", -5.2f, -20f, -16f, 0f, RailHeight, materials.StoneBase, walls.transform, RailThickness);
            WallX("Lower Porch East Rail", 5.2f, -20f, -16f, 0f, RailHeight, materials.StoneBase, walls.transform, RailThickness);
            WallXBaseUpper("Lower West Main Wall Front", -15f, -16f, -8.2f, 0f, WallHeight, materials.Wood, walls.transform);
            WallXBaseUpper("Lower West Main Wall Back", -15f, -5.4f, 4.2f, 0f, WallHeight, materials.Wood, walls.transform);
            WallXBaseUpper("Lower East Main Wall Front", 15f, -16f, -8.2f, 0f, WallHeight, materials.Stone, walls.transform);
            WallXBaseUpper("Lower East Main Wall Back", 15f, -5.4f, 4.2f, 0f, WallHeight, materials.Stone, walls.transform);

            WallXBaseUpper("Lower Left Wing Outer Wall", -27f, -12f, 5f, 0f, WallHeight, materials.Wood, walls.transform);
            WallZBaseUpper("Lower Left Wing Back Wall", 5f, -27f, -15f, 0f, WallHeight, materials.Wood, walls.transform);
            WallZBaseUpper("Lower Left Wing South Wall", -12f, -27f, -15f, 0f, WallHeight, materials.Wood, walls.transform);
            WallXBaseUpper("Lower Right Wing Outer Wall", 27f, -12f, 5f, 0f, WallHeight, materials.Stone, walls.transform);
            WallZBaseUpper("Lower Right Wing Back Wall", 5f, 15f, 27f, 0f, WallHeight, materials.Stone, walls.transform);
            WallZBaseUpper("Lower Right Wing South Wall", -12f, 15f, 27f, 0f, WallHeight, materials.Stone, walls.transform);

            WallZBaseUpper("Under Gallery Left Face", 4.2f, -15f, -4.8f, 0f, WallHeight, materials.Stone, walls.transform);
            WallZBaseUpper("Under Gallery Right Face", 4.2f, 4.8f, 15f, 0f, WallHeight, materials.Stone, walls.transform);
            WallXBaseUpper("Under Gallery Room Left Wall", -4.9f, 4.2f, 12.5f, 0f, WallHeight, materials.Stone, walls.transform);
            WallXBaseUpper("Under Gallery Room Right Wall", 4.9f, 4.2f, 12.5f, 0f, WallHeight, materials.Stone, walls.transform);
            WallZBaseUpper("Under Gallery Room Back Wall", 12.5f, -4.9f, 4.9f, 0f, WallHeight, materials.Stone, walls.transform);
            WallZBaseUpper("Upper Rear Wood Wall Left", 12.5f, -15f, -2.2f, UpperY, WallHeight, materials.Wood, walls.transform);
            WallZBaseUpper("Upper Rear Wood Wall Right", 12.5f, 2.2f, 15f, UpperY, WallHeight, materials.Wood, walls.transform);
            WallXBaseUpper("Upper Rear Connector West Wall", -27f, 12.5f, 22f, UpperY, WallHeight, materials.Wood, walls.transform);
            WallXBaseUpper("Upper Rear Connector East Wall", 27f, 12.5f, 22f, UpperY, WallHeight, materials.Wood, walls.transform);
            WallXBaseUpper("Upper West Main Wall", -15f, -6f, 12.5f, UpperY, WallHeight, materials.Stone, walls.transform);
            WallXBaseUpper("Upper East Main Wall", 15f, -6f, 12.5f, UpperY, WallHeight, materials.Stone, walls.transform);
            WallXBaseUpper("Upper Left Wing Outer Wall", -27f, -6f, 12.5f, UpperY, WallHeight, materials.Stone, walls.transform);
            WallZBaseUpper("Upper Left Wing Back Wall", 12.5f, -27f, -15f, UpperY, WallHeight, materials.Stone, walls.transform);
            WallXBaseUpper("Upper Right Wing Outer Wall", 27f, -6f, 12.5f, UpperY, WallHeight, materials.Stone, walls.transform);
            WallZBaseUpper("Upper Right Wing Back Wall", 12.5f, 15f, 27f, UpperY, WallHeight, materials.Stone, walls.transform);

            DoorMarker("Lower Front Metal Door", 0f, -16.08f, 0f, true, materials.DoorMarker, walls.transform);
            DoorMarker("Lower Left Blue Door", -15.1f, -6.8f, 0f, false, materials.BlueDoor, walls.transform);
            DoorMarker("Lower Right Blue Door", 15.1f, -6.8f, 0f, false, materials.BlueDoor, walls.transform);
            DoorMarker("Upper Center Blue Door", 0f, 12.42f, UpperY, true, materials.BlueDoor, walls.transform);
            DoorMarker("Upper Left Elevator Door", -21f, 12.42f, UpperY, true, materials.ElevatorDoor, walls.transform);
            DoorMarker("Upper Right Elevator Door", 21f, 12.42f, UpperY, true, materials.ElevatorDoor, walls.transform);

            BuildStairRun("Left Hall Stair", -4.1f, -6.4f, 2.35f, 4.4f, stairs.transform, materials);
            BuildStairRun("Right Hall Stair", 7.1f, -6.4f, 2.35f, 4.2f, stairs.transform, materials);

            RailingZ("Upper Front Rail Far Left", 2.15f, -15f, -6.8f, UpperY, RailHeight, rails.transform, RailThickness);
            RailingZ("Upper Front Rail Center", 2.15f, -1.8f, 4.9f, UpperY, RailHeight, rails.transform, RailThickness);
            RailingZ("Upper Front Rail Far Right", 2.15f, 9.3f, 15f, UpperY, RailHeight, rails.transform, RailThickness);
            RailingX("Upper Left Gallery Rail", -15f, -6f, 2.15f, UpperY, RailHeight, rails.transform, RailThickness);
            RailingX("Upper Right Gallery Rail", 15f, -6f, 2.15f, UpperY, RailHeight, rails.transform, RailThickness);

            CeilingPanel("Lower Gallery Underside Ceiling", -15f, 15f, 2.2f, 12.5f, UpperY - 0.02f, materials, floors.transform);
            CeilingPanel("Upper Dark Wolf Ceiling", -27f, 27f, -16f, 12.5f, UpperY + WallHeight + 0.03f, materials, floors.transform);

            WallQuad("Lower Right Banner", new Vector3(14.82f, 2.08f, -9.8f), new Vector2(1.25f, 2.05f), Quaternion.Euler(0f, -90f, 0f), materials.Banner, decor.transform);
            WallQuad("Upper Center Portrait", new Vector3(3.2f, UpperY + 1.98f, 12.18f), new Vector2(1.35f, 1.55f), Quaternion.Euler(0f, 180f, 0f), materials.Portrait, decor.transform);
            WallQuad("Lower Left Portrait", new Vector3(-14.82f, 1.98f, -8.2f), new Vector2(1.35f, 1.55f), Quaternion.Euler(0f, 90f, 0f), materials.Portrait, decor.transform);
            WallQuad("Upper Left Rail Banner", new Vector3(-6.5f, UpperY + 1.48f, 2.02f), new Vector2(1.05f, 1.8f), Quaternion.identity, materials.Banner, decor.transform);
            WallQuad("Upper Center Rail Banner", new Vector3(1.6f, UpperY + 1.48f, 2.02f), new Vector2(1.05f, 1.8f), Quaternion.identity, materials.Banner, decor.transform);
            WallQuad("Upper Right Rail Banner", new Vector3(11.0f, UpperY + 1.48f, 2.02f), new Vector2(1.05f, 1.8f), Quaternion.identity, materials.Banner, decor.transform);
            WallQuad("Upper Wood Wall Portrait", new Vector3(-7.8f, UpperY + 1.95f, 12.18f), new Vector2(1.25f, 1.45f), Quaternion.Euler(0f, 180f, 0f), materials.Portrait, decor.transform);
            CreateChandelier("Main Hall Chandelier", new Vector3(0f, 3.2f, -6.2f), decor.transform, materials, 1.05f);
            CreateChandelier("Upper Gallery Chandelier", new Vector3(0f, UpperY + 2.35f, 7.3f), decor.transform, materials, 0.75f);
            CreateTable("Right Hall Table", new Vector3(12.4f, 0.45f, -9.4f), decor.transform, materials);
            CreateBillboard("Lower Potted Plant", new Vector3(-12.5f, 0.78f, -10.2f), new Vector2(1.05f, 1.55f), GetStaticMaterial(25, materials), decor.transform, true);
        }

        private static void BuildStairRun(string name, float centerX, float startZ, float endZ, float width, Transform parent, BlockoutMaterials materials)
        {
            const int steps = 26;
            float stepRun = (endZ - startZ) / steps;

            for (int i = 1; i <= steps; i++)
            {
                float topY = UpperY * i / steps;
                float z = startZ + (i - 0.5f) * stepRun;
                Cube(
                    name + " Step " + i.ToString("00"),
                    new Vector3(centerX, topY * 0.5f, z),
                    new Vector3(width, topY, stepRun),
                    materials.Stair,
                    parent,
                    true,
                    true);
            }

            RailingX(name + " Left Rail", centerX - width * 0.5f - 0.24f, startZ, endZ, 0f, RailHeight, parent, RailThickness);
            RailingX(name + " Right Rail", centerX + width * 0.5f + 0.24f, startZ, endZ, 0f, RailHeight, parent, RailThickness);
        }

        private static SourceLevel LoadSourceLevel()
        {
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(WolfRepoLevelPath);
            if (asset == null)
            {
                throw new FileNotFoundException("Missing Wolf3D source level", WolfRepoLevelPath);
            }

            SourceLevel level = JsonUtility.FromJson<SourceLevel>(asset.text);
            if (level == null || level.walls == null || level.walls.Length != MapSize * MapSize)
            {
                throw new InvalidDataException("Invalid Wolf3D level data: " + WolfRepoLevelPath);
            }

            return level;
        }

        private static bool IsInsideMap(int x, int y)
        {
            return x >= 0 && x < MapSize && y >= 0 && y < MapSize;
        }

        private static bool IsInRect(int x, int y, int rectX, int rectY, int width, int height)
        {
            return x >= rectX && x < rectX + width && y >= rectY && y < rectY + height;
        }

        private static void CopyWolfModule(
            SourceLevel level,
            string name,
            int sourceX,
            int sourceY,
            int width,
            int height,
            int destGridX,
            int destGridY,
            float yBase,
            Transform parent,
            BlockoutMaterials materials)
        {
            GameObject module = CreateChild(name, parent);
            Transform root = module.transform;
            float worldX = destGridX * Cell;
            float worldZ = destGridY * Cell;

            Floor(
                name + " Floor",
                worldX,
                worldX + width * Cell,
                worldZ,
                worldZ + height * Cell,
                yBase,
                materials.Floor,
                root);
            CeilingPanel(
                name + " Ceiling",
                worldX,
                worldX + width * Cell,
                worldZ,
                worldZ + height * Cell,
                yBase + WallHeight,
                materials,
                root);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int mapX = sourceX + x;
                    int mapY = sourceY + y;
                    if (!IsInsideMap(mapX, mapY))
                    {
                        continue;
                    }

                    int wallValue = level.walls[mapY * MapSize + mapX];
                    if (wallValue <= 0)
                    {
                        continue;
                    }

                    Material material = GetWallMaterial(wallValue, materials);
                    Vector3 position = new Vector3(worldX + x * Cell + Cell * 0.5f, yBase + WallHeight * 0.5f, worldZ + y * Cell + Cell * 0.5f);
                    CellCube(
                        $"Wall {mapX:00},{mapY:00} Type {wallValue}",
                        position,
                        new Vector3(Cell, WallHeight, Cell),
                        material,
                        root,
                        true);
                }
            }

            if (level.doors != null)
            {
                foreach (SourceDoor door in level.doors)
                {
                    if (!IsInRect(door.x, door.y, sourceX, sourceY, width, height))
                    {
                        continue;
                    }

                    int x = door.x - sourceX;
                    int y = door.y - sourceY;
                    Vector3 position = new Vector3(worldX + x * Cell + Cell * 0.5f, yBase + WallHeight * 0.5f, worldZ + y * Cell + Cell * 0.5f);
                    Vector3 size = door.vertical
                        ? new Vector3(DoorThickness, WallHeight, Cell)
                        : new Vector3(Cell, WallHeight, DoorThickness);
                    Material material = door.type == "elevator" ? materials.ElevatorDoor : materials.BlueDoor;
                    GameObject doorObject = CellCube($"Door {door.type} {door.x:00},{door.y:00}", position, size, material, root, true);
                    WolfDoor wolfDoor = doorObject.AddComponent<WolfDoor>();
                    wolfDoor.Configure(door.vertical ? Vector3.forward * (Cell - 0.04f) : Vector3.right * (Cell - 0.04f), 2.7f, 5f, false);
                }
            }

            CopyStatics(level, sourceX, sourceY, width, height, worldX, worldZ, yBase, root, materials);
            CopyEnemies(level, sourceX, sourceY, width, height, worldX, worldZ, yBase, root, materials);
        }

        private static void CopyStatics(SourceLevel level, int sourceX, int sourceY, int width, int height, float worldX, float worldZ, float yBase, Transform parent, BlockoutMaterials materials)
        {
            if (level.statics == null)
            {
                return;
            }

            foreach (SourceStatic staticData in level.statics)
            {
                if (!IsInRect(staticData.x, staticData.y, sourceX, sourceY, width, height))
                {
                    continue;
                }

                Vector3 position = new Vector3(
                    worldX + (staticData.x - sourceX) * Cell + Cell * 0.5f,
                    yBase,
                    worldZ + (staticData.y - sourceY) * Cell + Cell * 0.5f);

                if (staticData.typeIndex == 14 || staticData.typeIndex == 4)
                {
                    CreateChandelier("Copied Ceiling Light " + staticData.x + "," + staticData.y, position + Vector3.up * (WallHeight - 0.45f), parent, materials, 0.45f);
                    continue;
                }

                Material material = GetStaticMaterial(staticData.typeIndex, materials);
                CreateBillboard(
                    "Copied Static " + staticData.typeIndex + " " + staticData.x + "," + staticData.y,
                    position + Vector3.up * 0.74f,
                    new Vector2(0.95f, 1.35f),
                    material,
                    parent,
                    IsBlockingStatic(staticData.typeIndex));
            }
        }

        private static void CopyEnemies(SourceLevel level, int sourceX, int sourceY, int width, int height, float worldX, float worldZ, float yBase, Transform parent, BlockoutMaterials materials)
        {
            if (level.enemies == null)
            {
                return;
            }

            foreach (SourceEnemy enemy in level.enemies)
            {
                if (!IsInRect(enemy.x, enemy.y, sourceX, sourceY, width, height))
                {
                    continue;
                }

                Vector3 position = new Vector3(
                    worldX + (enemy.x - sourceX) * Cell + Cell * 0.5f,
                    yBase + 1.05f,
                    worldZ + (enemy.y - sourceY) * Cell + Cell * 0.5f);
                CreateBillboard("Copied Guard " + enemy.x + "," + enemy.y, position, new Vector2(1.25f, 2.1f), materials.Guard, parent, true);
            }
        }

        private static void BuildStandardStairLink(Transform parent, BlockoutMaterials materials)
        {
            GameObject stairs = CreateChild("Copied Wolf3D Central Stair Link", parent);
            const int steps = 18;
            const float totalRise = UpperY;
            const float stepRun = 0.44f;
            const float stairWidth = 5.4f;
            Vector3 start = new Vector3(0f, 0f, -0.5f);

            Floor("Lower Stair Connector Floor", start.x - 5.5f, start.x + 5.5f, -4.0f, 1.2f, 0f, materials.Floor, stairs.transform);
            CeilingPanel("Lower Stair Connector Ceiling", start.x - 5.5f, start.x + 5.5f, -4.0f, 1.2f, WallHeight, materials, stairs.transform);

            for (int i = 1; i <= steps; i++)
            {
                float topY = totalRise * i / steps;
                float z = start.z + (i - 0.5f) * stepRun;
                Cube(
                    "Standard Stair Step " + i.ToString("00"),
                    new Vector3(start.x, topY * 0.5f, z),
                    new Vector3(stairWidth, topY, stepRun),
                    materials.Stair,
                    stairs.transform,
                    true,
                    true);
            }

            RailingX("Standard Stair Left Rail", start.x - stairWidth * 0.5f - 0.18f, start.z, start.z + steps * stepRun, 0f, RailHeight, stairs.transform, RailThickness);
            RailingX("Standard Stair Right Rail", start.x + stairWidth * 0.5f + 0.18f, start.z, start.z + steps * stepRun, 0f, RailHeight, stairs.transform, RailThickness);
            Floor("Upper Stair Landing Patch", start.x - stairWidth * 0.5f, start.x + stairWidth * 0.5f, start.z + steps * stepRun, start.z + steps * stepRun + 2.8f, UpperY, materials.Floor, stairs.transform);
        }

        private static void BuildLowerFloor(Transform parent, BlockoutMaterials materials)
        {
            GameObject floors = CreateChild("Floor Slabs", parent);
            GameObject walls = CreateChild("Stone And Wood Walls", parent);
            GameObject rails = CreateChild("Low Loggia Rails", parent);

            Floor("Lower Main Hall Floor", -12f, 12f, -13f, 7f, 0f, materials.Floor, floors.transform);
            Floor("Lower North Rooms Floor", -18f, 18f, 7f, 19f, 0f, materials.Floor, floors.transform);
            Floor("Lower Left Wing Floor", -28f, -18f, -7f, 10f, 0f, materials.Floor, floors.transform);
            Floor("Lower Left Link Floor", -18f, -12f, -7f, 7f, 0f, materials.Floor, floors.transform);
            Floor("Lower Right Wing Floor", 18f, 28f, -7f, 10f, 0f, materials.Floor, floors.transform);
            Floor("Lower Right Link Floor", 12f, 18f, -7f, 7f, 0f, materials.Floor, floors.transform);
            Floor("Lower Left Loggia Floor", -36f, -28f, -5f, 8f, 0f, materials.FloorLight, floors.transform);
            Floor("Lower Right Loggia Floor", 28f, 36f, -5f, 8f, 0f, materials.FloorLight, floors.transform);
            Floor("Lower Entry Porch", -5f, 5f, -17f, -13f, 0f, materials.FloorLight, floors.transform);

            WallZ("Lower South Wall Left", -13f, -12f, -2.2f, 0f, WallHeight, materials.Stone, walls.transform);
            WallZ("Lower South Wall Right", -13f, 2.2f, 12f, 0f, WallHeight, materials.Stone, walls.transform);
            WallZ("Lower Porch Rail", -17f, -5f, 5f, 0f, RailHeight, materials.Stone, rails.transform, RailThickness);
            WallX("Lower Porch Left Rail", -5f, -17f, -13f, 0f, RailHeight, materials.Stone, rails.transform, RailThickness);
            WallX("Lower Porch Right Rail", 5f, -17f, -13f, 0f, RailHeight, materials.Stone, rails.transform, RailThickness);

            WallX("Lower Main West Front Wall", -12f, -13f, -7f, 0f, WallHeight, materials.Stone, walls.transform);
            WallX("Lower Main West Rear Wall", -12f, 3.2f, 7f, 0f, WallHeight, materials.Stone, walls.transform);
            WallX("Lower Main East Front Wall", 12f, -13f, -7f, 0f, WallHeight, materials.Stone, walls.transform);
            WallX("Lower Main East Rear Wall", 12f, 3.2f, 7f, 0f, WallHeight, materials.Stone, walls.transform);

            WallZ("Lower Hall North Left Partition", 7f, -12f, -4f, 0f, WallHeight, materials.Stone, walls.transform);
            WallZ("Lower Hall North Right Partition", 7f, 4f, 12f, 0f, WallHeight, materials.Stone, walls.transform);
            WallX("Lower Stair Core Left", -4f, -0.6f, 7.2f, 0f, WallHeight, materials.Stone, walls.transform);
            WallX("Lower Stair Core Right", 4f, -0.6f, 7.2f, 0f, WallHeight, materials.Stone, walls.transform);
            WallZ("Lower Stair Core Back", 7.2f, -4f, 4f, 0f, WallHeight, materials.Stone, walls.transform);

            WallZ("Lower North Back Exterior", 19f, -18f, 18f, 0f, WallHeight, materials.Stone, walls.transform);
            WallX("Lower North West Exterior", -18f, 7f, 19f, 0f, WallHeight, materials.Stone, walls.transform);
            WallX("Lower North East Exterior", 18f, 7f, 19f, 0f, WallHeight, materials.Stone, walls.transform);
            WallX("Lower North Center Divider Left", -6f, 7f, 15.8f, 0f, WallHeight, materials.Wood, walls.transform);
            WallX("Lower North Center Divider Right", 6f, 7f, 15.8f, 0f, WallHeight, materials.Wood, walls.transform);
            WallZ("Lower North Room Split West", 13.2f, -18f, -8.5f, 0f, WallHeight, materials.Wood, walls.transform);
            WallZ("Lower North Room Split East", 13.2f, 8.5f, 18f, 0f, WallHeight, materials.Wood, walls.transform);

            WallX("Lower Left Wing Exterior", -28f, -7f, 10f, 0f, WallHeight, materials.Stone, walls.transform);
            WallZ("Lower Left Wing South Exterior", -7f, -28f, -12f, 0f, WallHeight, materials.Stone, walls.transform);
            WallZ("Lower Left Wing North Exterior", 10f, -28f, -18f, 0f, WallHeight, materials.Stone, walls.transform);
            WallX("Lower Left Wing North Step Wall", -18f, 7f, 10f, 0f, WallHeight, materials.Stone, walls.transform);
            WallZ("Lower Left Wing Inner Partition A", -1f, -28f, -21f, 0f, WallHeight, materials.Wood, walls.transform);
            WallZ("Lower Left Wing Inner Partition B", -1f, -18.2f, -12f, 0f, WallHeight, materials.Wood, walls.transform);
            WallX("Lower Left Wing Room Divider", -20.5f, -7f, -1f, 0f, WallHeight, materials.Wood, walls.transform);

            WallX("Lower Right Wing Exterior", 28f, -7f, 10f, 0f, WallHeight, materials.Stone, walls.transform);
            WallZ("Lower Right Wing South Exterior", -7f, 12f, 28f, 0f, WallHeight, materials.Stone, walls.transform);
            WallZ("Lower Right Wing North Exterior", 10f, 18f, 28f, 0f, WallHeight, materials.Stone, walls.transform);
            WallX("Lower Right Wing North Step Wall", 18f, 7f, 10f, 0f, WallHeight, materials.Stone, walls.transform);
            WallZ("Lower Right Wing Inner Partition A", -1f, 12f, 18.2f, 0f, WallHeight, materials.Wood, walls.transform);
            WallZ("Lower Right Wing Inner Partition B", -1f, 21f, 28f, 0f, WallHeight, materials.Wood, walls.transform);
            WallX("Lower Right Wing Room Divider", 20.5f, -7f, -1f, 0f, WallHeight, materials.Wood, walls.transform);

            WallZ("Lower Left Loggia Front Rail", -5f, -36f, -28f, 0f, RailHeight, materials.Stone, rails.transform, RailThickness);
            WallZ("Lower Left Loggia Back Rail", 8f, -36f, -28f, 0f, RailHeight, materials.Stone, rails.transform, RailThickness);
            WallX("Lower Left Loggia Outer Rail", -36f, -5f, 8f, 0f, RailHeight, materials.Stone, rails.transform, RailThickness);
            WallZ("Lower Right Loggia Front Rail", -5f, 28f, 36f, 0f, RailHeight, materials.Stone, rails.transform, RailThickness);
            WallZ("Lower Right Loggia Back Rail", 8f, 28f, 36f, 0f, RailHeight, materials.Stone, rails.transform, RailThickness);
            WallX("Lower Right Loggia Outer Rail", 36f, -5f, 8f, 0f, RailHeight, materials.Stone, rails.transform, RailThickness);

            DoorMarker("Lower Main Entrance Marker", 0f, -13.08f, 0f, true, materials.DoorMarker, parent);
            DoorMarker("Lower Left Wing Blue Door Marker", -12.1f, -3.6f, 0f, false, materials.BlueDoor, parent);
            DoorMarker("Lower Right Wing Blue Door Marker", 12.1f, -3.6f, 0f, false, materials.BlueDoor, parent);
            DoorMarker("Lower North Dining Door Marker", -2.8f, 7.1f, 0f, true, materials.DoorMarker, parent);
            DoorMarker("Lower North Service Door Marker", 2.8f, 7.1f, 0f, true, materials.DoorMarker, parent);
        }

        private static void BuildUpperFloor(Transform parent, BlockoutMaterials materials)
        {
            GameObject floors = CreateChild("Floor Slabs", parent);
            GameObject walls = CreateChild("Stone And Wood Walls", parent);
            GameObject rails = CreateChild("Atrium And Balcony Rails", parent);

            Floor("Upper South Gallery", -18f, 18f, -7f, -1f, UpperY, materials.Floor, floors.transform);
            Floor("Upper West Gallery", -18f, -5f, -1f, 16f, UpperY, materials.Floor, floors.transform);
            Floor("Upper East Gallery", 5f, 18f, -1f, 16f, UpperY, materials.Floor, floors.transform);
            Floor("Upper North Gallery", -18f, 18f, 8f, 16f, UpperY, materials.Floor, floors.transform);
            Floor("Upper Left Rooms", -30f, -18f, -3f, 13f, UpperY, materials.Floor, floors.transform);
            Floor("Upper Right Rooms", 18f, 30f, -3f, 13f, UpperY, materials.Floor, floors.transform);
            Floor("Upper North Rooms", -12f, 12f, 16f, 22f, UpperY, materials.Floor, floors.transform);
            Floor("Upper Left Loggia", -36f, -30f, -2f, 10f, UpperY, materials.FloorLight, floors.transform);
            Floor("Upper Right Loggia", 30f, 36f, -2f, 10f, UpperY, materials.FloorLight, floors.transform);

            WallZ("Upper South Exterior Left", -7f, -18f, -2f, UpperY, WallHeight, materials.Stone, walls.transform);
            WallZ("Upper South Exterior Right", -7f, 2f, 18f, UpperY, WallHeight, materials.Stone, walls.transform);
            WallX("Upper West Exterior", -18f, -7f, -3f, UpperY, WallHeight, materials.Stone, walls.transform);
            WallX("Upper East Exterior", 18f, -7f, -3f, UpperY, WallHeight, materials.Stone, walls.transform);
            WallZ("Upper North Exterior", 22f, -12f, 12f, UpperY, WallHeight, materials.Stone, walls.transform);
            WallX("Upper North West Exterior", -12f, 16f, 22f, UpperY, WallHeight, materials.Stone, walls.transform);
            WallX("Upper North East Exterior", 12f, 16f, 22f, UpperY, WallHeight, materials.Stone, walls.transform);

            WallX("Upper Left Rooms Exterior", -30f, -3f, 13f, UpperY, WallHeight, materials.Stone, walls.transform);
            WallZ("Upper Left Rooms South Exterior", -3f, -30f, -18f, UpperY, WallHeight, materials.Stone, walls.transform);
            WallZ("Upper Left Rooms North Exterior", 13f, -30f, -18f, UpperY, WallHeight, materials.Stone, walls.transform);
            WallX("Upper Right Rooms Exterior", 30f, -3f, 13f, UpperY, WallHeight, materials.Stone, walls.transform);
            WallZ("Upper Right Rooms South Exterior", -3f, 18f, 30f, UpperY, WallHeight, materials.Stone, walls.transform);
            WallZ("Upper Right Rooms North Exterior", 13f, 18f, 30f, UpperY, WallHeight, materials.Stone, walls.transform);

            WallX("Upper Left Room Divider", -24f, -3f, 13f, UpperY, WallHeight, materials.Wood, walls.transform);
            WallZ("Upper Left Room Cross Divider", 5f, -30f, -24f, UpperY, WallHeight, materials.Wood, walls.transform);
            WallX("Upper Right Room Divider", 24f, -3f, 13f, UpperY, WallHeight, materials.Wood, walls.transform);
            WallZ("Upper Right Room Cross Divider", 5f, 24f, 30f, UpperY, WallHeight, materials.Wood, walls.transform);
            WallX("Upper North Room Divider Left", -4f, 16f, 22f, UpperY, WallHeight, materials.Wood, walls.transform);
            WallX("Upper North Room Divider Right", 4f, 16f, 22f, UpperY, WallHeight, materials.Wood, walls.transform);

            WallX("Upper Atrium Rail West", -5f, -1f, 8f, UpperY, RailHeight, materials.Rail, rails.transform, RailThickness);
            WallX("Upper Atrium Rail East", 5f, -1f, 8f, UpperY, RailHeight, materials.Rail, rails.transform, RailThickness);
            WallZ("Upper Atrium Rail South", -1f, -5f, 5f, UpperY, RailHeight, materials.Rail, rails.transform, RailThickness);
            WallZ("Upper Atrium Rail North Left", 8f, -5f, -1.8f, UpperY, RailHeight, materials.Rail, rails.transform, RailThickness);
            WallZ("Upper Atrium Rail North Right", 8f, 1.8f, 5f, UpperY, RailHeight, materials.Rail, rails.transform, RailThickness);

            WallZ("Upper Left Loggia Front Rail", -2f, -36f, -30f, UpperY, RailHeight, materials.Stone, rails.transform, RailThickness);
            WallZ("Upper Left Loggia Back Rail", 10f, -36f, -30f, UpperY, RailHeight, materials.Stone, rails.transform, RailThickness);
            WallX("Upper Left Loggia Outer Rail", -36f, -2f, 10f, UpperY, RailHeight, materials.Stone, rails.transform, RailThickness);
            WallZ("Upper Right Loggia Front Rail", -2f, 30f, 36f, UpperY, RailHeight, materials.Stone, rails.transform, RailThickness);
            WallZ("Upper Right Loggia Back Rail", 10f, 30f, 36f, UpperY, RailHeight, materials.Stone, rails.transform, RailThickness);
            WallX("Upper Right Loggia Outer Rail", 36f, -2f, 10f, UpperY, RailHeight, materials.Stone, rails.transform, RailThickness);

            DoorMarker("Upper South Door Marker", 0f, -7.08f, UpperY, true, materials.DoorMarker, parent);
            DoorMarker("Upper Left Blue Door Marker", -18.1f, 1.6f, UpperY, false, materials.BlueDoor, parent);
            DoorMarker("Upper Right Blue Door Marker", 18.1f, 1.6f, UpperY, false, materials.BlueDoor, parent);
        }

        private static void BuildVerticalLinks(Transform parent, BlockoutMaterials materials)
        {
            GameObject stairs = CreateChild("Central Staircase", parent);

            const int steps = 28;
            const float totalRise = UpperY;
            const float stepRun = 0.34f;
            const float stairWidth = 4.3f;
            const float startZ = -1.52f;

            for (int i = 1; i <= steps; i++)
            {
                float topY = totalRise * i / steps;
                float z = startZ + (i - 0.5f) * stepRun;
                Cube(
                    "Main Stair Step " + i.ToString("00"),
                    new Vector3(0f, topY * 0.5f, z),
                    new Vector3(stairWidth, topY, stepRun),
                    materials.Stair,
                    stairs.transform,
                    true,
                    true);
            }

            WallX("Stair Rail Left", -2.35f, -1.2f, 8.0f, 0f, RailHeight, materials.Rail, stairs.transform, RailThickness);
            WallX("Stair Rail Right", 2.35f, -1.2f, 8.0f, 0f, RailHeight, materials.Rail, stairs.transform, RailThickness);
            WallX("Lower Stair Side Wall Left", -3.2f, -1.2f, 3.2f, 0f, 1.45f, materials.Stone, stairs.transform, RailThickness);
            WallX("Lower Stair Side Wall Right", 3.2f, -1.2f, 3.2f, 0f, 1.45f, materials.Stone, stairs.transform, RailThickness);
        }

        private static void BuildNavigationMarkers(Transform parent, BlockoutMaterials materials)
        {
            Cube("Spawn Direction Pad", new Vector3(0f, 0.04f, -10.5f), new Vector3(1.6f, 0.05f, 1.6f), materials.MarkerGreen, parent, false, false);
            Cube("Lower To Upper Route Marker", new Vector3(0f, 0.05f, 0.4f), new Vector3(2.0f, 0.06f, 2.0f), materials.MarkerBlue, parent, false, false);
            Cube("Upper Landing Marker", new Vector3(0f, UpperY + 0.05f, 8.8f), new Vector3(2.2f, 0.06f, 1.2f), materials.MarkerBlue, parent, false, false);
        }

        private static void BuildCeilings(Transform parent, BlockoutMaterials materials)
        {
            GameObject ceilings = CreateChild("05 Wolf3D Grey Ceilings", parent);

            CeilingPanel("Lower South Gallery Underside", -18f, 18f, -7f, -1f, UpperY - FloorThickness - 0.02f, materials, ceilings.transform);
            CeilingPanel("Lower West Gallery Underside", -18f, -5f, -1f, 16f, UpperY - FloorThickness - 0.02f, materials, ceilings.transform);
            CeilingPanel("Lower East Gallery Underside", 5f, 18f, -1f, 16f, UpperY - FloorThickness - 0.02f, materials, ceilings.transform);
            CeilingPanel("Lower North Gallery Underside", -18f, 18f, 8f, 16f, UpperY - FloorThickness - 0.02f, materials, ceilings.transform);
            CeilingPanel("Upper Continuous Grey Ceiling", -36f, 36f, -8f, 23f, UpperY + WallHeight + 0.05f, materials, ceilings.transform);
        }

        private static void BuildWolfDecor(Transform parent, BlockoutMaterials materials)
        {
            WallQuad("Lower Entry Banner", new Vector3(6.2f, 1.92f, -12.82f), new Vector2(1.2f, 2.05f), Quaternion.identity, materials.Banner, parent);
            WallQuad("Lower Left Portrait", new Vector3(-11.82f, 1.95f, -4.6f), new Vector2(1.35f, 1.55f), Quaternion.Euler(0f, 90f, 0f), materials.Portrait, parent);
            WallQuad("Lower North Portrait", new Vector3(-8.8f, 1.95f, 18.82f), new Vector2(1.35f, 1.55f), Quaternion.Euler(0f, 180f, 0f), materials.Portrait, parent);
            WallQuad("Upper South Banner", new Vector3(8.2f, UpperY + 1.92f, -6.82f), new Vector2(1.2f, 2.05f), Quaternion.identity, materials.Banner, parent);
            WallQuad("Upper North Portrait", new Vector3(0f, UpperY + 1.95f, 21.82f), new Vector2(1.35f, 1.55f), Quaternion.Euler(0f, 180f, 0f), materials.Portrait, parent);
            WallQuad("Upper Right Portrait", new Vector3(29.82f, UpperY + 1.95f, 3.8f), new Vector2(1.35f, 1.55f), Quaternion.Euler(0f, -90f, 0f), materials.Portrait, parent);

            CreateChandelier("Lower Hall Chandelier", new Vector3(0f, 3.35f, -5.6f), parent, materials, 1.15f);
            CreateChandelier("Upper Atrium Chandelier", new Vector3(0f, UpperY + 2.45f, 7.0f), parent, materials, 0.95f);
            CreateTable("Lower Right Table", new Vector3(9.5f, 0.45f, -8.5f), parent, materials);
            CreateTable("Upper North Table", new Vector3(-7.2f, UpperY + 0.45f, 17.8f), parent, materials);
        }

        private static void CreateHud()
        {
            GameObject hud = new GameObject("Wolf3D HUD");
            WolfHud wolfHud = hud.AddComponent<WolfHud>();

            SerializedObject serializedHud = new SerializedObject(wolfHud);
            serializedHud.FindProperty("faceTexture").objectReferenceValue = LoadTextureAsset(WolfRepoTextureFolder + "/bj.png");
            serializedHud.FindProperty("weaponTexture").objectReferenceValue = LoadTextureAsset(WolfRepoTextureFolder + "/attack.png");
            serializedHud.FindProperty("keyTexture").objectReferenceValue = LoadTextureAsset(WolfRepoTextureFolder + "/hudkeys.png");
            serializedHud.FindProperty("floor").intValue = 2;
            serializedHud.FindProperty("score").intValue = 102500;
            serializedHud.FindProperty("lives").intValue = 4;
            serializedHud.FindProperty("health").intValue = 100;
            serializedHud.FindProperty("ammo").intValue = 32;
            serializedHud.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreatePlayer()
        {
            GameObject player = new GameObject("Blockout Player");
            player.transform.position = new Vector3(0f, 0.12f, -12.4f);
            player.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

            CharacterController characterController = player.AddComponent<CharacterController>();
            characterController.height = 1.8f;
            characterController.radius = 0.35f;
            characterController.center = new Vector3(0f, 0.9f, 0f);
            characterController.stepOffset = 0.42f;
            characterController.slopeLimit = 55f;

            SimpleFirstPersonController controller = player.AddComponent<SimpleFirstPersonController>();
            WolfPlayerInteractor interactor = player.AddComponent<WolfPlayerInteractor>();

            GameObject cameraObject = new GameObject("First Person Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(player.transform);
            cameraObject.transform.localPosition = new Vector3(0f, 1.62f, 0f);
            cameraObject.transform.localRotation = Quaternion.identity;

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 74f;
            camera.nearClipPlane = 0.04f;
            camera.farClipPlane = 120f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.038f, 0.045f);
            cameraObject.AddComponent<AudioListener>();

            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("playerCamera").objectReferenceValue = camera;
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject serializedInteractor = new SerializedObject(interactor);
            serializedInteractor.FindProperty("playerCamera").objectReferenceValue = camera;
            serializedInteractor.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateLighting()
        {
            QualitySettings.shadowDistance = 90f;
            QualitySettings.antiAliasing = 4;

            GameObject directional = new GameObject("RTX Style Directional Light");
            directional.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
            Light directionalLight = directional.AddComponent<Light>();
            directionalLight.type = LightType.Directional;
            directionalLight.color = new Color(1f, 0.95f, 0.86f);
            directionalLight.intensity = 0.55f;
            directionalLight.shadows = LightShadows.Soft;
            directionalLight.shadowStrength = 0.65f;

            AddPointLight("Lower Warm Chandelier Fill", new Vector3(0f, 3.15f, -5.6f), new Color(1f, 0.73f, 0.36f), 2.8f, 24f);
            AddPointLight("Upper Warm Chandelier Fill", new Vector3(0f, UpperY + 2.9f, 7f), new Color(1f, 0.72f, 0.34f), 2.2f, 21f);
            AddPointLight("Left Wall Sconce Glow", new Vector3(-14.2f, 2.0f, -8.5f), new Color(1f, 0.58f, 0.20f), 1.25f, 10f);
            AddPointLight("Right Wall Sconce Glow", new Vector3(14.2f, 2.0f, -8.5f), new Color(1f, 0.58f, 0.20f), 1.25f, 10f);
            AddPointLight("Cool Door Reflection Fill", new Vector3(0f, 2.2f, -14.5f), new Color(0.18f, 0.82f, 0.86f), 0.85f, 16f);
            AddReflectionProbe("Main Hall Reflection Probe", new Vector3(0f, 2.4f, -2f), new Vector3(60f, 12f, 44f));
        }

        private static void AddPointLight(string name, Vector3 position, Color color, float intensity, float range)
        {
            GameObject lightObject = new GameObject(name);
            lightObject.transform.position = position;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.38f;
        }

        private static void AddReflectionProbe(string name, Vector3 position, Vector3 size)
        {
            GameObject probeObject = new GameObject(name);
            probeObject.transform.position = position;
            ReflectionProbe probe = probeObject.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
            probe.intensity = 1.35f;
            probe.size = size;
            probe.resolution = 256;
            probe.nearClipPlane = 0.1f;
            probe.farClipPlane = 90f;
        }

        private static void ConfigureRenderSettings()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.38f, 0.40f, 0.44f);
            RenderSettings.ambientEquatorColor = new Color(0.30f, 0.29f, 0.27f);
            RenderSettings.ambientGroundColor = new Color(0.16f, 0.16f, 0.16f);
            RenderSettings.reflectionIntensity = 1.1f;
            RenderSettings.defaultReflectionResolution = 256;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.18f, 0.19f, 0.20f);
            RenderSettings.fogDensity = 0.002f;
            RenderSettings.skybox = null;
            DynamicGI.UpdateEnvironment();
        }

        private static void CeilingPanel(string name, float xMin, float xMax, float zMin, float zMax, float y, BlockoutMaterials materials, Transform parent)
        {
            Cube(
                name,
                new Vector3((xMin + xMax) * 0.5f, y - CeilingThickness * 0.5f, (zMin + zMax) * 0.5f),
                new Vector3(xMax - xMin, CeilingThickness, zMax - zMin),
                materials.Ceiling,
                parent,
                true,
                false);
        }

        private static void Floor(string name, float xMin, float xMax, float zMin, float zMax, float yTop, Material material, Transform parent)
        {
            Cube(
                name,
                new Vector3((xMin + xMax) * 0.5f, yTop - FloorThickness * 0.5f, (zMin + zMax) * 0.5f),
                new Vector3(xMax - xMin, FloorThickness, zMax - zMin),
                material,
                parent,
                true,
                true);
        }

        private static void WallXBaseUpper(string name, float x, float zMin, float zMax, float yBase, float height, Material upperMaterial, Transform parent, float thickness = WallThickness)
        {
            if (zMax <= zMin)
            {
                return;
            }

            float baseHeight = Mathf.Min(StoneBaseHeight, height);
            Cube(
                name + " Grey Stone Base",
                new Vector3(x, yBase + baseHeight * 0.5f, (zMin + zMax) * 0.5f),
                new Vector3(thickness, baseHeight, zMax - zMin),
                activeMaterials.StoneBase,
                parent,
                true,
                true);

            float upperHeight = height - baseHeight;
            if (upperHeight <= 0.02f)
            {
                return;
            }

            Cube(
                name + " Upper Surface",
                new Vector3(x, yBase + baseHeight + upperHeight * 0.5f, (zMin + zMax) * 0.5f),
                new Vector3(thickness, upperHeight, zMax - zMin),
                upperMaterial,
                parent,
                true,
                true);
        }

        private static void WallZBaseUpper(string name, float z, float xMin, float xMax, float yBase, float height, Material upperMaterial, Transform parent, float thickness = WallThickness)
        {
            if (xMax <= xMin)
            {
                return;
            }

            float baseHeight = Mathf.Min(StoneBaseHeight, height);
            Cube(
                name + " Grey Stone Base",
                new Vector3((xMin + xMax) * 0.5f, yBase + baseHeight * 0.5f, z),
                new Vector3(xMax - xMin, baseHeight, thickness),
                activeMaterials.StoneBase,
                parent,
                true,
                true);

            float upperHeight = height - baseHeight;
            if (upperHeight <= 0.02f)
            {
                return;
            }

            Cube(
                name + " Upper Surface",
                new Vector3((xMin + xMax) * 0.5f, yBase + baseHeight + upperHeight * 0.5f, z),
                new Vector3(xMax - xMin, upperHeight, thickness),
                upperMaterial,
                parent,
                true,
                true);
        }

        private static void WallX(string name, float x, float zMin, float zMax, float yBase, float height, Material material, Transform parent, float thickness = WallThickness)
        {
            if (zMax <= zMin)
            {
                return;
            }

            if (activeMaterials != null && material == activeMaterials.Rail)
            {
                RailingX(name, x, zMin, zMax, yBase, height, parent, thickness);
                return;
            }

            if (activeMaterials != null && material == activeMaterials.Stone)
            {
                if (height > StoneBaseHeight + 0.25f)
                {
                    Cube(
                        name + " Grey Base",
                        new Vector3(x, yBase + StoneBaseHeight * 0.5f, (zMin + zMax) * 0.5f),
                        new Vector3(thickness, StoneBaseHeight, zMax - zMin),
                        activeMaterials.StoneBase,
                        parent,
                        true,
                        true);
                    Cube(
                        name + " Blue Upper",
                        new Vector3(x, yBase + StoneBaseHeight + (height - StoneBaseHeight) * 0.5f, (zMin + zMax) * 0.5f),
                        new Vector3(thickness, height - StoneBaseHeight, zMax - zMin),
                        activeMaterials.Stone,
                        parent,
                        true,
                        true);
                    return;
                }

                material = activeMaterials.StoneBase;
            }

            Cube(
                name,
                new Vector3(x, yBase + height * 0.5f, (zMin + zMax) * 0.5f),
                new Vector3(thickness, height, zMax - zMin),
                material,
                parent,
                true,
                true);
        }

        private static void WallZ(string name, float z, float xMin, float xMax, float yBase, float height, Material material, Transform parent, float thickness = WallThickness)
        {
            if (xMax <= xMin)
            {
                return;
            }

            if (activeMaterials != null && material == activeMaterials.Rail)
            {
                RailingZ(name, z, xMin, xMax, yBase, height, parent, thickness);
                return;
            }

            if (activeMaterials != null && material == activeMaterials.Stone)
            {
                if (height > StoneBaseHeight + 0.25f)
                {
                    Cube(
                        name + " Grey Base",
                        new Vector3((xMin + xMax) * 0.5f, yBase + StoneBaseHeight * 0.5f, z),
                        new Vector3(xMax - xMin, StoneBaseHeight, thickness),
                        activeMaterials.StoneBase,
                        parent,
                        true,
                        true);
                    Cube(
                        name + " Blue Upper",
                        new Vector3((xMin + xMax) * 0.5f, yBase + StoneBaseHeight + (height - StoneBaseHeight) * 0.5f, z),
                        new Vector3(xMax - xMin, height - StoneBaseHeight, thickness),
                        activeMaterials.Stone,
                        parent,
                        true,
                        true);
                    return;
                }

                material = activeMaterials.StoneBase;
            }

            Cube(
                name,
                new Vector3((xMin + xMax) * 0.5f, yBase + height * 0.5f, z),
                new Vector3(xMax - xMin, height, thickness),
                material,
                parent,
                true,
                true);
        }

        private static void RailingX(string name, float x, float zMin, float zMax, float yBase, float height, Transform parent, float thickness)
        {
            float length = zMax - zMin;
            Cube(
                name + " Top Rail",
                new Vector3(x, yBase + height, (zMin + zMax) * 0.5f),
                new Vector3(thickness * 1.45f, 0.16f, length),
                activeMaterials.Rail,
                parent,
                true,
                true);
            Cube(
                name + " Foot Rail",
                new Vector3(x, yBase + 0.24f, (zMin + zMax) * 0.5f),
                new Vector3(thickness * 1.10f, 0.10f, length),
                activeMaterials.StoneBase,
                parent,
                true,
                true);

            int posts = Mathf.Max(2, Mathf.CeilToInt(length / 1.1f) + 1);
            for (int i = 0; i < posts; i++)
            {
                float t = posts == 1 ? 0f : i / (float)(posts - 1);
                float z = Mathf.Lerp(zMin, zMax, t);
                Cube(
                    name + " Post " + i.ToString("00"),
                    new Vector3(x, yBase + height * 0.5f, z),
                    new Vector3(thickness * 1.55f, height, 0.16f),
                    activeMaterials.StoneBase,
                    parent,
                    true,
                    true);
            }
        }

        private static void RailingZ(string name, float z, float xMin, float xMax, float yBase, float height, Transform parent, float thickness)
        {
            float length = xMax - xMin;
            Cube(
                name + " Top Rail",
                new Vector3((xMin + xMax) * 0.5f, yBase + height, z),
                new Vector3(length, 0.16f, thickness * 1.45f),
                activeMaterials.Rail,
                parent,
                true,
                true);
            Cube(
                name + " Foot Rail",
                new Vector3((xMin + xMax) * 0.5f, yBase + 0.24f, z),
                new Vector3(length, 0.10f, thickness * 1.10f),
                activeMaterials.StoneBase,
                parent,
                true,
                true);

            int posts = Mathf.Max(2, Mathf.CeilToInt(length / 1.1f) + 1);
            for (int i = 0; i < posts; i++)
            {
                float t = posts == 1 ? 0f : i / (float)(posts - 1);
                float x = Mathf.Lerp(xMin, xMax, t);
                Cube(
                    name + " Post " + i.ToString("00"),
                    new Vector3(x, yBase + height * 0.5f, z),
                    new Vector3(0.16f, height, thickness * 1.55f),
                    activeMaterials.StoneBase,
                    parent,
                    true,
                    true);
            }
        }

        private static void DoorMarker(string name, float x, float z, float yBase, bool onZWall, Material material, Transform parent)
        {
            Vector3 scale = onZWall
                ? new Vector3(1.55f, 2.15f, 0.08f)
                : new Vector3(0.08f, 2.15f, 1.55f);
            Cube(name, new Vector3(x, yBase + 1.08f, z), scale, material, parent, false, false);
        }

        private static GameObject WallQuad(string name, Vector3 position, Vector2 scale, Quaternion rotation, Material material, Transform parent)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            quad.transform.position = position;
            quad.transform.rotation = rotation;
            quad.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            quad.transform.SetParent(parent, true);
            quad.isStatic = true;

            Collider collider = quad.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            Renderer renderer = quad.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            return quad;
        }

        private static GameObject CreateBillboard(string name, Vector3 position, Vector2 scale, Material material, Transform parent, bool blocking)
        {
            GameObject billboard = WallQuad(name, position, scale, Quaternion.identity, material, parent);
            billboard.AddComponent<WolfBillboard>();

            if (blocking)
            {
                BoxCollider collider = billboard.AddComponent<BoxCollider>();
                collider.size = new Vector3(0.75f, 1.55f, 0.75f);
                collider.center = Vector3.zero;
            }

            return billboard;
        }

        private static bool IsBlockingStatic(int typeIndex)
        {
            switch (typeIndex)
            {
                case 1:
                case 2:
                case 3:
                case 5:
                case 7:
                case 8:
                case 10:
                case 11:
                case 12:
                case 13:
                case 16:
                case 17:
                case 18:
                case 22:
                case 35:
                case 36:
                    return true;
                default:
                    return false;
            }
        }

        private static void CreateChandelier(string name, Vector3 position, Transform parent, BlockoutMaterials materials, float scale)
        {
            GameObject root = CreateChild(name, parent);
            root.transform.position = position;

            Cylinder(name + " Stem", position + Vector3.up * 0.42f * scale, 0.035f * scale, 0.9f * scale, materials.Brass, root.transform, false);
            Sphere(name + " Center", position, 0.22f * scale, materials.Brass, root.transform, false);

            for (int i = 0; i < 6; i++)
            {
                float angle = i * Mathf.PI * 2f / 6f;
                Vector3 arm = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 0.55f * scale;
                Cube(name + " Arm " + i.ToString("00"), position + arm * 0.5f, new Vector3(0.08f * scale, 0.05f * scale, 0.64f * scale), materials.Brass, root.transform, false, false)
                    .transform.rotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg + 90f, 0f);
                Sphere(name + " Bulb " + i.ToString("00"), position + arm + Vector3.down * 0.05f * scale, 0.12f * scale, materials.LampGlow, root.transform, false);
            }

            GameObject lightObject = new GameObject(name + " Light");
            lightObject.transform.position = position + Vector3.down * 0.1f;
            lightObject.transform.SetParent(root.transform, true);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.84f, 0.38f);
            light.intensity = 2.4f;
            light.range = 12f;
        }

        private static void CreateTable(string name, Vector3 position, Transform parent, BlockoutMaterials materials)
        {
            GameObject root = CreateChild(name, parent);
            Cube(name + " Top", position + Vector3.up * 0.28f, new Vector3(2.2f, 0.16f, 1.05f), materials.Wood, root.transform, true, true);
            for (int ix = -1; ix <= 1; ix += 2)
            {
                for (int iz = -1; iz <= 1; iz += 2)
                {
                    Cube(
                        name + " Leg " + ix + "," + iz,
                        position + new Vector3(ix * 0.86f, -0.02f, iz * 0.36f),
                        new Vector3(0.16f, 0.62f, 0.16f),
                        materials.Wood,
                        root.transform,
                        true,
                        true);
                }
            }
        }

        private static void Cylinder(string name, Vector3 position, float radius, float height, Material material, Transform parent, bool keepCollider)
        {
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            gameObject.name = name;
            gameObject.transform.position = position;
            gameObject.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
            gameObject.transform.SetParent(parent, true);
            gameObject.GetComponent<Renderer>().sharedMaterial = material;
            if (!keepCollider)
            {
                Collider collider = gameObject.GetComponent<Collider>();
                if (collider != null)
                {
                    UnityEngine.Object.DestroyImmediate(collider);
                }
            }
        }

        private static void Sphere(string name, Vector3 position, float radius, Material material, Transform parent, bool keepCollider)
        {
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            gameObject.name = name;
            gameObject.transform.position = position;
            gameObject.transform.localScale = Vector3.one * (radius * 2f);
            gameObject.transform.SetParent(parent, true);
            gameObject.GetComponent<Renderer>().sharedMaterial = material;
            if (!keepCollider)
            {
                Collider collider = gameObject.GetComponent<Collider>();
                if (collider != null)
                {
                    UnityEngine.Object.DestroyImmediate(collider);
                }
            }
        }

        private static GameObject Cube(string name, Vector3 position, Vector3 scale, Material material, Transform parent, bool isStatic = true, bool keepCollider = true)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.name = name;
            gameObject.transform.position = position;
            gameObject.transform.SetParent(parent, true);
            gameObject.isStatic = isStatic;

            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = CreateBoxMesh(scale, ShouldTileMaterial(material));

            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;

            if (keepCollider)
            {
                BoxCollider collider = gameObject.AddComponent<BoxCollider>();
                collider.size = scale;
            }

            return gameObject;
        }

        private static GameObject CellCube(string name, Vector3 position, Vector3 scale, Material material, Transform parent, bool keepCollider)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.position = position;
            gameObject.transform.SetParent(parent, true);
            gameObject.isStatic = true;

            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = CreateBoxMesh(scale, false);

            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;

            if (keepCollider)
            {
                BoxCollider collider = gameObject.AddComponent<BoxCollider>();
                collider.size = scale;
            }

            return gameObject;
        }

        private static bool ShouldTileMaterial(Material material)
        {
            if (activeMaterials == null)
            {
                return true;
            }

            return material != activeMaterials.DoorMarker
                   && material != activeMaterials.BlueDoor
                   && material != activeMaterials.Banner
                   && material != activeMaterials.Portrait
                   && material != activeMaterials.Brass
                   && material != activeMaterials.LampGlow
                   && material != activeMaterials.MarkerGreen
                   && material != activeMaterials.MarkerBlue
                   && material != activeMaterials.DarkVoid;
        }

        private static Mesh CreateBoxMesh(Vector3 size, bool tiled)
        {
            float hx = size.x * 0.5f;
            float hy = size.y * 0.5f;
            float hz = size.z * 0.5f;

            Vector3[] vertices =
            {
                new(-hx, -hy, -hz), new(-hx, hy, -hz), new(-hx, hy, hz), new(-hx, -hy, hz),
                new(hx, -hy, hz), new(hx, hy, hz), new(hx, hy, -hz), new(hx, -hy, -hz),
                new(-hx, hy, -hz), new(hx, hy, -hz), new(hx, hy, hz), new(-hx, hy, hz),
                new(-hx, -hy, hz), new(hx, -hy, hz), new(hx, -hy, -hz), new(-hx, -hy, -hz),
                new(-hx, -hy, hz), new(-hx, hy, hz), new(hx, hy, hz), new(hx, -hy, hz),
                new(hx, -hy, -hz), new(hx, hy, -hz), new(-hx, hy, -hz), new(-hx, -hy, -hz)
            };

            Vector2[] uvs = new Vector2[24];
            SetFaceUvs(uvs, 0, size.z, size.y, tiled);
            SetFaceUvs(uvs, 4, size.z, size.y, tiled);
            SetFaceUvs(uvs, 8, size.x, size.z, tiled);
            SetFaceUvs(uvs, 12, size.x, size.z, tiled);
            SetFaceUvs(uvs, 16, size.x, size.y, tiled);
            SetFaceUvs(uvs, 20, size.x, size.y, tiled);

            int[] triangles =
            {
                0, 1, 2, 0, 2, 3,
                4, 5, 6, 4, 6, 7,
                8, 9, 10, 8, 10, 11,
                12, 13, 14, 12, 14, 15,
                16, 17, 18, 16, 18, 19,
                20, 21, 22, 20, 22, 23
            };

            Mesh mesh = new Mesh
            {
                name = "Tiled Box Mesh"
            };
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void SetFaceUvs(Vector2[] uvs, int offset, float width, float height, bool tiled)
        {
            float u = tiled ? Mathf.Max(0.05f, width / TextureWorldUnits) : 1f;
            float v = tiled ? Mathf.Max(0.05f, height / TextureWorldUnits) : 1f;

            uvs[offset + 0] = new Vector2(0f, 0f);
            uvs[offset + 1] = new Vector2(0f, v);
            uvs[offset + 2] = new Vector2(u, v);
            uvs[offset + 3] = new Vector2(u, 0f);
        }

        private static GameObject CreateChild(string name, Transform parent)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent);
            return child;
        }

        private static BlockoutMaterials CreateMaterials()
        {
            Texture2D floorTexture = CreateModernAlbedo("Tex_BlockoutRTX_OldPaletteV4_PolishedMarbleFloor", ModernSurface.MarbleFloor);
            Texture2D floorNormal = CreateModernNormal("Tex_BlockoutRTX_OldPaletteV4_PolishedMarbleFloor_N", ModernSurface.MarbleFloor, 2.4f);
            Texture2D ceilingTexture = CreateModernAlbedo("Tex_BlockoutRTX_OldPaletteV4_BrightPlasterCeiling", ModernSurface.PlasterCeiling);
            Texture2D ceilingNormal = CreateModernNormal("Tex_BlockoutRTX_OldPaletteV4_BrightPlasterCeiling_N", ModernSurface.PlasterCeiling, 1.6f);
            Texture2D blueStoneTexture = CreateModernAlbedo("Tex_BlockoutRTX_OldPaletteV4_GlossyCobaltWall", ModernSurface.BlueTileWall);
            Texture2D blueStoneNormal = CreateModernNormal("Tex_BlockoutRTX_OldPaletteV4_GlossyCobaltWall_N", ModernSurface.BlueTileWall, 3.0f);
            Texture2D greyStoneTexture = CreateModernAlbedo("Tex_BlockoutRTX_OldPaletteV4_GreyStoneBlockBase", ModernSurface.GreyBlock);
            Texture2D greyStoneNormal = CreateModernNormal("Tex_BlockoutRTX_OldPaletteV4_GreyStoneBlockBase_N", ModernSurface.GreyBlock, 2.2f);
            Texture2D woodTexture = CreateModernAlbedo("Tex_BlockoutRTX_OldPaletteV4_WarmVarnishedWood", ModernSurface.WarmWood);
            Texture2D woodNormal = CreateModernNormal("Tex_BlockoutRTX_OldPaletteV4_WarmVarnishedWood_N", ModernSurface.WarmWood, 1.8f);
            Texture2D stairTexture = CreateModernAlbedo("Tex_BlockoutRTX_OldPaletteV4_GreyMarbleStair", ModernSurface.MarbleStair);
            Texture2D stairNormal = CreateModernNormal("Tex_BlockoutRTX_OldPaletteV4_GreyMarbleStair_N", ModernSurface.MarbleStair, 2.4f);
            Texture2D tealDoorTexture = CreateModernAlbedo("Tex_BlockoutRTX_OldPaletteV4_TurquoiseMetalDoor", ModernSurface.TealDoor);
            Texture2D tealDoorNormal = CreateModernNormal("Tex_BlockoutRTX_OldPaletteV4_TurquoiseMetalDoor_N", ModernSurface.TealDoor, 2.1f);
            Texture2D metalDoorTexture = CreateModernAlbedo("Tex_BlockoutRTX_OldPaletteV4_BrushedSteelDoor", ModernSurface.BrushedSteel);
            Texture2D metalDoorNormal = CreateModernNormal("Tex_BlockoutRTX_OldPaletteV4_BrushedSteelDoor_N", ModernSurface.BrushedSteel, 1.2f);
            Texture2D bannerTexture = ExtractWolfTile("Tex_BlockoutWolf3D_Banner_004", 4);
            Texture2D portraitTexture = ExtractWolfTile("Tex_BlockoutWolf3D_Portrait_020", 20);

            BlockoutMaterials materials = new BlockoutMaterials
            {
                Floor = CreatePbrMaterial("Mat_BlockoutRTX_PolishedMarbleFloor", floorTexture, floorNormal, Color.white, 0.02f, 0.86f, Vector2.one, 1.05f),
                FloorLight = CreatePbrMaterial("Mat_BlockoutRTX_PolishedMarbleFloorLight", floorTexture, floorNormal, Color.white, 0.02f, 0.90f, Vector2.one, 1.05f),
                Ceiling = CreatePbrMaterial("Mat_BlockoutRTX_BrightPlasterCeiling", ceilingTexture, ceilingNormal, Color.white, 0f, 0.42f, Vector2.one, 0.75f),
                Stone = CreatePbrMaterial("Mat_BlockoutRTX_GlossyCobaltWall", blueStoneTexture, blueStoneNormal, Color.white, 0.08f, 0.82f, Vector2.one, 1.25f),
                StoneBase = CreatePbrMaterial("Mat_BlockoutRTX_GreyStoneBlockBase", greyStoneTexture, greyStoneNormal, Color.white, 0.02f, 0.58f, Vector2.one, 1.0f),
                Wood = CreatePbrMaterial("Mat_BlockoutRTX_WarmVarnishedWood", woodTexture, woodNormal, Color.white, 0f, 0.62f, Vector2.one, 0.9f),
                Rail = CreatePbrColorMaterial("Mat_BlockoutRTX_GoldRail", new Color(1f, 0.70f, 0.20f), 1f, 0.88f),
                Stair = CreatePbrMaterial("Mat_BlockoutRTX_GreyMarbleStair", stairTexture, stairNormal, Color.white, 0.02f, 0.70f, Vector2.one, 1.1f),
                DoorMarker = CreatePbrMaterial("Mat_BlockoutRTX_BrushedSteelDoor", metalDoorTexture, metalDoorNormal, Color.white, 0.75f, 0.74f, Vector2.one, 0.8f),
                BlueDoor = CreatePbrMaterial("Mat_BlockoutRTX_TurquoiseMetalDoor", tealDoorTexture, tealDoorNormal, Color.white, 0.45f, 0.82f, Vector2.one, 0.9f),
                ElevatorDoor = CreatePbrMaterial("Mat_BlockoutRTX_ChromeTurquoiseDoor", tealDoorTexture, tealDoorNormal, new Color(0.88f, 1f, 1f), 0.65f, 0.88f, Vector2.one, 0.9f),
                Banner = CreateTexturedMaterial("Mat_BlockoutRTX_Banner", bannerTexture, Color.white, Vector2.one),
                Portrait = CreateTexturedMaterial("Mat_BlockoutRTX_Portrait", portraitTexture, Color.white, Vector2.one),
                Brass = CreatePbrColorMaterial("Mat_BlockoutRTX_Brass", new Color(1f, 0.76f, 0.22f), 1f, 0.9f),
                LampGlow = CreateEmissiveMaterial("Mat_BlockoutRTX_WarmLampGlow", new Color(1f, 0.78f, 0.30f), 2.8f),
                Guard = CreateAtlasMaterial("Mat_BlockoutWolf3D_Guard", LoadTextureAsset(WolfRepoTextureFolder + "/guard.png"), 8, 0, 0, true),
                MarkerGreen = CreateColorMaterial("Mat_Blockout_StartMarker", new Color(0.12f, 0.52f, 0.24f)),
                MarkerBlue = CreateColorMaterial("Mat_Blockout_RouteMarker", new Color(0.05f, 0.32f, 0.70f)),
                DarkVoid = CreateColorMaterial("Mat_BlockoutWolf3D_DarkOpening", Color.black)
            };

            return materials;
        }

        private static Material GetWallMaterial(int wallValue, BlockoutMaterials materials)
        {
            if (wallValue <= 0)
            {
                return materials.Stone;
            }

            materials.WallTiles[wallValue] = materials.Stone;
            return materials.Stone;
        }

        private static Material GetStaticMaterial(int typeIndex, BlockoutMaterials materials)
        {
            if (materials.StaticMaterials.TryGetValue(typeIndex, out Material cached))
            {
                return cached;
            }

            Texture2D sprites = LoadTextureAsset(WolfRepoTextureFolder + "/sprites.png");
            int tileIndex = Mathf.Clamp(typeIndex + 2, 0, 255);
            int col = tileIndex % WolfAtlasSize;
            int row = tileIndex / WolfAtlasSize;
            Material material = CreateAtlasMaterial("Mat_BlockoutWolf3D_Static_" + typeIndex.ToString("00"), sprites, WolfAtlasSize, col, row, true);
            materials.StaticMaterials[typeIndex] = material;
            return material;
        }

        private static Material CreatePbrMaterial(string name, Texture2D albedo, Texture2D normal, Color tint, float metallic, float smoothness, Vector2 tiling, float bumpScale)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Standard");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Texture");
            }

            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.color = tint;
            material.mainTexture = albedo;
            material.mainTextureScale = tiling;
            material.mainTextureOffset = Vector2.zero;

            if (shader != null && shader.name == "Standard")
            {
                material.SetFloat("_Metallic", metallic);
                material.SetFloat("_Glossiness", smoothness);
                material.SetFloat("_BumpScale", bumpScale);
                if (normal != null)
                {
                    material.SetTexture("_BumpMap", normal);
                    material.EnableKeyword("_NORMALMAP");
                }
                else
                {
                    material.SetTexture("_BumpMap", null);
                    material.DisableKeyword("_NORMALMAP");
                }

                material.DisableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", Color.black);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreatePbrColorMaterial(string name, Color color, float metallic, float smoothness)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Standard");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.color = color;
            material.mainTexture = null;

            if (shader != null && shader.name == "Standard")
            {
                material.SetFloat("_Metallic", metallic);
                material.SetFloat("_Glossiness", smoothness);
                material.DisableKeyword("_NORMALMAP");
                material.DisableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", Color.black);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateEmissiveMaterial(string name, Color color, float intensity)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Standard");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.color = color;

            if (shader != null && shader.name == "Standard")
            {
                material.SetFloat("_Metallic", 0f);
                material.SetFloat("_Glossiness", 0.65f);
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * intensity);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateColorMaterial(string name, Color color)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Unlit/Color");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateTexturedMaterial(string name, Texture2D texture, Color tint, Vector2 tiling)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Unlit/Texture");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.color = tint;
            material.mainTexture = texture;
            material.mainTextureScale = tiling;
            material.mainTextureOffset = Vector2.zero;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateAtlasMaterial(string name, Texture2D texture, int atlasSize, int col, int row, bool transparent)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find(transparent ? "Unlit/Transparent" : "Unlit/Texture");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.mainTexture = texture;
            material.color = Color.white;
            material.mainTextureScale = new Vector2(1f / atlasSize, 1f / atlasSize);
            material.mainTextureOffset = new Vector2(col / (float)atlasSize, 1f - (row + 1f) / atlasSize);

            if (transparent && material.shader.name == "Standard")
            {
                material.SetFloat("_Mode", 3f);
                material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.EnableKeyword("_ALPHABLEND_ON");
                material.renderQueue = 3000;
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static Texture2D LoadTextureAsset(string assetPath)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (texture != null)
            {
                return texture;
            }

            Debug.LogWarning("[WolfBlockout] Missing texture asset: " + assetPath + ". Falling back to Wolf3D grey stone tile.");
            return ExtractWolfTile("Tex_BlockoutWolf3D_FallbackStone", 0);
        }

        private static Texture2D CreateModernAlbedo(string textureName, ModernSurface surface)
        {
            string outputPath = GeneratedTextureFolder + "/" + textureName + ".png";
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(outputPath);
            if (existing != null)
            {
                return existing;
            }

            const int size = 512;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                filterMode = FilterMode.Trilinear,
                wrapMode = TextureWrapMode.Repeat
            };

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                float v = y / (float)(size - 1);
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)(size - 1);
                    pixels[y * size + x] = ModernAlbedo(surface, u, v);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(true);
            File.WriteAllBytes(ToAbsoluteAssetPath(outputPath), texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            ConfigureModernTexture(outputPath, false);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(outputPath);
        }

        private static Texture2D CreateModernNormal(string textureName, ModernSurface surface, float strength)
        {
            string outputPath = GeneratedTextureFolder + "/" + textureName + ".png";
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(outputPath);
            if (existing != null)
            {
                return existing;
            }

            const int size = 512;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                filterMode = FilterMode.Trilinear,
                wrapMode = TextureWrapMode.Repeat
            };

            Color[] pixels = new Color[size * size];
            float step = 1f / size;
            for (int y = 0; y < size; y++)
            {
                float v = y / (float)(size - 1);
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)(size - 1);
                    float left = ModernHeight(surface, u - step, v);
                    float right = ModernHeight(surface, u + step, v);
                    float down = ModernHeight(surface, u, v - step);
                    float up = ModernHeight(surface, u, v + step);
                    Vector3 normal = new Vector3((left - right) * strength, (down - up) * strength, 1f).normalized;
                    pixels[y * size + x] = new Color(normal.x * 0.5f + 0.5f, normal.y * 0.5f + 0.5f, normal.z * 0.5f + 0.5f, 1f);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(true);
            File.WriteAllBytes(ToAbsoluteAssetPath(outputPath), texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            ConfigureModernTexture(outputPath, true);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(outputPath);
        }

        private static Color ModernAlbedo(ModernSurface surface, float u, float v)
        {
            float noise = FractalNoise(u * 10.7f + 8.1f, v * 10.7f + 2.4f, 5);
            float fine = FractalNoise(u * 45.5f + 4.2f, v * 45.5f + 9.8f, 3);
            switch (surface)
            {
                case ModernSurface.MarbleFloor:
                {
                    float seam = TileSeam(u, v, 4f, 4f, 0.018f);
                    float vein = MarbleVein(u, v, noise, 9.0f);
                    Color baseColor = Color.Lerp(new Color(0.58f, 0.59f, 0.58f), new Color(0.90f, 0.91f, 0.88f), noise);
                    baseColor = Color.Lerp(baseColor, new Color(0.26f, 0.27f, 0.27f), vein * 0.46f);
                    baseColor = Color.Lerp(baseColor, new Color(0.12f, 0.13f, 0.13f), seam);
                    return baseColor + Color.white * fine * 0.040f;
                }
                case ModernSurface.PlasterCeiling:
                {
                    Color baseColor = Color.Lerp(new Color(0.72f, 0.70f, 0.64f), new Color(0.96f, 0.93f, 0.84f), noise);
                    return Color.Lerp(baseColor, new Color(1.0f, 0.94f, 0.76f), fine * 0.10f);
                }
                case ModernSurface.BlueTileWall:
                {
                    float seam = TileSeam(u, v, 3f, 3f, 0.026f);
                    float bevel = TileBevel(u, v, 3f, 3f);
                    Color oldBlueDark = new Color(0.03f, 0.035f, 0.58f);
                    Color oldBlueMid = new Color(0.04f, 0.055f, 0.86f);
                    Color oldBlueLit = new Color(0.12f, 0.25f, 1.0f);
                    Color baseColor = Color.Lerp(oldBlueDark, oldBlueLit, Mathf.Clamp01(noise * 0.72f + bevel * 0.42f));
                    baseColor = Color.Lerp(baseColor, oldBlueMid, 0.45f);
                    baseColor = Color.Lerp(baseColor, new Color(0.0f, 0.0f, 0.11f), seam);
                    return baseColor + new Color(0.12f, 0.18f, 0.85f) * Mathf.Pow(bevel, 4.5f) * 0.45f;
                }
                case ModernSurface.GreyBlock:
                {
                    float seam = OffsetBlockSeam(u, v, 4f, 3f, 0.024f);
                    Color baseColor = Color.Lerp(new Color(0.50f, 0.50f, 0.50f), new Color(0.88f, 0.88f, 0.86f), noise);
                    baseColor = Color.Lerp(baseColor, new Color(0.18f, 0.18f, 0.18f), seam);
                    return baseColor + Color.white * fine * 0.035f;
                }
                case ModernSurface.WarmWood:
                {
                    float seam = TileSeam(u, v, 5f, 1f, 0.018f);
                    float grain = Mathf.Abs(Mathf.Sin((u * 54f + FractalNoise(u * 22f, v * 7f, 3) * 6f) * Mathf.PI));
                    Color oldWoodDark = new Color(0.40f, 0.18f, 0.055f);
                    Color oldWoodMid = new Color(0.62f, 0.39f, 0.17f);
                    Color oldWoodLit = new Color(1.0f, 0.58f, 0.18f);
                    Color baseColor = Color.Lerp(oldWoodDark, oldWoodLit, Mathf.Clamp01(noise * 0.55f + grain * 0.40f));
                    baseColor = Color.Lerp(baseColor, oldWoodMid, 0.28f);
                    return Color.Lerp(baseColor, new Color(0.18f, 0.07f, 0.018f), seam);
                }
                case ModernSurface.MarbleStair:
                {
                    float seam = TileSeam(u, v, 2f, 4f, 0.018f);
                    float vein = MarbleVein(u + 0.17f, v + 0.23f, noise, 11.0f);
                    Color baseColor = Color.Lerp(new Color(0.45f, 0.46f, 0.46f), new Color(0.78f, 0.79f, 0.76f), noise);
                    baseColor = Color.Lerp(baseColor, new Color(0.18f, 0.19f, 0.19f), vein * 0.42f);
                    return Color.Lerp(baseColor, new Color(0.11f, 0.11f, 0.11f), seam);
                }
                case ModernSurface.TealDoor:
                {
                    float seam = DoorPanelSeam(u, v);
                    float rivet = DoorRivetMask(u, v);
                    Color baseColor = Color.Lerp(new Color(0.04f, 0.48f, 0.48f), new Color(0.18f, 0.92f, 0.88f), Mathf.Clamp01(noise * 0.55f + 0.25f));
                    baseColor = Color.Lerp(baseColor, new Color(0.02f, 0.18f, 0.18f), seam);
                    return Color.Lerp(baseColor, new Color(0.78f, 0.96f, 0.92f), rivet * 0.78f);
                }
                case ModernSurface.BrushedSteel:
                default:
                {
                    float streak = Mathf.Abs(Mathf.Sin((v * 160f + noise * 3f) * Mathf.PI)) * 0.08f;
                    return Color.Lerp(new Color(0.58f, 0.59f, 0.59f), new Color(0.96f, 0.97f, 0.96f), Mathf.Clamp01(noise * 0.68f + streak));
                }
            }
        }

        private static float ModernHeight(ModernSurface surface, float u, float v)
        {
            u = Mathf.Repeat(u, 1f);
            v = Mathf.Repeat(v, 1f);
            float noise = FractalNoise(u * 12.5f, v * 12.5f, 4);
            switch (surface)
            {
                case ModernSurface.MarbleFloor:
                    return noise * 0.10f + MarbleVein(u, v, noise, 9.0f) * 0.18f - TileSeam(u, v, 4f, 4f, 0.018f) * 0.55f;
                case ModernSurface.PlasterCeiling:
                    return noise * 0.28f + FractalNoise(u * 42f, v * 42f, 3) * 0.12f;
                case ModernSurface.BlueTileWall:
                    return noise * 0.10f + TileBevel(u, v, 3f, 3f) * 0.18f - TileSeam(u, v, 3f, 3f, 0.026f) * 0.72f;
                case ModernSurface.GreyBlock:
                    return noise * 0.16f - OffsetBlockSeam(u, v, 4f, 3f, 0.024f) * 0.65f;
                case ModernSurface.WarmWood:
                    return noise * 0.11f + Mathf.Abs(Mathf.Sin((u * 58f + noise * 4f) * Mathf.PI)) * 0.08f - TileSeam(u, v, 5f, 1f, 0.018f) * 0.45f;
                case ModernSurface.MarbleStair:
                    return noise * 0.11f + MarbleVein(u + 0.17f, v + 0.23f, noise, 11.0f) * 0.14f - TileSeam(u, v, 2f, 4f, 0.018f) * 0.50f;
                case ModernSurface.TealDoor:
                    return noise * 0.08f - DoorPanelSeam(u, v) * 0.42f + DoorRivetMask(u, v) * 0.45f;
                case ModernSurface.BrushedSteel:
                default:
                    return noise * 0.05f + Mathf.Abs(Mathf.Sin(v * 180f * Mathf.PI)) * 0.025f;
            }
        }

        private static float FractalNoise(float x, float y, int octaves)
        {
            float value = 0f;
            float amplitude = 0.5f;
            float frequency = 1f;
            float weight = 0f;
            for (int i = 0; i < octaves; i++)
            {
                value += Mathf.PerlinNoise(x * frequency, y * frequency) * amplitude;
                weight += amplitude;
                amplitude *= 0.5f;
                frequency *= 2.03f;
            }

            return weight <= 0f ? 0f : value / weight;
        }

        private static float MarbleVein(float u, float v, float noise, float frequency)
        {
            float vein = Mathf.Abs(Mathf.Sin((u * frequency + v * (frequency * 0.42f) + noise * 3.6f) * Mathf.PI));
            return 1f - SmoothRange(0.04f, 0.16f, vein);
        }

        private static float TileSeam(float u, float v, float tilesX, float tilesY, float thickness)
        {
            float fu = Mathf.Repeat(u * tilesX, 1f);
            float fv = Mathf.Repeat(v * tilesY, 1f);
            float dx = Mathf.Min(fu, 1f - fu);
            float dy = Mathf.Min(fv, 1f - fv);
            float seamX = 1f - SmoothRange(thickness, thickness * 2.25f, dx);
            float seamY = 1f - SmoothRange(thickness, thickness * 2.25f, dy);
            return Mathf.Clamp01(Mathf.Max(seamX, seamY));
        }

        private static float TileBevel(float u, float v, float tilesX, float tilesY)
        {
            float fu = Mathf.Repeat(u * tilesX, 1f);
            float fv = Mathf.Repeat(v * tilesY, 1f);
            float edge = Mathf.Min(Mathf.Min(fu, 1f - fu), Mathf.Min(fv, 1f - fv));
            return SmoothRange(0.02f, 0.14f, edge);
        }

        private static float OffsetBlockSeam(float u, float v, float tilesX, float tilesY, float thickness)
        {
            float row = Mathf.Floor(v * tilesY);
            float offset = Mathf.Repeat(row, 2f) > 0.5f ? 0.5f / tilesX : 0f;
            return TileSeam(u + offset, v, tilesX, tilesY, thickness);
        }

        private static float DoorPanelSeam(float u, float v)
        {
            float outer = Mathf.Max(1f - SmoothRange(0.035f, 0.075f, Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v))), 0f);
            float center = 1f - SmoothRange(0.012f, 0.03f, Mathf.Abs(u - 0.5f));
            float diamond = 1f - SmoothRange(0.010f, 0.035f, Mathf.Abs(Mathf.Abs(u - 0.5f) + Mathf.Abs(v - 0.5f) - 0.34f));
            return Mathf.Clamp01(Mathf.Max(outer, Mathf.Max(center * 0.5f, diamond)));
        }

        private static float DoorRivetMask(float u, float v)
        {
            float mask = 0f;
            for (int y = 0; y < 5; y++)
            {
                float ry = 0.14f + y * 0.18f;
                mask = Mathf.Max(mask, Rivet(u, v, 0.08f, ry));
                mask = Mathf.Max(mask, Rivet(u, v, 0.92f, ry));
            }

            return mask;
        }

        private static float Rivet(float u, float v, float cx, float cy)
        {
            float d = Vector2.Distance(new Vector2(u, v), new Vector2(cx, cy));
            return 1f - SmoothRange(0.018f, 0.04f, d);
        }

        private static float SmoothRange(float edge0, float edge1, float value)
        {
            if (edge1 <= edge0)
            {
                return value >= edge1 ? 1f : 0f;
            }

            float t = Mathf.Clamp01((value - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }

        private static Texture2D CreateNoiseTexture(string textureName, Color low, Color high)
        {
            string outputPath = GeneratedTextureFolder + "/" + textureName + ".png";
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(outputPath);
            if (existing != null)
            {
                return existing;
            }

            const int size = 128;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Repeat;

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float n = Mathf.PerlinNoise((x + 17f) / 18f, (y + 91f) / 18f);
                    float grain = ((x * 13 + y * 7) % 17) / 16f * 0.08f;
                    pixels[y * size + x] = Color.Lerp(low, high, Mathf.Clamp01(n * 0.78f + grain));
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(true);

            File.WriteAllBytes(ToAbsoluteAssetPath(outputPath), texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            ConfigureGeneratedTexture(outputPath);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(outputPath);
        }

        private static Texture2D ExtractWolfTile(string textureName, int tileIndex)
        {
            string outputPath = GeneratedTextureFolder + "/" + textureName + ".png";
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(outputPath);
            if (existing != null)
            {
                return existing;
            }

            string atlasPath = WolfRepoTextureFolder + "/walls.png";
            string absoluteAtlasPath = ToAbsoluteAssetPath(atlasPath);
            if (!File.Exists(absoluteAtlasPath))
            {
                throw new FileNotFoundException("Missing Wolf3D wall atlas", absoluteAtlasPath);
            }

            Texture2D atlas = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!atlas.LoadImage(File.ReadAllBytes(absoluteAtlasPath)))
            {
                throw new InvalidDataException("Could not decode Wolf3D wall atlas: " + absoluteAtlasPath);
            }

            int tileWidth = atlas.width / WolfAtlasSize;
            int tileHeight = atlas.height / WolfAtlasSize;
            int col = tileIndex % WolfAtlasSize;
            int row = tileIndex / WolfAtlasSize;
            int sourceX = col * tileWidth;
            int sourceY = atlas.height - (row + 1) * tileHeight;

            Texture2D tile = new Texture2D(tileWidth, tileHeight, TextureFormat.RGBA32, true);
            tile.filterMode = FilterMode.Point;
            tile.wrapMode = TextureWrapMode.Repeat;

            Color[] pixels = atlas.GetPixels(sourceX, sourceY, tileWidth, tileHeight);
            tile.SetPixels(pixels);
            tile.Apply(true);

            File.WriteAllBytes(ToAbsoluteAssetPath(outputPath), tile.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(atlas);
            UnityEngine.Object.DestroyImmediate(tile);

            AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            ConfigureGeneratedTexture(outputPath);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(outputPath);
        }

        private static void ConfigureGeneratedTexture(string assetPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static void ConfigureModernTexture(string assetPath, bool normalMap)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 8;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static void EnsureFolders()
        {
            CreateFolderIfMissing("Assets", "Scenes");
            CreateFolderIfMissing("Assets", "Materials");
            CreateFolderIfMissing("Assets/Materials", "Blockout");
            CreateFolderIfMissing("Assets", "Textures");
            CreateFolderIfMissing("Assets/Textures", "BlockoutWolf3D");
        }

        private static void CreateFolderIfMissing(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static string ToAbsoluteAssetPath(string assetPath)
        {
            string relative = assetPath.StartsWith("Assets/")
                ? assetPath.Substring("Assets/".Length)
                : assetPath;
            return Path.Combine(Application.dataPath, relative.Replace('/', Path.DirectorySeparatorChar));
        }

        private sealed class BlockoutMaterials
        {
            public Material Floor;
            public Material FloorLight;
            public Material Ceiling;
            public Material Stone;
            public Material StoneBase;
            public Material Wood;
            public Material Rail;
            public Material Stair;
            public Material DoorMarker;
            public Material BlueDoor;
            public Material ElevatorDoor;
            public Material Banner;
            public Material Portrait;
            public Material Brass;
            public Material LampGlow;
            public Material Guard;
            public Material MarkerGreen;
            public Material MarkerBlue;
            public Material DarkVoid;
            public readonly Dictionary<int, Material> WallTiles = new();
            public readonly Dictionary<int, Material> StaticMaterials = new();
        }

        private enum ModernSurface
        {
            MarbleFloor,
            PlasterCeiling,
            BlueTileWall,
            GreyBlock,
            WarmWood,
            MarbleStair,
            TealDoor,
            BrushedSteel
        }

        [Serializable]
        private sealed class SourceLevel
        {
            public string name;
            public int[] walls;
            public SourceDoor[] doors;
            public SourceEnemy[] enemies;
            public SourceStatic[] statics;
            public int spawnX;
            public int spawnY;
            public int spawnAngle;
        }

        [Serializable]
        private sealed class SourceDoor
        {
            public int x;
            public int y;
            public string type;
            public bool vertical;
        }

        [Serializable]
        private sealed class SourceEnemy
        {
            public int x;
            public int y;
            public string type;
            public int dir;
            public bool patrol;
            public int difficulty;
        }

        [Serializable]
        private sealed class SourceStatic
        {
            public int x;
            public int y;
            public int typeIndex;
            public string typeName;
        }
    }
}
