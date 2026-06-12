using System;
using System.IO;
using HelloWorldRoom;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using WolfMini.Core;

namespace HelloWorldRoom.Editor
{
    public static class WolfRemakeVerticalSliceBuilder
    {
        private const string ScenePath = "Assets/Scenes/WolfRemakeVerticalSlice.unity";
        private const string MaterialFolder = "Assets/Materials/WolfRemake";
        private const string TextureFolder = "Assets/Textures/WolfRemake";

        [MenuItem("Tools/Wolf Remake/Create Vertical Slice")]
        public static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[WolfRemake] Unity is in Play Mode. Stopping Play Mode; run this menu item again after it exits.");
                EditorApplication.isPlaying = false;
                return;
            }

            EnsureFolders();
            Debug.Log("[WolfRemake] Generating procedural textures and materials...");

            WolfTextures textures = LoadOrCreateTextures();
            WolfMaterials materials = CreateMaterials(textures);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "WolfRemakeVerticalSlice";

            ConfigureRenderSettings();

            GameObject root = new GameObject("Wolf Remake Vertical Slice");
            GameObject levelRoot = new GameObject("Level Geometry");
            GameObject decorRoot = new GameObject("Wolf Decor");
            GameObject actorsRoot = new GameObject("Actors");
            levelRoot.transform.SetParent(root.transform);
            decorRoot.transform.SetParent(root.transform);
            actorsRoot.transform.SetParent(root.transform);

