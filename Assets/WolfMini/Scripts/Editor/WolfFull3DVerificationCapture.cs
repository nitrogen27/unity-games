using System.IO;
using UnityEditor;
using UnityEngine;
using WolfMini.Core;
using WolfMini.Level;

namespace WolfMini.EditorTools
{
    /// <summary>
    /// Renders control screenshots from the player spawn at fixed pitches
    /// (straight, +25°, +40° up) into Docs/Verification so wall seams, door
    /// proportions and ceiling height can be checked without entering play mode.
    /// </summary>
    public static class WolfFull3DVerificationCapture
    {
        private const string OutputFolder = "Docs/Verification";
        private const int Width = 1280;
        private const int Height = 800;

        [MenuItem("Tools/Wolf Full3D/Capture Verification Screens")]
        public static void Capture()
        {
            WolfSectorMeshBuilder builder = Object.FindFirstObjectByType<WolfSectorMeshBuilder>();
            if (builder == null)
            {
                Debug.LogError("[WolfFull3D] No WolfSectorMeshBuilder in the open scene. Run 'Build Full3D Scene' first.");
                return;
            }

            builder.Build();
            WolfSectorLevelDefinition definition = builder.Definition;
            if (definition == null)
            {
                Debug.LogError("[WolfFull3D] Builder has no definition assigned.");
                return;
            }

            Directory.CreateDirectory(OutputFolder);

            GameObject rig = new GameObject("Wolf Verification Camera");
            try
            {
                Camera camera = rig.AddComponent<Camera>();
                camera.fieldOfView = 58f;
                camera.nearClipPlane = 0.05f;
                camera.farClipPlane = 260f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color32(56, 56, 56, 255);
                rig.transform.position = definition.playerSpawnPosition + Vector3.up * WolfMiniConstants.EyeHeight;

                foreach (float pitch in new[] { 0f, 25f, 40f })
                {
                    rig.transform.rotation = Quaternion.Euler(-pitch, definition.playerSpawnYaw, 0f);
                    RenderToFile(camera, $"{OutputFolder}/pitch_{pitch:00}.png");
                }

                Debug.Log($"[WolfFull3D] Saved verification screens to {OutputFolder}/pitch_00|25|40.png " +
                          $"(eye {WolfMiniConstants.EyeHeight} m at spawn {definition.playerSpawnPosition}).");
            }
            finally
            {
                Object.DestroyImmediate(rig);
            }
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
