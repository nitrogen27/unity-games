using UnityEngine;

namespace HelloWorldRoom
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class WolfDeferredPostAntiAliasing : MonoBehaviour
    {
        private const string ShaderName = "Hidden/Wolf/DeferredFXAA";

        [SerializeField, Range(0f, 1f)] private float subpixelBlending = 0.72f;
        [SerializeField, Range(0.0312f, 0.333f)] private float edgeThreshold = 0.11f;
        [SerializeField, Range(0.0156f, 0.0833f)] private float edgeThresholdMin = 0.0312f;

        private Material material;

        public void Configure(float subpixelBlend, float threshold, float thresholdMin)
        {
            subpixelBlending = Mathf.Clamp01(subpixelBlend);
            edgeThreshold = Mathf.Clamp(threshold, 0.0312f, 0.333f);
            edgeThresholdMin = Mathf.Clamp(thresholdMin, 0.0156f, 0.0833f);
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (!enabled || !EnsureMaterial())
            {
                Graphics.Blit(source, destination);
                return;
            }

            material.SetFloat("_SubpixelBlending", subpixelBlending);
            material.SetFloat("_EdgeThreshold", edgeThreshold);
            material.SetFloat("_EdgeThresholdMin", edgeThresholdMin);

            if (source == destination)
            {
                RenderTexture temporary = RenderTexture.GetTemporary(source.descriptor);
                Graphics.Blit(source, temporary, material);
                Graphics.Blit(temporary, destination);
                RenderTexture.ReleaseTemporary(temporary);
                return;
            }

            Graphics.Blit(source, destination, material);
        }

        private bool EnsureMaterial()
        {
            if (material != null)
            {
                return true;
            }

            Shader shader = Shader.Find(ShaderName);
            if (shader == null || !shader.isSupported)
            {
                return false;
            }

            material = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            return true;
        }

        private void OnDisable()
        {
            if (material == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(material);
            }
            else
            {
                DestroyImmediate(material);
            }

            material = null;
        }
    }
}
