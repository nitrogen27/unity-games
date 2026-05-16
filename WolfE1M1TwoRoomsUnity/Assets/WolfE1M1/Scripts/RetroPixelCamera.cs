using UnityEngine;

namespace WolfE1M1
{
    [RequireComponent(typeof(Camera))]
    public sealed class RetroPixelCamera : MonoBehaviour
    {
        [SerializeField] private int width = 320;
        [SerializeField] private int height = 180;

        private RenderTexture lowResolutionBuffer;
        private Camera targetCamera;

        private void Awake()
        {
            targetCamera = GetComponent<Camera>();
            targetCamera.allowMSAA = false;
            QualitySettings.antiAliasing = 0;
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            EnsureBuffer();
            Graphics.Blit(source, lowResolutionBuffer);
            Graphics.Blit(lowResolutionBuffer, destination);
        }

        private void EnsureBuffer()
        {
            if (lowResolutionBuffer != null && lowResolutionBuffer.width == width && lowResolutionBuffer.height == height)
            {
                return;
            }

            ReleaseBuffer();
            lowResolutionBuffer = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                autoGenerateMips = false
            };
            lowResolutionBuffer.Create();
        }

        private void OnDisable()
        {
            ReleaseBuffer();
        }

        private void ReleaseBuffer()
        {
            if (lowResolutionBuffer == null)
            {
                return;
            }

            lowResolutionBuffer.Release();
            if (Application.isPlaying)
            {
                Destroy(lowResolutionBuffer);
            }
            else
            {
                DestroyImmediate(lowResolutionBuffer);
            }
            lowResolutionBuffer = null;
        }
    }
}
