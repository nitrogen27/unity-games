using WolfMini.Core;
using WolfMini.Level;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WolfMini.EditorTools
{
    public static class WolfMiniDemoLevelMenu
    {
        private const string DataPath = "Assets/WolfMini/Data/DemoTwoFloorLevel.asset";
        private const string MaterialLibraryPath = "Assets/WolfMini/Data/WolfMiniMaterialLibrary.asset";
        private const string ScenePath = "Assets/WolfMini/Scenes/DemoTwoFloorBlockout.unity";

        [MenuItem("WolfMini/Create Demo Two Floor Level")]
        public static void CreateDemoTwoFloorLevelMenu()
        {
            CreateOrUpdateDemoTwoFloorLevel();
            AssetDatabase.Refresh();
        }

        private static MiniLevelDefinition CreateOrUpdateDemoTwoFloorLevel()
        {
            EnsureWolfMiniFolders();

            MiniLevelDefinition level = AssetDatabase.LoadAssetAtPath<MiniLevelDefinition>(DataPath);
            if (level == null)
            {
                level = ScriptableObject.CreateInstance<MiniLevelDefinition>();
                AssetDatabase.CreateAsset(level, DataPath);
            }

            PopulateLevel(level);
            EditorUtility.SetDirty(level);
            AssetDatabase.SaveAssets();
            Debug.Log($"[WolfMini] Demo level asset written to {DataPath}");
            return level;
        }

        [MenuItem("WolfMini/Create Demo Two Floor Blockout Scene")]
        public static void CreateDemoTwoFloorBlockoutScene()
        {
            CreateOrUpdateDemoTwoFloorLevel();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "DemoTwoFloorBlockout";

            MiniLevelDefinition assetLevel = AssetDatabase.LoadAssetAtPath<MiniLevelDefinition>(DataPath);
            if (assetLevel == null)
            {
                Debug.LogError($"[WolfMini] Could not load demo level asset from {DataPath}");
                return;
            }

            RenderSettings.ambientLight = new Color(0.42f, 0.42f, 0.42f);
            RenderSettings.fog = false;

            GameObject builderObject = new GameObject("WolfMini LevelBuilder");
            LevelBuilder builder = builderObject.AddComponent<LevelBuilder>();
            builder.BuildOnStart = false;
            builder.Build(assetLevel);
            builder.BuildOnStart = true;
            EditorUtility.SetDirty(builder);

            CreateOverviewCamera();
            CreateSimpleLighting();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[WolfMini] Demo blockout scene written to {ScenePath}");
        }

        private static void PopulateLevel(MiniLevelDefinition level)
        {
            level.levelName = "WolfMini Two Floor Demo";
            if (level.materialLibrary == null)
            {
                level.materialLibrary = AssetDatabase.LoadAssetAtPath<WolfMaterialLibrary>(MaterialLibraryPath);
            }
            level.floors.Clear();
            level.doors.Clear();
            level.stairs.Clear();
            level.decor.Clear();

            level.floors.Add(new FloorSpec
            {
                id = "Upper",
                y = WolfMiniConstants.UpperFloorY,
                wallValue = WolfMaterialLibrary.DefaultUpperWallValue,
                floorColor = new Color32(78, 78, 78, 255),
                ceilingColor = new Color32(62, 62, 62, 255),
                corridors =
                {
                    new CorridorRect { id = "Upper_Main_Corridor", x = 0, z = 0, widthCells = 3, depthCells = 12 }
                },
                rooms =
                {
                    new RoomRect { id = "Upper_Spawn_Room", x = -1, z = -5, widthCells = 5, depthCells = 5 },
                    new RoomRect { id = "Upper_Left_Room", x = -5, z = 2, widthCells = 5, depthCells = 4 },
                    new RoomRect { id = "Upper_Right_Room", x = 3, z = 3, widthCells = 5, depthCells = 5 },
                    new RoomRect { id = "Upper_End_Room", x = -1, z = 12, widthCells = 5, depthCells = 5 }
                }
            });

            level.floors.Add(new FloorSpec
            {
                id = "Lower",
                y = WolfMiniConstants.LowerFloorY,
                wallValue = WolfMaterialLibrary.DefaultLowerWallValue,
                floorColor = new Color32(68, 68, 68, 255),
                ceilingColor = new Color32(54, 54, 54, 255),
                corridors =
                {
                    new CorridorRect { id = "Lower_Main_Corridor", x = 0, z = 9, widthCells = 3, depthCells = 8 }
                },
                rooms =
                {
                    new RoomRect { id = "Lower_Landing", x = -1, z = 4, widthCells = 5, depthCells = 5 },
                    new RoomRect { id = "Lower_Storage", x = -5, z = 10, widthCells = 4, depthCells = 4 },
                    new RoomRect { id = "Lower_Guard_Room", x = 3, z = 10, widthCells = 5, depthCells = 4 },
                    new RoomRect { id = "Lower_Utility", x = -1, z = 17, widthCells = 4, depthCells = 5 }
                }
            });

            level.stairs.Add(new StairSpec
            {
                id = "Main_Descent",
                upperFloorId = "Upper",
                lowerFloorId = "Lower",
                upperOpeningCells = new RectInt(0, 5, 3, 3),
                lowerLandingCells = new RectInt(-1, 4, 5, 5),
                direction = StairDirection.South,
                addRailings = true,
                addSideWalls = true
            });

            level.doors.Add(new DoorSpec { id = "Upper_Spawn_Door", floorId = "Upper", cell = new Vector2Int(1, 0), orientation = DoorOrientation.NorthSouth, connectsA = "Upper_Spawn_Room", connectsB = "Upper_Main_Corridor" });
            level.doors.Add(new DoorSpec { id = "Upper_Left_Door", floorId = "Upper", cell = new Vector2Int(0, 3), orientation = DoorOrientation.EastWest, connectsA = "Upper_Left_Room", connectsB = "Upper_Main_Corridor" });
            level.doors.Add(new DoorSpec { id = "Upper_Right_Door", floorId = "Upper", cell = new Vector2Int(2, 5), orientation = DoorOrientation.EastWest, connectsA = "Upper_Right_Room", connectsB = "Upper_Main_Corridor" });
            level.doors.Add(new DoorSpec { id = "Upper_End_Door", floorId = "Upper", cell = new Vector2Int(1, 11), orientation = DoorOrientation.NorthSouth, connectsA = "Upper_End_Room", connectsB = "Upper_Main_Corridor" });
            level.doors.Add(new DoorSpec { id = "Lower_Storage_Door", floorId = "Lower", cell = new Vector2Int(0, 11), orientation = DoorOrientation.EastWest, connectsA = "Lower_Storage", connectsB = "Lower_Main_Corridor" });
            level.doors.Add(new DoorSpec { id = "Lower_Guard_Door", floorId = "Lower", cell = new Vector2Int(2, 11), orientation = DoorOrientation.EastWest, connectsA = "Lower_Guard_Room", connectsB = "Lower_Main_Corridor" });
            level.doors.Add(new DoorSpec { id = "Lower_Utility_Door", floorId = "Lower", cell = new Vector2Int(1, 16), orientation = DoorOrientation.NorthSouth, connectsA = "Lower_Utility", connectsB = "Lower_Main_Corridor" });

            level.decor.Add(new DecorSpec { id = "Upper_Corridor_Light_A", floorId = "Upper", type = DecorType.CeilingLight, cell = new Vector2(1.5f, 2.5f) });
            level.decor.Add(new DecorSpec { id = "Upper_Stair_Chandelier", floorId = "Upper", type = DecorType.Chandelier, cell = new Vector2(1.5f, 5.5f) });
            level.decor.Add(new DecorSpec { id = "Upper_End_Light", floorId = "Upper", type = DecorType.CeilingLight, cell = new Vector2(1.5f, 14.5f) });
            level.decor.Add(new DecorSpec { id = "Lower_Landing_Light", floorId = "Lower", type = DecorType.CeilingLight, cell = new Vector2(1.5f, 6.5f) });
            level.decor.Add(new DecorSpec { id = "Lower_Corridor_Light", floorId = "Lower", type = DecorType.CeilingLight, cell = new Vector2(1.5f, 13.5f) });

            level.playerSpawn = new PlayerSpawnSpec
            {
                floorId = "Upper",
                cell = new Vector2(1.5f, -3.0f),
                rotationY = 0f
            };
        }

        private static void EnsureWolfMiniFolders()
        {
            EnsureFolder("Assets", "WolfMini");
            EnsureFolder("Assets/WolfMini", "Data");
            EnsureFolder("Assets/WolfMini", "Scenes");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static void CreateOverviewCamera()
        {
            GameObject cameraObject = new GameObject("Overview Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(3f, 17f, 17f);
            cameraObject.transform.rotation = Quaternion.Euler(58f, 180f, 0f);

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 54f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 120f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.05f, 0.06f, 0.08f);
        }

        private static void CreateSimpleLighting()
        {
            GameObject lightObject = new GameObject("Soft Directional Light");
            lightObject.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.75f;
            light.color = new Color(1f, 0.94f, 0.84f);
        }
    }
}
