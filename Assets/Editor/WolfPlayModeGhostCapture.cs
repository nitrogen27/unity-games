using System.IO;
using UnityEditor;
using UnityEngine;
using WolfMini.Core;

namespace WolfMini.EditorTools
{
    /// <summary>
    /// Temporary check tool: when the editor already sits in Play Mode (a live
    /// player session), clones the player camera together with its post stack,
    /// renders the atrium reference viewpoint offscreen and saves it — without
    /// moving the player's camera or toggling the play state.
    /// </summary>
    public static class WolfPlayModeGhostCapture
    {
        private const string OutputPath = "Docs/Verification/playmode_ghost_atrium.png";
        private const int Width = 1280;
        private const int Height = 800;

        private static readonly Vector3 ViewPosition = new Vector3(124.2f, WolfMiniConstants.EyeHeight + 4.65f, 118f);
        private static readonly Vector3 ViewLookAt = new Vector3(108f, 3f, 99f);

        [MenuItem("Tools/Wolf Full3D/Capture Atrium Ghost View (Play Mode)")]
        public static void Capture()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[WolfGhostCapture] Editor is not in Play Mode; use Tools/Wolf Full3D/Play Mode Lighting Check instead.");
                return;
            }

            Camera source = Camera.main != null ? Camera.main : Object.FindAnyObjectByType<Camera>();
            if (source == null)
            {
                Debug.LogWarning("[WolfGhostCapture] No camera found in the play session.");
                return;
            }

            GameObject rig = Object.Instantiate(source.gameObject);
            rig.name = "Wolf Ghost Capture Camera";
            try
            {
                // Children carry view models and similar attachments; the ghost
                // only needs the camera plus the image-effect components on it.
                foreach (Transform child in rig.transform)
                {
                    Object.Destroy(child.gameObject);
                }

                Camera camera = rig.GetComponent<Camera>();
                camera.enabled = false;
                foreach (Behaviour behaviour in rig.GetComponents<Behaviour>())
                {
                    if (behaviour is Camera)
                    {
                        continue;
                    }

                    // OnRenderImage effects must stay enabled to run during the
                    // manual render; everything else (audio listener, player
                    // logic) must not touch the live session.
                    bool isImageEffect = behaviour.GetType().GetMethod(
                        "OnRenderImage",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public) != null;
                    behaviour.enabled = isImageEffect;
                }

                rig.transform.SetParent(null, true);
                rig.transform.position = ViewPosition;
                rig.transform.rotation = Quaternion.LookRotation(ViewLookAt - ViewPosition, Vector3.up);

                Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
                RenderToFile(camera, OutputPath);
                Debug.Log($"[WolfGhostCapture] Saved atrium ghost view to {OutputPath}");
            }
            finally
            {
                Object.Destroy(rig);
            }
        }

        private static void RenderToFile(Camera camera, string path)
        {
            RenderTexture previousActive = RenderTexture.active;
            var renderTexture = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGBHalf)
            {
                name = "WolfGhostCapture RT"
            };
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
                RenderTexture.active = previousActive;
                Object.Destroy(texture);
                renderTexture.Release();
                Object.Destroy(renderTexture);
            }
        }
    }
}
