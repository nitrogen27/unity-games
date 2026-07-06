using HelloWorldRoom;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using WolfMini.Core;
using WolfMini.Level;
using WolfMini.Rendering;

namespace WolfMini.EditorTools
{
    /// <summary>
    /// Creates the true-3D game scene: level builder (geometry is generated at
    /// Start so the saved scene stays small), player rig with a standard
    /// physically pitching first-person camera, HUD and ambient setup.
    /// </summary>
    public static class WolfFull3DSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/WolfRepoLevel1Full3D.unity";
        private const string RepoTextureFolder = "Assets/Textures/WolfRepo";

        // Honest perspective with the low Wolf ceilings kept: the narrower FOV
        // and the pitch clamp limit how strongly verticals converge when the
        // player looks up at the nearby ceiling.
        private const float CameraFieldOfView = 58f;
        private const float CameraMaxPitch = 45f;

        [MenuItem("Tools/Wolf Full3D/Build Full3D Scene")]
        public static void BuildScene()
        {
            if (AssetDatabase.LoadAssetAtPath<WolfSectorLevelDefinition>(WolfSectorLevelConverter.SectorAssetPath) == null)
            {
                Debug.LogError("[WolfFull3D] Run 'Convert Repo Level To Sector Definition' first.");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "WolfRepoLevel1Full3D";

            // Loaded after the scene switch: NewScene(Single) can unload
            // non-dirty assets, which would leave a fake-null reference on the
            // builder in the open scene (the saved file still got the guid, but
            // edit-mode rebuilds saw no definition).
            var definition = AssetDatabase.LoadAssetAtPath<WolfSectorLevelDefinition>(WolfSectorLevelConverter.SectorAssetPath);
            WolfFull3DMaterialLibrary library = WolfFull3DImporter.EnsureMaterialLibrary();

            // Warm, low-key ambient; local lamps and probes carry the cinematic contrast.
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.23f, 0.215f, 0.19f);
            RenderSettings.ambientEquatorColor = new Color(0.18f, 0.17f, 0.15f);
            RenderSettings.ambientGroundColor = new Color(0.105f, 0.10f, 0.09f);
            RenderSettings.ambientIntensity = 0.54f;
            RenderSettings.reflectionIntensity = 0.92f;
            RenderSettings.reflectionBounces = 1;
            RenderSettings.defaultReflectionResolution = 256;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.00055f;
            RenderSettings.fogColor = new Color(0.19f, 0.205f, 0.22f);

            CreateLevelBuilder(definition, library);
            CreatePlayer(definition);
            CreateHud();
            CreateDirectionalLight();
            CreatePerformanceSettings();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[WolfFull3D] Saved {ScenePath} and set it as the enabled build scene. Enter Play Mode to generate and walk the level.");
        }

        [MenuItem("Tools/Wolf Full3D/Rebuild Level In Open Scene")]
        public static void RebuildLevelInOpenScene()
        {
            WolfSectorMeshBuilder builder = Object.FindFirstObjectByType<WolfSectorMeshBuilder>();
            if (builder == null)
            {
                Debug.LogError("[WolfFull3D] No WolfSectorMeshBuilder in the open scene. Run 'Build Full3D Scene' first.");
                return;
            }

            builder.Build();
            Debug.Log("[WolfFull3D] Rebuilt generated level in the open scene (editor preview).");
        }

        private static void CreateLevelBuilder(WolfSectorLevelDefinition definition, WolfFull3DMaterialLibrary library)
        {
            GameObject builderObject = new GameObject("Wolf Sector Level Builder");
            WolfSectorMeshBuilder builder = builderObject.AddComponent<WolfSectorMeshBuilder>();
            builder.Definition = definition;
            builder.MaterialLibrary = library;
            builder.BuildOnStart = true;
        }

        private static void CreatePlayer(WolfSectorLevelDefinition definition)
        {
            GameObject player = new GameObject("Wolf Full3D Player");
            player.transform.position = definition.playerSpawnPosition;
            player.transform.rotation = Quaternion.Euler(0f, definition.playerSpawnYaw, 0f);

            CharacterController characterController = player.AddComponent<CharacterController>();
            characterController.height = WolfMiniConstants.PlayerHeight;
            characterController.radius = WolfMiniConstants.PlayerRadius;
            characterController.center = new Vector3(0f, WolfMiniConstants.PlayerCenterY, 0f);
            characterController.stepOffset = WolfMiniConstants.PlayerStepOffset;
            characterController.slopeLimit = 50f;

            SimpleFirstPersonController controller = player.AddComponent<SimpleFirstPersonController>();
            WolfPlayerInteractor interactor = player.AddComponent<WolfPlayerInteractor>();

            GameObject cameraObject = new GameObject("First Person Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(player.transform);
            cameraObject.transform.localPosition = new Vector3(0f, WolfMiniConstants.EyeHeight, 0f);
            cameraObject.transform.localRotation = Quaternion.identity;

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = CameraFieldOfView;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 260f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(56, 56, 56, 255);
            cameraObject.AddComponent<AudioListener>();

            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("playerCamera").objectReferenceValue = camera;
            serializedController.FindProperty("maxPitch").floatValue = CameraMaxPitch;
            // Speeds scale with the cell so traversal keeps the Wolf pace
            // (cells per second), otherwise the larger world feels like wading.
            serializedController.FindProperty("walkSpeed").floatValue = 4f * WolfMiniConstants.WorldScale;
            serializedController.FindProperty("runSpeed").floatValue = 7f * WolfMiniConstants.WorldScale;
            serializedController.FindProperty("jumpHeight").floatValue = 1.1f * WolfMiniConstants.WorldScale;
            serializedController.FindProperty("gravity").floatValue = -18f * WolfMiniConstants.WorldScale;
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject serializedInteractor = new SerializedObject(interactor);
            serializedInteractor.FindProperty("playerCamera").objectReferenceValue = camera;
            serializedInteractor.FindProperty("range").floatValue = 2.4f * WolfMiniConstants.WorldScale;
            serializedInteractor.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateHud()
        {
            GameObject hud = new GameObject("Wolf Full3D HUD");
            WolfHud wolfHud = hud.AddComponent<WolfHud>();

            SerializedObject serializedHud = new SerializedObject(wolfHud);
            serializedHud.FindProperty("faceTexture").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Texture2D>($"{RepoTextureFolder}/bj.png");
            serializedHud.FindProperty("weaponTexture").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Texture2D>($"{RepoTextureFolder}/attack.png");
            serializedHud.FindProperty("keyTexture").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Texture2D>($"{RepoTextureFolder}/hudkeys.png");
            serializedHud.FindProperty("showFps").boolValue = true;
            serializedHud.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateDirectionalLight()
        {
            GameObject directional = new GameObject("Full3D Directional Light");
            Light light = directional.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.88f, 0.66f);
            light.intensity = 0.08f;
            light.shadows = LightShadows.None;
            light.bounceIntensity = 0.05f;
            directional.transform.rotation = Quaternion.Euler(55f, -45f, 0f);
        }

        private static void CreatePerformanceSettings()
        {
            GameObject settingsObject = new GameObject("Wolf Performance Settings");
            WolfPerformanceSettings settings = settingsObject.AddComponent<WolfPerformanceSettings>();
            settings.Apply();
        }
    }
}
