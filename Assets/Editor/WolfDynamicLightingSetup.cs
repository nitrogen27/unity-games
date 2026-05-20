#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using HelloWorldRoom;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class WolfDynamicLightingSetup
{
    private const string ScenePath = "Assets/Scenes/WolfRepoLevel1.unity";
    private const string RootName = "Wolf Dynamic Lighting";
    private const string MaterialRoot = "Assets/Materials/WolfTargetLook";
    private const string LampGlowMaterialPath = MaterialRoot + "/DynamicLampGlow.mat";

    private static readonly Color WarmLamp = new Color(1.0f, 0.72f, 0.36f);
    private static readonly Color CoolLamp = new Color(0.32f, 0.88f, 1.0f);

    [MenuItem("Tools/Wolf Target Look/Apply Dynamic Lighting")]
    public static void ApplyDynamicLighting()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[WolfDynamicLightingSetup] Exit Play Mode before applying scene lighting.");
            return;
        }

        AssetDatabase.Refresh();
        Directory.CreateDirectory(MaterialRoot);

        Material glowMaterial = CreateGlowMaterial();

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        GameObject existingRoot = GameObject.Find(RootName);
        if (existingRoot != null)
        {
            Object.DestroyImmediate(existingRoot);
        }

        GameObject root = new GameObject(RootName);
        GameObject lampGroup = CreateChild(root.transform, "Lamp Sources");
        GameObject probeGroup = CreateChild(root.transform, "Reflection Probes");

        ApplySceneLightingSettings();
        EnhanceReflectiveMaterials();
        ApplyRendererLightingFlags(root.transform);

        List<LampAnchor> anchors = FindLampAnchors();
        for (int i = 0; i < anchors.Count; i++)
        {
            CreateLampRig(anchors[i], i, lampGroup.transform, glowMaterial);
        }

        Bounds sceneBounds = CalculateSceneBounds(root.transform);
        CreateDiffuseRoomFill(root.transform, sceneBounds);
        CreateReflectionProbes(probeGroup.transform, sceneBounds);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[WolfDynamicLightingSetup] Added {anchors.Count} dynamic lamp rigs and reflection probes to {ScenePath}.");
    }

    private static void ApplySceneLightingSettings()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.145f, 0.15f, 0.16f);
        RenderSettings.reflectionIntensity = 1.18f;
        RenderSettings.reflectionBounces = 3;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.0032f;
        RenderSettings.fogColor = new Color(0.075f, 0.082f, 0.095f);

        foreach (Camera camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude))
        {
            camera.allowHDR = true;
            camera.renderingPath = RenderingPath.Forward;
            EditorUtility.SetDirty(camera);
        }
    }

    private static void EnhanceReflectiveMaterials()
    {
        SetMaterialSurface("BlueWall_Target", 0.0f, 0.58f);
        SetMaterialSurface("DoorTeal_Target", 0.70f, 0.72f);
        SetMaterialSurface("FloorTile_Target", 0.04f, 0.62f);
        SetMaterialSurface("CeilingPanel_Target", 0.20f, 0.38f);
        SetMaterialSurface("DarkMetalTrim_Target", 0.85f, 0.68f);
        SetMaterialSurface("PrisonCellDoor_Target", 0.60f, 0.58f);

        SetEmission("Assets/Materials/Mat_Lamp_Glow.mat", WarmLamp * 3.2f);
        SetEmission("Assets/Materials/WolfRepo/Mat_WolfRepo_LampWarmBulb.mat", WarmLamp * 3.4f);
        SetEmission("Assets/Materials/WolfRepo/Mat_WolfRepo_LampGreenBulb.mat", CoolLamp * 2.55f);
        SetEmission("Assets/Materials/WolfRepo/Mat_WolfRepo_StairLampBulb.mat", WarmLamp * 2.85f);
    }

    private static void ApplyRendererLightingFlags(Transform generatedRoot)
    {
        foreach (Renderer renderer in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
        {
            if (renderer.transform.IsChildOf(generatedRoot))
            {
                continue;
            }

            renderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;
            renderer.receiveShadows = true;

            if (renderer is MeshRenderer)
            {
                renderer.shadowCastingMode = ShadowCastingMode.On;
            }

            EditorUtility.SetDirty(renderer);
        }
    }

    private static List<LampAnchor> FindLampAnchors()
    {
        List<LampAnchor> anchors = new List<LampAnchor>();
        foreach (Transform transform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
        {
            string objectName = transform.gameObject.name;
            if (objectName.StartsWith("chandelier bulb ", System.StringComparison.Ordinal))
            {
                anchors.Add(new LampAnchor(objectName, transform.position, WarmLamp, 1.25f, 15.5f, true));
            }
            else if (objectName.StartsWith("ceilLight cap ", System.StringComparison.Ordinal))
            {
                anchors.Add(new LampAnchor(objectName, transform.position + Vector3.down * 0.05f, CoolLamp, 1.08f, 14.5f, false));
            }
        }

        anchors.Sort((left, right) => string.Compare(left.Name, right.Name, System.StringComparison.Ordinal));
        return anchors;
    }

    private static void CreateLampRig(
        LampAnchor anchor,
        int index,
        Transform parent,
        Material glowMaterial)
    {
        GameObject rig = CreateChild(parent, $"Dynamic Lamp {index:00} - {anchor.Name}");
        rig.transform.position = anchor.Position;

        GameObject glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        glow.name = "emissive bulb";
        glow.transform.SetParent(rig.transform, false);
        glow.transform.localPosition = Vector3.zero;
        glow.transform.localScale = Vector3.one * (anchor.IsChandelier ? 0.18f : 0.13f);
        glow.GetComponent<Renderer>().sharedMaterial = glowMaterial;
        Collider collider = glow.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        GameObject pointObject = CreateChild(rig.transform, "point bounce");
        pointObject.transform.localPosition = Vector3.down * 0.08f;
        Light point = pointObject.AddComponent<Light>();
        point.type = LightType.Point;
        point.color = Color.Lerp(anchor.Color, Color.white, 0.44f);
        point.intensity = anchor.BaseIntensity * 0.32f;
        point.range = anchor.Range * 1.65f;
        point.bounceIntensity = 0.95f;
        point.shadows = LightShadows.None;
        point.renderMode = LightRenderMode.Auto;
        point.lightmapBakeType = LightmapBakeType.Realtime;
        ConfigureFlicker(pointObject.AddComponent<FlickerLight>(), point.intensity * 0.94f, point.intensity * 1.04f, 0.25f, 0.65f, 0.98f);

        GameObject fillObject = CreateChild(rig.transform, "soft room fill");
        fillObject.transform.localPosition = Vector3.down * 1.05f;
        Light fill = fillObject.AddComponent<Light>();
        fill.type = LightType.Point;
        fill.color = Color.Lerp(anchor.Color, Color.white, 0.62f);
        fill.intensity = anchor.BaseIntensity * 0.24f;
        fill.range = anchor.Range * 2.35f;
        fill.bounceIntensity = 1.15f;
        fill.shadows = LightShadows.None;
        fill.renderMode = LightRenderMode.Auto;
        fill.lightmapBakeType = LightmapBakeType.Realtime;
        ConfigureFlicker(fillObject.AddComponent<FlickerLight>(), fill.intensity * 0.96f, fill.intensity * 1.03f, 0.35f, 0.85f, 0.99f);
    }

    private static void CreateDiffuseRoomFill(Transform parent, Bounds sceneBounds)
    {
        GameObject fillGroup = CreateChild(parent, "Diffuse Room Fill");
        Vector3 center = sceneBounds.center;
        float roomRange = Mathf.Max(sceneBounds.size.x, sceneBounds.size.z) * 0.68f;
        float y = Mathf.Clamp(center.y + 0.85f, 1.05f, 1.65f);

        CreateDirectionalFill(
            fillGroup.transform,
            "cool ambient key",
            new Vector3(62f, -28f, 0f),
            new Color(0.72f, 0.83f, 1f),
            0.58f);

        CreateDirectionalFill(
            fillGroup.transform,
            "warm ceiling bounce",
            new Vector3(78f, 128f, 0f),
            new Color(1f, 0.82f, 0.58f),
            0.24f);

        Vector3[] fillPositions =
        {
            new Vector3(center.x, y, center.z),
            new Vector3(Mathf.Lerp(sceneBounds.min.x, sceneBounds.max.x, 0.25f), y, Mathf.Lerp(sceneBounds.min.z, sceneBounds.max.z, 0.28f)),
            new Vector3(Mathf.Lerp(sceneBounds.min.x, sceneBounds.max.x, 0.75f), y, Mathf.Lerp(sceneBounds.min.z, sceneBounds.max.z, 0.28f)),
            new Vector3(Mathf.Lerp(sceneBounds.min.x, sceneBounds.max.x, 0.25f), y, Mathf.Lerp(sceneBounds.min.z, sceneBounds.max.z, 0.72f)),
            new Vector3(Mathf.Lerp(sceneBounds.min.x, sceneBounds.max.x, 0.75f), y, Mathf.Lerp(sceneBounds.min.z, sceneBounds.max.z, 0.72f))
        };

        for (int i = 0; i < fillPositions.Length; i++)
        {
            GameObject fillObject = CreateChild(fillGroup.transform, $"broad room fill {i + 1}");
            fillObject.transform.position = fillPositions[i];
            Light fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Point;
            fill.color = new Color(0.68f, 0.78f, 0.92f);
            fill.intensity = 0.34f;
            fill.range = roomRange;
            fill.bounceIntensity = 1.0f;
            fill.shadows = LightShadows.None;
            fill.renderMode = LightRenderMode.Auto;
            fill.lightmapBakeType = LightmapBakeType.Realtime;
        }
    }

    private static void CreateDirectionalFill(Transform parent, string name, Vector3 eulerAngles, Color color, float intensity)
    {
        GameObject lightObject = CreateChild(parent, name);
        lightObject.transform.rotation = Quaternion.Euler(eulerAngles);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = color;
        light.intensity = intensity;
        light.bounceIntensity = 0.8f;
        light.shadows = LightShadows.None;
        light.renderMode = LightRenderMode.Auto;
        light.lightmapBakeType = LightmapBakeType.Realtime;
    }

    private static void CreateReflectionProbes(Transform parent, Bounds sceneBounds)
    {
        Vector3 size = sceneBounds.size;
        Vector3 center = sceneBounds.center;
        float y = Mathf.Clamp(center.y + 0.6f, 0.8f, 1.4f);
        Vector3 probeSize = new Vector3(Mathf.Max(size.x * 0.46f, 18f), 4.2f, Mathf.Max(size.z * 0.46f, 18f));

        Vector3[] probePositions =
        {
            new Vector3(center.x, y, center.z),
            new Vector3(Mathf.Lerp(sceneBounds.min.x, sceneBounds.max.x, 0.28f), y, Mathf.Lerp(sceneBounds.min.z, sceneBounds.max.z, 0.32f)),
            new Vector3(Mathf.Lerp(sceneBounds.min.x, sceneBounds.max.x, 0.72f), y, Mathf.Lerp(sceneBounds.min.z, sceneBounds.max.z, 0.68f))
        };

        for (int i = 0; i < probePositions.Length; i++)
        {
            GameObject probeObject = CreateChild(parent, $"Realtime Reflection Probe {i + 1}");
            probeObject.transform.position = probePositions[i];
            ReflectionProbe probe = probeObject.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.EveryFrame;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
            probe.resolution = 64;
            probe.intensity = 1.1f;
            probe.boxProjection = true;
            probe.size = probeSize;
            probe.center = Vector3.zero;
        }
    }

    private static Bounds CalculateSceneBounds(Transform generatedRoot)
    {
        bool hasBounds = false;
        Bounds bounds = new Bounds(Vector3.zero, Vector3.one);

        foreach (Renderer renderer in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
        {
            if (renderer.transform.IsChildOf(generatedRoot))
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return bounds;
    }

    private static Material CreateGlowMaterial()
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Lit");
        }
        Material material = LoadOrCreateMaterial(LampGlowMaterialPath, shader);
        SetColor(material, "_Color", new Color(1.0f, 0.78f, 0.38f, 1f));
        SetEmission(material, WarmLamp * 3.8f);
        SetFloat(material, "_Glossiness", 0.48f);
        SetFloat(material, "_Metallic", 0.0f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material LoadOrCreateMaterial(string path, Shader shader)
    {
        if (shader == null)
        {
            throw new MissingReferenceException($"No shader available for generated material at {path}.");
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        else if (shader != null)
        {
            material.shader = shader;
        }

        return material;
    }

    private static void SetMaterialSurface(string materialName, float metallic, float smoothness)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialRoot}/{materialName}.mat");
        if (material == null)
        {
            return;
        }

        SetFloat(material, "_Metallic", metallic);
        SetFloat(material, "_Smoothness", smoothness);
        SetFloat(material, "_Glossiness", smoothness);
        SetFloat(material, "_GlossyReflections", 1f);
        SetFloat(material, "_SpecularHighlights", 1f);
        EditorUtility.SetDirty(material);
    }

    private static void SetEmission(string materialPath, Color emission)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            return;
        }

        SetEmission(material, emission);
        EditorUtility.SetDirty(material);
    }

    private static void SetEmission(Material material, Color emission)
    {
        material.EnableKeyword("_EMISSION");
        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        SetColor(material, "_EmissionColor", emission);
    }

    private static void SetFloat(Material material, string propertyName, float value)
    {
        if (material.HasProperty(propertyName))
        {
            material.SetFloat(propertyName, value);
        }
    }

    private static void SetColor(Material material, string propertyName, Color value)
    {
        if (material.HasProperty(propertyName))
        {
            material.SetColor(propertyName, value);
        }
    }

    private static GameObject CreateChild(Transform parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        return child;
    }

    private static GameObject CreateChild(GameObject parent, string name)
    {
        return CreateChild(parent.transform, name);
    }

    private static void ConfigureFlicker(
        FlickerLight flicker,
        float minIntensity,
        float maxIntensity,
        float minInterval,
        float maxInterval,
        float smoothing)
    {
        SerializedObject serialized = new SerializedObject(flicker);
        serialized.FindProperty("minIntensity").floatValue = minIntensity;
        serialized.FindProperty("maxIntensity").floatValue = maxIntensity;
        serialized.FindProperty("minInterval").floatValue = minInterval;
        serialized.FindProperty("maxInterval").floatValue = maxInterval;
        serialized.FindProperty("smoothing").floatValue = smoothing;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private readonly struct LampAnchor
    {
        public LampAnchor(string name, Vector3 position, Color color, float baseIntensity, float range, bool isChandelier)
        {
            Name = name;
            Position = position;
            Color = color;
            BaseIntensity = baseIntensity;
            Range = range;
            IsChandelier = isChandelier;
        }

        public string Name { get; }
        public Vector3 Position { get; }
        public Color Color { get; }
        public float BaseIntensity { get; }
        public float Range { get; }
        public bool IsChandelier { get; }
    }
}
#endif
