using UnityEngine;

namespace HelloWorldRoom
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    [DefaultExecutionOrder(1500)]
    public sealed class WolfCinematicPostProcess : MonoBehaviour
    {
        private const string ShaderName = "Hidden/Wolf/CinematicPost";

        [SerializeField, Range(0.5f, 2.0f)] private float exposure = 0.94f;
        [SerializeField, Range(0.8f, 1.6f)] private float contrast = 1.06f;
        [SerializeField, Range(0.0f, 0.18f)] private float blackPoint = 0.002f;
        [SerializeField, Range(0.7f, 1.4f)] private float saturation = 1.04f;
        [SerializeField, Range(0f, 0.45f)] private float localContrast = 0.09f;
        [SerializeField, Range(0.5f, 3.5f)] private float localRadius = 1.7f;
        [SerializeField, Range(0f, 0.18f)] private float shadowLift = 0.13f;
        [SerializeField, Range(0.6f, 2.5f)] private float bloomThreshold = 1.15f;
        [SerializeField, Range(0f, 1.5f)] private float bloomIntensity = 0.28f;
        [SerializeField, Range(0f, 1.0f)] private float bloomKnee = 0.46f;
        [SerializeField, Range(0.7f, 2.4f)] private float bloomScatter = 1.35f;
        [SerializeField, Range(1, 5)] private int bloomIterations = 3;
        [SerializeField, Range(1, 4)] private int bloomDownsample = 2;
        [SerializeField, Range(0f, 0.35f)] private float vignette = 0.07f;
        [SerializeField, Range(0f, 0.35f)] private float warmHighlights = 0.055f;
        [SerializeField, Range(0f, 0.2f)] private float coolShadows = 0.045f;

        private static readonly int ExposureId = Shader.PropertyToID("_Exposure");
        private static readonly int ContrastId = Shader.PropertyToID("_Contrast");
        private static readonly int BlackPointId = Shader.PropertyToID("_BlackPoint");
        private static readonly int SaturationId = Shader.PropertyToID("_Saturation");
        private static readonly int LocalContrastId = Shader.PropertyToID("_LocalContrast");
        private static readonly int LocalRadiusId = Shader.PropertyToID("_LocalRadius");
        private static readonly int ShadowLiftId = Shader.PropertyToID("_ShadowLift");
        private static readonly int BloomThresholdId = Shader.PropertyToID("_BloomThreshold");
        private static readonly int BloomIntensityId = Shader.PropertyToID("_BloomIntensity");
        private static readonly int BloomKneeId = Shader.PropertyToID("_BloomKnee");
        private static readonly int BloomScatterId = Shader.PropertyToID("_BloomScatter");
        private static readonly int VignetteId = Shader.PropertyToID("_Vignette");
        private static readonly int WarmHighlightsId = Shader.PropertyToID("_WarmHighlights");
        private static readonly int CoolShadowsId = Shader.PropertyToID("_CoolShadows");
        private static readonly int BlurDirectionId = Shader.PropertyToID("_BlurDirection");
        private static readonly int BloomTexId = Shader.PropertyToID("_BloomTex");

        private Material material;

        public void Configure(
            float targetExposure,
            float targetContrast,
            float targetBlackPoint,
            float targetSaturation,
            float targetLocalContrast,
            float targetLocalRadius,
            float targetShadowLift,
            float targetBloomThreshold,
            float targetBloomIntensity,
            float targetBloomKnee,
            float targetBloomScatter,
            int targetBloomIterations,
            int targetBloomDownsample,
            float targetVignette,
            float targetWarmHighlights,
            float targetCoolShadows)
        {
            exposure = Mathf.Clamp(targetExposure, 0.5f, 2.0f);
            contrast = Mathf.Clamp(targetContrast, 0.8f, 1.6f);
            blackPoint = Mathf.Clamp(targetBlackPoint, 0.0f, 0.18f);
            saturation = Mathf.Clamp(targetSaturation, 0.7f, 1.4f);
            localContrast = Mathf.Clamp(targetLocalContrast, 0.0f, 0.45f);
            localRadius = Mathf.Clamp(targetLocalRadius, 0.5f, 3.5f);
            shadowLift = Mathf.Clamp(targetShadowLift, 0.0f, 0.18f);
            bloomThreshold = Mathf.Clamp(targetBloomThreshold, 0.6f, 2.5f);
            bloomIntensity = Mathf.Clamp(targetBloomIntensity, 0.0f, 1.5f);
            bloomKnee = Mathf.Clamp01(targetBloomKnee);
            bloomScatter = Mathf.Clamp(targetBloomScatter, 0.7f, 2.4f);
            bloomIterations = Mathf.Clamp(targetBloomIterations, 1, 5);
            bloomDownsample = Mathf.Clamp(targetBloomDownsample, 1, 4);
            vignette = Mathf.Clamp(targetVignette, 0.0f, 0.35f);
            warmHighlights = Mathf.Clamp(targetWarmHighlights, 0.0f, 0.35f);
            coolShadows = Mathf.Clamp(targetCoolShadows, 0.0f, 0.2f);
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (!enabled || !EnsureMaterial())
            {
                Graphics.Blit(source, destination);
                return;
            }

            ConfigureMaterial();

            RenderTexture bloomA = null;
            RenderTexture bloomB = null;
            try
            {
                int width = Mathf.Max(1, source.width / bloomDownsample);
                int height = Mathf.Max(1, source.height / bloomDownsample);
                bloomA = GetTemporary(source, width, height);
                bloomB = GetTemporary(source, width, height);

                Graphics.Blit(source, bloomA, material, 0);

                for (int i = 0; i < bloomIterations; i++)
                {
                    material.SetVector(BlurDirectionId, new Vector4(1f, 0f, 0f, 0f));
                    Graphics.Blit(bloomA, bloomB, material, 1);
                    Swap(ref bloomA, ref bloomB);

                    material.SetVector(BlurDirectionId, new Vector4(0f, 1f, 0f, 0f));
                    Graphics.Blit(bloomA, bloomB, material, 1);
                    Swap(ref bloomA, ref bloomB);
                }

                material.SetTexture(BloomTexId, bloomA);
                Graphics.Blit(source, destination, material, 2);
            }
            finally
            {
                if (bloomA != null)
                {
                    RenderTexture.ReleaseTemporary(bloomA);
                }

                if (bloomB != null)
                {
                    RenderTexture.ReleaseTemporary(bloomB);
                }
            }
        }

        private static RenderTexture GetTemporary(RenderTexture source, int width, int height)
        {
            RenderTexture texture = RenderTexture.GetTemporary(width, height, 0, source.format, RenderTextureReadWrite.Default);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            return texture;
        }

        private void ConfigureMaterial()
        {
            material.SetFloat(ExposureId, exposure);
            material.SetFloat(ContrastId, contrast);
            material.SetFloat(BlackPointId, blackPoint);
            material.SetFloat(SaturationId, saturation);
            material.SetFloat(LocalContrastId, localContrast);
            material.SetFloat(LocalRadiusId, localRadius);
            material.SetFloat(ShadowLiftId, shadowLift);
            material.SetFloat(BloomThresholdId, bloomThreshold);
            material.SetFloat(BloomIntensityId, bloomIntensity);
            material.SetFloat(BloomKneeId, bloomKnee);
            material.SetFloat(BloomScatterId, bloomScatter);
            material.SetFloat(VignetteId, vignette);
            material.SetFloat(WarmHighlightsId, warmHighlights);
            material.SetFloat(CoolShadowsId, coolShadows);
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

        private static void Swap(ref RenderTexture first, ref RenderTexture second)
        {
            RenderTexture temporary = first;
            first = second;
            second = temporary;
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
