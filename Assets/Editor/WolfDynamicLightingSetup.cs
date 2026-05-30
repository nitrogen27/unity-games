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
    private const string GlossReflectionProbeRootName = "Wolf Gloss Reflection Probes";
    private const string PerformanceRootName = "Wolf Performance Settings";
    private const string LightProbeRootName = "Wolf Baked Light Probes";
    private const string BeautifulLightingSettingsPath = "Assets/Settings/WolfRepoLevel1BeautifulBakedLighting.lighting";
    private const string BakedReflectionProbeRoot = "Assets/Scenes/WolfRepoLevel1_BakedReflectionProbes";
    private const string MaterialRoot = "Assets/Materials/WolfTargetLook";
    private const string LampGlowMaterialPath = MaterialRoot + "/DynamicLampGlow.mat";
    private const string GeneratedLampCapMaterialPath = MaterialRoot + "/GeneratedCeilingLampCap.mat";
    private const string CeilingSpillTexturePath = MaterialRoot + "/CeilingLampSpillFalloff.png";
    private const string FloorReflectionTexturePath = MaterialRoot + "/LampFloorReflectionFalloff.png";
    private const string WallReflectionTexturePath = MaterialRoot + "/LampWallReflectionFalloff.png";
    private const string WarmCeilingSpillMaterialPath = MaterialRoot + "/CeilingLampSpillWarm.mat";
    private const string CoolCeilingSpillMaterialPath = MaterialRoot + "/CeilingLampSpillCool.mat";
    private const string WarmFloorReflectionMaterialPath = MaterialRoot + "/LampFloorReflectionWarm.mat";
    private const string CoolFloorReflectionMaterialPath = MaterialRoot + "/LampFloorReflectionCool.mat";
    private const string WarmWallReflectionMaterialPath = MaterialRoot + "/LampWallReflectionWarm.mat";
    private const string CoolWallReflectionMaterialPath = MaterialRoot + "/LampWallReflectionCool.mat";
    private const int MaxRealtimeLampLights = 36;
    private const int MaxFlickeringLampLights = 0;
    private const int MaxRealtimeSpecularAccentLights = 0;
    private const int PerformanceAntiAliasingSamples = 2;
    private const int QualityAntiAliasingSamples = 4;
    private const int QualityReflectionProbeResolution = 128;
    private const int LampLightingMask = ~0;
    private const float LightProbeSpacing = 5.5f;
    private const float UpperProbeHeight = 0.9f;
    private const float LowerProbeHeight = -2.1f;
    private const float BakedPointIntensity = 1.70f;
    private const float BakedChandelierPointIntensity = 2.00f;
    private const float BakedCeilingScatterIntensity = 0.70f;
    private const float BakedChandelierScatterIntensity = 0.85f;

    private static readonly Color WarmLamp = new Color(1.0f, 0.72f, 0.36f);
    private static readonly Color CoolLamp = new Color(1.0f, 0.84f, 0.62f);
    private static readonly RoomBalanceFill[] LargeLocationBalanceFills =
    {
        new RoomBalanceFill("large location north west", new Vector3(69.0f, 0.98f, 73.0f), 0.16f, 18.0f),
        new RoomBalanceFill("large location north mid", new Vector3(87.0f, 0.98f, 73.0f), 0.38f, 24.0f),
        new RoomBalanceFill("large location north east", new Vector3(105.0f, 0.98f, 73.0f), 0.38f, 24.0f),
        new RoomBalanceFill("large location far north east", new Vector3(123.0f, 0.98f, 75.0f), 0.38f, 24.0f),
        new RoomBalanceFill("large location center west", new Vector3(69.0f, 0.98f, 93.0f), 0.38f, 24.0f),
        new RoomBalanceFill("large location center mid", new Vector3(87.0f, 0.98f, 93.0f), 0.38f, 24.0f),
        new RoomBalanceFill("large location center east", new Vector3(105.0f, 0.98f, 93.0f), 0.38f, 24.0f),
        new RoomBalanceFill("large location far center east", new Vector3(123.0f, 0.98f, 93.0f), 0.38f, 24.0f),
        new RoomBalanceFill("dog room north west", new Vector3(109.0f, 0.98f, 39.0f), 0.44f, 22.0f),
        new RoomBalanceFill("dog room north east", new Vector3(119.0f, 0.98f, 39.0f), 0.42f, 20.0f),
        new RoomBalanceFill("dog room south west", new Vector3(113.0f, 0.98f, 57.0f), 0.42f, 22.0f),
        new RoomBalanceFill("dog room south east", new Vector3(121.0f, 0.98f, 63.0f), 0.42f, 22.0f),
        new RoomBalanceFill("large location trophy west north", new Vector3(13.0f, 0.98f, 99.0f), 0.34f, 22.0f),
        new RoomBalanceFill("large location trophy west mid", new Vector3(25.0f, 0.98f, 107.0f), 0.38f, 24.0f),
        new RoomBalanceFill("large location trophy west aisle", new Vector3(45.0f, 0.98f, 95.0f), 0.34f, 22.0f),
        new RoomBalanceFill("large location south west", new Vector3(69.0f, 0.98f, 119.0f), 0.38f, 24.0f),
        new RoomBalanceFill("large location far southwest", new Vector3(13.0f, 0.98f, 119.0f), 0.38f, 24.0f),
        new RoomBalanceFill("large location southwest mid", new Vector3(31.0f, 0.98f, 119.0f), 0.38f, 24.0f),
        new RoomBalanceFill("large location south mid", new Vector3(87.0f, 0.98f, 113.0f), 0.38f, 24.0f),
        new RoomBalanceFill("large location south east", new Vector3(105.0f, 0.98f, 113.0f), 0.38f, 24.0f),
        new RoomBalanceFill("large location far south east", new Vector3(123.0f, 0.98f, 113.0f), 0.38f, 24.0f),
    };

    private static bool waitingForBeautifulBake;
    private static double nextBeautifulBakeLogTime;

    [MenuItem("Tools/Wolf Target Look/Apply Dynamic Lighting")]
    public static void ApplyDynamicLighting()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[WolfDynamicLightingSetup] Leaving Play Mode before applying scene lighting.");
            EditorApplication.playModeStateChanged -= ApplyDynamicLightingAfterPlayMode;
            EditorApplication.playModeStateChanged += ApplyDynamicLightingAfterPlayMode;
            EditorApplication.isPlaying = false;
            return;
        }

        AssetDatabase.Refresh();
        Directory.CreateDirectory(MaterialRoot);

        Material glowMaterial = CreateGlowMaterial();
        Material generatedLampCapMaterial = CreateGeneratedLampCapMaterial();
        Material warmCeilingSpillMaterial = CreateCeilingSpillMaterial(
            WarmCeilingSpillMaterialPath,
            Color.Lerp(WarmLamp, Color.white, 0.52f),
            0.050f,
            0.075f);
        Material coolCeilingSpillMaterial = CreateCeilingSpillMaterial(
            CoolCeilingSpillMaterialPath,
            Color.Lerp(CoolLamp, Color.white, 0.58f),
            0.045f,
            0.065f);
        Material warmFloorReflectionMaterial = null;
        Material coolFloorReflectionMaterial = null;
        Material warmWallReflectionMaterial = null;
        Material coolWallReflectionMaterial = null;

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        ClearBakedLightingData();
        DestroyExistingDynamicLightingRoots();

        GameObject root = new GameObject(RootName);
        GameObject lampGroup = CreateChild(root.transform, "Lamp Sources");

        ApplySceneLightingSettings();
        EnhanceReflectiveMaterials();
        ApplyRendererLightingFlags(root.transform);
        EnsureGlossReflectionProbeCoverage();
        int disabledAuthoredFixtureLights = DisableAuthoredFixtureLights();

        List<LampAnchor> anchors = FindLampAnchors();
        HashSet<int> realtimeLampIndices = SelectRealtimeLampIndices(anchors);
        Physics.SyncTransforms();
        int realtimeLampCount = 0;
        int specularAccentCount = 0;
        for (int i = 0; i < anchors.Count; i++)
        {
            bool hasRealtimeLight = realtimeLampIndices.Contains(i);
            bool hasSpecularAccent = hasRealtimeLight && specularAccentCount < MaxRealtimeSpecularAccentLights;
            bool hasFlicker = hasRealtimeLight && realtimeLampCount < MaxFlickeringLampLights;
            if (CreateLampRig(
                anchors[i],
                i,
                lampGroup.transform,
                glowMaterial,
                generatedLampCapMaterial,
                warmCeilingSpillMaterial,
                coolCeilingSpillMaterial,
                warmFloorReflectionMaterial,
                coolFloorReflectionMaterial,
                warmWallReflectionMaterial,
                coolWallReflectionMaterial,
                hasRealtimeLight,
                hasSpecularAccent,
                hasFlicker))
            {
                realtimeLampCount++;

                if (hasSpecularAccent)
                {
                    specularAccentCount++;
                }
            }
        }
        int largeLocationFillCount = CreateLargeLocationBalanceFills(root.transform);

        ApplyQualityLightingProfileToOpenScene();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[WolfDynamicLightingSetup] Added {anchors.Count} green lamp glows, {realtimeLampCount} green lamp ceiling-scatter rigs, {specularAccentCount} no-shadow specular accents, {largeLocationFillCount} low-light large-location balance fills, and disabled {disabledAuthoredFixtureLights} non-green authored lights in {ScenePath}.");
    }

    private static void ApplyDynamicLightingAfterPlayMode(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode)
        {
            return;
        }

        EditorApplication.playModeStateChanged -= ApplyDynamicLightingAfterPlayMode;
        EditorApplication.delayCall += ApplyDynamicLighting;
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
        ClearBakedLightingData();
        ApplySceneLightingSettings();
        ApplyRendererLightingFlags(null);
        EnsureGlossReflectionProbeCoverage();
        ConfigureReflectionProbesForQuality();
        ApplyQualityLightingProfileToOpenScene();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[WolfDynamicLightingSetup] Applied quality lighting profile with realtime shadows and reflections to {ScenePath}.");
    }

    [MenuItem("Tools/Wolf Target Look/Apply Ceiling Realism")]
    public static void ApplyCeilingRealism()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[WolfDynamicLightingSetup] Exit Play Mode before applying ceiling realism.");
            return;
        }

        AssetDatabase.Refresh();
        Directory.CreateDirectory(MaterialRoot);
        Material warmCeilingSpillMaterial = CreateCeilingSpillMaterial(
            WarmCeilingSpillMaterialPath,
            Color.Lerp(WarmLamp, Color.white, 0.52f),
            0.050f,
            0.075f);
        Material coolCeilingSpillMaterial = CreateCeilingSpillMaterial(
            CoolCeilingSpillMaterialPath,
            Color.Lerp(CoolLamp, Color.white, 0.58f),
            0.045f,
            0.065f);

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        WolfTargetMaterialSetup.ConfigureCeilingPanelVisibility();
        ApplyRendererLightingFlags(null);
        int spillCount = EnsureCeilingLightSpills(warmCeilingSpillMaterial, coolCeilingSpillMaterial);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[WolfDynamicLightingSetup] Applied semi-matte ceiling material and {spillCount} soft ceiling light spills to {ScenePath}.");
    }

    [MenuItem("Tools/Wolf Target Look/Apply Gloss Reflection Probe Coverage")]
    public static void ApplyGlossReflectionProbeCoverage()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[WolfDynamicLightingSetup] Exit Play Mode before applying reflection probe coverage.");
            return;
        }

        AssetDatabase.Refresh();
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ApplySceneLightingSettings();
        ApplyRendererLightingFlags(null);
        int probeCount = EnsureGlossReflectionProbeCoverage();
        ConfigureReflectionProbesForQuality();
        ApplyQualityLightingProfileToOpenScene();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[WolfDynamicLightingSetup] Applied {probeCount} box-projected gloss reflection probes to {ScenePath}.");
    }

    [MenuItem("Tools/Wolf Target Look/Apply Beautiful Baked Lighting Settings")]
    public static void ApplyBeautifulBakedLightingSettings()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[WolfDynamicLightingSetup] Exit Play Mode before applying baked lighting settings.");
            return;
        }

        AssetDatabase.Refresh();
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        BeautifulLightingSummary summary = ApplyBeautifulBakedLightingProfile();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[WolfDynamicLightingSetup] Applied beautiful baked lighting profile to {ScenePath}: {summary}.");
    }

    [MenuItem("Tools/Wolf Target Look/Bake Beautiful Lighting And Occlusion")]
    public static void BakeBeautifulLightingAndOcclusion()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[WolfDynamicLightingSetup] Exit Play Mode before baking lighting and occlusion.");
            return;
        }

        if (Lightmapping.isRunning || StaticOcclusionCulling.isRunning)
        {
            Debug.LogWarning("[WolfDynamicLightingSetup] Lighting or occlusion bake is already running.");
            return;
        }

        AssetDatabase.Refresh();
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        BeautifulLightingSummary summary = ApplyBeautifulBakedLightingProfile();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        StaticOcclusionCulling.Clear();
        StaticOcclusionCulling.smallestOccluder = 1.0f;
        StaticOcclusionCulling.smallestHole = 0.25f;
        StaticOcclusionCulling.backfaceThreshold = 100f;
        bool occlusionStarted = StaticOcclusionCulling.GenerateInBackground();

        Lightmapping.Clear();
        bool lightingStarted = Lightmapping.BakeAsync();
        if (!lightingStarted && !occlusionStarted)
        {
            Debug.LogError("[WolfDynamicLightingSetup] Failed to start lighting and occlusion bake.");
            return;
        }

        waitingForBeautifulBake = true;
        nextBeautifulBakeLogTime = 0d;
        EditorApplication.update -= MonitorBeautifulBake;
        EditorApplication.update += MonitorBeautifulBake;
        Debug.Log($"[WolfDynamicLightingSetup] Started beautiful lighting/occlusion bake for {ScenePath}. Occlusion started={occlusionStarted}, lighting started={lightingStarted}. Profile: {summary}.");
    }

    [MenuItem("Tools/Wolf Target Look/Log Beautiful Bake Status")]
    public static void LogBeautifulBakeStatus()
    {
        Debug.Log($"[WolfDynamicLightingSetup] Beautiful bake status: lightingRunning={Lightmapping.isRunning}, lightingProgress={Lightmapping.buildProgress:P0}, occlusionRunning={StaticOcclusionCulling.isRunning}, occlusionData={StaticOcclusionCulling.umbraDataSize:N0} bytes.");
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

    private static BeautifulLightingSummary ApplyBeautifulBakedLightingProfile()
    {
        ApplySceneLightingSettings();
        ApplyRendererLightingFlags(null);
        int staticRenderers = ConfigureRendererStaticBakeFlags();
        int bakedLights = ConfigureLightsForBeautifulBake();
        int lightProbeCount = CreateOrUpdateBakedLightProbes();
        ConfigureReflectionProbesForBakedQuality();
        ConfigureBeautifulLightingSettings();
        ApplyBeautifulRuntimeProfileToOpenScene();

        return new BeautifulLightingSummary(staticRenderers, bakedLights, lightProbeCount);
    }

    private static int ConfigureRendererStaticBakeFlags()
    {
        int staticRendererCount = 0;
        StaticEditorFlags staticFlags =
            StaticEditorFlags.ContributeGI |
            StaticEditorFlags.OccluderStatic |
            StaticEditorFlags.OccludeeStatic |
            StaticEditorFlags.BatchingStatic |
            StaticEditorFlags.ReflectionProbeStatic;

        foreach (Renderer renderer in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
        {
            GameObject go = renderer.gameObject;
            StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(go);
            if (IsDynamicBakeObject(renderer.transform))
            {
                flags &= ~staticFlags;
                GameObjectUtility.SetStaticEditorFlags(go, flags);
                EditorUtility.SetDirty(go);
                continue;
            }

            if (renderer is MeshRenderer meshRenderer)
            {
                flags |= staticFlags;
                GameObjectUtility.SetStaticEditorFlags(go, flags);
                renderer.receiveShadows = true;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Simple;
                meshRenderer.stitchLightmapSeams = true;
                meshRenderer.scaleInLightmap = GetStructuralLightmapScale(renderer.gameObject.name);
                EditorUtility.SetDirty(renderer);
                EditorUtility.SetDirty(go);
                staticRendererCount++;
            }
        }

        return staticRendererCount;
    }

    private static float GetStructuralLightmapScale(string objectName)
    {
        if (objectName == "Floor" ||
            objectName == "Ceiling" ||
            objectName.StartsWith("Wall ", System.StringComparison.Ordinal) ||
            objectName.Contains(" Floor", System.StringComparison.Ordinal) ||
            objectName.Contains(" Ceiling", System.StringComparison.Ordinal) ||
            objectName.Contains(" Wall", System.StringComparison.Ordinal))
        {
            return 1.55f;
        }

        return 0.75f;
    }

    private static void DestroyExistingDynamicLightingRoots()
    {
        List<GameObject> roots = new List<GameObject>();
        foreach (GameObject gameObject in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
        {
            if (gameObject != null &&
                gameObject.name == RootName &&
                gameObject.transform.parent == null)
            {
                roots.Add(gameObject);
            }
        }

        foreach (GameObject root in roots)
        {
            Object.DestroyImmediate(root);
        }
    }

    private static int ConfigureLightsForBeautifulBake()
    {
        int bakedLightCount = 0;
        foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude))
        {
            if (IsPlayerLight(light))
            {
                light.lightmapBakeType = LightmapBakeType.Realtime;
                light.shadows = LightShadows.None;
                light.renderMode = LightRenderMode.Auto;
                light.intensity = 0.08f;
                light.range = 5.5f;
                light.bounceIntensity = 0f;
                light.color = new Color(1f, 0.96f, 0.88f);
                EditorUtility.SetDirty(light);
                continue;
            }

            if (IsRealtimeSpecularLight(light))
            {
                ConfigureRealtimeSpecularLight(light);
                EditorUtility.SetDirty(light);
                continue;
            }

            light.lightmapBakeType = LightmapBakeType.Baked;
            light.renderMode = LightRenderMode.Auto;
            ConfigureBeautifulBakedLightOutput(light);
            bakedLightCount++;

            FlickerLight flicker = light.GetComponent<FlickerLight>();
            if (flicker != null)
            {
                flicker.enabled = false;
                EditorUtility.SetDirty(flicker);
            }

            EditorUtility.SetDirty(light);
        }

        return bakedLightCount;
    }

    private static void ConfigureBeautifulBakedLightOutput(Light light)
    {
        string objectName = light.gameObject.name;
        string parentName = light.transform.parent != null ? light.transform.parent.name : string.Empty;
        bool isChandelier = parentName.IndexOf("chandelier", System.StringComparison.OrdinalIgnoreCase) >= 0;

        if (objectName == "point bounce")
        {
            light.intensity = isChandelier ? BakedChandelierPointIntensity : BakedPointIntensity;
            light.range = isChandelier ? 22f : 18f;
            light.bounceIntensity = 0.65f;
            light.shadows = LightShadows.None;
            light.shadowStrength = 0f;
            light.shadowBias = 0.028f;
            light.shadowNormalBias = 0.18f;
            light.shadowNearPlane = 0.1f;
            light.shadowResolution = LightShadowResolution.High;
            return;
        }

        if (objectName == "ceiling scatter")
        {
            light.intensity = isChandelier ? BakedChandelierScatterIntensity : BakedCeilingScatterIntensity;
            light.range = isChandelier ? 11f : 9.5f;
            light.bounceIntensity = 0.45f;
            light.shadows = LightShadows.None;
            return;
        }

        if (objectName == "cool ambient key")
        {
            light.intensity = 0.55f;
            light.bounceIntensity = 0.25f;
            light.shadows = LightShadows.None;
            return;
        }

        if (objectName == "warm ceiling bounce")
        {
            light.intensity = 0.28f;
            light.bounceIntensity = 0.20f;
            light.shadows = LightShadows.None;
            return;
        }

        if (light.type == LightType.Point)
        {
            light.intensity = Mathf.Min(Mathf.Max(light.intensity, 0.45f), 1.20f);
            light.range = Mathf.Min(Mathf.Max(light.range, 10f), 18f);
            light.bounceIntensity = Mathf.Min(Mathf.Max(light.bounceIntensity, 0.20f), 0.65f);
            light.shadows = LightShadows.None;
        }
    }

    private static bool IsRealtimeSpecularLight(Light light)
    {
        string objectName = light.gameObject.name;
        return objectName == "specular accent" ||
               objectName == "cool specular key" ||
               objectName == "warm specular lift";
    }

    private static void ConfigureRealtimeSpecularLight(Light light)
    {
        string objectName = light.gameObject.name;
        light.lightmapBakeType = LightmapBakeType.Realtime;
        light.shadows = LightShadows.None;
        light.renderMode = LightRenderMode.ForcePixel;
        light.bounceIntensity = 0f;

        if (objectName == "cool specular key")
        {
            light.intensity = 0.10f;
            light.color = new Color(0.74f, 0.88f, 1f);
            return;
        }

        if (objectName == "warm specular lift")
        {
            light.intensity = 0.035f;
            light.color = new Color(1f, 0.84f, 0.64f);
            return;
        }

        string parentName = light.transform.parent != null ? light.transform.parent.name : string.Empty;
        bool isChandelier = parentName.IndexOf("chandelier", System.StringComparison.OrdinalIgnoreCase) >= 0;
        light.intensity = isChandelier ? 0.14f : 0.11f;
        light.range = isChandelier ? 13.5f : 11.5f;
    }

    private static void ConfigureReflectionProbesForBakedQuality()
    {
        foreach (ReflectionProbe probe in Object.FindObjectsByType<ReflectionProbe>(FindObjectsInactive.Exclude))
        {
            probe.mode = ReflectionProbeMode.Baked;
            probe.resolution = Mathf.Max(probe.resolution, QualityReflectionProbeResolution);
            probe.intensity = Mathf.Max(probe.intensity, 1.55f);
            probe.boxProjection = true;
            probe.shadowDistance = 42f;
            EditorUtility.SetDirty(probe);
        }
    }

    private static int CreateOrUpdateBakedLightProbes()
    {
        GameObject probeRoot = GameObject.Find(LightProbeRootName);
        if (probeRoot == null)
        {
            probeRoot = new GameObject(LightProbeRootName);
        }

        LightProbeGroup probeGroup = probeRoot.GetComponent<LightProbeGroup>();
        if (probeGroup == null)
        {
            probeGroup = probeRoot.AddComponent<LightProbeGroup>();
        }

        probeRoot.transform.position = Vector3.zero;
        probeRoot.transform.rotation = Quaternion.identity;
        probeRoot.transform.localScale = Vector3.one;

        Bounds bounds = CalculateSceneBounds(GameObject.Find(RootName) != null ? GameObject.Find(RootName).transform : null);
        Physics.SyncTransforms();

        List<Vector3> probes = new List<Vector3>();
        AddProbeGrid(probes, bounds, UpperProbeHeight);
        AddProbeGrid(probes, bounds, LowerProbeHeight);

        if (probes.Count < 4)
        {
            probes.Add(bounds.center + Vector3.up * 0.5f);
            probes.Add(bounds.center + Vector3.right * 2f + Vector3.up * 0.5f);
            probes.Add(bounds.center - Vector3.right * 2f + Vector3.up * 0.5f);
            probes.Add(bounds.center + Vector3.forward * 2f + Vector3.up * 0.5f);
        }

        probeGroup.probePositions = probes.ToArray();
        EditorUtility.SetDirty(probeGroup);
        EditorUtility.SetDirty(probeRoot);
        return probes.Count;
    }

    private static void AddProbeGrid(List<Vector3> probes, Bounds bounds, float y)
    {
        if (y < bounds.min.y + 0.25f || y > bounds.max.y - 0.25f)
        {
            return;
        }

        float minX = Mathf.Ceil((bounds.min.x + 1f) / LightProbeSpacing) * LightProbeSpacing;
        float maxX = bounds.max.x - 1f;
        float minZ = Mathf.Ceil((bounds.min.z + 1f) / LightProbeSpacing) * LightProbeSpacing;
        float maxZ = bounds.max.z - 1f;

        for (float x = minX; x <= maxX; x += LightProbeSpacing)
        {
            for (float z = minZ; z <= maxZ; z += LightProbeSpacing)
            {
                Vector3 position = new Vector3(x, y, z);
                if (Physics.CheckSphere(position, 0.28f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                probes.Add(position);
            }
        }
    }

    private static void ConfigureBeautifulLightingSettings()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(BeautifulLightingSettingsPath));
        LightingSettings settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(BeautifulLightingSettingsPath);
        if (settings == null)
        {
            settings = new LightingSettings();
            AssetDatabase.CreateAsset(settings, BeautifulLightingSettingsPath);
        }

        settings.bakedGI = true;
        settings.realtimeGI = false;
        settings.realtimeEnvironmentLighting = false;
        settings.autoGenerate = false;
        settings.mixedBakeMode = MixedLightingMode.Shadowmask;
        settings.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
        settings.directionalityMode = LightmapsMode.CombinedDirectional;
        settings.lightmapResolution = 32f;
        settings.lightmapMaxSize = 2048;
        settings.lightmapPadding = 8;
        settings.lightmapCompression = LightmapCompression.None;
        settings.compressLightmaps = false;
        settings.ao = false;
        settings.aoMaxDistance = 0.25f;
        settings.aoExponentDirect = 0.05f;
        settings.aoExponentIndirect = 0.04f;
        settings.directSampleCount = 192;
        settings.indirectSampleCount = 768;
        settings.environmentSampleCount = 192;
        settings.maxBounces = 6;
        settings.indirectResolution = 2.5f;
        settings.filteringMode = LightingSettings.FilterMode.Auto;
        settings.prioritizeView = false;
        settings.respectSceneVisibilityWhenBakingGI = false;

        Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.OnDemand;
        Lightmapping.bakedGI = true;
        Lightmapping.realtimeGI = false;
        Lightmapping.lightingSettings = settings;
        LightmapSettings.lightmapsMode = LightmapsMode.CombinedDirectional;

        EditorUtility.SetDirty(settings);
    }

    private static void ApplyBeautifulRuntimeProfileToOpenScene()
    {
        WolfPerformanceSettings settings = FindOrCreatePerformanceSettings();
        ConfigureRuntimeLightingSettings(settings, MaxRealtimeLampLights * 13, 0, true, true, false, RenderingPath.DeferredShading);
        settings.Apply();

        QualitySettings.pixelLightCount = MaxRealtimeLampLights * 13;
        QualitySettings.realtimeReflectionProbes = false;
        QualitySettings.shadows = ShadowQuality.Disable;
        QualitySettings.shadowResolution = ShadowResolution.High;
        QualitySettings.shadowProjection = ShadowProjection.StableFit;
        QualitySettings.shadowDistance = 0f;
        QualitySettings.shadowNearPlaneOffset = 2f;
        QualitySettings.antiAliasing = 0;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;

        foreach (Camera camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude))
        {
            camera.allowHDR = true;
            camera.allowMSAA = false;
            camera.useOcclusionCulling = true;
            camera.renderingPath = RenderingPath.DeferredShading;
            EnsureDeferredPostAntialiasing(camera, true);
            EditorUtility.SetDirty(camera);
        }

        EditorUtility.SetDirty(settings);
        EditorUtility.SetDirty(settings.gameObject);
    }

    private static void MonitorBeautifulBake()
    {
        if (!waitingForBeautifulBake)
        {
            EditorApplication.update -= MonitorBeautifulBake;
            return;
        }

        if (Lightmapping.isRunning || StaticOcclusionCulling.isRunning)
        {
            if (EditorApplication.timeSinceStartup >= nextBeautifulBakeLogTime)
            {
                nextBeautifulBakeLogTime = EditorApplication.timeSinceStartup + 10d;
                LogBeautifulBakeStatus();
            }

            return;
        }

        waitingForBeautifulBake = false;
        EditorApplication.update -= MonitorBeautifulBake;
        int bakedProbes = BakeReflectionProbesForBeautifulProfile();
        LightProbes.Tetrahedralize();
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[WolfDynamicLightingSetup] Beautiful lighting/occlusion bake finished for {ScenePath}. Baked reflection probes: {bakedProbes}, occlusion data: {StaticOcclusionCulling.umbraDataSize:N0} bytes.");
    }

    private static int BakeReflectionProbesForBeautifulProfile()
    {
        Directory.CreateDirectory(BakedReflectionProbeRoot);
        int bakedCount = 0;
        foreach (ReflectionProbe probe in Object.FindObjectsByType<ReflectionProbe>(FindObjectsInactive.Exclude))
        {
            if (probe.mode != ReflectionProbeMode.Baked)
            {
                continue;
            }

            string path = $"{BakedReflectionProbeRoot}/{SanitizeFileName(probe.name)}.exr";
            if (Lightmapping.BakeReflectionProbe(probe, path))
            {
                bakedCount++;
                EditorUtility.SetDirty(probe);
            }
        }

        return bakedCount;
    }

    private static string SanitizeFileName(string value)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(invalid, '_');
        }

        return string.IsNullOrWhiteSpace(value) ? "ReflectionProbe" : value;
    }

    private static bool IsDynamicBakeObject(Transform transform)
    {
        if (transform.GetComponentInParent<WolfDoor>() != null ||
            transform.GetComponentInParent<WolfBillboard>() != null ||
            transform.GetComponentInParent<SimpleFirstPersonController>() != null)
        {
            return true;
        }

        string rootName = transform.root != null ? transform.root.name : string.Empty;
        return rootName == RootName ||
               rootName == LightProbeRootName ||
               rootName == "Wolf Repo Player" ||
               rootName == "Wolf Repo HUD" ||
               rootName == PerformanceRootName;
    }

    private static bool IsPlayerLight(Light light)
    {
        string rootName = light.transform.root != null ? light.transform.root.name : string.Empty;
        return rootName == "Wolf Repo Player";
    }

    private static void ApplySceneLightingSettings()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.24f, 0.23f, 0.21f);
        RenderSettings.ambientEquatorColor = new Color(0.19f, 0.18f, 0.16f);
        RenderSettings.ambientGroundColor = new Color(0.14f, 0.135f, 0.125f);
        RenderSettings.ambientLight = new Color(0.18f, 0.17f, 0.155f);
        RenderSettings.ambientIntensity = 0.42f;
        RenderSettings.reflectionIntensity = 0.18f;
        RenderSettings.reflectionBounces = 1;
        RenderSettings.defaultReflectionResolution = QualityReflectionProbeResolution;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.00045f;
        RenderSettings.fogColor = new Color(0.24f, 0.27f, 0.32f);

        foreach (Camera camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude))
        {
            camera.allowHDR = true;
            camera.allowMSAA = false;
            camera.useOcclusionCulling = true;
            camera.renderingPath = RenderingPath.DeferredShading;
            EnsureDeferredPostAntialiasing(camera, true);
            EditorUtility.SetDirty(camera);
        }
    }

    private static void EnhanceReflectiveMaterials()
    {
        SetMaterialSurface("BlueWall_Target", 0.0f, 0.64f, 0.82f, 0.88f, 0.78f);
        SetMaterialSurface("WhiteStoneWall_Target", 0.0f, 0.48f, 0.0f, 0.72f, 0.70f);
        SetMaterialSurface("WhiteStoneWall_Dark_Target", 0.0f, 0.48f, 0.0f, 0.72f, 0.70f);
        SetMaterialSurface("DoorTeal_Target", 0.34f, 0.54f, 0.68f, 0.62f, 0.45f);
        SetMaterialSurface("FloorTile_Target", 0.0f, 0.56f, 1.0f, 1.0f, 1.0f);
        SetMaterialSurface("CeilingPanel_Target", 0.0f, 0.20f, 0.28f, 0.18f, 0.30f);
        SetMaterialSurface("DarkMetalTrim_Target", 0.34f, 0.38f, 0.48f, 0.40f, 0.46f);
        SetMaterialSurface("PrisonCellDoor_Target", 0.18f, 0.55f, 0.72f, 0.85f, 0.90f);
        WolfTargetMaterialSetup.ConfigureDoorTargetMaterial();
        WolfTargetMaterialSetup.ConfigureFloorTileMaterial();
        WolfTargetMaterialSetup.ConfigureCeilingPanelVisibility();
        TuneReferenceMaterialDetails();

        SetEmission("Assets/Materials/Mat_Lamp_Glow.mat", WarmLamp * 4.4f);
        SetEmission("Assets/Materials/WolfRepo/Mat_WolfRepo_LampWarmBulb.mat", WarmLamp * 4.7f);
        SetEmission("Assets/Materials/WolfRepo/Mat_WolfRepo_LampGreenBulb.mat", CoolLamp * 6.0f);
        SetEmission("Assets/Materials/WolfRepo/Mat_WolfRepo_StairLampBulb.mat", WarmLamp * 4.0f);
    }

    private static void TuneReferenceMaterialDetails()
    {
        TuneTargetMaterial(
            "BlueWall_Target",
            new Color(1.44f, 1.54f, 2.08f, 1f),
            new Color(0.075f, 0.10f, 0.24f, 1f),
            0.20f,
            0.0f,
            0f,
            0.88f,
            0.78f);

        TuneTargetMaterial(
            "WhiteStoneWall_Target",
            new Color(1.08f, 1.06f, 1.00f, 1f),
            new Color(0.10f, 0.096f, 0.086f, 1f),
            0.18f,
            0.0f,
            0f,
            0.72f,
            0.70f);

        TuneTargetMaterial(
            "WhiteStoneWall_Dark_Target",
            new Color(1.08f, 1.06f, 1.00f, 1f),
            new Color(0.10f, 0.096f, 0.086f, 1f),
            0.18f,
            0.0f,
            0f,
            0.72f,
            0.70f);

        TuneTargetMaterial(
            "DoorTeal_Target",
            new Color(0.94f, 1.06f, 1.10f, 1f),
            new Color(0.012f, 0.038f, 0.044f, 1f),
            0.72f,
            0.24f,
            0f,
            0.62f,
            0.45f);

        TuneTargetMaterial(
            "FloorTile_Target",
            new Color(1.22f, 1.22f, 1.17f, 1f),
            Color.black,
            0.08f,
            0.0f,
            0f,
            1.0f,
            1.0f);

        TuneTargetMaterial(
            "CeilingPanel_Target",
            new Color(1.28f, 1.32f, 1.26f, 1f),
            new Color(0.11f, 0.114f, 0.122f, 1f),
            0.18f,
            0.0f,
            0f,
            0.18f,
            0.30f);

        TuneTargetMaterial(
            "DarkMetalTrim_Target",
            new Color(1.18f, 1.22f, 1.30f, 1f),
            new Color(0.035f, 0.040f, 0.048f, 1f),
            0.64f,
            0.22f,
            0f,
            0.40f,
            0.46f);

        TuneTargetMaterial(
            "PrisonCellDoor_Target",
            new Color(1.24f, 1.28f, 1.34f, 1f),
            new Color(0.04f, 0.044f, 0.052f, 1f),
            0.52f,
            0.22f,
            0f,
            0.85f,
            0.90f);
    }

    private static void TuneTargetMaterial(
        string materialName,
        Color colorTint,
        Color emissionTint,
        float bumpScale,
        float occlusionStrength,
        float parallax,
        float glossyReflections,
        float specularHighlights)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialRoot}/{materialName}.mat");
        if (material == null)
        {
            return;
        }

        SetColor(material, "_Color", colorTint);
        SetColor(material, "_BaseColor", colorTint);
        SetColor(material, "_EmissionColor", emissionTint);
        SetFloat(material, "_BumpScale", bumpScale);
        SetFloat(material, "_OcclusionStrength", occlusionStrength);
        SetFloat(material, "_Parallax", parallax);
        SetFloat(material, "_GlossyReflections", glossyReflections);
        SetFloat(material, "_SpecularHighlights", specularHighlights);
        ConfigureStandardReflectionKeywords(material, glossyReflections, specularHighlights);

        if (emissionTint.maxColorComponent > 0f)
        {
            material.EnableKeyword("_EMISSION");
        }
        else
        {
            material.DisableKeyword("_EMISSION");
        }

        bool useParallax = parallax > 0f && material.HasProperty("_ParallaxMap") && material.GetTexture("_ParallaxMap") != null;
        if (useParallax)
        {
            material.EnableKeyword("_PARALLAXMAP");
        }
        else
        {
            material.DisableKeyword("_PARALLAXMAP");
        }

        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        EditorUtility.SetDirty(material);
    }

    private static void ApplyRendererLightingFlags(Transform generatedRoot)
    {
        GameObject dynamicLightingRoot = GameObject.Find(RootName);
        Transform dynamicLightingTransform = dynamicLightingRoot != null ? dynamicLightingRoot.transform : null;

        foreach (Renderer renderer in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
        {
            if (generatedRoot != null && renderer.transform.IsChildOf(generatedRoot))
            {
                continue;
            }

            if (dynamicLightingTransform != null && renderer.transform.IsChildOf(dynamicLightingTransform))
            {
                renderer.receiveShadows = false;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                EditorUtility.SetDirty(renderer);
                continue;
            }

            renderer.lightmapIndex = -1;
            renderer.realtimeLightmapIndex = -1;
            renderer.lightmapScaleOffset = new Vector4(1f, 1f, 0f, 0f);
            renderer.realtimeLightmapScaleOffset = new Vector4(1f, 1f, 0f, 0f);

            string objectName = renderer.gameObject.name;
            bool isFloor = IsFloorObject(objectName);
            bool isWall = IsWallObject(objectName);
            bool isLargeStructuralSurface = IsLargeStructuralSurface(objectName);

            if (isFloor)
            {
                renderer.gameObject.layer = 0;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Simple;
                renderer.receiveShadows = true;
                if (renderer is MeshRenderer)
                {
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                }
            }
            else if (isWall)
            {
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Simple;
                renderer.receiveShadows = false;
                if (renderer is MeshRenderer)
                {
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                }
            }
            else if (isLargeStructuralSurface)
            {
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                renderer.receiveShadows = false;
                if (renderer is MeshRenderer)
                {
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                }
            }
            else
            {
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Simple;
                renderer.receiveShadows = true;
                if (renderer is MeshRenderer)
                {
                    renderer.shadowCastingMode = ShadowCastingMode.On;
                }
            }

            EditorUtility.SetDirty(renderer);
        }
    }

    private static bool IsFloorObject(string objectName)
    {
        return objectName == "Floor" ||
            objectName.StartsWith("Floor ", System.StringComparison.Ordinal) ||
            objectName.Contains("_Floor_", System.StringComparison.Ordinal) ||
            objectName.Contains(" Floor", System.StringComparison.Ordinal);
    }

    private static bool IsLargeStructuralSurface(string objectName)
    {
        return objectName == "Ceiling" ||
            objectName.StartsWith("Ceiling ", System.StringComparison.Ordinal) ||
            objectName.Contains(" Ceiling", System.StringComparison.Ordinal);
    }

    private static bool IsWallObject(string objectName)
    {
        return objectName.StartsWith("Wall ", System.StringComparison.Ordinal) ||
            objectName.StartsWith("WolfRepoWallCell_", System.StringComparison.Ordinal) ||
            objectName.Contains(" Wall", System.StringComparison.Ordinal) ||
            objectName.Contains(" Header", System.StringComparison.Ordinal) ||
            objectName.Contains("Void Blocker", System.StringComparison.Ordinal) ||
            objectName.Contains("Facade", System.StringComparison.Ordinal) ||
            objectName.Contains("Pier", System.StringComparison.Ordinal);
    }

    private static void ClearBakedLightingData()
    {
        if (Lightmapping.isRunning)
        {
            Lightmapping.Cancel();
        }

        Lightmapping.Clear();
        Lightmapping.ClearLightingDataAsset();
        StaticOcclusionCulling.Clear();
        LightmapSettings.lightmaps = new LightmapData[0];
        LightmapSettings.lightProbes = null;

        foreach (Renderer renderer in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include))
        {
            renderer.lightmapIndex = -1;
            renderer.realtimeLightmapIndex = -1;
            renderer.lightmapScaleOffset = new Vector4(1f, 1f, 0f, 0f);
            renderer.realtimeLightmapScaleOffset = new Vector4(1f, 1f, 0f, 0f);
            EditorUtility.SetDirty(renderer);
        }
    }

    private static int DisableAuthoredFixtureLights()
    {
        int disabledCount = 0;
        foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude))
        {
            if (!light.enabled)
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
            if (objectName.StartsWith("ceilLight cap ", System.StringComparison.Ordinal))
            {
                Vector3 anchorPosition = transform.position + Vector3.down * 0.05f;
                float baseIntensity = IsLargeStoneRoomAnchor(anchorPosition) ? 0.86f : 1.55f;
                anchors.Add(new LampAnchor(objectName, anchorPosition, CoolLamp, baseIntensity, 15.5f, false));
            }
            else if (objectName.StartsWith("chandelier cap ", System.StringComparison.Ordinal))
            {
                Vector3 anchorPosition = transform.position + Vector3.down * 0.05f;
                float baseIntensity = IsLargeStoneRoomAnchor(anchorPosition) ? 0.96f : 1.20f;
                anchors.Add(new LampAnchor(objectName, anchorPosition, WarmLamp, baseIntensity, 16.0f, true));
            }
        }

        AddStartRoomLampAnchor(anchors);
        anchors.Sort((left, right) => string.Compare(left.Name, right.Name, System.StringComparison.Ordinal));
        return anchors;
    }

    private static void AddStartRoomLampAnchor(List<LampAnchor> anchors)
    {
        Vector3 startLampPosition = new Vector3(59.0f, 1.86f, 13.0f);
        Vector2 startXZ = new Vector2(startLampPosition.x, startLampPosition.z);

        for (int i = 0; i < anchors.Count; i++)
        {
            Vector2 anchorXZ = new Vector2(anchors[i].Position.x, anchors[i].Position.z);
            if ((anchorXZ - startXZ).sqrMagnitude < 6.25f)
            {
                return;
            }
        }

        anchors.Add(new LampAnchor("start room generated ceilLight cap", startLampPosition, CoolLamp, 1.75f, 15.5f, false, true));
    }

    private static bool CreateLampRig(
        LampAnchor anchor,
        int index,
        Transform parent,
        Material glowMaterial,
        Material generatedLampCapMaterial,
        Material warmCeilingSpillMaterial,
        Material coolCeilingSpillMaterial,
        Material warmFloorReflectionMaterial,
        Material coolFloorReflectionMaterial,
        Material warmWallReflectionMaterial,
        Material coolWallReflectionMaterial,
        bool createRealtimeLight,
        bool createSpecularAccent,
        bool createFlicker)
    {
        GameObject rig = CreateChild(parent, $"Dynamic Lamp {index:00} - {anchor.Name}");
        rig.transform.position = anchor.Position;

        if (anchor.CreateFixtureMesh)
        {
            CreateGeneratedCeilingLampFixture(rig.transform, generatedLampCapMaterial);
        }

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

        if (!createRealtimeLight)
        {
            return false;
        }

        GameObject pointObject = CreateChild(rig.transform, "point bounce");
        pointObject.transform.localPosition = Vector3.down * 0.06f;
        Light point = pointObject.AddComponent<Light>();
        point.type = LightType.Point;
        point.color = Color.Lerp(anchor.Color, Color.white, 0.34f);
        point.intensity = anchor.BaseIntensity * (anchor.IsChandelier ? 0.08f : 0.12f);
        point.range = anchor.IsChandelier ? 26.0f : 30.0f;
        point.bounceIntensity = 0.0f;
        point.shadows = LightShadows.None;
        point.cullingMask = LampLightingMask;
        point.shadowStrength = 0f;
        point.shadowBias = 0.035f;
        point.shadowNormalBias = 0.24f;
        point.shadowNearPlane = 0.12f;
        point.shadowResolution = LightShadowResolution.High;
        point.renderMode = LightRenderMode.ForcePixel;
        point.lightmapBakeType = LightmapBakeType.Realtime;

        GameObject ceilingBounceObject = CreateChild(rig.transform, "ceiling bounce");
        ceilingBounceObject.transform.localPosition = Vector3.down * 0.16f;
        Light ceilingBounce = ceilingBounceObject.AddComponent<Light>();
        ceilingBounce.type = LightType.Point;
        ceilingBounce.color = Color.Lerp(anchor.Color, Color.white, 0.42f);
        ceilingBounce.intensity = anchor.BaseIntensity * (anchor.IsChandelier ? 0.06f : 0.10f);
        ceilingBounce.range = anchor.IsChandelier ? 24.0f : 28.0f;
        ceilingBounce.bounceIntensity = 0.0f;
        ceilingBounce.shadows = LightShadows.None;
        ceilingBounce.cullingMask = LampLightingMask;
        ceilingBounce.renderMode = LightRenderMode.ForcePixel;
        ceilingBounce.lightmapBakeType = LightmapBakeType.Realtime;

        GameObject ceilingScatterObject = CreateChild(rig.transform, "ceiling scatter");
        ceilingScatterObject.transform.localPosition = Vector3.down * 0.02f;
        ceilingScatterObject.transform.localRotation = Quaternion.LookRotation(Vector3.down);
        Light ceilingScatter = ceilingScatterObject.AddComponent<Light>();
        ceilingScatter.type = LightType.Point;
        ceilingScatter.color = Color.Lerp(anchor.Color, Color.white, 0.42f);
        ceilingScatter.intensity = anchor.BaseIntensity * (anchor.IsChandelier ? 0.055f : 0.10f);
        ceilingScatter.range = anchor.IsChandelier ? 24.0f : 30.0f;
        ceilingScatter.spotAngle = 30f;
        ceilingScatter.bounceIntensity = 0.0f;
        ceilingScatter.shadows = LightShadows.None;
        ceilingScatter.cullingMask = LampLightingMask;
        ceilingScatter.renderMode = LightRenderMode.ForcePixel;
        ceilingScatter.lightmapBakeType = LightmapBakeType.Realtime;

        CreateCeilingGlowLight(rig.transform, anchor);
        CreateCeilingLightSpill(rig.transform, anchor, warmCeilingSpillMaterial, coolCeilingSpillMaterial);
        CreateCeilingDiffuserLights(rig.transform, anchor);
        CreateWallScatterLights(rig.transform, anchor);
        if (IsLargeStoneRoomAnchor(anchor.Position))
        {
            CreateLargeStoneRoomFillLight(rig.transform, anchor);
        }

        if (createSpecularAccent)
        {
            GameObject specularObject = CreateChild(rig.transform, "specular accent");
            specularObject.transform.localPosition = Vector3.down * (anchor.IsChandelier ? 0.20f : 0.12f);
            Light specular = specularObject.AddComponent<Light>();
            specular.type = LightType.Point;
            specular.color = Color.Lerp(anchor.Color, Color.white, anchor.IsChandelier ? 0.68f : 0.74f);
            specular.intensity = anchor.BaseIntensity * (anchor.IsChandelier ? 0.045f : 0.035f);
            specular.range = anchor.IsChandelier ? 9.0f : 7.5f;
            specular.bounceIntensity = 0f;
            specular.shadows = LightShadows.None;
            specular.renderMode = LightRenderMode.ForcePixel;
            specular.lightmapBakeType = LightmapBakeType.Realtime;
        }

        if (createFlicker)
        {
            ConfigureFlicker(pointObject.AddComponent<FlickerLight>(), point.intensity * 0.97f, point.intensity * 1.02f, 0.45f, 0.95f, 0.92f);
        }

        return true;
    }

    private static void CreateCeilingGlowLight(Transform parent, LampAnchor anchor)
    {
        GameObject glowObject = CreateChild(parent, "ceiling glow");
        glowObject.transform.localPosition = Vector3.down * 0.20f;
        glowObject.transform.localRotation = Quaternion.LookRotation(Vector3.up);

        Light glow = glowObject.AddComponent<Light>();
        glow.type = LightType.Spot;
        glow.color = Color.Lerp(anchor.Color, Color.white, 0.54f);
        glow.intensity = anchor.BaseIntensity * (anchor.IsChandelier ? 0.42f : 0.55f);
        glow.range = anchor.IsChandelier ? 6.0f : 6.5f;
        glow.spotAngle = 145f;
        glow.bounceIntensity = 0.0f;
        glow.shadows = LightShadows.None;
        glow.cullingMask = LampLightingMask;
        glow.renderMode = LightRenderMode.ForcePixel;
        glow.lightmapBakeType = LightmapBakeType.Realtime;
    }

    private static void CreateCeilingDiffuserLights(Transform parent, LampAnchor anchor)
    {
        CreateCeilingDiffuserLight(parent, anchor, "ceiling diffuser north", Vector3.forward);
        CreateCeilingDiffuserLight(parent, anchor, "ceiling diffuser south", Vector3.back);
        CreateCeilingDiffuserLight(parent, anchor, "ceiling diffuser east", Vector3.right);
        CreateCeilingDiffuserLight(parent, anchor, "ceiling diffuser west", Vector3.left);
    }

    private static void CreateCeilingDiffuserLight(Transform parent, LampAnchor anchor, string name, Vector3 horizontalOffset)
    {
        GameObject diffuserObject = CreateChild(parent, name);
        diffuserObject.transform.localPosition = horizontalOffset * 1.35f + Vector3.down * 0.14f;

        Light diffuser = diffuserObject.AddComponent<Light>();
        diffuser.type = LightType.Point;
        diffuser.color = Color.Lerp(anchor.Color, Color.white, 0.48f);
        diffuser.intensity = anchor.BaseIntensity * (anchor.IsChandelier ? 0.04f : 0.07f);
        diffuser.range = anchor.IsChandelier ? 24.0f : 30.0f;
        diffuser.bounceIntensity = 0.0f;
        diffuser.shadows = LightShadows.None;
        diffuser.cullingMask = LampLightingMask;
        diffuser.renderMode = LightRenderMode.ForcePixel;
        diffuser.lightmapBakeType = LightmapBakeType.Realtime;
    }

    private static void CreateWallScatterLights(Transform parent, LampAnchor anchor)
    {
        CreateWallScatterLight(parent, anchor, "wall scatter north", Vector3.forward);
        CreateWallScatterLight(parent, anchor, "wall scatter south", Vector3.back);
        CreateWallScatterLight(parent, anchor, "wall scatter east", Vector3.right);
        CreateWallScatterLight(parent, anchor, "wall scatter west", Vector3.left);
    }

    private static void CreateWallScatterLight(Transform parent, LampAnchor anchor, string name, Vector3 horizontalDirection)
    {
        GameObject scatterObject = CreateChild(parent, name);
        scatterObject.transform.localPosition = horizontalDirection * 1.85f + Vector3.down * 0.18f;

        Light scatter = scatterObject.AddComponent<Light>();
        scatter.type = LightType.Point;
        scatter.color = Color.Lerp(anchor.Color, Color.white, 0.50f);
        scatter.intensity = anchor.BaseIntensity * (anchor.IsChandelier ? 0.035f : 0.07f);
        scatter.range = anchor.IsChandelier ? 22.0f : 28.0f;
        scatter.spotAngle = 30f;
        scatter.bounceIntensity = 0.0f;
        scatter.shadows = LightShadows.None;
        scatter.cullingMask = LampLightingMask;
        scatter.renderMode = LightRenderMode.ForcePixel;
        scatter.lightmapBakeType = LightmapBakeType.Realtime;
    }

    private static bool IsLargeStoneRoomAnchor(Vector3 position)
    {
        // Localize the extra brightness to the authored white-stone hall only.
        return position.x >= 4.0f
            && position.x <= 52.0f
            && position.z >= 31.0f
            && position.z <= 109.0f;
    }

    private static void CreateLargeStoneRoomFillLight(Transform parent, LampAnchor anchor)
    {
        GameObject fillObject = CreateChild(parent, "stone room fill");
        fillObject.transform.localPosition = Vector3.down * 0.92f;

        Light fill = fillObject.AddComponent<Light>();
        fill.type = LightType.Point;
        fill.color = Color.Lerp(anchor.Color, Color.white, 0.64f);
        fill.intensity = anchor.BaseIntensity * (anchor.IsChandelier ? 0.055f : 0.050f);
        fill.range = anchor.IsChandelier ? 20.0f : 17.0f;
        fill.bounceIntensity = 0.0f;
        fill.shadows = LightShadows.None;
        fill.cullingMask = LampLightingMask;
        fill.renderMode = LightRenderMode.ForcePixel;
        fill.lightmapBakeType = LightmapBakeType.Realtime;
    }

    private static int CreateLargeLocationBalanceFills(Transform parent)
    {
        GameObject fillGroup = CreateChild(parent, "Large Location Balance Fills");
        for (int i = 0; i < LargeLocationBalanceFills.Length; i++)
        {
            RoomBalanceFill balanceFill = LargeLocationBalanceFills[i];
            GameObject fillObject = CreateChild(fillGroup.transform, $"balance fill {i + 1:00} - {balanceFill.Name}");
            fillObject.transform.position = balanceFill.Position;

            Light fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Point;
            fill.color = Color.Lerp(CoolLamp, Color.white, 0.68f);
            fill.intensity = balanceFill.Intensity;
            fill.range = balanceFill.Range;
            fill.bounceIntensity = 0.0f;
            fill.shadows = LightShadows.None;
            fill.cullingMask = LampLightingMask;
            fill.renderMode = LightRenderMode.ForcePixel;
            fill.lightmapBakeType = LightmapBakeType.Realtime;
        }

        return LargeLocationBalanceFills.Length;
    }

    private static void CreateGeneratedCeilingLampFixture(Transform parent, Material capMaterial)
    {
        GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cap.name = "generated ceiling lamp cap";
        cap.transform.SetParent(parent, false);
        cap.transform.localPosition = Vector3.up * 0.08f;
        cap.transform.localScale = new Vector3(0.46f, 0.045f, 0.46f);

        Renderer capRenderer = cap.GetComponent<Renderer>();
        capRenderer.sharedMaterial = capMaterial;
        capRenderer.receiveShadows = false;
        capRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        capRenderer.shadowCastingMode = ShadowCastingMode.Off;

        Collider capCollider = cap.GetComponent<Collider>();
        if (capCollider != null)
        {
            Object.DestroyImmediate(capCollider);
        }
    }

    private static void CreateCeilingLightSpill(
        Transform parent,
        LampAnchor anchor,
        Material warmCeilingSpillMaterial,
        Material coolCeilingSpillMaterial)
    {
        UpsertCeilingLightSpill(
            parent,
            anchor.IsChandelier,
            anchor.IsChandelier ? warmCeilingSpillMaterial : coolCeilingSpillMaterial);
    }

    private static int EnsureCeilingLightSpills(Material warmCeilingSpillMaterial, Material coolCeilingSpillMaterial)
    {
        int spillCount = 0;
        foreach (Transform transform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
        {
            if (!transform.gameObject.name.StartsWith("Dynamic Lamp ", System.StringComparison.Ordinal))
            {
                continue;
            }

            bool isChandelier = transform.gameObject.name.IndexOf("chandelier", System.StringComparison.OrdinalIgnoreCase) >= 0;
            UpsertCeilingLightSpill(transform, isChandelier, isChandelier ? warmCeilingSpillMaterial : coolCeilingSpillMaterial);
            spillCount++;
        }

        return spillCount;
    }

    private static void UpsertCeilingLightSpill(Transform parent, bool isChandelier, Material material)
    {
        if (material == null)
        {
            return;
        }

        Transform existing = parent.Find("ceiling light spill");
        GameObject spill = existing != null ? existing.gameObject : GameObject.CreatePrimitive(PrimitiveType.Quad);
        spill.name = "ceiling light spill";
        spill.transform.SetParent(parent, false);
        spill.transform.localPosition = Vector3.up * (isChandelier ? 0.36f : 0.075f);
        spill.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        float diameter = isChandelier ? 6.2f : 4.2f;
        spill.transform.localScale = new Vector3(diameter, diameter, 1f);

        Renderer renderer = spill.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        renderer.receiveShadows = false;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        renderer.shadowCastingMode = ShadowCastingMode.Off;

        Collider collider = spill.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        EditorUtility.SetDirty(renderer);
        EditorUtility.SetDirty(spill);
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

        float wide = anchor.IsChandelier ? 6.2f : 4.8f;
        float longAxis = anchor.IsChandelier ? 7.2f : 5.4f;
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
                float width = anchor.IsChandelier ? 4.2f : 3.4f;
                float height = anchor.IsChandelier ? 2.2f : 1.8f;
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

    private static void CreateReflectionProbes(Transform parent, Bounds sceneBounds)
    {
        Vector3 size = sceneBounds.size;
        Vector3 center = sceneBounds.center;
        Vector3 probeSize = new Vector3(Mathf.Max(size.x * 0.72f, 18f), 4.2f, Mathf.Max(size.z * 0.72f, 18f));

        CreateReflectionProbe(parent, "Upper Floor Quality Reflection Probe", new Vector3(center.x, 1.05f, center.z), probeSize);
        CreateReflectionProbe(parent, "Lower Floor Quality Reflection Probe", new Vector3(center.x, -1.95f, center.z), probeSize);
    }

    private static int EnsureGlossReflectionProbeCoverage()
    {
        GameObject root = GameObject.Find(GlossReflectionProbeRootName);
        if (root == null)
        {
            root = new GameObject(GlossReflectionProbeRootName);
        }

        root.transform.position = Vector3.zero;
        root.transform.rotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        Bounds sceneBounds = CalculateSceneBounds(root.transform);
        Vector3 size = sceneBounds.size;
        Vector3 center = sceneBounds.center;
        Vector3 probeSize = new Vector3(
            Mathf.Max(size.x + 4f, 22f),
            4.4f,
            Mathf.Max(size.z + 4f, 22f));

        UpsertGlossReflectionProbe(root.transform, "Upper Floor Gloss Reflection Probe", new Vector3(center.x, 1.05f, center.z), probeSize);
        UpsertGlossReflectionProbe(root.transform, "Lower Floor Gloss Reflection Probe", new Vector3(center.x, -1.95f, center.z), probeSize);

        EditorUtility.SetDirty(root);
        return 2;
    }

    private static void UpsertGlossReflectionProbe(Transform parent, string name, Vector3 position, Vector3 size)
    {
        Transform existing = parent.Find(name);
        GameObject probeObject = existing != null ? existing.gameObject : CreateChild(parent, name);
        probeObject.transform.position = position;
        probeObject.transform.rotation = Quaternion.identity;
        probeObject.transform.localScale = Vector3.one;

        ReflectionProbe probe = probeObject.GetComponent<ReflectionProbe>();
        if (probe == null)
        {
            probe = probeObject.AddComponent<ReflectionProbe>();
        }

        probe.mode = ReflectionProbeMode.Realtime;
        probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
        probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
        probe.resolution = QualityReflectionProbeResolution;
        probe.intensity = 1.15f;
        probe.boxProjection = true;
        probe.size = size;
        probe.center = Vector3.zero;
        probe.shadowDistance = 42f;
        probe.renderDynamicObjects = false;

        EditorUtility.SetDirty(probe);
        EditorUtility.SetDirty(probeObject);
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
        probe.intensity = 1.15f;
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
            probe.intensity = Mathf.Max(probe.intensity, 1.15f);
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
            if (generatedRoot != null && renderer.transform.IsChildOf(generatedRoot))
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
        SetColor(material, "_Color", new Color(1.0f, 0.88f, 0.68f, 1f));
        SetEmission(material, CoolLamp * 8.5f);
        SetFloat(material, "_Glossiness", 0.62f);
        SetFloat(material, "_Metallic", 0.0f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material CreateGeneratedLampCapMaterial()
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Lit");
        }

        Material material = LoadOrCreateMaterial(GeneratedLampCapMaterialPath, shader);
        SetColor(material, "_Color", new Color(0.05f, 0.20f, 0.13f, 1f));
        SetFloat(material, "_Metallic", 0.12f);
        SetFloat(material, "_Glossiness", 0.44f);
        SetFloat(material, "_GlossMapScale", 0.44f);
        material.DisableKeyword("_EMISSION");
        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
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
        SetFloat(material, "_Mode", 2f);
        SetFloat(material, "_SrcBlend", (float)BlendMode.SrcAlpha);
        SetFloat(material, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        SetFloat(material, "_ZWrite", 0f);
        SetFloat(material, "_Glossiness", 0f);
        SetFloat(material, "_GlossMapScale", 0f);
        SetFloat(material, "_Metallic", 0f);
        SetFloat(material, "_GlossyReflections", 0f);
        SetFloat(material, "_SpecularHighlights", 0f);
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
        SetTexture(material, "_EmissionMap", null);
        SetColor(material, "_Color", tint);
        SetColor(material, "_EmissionColor", Color.black);
        SetFloat(material, "_Mode", 2f);
        SetFloat(material, "_SrcBlend", (float)BlendMode.SrcAlpha);
        SetFloat(material, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        SetFloat(material, "_ZWrite", 0f);
        SetFloat(material, "_Glossiness", floorReflection ? 0.12f : 0.08f);
        SetFloat(material, "_GlossMapScale", 0f);
        SetFloat(material, "_Metallic", 0f);
        SetFloat(material, "_GlossyReflections", 0f);
        SetFloat(material, "_SpecularHighlights", 0f);
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_EMISSION");
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = (int)RenderQueue.Transparent;
        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Texture2D LoadOrCreateCeilingSpillTexture()
    {
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
                falloff = Mathf.SmoothStep(0f, 1f, falloff);
                float alpha = Mathf.Pow(falloff, 1.35f);
                pixels[y * size + x] = new Color(alpha, alpha, alpha, alpha);
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
            importer.textureCompression = TextureImporterCompression.Uncompressed;
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
                float radius = Mathf.Sqrt(nx * nx + ny * ny);
                float radial = Mathf.Clamp01(1f - radius);
                radial = Mathf.SmoothStep(0f, 1f, radial);
                radial = Mathf.SmoothStep(0f, 1f, radial);
                float borderDistance = 1f - Mathf.Max(Mathf.Abs(nx), Mathf.Abs(ny));
                float edgeFade = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(borderDistance / 0.28f));

                float broadReflection = floorReflection ? 0.30f : 0.26f;
                float falloffPower = floorReflection ? 2.10f : 2.35f;
                float alpha = Mathf.Clamp01(Mathf.Pow(radial, falloffPower) * broadReflection) * edgeFade;
                pixels[y * size + x] = new Color(alpha, alpha, alpha, alpha);
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
            importer.textureCompression = TextureImporterCompression.Uncompressed;
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

    private static void SetMaterialSurface(
        string materialName,
        float metallic,
        float smoothness,
        float glossMapScale,
        float glossyReflections,
        float specularHighlights)
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
        SetFloat(material, "_GlossyReflections", glossyReflections);
        SetFloat(material, "_SpecularHighlights", specularHighlights);
        ConfigureStandardReflectionKeywords(material, glossyReflections, specularHighlights);
        EditorUtility.SetDirty(material);
    }

    private static void ConfigureStandardReflectionKeywords(Material material, float glossyReflections, float specularHighlights)
    {
        if (glossyReflections <= 0f)
        {
            material.EnableKeyword("_GLOSSYREFLECTIONS_OFF");
        }
        else
        {
            material.DisableKeyword("_GLOSSYREFLECTIONS_OFF");
        }

        if (specularHighlights <= 0f)
        {
            material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
        }
        else
        {
            material.DisableKeyword("_SPECULARHIGHLIGHTS_OFF");
        }
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
        ConfigureRuntimeLightingSettings(settings, MaxRealtimeLampLights * 13, 0, false, true, false, RenderingPath.DeferredShading);
        settings.Apply();

        QualitySettings.pixelLightCount = MaxRealtimeLampLights * 13;
        QualitySettings.realtimeReflectionProbes = true;
        QualitySettings.shadows = ShadowQuality.Disable;
        QualitySettings.shadowResolution = ShadowResolution.High;
        QualitySettings.shadowProjection = ShadowProjection.StableFit;
        QualitySettings.shadowDistance = 0f;
        QualitySettings.shadowNearPlaneOffset = 2f;
        QualitySettings.antiAliasing = QualityAntiAliasingSamples;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;

        foreach (Camera camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude))
        {
            camera.allowHDR = true;
            camera.allowMSAA = false;
            camera.useOcclusionCulling = true;
            camera.renderingPath = RenderingPath.DeferredShading;
            EnsureDeferredPostAntialiasing(camera, true);
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
        bool disableCameraHdr,
        RenderingPath cameraRenderingPath = RenderingPath.Forward)
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
        SetSerializedBool(serialized, "enableCameraMsaa", cameraRenderingPath != RenderingPath.DeferredShading);
        SetSerializedBool(serialized, "disableCameraHdr", disableCameraHdr);
        SetSerializedInt(serialized, "cameraRenderingPath", (int)cameraRenderingPath);
        SetSerializedBool(serialized, "enableDeferredPostAntialiasing", cameraRenderingPath == RenderingPath.DeferredShading);
        SetSerializedFloat(serialized, "postAntialiasingSubpixelBlending", 0.72f);
        SetSerializedFloat(serialized, "postAntialiasingEdgeThreshold", 0.11f);
        SetSerializedFloat(serialized, "postAntialiasingEdgeThresholdMin", 0.0312f);
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

    private static void SetSerializedFloat(SerializedObject serialized, string propertyName, float value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
        {
            property.floatValue = value;
        }
    }

    private static void EnsureDeferredPostAntialiasing(Camera camera, bool enabled)
    {
        WolfDeferredPostAntiAliasing postAntialiasing = camera.GetComponent<WolfDeferredPostAntiAliasing>();
        if (!enabled)
        {
            if (postAntialiasing != null)
            {
                postAntialiasing.enabled = false;
                EditorUtility.SetDirty(postAntialiasing);
            }

            return;
        }

        if (postAntialiasing == null)
        {
            postAntialiasing = camera.gameObject.AddComponent<WolfDeferredPostAntiAliasing>();
        }

        postAntialiasing.Configure(0.72f, 0.11f, 0.0312f);
        postAntialiasing.enabled = true;
        EditorUtility.SetDirty(postAntialiasing);
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

    private readonly struct BeautifulLightingSummary
    {
        public BeautifulLightingSummary(int staticRenderers, int bakedLights, int lightProbes)
        {
            StaticRenderers = staticRenderers;
            BakedLights = bakedLights;
            LightProbes = lightProbes;
        }

        private int StaticRenderers { get; }
        private int BakedLights { get; }
        private int LightProbes { get; }

        public override string ToString()
        {
            return $"staticRenderers={StaticRenderers}, bakedLights={BakedLights}, lightProbes={LightProbes}";
        }
    }

    private enum SurfaceTarget
    {
        Floor,
        Wall
    }

    private readonly struct LampAnchor
    {
        public LampAnchor(string name, Vector3 position, Color color, float baseIntensity, float range, bool isChandelier, bool createFixtureMesh = false)
        {
            Name = name;
            Position = position;
            Color = color;
            BaseIntensity = baseIntensity;
            Range = range;
            IsChandelier = isChandelier;
            CreateFixtureMesh = createFixtureMesh;
        }

        public string Name { get; }
        public Vector3 Position { get; }
        public Color Color { get; }
        public float BaseIntensity { get; }
        public float Range { get; }
        public bool IsChandelier { get; }
        public bool CreateFixtureMesh { get; }
    }

    private readonly struct RoomBalanceFill
    {
        public RoomBalanceFill(string name, Vector3 position, float intensity, float range)
        {
            Name = name;
            Position = position;
            Intensity = intensity;
            Range = range;
        }

        public string Name { get; }
        public Vector3 Position { get; }
        public float Intensity { get; }
        public float Range { get; }
    }
}
#endif
