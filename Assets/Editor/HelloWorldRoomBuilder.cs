using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace HelloWorldRoom.Editor
{
    public static class HelloWorldRoomBuilder
    {
        private const string ScenePath = "Assets/Scenes/HelloWorldRoom.unity";
        private const string MaterialFolder = "Assets/Materials";
        private const string TextureFolder = "Assets/Textures";

        [MenuItem("Tools/Hello World/Create Walkable Room")]
        public static void Build()
        {
            EnsureFolders();

            Material floor = CreateMaterial("Mat_Floor_WarmConcrete", new Color(0.55f, 0.52f, 0.46f));
            Material wall = CreateMaterial("Mat_Wall_SoftWhite", new Color(0.88f, 0.88f, 0.82f));
            Material accent = CreateMaterial("Mat_Accent_Teal", new Color(0.11f, 0.52f, 0.58f));
            Material wood = CreateMaterial("Mat_Wood", new Color(0.50f, 0.31f, 0.16f));
            Material lamp = CreateMaterial("Mat_Lamp_Glow", new Color(1.00f, 0.78f, 0.38f), true);
            Material dark = CreateMaterial("Mat_DarkMetal", new Color(0.08f, 0.09f, 0.10f));

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "HelloWorldRoom";

            RenderSettings.ambientLight = new Color(0.45f, 0.47f, 0.50f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.62f, 0.67f, 0.70f);
            RenderSettings.fogDensity = 0.012f;

            CreateRoom(floor, wall, accent);
            CreateProps(wood, accent, lamp, dark);
            CreateLighting(lamp);
            CreatePlayer();
            CreateHelloText(accent);
            CreateInstructionsText();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Hello World room created at {ScenePath}. Press Play, click the Game view, then use WASD, mouse, Space, Shift, and Esc.");
        }

        [MenuItem("Tools/Hello World/Add Second Floor")]
        public static void AddSecondFloor()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.name != "HelloWorldRoom")
            {
                Debug.LogWarning("[AddSecondFloor] Active scene is not HelloWorldRoom; aborting.");
                return;
            }

            if (GameObject.Find("Second Floor") != null)
            {
                Debug.LogWarning("[AddSecondFloor] 'Second Floor' already exists in the scene; aborting.");
                return;
            }

            EnsureFolders();

            Material wall = CreateMaterial("Mat_Wall_SoftWhite", new Color(0.88f, 0.88f, 0.82f));
            Material wood = CreateMaterial("Mat_Wood", new Color(0.50f, 0.31f, 0.16f));
            Material accent = CreateMaterial("Mat_Accent_Teal", new Color(0.11f, 0.52f, 0.58f));
            Material lamp = CreateMaterial("Mat_Lamp_Glow", new Color(1.00f, 0.78f, 0.38f), true);
            Material dark = CreateMaterial("Mat_DarkMetal", new Color(0.08f, 0.09f, 0.10f));

            // 1. Raise side walls from 3 m tall to 6 m tall (centered y: 1.5 -> 3, scale.y: 3 -> 6).
            string[] wallNames = { "Back Wall", "Left Wall", "Right Wall", "Front Wall Left", "Front Wall Right" };
            foreach (string n in wallNames)
            {
                GameObject w = GameObject.Find(n);
                if (w == null)
                {
                    continue;
                }

                Undo.RecordObject(w.transform, "Raise wall");
                Vector3 p = w.transform.position;
                Vector3 s = w.transform.localScale;
                w.transform.position = new Vector3(p.x, 3f, p.z);
                w.transform.localScale = new Vector3(s.x, 6f, s.z);
            }

            // 2. Lift the ceiling so the room becomes a 6 m high double-height shell.
            GameObject ceiling = GameObject.Find("Ceiling");
            if (ceiling != null)
            {
                Undo.RecordObject(ceiling.transform, "Raise ceiling");
                Vector3 cp = ceiling.transform.position;
                ceiling.transform.position = new Vector3(cp.x, 6.05f, cp.z);
            }

            // 3. Patch the front wall above the door header, since the original wall stopped at y=3.
            GameObject aboveDoor = CreateCube(
                "Above Door Wall",
                new Vector3(0f, 4.5f, -5f),
                new Vector3(2.8f, 3f, 0.25f),
                wall);
            Undo.RegisterCreatedObjectUndo(aboveDoor, "Create Above Door Wall");

            // 4. Mezzanine slab covering the back half of the room. Top surface at y = 3.0.
            GameObject mezz = CreateCube(
                "Second Floor",
                new Vector3(0f, 2.9f, 3.1875f),
                new Vector3(9.75f, 0.2f, 3.375f),
                wood);
            Undo.RegisterCreatedObjectUndo(mezz, "Create Second Floor");

            // 5. Stair flight along the right wall: 15 risers, 0.2 m tall, 0.3 m run, 1.5 m wide.
            GameObject stairsRoot = new GameObject("Stairs");
            Undo.RegisterCreatedObjectUndo(stairsRoot, "Create Stairs");
            const int stepCount = 15;
            const float stepRise = 0.2f;
            const float stepRun = 0.3f;
            const float stairStartZ = -3f;
            const float stairCenterX = 4.125f;
            const float stairWidth = 1.5f;
            for (int i = 1; i <= stepCount; i++)
            {
                float topY = i * stepRise;
                float yCenter = topY * 0.5f;
                float zCenter = stairStartZ + (i - 1) * stepRun + stepRun * 0.5f;
                GameObject step = CreateCube(
                    $"Step {i:00}",
                    new Vector3(stairCenterX, yCenter, zCenter),
                    new Vector3(stairWidth, topY, stepRun),
                    dark);
                step.transform.SetParent(stairsRoot.transform, true);
            }

            // 6. Parapet wall + accent trim along the open edge of the mezzanine (z = +1.5).
            //    Skip the rightmost lane where the stairs land (x in [3.375, 4.875]).
            GameObject parapet = CreateCube(
                "Mezzanine Parapet",
                new Vector3(-0.75f, 3.5f, 1.5f),
                new Vector3(8.25f, 1f, 0.1f),
                wall);
            Undo.RegisterCreatedObjectUndo(parapet, "Create Mezzanine Parapet");

            GameObject parapetTrim = CreateCube(
                "Mezzanine Parapet Trim",
                new Vector3(-0.75f, 4.05f, 1.5f),
                new Vector3(8.25f, 0.06f, 0.14f),
                accent);
            Undo.RegisterCreatedObjectUndo(parapetTrim, "Create Mezzanine Parapet Trim");

            // 7. Mezzanine accent lamp (glowing sphere + warm point light).
            GameObject mezzLamp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            mezzLamp.name = "Mezzanine Glow Lamp";
            mezzLamp.transform.position = new Vector3(0f, 4.7f, 3.2f);
            mezzLamp.transform.localScale = Vector3.one * 0.4f;
            mezzLamp.GetComponent<Renderer>().sharedMaterial = lamp;
            Undo.RegisterCreatedObjectUndo(mezzLamp, "Create Mezzanine Lamp");

            GameObject mezzLightObject = new GameObject("Mezzanine Light");
            mezzLightObject.transform.position = new Vector3(0f, 4.5f, 3.2f);
            Light mezzLight = mezzLightObject.AddComponent<Light>();
            mezzLight.type = LightType.Point;
            mezzLight.range = 10f;
            mezzLight.intensity = 4f;
            mezzLight.color = new Color(1f, 0.82f, 0.5f);
            Undo.RegisterCreatedObjectUndo(mezzLightObject, "Create Mezzanine Light");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("[AddSecondFloor] Mezzanine, stairs, parapet and accent light added; scene saved.");
        }

        private static void EnsureFolders()
        {
            CreateFolderIfMissing("Assets", "Scenes");
            CreateFolderIfMissing("Assets", "Scripts");
            CreateFolderIfMissing("Assets", "Editor");
            CreateFolderIfMissing("Assets", "Materials");
            CreateFolderIfMissing("Assets", "Textures");
        }

        private static void CreateFolderIfMissing(string parent, string child)
        {
            string path = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static Material CreateMaterial(string name, Color color, bool emissive = false)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material material = new Material(shader);
            material.color = color;

            if (emissive)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 1.7f);
            }

            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void CreateRoom(Material floor, Material wall, Material accent)
        {
            CreateCube("Floor", new Vector3(0f, -0.1f, 0f), new Vector3(10f, 0.2f, 10f), floor);
            CreateCube("Ceiling", new Vector3(0f, 3.05f, 0f), new Vector3(10f, 0.12f, 10f), wall);

            CreateCube("Back Wall", new Vector3(0f, 1.5f, 5f), new Vector3(10f, 3f, 0.25f), wall);
            CreateCube("Left Wall", new Vector3(-5f, 1.5f, 0f), new Vector3(0.25f, 3f, 10f), wall);
            CreateCube("Right Wall", new Vector3(5f, 1.5f, 0f), new Vector3(0.25f, 3f, 10f), wall);

            CreateCube("Front Wall Left", new Vector3(-3.2f, 1.5f, -5f), new Vector3(3.6f, 3f, 0.25f), wall);
            CreateCube("Front Wall Right", new Vector3(3.2f, 1.5f, -5f), new Vector3(3.6f, 3f, 0.25f), wall);
            CreateCube("Door Header", new Vector3(0f, 2.62f, -5f), new Vector3(2.8f, 0.76f, 0.25f), wall);

            CreateCube("Back Accent Panel", new Vector3(0f, 1.55f, 4.86f), new Vector3(5f, 1.7f, 0.05f), accent);
        }

        private static void CreateProps(Material wood, Material accent, Material lamp, Material dark)
        {
            CreateCube("Table Top", new Vector3(-2.7f, 0.82f, 1.6f), new Vector3(2f, 0.15f, 1.1f), wood);
            CreateCube("Table Leg FL", new Vector3(-3.55f, 0.38f, 1.15f), new Vector3(0.16f, 0.75f, 0.16f), wood);
            CreateCube("Table Leg FR", new Vector3(-1.85f, 0.38f, 1.15f), new Vector3(0.16f, 0.75f, 0.16f), wood);
            CreateCube("Table Leg BL", new Vector3(-3.55f, 0.38f, 2.05f), new Vector3(0.16f, 0.75f, 0.16f), wood);
            CreateCube("Table Leg BR", new Vector3(-1.85f, 0.38f, 2.05f), new Vector3(0.16f, 0.75f, 0.16f), wood);

            CreateCube("Accent Cube", new Vector3(2.8f, 0.45f, 1.8f), Vector3.one * 0.9f, accent);
            CreateCube("Dark Step", new Vector3(0f, 0.15f, -3.7f), new Vector3(2.2f, 0.3f, 0.7f), dark);

            GameObject glowSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            glowSphere.name = "Glowing Hello Lamp";
            glowSphere.transform.position = new Vector3(-2.7f, 1.12f, 1.6f);
            glowSphere.transform.localScale = Vector3.one * 0.35f;
            glowSphere.GetComponent<Renderer>().sharedMaterial = lamp;
        }

        private static void CreateLighting(Material lamp)
        {
            GameObject directional = new GameObject("Soft Directional Light");
            Light directionalLight = directional.AddComponent<Light>();
            directionalLight.type = LightType.Directional;
            directionalLight.intensity = 0.8f;
            directionalLight.color = new Color(1f, 0.95f, 0.86f);
            directional.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            GameObject roomLight = new GameObject("Warm Room Light");
            Light point = roomLight.AddComponent<Light>();
            point.type = LightType.Point;
            point.range = 9f;
            point.intensity = 4.5f;
            point.color = new Color(1f, 0.78f, 0.48f);
            roomLight.transform.position = new Vector3(-2.7f, 1.45f, 1.6f);

            GameObject fillLight = new GameObject("Cool Fill Light");
            Light fill = fillLight.AddComponent<Light>();
            fill.type = LightType.Point;
            fill.range = 8f;
            fill.intensity = 2f;
            fill.color = new Color(0.48f, 0.82f, 1f);
            fillLight.transform.position = new Vector3(3.5f, 2.2f, -2.8f);
        }

        private static void CreatePlayer()
        {
            GameObject player = new GameObject("Player");
            player.transform.position = new Vector3(0f, 0f, -2.8f);

            CharacterController characterController = player.AddComponent<CharacterController>();
            characterController.height = 1.8f;
            characterController.radius = 0.35f;
            characterController.center = new Vector3(0f, 0.9f, 0f);
            characterController.slopeLimit = 50f;

            SimpleFirstPersonController controller = player.AddComponent<SimpleFirstPersonController>();

            GameObject cameraObject = new GameObject("First Person Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(player.transform);
            cameraObject.transform.localPosition = new Vector3(0f, 1.62f, 0f);
            cameraObject.transform.localRotation = Quaternion.identity;

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 72f;
            camera.nearClipPlane = 0.05f;
            cameraObject.AddComponent<AudioListener>();

            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("playerCamera").objectReferenceValue = camera;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateHelloText(Material accent)
        {
            GameObject textObject = new GameObject("HELLO WORLD Text");
            TextMesh textMesh = textObject.AddComponent<TextMesh>();
            textMesh.text = "HELLO WORLD";
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.characterSize = 0.45f;
            textMesh.fontSize = 64;
            textMesh.color = Color.white;
            textObject.transform.position = new Vector3(0f, 1.7f, 4.78f);
            textObject.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        }

        private static void CreateInstructionsText()
        {
            GameObject textObject = new GameObject("Controls Text");
            TextMesh textMesh = textObject.AddComponent<TextMesh>();
            textMesh.text = "WASD - move\\nMouse - look\\nSpace - jump\\nShift - run\\nEsc - unlock mouse";
            textMesh.anchor = TextAnchor.MiddleLeft;
            textMesh.alignment = TextAlignment.Left;
            textMesh.characterSize = 0.18f;
            textMesh.fontSize = 42;
            textMesh.color = new Color(0.95f, 0.98f, 1f);
            textObject.transform.position = new Vector3(-4.82f, 1.55f, -1.8f);
            textObject.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        }

        [MenuItem("Tools/Hello World/Apply Quake2 Atmosphere")]
        public static void ApplyQuake2Atmosphere()
        {
            if (GameObject.Find("Q2 Decor Root") != null)
            {
                Debug.LogWarning("[Q2Atmosphere] Already applied; aborting.");
                return;
            }

            EnsureFolders();
            Scene scene = SceneManager.GetActiveScene();

            Debug.Log("[Q2Atmosphere] Generating textures...");
            Texture2D wallTex = WriteAndImportTexture("Tex_Q2_Wall", BuildWallTex());
            Texture2D floorTex = WriteAndImportTexture("Tex_Q2_Floor", BuildFloorTex());
            Texture2D ceilingTex = WriteAndImportTexture("Tex_Q2_Ceiling", BuildCeilingTex());
            Texture2D stairsTex = WriteAndImportTexture("Tex_Q2_Stairs", BuildStairsTex());
            Texture2D hazardTex = WriteAndImportTexture("Tex_Q2_Hazard", BuildHazardTex());
            Texture2D pipeTex = WriteAndImportTexture("Tex_Q2_Pipe", BuildPipeTex());
            Debug.Log("[Q2Atmosphere] Generating textures... done");

            Material q2Wall = CreateQ2Material("Mat_Q2_Wall", wallTex, new Vector2(3f, 1.5f), 0.10f, 0.10f, Color.white);
            Material q2Floor = CreateQ2Material("Mat_Q2_Floor", floorTex, new Vector2(4f, 4f), 0.40f, 0.10f, Color.white);
            Material q2Ceiling = CreateQ2Material("Mat_Q2_Ceiling", ceilingTex, new Vector2(4f, 4f), 0.40f, 0.10f, Color.white);
            Material q2Stairs = CreateQ2Material("Mat_Q2_Stairs", stairsTex, new Vector2(1.5f, 0.5f), 0.40f, 0.25f, Color.white);
            Material q2Hazard = CreateQ2Material("Mat_Q2_Hazard", hazardTex, new Vector2(3f, 1f), 0.10f, 0.10f, new Color(1f, 0.9f, 0.5f, 1f));
            Material q2Pipe = CreateQ2Material("Mat_Q2_Pipe", pipeTex, new Vector2(4f, 1f), 0.40f, 0.05f, Color.white);
            Material q2MezzWood = CreateQ2Material("Mat_Q2_Mezz_Wood", floorTex, new Vector2(3f, 1f), 0.40f, 0.10f, Color.white);
            Material q2LampCage = EmissiveCageMaterial();
            Debug.Log("[Q2Atmosphere] Created 8 materials");

            int reassigned = RetexAllRenderers(q2Wall, q2Floor, q2Ceiling, q2Stairs, q2Hazard, q2Pipe, q2MezzWood, q2LampCage);
            Debug.Log($"[Q2Atmosphere] Reassigned {reassigned} renderers");

            ApplyQ2RenderSettings();
            SetCameraDarkBg();
            ConfigureExistingLights();

            GameObject root = new GameObject("Q2 Decor Root");
            Undo.RegisterCreatedObjectUndo(root, "Create Q2 Decor Root");
            SpawnQ2Decor(root.transform, q2Wall, q2Floor, q2Hazard, q2Pipe, q2LampCage);
            Debug.Log($"[Q2Atmosphere] Spawned {root.transform.childCount} decor objects under 'Q2 Decor Root'");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Q2Atmosphere] Scene saved.");
        }

        private static Texture2D WriteAndImportTexture(string name, Color[] pixels)
        {
            string assetPath = $"{TextureFolder}/{name}.png";
            string absPath = Path.GetFullPath(assetPath);

            if (File.Exists(absPath))
            {
                Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                if (existing != null)
                {
                    return existing;
                }
            }

            Texture2D tex = new Texture2D(256, 256, TextureFormat.RGBA32, mipChain: true);
            tex.SetPixels(pixels);
            tex.Apply(updateMipmaps: true);
            File.WriteAllBytes(absPath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            TextureImporter imp = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            imp.textureType = TextureImporterType.Default;
            imp.sRGBTexture = true;
            imp.alphaSource = TextureImporterAlphaSource.None;
            imp.mipmapEnabled = true;
            imp.wrapMode = TextureWrapMode.Repeat;
            imp.filterMode = FilterMode.Bilinear;
            imp.anisoLevel = 4;
            imp.maxTextureSize = 512;
            imp.textureCompression = TextureImporterCompression.Compressed;
            imp.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        private static Color[] BuildWallTex()
        {
            Random.InitState(2401);
            const int size = 256;
            const int panelW = 128;
            const int seamPx = 2;
            const int rivetRadius = 3;
            Color baseLow = new Color(0.31f, 0.28f, 0.18f, 1f);
            Color baseHigh = new Color(0.46f, 0.42f, 0.27f, 1f);
            Color seam = new Color(0.10f, 0.09f, 0.06f, 1f);
            Color rivet = new Color(0.62f, 0.58f, 0.45f, 1f);
            Color rust = new Color(0.42f, 0.18f, 0.08f, 1f);
            float offsetA = Random.Range(11f, 51f);
            float offsetB = Random.Range(71f, 131f);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int lx = x % panelW;
                    int ly = y % panelW;
                    bool isSeam = lx < seamPx || ly < seamPx;
                    float n = Mathf.PerlinNoise((x + offsetA) / 48f, (y + offsetA) / 48f) * 0.65f
                        + Mathf.PerlinNoise((x + offsetB) / 14f, (y + offsetB) / 14f) * 0.35f;
                    Color c = Color.Lerp(baseLow, baseHigh, n);

                    if (Mathf.PerlinNoise((x + 33.7f) / 30f, (y + 91.3f) / 120f) > 0.78f)
                    {
                        c = Color.Lerp(c, rust, 0.45f);
                    }

                    float edge = Mathf.Min(Mathf.Min(lx, panelW - 1 - lx), Mathf.Min(ly, panelW - 1 - ly));
                    c = Scale(c, Mathf.Lerp(0.68f, 1f, Mathf.SmoothStep(0f, 24f, edge)));

                    if (isSeam)
                    {
                        c = seam;
                    }

                    bool rivetHit = false;
                    for (int py = 0; py < 2 && !rivetHit; py++)
                    {
                        for (int px = 0; px < 2 && !rivetHit; px++)
                        {
                            int cx = (x / panelW) * panelW + (px == 0 ? 32 : 96);
                            int cy = (y / panelW) * panelW + (py == 0 ? 32 : 96);
                            int dx = x - cx;
                            int dy = y - cy;
                            float dist = Mathf.Sqrt(dx * dx + dy * dy);
                            if (dist <= rivetRadius)
                            {
                                float shade = Mathf.Lerp(1.25f, 0.62f, Mathf.Clamp01((dy + rivetRadius) / (float)(rivetRadius * 2)));
                                c = Scale(rivet, shade);
                                rivetHit = true;
                            }
                        }
                    }

                    pixels[y * size + x] = c;
                }
            }

            return pixels;
        }

        private static Color[] BuildFloorTex()
        {
            Random.InitState(2402);
            const int size = 256;
            Color baseColor = new Color(0.18f, 0.18f, 0.19f, 1f);
            float offset = Random.Range(20f, 90f);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float ridge = y % 8 == 0 ? 0.70f : (y % 8 == 1 ? 1.10f : 1f);
                    float streak = Mathf.PerlinNoise((x + offset) / 32f, offset * 0.25f) * 0.22f
                        + Mathf.PerlinNoise((y + offset) / 64f, offset * 0.5f) * 0.12f;
                    pixels[y * size + x] = Scale(baseColor, ridge + streak);
                }
            }

            return pixels;
        }

        private static Color[] BuildCeilingTex()
        {
            Random.InitState(2403);
            const int size = 256;
            Color baseColor = new Color(0.30f, 0.30f, 0.32f, 1f);
            Color boltColor = new Color(0.12f, 0.12f, 0.13f, 1f);
            float offset = Random.Range(10f, 50f);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Color c = Scale(baseColor, 0.86f + Mathf.PerlinNoise((x + offset) / 32f, (y + offset) / 32f) * 0.28f);
                    for (int by = 0; by < 4; by++)
                    {
                        for (int bx = 0; bx < 4; bx++)
                        {
                            int dx = x - (32 + bx * 64);
                            int dy = y - (32 + by * 64);
                            if (dx * dx + dy * dy <= 16)
                            {
                                c = boltColor;
                            }
                        }
                    }

                    pixels[y * size + x] = c;
                }
            }

            return pixels;
        }

        private static Color[] BuildStairsTex()
        {
            Random.InitState(2404);
            const int size = 256;
            Color mustard = new Color(0.50f, 0.42f, 0.18f, 1f);
            Color highlight = new Color(0.78f, 0.68f, 0.31f, 1f);
            Color dirt = new Color(0.12f, 0.10f, 0.07f, 1f);
            float offset = Random.Range(50f, 110f);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Color c = mustard;
                    int lx = x % 16;
                    int ly = y % 16;
                    bool plus = (Mathf.Abs(lx - 8) <= 1 && Mathf.Abs(ly - 8) <= 5)
                        || (Mathf.Abs(ly - 8) <= 1 && Mathf.Abs(lx - 8) <= 5);
                    if (plus)
                    {
                        c = Color.Lerp(c, highlight, 0.65f);
                    }

                    float grime = Mathf.PerlinNoise((x + offset) / 64f, (y + offset) / 64f);
                    c = Color.Lerp(c, dirt, Mathf.Clamp01((grime - 0.55f) * 1.6f));
                    pixels[y * size + x] = c;
                }
            }

            return pixels;
        }

        private static Color[] BuildHazardTex()
        {
            Random.InitState(2405);
            const int size = 256;
            Color yellow = new Color(0.95f, 0.72f, 0.08f, 1f);
            Color black = new Color(0.03f, 0.025f, 0.02f, 1f);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Color c = ((x + y) % 32 < 16) ? yellow : black;
                    float edgeNoise = Mathf.PerlinNoise(x / 16f + 9.1f, y / 16f + 41.7f);
                    pixels[y * size + x] = Scale(c, 0.86f + edgeNoise * 0.20f);
                }
            }

            return pixels;
        }

        private static Color[] BuildPipeTex()
        {
            Random.InitState(2406);
            const int size = 256;
            Color brown = new Color(0.34f, 0.22f, 0.12f, 1f);
            Color rust = new Color(0.48f, 0.16f, 0.06f, 1f);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int band = x % 64;
                    float mid = 1f - Mathf.Abs(band - 32f) / 32f;
                    Color c = Scale(brown, 0.78f + mid * 0.45f);
                    if (band < 3 || band > 61)
                    {
                        c = Scale(c, 0.55f);
                    }

                    float noise = Mathf.PerlinNoise(x / 32f + 17.2f, y / 64f + 8.6f);
                    c = Color.Lerp(c, rust, Mathf.Clamp01((noise - 0.58f) * 0.75f));
                    pixels[y * size + x] = c;
                }
            }

            return pixels;
        }

        private static Material CreateQ2Material(string name, Texture2D texture, Vector2 tiling, float metallic, float gloss, Color tint)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }

            Material material = new Material(Shader.Find("Standard"));
            material.name = name;
            material.SetTexture("_MainTex", texture);
            material.SetColor("_Color", tint);
            material.mainTextureScale = tiling;
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Glossiness", gloss);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Material EmissiveCageMaterial()
        {
            string path = $"{MaterialFolder}/Mat_Q2_Lamp_Cage.mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }

            Material material = new Material(Shader.Find("Standard"));
            material.name = "Mat_Q2_Lamp_Cage";
            material.color = new Color(1f, 0.55f, 0.20f, 1f);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Glossiness", 0f);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(1f, 0.55f, 0.20f) * 1.7f);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static int RetexAllRenderers(
            Material q2Wall,
            Material q2Floor,
            Material q2Ceiling,
            Material q2Stairs,
            Material q2Hazard,
            Material q2Pipe,
            Material q2MezzWood,
            Material q2LampCage)
        {
            int count = 0;
            count += AssignMaterialToObject("Floor", q2Floor);
            count += AssignMaterialToObject("Ceiling", q2Ceiling);

            string[] walls =
            {
                "Back Wall",
                "Left Wall",
                "Right Wall",
                "Front Wall Left",
                "Front Wall Right",
                "Above Door Wall",
                "Door Header",
                "Mezzanine Parapet"
            };
            foreach (string wallName in walls)
            {
                count += AssignMaterialToObject(wallName, q2Wall);
            }

            string[] hazards =
            {
                "Back Accent Panel",
                "Mezzanine Parapet Trim",
                "Accent Cube"
            };
            foreach (string hazardName in hazards)
            {
                count += AssignMaterialToObject(hazardName, q2Hazard);
            }

            count += AssignMaterialToObject("Second Floor", q2MezzWood);
            count += AssignMaterialToChildren("Stairs", q2Stairs);
            count += AssignMaterialToObject("Dark Step", q2Stairs);
            count += AssignMaterialToObject("Mezzanine Glow Lamp", q2LampCage);
            count += AssignMaterialToObject("Glowing Hello Lamp", q2LampCage);

            string[] table =
            {
                "Table Top",
                "Table Leg FL",
                "Table Leg FR",
                "Table Leg BL",
                "Table Leg BR"
            };
            foreach (string tableName in table)
            {
                count += AssignMaterialToObject(tableName, q2Wall);
            }

            return count;
        }

        private static int AssignMaterialToObject(string name, Material material)
        {
            GameObject go = GameObject.Find(name);
            if (go == null)
            {
                Debug.Log($"[Q2Atmosphere] Optional object '{name}' not found; skipping.");
                return 0;
            }

            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer == null)
            {
                Debug.Log($"[Q2Atmosphere] Object '{name}' has no renderer; skipping.");
                return 0;
            }

            Undo.RecordObject(renderer, "Assign Q2 material");
            renderer.sharedMaterial = material;
            EditorUtility.SetDirty(renderer);
            return 1;
        }

        private static int AssignMaterialToChildren(string rootName, Material material)
        {
            GameObject root = GameObject.Find(rootName);
            if (root == null)
            {
                Debug.Log($"[Q2Atmosphere] Optional object '{rootName}' not found; skipping.");
                return 0;
            }

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                Undo.RecordObject(renderer, "Assign Q2 material");
                renderer.sharedMaterial = material;
                EditorUtility.SetDirty(renderer);
            }

            return renderers.Length;
        }

        private static void ApplyQ2RenderSettings()
        {
            UnityEngine.Object renderSettings = Unsupported.GetSerializedAssetInterfaceSingleton("RenderSettings");
            if (renderSettings != null)
            {
                Undo.RecordObject(renderSettings, "Apply Q2 render settings");
            }

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.07f, 0.06f, 0.05f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.18f, 0.13f, 0.08f);
            RenderSettings.fogDensity = 0.055f;
            RenderSettings.skybox = null;
            RenderSettings.subtractiveShadowColor = new Color(0.04f, 0.03f, 0.02f);
            DynamicGI.UpdateEnvironment();
        }

        private static void SetCameraDarkBg()
        {
            GameObject cameraObject = GameObject.Find("First Person Camera");
            if (cameraObject == null)
            {
                Debug.Log("[Q2Atmosphere] Optional camera 'First Person Camera' not found; skipping.");
                return;
            }

            Camera camera = cameraObject.GetComponent<Camera>();
            if (camera == null)
            {
                Debug.Log("[Q2Atmosphere] 'First Person Camera' has no Camera component; skipping.");
                return;
            }

            Undo.RecordObject(camera, "Apply Q2 camera settings");
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.05f, 0.04f, 0.03f);
            EditorUtility.SetDirty(camera);
        }

        private static void ConfigureExistingLights()
        {
            ConfigureLight("Soft Directional Light", new Color(0.55f, 0.45f, 0.30f), 0.15f, null);
            ConfigureLight("Warm Room Light", new Color(1.0f, 0.45f, 0.20f), 3.5f, 8f);
            ConfigureLight("Cool Fill Light", new Color(0.85f, 0.35f, 0.25f), 1.8f, 7f);
            ConfigureLight("Mezzanine Light", new Color(1.0f, 0.55f, 0.25f), 3.0f, 8f);
        }

        private static void ConfigureLight(string name, Color color, float intensity, float? range)
        {
            GameObject go = GameObject.Find(name);
            if (go == null)
            {
                Debug.Log($"[Q2Atmosphere] Optional light '{name}' not found; skipping.");
                return;
            }

            Light light = go.GetComponent<Light>();
            if (light == null)
            {
                Debug.Log($"[Q2Atmosphere] Object '{name}' has no Light component; skipping.");
                return;
            }

            Undo.RecordObject(light, "Apply Q2 light settings");
            light.color = color;
            light.intensity = intensity;
            if (range.HasValue)
            {
                light.range = range.Value;
            }
            EditorUtility.SetDirty(light);
        }

        private static void SpawnQ2Decor(Transform root, Material q2Wall, Material q2Floor, Material q2Hazard, Material q2Pipe, Material q2LampCage)
        {
            CreateDecorCylinder(root, "Q2 Pipe Ceiling A", new Vector3(-2.5f, 5.5f, 0f), new Vector3(0.36f, 2.45f, 0.36f), new Vector3(90f, 0f, 0f), q2Pipe);
            CreateDecorCylinder(root, "Q2 Pipe Ceiling B", new Vector3(2.5f, 5.5f, -2f), new Vector3(0.28f, 0.7f, 0.28f), new Vector3(90f, 0f, 0f), q2Pipe);
            CreateDecorCylinder(root, "Q2 Pipe Wall Vertical", new Vector3(-4.85f, 3f, -2f), new Vector3(0.24f, 1.5f, 0.24f), Vector3.zero, q2Pipe);

            CreateDecorCube(root, "Q2 Vent Left", new Vector3(-4.86f, 2.0f, -1.0f), new Vector3(0.05f, 0.6f, 0.9f), Vector3.zero, q2Hazard);
            CreateDecorCube(root, "Q2 Vent Right", new Vector3(4.86f, 2.0f, -2.5f), new Vector3(0.05f, 0.6f, 0.9f), Vector3.zero, q2Hazard);
            CreateDecorCube(root, "Q2 Vent Back", new Vector3(2.5f, 4.5f, 4.86f), new Vector3(1.2f, 0.6f, 0.05f), Vector3.zero, q2Hazard);

            CreateDecorCube(root, "Q2 Hazard Door Frame Left", new Vector3(-1.55f, 0.3f, -4.85f), new Vector3(0.2f, 0.6f, 0.05f), Vector3.zero, q2Hazard);
            CreateDecorCube(root, "Q2 Hazard Door Frame Right", new Vector3(1.55f, 0.3f, -4.85f), new Vector3(0.2f, 0.6f, 0.05f), Vector3.zero, q2Hazard);
            CreateDecorCube(root, "Q2 Hazard Stair Base", new Vector3(4.125f, 0.05f, -3.2f), new Vector3(1.6f, 0.1f, 0.4f), Vector3.zero, q2Hazard);
            CreateDecorCube(root, "Q2 Floor Grate", new Vector3(2.0f, 0.01f, 2.5f), new Vector3(1.5f, 0.02f, 1.5f), Vector3.zero, q2Floor);

            CreateCageLamp(root, "Q2 Cage Lamp 1", new Vector3(-2.0f, 4.6f, -2.5f), new Vector3(0.25f, 0.25f, 0.25f), 6f, 2.5f, new Color(1f, 0.55f, 0.20f), true, q2LampCage, q2Wall);
            CreateCageLamp(root, "Q2 Cage Lamp 2", new Vector3(2.0f, 4.6f, -1.0f), new Vector3(0.25f, 0.25f, 0.25f), 6f, 2.5f, new Color(1f, 0.45f, 0.15f), false, q2LampCage, q2Wall);
            CreateCageLamp(root, "Q2 Cage Lamp 3", new Vector3(0f, 5.6f, -4.0f), new Vector3(0.30f, 0.30f, 0.30f), 8f, 3.5f, new Color(1f, 0.50f, 0.25f), true, q2LampCage, q2Wall);
        }

        private static GameObject CreateDecorCube(Transform root, string name, Vector3 position, Vector3 scale, Vector3 rotation, Material material)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(root, true);
            cube.transform.position = position;
            cube.transform.localScale = scale;
            cube.transform.rotation = Quaternion.Euler(rotation);
            cube.GetComponent<Renderer>().sharedMaterial = material;
            Undo.RegisterCreatedObjectUndo(cube, $"Create {name}");
            return cube;
        }

        private static GameObject CreateDecorCylinder(Transform root, string name, Vector3 position, Vector3 scale, Vector3 rotation, Material material)
        {
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = name;
            cylinder.transform.SetParent(root, true);
            cylinder.transform.position = position;
            cylinder.transform.localScale = scale;
            cylinder.transform.rotation = Quaternion.Euler(rotation);
            cylinder.GetComponent<Renderer>().sharedMaterial = material;
            Undo.RegisterCreatedObjectUndo(cylinder, $"Create {name}");
            return cylinder;
        }

        private static void CreateCageLamp(
            Transform root,
            string name,
            Vector3 position,
            Vector3 scale,
            float range,
            float intensity,
            Color color,
            bool flicker,
            Material cageMaterial,
            Material rodMaterial)
        {
            GameObject lampObject = CreateDecorCube(root, name, position, scale, Vector3.zero, cageMaterial);
            Light light = Undo.AddComponent<Light>(lampObject);
            light.type = LightType.Point;
            light.range = range;
            light.intensity = intensity;
            light.color = color;

            if (flicker)
            {
                Undo.AddComponent<FlickerLight>(lampObject);
            }

            CreateDecorCube(
                root,
                $"{name} Rod",
                new Vector3(position.x, position.y + 0.7f, position.z),
                new Vector3(0.04f, 0.6f, 0.04f),
                Vector3.zero,
                rodMaterial);
        }

        private static Color Scale(Color color, float value)
        {
            return new Color(
                Mathf.Clamp01(color.r * value),
                Mathf.Clamp01(color.g * value),
                Mathf.Clamp01(color.b * value),
                color.a);
        }

        private static GameObject CreateCube(string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.position = position;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            return cube;
        }
    }
}