            BuildArchitecture(levelRoot.transform, materials);
            BuildDecor(decorRoot.transform, materials);
            BuildActors(actorsRoot.transform, materials);
            CreatePlayer(textures);
            CreateHud(textures);
            CreateLighting(materials);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[WolfRemake] Scene saved at {ScenePath}. Press Play: WASD/mouse, Shift, Space, E for doors.");
        }

        [MenuItem("Tools/Wolf Remake/Exit Play Mode")]
        public static void ExitPlayMode()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                Debug.Log("[WolfRemake] Requested Play Mode exit.");
            }
        }

        private static void BuildArchitecture(Transform parent, WolfMaterials materials)
        {
            const float width = 12f;
            const float wallT = 0.25f;
            const float lowZMin = -8f;
            const float lowZMax = 1f;
            const float upZMin = 1f;
            const float upZMax = 9f;
            const float upperY = 3f;
            const float ceilingY = 6.35f;
            const float doorH = 2.4f;

            CreateCube("Lower Stone Floor", new Vector3(0f, -0.1f, -3.5f), new Vector3(width, 0.2f, lowZMax - lowZMin), materials.Floor, parent);
            CreateCube("Upper Stone Floor", new Vector3(0f, upperY - 0.1f, 5f), new Vector3(width, 0.2f, upZMax - upZMin), materials.Floor, parent);
            CreateCube("Upper Room Ceiling", new Vector3(0f, ceilingY + 0.05f, 5f), new Vector3(width, 0.1f, upZMax - upZMin), materials.Ceiling, parent);
            CreateCube("Lower Room Red Ceiling", new Vector3(0f, upperY + 0.05f, -3.5f), new Vector3(width, 0.1f, lowZMax - lowZMin), materials.Ceiling, parent);

            CreateCube("Left Exterior Wall", new Vector3(-width * 0.5f, ceilingY * 0.5f, 0.5f), new Vector3(wallT, ceilingY, upZMax - lowZMin), materials.BlueStone, parent);
            CreateCube("Right Exterior Wall", new Vector3(width * 0.5f, ceilingY * 0.5f, 0.5f), new Vector3(wallT, ceilingY, upZMax - lowZMin), materials.BlueStone, parent);
            CreateCube("Upper Back Wall", new Vector3(0f, ceilingY * 0.5f, upZMax), new Vector3(width, ceilingY, wallT), materials.BlueStone, parent);

            CreateCube("Front Wall Left", new Vector3(-3.9f, ceilingY * 0.5f, lowZMin), new Vector3(4.2f, ceilingY, wallT), materials.BlueStone, parent);
            CreateCube("Front Wall Right", new Vector3(3.9f, ceilingY * 0.5f, lowZMin), new Vector3(4.2f, ceilingY, wallT), materials.BlueStone, parent);
            CreateCube("Front Door Header", new Vector3(0f, 4.45f, lowZMin), new Vector3(3.6f, 3.8f, wallT), materials.BlueStone, parent);
            CreateCube("Upper Door Header", new Vector3(0f, 5.4f, upZMax - 0.03f), new Vector3(3.7f, 1.8f, wallT), materials.BlueStone, parent);

            GameObject lowerDoor = CreateCube("Wolf Door Lower", new Vector3(0f, doorH * 0.5f, lowZMin + 0.08f), new Vector3(1.85f, doorH, 0.16f), materials.Door, parent);
            WolfDoor lowerWolfDoor = lowerDoor.AddComponent<WolfDoor>();
            lowerWolfDoor.Configure(new Vector3(2.05f, 0f, 0f), 2.9f, 4.5f, false);

            GameObject upperDoor = CreateCube("Wolf Door Upper", new Vector3(0f, upperY + doorH * 0.5f, upZMax - 0.08f), new Vector3(1.85f, doorH, 0.16f), materials.Door, parent);
            WolfDoor upperWolfDoor = upperDoor.AddComponent<WolfDoor>();
            upperWolfDoor.Configure(new Vector3(-2.05f, 0f, 0f), 2.9f, 4.5f, false);

            CreateStairs(parent, materials);

            CreateCube("Upper Front Parapet Left", new Vector3(-2.4f, upperY + 0.55f, upZMin), new Vector3(6.0f, 1.1f, 0.18f), materials.Metal, parent);
            CreateCube("Upper Front Parapet Right", new Vector3(5.45f, upperY + 0.55f, upZMin), new Vector3(1.1f, 1.1f, 0.18f), materials.Metal, parent);
            CreateCube("Upper Parapet Gold Trim", new Vector3(-2.4f, upperY + 1.15f, upZMin - 0.02f), new Vector3(6.0f, 0.12f, 0.22f), materials.Gold, parent);

            CreateCube("Lower Left Pillar", new Vector3(-4.3f, 1.5f, -4.8f), new Vector3(0.55f, 3f, 0.55f), materials.Metal, parent);
            CreateCube("Lower Right Pillar", new Vector3(4.3f, 1.5f, -4.8f), new Vector3(0.55f, 3f, 0.55f), materials.Metal, parent);
            CreateCube("Upper Left Pillar", new Vector3(-4.4f, upperY + 1.65f, 6.8f), new Vector3(0.55f, 3.3f, 0.55f), materials.Metal, parent);
            CreateCube("Upper Right Pillar", new Vector3(4.4f, upperY + 1.65f, 6.8f), new Vector3(0.55f, 3.3f, 0.55f), materials.Metal, parent);
        }

        private static void CreateStairs(Transform parent, WolfMaterials materials)
        {
            GameObject stairsRoot = new GameObject("Full 3D Staircase");
            stairsRoot.transform.SetParent(parent);

            const int stepCount = 16;
            const float totalRise = 3f;
            const float stepRise = totalRise / stepCount;
            const float stepRun = 0.34f;
            const float stairStartZ = -4.5f;
            const float stairX = 4.65f;
            const float stairWidth = 1.75f;

            for (int i = 1; i <= stepCount; i++)
            {
                float topY = i * stepRise;
                float y = topY * 0.5f;
                float z = stairStartZ + (i - 0.5f) * stepRun;
                GameObject step = CreateCube(
                    $"Wolf Stair Step {i:00}",
                    new Vector3(stairX, y, z),
                    new Vector3(stairWidth, topY, stepRun),
                    materials.Floor,
                    stairsRoot.transform);

                CreateCube(
                    $"Wolf Stair Nose {i:00}",
                    new Vector3(stairX, topY + 0.012f, z - stepRun * 0.35f),
                    new Vector3(stairWidth + 0.03f, 0.024f, 0.035f),
                    materials.Gold,
                    stairsRoot.transform);
            }

            CreateCube("Stair Guard Rail", new Vector3(3.58f, 1.8f, -1.6f), new Vector3(0.16f, 2.2f, 4.8f), materials.Metal, stairsRoot.transform);
            CreateCube("Stair Rail Gold Cap", new Vector3(3.58f, 2.95f, -1.6f), new Vector3(0.22f, 0.12f, 4.8f), materials.Gold, stairsRoot.transform);
        }

        private static void BuildDecor(Transform parent, WolfMaterials materials)
        {
            CreateQuad("Lower Red Banner", new Vector3(0f, 2.05f, -7.86f), new Vector2(1.7f, 2.3f), Quaternion.identity, materials.Banner, parent);
            CreateQuad("Left Wall Banner", new Vector3(-5.86f, 1.9f, -2.0f), new Vector2(1.5f, 2.0f), Quaternion.Euler(0f, 90f, 0f), materials.Banner, parent);
            CreateQuad("Upper Back Banner", new Vector3(0f, 4.9f, 8.86f), new Vector2(1.9f, 2.4f), Quaternion.Euler(0f, 180f, 0f), materials.Banner, parent);

            CreateCrateCluster(parent, materials, -4.5f, -6.5f);
            CreateCrateCluster(parent, materials, 3.7f, 6.7f, 3f);

            CreateCylinder("Lower Barrel A", new Vector3(-3.1f, 0.52f, -2.6f), 0.42f, 1.04f, materials.Metal, parent);
            CreateCylinder("Lower Barrel B", new Vector3(-3.8f, 0.52f, -2.05f), 0.42f, 1.04f, materials.Metal, parent);
            CreateCylinder("Upper Barrel", new Vector3(3.1f, 3.52f, 7.1f), 0.42f, 1.04f, materials.Metal, parent);

            CreateLamp("Lower Ceiling Lamp", new Vector3(0f, 2.82f, -3.8f), parent, materials, new Color(1f, 0.78f, 0.48f), 4.0f, 8f);
            CreateLamp("Stair Lamp", new Vector3(3.8f, 4.2f, -0.9f), parent, materials, new Color(1f, 0.55f, 0.34f), 3.2f, 7f);
            CreateLamp("Upper Ceiling Lamp", new Vector3(0f, 5.95f, 5.7f), parent, materials, new Color(0.8f, 0.92f, 1f), 3.6f, 8f);

            CreatePickup("Gold Key Pickup", new Vector3(-2.9f, 3.35f, 6.5f), materials.Gold, parent);
            CreatePickup("Ammo Pickup", new Vector3(2.2f, 0.25f, -5.1f), materials.Metal, parent);
        }

        private static void BuildActors(Transform parent, WolfMaterials materials)
        {
            CreateBillboard("Lower Guard Standee", new Vector3(2.5f, 1.05f, -2.5f), new Vector2(1.35f, 2.1f), materials.Guard, parent);
            CreateBillboard("Upper Guard Standee", new Vector3(-2.7f, 4.05f, 5.2f), new Vector2(1.35f, 2.1f), materials.Guard, parent);
        }

        private static void CreateLighting(WolfMaterials materials)
        {
            GameObject directional = new GameObject("Low Amber Directional");
            Light directionalLight = directional.AddComponent<Light>();
            directionalLight.type = LightType.Directional;
            directionalLight.color = new Color(1f, 0.78f, 0.48f);
            directionalLight.intensity = 0.22f;
            directional.transform.rotation = Quaternion.Euler(55f, -35f, 0f);

            GameObject lowerFill = new GameObject("Lower Blue Fill Light");
            lowerFill.transform.position = new Vector3(-4.2f, 2.1f, -3.5f);
            Light lowerLight = lowerFill.AddComponent<Light>();
            lowerLight.type = LightType.Point;
            lowerLight.color = new Color(0.35f, 0.55f, 1f);
            lowerLight.range = 8f;
            lowerLight.intensity = 1.2f;

            GameObject upperWarm = new GameObject("Upper Warm Fill Light");
            upperWarm.transform.position = new Vector3(3.8f, 4.7f, 6.2f);
            Light upperLight = upperWarm.AddComponent<Light>();
            upperLight.type = LightType.Point;
            upperLight.color = new Color(1f, 0.54f, 0.28f);
            upperLight.range = 9f;
            upperLight.intensity = 2.1f;
        }

        private static void CreatePlayer(WolfTextures textures)
        {
            GameObject player = new GameObject("Wolf Player");
            player.transform.position = new Vector3(0f, 0f, -6.2f);
            player.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

            CharacterController characterController = player.AddComponent<CharacterController>();
            characterController.height = WolfMiniConstants.PlayerHeight;
            characterController.radius = WolfMiniConstants.PlayerRadius;
            characterController.center = new Vector3(0f, WolfMiniConstants.PlayerCenterY, 0f);
            characterController.stepOffset = WolfMiniConstants.PlayerStepOffset;
            characterController.slopeLimit = 55f;

            SimpleFirstPersonController controller = player.AddComponent<SimpleFirstPersonController>();
            WolfPlayerInteractor interactor = player.AddComponent<WolfPlayerInteractor>();

            GameObject cameraObject = new GameObject("First Person Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(player.transform);
            cameraObject.transform.localPosition = new Vector3(0f, WolfMiniConstants.EyeHeight, 0f);
            cameraObject.transform.localRotation = Quaternion.identity;

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 72f;
            camera.nearClipPlane = 0.05f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.026f, 0.035f);
            cameraObject.AddComponent<AudioListener>();

            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("playerCamera").objectReferenceValue = camera;
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject serializedInteractor = new SerializedObject(interactor);
            serializedInteractor.FindProperty("playerCamera").objectReferenceValue = camera;
            serializedInteractor.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateHud(WolfTextures textures)
        {
            GameObject hud = new GameObject("Wolf HUD");
            WolfHud wolfHud = hud.AddComponent<WolfHud>();

            SerializedObject serializedHud = new SerializedObject(wolfHud);
            serializedHud.FindProperty("faceTexture").objectReferenceValue = textures.Face;
            serializedHud.FindProperty("weaponTexture").objectReferenceValue = textures.Weapon;
            serializedHud.FindProperty("keyTexture").objectReferenceValue = textures.Key;
            serializedHud.FindProperty("showFps").boolValue = true;
            serializedHud.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureRenderSettings()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.055f, 0.055f, 0.07f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.035f, 0.035f, 0.045f);
            RenderSettings.fogDensity = 0.034f;
            RenderSettings.skybox = null;
            RenderSettings.subtractiveShadowColor = new Color(0.03f, 0.025f, 0.02f);
            DynamicGI.UpdateEnvironment();
        }

        private static WolfMaterials CreateMaterials(WolfTextures textures)
        {
            return new WolfMaterials
            {
                BlueStone = CreateMaterial("Mat_WR_BlueStone", textures.BlueStone, Color.white, new Vector2(3f, 2f), 0.05f, 0.16f),
                Floor = CreateMaterial("Mat_WR_GreyFloor", textures.Floor, Color.white, new Vector2(4f, 4f), 0.15f, 0.18f),
                Ceiling = CreateMaterial("Mat_WR_RedCeiling", textures.Ceiling, Color.white, new Vector2(3f, 2f), 0.03f, 0.08f),
                Door = CreateMaterial("Mat_WR_WoodDoor", textures.Door, Color.white, new Vector2(1f, 1f), 0.05f, 0.20f),
                Metal = CreateMaterial("Mat_WR_Metal", textures.Metal, Color.white, new Vector2(2.5f, 1.5f), 0.5f, 0.22f),
                Gold = CreateMaterial("Mat_WR_Gold", textures.Gold, Color.white, new Vector2(2f, 1f), 0.35f, 0.35f),
                Banner = CreateMaterial("Mat_WR_Banner", textures.Banner, Color.white, Vector2.one, 0f, 0f, true),
                Guard = CreateMaterial("Mat_WR_Guard", textures.Guard, Color.white, Vector2.one, 0f, 0f, true),
                Lamp = CreateMaterial("Mat_WR_Lamp", null, new Color(1f, 0.78f, 0.36f), Vector2.one, 0f, 0f, false, true, new Color(1f, 0.58f, 0.22f) * 2.2f)
            };
        }

        private static Material CreateMaterial(
            string name,
            Texture2D texture,
            Color color,
            Vector2 tiling,
            float metallic,
            float gloss,
            bool transparent = false,
            bool emissive = false,
            Color emission = default)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = transparent ? Shader.Find("Unlit/Transparent") : Shader.Find("Standard");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = transparent && Shader.Find("Unlit/Transparent") != null
                ? Shader.Find("Unlit/Transparent")
                : Shader.Find("Standard");
            material.color = color;
            material.mainTexture = texture;
            material.mainTextureScale = tiling;

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", metallic);
            }

            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", gloss);
            }

            if (transparent && material.shader.name == "Standard")
            {
                material.SetFloat("_Mode", 3f);
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;
            }

            if (emissive)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static WolfTextures LoadOrCreateTextures()
        {
            return new WolfTextures
            {
                BlueStone = WriteAndImportTexture("Tex_WR_BlueStone", BuildBlueStoneTex, false, TextureWrapMode.Repeat, FilterMode.Bilinear),
                Floor = WriteAndImportTexture("Tex_WR_GreyFloor", BuildFloorTex, false, TextureWrapMode.Repeat, FilterMode.Bilinear),
                Ceiling = WriteAndImportTexture("Tex_WR_RedCeiling", BuildCeilingTex, false, TextureWrapMode.Repeat, FilterMode.Bilinear),
                Door = WriteAndImportTexture("Tex_WR_WoodDoor", BuildDoorTex, false, TextureWrapMode.Repeat, FilterMode.Bilinear),
                Metal = WriteAndImportTexture("Tex_WR_Metal", BuildMetalTex, false, TextureWrapMode.Repeat, FilterMode.Bilinear),
                Gold = WriteAndImportTexture("Tex_WR_Gold", BuildGoldTex, false, TextureWrapMode.Repeat, FilterMode.Bilinear),
                Banner = WriteAndImportTexture("Tex_WR_Banner", BuildBannerTex, true, TextureWrapMode.Clamp, FilterMode.Point),
                Guard = WriteAndImportTexture("Tex_WR_Guard", BuildGuardTex, true, TextureWrapMode.Clamp, FilterMode.Point),
                Weapon = WriteAndImportTexture("Tex_WR_WeaponPistol", BuildWeaponTex, true, TextureWrapMode.Clamp, FilterMode.Point),
                Face = WriteAndImportTexture("Tex_WR_Face", BuildFaceTex, true, TextureWrapMode.Clamp, FilterMode.Point),
                Key = WriteAndImportTexture("Tex_WR_Key", BuildKeyTex, true, TextureWrapMode.Clamp, FilterMode.Point)
            };
        }

        private static Texture2D WriteAndImportTexture(string name, Func<Texture2D> build, bool alpha, TextureWrapMode wrapMode, FilterMode filterMode)
        {
            string assetPath = $"{TextureFolder}/{name}.png";
            string absPath = ToAbsoluteAssetPath(assetPath);

            if (!File.Exists(absPath))
            {
                Texture2D texture = build();
                File.WriteAllBytes(absPath, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
            }

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = alpha ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
            importer.mipmapEnabled = !alpha;
            importer.wrapMode = wrapMode;
            importer.filterMode = filterMode;
            importer.anisoLevel = 4;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        private static Texture2D BuildBlueStoneTex()
        {
            const int size = 512;
            Texture2D texture = NewTexture(size);
            Color[] pixels = new Color[size * size];
            Color baseLow = new Color(0.11f, 0.16f, 0.27f);
            Color baseHigh = new Color(0.30f, 0.39f, 0.58f);
            Color mortar = new Color(0.035f, 0.04f, 0.055f);

            for (int y = 0; y < size; y++)
            {
                int row = y / 64;
                int offset = (row % 2) * 64;
                for (int x = 0; x < size; x++)
                {
                    int brickX = Mod(x + offset, 128);
                    int brickY = Mod(y, 64);
                    bool seam = brickX < 4 || brickY < 4;
                    float n = Mathf.PerlinNoise((x + 31f) / 37f, (y + 77f) / 29f);
                    float stain = Mathf.PerlinNoise((x + 9f) / 92f, (y + 133f) / 52f);
                    float edge = Mathf.Min(Mathf.Min(brickX, 127 - brickX), Mathf.Min(brickY, 63 - brickY)) / 16f;
                    Color c = seam ? mortar : Color.Lerp(baseLow, baseHigh, Mathf.Clamp01(n * 0.8f + 0.15f));
                    c *= Mathf.Lerp(0.72f, 1.12f, Mathf.Clamp01(edge));
                    if (!seam && stain > 0.76f)
                    {
                        c = Color.Lerp(c, new Color(0.055f, 0.06f, 0.075f), 0.45f);
                    }

                    if (!seam && Mathf.Abs(brickY - 31) < 2 && n > 0.64f)
                    {
                        c *= 0.55f;
                    }

                    pixels[y * size + x] = ClampColor(c);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(true);
            return texture;
        }

        private static Texture2D BuildFloorTex()
        {
            const int size = 512;
            Texture2D texture = NewTexture(size);
            Color[] pixels = new Color[size * size];
            Color low = new Color(0.16f, 0.16f, 0.17f);
            Color high = new Color(0.39f, 0.39f, 0.40f);
            Color seamColor = new Color(0.045f, 0.045f, 0.05f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int tx = Mod(x, 96);
                    int ty = Mod(y, 96);
                    bool seam = tx < 4 || ty < 4;
                    float n = Mathf.PerlinNoise((x + 123f) / 44f, (y + 47f) / 44f);
                    Color c = seam ? seamColor : Color.Lerp(low, high, n);
                    if (!seam && ((x + y) % 37 == 0 || (x - y + 2000) % 53 == 0))
                    {
                        c *= 0.65f;
                    }

                    pixels[y * size + x] = ClampColor(c);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(true);
            return texture;
        }

        private static Texture2D BuildCeilingTex()
        {
            const int size = 512;
            Texture2D texture = NewTexture(size);
            Color[] pixels = new Color[size * size];
            Color low = new Color(0.20f, 0.05f, 0.035f);
            Color high = new Color(0.46f, 0.12f, 0.07f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool seam = Mod(x, 128) < 4 || Mod(y, 128) < 4;
                    float n = Mathf.PerlinNoise((x + 211f) / 36f, (y + 18f) / 58f);
                    Color c = seam ? new Color(0.06f, 0.018f, 0.014f) : Color.Lerp(low, high, n);
                    pixels[y * size + x] = ClampColor(c);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(true);
            return texture;
        }

        private static Texture2D BuildDoorTex()
        {
            const int size = 512;
            Texture2D texture = NewTexture(size);
            Color[] pixels = new Color[size * size];
            Color woodA = new Color(0.33f, 0.18f, 0.08f);
            Color woodB = new Color(0.66f, 0.38f, 0.14f);
            Color metal = new Color(0.13f, 0.13f, 0.14f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int plank = x / 64;
                    bool plankGap = Mod(x, 64) < 4;
                    bool brace = (y > 105 && y < 145) || (y > 365 && y < 405) || Math.Abs(x - y) < 6 || Math.Abs((size - x) - y) < 6;
                    float n = Mathf.PerlinNoise((x + plank * 17f) / 18f, y / 54f);
                    Color c = Color.Lerp(woodA, woodB, n);
                    if (plankGap) c = new Color(0.055f, 0.035f, 0.02f);
                    if (brace) c = metal * Mathf.Lerp(0.85f, 1.2f, n);
                    if (brace && ((x - 16) % 96 < 8) && ((y - 10) % 120 < 8)) c = new Color(0.75f, 0.62f, 0.34f);
                    pixels[y * size + x] = ClampColor(c);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(true);
            return texture;
        }

        private static Texture2D BuildMetalTex()
        {
            const int size = 512;
            Texture2D texture = NewTexture(size);
            Color[] pixels = new Color[size * size];
            Color low = new Color(0.10f, 0.11f, 0.13f);
            Color high = new Color(0.34f, 0.36f, 0.39f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool seam = Mod(x, 128) < 3 || Mod(y, 96) < 3;
                    float n = Mathf.PerlinNoise((x + 22f) / 26f, (y + 310f) / 42f);
                    Color c = seam ? new Color(0.035f, 0.038f, 0.044f) : Color.Lerp(low, high, n);
                    int cx = Mod(x, 128) - 16;
                    int cy = Mod(y, 96) - 16;
                    if (cx * cx + cy * cy < 42)
                    {
                        c = new Color(0.62f, 0.58f, 0.48f);
                    }

                    pixels[y * size + x] = ClampColor(c);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(true);
            return texture;
        }

        private static Texture2D BuildGoldTex()
        {
            const int size = 256;
            Texture2D texture = NewTexture(size);
            Color[] pixels = new Color[size * size];
            Color low = new Color(0.55f, 0.35f, 0.07f);
            Color high = new Color(1f, 0.78f, 0.25f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float stripe = Mathf.Sin((x + y) * 0.09f) * 0.5f + 0.5f;
                    float n = Mathf.PerlinNoise((x + 7f) / 31f, (y + 93f) / 31f);
                    Color c = Color.Lerp(low, high, Mathf.Clamp01(stripe * 0.45f + n * 0.55f));
                    pixels[y * size + x] = ClampColor(c);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(true);
            return texture;
        }

        private static Texture2D BuildBannerTex()
        {
            const int w = 256;
            const int h = 512;
            Texture2D texture = NewTexture(w, h);
            Color[] pixels = ClearPixels(w, h);
            Color red = new Color(0.55f, 0.02f, 0.02f, 1f);
            Color redDark = new Color(0.20f, 0.01f, 0.01f, 1f);
            Color gold = new Color(0.95f, 0.68f, 0.18f, 1f);
            Color black = new Color(0.015f, 0.014f, 0.012f, 1f);

            FillRect(pixels, w, h, 32, 18, 192, 462, red);
            for (int y = 18; y < 480; y++)
            {
                for (int x = 32; x < 224; x++)
                {
                    float shade = Mathf.Abs(x - 128) / 128f;
                    Color c = Color.Lerp(red, redDark, shade * 0.55f);
                    if (x < 38 || x > 218 || y < 24 || y > 470) c = gold;
                    pixels[y * w + x] = c;
                }
            }

            FillCircle(pixels, w, h, 128, 300, 64, new Color(0.92f, 0.86f, 0.70f, 1f));
            FillCircle(pixels, w, h, 128, 300, 43, red);
            FillRect(pixels, w, h, 88, 290, 80, 18, black);
            FillRect(pixels, w, h, 112, 256, 32, 86, black);
            FillTriangle(pixels, w, h, new Vector2Int(128, 355), new Vector2Int(82, 308), new Vector2Int(110, 308), black);
            FillTriangle(pixels, w, h, new Vector2Int(128, 355), new Vector2Int(174, 308), new Vector2Int(146, 308), black);
            FillTriangle(pixels, w, h, new Vector2Int(128, 238), new Vector2Int(82, 286), new Vector2Int(174, 286), black);

            texture.SetPixels(pixels);
            texture.Apply(false);
            return texture;
        }

        private static Texture2D BuildGuardTex()
        {
            const int w = 128;
            const int h = 192;
            Texture2D texture = NewTexture(w, h);
            Color[] pixels = ClearPixels(w, h);
            Color skin = new Color(0.86f, 0.62f, 0.40f, 1f);
            Color hair = new Color(0.17f, 0.10f, 0.05f, 1f);
            Color uniform = new Color(0.19f, 0.42f, 0.36f, 1f);
            Color uniformDark = new Color(0.08f, 0.19f, 0.18f, 1f);
            Color black = new Color(0.02f, 0.018f, 0.015f, 1f);
            Color metal = new Color(0.55f, 0.58f, 0.60f, 1f);

            FillRect(pixels, w, h, 43, 16, 18, 44, black);
            FillRect(pixels, w, h, 70, 16, 18, 44, black);
            FillRect(pixels, w, h, 38, 58, 52, 66, uniform);
            FillRect(pixels, w, h, 34, 112, 60, 18, uniformDark);
            FillRect(pixels, w, h, 48, 132, 32, 30, skin);
            FillRect(pixels, w, h, 46, 154, 36, 13, hair);
            FillRect(pixels, w, h, 56, 144, 5, 5, black);
            FillRect(pixels, w, h, 70, 144, 5, 5, black);
            FillRect(pixels, w, h, 58, 136, 18, 4, black);
            FillRect(pixels, w, h, 28, 76, 16, 48, uniformDark);
            FillRect(pixels, w, h, 87, 76, 16, 48, uniformDark);
            FillRect(pixels, w, h, 43, 84, 46, 8, black);
            FillRect(pixels, w, h, 57, 86, 16, 48, metal);
            FillCircle(pixels, w, h, 65, 96, 8, new Color(0.08f, 0.08f, 0.08f, 1f));
            FillRect(pixels, w, h, 36, 7, 28, 11, black);
            FillRect(pixels, w, h, 68, 7, 28, 11, black);

            AddPixelNoise(pixels, w, h, 0.06f);
            texture.SetPixels(pixels);
            texture.Apply(false);
            return texture;
        }

        private static Texture2D BuildWeaponTex()
        {
            const int w = 256;
            const int h = 256;
            Texture2D texture = NewTexture(w, h);
            Color[] pixels = ClearPixels(w, h);
            Color hand = new Color(0.82f, 0.56f, 0.35f, 1f);
            Color metal = new Color(0.18f, 0.18f, 0.19f, 1f);
            Color shine = new Color(0.72f, 0.74f, 0.76f, 1f);
            Color dark = new Color(0.04f, 0.04f, 0.045f, 1f);

            FillRect(pixels, w, h, 94, 54, 68, 80, hand);
            FillRect(pixels, w, h, 104, 118, 50, 38, metal);
            FillRect(pixels, w, h, 82, 154, 104, 20, metal);
            FillRect(pixels, w, h, 176, 160, 38, 12, dark);
            FillRect(pixels, w, h, 112, 133, 32, 24, dark);
            FillRect(pixels, w, h, 89, 176, 118, 10, shine);
            FillRect(pixels, w, h, 120, 154, 14, 50, metal);
            FillRect(pixels, w, h, 114, 202, 28, 26, dark);
            FillRect(pixels, w, h, 96, 64, 16, 56, new Color(0.95f, 0.70f, 0.48f, 1f));
            FillRect(pixels, w, h, 142, 64, 16, 56, new Color(0.95f, 0.70f, 0.48f, 1f));

            texture.SetPixels(pixels);
            texture.Apply(false);
            return texture;
        }

        private static Texture2D BuildFaceTex()
        {
            const int w = 128;
            const int h = 128;
            Texture2D texture = NewTexture(w, h);
            Color[] pixels = ClearPixels(w, h);
            Color skin = new Color(0.86f, 0.61f, 0.40f, 1f);
            Color shade = new Color(0.55f, 0.31f, 0.21f, 1f);
            Color hair = new Color(0.16f, 0.10f, 0.07f, 1f);
            Color white = new Color(0.90f, 0.86f, 0.78f, 1f);
            Color black = new Color(0.02f, 0.018f, 0.014f, 1f);

            FillCircle(pixels, w, h, 64, 62, 39, skin);
            FillRect(pixels, w, h, 33, 88, 62, 16, hair);
            FillRect(pixels, w, h, 36, 82, 55, 12, hair);
            FillRect(pixels, w, h, 41, 61, 17, 7, white);
            FillRect(pixels, w, h, 70, 61, 17, 7, white);
            FillRect(pixels, w, h, 48, 62, 5, 5, black);
            FillRect(pixels, w, h, 77, 62, 5, 5, black);
            FillRect(pixels, w, h, 48, 35, 32, 7, black);
            FillRect(pixels, w, h, 56, 42, 17, 5, white);
            FillRect(pixels, w, h, 61, 51, 7, 13, shade);
            FillRect(pixels, w, h, 28, 23, 72, 15, new Color(0.06f, 0.07f, 0.10f, 1f));

            texture.SetPixels(pixels);
            texture.Apply(false);
            return texture;
        }

        private static Texture2D BuildKeyTex()
        {
            const int w = 64;
            const int h = 64;
            Texture2D texture = NewTexture(w, h);
            Color[] pixels = ClearPixels(w, h);
            Color gold = new Color(1f, 0.76f, 0.18f, 1f);
            Color darkGold = new Color(0.45f, 0.28f, 0.03f, 1f);

            FillCircle(pixels, w, h, 19, 34, 13, gold);
            FillCircle(pixels, w, h, 19, 34, 6, Color.clear);
            FillRect(pixels, w, h, 29, 31, 28, 7, gold);
            FillRect(pixels, w, h, 48, 24, 6, 14, gold);
            FillRect(pixels, w, h, 55, 24, 5, 10, darkGold);
            FillRect(pixels, w, h, 29, 29, 28, 2, darkGold);

            texture.SetPixels(pixels);
            texture.Apply(false);
            return texture;
        }

        private static GameObject CreateCube(string name, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gameObject.name = name;
            gameObject.transform.position = position;
            gameObject.transform.localScale = scale;
            gameObject.transform.SetParent(parent, true);
            gameObject.isStatic = true;

            Renderer renderer = gameObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            return gameObject;
        }

        private static GameObject CreateQuad(string name, Vector3 position, Vector2 scale, Quaternion rotation, Material material, Transform parent)
        {
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            gameObject.name = name;
            gameObject.transform.position = position;
            gameObject.transform.rotation = rotation;
            gameObject.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            gameObject.transform.SetParent(parent, true);
            gameObject.isStatic = true;

            Collider collider = gameObject.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            Renderer renderer = gameObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            return gameObject;
        }

        private static GameObject CreateBillboard(string name, Vector3 position, Vector2 scale, Material material, Transform parent)
        {
            GameObject billboard = CreateQuad(name, position, scale, Quaternion.identity, material, parent);
            billboard.AddComponent<WolfBillboard>();
            return billboard;
        }

        private static void CreateCylinder(string name, Vector3 position, float radius, float height, Material material, Transform parent)
        {
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            gameObject.name = name;
            gameObject.transform.position = position;
            gameObject.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
            gameObject.transform.SetParent(parent, true);
            gameObject.isStatic = true;
            gameObject.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void CreateLamp(string name, Vector3 position, Transform parent, WolfMaterials materials, Color color, float intensity, float range)
        {
            GameObject lamp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lamp.name = name;
            lamp.transform.position = position;
            lamp.transform.localScale = Vector3.one * 0.22f;
            lamp.transform.SetParent(parent, true);
            lamp.GetComponent<Renderer>().sharedMaterial = materials.Lamp;

            Collider collider = lamp.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            GameObject lightObject = new GameObject($"{name} Light");
            lightObject.transform.position = position + Vector3.down * 0.08f;
            lightObject.transform.SetParent(parent, true);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
        }

        private static void CreateCrateCluster(Transform parent, WolfMaterials materials, float x, float z, float y = 0f)
        {
            CreateCube("Supply Crate A", new Vector3(x, y + 0.35f, z), new Vector3(0.7f, 0.7f, 0.7f), materials.Door, parent);
            CreateCube("Supply Crate B", new Vector3(x + 0.75f, y + 0.35f, z + 0.25f), new Vector3(0.7f, 0.7f, 0.7f), materials.Door, parent);
            CreateCube("Supply Crate C", new Vector3(x + 0.35f, y + 1.05f, z + 0.1f), new Vector3(0.7f, 0.7f, 0.7f), materials.Door, parent);
        }

        private static void CreatePickup(string name, Vector3 position, Material material, Transform parent)
        {
            GameObject pickup = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pickup.name = name;
            pickup.transform.position = position;
            pickup.transform.localScale = new Vector3(0.32f, 0.22f, 0.32f);
            pickup.transform.SetParent(parent, true);
            pickup.GetComponent<Renderer>().sharedMaterial = material;

            Collider collider = pickup.GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }
        }

        private static void EnsureFolders()
        {
            CreateFolderIfMissing("Assets", "Scenes");
            CreateFolderIfMissing("Assets", "Materials");
            CreateFolderIfMissing("Assets/Materials", "WolfRemake");
            CreateFolderIfMissing("Assets", "Textures");
            CreateFolderIfMissing("Assets/Textures", "WolfRemake");
            CreateFolderIfMissing("Assets", "Scripts");
        }

        private static void CreateFolderIfMissing(string parent, string child)
        {
            string path = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
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
            return NewTexture(size, size);
        }

        private static Texture2D NewTexture(int width, int height)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, true);
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Bilinear;
            return texture;
        }

        private static Color[] ClearPixels(int width, int height)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.clear;
            }
            return pixels;
        }

        private static void FillRect(Color[] pixels, int width, int height, int x, int y, int w, int h, Color color)
        {
            int xMax = Mathf.Min(width, x + w);
            int yMax = Mathf.Min(height, y + h);
            for (int yy = Mathf.Max(0, y); yy < yMax; yy++)
            {
                for (int xx = Mathf.Max(0, x); xx < xMax; xx++)
                {
                    pixels[yy * width + xx] = color;
                }
            }
        }

        private static void FillCircle(Color[] pixels, int width, int height, int cx, int cy, int radius, Color color)
        {
            int r2 = radius * radius;
            for (int y = cy - radius; y <= cy + radius; y++)
            {
                if (y < 0 || y >= height) continue;
                for (int x = cx - radius; x <= cx + radius; x++)
                {
                    if (x < 0 || x >= width) continue;
                    int dx = x - cx;
                    int dy = y - cy;
                    if (dx * dx + dy * dy <= r2)
                    {
                        pixels[y * width + x] = color;
                    }
                }
            }
        }

        private static void FillTriangle(Color[] pixels, int width, int height, Vector2Int a, Vector2Int b, Vector2Int c, Color color)
        {
            int minX = Mathf.Max(0, Mathf.Min(a.x, Mathf.Min(b.x, c.x)));
            int maxX = Mathf.Min(width - 1, Mathf.Max(a.x, Mathf.Max(b.x, c.x)));
            int minY = Mathf.Max(0, Mathf.Min(a.y, Mathf.Min(b.y, c.y)));
            int maxY = Mathf.Min(height - 1, Mathf.Max(a.y, Mathf.Max(b.y, c.y)));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Vector2Int p = new Vector2Int(x, y);
                    if (SameSide(p, a, b, c) && SameSide(p, b, a, c) && SameSide(p, c, a, b))
                    {
                        pixels[y * width + x] = color;
                    }
                }
            }
        }

        private static bool SameSide(Vector2Int p1, Vector2Int p2, Vector2Int a, Vector2Int b)
        {
            int cp1 = Cross(b - a, p1 - a);
            int cp2 = Cross(b - a, p2 - a);
            return cp1 * cp2 >= 0;
        }

        private static int Cross(Vector2Int a, Vector2Int b)
        {
            return a.x * b.y - a.y * b.x;
        }

        private static void AddPixelNoise(Color[] pixels, int width, int height, float amount)
        {
            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a <= 0f) continue;
                float n = Mathf.PerlinNoise((i % width) / 9f, (i / width) / 11f) - 0.5f;
                pixels[i] = ClampColor(pixels[i] * (1f + n * amount));
                pixels[i].a = 1f;
            }
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

        private sealed class WolfTextures
        {
            public Texture2D BlueStone;
            public Texture2D Floor;
            public Texture2D Ceiling;
            public Texture2D Door;
            public Texture2D Metal;
            public Texture2D Gold;
            public Texture2D Banner;
            public Texture2D Guard;
            public Texture2D Weapon;
            public Texture2D Face;
            public Texture2D Key;
        }

        private sealed class WolfMaterials
        {
            public Material BlueStone;
            public Material Floor;
            public Material Ceiling;
            public Material Door;
            public Material Metal;
            public Material Gold;
            public Material Banner;
            public Material Guard;
            public Material Lamp;
        }
    }
}
