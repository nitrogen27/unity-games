using System.IO;
using UnityEditor;
using UnityEngine;
using WolfMini.Core;

namespace WolfMini.EditorTools
{
    /// <summary>
    /// Temporary check tool: enters Play Mode, lets the level build and the
    /// realtime reflection probes render, saves offscreen camera renders to
    /// Docs/Verification and exits Play Mode again.
    /// </summary>
    [InitializeOnLoad]
    public static class WolfPlayModeLightingCheck
    {
        private const string PendingFlag = "WolfPlayModeLightingCheck.Pending";
        private const string PendingStamp = "WolfPlayModeLightingCheck.Stamp";
        private const string OutputFolder = "Docs/Verification";
        private const string LegacyOutputPath = OutputFolder + "/playmode_lighting.png";
        private const string AtriumOutputPath = OutputFolder + "/playmode_lighting_atrium.png";
        private const string CorridorOutputPath = OutputFolder + "/playmode_lighting_corridor.png";
        private const int CaptureWidth = 1280;
        private const int CaptureHeight = 800;
        private const int CaptureFrame = 150;
        private const int ExitFrame = 170;
        // A stale request must never hijack a manual play session later on.
        private const float PendingValiditySeconds = 120f;

        private static readonly Vector3 AtriumCameraPosition = new Vector3(124.2f, WolfMiniConstants.EyeHeight + 4.65f, 118f);
        private static readonly Vector3 AtriumLookAt = new Vector3(108f, 3f, 99f);
        private static readonly Vector3 CorridorCameraPosition = new Vector3(102.6f, WolfMiniConstants.EyeHeight, 9.0f);
        private static readonly Vector3 CorridorLookAt = new Vector3(145.0f, WolfMiniConstants.EyeHeight + 0.80f, 9.0f);

        private static int frames;

        static WolfPlayModeLightingCheck()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [MenuItem("Tools/Wolf Full3D/Play Mode Lighting Check")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[WolfPlayCheck] Editor is already in (or entering) Play Mode; not starting the check.");
                return;
            }

            SessionState.SetBool(PendingFlag, true);
            SessionState.SetFloat(PendingStamp, (float)EditorApplication.timeSinceStartup);
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(PendingFlag, false))
            {
                return;
            }

            float stamp = SessionState.GetFloat(PendingStamp, -10000f);
            if ((float)EditorApplication.timeSinceStartup - stamp > PendingValiditySeconds)
            {
                SessionState.SetBool(PendingFlag, false);
                return;
            }

            frames = 0;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (!Application.isPlaying)
            {
                Cleanup();
                return;
            }

            frames++;
            PositionAtriumCamera();
            if (frames == CaptureFrame)
            {
                CaptureLightingScreens();
            }
            else if (frames >= ExitFrame)
            {
                Cleanup();
                EditorApplication.ExitPlaymode();
            }
        }

        private static void CaptureLightingScreens()
        {
            Camera camera = FindCamera();
            if (camera == null)
            {
                Debug.LogWarning("[WolfPlayCheck] No camera found; could not capture lighting screens.");
                return;
            }

            Directory.CreateDirectory(OutputFolder);

            PositionCamera(camera, AtriumCameraPosition, AtriumLookAt);
            RenderToFile(camera, AtriumOutputPath);
            File.Copy(AtriumOutputPath, LegacyOutputPath, true);

            PositionCamera(camera, CorridorCameraPosition, CorridorLookAt);
            RenderToFile(camera, CorridorOutputPath);

            Debug.Log($"[WolfPlayCheck] Saved lighting renders to {AtriumOutputPath} and {CorridorOutputPath}");
        }

        private static void Cleanup()
        {
            SessionState.SetBool(PendingFlag, false);
            EditorApplication.update -= Tick;
        }

        private static void PositionAtriumCamera()
        {
            Camera camera = FindCamera();
            if (camera == null)
            {
                return;
            }

            PositionCamera(camera, AtriumCameraPosition, AtriumLookAt);
        }

        private static Camera FindCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                camera = Object.FindAnyObjectByType<Camera>();
            }

            if (camera == null)
            {
                return null;
            }

            return camera;
        }

        private static void PositionCamera(Camera camera, Vector3 position, Vector3 lookAt)
        {
            Quaternion rotation = Quaternion.LookRotation(lookAt - position, Vector3.up);
            if (camera.transform.parent != null)
            {
                Transform parent = camera.transform.parent;
                parent.position = position - Vector3.up * WolfMiniConstants.EyeHeight;
                parent.rotation = Quaternion.Euler(0f, rotation.eulerAngles.y, 0f);
                camera.transform.localPosition = Vector3.up * WolfMiniConstants.EyeHeight;
            }

            camera.transform.position = position;
            camera.transform.rotation = rotation;
        }

        private static void RenderToFile(Camera camera, string path)
        {
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            var renderTexture = new RenderTexture(CaptureWidth, CaptureHeight, 24, RenderTextureFormat.ARGBHalf)
            {
                name = "WolfPlayModeLightingCheck RT"
            };
            var texture = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGB24, false);

            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();

                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0, 0, CaptureWidth, CaptureHeight), 0, 0);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                Object.Destroy(texture);
                renderTexture.Release();
                Object.Destroy(renderTexture);
            }
        }
    }
}
