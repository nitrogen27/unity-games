using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WolfE1M1.Editor
{
    public static class SceneBootstrapper
    {
        private const string ScenePath = "Assets/WolfE1M1/Scenes/TwoRoomsDemo.unity";

        [MenuItem("WolfE1M1/Create Two Rooms Demo Scene")]
        public static void CreateTwoRoomsDemoScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject root = new GameObject("Wolf E1M1 Two Rooms Demo");
            E1M1TwoRoomsBuilder builder = root.AddComponent<E1M1TwoRoomsBuilder>();
            builder.Rebuild();

            GameObject player = new GameObject("Player");
            player.transform.position = builder.GetSpawnPosition();
            player.transform.rotation = Quaternion.identity;
            CharacterController controller = player.AddComponent<CharacterController>();
            controller.height = WolfE1M1Constants.EyeHeight * 2f;
            controller.center = Vector3.up * WolfE1M1Constants.EyeHeight;
            controller.radius = 0.35f;

            GameObject cameraObject = new GameObject("Player Camera");
            cameraObject.transform.SetParent(player.transform, false);
            cameraObject.transform.localPosition = Vector3.up * WolfE1M1Constants.EyeHeight;
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 67f;
            camera.nearClipPlane = 0.02f;
            camera.farClipPlane = 80f;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<RetroPixelCamera>();

            DemoFirstPersonController fps = player.AddComponent<DemoFirstPersonController>();
            SerializedObject serializedFps = new SerializedObject(fps);
            serializedFps.FindProperty("playerCamera").objectReferenceValue = camera;
            serializedFps.ApplyModifiedPropertiesWithoutUndo();

            GameObject hud = new GameObject("Simple HUD");
            hud.AddComponent<SimpleHud>();

            GameObject light = new GameObject("Directional Light");
            Light directional = light.AddComponent<Light>();
            directional.type = LightType.Directional;
            directional.intensity = 0.55f;
            light.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Color.white;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
        }

        public static void ValidateTwoRoomsDemoScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            E1M1TwoRoomsBuilder builder = Object.FindAnyObjectByType<E1M1TwoRoomsBuilder>();
            DemoDoor[] doors = Object.FindObjectsByType<DemoDoor>(FindObjectsInactive.Include);
            DemoFirstPersonController player = Object.FindAnyObjectByType<DemoFirstPersonController>();
            RetroPixelCamera pixelCamera = Object.FindAnyObjectByType<RetroPixelCamera>();
            SimpleHud hud = Object.FindAnyObjectByType<SimpleHud>();
            Texture2D wallTexture = Resources.Load<Texture2D>("WolfE1M1/Textures/wall_08_light");
            Texture2D doorTexture = Resources.Load<Texture2D>("WolfE1M1/Textures/door_normal_face");

            if (!scene.isLoaded)
            {
                throw new System.InvalidOperationException("TwoRoomsDemo scene did not load.");
            }
            if (builder == null)
            {
                throw new System.InvalidOperationException("Missing E1M1TwoRoomsBuilder.");
            }
            if (doors.Length != 2)
            {
                throw new System.InvalidOperationException($"Expected 2 demo doors, found {doors.Length}.");
            }
            if (player == null)
            {
                throw new System.InvalidOperationException("Missing DemoFirstPersonController.");
            }
            if (pixelCamera == null)
            {
                throw new System.InvalidOperationException("Missing RetroPixelCamera.");
            }
            if (hud == null)
            {
                throw new System.InvalidOperationException("Missing SimpleHud.");
            }
            if (wallTexture == null || doorTexture == null)
            {
                throw new System.InvalidOperationException("Missing required Resources textures.");
            }
            if (WolfE1M1Constants.CellSize != 2f || WolfE1M1Constants.WallHeight != 2f || WolfE1M1Constants.EyeHeight != 0.9f)
            {
                throw new System.InvalidOperationException("WolfMini scale constants changed.");
            }

            Debug.Log("WolfE1M1 validation passed: scene, player, camera, HUD, doors, textures, and constants are present.");
        }
    }
}
