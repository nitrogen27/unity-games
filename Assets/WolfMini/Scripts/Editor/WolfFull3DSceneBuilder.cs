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
    /// Start so the saved scene stays small), player rig with the anti-distortion
    /// camera settings, HUD and ambient setup.
    /// </summary>
    public static class WolfFull3DSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/WolfRepoLevel1Full3D.unity";
        private const string RepoTextureFolder = "Assets/Textures/WolfRepo";

        // Vertical FOV 62 instead of 75 reduces edge stretching; look-up/down is
        // rendered with a y-sheared projection (WolfYShearLook) so wall verticals
        // never converge, and the pitch clamp keeps the shear in its sweet spot.
        private const float CameraFieldOfView = 62f;
        private const float CameraMaxPitch = 45f;

        [MenuItem("Tools/Wolf Full3D/Build Full3D Scene")]
        public static void BuildScene()
        {
            var definition = AssetDatabase.LoadAssetAtPath<WolfLevelDefinition>(WolfFull3DImporter.DefinitionAssetPath);
            if (definition == null)
            {
                Debug.LogError("[WolfFull3D] Run 'Import Repo Level Into Definition' first.");
                return;
            }

            WolfFull3DMaterialLibrary library = WolfFull3DImporter.EnsureMaterialLibrary();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "WolfRepoLevel1Full3D";

            // Ambient ported from WolfDynamicLightingSetup (warm trilight dusk).
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.32f, 0.30f, 0.27f);
            RenderSettings.ambientEquatorColor = new Color(0.26f, 0.24f, 0.21f);
            RenderSettings.ambientGroundColor = new Color(0.20f, 0.19f, 0.17f);
            RenderSettings.ambientIntensity = 0.68f;

            CreateLevelBuilder(definition, library);
            CreatePlayer(definition);
            CreateHud();
            CreateDirectionalLight();
            CreatePerformanceSettings();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[WolfFull3D] Saved {ScenePath}. Enter Play Mode to generate and walk the level.");
        }

        [MenuItem("Tools/Wolf Full3D/Rebuild Level In Open Scene")]
        public static void RebuildLevelInOpenScene()
        {
            WolfLevelMeshBuilder builder = Object.FindFirstObjectByType<WolfLevelMeshBuilder>();
            if (builder == null)
            {
                Debug.LogError("[WolfFull3D] No WolfLevelMeshBuilder in the open scene. Run 'Build Full3D Scene' first.");
                return;
            }

            builder.Build();
            Debug.Log("[WolfFull3D] Rebuilt generated level in the open scene (editor preview).");
        }

        private static void CreateLevelBuilder(WolfLevelDefinition definition, WolfFull3DMaterialLibrary library)
        {
            GameObject builderObject = new GameObject("Wolf Full3D Level Builder");
            WolfLevelMeshBuilder builder = builderObject.AddComponent<WolfLevelMeshBuilder>();
            builder.Definition = definition;
            builder.MaterialLibrary = library;
            builder.BuildOnStart = true;
        }

        private static void CreatePlayer(WolfLevelDefinition definition)
        {
            GridFloorSpec spawnFloor = definition.FindFloor(definition.playerSpawn.floorId);
            float floorY = spawnFloor?.y ?? 0f;
            Vector2Int cell = definition.playerSpawn.cell;
            Vector3 spawnPosition = new Vector3(
                (cell.x + 0.5f) * WolfMiniConstants.CellSize,
                floorY,
                (cell.y + 0.5f) * WolfMiniConstants.CellSize);

            GameObject player = new GameObject("Wolf Full3D Player");
            player.transform.position = spawnPosition;
            player.transform.rotation = Quaternion.Euler(0f, 90f + definition.playerSpawn.angle, 0f);

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
            cameraObject.transform.localPosition = new Vector3(0f, WolfMiniConstants.EyeHeight, 0f);
            cameraObject.transform.localRotation = Quaternion.identity;

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = CameraFieldOfView;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 200f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(56, 56, 56, 255);
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<WolfYShearLook>();

            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("playerCamera").objectReferenceValue = camera;
            serializedController.FindProperty("maxPitch").floatValue = CameraMaxPitch;
            serializedController.FindProperty("rotateCameraPitch").boolValue = false;
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject serializedInteractor = new SerializedObject(interactor);
            serializedInteractor.FindProperty("playerCamera").objectReferenceValue = camera;
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
            light.color = Color.white;
            light.intensity = 0.30f;
            light.shadows = LightShadows.None;
            light.bounceIntensity = 0.2f;
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
