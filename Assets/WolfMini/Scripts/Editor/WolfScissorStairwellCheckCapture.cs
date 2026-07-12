using System.IO;
using UnityEditor;
using UnityEngine;
using WolfMini.Core;
using WolfMini.Level;

namespace WolfMini.EditorTools
{
    /// <summary>
    /// Renders control screenshots of the scissor stairwell room east of the
    /// start T-corridor into Docs/Verification: the middle-floor landing with
    /// both flights, the lower flight seen from its mouth, the upper flight's
    /// mouth on the middle floor and the guarded pit on the top storey. Runs
    /// an editor rebuild first so the shots always show the current sector
    /// definition without entering play mode.
    /// </summary>
    public static class WolfScissorStairwellCheckCapture
    {
        private const string OutputFolder = "Docs/Verification";
        private const int Width = 1280;
        private const int Height = 800;

        private struct Shot
        {
            public string name;
            public Vector3 position;
            public float yaw;
            public float pitch;
        }

        [MenuItem("Tools/Wolf Full3D/Capture Scissor Stairwell Screens")]
        public static void Capture()
        {
            WolfSectorMeshBuilder builder = Object.FindAnyObjectByType<WolfSectorMeshBuilder>();
            if (builder == null)
            {
                Debug.LogError("[WolfFull3D] No WolfSectorMeshBuilder in the open scene. Run 'Build Full3D Scene' first.");
                return;
            }

            builder.Build();
            Directory.CreateDirectory(OutputFolder);

            float eye = WolfMiniConstants.EyeHeight;
            float storey = WolfMiniConstants.WallHeight + WolfMiniConstants.FloorSlabThickness;
            Shot[] shots =
            {
                // Middle floor, standing at the door: landing lane ahead, the
                // lower flight's pit to the right, the upper flight's mouth to
                // the left.
                new Shot { name = "scissor_floor0_landing", position = new Vector3(134.0f, eye, 23.4f), yaw = 90f, pitch = 6f },
                // Bottom storey, at the lower flight's mouth looking up the
                // stairs toward the middle floor.
                new Shot { name = "scissor_lower_flight_up", position = new Vector3(149.8f, -storey + eye, 19.8f), yaw = 270f, pitch = -10f },
                // Middle floor, west landing, looking into the upper flight's
                // mouth with its stairs climbing east.
                new Shot { name = "scissor_floor0_mouth_up", position = new Vector3(134.2f, eye, 27.0f), yaw = 90f, pitch = -8f },
                // Top storey, over the deep end of the upper flight's pit:
                // mouth rail in front, pit rising east to the flush head.
                new Shot { name = "scissor_top_pit_rails", position = new Vector3(134.2f, storey + eye, 27.0f), yaw = 90f, pitch = 14f }
            };

            GameObject rig = new GameObject("Wolf Scissor Capture Camera");
            try
            {
                Camera camera = rig.AddComponent<Camera>();
                camera.fieldOfView = 58f;
                camera.nearClipPlane = 0.05f;
                camera.farClipPlane = 260f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color32(56, 56, 56, 255);

                foreach (Shot shot in shots)
                {
                    rig.transform.position = shot.position;
                    rig.transform.rotation = Quaternion.Euler(shot.pitch, shot.yaw, 0f);
                    RenderToFile(camera, $"{OutputFolder}/{shot.name}.png");
                }

                Debug.Log($"[WolfFull3D] Saved scissor stairwell screens to {OutputFolder}/scissor_*.png.");
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
