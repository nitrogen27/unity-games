using System.IO;
using UnityEditor;
using UnityEngine;
using WolfMini.Core;
using WolfMini.Level;

namespace WolfMini.EditorTools
{
    /// <summary>
    /// Temporary check tool: rebuilds the level and renders the atrium hall
    /// decor walls (banner / portrait / eagle panels) to Docs/Verification.
    /// </summary>
    public static class WolfAtriumDecorCheckCapture
    {
        private const string OutputFolder = "Docs/Verification";
        private const int Width = 1280;
        private const int Height = 800;

        /// <summary>
        /// Renders the already generated level three times (probes disabled /
        /// probes without cubemaps / probes intact) without rebuilding, to
        /// isolate what turns edit-mode offscreen renders black.
        /// </summary>
        [MenuItem("Tools/Wolf Full3D/Debug Probe Render Isolation")]
        public static void DebugProbeIsolation()
        {
            WolfSectorMeshBuilder builder = Object.FindFirstObjectByType<WolfSectorMeshBuilder>();
            if (builder == null)
            {
                Debug.LogError("[WolfAtriumDecor] No WolfSectorMeshBuilder in the open scene.");
                return;
            }

            builder.Build();
            Directory.CreateDirectory(OutputFolder);
            UnityEngine.ReflectionProbe[] probes = Object.FindObjectsByType<UnityEngine.ReflectionProbe>(FindObjectsSortMode.None);
            Debug.Log($"[WolfAtriumDecor] Probe isolation over {probes.Length} probes.");

            GameObject rig = new GameObject("Wolf Debug Capture Camera");
            try
            {
                Camera camera = rig.AddComponent<Camera>();
                camera.fieldOfView = 58f;
                camera.nearClipPlane = 0.05f;
                camera.farClipPlane = 260f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color32(56, 56, 56, 255);

                foreach (UnityEngine.ReflectionProbe probe in probes)
                {
                    probe.enabled = false;
                }

                Shoot(camera, rig, new Vector3(124.2f, WolfMiniConstants.EyeHeight + 4.65f, 118f), new Vector3(108f, 3f, 99f), "debug_noprobes");

                var textures = new Texture[probes.Length];
                for (int i = 0; i < probes.Length; i++)
                {
                    textures[i] = probes[i].customBakedTexture;
                    probes[i].customBakedTexture = null;
                    probes[i].enabled = true;
                }

                Shoot(camera, rig, new Vector3(124.2f, WolfMiniConstants.EyeHeight + 4.65f, 118f), new Vector3(108f, 3f, 99f), "debug_probes_notex");

                for (int i = 0; i < probes.Length; i++)
                {
                    probes[i].customBakedTexture = textures[i];
                }

                Shoot(camera, rig, new Vector3(124.2f, WolfMiniConstants.EyeHeight + 4.65f, 118f), new Vector3(108f, 3f, 99f), "debug_probes");
                Debug.Log("[WolfAtriumDecor] Probe isolation captures saved.");

                DumpProbeCubemaps(probes);
            }
            finally
            {
                Object.DestroyImmediate(rig);
            }
        }

        private static void DumpProbeCubemaps(UnityEngine.ReflectionProbe[] probes)
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine($"probes: {probes.Length}");

            WolfSectorMeshBuilder builder = Object.FindFirstObjectByType<WolfSectorMeshBuilder>();
            if (builder != null)
            {
                SerializedProperty flag = new SerializedObject(builder).FindProperty("buildReflectionProbes");
                report.AppendLine($"buildReflectionProbes: {(flag != null ? flag.boolValue.ToString() : "field missing")}");
            }

            GameObject geometry = GameObject.Find("Level Geometry");
            if (geometry != null && geometry.TryGetComponent(out MeshRenderer meshRenderer))
            {
                report.AppendLine($"level geometry probe usage: {meshRenderer.reflectionProbeUsage}");
            }

