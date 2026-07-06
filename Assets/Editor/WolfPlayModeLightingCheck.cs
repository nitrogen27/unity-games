using UnityEditor;
using UnityEngine;
using WolfMini.Core;

namespace WolfMini.EditorTools
{
    /// <summary>
    /// Temporary check tool: enters Play Mode, lets the level build and the
    /// realtime reflection probes render, saves a Game-view screenshot to
    /// Docs/Verification and exits Play Mode again.
    /// </summary>
    [InitializeOnLoad]
    public static class WolfPlayModeLightingCheck
    {
        private const string PendingFlag = "WolfPlayModeLightingCheck.Pending";
        private const string PendingStamp = "WolfPlayModeLightingCheck.Stamp";
        private const string OutputPath = "Docs/Verification/playmode_lighting.png";
        private const int CaptureFrame = 150;
        private const int ExitFrame = 230;
        // A stale request must never hijack a manual play session later on.
        private const float PendingValiditySeconds = 120f;

        private static readonly Vector3 AtriumCameraPosition = new Vector3(124.2f, WolfMiniConstants.EyeHeight + 4.65f, 118f);
        private static readonly Vector3 AtriumLookAt = new Vector3(108f, 3f, 99f);

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
                PositionAtriumCamera();
                ScreenCapture.CaptureScreenshot(OutputPath, 1);
                Debug.Log($"[WolfPlayCheck] Queued atrium lighting screenshot to {OutputPath}");
            }
            else if (frames >= ExitFrame)
            {
                Cleanup();
                EditorApplication.ExitPlaymode();
            }
        }

        private static void Cleanup()
        {
            SessionState.SetBool(PendingFlag, false);
            EditorApplication.update -= Tick;
        }

        private static void PositionAtriumCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                camera = Object.FindAnyObjectByType<Camera>();
            }

            if (camera == null)
            {
                return;
            }

            Quaternion rotation = Quaternion.LookRotation(AtriumLookAt - AtriumCameraPosition, Vector3.up);
            if (camera.transform.parent != null)
            {
                Transform parent = camera.transform.parent;
                parent.position = AtriumCameraPosition - Vector3.up * WolfMiniConstants.EyeHeight;
                parent.rotation = Quaternion.Euler(0f, rotation.eulerAngles.y, 0f);
                camera.transform.localPosition = Vector3.up * WolfMiniConstants.EyeHeight;
            }

            camera.transform.position = AtriumCameraPosition;
            camera.transform.rotation = rotation;
        }
    }
}
