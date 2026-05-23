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
    private const string PerformanceRootName = "Wolf Performance Settings";
    private const string MaterialRoot = "Assets/Materials/WolfTargetLook";
    private const string LampGlowMaterialPath = MaterialRoot + "/DynamicLampGlow.mat";
    private const string CeilingSpillTexturePath = MaterialRoot + "/CeilingLampSpillFalloff.png";
    private const string FloorReflectionTexturePath = MaterialRoot + "/LampFloorReflectionFalloff.png";
    private const string WallReflectionTexturePath = MaterialRoot + "/LampWallReflectionFalloff.png";
    private const string WarmCeilingSpillMaterialPath = MaterialRoot + "/CeilingLampSpillWarm.mat";
    private const string CoolCeilingSpillMaterialPath = MaterialRoot + "/CeilingLampSpillCool.mat";
    private const string WarmFloorReflectionMaterialPath = MaterialRoot + "/LampFloorReflectionWarm.mat";
    private const string CoolFloorReflectionMaterialPath = MaterialRoot + "/LampFloorReflectionCool.mat";
    private const string WarmWallReflectionMaterialPath = MaterialRoot + "/LampWallReflectionWarm.mat";
    private const string CoolWallReflectionMaterialPath = MaterialRoot + "/LampWallReflectionCool.mat";
    private const int MaxRealtimeLampLights = 34;
    private const int MaxFlickeringLampLights = 4;
    private const int PerformanceAntiAliasingSamples = 2;
    private const int QualityAntiAliasingSamples = 4;
    private const int QualityReflectionProbeResolution = 128;

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
        Material warmCeilingSpillMaterial = CreateCeilingSpillMaterial(WarmCeilingSpillMaterialPath, WarmLamp, 1.45f, 0.48f);
        Material coolCeilingSpillMaterial = CreateCeilingSpillMaterial(CoolCeilingSpillMaterialPath, CoolLamp, 1.25f, 0.42f);
        Material warmFloorReflectionMaterial = CreateSurfaceReflectionMaterial(WarmFloorReflectionMaterialPath, FloorReflectionTexturePath, WarmLamp, 2.0f, 0.54f, true);
        Material coolFloorReflectionMaterial = CreateSurfaceReflectionMaterial(CoolFloorReflectionMaterialPath, FloorReflectionTexturePath, CoolLamp, 1.8f, 0.48f, true);
        Material warmWallReflectionMaterial = CreateSurfaceReflectionMaterial(WarmWallReflectionMaterialPath, WallReflectionTexturePath, WarmLamp, 1.65f, 0.42f, false);
        Material coolWallReflectionMaterial = CreateSurfaceReflectionMaterial(CoolWallReflectionMaterialPath, WallReflectionTexturePath, CoolLamp, 1.45f, 0.38f, false);

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
        int disabledAuthoredFixtureLights = DisableAuthoredFixtureLights();

        List<LampAnchor> anchors = FindLampAnchors();
        HashSet<int> realtimeLampIndices = SelectRealtimeLampIndices(anchors);
        Physics.SyncTransforms();
        int realtimeLampCount = 0;
        for (int i = 0; i < anchors.Count; i++)
        {
            bool hasRealtimeLight = realtimeLampIndices.Contains(i);
            bool hasFlicker = hasRealtimeLight && realtimeLampCount < MaxFlickeringLampLights;
            if (CreateLampRig(
                anchors[i],
                i,
                lampGroup.transform,
                glowMaterial,
                warmCeilingSpillMaterial,
                coolCeilingSpillMaterial,
                warmFloorReflectionMaterial,
                coolFloorReflectionMaterial,
                warmWallReflectionMaterial,
                coolWallReflectionMaterial,
                hasRealtimeLight,
                hasFlicker))
            {
                realtimeLampCount++;
            }
        }

        Bounds sceneBounds = CalculateSceneBounds(root.transform);
        CreateDiffuseRoomFill(root.transform, sceneBounds);
        CreateReflectionProbes(probeGroup.transform, sceneBounds);
        ConfigureReflectionProbesForQuality();
        ApplyQualityLightingProfileToOpenScene();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[WolfDynamicLightingSetup] Added {anchors.Count} lamp glows, {realtimeLampCount} realtime point lights, disabled {disabledAuthoredFixtureLights} duplicate authored fixture lights, and added performance reflection probes to {ScenePath}.");
    }

    [MenuItem("Tools/Wolf Target Look/Apply Quality Lighting Settings")]
    public static void ApplyQualityLightingSettings()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[WolfDynamicLightingSetup] Exit Play Mode before applying quality lighting settings.");
            return;
        }

        AssetDatabase.Refresh();
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ApplySceneLightingSettings();
        ApplyRendererLightingFlags(null);
        ConfigureReflectionProbesForQuality();
        ApplyQualityLightingProfileToOpenScene();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[WolfDynamicLightingSetup] Applied quality lighting profile with realtime shadows and reflections to {ScenePath}.");
    }

    [MenuItem("Tools/Wolf Target Look/Apply Performance Settings")]
    public static void ApplyPerformanceSettings()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[WolfDynamicLightingSetup] Exit Play Mode before applying performance settings.");
            return;
        }

        AssetDatabase.Refresh();
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ApplySceneLightingSettings();
        ApplyRendererLightingFlags(null);
        int disabledAuthoredFixtureLights = DisableAuthoredFixtureLights();
        ConfigureReflectionProbesForPerformance();
        ApplyPerformanceProfileToOpenScene();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[WolfDynamicLightingSetup] Applied 60 FPS performance profile to {ScenePath}; disabled {disabledAuthoredFixtureLights} duplicate authored fixture lights.");
    }

    private static void ApplySceneLightingSettings()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.22f, 0.225f, 0.24f);
        RenderSettings.reflectionIntensity = 1.25f;
        RenderSettings.reflectionBounces = 2;
        RenderSettings.defaultReflectionResolution = QualityReflectionProbeResolution;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.0022f;
        RenderSettings.fogColor = new Color(0.105f, 0.115f, 0.13f);

        foreach (Camera camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude))
        {
            camera.allowHDR = true;
            camera.allowMSAA = true;
            camera.useOcclusionCulling = true;
            camera.renderingPath = RenderingPath.Forward;
            EditorUtility.SetDirty(camera);
        }
    }

    private static void EnhanceReflectiveMaterials()
    {
        SetMaterialSurface("BlueWall_Target", 0.0f, 0.74f, 0.92f);
        SetMaterialSurface("WhiteStoneWall_Target", 0.0f, 0.56f, 0.72f);
        SetMaterialSurface("WhiteStoneWall_Dark_Target", 0.0f, 0.52f, 0.68f);
        SetMaterialSurface("DoorTeal_Target", 0.82f, 0.84f, 0.95f);
        SetMaterialSurface("FloorTile_Target", 0.06f, 0.78f, 0.92f);
        SetMaterialSurface("CeilingPanel_Target", 0.0f, 0.46f, 0.62f);
        SetMaterialSurface("DarkMetalTrim_Target", 0.88f, 0.82f, 0.94f);
        SetMaterialSurface("PrisonCellDoor_Target", 0.72f, 0.74f, 0.86f);
        WolfTargetMaterialSetup.ConfigureDoorTargetMaterial();
        WolfTargetMaterialSetup.ConfigureFloorTileMaterial();
        WolfTargetMaterialSetup.ConfigureCeilingPanelVisibility();

        SetEmission("Assets/Materials/Mat_Lamp_Glow.mat", WarmLamp * 3.2f);
        SetEmission("Assets/Materials/WolfRepo/Mat_WolfRepo_LampWarmBulb.mat", WarmLamp * 3.4f);
        SetEmission("Assets/Materials/WolfRepo/Mat_WolfRepo_LampGreenBulb.mat", CoolLamp * 2.55f);
        SetEmission("Assets/Materials/WolfRepo/Mat_WolfRepo_StairLampBulb.mat", WarmLamp * 2.85f);
    }

    private static void ApplyRendererLightingFlags(Transform generatedRoot)
    {
        foreach (Renderer renderer in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
        {
            if (generatedRoot != null && renderer.transform.IsChildOf(generatedRoot))
            {
                continue;
            }

            renderer.reflectionProbeUsage = ReflectionProbeUsage.Simple;
            renderer.receiveShadows = true;

            if (renderer is MeshRenderer)
            {
                renderer.shadowCastingMode = ShadowCastingMode.On;
            }

            EditorUtility.SetDirty(renderer);
        }
    }

    private static int DisableAuthoredFixtureLights()
    {
        int disabledCount = 0;
        foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude))
        {
            string objectName = light.gameObject.name;
            bool duplicateFixtureLight =
                objectName.StartsWith("ceilLight light ", System.StringComparison.Ordinal) ||
                objectName.StartsWith("chandelier light ", System.StringComparison.Ordinal) ||
                objectName.StartsWith("Chandelier Light", System.StringComparison.Ordinal);

            if (!duplicateFixtureLight || !light.enabled)
            {
                continue;
            }

            light.enabled = false;
            disabledCount++;
            EditorUtility.SetDirty(light);
        }

        return disabledCount;
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

    private static bool CreateLampRig(
        LampAnchor anchor,
        int index,
        Transform parent,
        Material glowMaterial,
        Material warmCeilingSpillMaterial,
        Material coolCeilingSpillMaterial,
        Material warmFloorReflectionMaterial,
        Material coolFloorReflectionMaterial,
        Material warmWallReflectionMaterial,
        Material coolWallReflectionMaterial,
        bool createRealtimeLight,
        bool createFlicker)
    {
        GameObject rig = CreateChild(parent, $"Dynamic Lamp {index:00} - {anchor.Name}");
        rig.transform.position = anchor.Position;

        GameObject glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        glow.name = "emissive bulb";
        glow.transform.SetParent(rig.transform, false);
        glow.transform.localPosition = Vector3.zero;
        glow.transform.localScale = Vector3.one * (anchor.IsChandelier ? 0.18f : 0.13f);
        Renderer glowRenderer = glow.GetComponent<Renderer>();
        glowRenderer.sharedMaterial = glowMaterial;
        glowRenderer.receiveShadows = false;
        glowRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        glowRenderer.shadowCastingMode = ShadowCastingMode.Off;
        Collider collider = glow.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        CreateCeilingLightSpill(rig.transform, anchor, warmCeilingSpillMaterial, coolCeilingSpillMaterial);
        CreateFloorLightReflection(rig.transform, anchor, warmFloorReflectionMaterial, coolFloorReflectionMaterial);
        CreateWallLightReflections(rig.transform, anchor, warmWallReflectionMaterial, coolWallReflectionMaterial);

        if (!createRealtimeLight)
        {
            return false;
        }

        GameObject pointObject = CreateChild(rig.transform, "point bounce");
        pointObject.transform.localPosition = Vector3.down * 0.08f;
        Light point = pointObject.AddComponent<Light>();
        point.type = LightType.Point;
        point.color = Color.Lerp(anchor.Color, Color.white, 0.38f);
        point.intensity = anchor.BaseIntensity * 0.82f;
        point.range = anchor.Range * 1.08f;
        point.bounceIntensity = 1.2f;
        point.shadows = LightShadows.Soft;
        point.shadowStrength = anchor.IsChandelier ? 0.58f : 0.44f;
        point.shadowBias = 0.035f;
        point.shadowNormalBias = 0.24f;
        point.shadowNearPlane = 0.12f;
        point.shadowResolution = LightShadowResolution.High;
        point.renderMode = LightRenderMode.ForcePixel;
        point.lightmapBakeType = LightmapBakeType.Realtime;

        GameObject ceilingScatterObject = CreateChild(rig.transform, "ceiling scatter");
        ceilingScatterObject.transform.localPosition = Vector3.up * (anchor.IsChandelier ? 0.34f : 0.07f);
        Light ceilingScatter = ceilingScatterObject.AddComponent<Light>();
        ceilingScatter.type = LightType.Point;
        ceilingScatter.color = Color.Lerp(anchor.Color, Color.white, anchor.IsChandelier ? 0.36f : 0.28f);
        ceilingScatter.intensity = anchor.BaseIntensity * (anchor.IsChandelier ? 0.30f : 0.22f);
        ceilingScatter.range = anchor.IsChandelier ? 5.4f : 3.8f;
        ceilingScatter.bounceIntensity = 0.9f;
        ceilingScatter.shadows = LightShadows.None;
        ceilingScatter.renderMode = LightRenderMode.ForcePixel;
        ceilingScatter.lightmapBakeType = LightmapBakeType.Realtime;

        if (createFlicker)
        {
            ConfigureFlicker(pointObject.AddComponent<FlickerLight>(), point.intensity * 0.96f, point.intensity * 1.03f, 0.45f, 0.95f, 0.92f);
        }

        return true;
    }

    private static void CreateCeilingLightSpill(
        Transform parent,
        LampAnchor anchor,
        Material warmCeilingSpillMaterial,
        Material coolCeilingSpillMaterial)
    {
        GameObject spill = GameObject.CreatePrimitive(PrimitiveType.Quad);
        spill.name = "ceiling light spill";
        spill.transform.SetParent(parent, false);
        spill.transform.localPosition = Vector3.up * (anchor.IsChandelier ? 0.36f : 0.075f);
        spill.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        float diameter = anchor.IsChandelier ? 5.8f : 3.6f;
        spill.transform.localScale = new Vector3(diameter, diameter, 1f);

        Renderer renderer = spill.GetComponent<Renderer>();
        renderer.sharedMaterial = anchor.IsChandelier ? warmCeilingSpillMaterial : coolCeilingSpillMaterial;
        renderer.receiveShadows = false;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        renderer.shadowCastingMode = ShadowCastingMode.Off;

        Collider collider = spill.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }
    }

    private static void CreateFloorLightReflection(
        Transform parent,
        LampAnchor anchor,
        Material warmFloorReflectionMaterial,
        Material coolFloorReflectionMaterial)
    {
        Vector3 position;
        Vector3 normal;
        if (TryFindSceneSurface(anchor.Position + Vector3.up * 0.08f, Vector3.down, anchor.IsChandelier ? 8.5f : 6.5f, SurfaceTarget.Floor, out RaycastHit hit))
        {
            position = hit.point + hit.normal * 0.018f;
            normal = hit.normal;
        }
        else
        {
            float floorY = anchor.Position.y < -1f ? -3f : 0f;
            position = new Vector3(anchor.Position.x, floorY + 0.018f, anchor.Position.z);
            normal = Vector3.up;
        }

        float wide = anchor.IsChandelier ? 7.2f : 5.2f;
        float longAxis = anchor.IsChandelier ? 8.2f : 6.0f;
        Material material = anchor.IsChandelier ? warmFloorReflectionMaterial : coolFloorReflectionMaterial;
        CreateSurfaceReflectionQuad(parent, "floor lamp reflection", position, normal, material, wide, longAxis);
    }

    private static void CreateWallLightReflections(
        Transform parent,
        LampAnchor anchor,
        Material warmWallReflectionMaterial,
        Material coolWallReflectionMaterial)
    {
        Vector3 origin = anchor.Position + Vector3.down * (anchor.IsChandelier ? 0.42f : 0.32f);
        Vector3[] directions =
        {
            Vector3.forward,
            Vector3.back,
            Vector3.left,
            Vector3.right
        };

        float maxDistance = anchor.IsChandelier ? 7.5f : 5.8f;
        Material material = anchor.IsChandelier ? warmWallReflectionMaterial : coolWallReflectionMaterial;

        for (int i = 0; i < directions.Length; i++)
        {
            Vector3 direction = directions[i];
            if (TryFindSceneSurface(origin, direction, maxDistance, SurfaceTarget.Wall, out RaycastHit hit))
            {
                Vector3 position = hit.point + hit.normal * 0.024f;
                float width = anchor.IsChandelier ? 4.4f : 3.35f;
                float height = anchor.IsChandelier ? 2.25f : 1.75f;
                CreateSurfaceReflectionQuad(parent, "wall lamp reflection", position, hit.normal, material, width, height);
            }
        }
    }

    private static bool TryFindSceneSurface(
        Vector3 origin,
        Vector3 direction,
        float maxDistance,
        SurfaceTarget target,
        out RaycastHit bestHit)
    {
        RaycastHit[] hits = Physics.RaycastAll(origin, direction, maxDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        bestHit = default;
        float bestDistance = float.PositiveInfinity;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider == null || hit.distance >= bestDistance || ShouldSkipReflectionSurface(hit.transform))
            {
                continue;
            }

            float upDot = Vector3.Dot(hit.normal.normalized, Vector3.up);
            if (target == SurfaceTarget.Floor && upDot < 0.55f)
            {
                continue;
            }

            if (target == SurfaceTarget.Wall && Mathf.Abs(upDot) > 0.35f)
            {
                continue;
            }

            bestHit = hit;
            bestDistance = hit.distance;
        }

        return bestDistance < float.PositiveInfinity;
    }

    private static bool ShouldSkipReflectionSurface(Transform transform)
    {
        if (transform == null)
        {
            return true;
        }

        string objectName = transform.gameObject.name;
        return transform.GetComponentInParent<WolfDoor>() != null ||
            objectName.StartsWith("Door ", System.StringComparison.Ordinal) ||
            objectName.Contains(" Door ", System.StringComparison.Ordinal) ||
            objectName.StartsWith("ceilLight", System.StringComparison.Ordinal) ||
            objectName.StartsWith("chandelier", System.StringComparison.Ordinal) ||
            objectName.StartsWith("Dynamic Lamp", System.StringComparison.Ordinal) ||
            objectName == "emissive bulb" ||
            objectName == "ceiling light spill" ||
            objectName == "floor lamp reflection" ||
            objectName == "wall lamp reflection";
    }

    private static void CreateSurfaceReflectionQuad(
        Transform parent,
        string name,
        Vector3 position,
        Vector3 normal,
        Material material,
        float width,
        float height)
    {
        GameObject reflection = GameObject.CreatePrimitive(PrimitiveType.Quad);
        reflection.name = name;
        reflection.transform.SetParent(parent, true);
        reflection.transform.position = position;
        reflection.transform.rotation = Quaternion.FromToRotation(Vector3.forward, normal.normalized);
        reflection.transform.localScale = new Vector3(width, height, 1f);

        Renderer renderer = reflection.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        renderer.receiveShadows = false;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        renderer.shadowCastingMode = ShadowCastingMode.Off;

        Collider collider = reflection.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }
    }

    private static void CreateDiffuseRoomFill(Transform parent, Bounds sceneBounds)
    {
        GameObject fillGroup = CreateChild(parent, "Diffuse Room Fill");
        CreateDirectionalFill(
            fillGroup.transform,
            "cool ambient key",
            new Vector3(62f, -28f, 0f),
            new Color(0.72f, 0.83f, 1f),
            0.72f);

        CreateDirectionalFill(
            fillGroup.transform,
            "warm ceiling bounce",
            new Vector3(78f, 128f, 0f),
            new Color(1f, 0.82f, 0.58f),
            0.34f);
    }

    private static void CreateDirectionalFill(Transform parent, string name, Vector3 eulerAngles, Color color, float intensity)
    {
        GameObject lightObject = CreateChild(parent, name);
        lightObject.transform.rotation = Quaternion.Euler(eulerAngles);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = color;
        light.intensity = intensity;
        light.bounceIntensity = 0.65f;
        light.shadows = LightShadows.None;
        light.renderMode = LightRenderMode.Auto;
        light.lightmapBakeType = LightmapBakeType.Realtime;
    }

    private static void CreateReflectionProbes(Transform parent, Bounds sceneBounds)
    {
        Vector3 size = sceneBounds.size;
        Vector3 center = sceneBounds.center;
        Vector3 probeSize = new Vector3(Mathf.Max(size.x * 0.72f, 18f), 4.2f, Mathf.Max(size.z * 0.72f, 18f));

        CreateReflectionProbe(parent, "Upper Floor Quality Reflection Probe", new Vector3(center.x, 1.05f, center.z), probeSize);
        CreateReflectionProbe(parent, "Lower Floor Quality Reflection Probe", new Vector3(center.x, -1.95f, center.z), probeSize);
    }

    private static void CreateReflectionProbe(Transform parent, string name, Vector3 position, Vector3 size)
    {
        GameObject probeObject = CreateChild(parent, name);
        probeObject.transform.position = position;
        ReflectionProbe probe = probeObject.AddComponent<ReflectionProbe>();
        probe.mode = ReflectionProbeMode.Realtime;
        probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
        probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
        probe.resolution = QualityReflectionProbeResolution;
        probe.intensity = 1.12f;
        probe.boxProjection = true;
        probe.size = size;
        probe.center = Vector3.zero;
        probe.shadowDistance = 42f;
    }

    private static void ConfigureReflectionProbesForPerformance()
    {
        foreach (ReflectionProbe probe in Object.FindObjectsByType<ReflectionProbe>(FindObjectsInactive.Exclude))
        {
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
            probe.resolution = Mathf.Min(probe.resolution, 32);
            probe.intensity = Mathf.Min(probe.intensity, 0.65f);
            probe.shadowDistance = 0f;
            EditorUtility.SetDirty(probe);
        }
    }

    private static void ConfigureReflectionProbesForQuality()
    {
        foreach (ReflectionProbe probe in Object.FindObjectsByType<ReflectionProbe>(FindObjectsInactive.Exclude))
        {
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
            probe.resolution = Mathf.Max(probe.resolution, QualityReflectionProbeResolution);
            probe.intensity = Mathf.Max(probe.intensity, 1.05f);
            probe.boxProjection = true;
            probe.shadowDistance = 42f;
            EditorUtility.SetDirty(probe);
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

    private static Material CreateCeilingSpillMaterial(string materialPath, Color lightColor, float emissionScale, float alpha)
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Lit");
        }

        Material material = LoadOrCreateMaterial(materialPath, shader);
        Texture2D falloff = LoadOrCreateCeilingSpillTexture();
        Color tint = new Color(lightColor.r, lightColor.g, lightColor.b, alpha);

        SetTexture(material, "_MainTex", falloff);
        SetTexture(material, "_EmissionMap", falloff);
        SetColor(material, "_Color", tint);
        SetColor(material, "_EmissionColor", lightColor * emissionScale);
        SetFloat(material, "_Mode", 3f);
        SetFloat(material, "_SrcBlend", (float)BlendMode.SrcAlpha);
        SetFloat(material, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        SetFloat(material, "_ZWrite", 0f);
        SetFloat(material, "_Glossiness", 0f);
        SetFloat(material, "_Metallic", 0f);
        material.EnableKeyword("_ALPHABLEND_ON");
        material.EnableKeyword("_EMISSION");
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = (int)RenderQueue.Transparent;
        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material CreateSurfaceReflectionMaterial(string materialPath, string texturePath, Color lightColor, float emissionScale, float alpha, bool floorReflection)
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Lit");
        }

        Material material = LoadOrCreateMaterial(materialPath, shader);
        Texture2D falloff = LoadOrCreateReflectionFalloffTexture(texturePath, floorReflection);
        Color tint = new Color(lightColor.r, lightColor.g, lightColor.b, alpha);

        SetTexture(material, "_MainTex", falloff);
        SetTexture(material, "_EmissionMap", falloff);
        SetColor(material, "_Color", tint);
        SetColor(material, "_EmissionColor", lightColor * emissionScale);
        SetFloat(material, "_Mode", 3f);
        SetFloat(material, "_SrcBlend", (float)BlendMode.SrcAlpha);
        SetFloat(material, "_DstBlend", (float)BlendMode.One);
        SetFloat(material, "_ZWrite", 0f);
        SetFloat(material, "_Glossiness", floorReflection ? 0.92f : 0.76f);
        SetFloat(material, "_GlossMapScale", 1f);
        SetFloat(material, "_Metallic", 0f);
        SetFloat(material, "_GlossyReflections", 1f);
        SetFloat(material, "_SpecularHighlights", 1f);
        material.EnableKeyword("_ALPHABLEND_ON");
        material.EnableKeyword("_EMISSION");
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = (int)RenderQueue.Transparent;
        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Texture2D LoadOrCreateCeilingSpillTexture()
    {
        Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(CeilingSpillTexturePath);
        if (existing != null)
        {
            return existing;
        }

        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / radius;
                float falloff = Mathf.Clamp01(1f - distance);
                falloff = Mathf.SmoothStep(0f, 1f, falloff);
                float alpha = Mathf.Pow(falloff, 2.35f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(true, false);
        File.WriteAllBytes(CeilingSpillTexturePath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(CeilingSpillTexturePath, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(CeilingSpillTexturePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Trilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.anisoLevel = 4;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(CeilingSpillTexturePath);
    }

    private static Texture2D LoadOrCreateReflectionFalloffTexture(string path, bool floorReflection)
    {
        const int size = 160;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x - center.x) / center.x;
                float ny = (y - center.y) / center.y;
                float radial = Mathf.Clamp01(1f - Mathf.Sqrt(nx * nx + ny * ny));
                radial = Mathf.SmoothStep(0f, 1f, radial);
                float borderDistance = 1f - Mathf.Max(Mathf.Abs(nx), Mathf.Abs(ny));
                float edgeFade = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(borderDistance / 0.18f));

                float streak;
                if (floorReflection)
                {
                    float horizontal = Mathf.Clamp01(1f - Mathf.Abs(nx));
                    float vertical = Mathf.Exp(-Mathf.Abs(ny) * 5.4f);
                    streak = Mathf.Pow(horizontal, 0.7f) * vertical;
                }
                else
                {
                    float core = Mathf.Exp(-Mathf.Abs(nx) * 4.2f);
                    float verticalFade = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(ny)), 0.55f);
                    float fallDown = Mathf.Clamp01(1f - Mathf.Max(0f, -ny) * 0.75f);
                    streak = core * verticalFade * fallDown;
                }

                float alpha = Mathf.Clamp01(Mathf.Pow(radial, floorReflection ? 1.75f : 2.1f) * 0.72f + streak * 0.42f) * edgeFade;
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(true, false);
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Trilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.anisoLevel = 8;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
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

    private static void SetMaterialSurface(string materialName, float metallic, float smoothness, float glossMapScale)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialRoot}/{materialName}.mat");
        if (material == null)
        {
            return;
        }

        SetFloat(material, "_Metallic", metallic);
        SetFloat(material, "_Smoothness", smoothness);
        SetFloat(material, "_Glossiness", smoothness);
        SetFloat(material, "_GlossMapScale", glossMapScale);
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
        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
        SetColor(material, "_EmissionColor", emission);
    }

    private static void SetTexture(Material material, string propertyName, Texture texture)
    {
        if (material.HasProperty(propertyName))
        {
            material.SetTexture(propertyName, texture);
        }
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
        serialized.FindProperty("updateInterval").floatValue = 0.10f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static HashSet<int> SelectRealtimeLampIndices(IReadOnlyList<LampAnchor> anchors)
    {
        HashSet<int> indices = new HashSet<int>();
        if (anchors.Count <= MaxRealtimeLampLights)
        {
            for (int i = 0; i < anchors.Count; i++)
            {
                indices.Add(i);
            }

            return indices;
        }

        float step = (anchors.Count - 1f) / (MaxRealtimeLampLights - 1f);
        for (int i = 0; i < MaxRealtimeLampLights; i++)
        {
            indices.Add(Mathf.RoundToInt(i * step));
        }

        for (int i = 0; indices.Count < MaxRealtimeLampLights && i < anchors.Count; i++)
        {
            indices.Add(i);
        }

        return indices;
    }

    private static void ApplyPerformanceProfileToOpenScene()
    {
        WolfPerformanceSettings settings = FindOrCreatePerformanceSettings();
        ConfigureRuntimeLightingSettings(settings, 1, PerformanceAntiAliasingSamples, true, true, true);
        settings.Apply();

        QualitySettings.pixelLightCount = 1;
        QualitySettings.realtimeReflectionProbes = false;
        QualitySettings.shadows = ShadowQuality.Disable;
        QualitySettings.shadowDistance = 0f;
        QualitySettings.antiAliasing = PerformanceAntiAliasingSamples;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;

        EditorUtility.SetDirty(settings);
        EditorUtility.SetDirty(settings.gameObject);
    }

    private static void ApplyQualityLightingProfileToOpenScene()
    {
        WolfPerformanceSettings settings = FindOrCreatePerformanceSettings();
        ConfigureRuntimeLightingSettings(settings, 24, QualityAntiAliasingSamples, false, false, false);
        settings.Apply();

        QualitySettings.pixelLightCount = 24;
        QualitySettings.realtimeReflectionProbes = true;
        QualitySettings.shadows = ShadowQuality.All;
        QualitySettings.shadowResolution = ShadowResolution.High;
        QualitySettings.shadowProjection = ShadowProjection.StableFit;
        QualitySettings.shadowDistance = 42f;
        QualitySettings.shadowNearPlaneOffset = 2f;
        QualitySettings.antiAliasing = QualityAntiAliasingSamples;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;

        foreach (Camera camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude))
        {
            camera.allowHDR = true;
            camera.allowMSAA = true;
            camera.useOcclusionCulling = true;
            EditorUtility.SetDirty(camera);
        }

        EditorUtility.SetDirty(settings);
        EditorUtility.SetDirty(settings.gameObject);
    }

    private static void ConfigureRuntimeLightingSettings(
        WolfPerformanceSettings settings,
        int maxPixelLights,
        int antiAliasingSamples,
        bool disableRealtimeReflectionProbes,
        bool disableRealtimeShadows,
        bool disableCameraHdr)
    {
        SerializedObject serialized = new SerializedObject(settings);
        SetSerializedInt(serialized, "targetFrameRate", 60);
        SetSerializedInt(serialized, "qualityLevel", 2);
        SetSerializedInt(serialized, "maxPixelLights", maxPixelLights);
        SetSerializedInt(serialized, "antiAliasingSamples", antiAliasingSamples);
        SetSerializedBool(serialized, "disableVSync", true);
        SetSerializedBool(serialized, "disableRealtimeReflectionProbes", disableRealtimeReflectionProbes);
        SetSerializedBool(serialized, "disableRealtimeShadows", disableRealtimeShadows);
        SetSerializedBool(serialized, "forceAnisotropicTextures", true);
        SetSerializedBool(serialized, "enableCameraMsaa", true);
        SetSerializedBool(serialized, "disableCameraHdr", disableCameraHdr);
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetSerializedInt(SerializedObject serialized, string propertyName, int value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
        {
            property.intValue = value;
        }
    }

    private static void SetSerializedBool(SerializedObject serialized, string propertyName, bool value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
        {
            property.boolValue = value;
        }
    }

    private static WolfPerformanceSettings FindOrCreatePerformanceSettings()
    {
        WolfPerformanceSettings[] existingSettings = Object.FindObjectsByType<WolfPerformanceSettings>(FindObjectsInactive.Include);
        if (existingSettings.Length > 0)
        {
            existingSettings[0].gameObject.name = PerformanceRootName;
            return existingSettings[0];
        }

        GameObject settingsObject = new GameObject(PerformanceRootName);
        return settingsObject.AddComponent<WolfPerformanceSettings>();
    }

    private enum SurfaceTarget
    {
        Floor,
        Wall
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
