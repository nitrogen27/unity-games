#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

public static class ScenePerformanceAudit
{
    private const int RuntimeSampleFrames = 180;

    private static readonly CounterSpec[] CounterSpecs =
    {
        new CounterSpec(ProfilerCategory.Render, "Draw Calls Count", false),
        new CounterSpec(ProfilerCategory.Render, "Batches Count", false),
        new CounterSpec(ProfilerCategory.Render, "SetPass Calls Count", false),
        new CounterSpec(ProfilerCategory.Render, "Triangles Count", false),
        new CounterSpec(ProfilerCategory.Render, "Vertices Count", false),
        new CounterSpec(ProfilerCategory.Render, "Shadow Casters Count", false),
        new CounterSpec(ProfilerCategory.Internal, "Main Thread", true),
        new CounterSpec(ProfilerCategory.Internal, "Render Thread", true),
        new CounterSpec(ProfilerCategory.Internal, "Gfx.WaitForPresentOnGfxThread", true),
        new CounterSpec(ProfilerCategory.Scripts, "BehaviourUpdate", true),
        new CounterSpec(ProfilerCategory.Render, "Camera.Render", true),
        new CounterSpec(ProfilerCategory.Render, "RenderLoop.Draw", true)
    };

    private static readonly List<RecorderState> ActiveRecorders = new List<RecorderState>();
    private static int sampleTicks;

    [MenuItem("Tools/Performance/Audit Active Scene")]
    public static void AuditActiveScene()
    {
        LogStaticAudit();

        if (!Application.isPlaying)
        {
            Debug.Log("[PerfAudit] Runtime counters skipped because the editor is not in Play Mode.");
            return;
        }

        StartRuntimeCounters();
    }