            int dumped = 0;
            foreach (UnityEngine.ReflectionProbe probe in probes)
            {
                var cubemap = probe.customBakedTexture as Cubemap;
                report.AppendLine($"{probe.name}: enabled={probe.enabled}, mode={probe.mode}, texture={(probe.customBakedTexture != null ? probe.customBakedTexture.name : "none")}, importance={probe.importance}, size={probe.size}, pos={probe.transform.position}");
                if (cubemap == null || dumped >= 2)
                {
                    continue;
                }

                var face = new Texture2D(cubemap.width, cubemap.width, TextureFormat.RGBA32, false);
                face.SetPixels(cubemap.GetPixels(CubemapFace.PositiveZ));
                face.Apply();
                File.WriteAllBytes($"{OutputFolder}/debug_cubemap_{dumped:0}.png", face.EncodeToPNG());
                Object.DestroyImmediate(face);
                dumped++;
            }

            report.AppendLine($"dumped cubemap faces: {dumped}");
            File.WriteAllText($"{OutputFolder}/debug_probes.txt", report.ToString());
            Debug.Log($"[WolfAtriumDecor] Dumped {dumped} probe cubemap faces.");
        }

        [MenuItem("Tools/Wolf Full3D/Capture Atrium Decor Check")]
        public static void Capture()
        {
            WolfSectorMeshBuilder builder = Object.FindFirstObjectByType<WolfSectorMeshBuilder>();
            if (builder == null)
            {
                Debug.LogError("[WolfAtriumDecor] No WolfSectorMeshBuilder in the open scene.");
                return;
            }

            builder.Build();
            Directory.CreateDirectory(OutputFolder);

            GameObject rig = new GameObject("Wolf Atrium Decor Camera");
            try
            {
                Camera camera = rig.AddComponent<Camera>();
                camera.fieldOfView = 58f;
                camera.nearClipPlane = 0.05f;
                camera.farClipPlane = 260f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color32(56, 56, 56, 255);
                // Offscreen deferred/HDR renders come out black in edit mode,
                // so captures stay on the default forward LDR path; probes are
                // still box-projected per pixel, only probe selection is per
                // renderer here.

                float eye = WolfMiniConstants.EyeHeight;
                // Throwaway frame: reflection-probe bindings for the freshly
                // generated renderers are computed during the first render, so
                // the real shots below actually show the probes.
                rig.transform.position = new Vector3(124.2f, eye + 4.65f, 118f);
                RenderToFile(camera, $"{OutputFolder}/warmup.png");
                File.Delete($"{OutputFolder}/warmup.png");

                Shoot(camera, rig, new Vector3(109.8f, eye, 112f), new Vector3(109.8f, 1.8f, 122.4f), "atrium_banner");
                Shoot(camera, rig, new Vector3(102.6f, eye, 108f), new Vector3(102.6f, 1.8f, 97.2f), "atrium_portrait");
                Shoot(camera, rig, new Vector3(140f, eye, 102.6f), new Vector3(151.2f, 1.8f, 102.6f), "atrium_eagle");
                Shoot(camera, rig, new Vector3(124.2f, eye + 4.65f, 118f), new Vector3(108f, 3f, 99f), "atrium_wide");
                // Chandelier hangs over the void center (126, 109.8): one shot
                // at top-gallery eye level, one from the atrium floor below.
                Shoot(camera, rig, new Vector3(123f, eye + 4.65f, 117.4f), new Vector3(126f, 6.1f, 109.8f), "atrium_chandelier");
                Shoot(camera, rig, new Vector3(119.5f, eye - 4.65f, 103.5f), new Vector3(126f, 5.6f, 109.8f), "atrium_chandelier_low");

                Debug.Log($"[WolfAtriumDecor] Saved decor check screens to {OutputFolder}/atrium_*.png");
            }
            finally
            {
                Object.DestroyImmediate(rig);
            }
        }

        private static void Shoot(Camera camera, GameObject rig, Vector3 position, Vector3 lookAt, string name)
        {
            rig.transform.position = position;
            rig.transform.rotation = Quaternion.LookRotation(lookAt - position, Vector3.up);
            RenderToFile(camera, $"{OutputFolder}/{name}.png");
        }

        private static void RenderToFile(Camera camera, string path)
        {
            var renderTexture = new RenderTexture(Width, Height, 24);
            var texture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();

                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = null;
                Object.DestroyImmediate(texture);
                renderTexture.Release();
                Object.DestroyImmediate(renderTexture);
            }
        }
    }
}
