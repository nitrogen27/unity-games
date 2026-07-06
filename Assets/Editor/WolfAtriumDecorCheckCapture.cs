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

                float eye = WolfMiniConstants.EyeHeight;
                Shoot(camera, rig, new Vector3(109.8f, eye, 112f), new Vector3(109.8f, 1.8f, 122.4f), "atrium_banner");
                Shoot(camera, rig, new Vector3(102.6f, eye, 108f), new Vector3(102.6f, 1.8f, 97.2f), "atrium_portrait");
                Shoot(camera, rig, new Vector3(140f, eye, 102.6f), new Vector3(151.2f, 1.8f, 102.6f), "atrium_eagle");
                Shoot(camera, rig, new Vector3(124.2f, eye + 4.65f, 118f), new Vector3(108f, 3f, 99f), "atrium_wide");

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