    [MenuItem("Tools/Performance/Capture Main Camera Frame")]
    public static void CaptureMainCameraFrame()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            camera = Resources.FindObjectsOfTypeAll<Camera>()
                .FirstOrDefault(candidate => candidate.gameObject.scene == SceneManager.GetActiveScene() && candidate.isActiveAndEnabled);
        }

        if (camera == null)
        {
            Debug.LogWarning("[PerfAudit] No active camera found to capture.");
            return;
        }

        int width = Mathf.Clamp(camera.pixelWidth > 0 ? camera.pixelWidth : 1280, 320, 1920);
        int height = Mathf.Clamp(camera.pixelHeight > 0 ? camera.pixelHeight : 720, 180, 1080);
        RenderTextureFormat format = camera.allowHDR ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32;
        RenderTexture target = RenderTexture.GetTemporary(width, height, 24, format, RenderTextureReadWrite.Default);
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture previousTarget = camera.targetTexture;

        Texture2D image = null;
        try
        {
            camera.targetTexture = target;
            camera.Render();

            RenderTexture.active = target;
            image = new Texture2D(width, height, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply(false, false);

            string capturePath = Path.GetFullPath(Path.Combine("Library", "WolfMainCameraCapture.png"));
            File.WriteAllBytes(capturePath, image.EncodeToPNG());
            Debug.Log(BuildCameraCaptureSummary(camera, image, capturePath));
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(target);
            if (image != null)
            {
                UnityEngine.Object.DestroyImmediate(image);
            }
        }
    }

    private static void LogStaticAudit()
    {
        Scene scene = SceneManager.GetActiveScene();
        GameObject[] roots = scene.GetRootGameObjects();
        GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>()
            .Where(go => go.scene == scene)
            .ToArray();
        Renderer[] renderers = Resources.FindObjectsOfTypeAll<Renderer>()
            .Where(renderer => renderer.gameObject.scene == scene)
            .ToArray();
        MeshFilter[] meshFilters = Resources.FindObjectsOfTypeAll<MeshFilter>()
            .Where(filter => filter.gameObject.scene == scene)
            .ToArray();
        Collider[] colliders = Resources.FindObjectsOfTypeAll<Collider>()
            .Where(collider => collider.gameObject.scene == scene)
            .ToArray();
        Light[] lights = Resources.FindObjectsOfTypeAll<Light>()
            .Where(light => light.gameObject.scene == scene)
            .ToArray();
        ReflectionProbe[] probes = Resources.FindObjectsOfTypeAll<ReflectionProbe>()
            .Where(probe => probe.gameObject.scene == scene)
            .ToArray();
        Camera[] cameras = Resources.FindObjectsOfTypeAll<Camera>()
            .Where(camera => camera.gameObject.scene == scene)
            .ToArray();

        var materialCounts = new Dictionary<Material, int>();
        var shaderCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        int materialSlots = 0;
        int instancedSlots = 0;
        foreach (Renderer renderer in renderers)
        {
            foreach (Material material in renderer.sharedMaterials)
            {
                if (material == null)
                {
                    continue;
                }

                materialSlots++;
                if (material.enableInstancing)
                {
                    instancedSlots++;
                }

                materialCounts.TryGetValue(material, out int count);
                materialCounts[material] = count + 1;

                string shaderName = material.shader != null ? material.shader.name : "<missing shader>";
                shaderCounts.TryGetValue(shaderName, out int shaderCount);
                shaderCounts[shaderName] = shaderCount + 1;
            }
        }

        var uniqueMeshes = new HashSet<Mesh>();
        long uniqueVertices = 0;
        long uniqueTriangles = 0;
        long submittedVertices = 0;
        long submittedTriangles = 0;
        foreach (MeshFilter filter in meshFilters)
        {
            Mesh mesh = filter.sharedMesh;
            if (mesh == null)
            {
                continue;
            }

            submittedVertices += mesh.vertexCount;
            submittedTriangles += SumTriangles(mesh);
            if (uniqueMeshes.Add(mesh))
            {
                uniqueVertices += mesh.vertexCount;
                uniqueTriangles += SumTriangles(mesh);
            }
        }

        int staticObjects = 0;
        foreach (GameObject go in objects)
        {
            if (GameObjectUtility.GetStaticEditorFlags(go) != 0)
            {
                staticObjects++;
            }
        }

        var rootSummaries = roots
            .Select(root => new RootSummary(root))
            .OrderByDescending(summary => summary.Renderers)
            .ThenByDescending(summary => summary.GameObjects)
            .Take(12)
            .ToArray();

        var lightSummary = lights
            .GroupBy(light => $"{light.type}/{light.lightmapBakeType}/{light.shadows}")
            .OrderByDescending(group => group.Count())
            .Select(group => $"{group.Key}: {group.Count()}");

        var rendererShadowSummary = renderers
            .GroupBy(renderer => $"{renderer.shadowCastingMode}/receive:{renderer.receiveShadows}")
            .OrderByDescending(group => group.Count())
            .Select(group => $"{group.Key}: {group.Count()}");

        var colliderSummary = colliders
            .GroupBy(collider => collider.GetType().Name)
            .OrderByDescending(group => group.Count())
            .Select(group => $"{group.Key}: {group.Count()}");

        var cameraSummary = cameras.Select(camera =>
            $"{camera.name}: path={camera.actualRenderingPath}, setting={camera.renderingPath}, occlusion={camera.useOcclusionCulling}, hdr={camera.allowHDR}, msaa={camera.allowMSAA}, far={camera.farClipPlane}");

        var probeSummary = probes
            .GroupBy(probe => $"{probe.mode}/{probe.refreshMode}/res:{probe.resolution}")
            .OrderByDescending(group => group.Count())
            .Select(group => $"{group.Key}: {group.Count()}");

        StringBuilder sb = new StringBuilder(4096);
        sb.AppendLine("[PerfAudit] Static scene audit");
        sb.AppendLine($"Scene: {scene.name} ({scene.path})");
        sb.AppendLine($"Quality: {QualitySettings.names[QualitySettings.GetQualityLevel()]} index={QualitySettings.GetQualityLevel()} pixelLights={QualitySettings.pixelLightCount} shadows={QualitySettings.shadows} shadowDistance={QualitySettings.shadowDistance} msaa={QualitySettings.antiAliasing} realtimeReflectionProbes={QualitySettings.realtimeReflectionProbes} vSync={QualitySettings.vSyncCount}");
        sb.AppendLine($"Render: colorSpace={QualitySettings.activeColorSpace} graphicsDevice={SystemInfo.graphicsDeviceName} api={SystemInfo.graphicsDeviceType} multithreaded={SystemInfo.graphicsMultiThreaded}");
        sb.AppendLine($"Counts: roots={roots.Length}, gameObjects={objects.Length}, staticObjects={staticObjects}, renderers={renderers.Length}, meshRenderers={renderers.OfType<MeshRenderer>().Count()}, skinnedRenderers={renderers.OfType<SkinnedMeshRenderer>().Count()}, meshFilters={meshFilters.Length}, colliders={colliders.Length}, lights={lights.Length}, reflectionProbes={probes.Length}, cameras={cameras.Length}");
        sb.AppendLine($"Materials: slots={materialSlots}, unique={materialCounts.Count}, instancingEnabledSlots={instancedSlots}");
        sb.AppendLine($"Meshes: unique={uniqueMeshes.Count}, uniqueVerts={uniqueVertices:N0}, uniqueTris={uniqueTriangles:N0}, submittedVerts={submittedVertices:N0}, submittedTris={submittedTriangles:N0}");
        sb.AppendLine("Root hot spots:");
        foreach (RootSummary summary in rootSummaries)
        {
            sb.AppendLine($"  {summary.Name}: objects={summary.GameObjects}, renderers={summary.Renderers}, lights={summary.Lights}, colliders={summary.Colliders}");
        }

        AppendSection(sb, "Lights by type/bake/shadows", lightSummary.Take(20));
        AppendSection(sb, "Renderer shadow modes", rendererShadowSummary.Take(20));
        AppendSection(sb, "Colliders", colliderSummary.Take(20));
        AppendSection(sb, "Cameras", cameraSummary.Take(20));
        AppendSection(sb, "Reflection probes", probeSummary.Take(20));
        AppendSection(sb, "Top shaders", shaderCounts.OrderByDescending(pair => pair.Value).Take(20).Select(pair => $"{pair.Key}: {pair.Value}"));
        AppendSection(sb, "Top materials", materialCounts.OrderByDescending(pair => pair.Value).Take(20).Select(pair => $"{pair.Key.name}: {pair.Value} ({(pair.Key.shader != null ? pair.Key.shader.name : "<missing shader>")})"));
        AppendUnityStats(sb);

        Debug.Log(sb.ToString());
    }

    private static void StartRuntimeCounters()
    {
        StopRuntimeCounters();
        foreach (CounterSpec spec in CounterSpecs)
        {
            try
            {
                ProfilerRecorder recorder = ProfilerRecorder.StartNew(spec.Category, spec.Name, RuntimeSampleFrames);
                if (recorder.Valid)
                {
                    ActiveRecorders.Add(new RecorderState(spec, recorder));
                }
                else
                {
                    recorder.Dispose();
                }
            }
            catch (Exception)
            {
                // Some counters are version or platform dependent.
            }
        }

        sampleTicks = 0;
        EditorApplication.update += RuntimeCounterTick;
        Debug.Log($"[PerfAudit] Runtime counters started for {RuntimeSampleFrames} frames. Valid counters: {ActiveRecorders.Count}");
    }

    private static void RuntimeCounterTick()
    {
        if (!Application.isPlaying)
        {
            StopRuntimeCounters();
            return;
        }

        foreach (RecorderState state in ActiveRecorders)
        {
            if (!state.Recorder.Valid)
            {
                continue;
            }

            long value = state.Recorder.LastValue;
            state.Add(value);
        }

        sampleTicks++;
        if (sampleTicks < RuntimeSampleFrames)
        {
            return;
        }

        StringBuilder sb = new StringBuilder(2048);
        sb.AppendLine($"[PerfAudit] Runtime profiler counters over {RuntimeSampleFrames} frames");
        foreach (RecorderState state in ActiveRecorders.OrderBy(state => state.Spec.Name))
        {
            sb.AppendLine($"  {state.Format()}");
        }

        Debug.Log(sb.ToString());
        StopRuntimeCounters();
    }

    private static void StopRuntimeCounters()
    {
        EditorApplication.update -= RuntimeCounterTick;
        foreach (RecorderState state in ActiveRecorders)
        {
            if (state.Recorder.Valid)
            {
                state.Recorder.Dispose();
            }
        }

        ActiveRecorders.Clear();
    }

    private static long SumTriangles(Mesh mesh)
    {
        long total = 0;
        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            total += mesh.GetIndexCount(i) / 3;
        }

        return total;
    }

    private static void AppendSection(StringBuilder sb, string title, IEnumerable<string> rows)
    {
        sb.AppendLine($"{title}:");
        bool wroteAny = false;
        foreach (string row in rows)
        {
            wroteAny = true;
            sb.AppendLine($"  {row}");
        }

        if (!wroteAny)
        {
            sb.AppendLine("  <none>");
        }
    }

    private static void AppendUnityStats(StringBuilder sb)
    {
        Type unityStatsType = typeof(Editor).Assembly.GetType("UnityEditor.UnityStats");
        if (unityStatsType == null)
        {
            return;
        }

        string[] names =
        {
            "screenRes",
            "drawCalls",
            "batches",
            "setPassCalls",
            "dynamicBatchedDrawCalls",
            "staticBatchedDrawCalls",
            "instancedBatchedDrawCalls",
            "triangles",
            "vertices",
            "shadowCasters",
            "renderTextureChanges",
            "usedTextureMemorySize",
            "renderTextureCount",
            "renderTextureBytes",
            "vboTotal",
            "vboTotalBytes",
            "ibTotal",
            "ibTotalBytes"
        };

        sb.AppendLine("UnityStats:");
        bool wroteAny = false;
        foreach (string name in names)
        {
            object value = ReadUnityStatsValue(unityStatsType, name);
            if (value == null)
            {
                continue;
            }

            wroteAny = true;
            sb.AppendLine($"  {name}: {value}");
        }

        if (!wroteAny)
        {
            sb.AppendLine("  <unavailable>");
        }
    }

    private static object ReadUnityStatsValue(Type unityStatsType, string name)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        PropertyInfo property = unityStatsType.GetProperty(name, flags);
        if (property != null)
        {
            return property.GetValue(null);
        }

        FieldInfo field = unityStatsType.GetField(name, flags);
        return field != null ? field.GetValue(null) : null;
    }

    private static string BuildCameraCaptureSummary(Camera camera, Texture2D image, string capturePath)
    {
        Color background = camera.backgroundColor;
        Vector3 colorSum = Vector3.zero;
        Vector3 colorSqSum = Vector3.zero;
        int nearBackground = 0;
        int nearGray = 0;
        int samples = 0;
        int strideX = Mathf.Max(1, image.width / 160);
        int strideY = Mathf.Max(1, image.height / 90);

        for (int y = 0; y < image.height; y += strideY)
        {
            for (int x = 0; x < image.width; x += strideX)
            {
                Color color = image.GetPixel(x, y);
                Vector3 rgb = new Vector3(color.r, color.g, color.b);
                colorSum += rgb;
                colorSqSum += Vector3.Scale(rgb, rgb);
                samples++;

                if (Mathf.Abs(color.r - color.g) < 0.015f && Mathf.Abs(color.g - color.b) < 0.015f)
                {
                    nearGray++;
                }

                float backgroundDelta =
                    Mathf.Abs(color.r - background.r) +
                    Mathf.Abs(color.g - background.g) +
                    Mathf.Abs(color.b - background.b);
                if (backgroundDelta < 0.05f)
                {
                    nearBackground++;
                }
            }
        }

        Vector3 average = colorSum / Mathf.Max(1, samples);
        Vector3 variance = (colorSqSum / Mathf.Max(1, samples)) - Vector3.Scale(average, average);
        float standardDeviation = Mathf.Sqrt(Mathf.Max(0f, (variance.x + variance.y + variance.z) / 3f));
        float backgroundPercent = 100f * nearBackground / Mathf.Max(1, samples);
        float grayPercent = 100f * nearGray / Mathf.Max(1, samples);

        return
            "[PerfAudit] Main camera frame capture\n" +
            $"  path={capturePath}\n" +
            $"  size={image.width}x{image.height} samples={samples}\n" +
            $"  camera={camera.name} position={camera.transform.position} forward={camera.transform.forward} path={camera.actualRenderingPath} occlusion={camera.useOcclusionCulling} hdr={camera.allowHDR}\n" +
            $"  avgRgb=({average.x:F3}, {average.y:F3}, {average.z:F3}) stdDev={standardDeviation:F4} nearGray={grayPercent:F1}% nearBackground={backgroundPercent:F1}%";
    }

    private readonly struct CounterSpec
    {
        public CounterSpec(ProfilerCategory category, string name, bool timeMetric)
        {
            Category = category;
            Name = name;
            TimeMetric = timeMetric;
        }

        public readonly ProfilerCategory Category;
        public readonly string Name;
        public readonly bool TimeMetric;
    }

    private sealed class RecorderState
    {
        public RecorderState(CounterSpec spec, ProfilerRecorder recorder)
        {
            Spec = spec;
            Recorder = recorder;
            Min = long.MaxValue;
        }

        public CounterSpec Spec { get; }
        public ProfilerRecorder Recorder { get; }
        private long Sum { get; set; }
        private long Min { get; set; }
        private long Max { get; set; }
        private int Count { get; set; }

        public void Add(long value)
        {
            Count++;
            Sum += value;
            Min = Math.Min(Min, value);
            Max = Math.Max(Max, value);
        }

        public string Format()
        {
            if (Count == 0)
            {
                return $"{Spec.Name}: no samples";
            }

            double average = Sum / (double)Count;
            if (Spec.TimeMetric)
            {
                return $"{Spec.Name}: avg={average / 1000000.0:F3}ms min={Min / 1000000.0:F3}ms max={Max / 1000000.0:F3}ms samples={Count}";
            }

            return $"{Spec.Name}: avg={average:N0} min={Min:N0} max={Max:N0} samples={Count}";
        }
    }

    private sealed class RootSummary
    {
        public RootSummary(GameObject root)
        {
            Name = root.name;
            GameObjects = root.GetComponentsInChildren<Transform>(true).Length;
            Renderers = root.GetComponentsInChildren<Renderer>(true).Length;
            Lights = root.GetComponentsInChildren<Light>(true).Length;
            Colliders = root.GetComponentsInChildren<Collider>(true).Length;
        }

        public string Name { get; }
        public int GameObjects { get; }
        public int Renderers { get; }
        public int Lights { get; }
        public int Colliders { get; }
    }
}
#endif
