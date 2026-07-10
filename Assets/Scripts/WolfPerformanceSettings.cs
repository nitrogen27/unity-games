using UnityEngine;

namespace HelloWorldRoom
{
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class WolfPerformanceSettings : MonoBehaviour
    {
        [SerializeField] private int targetFrameRate = 60;
        [SerializeField] private int qualityLevel = 2;
        [SerializeField] private int maxPixelLights = 24;
        [SerializeField] private int antiAliasingSamples = 0;
        [SerializeField] private bool disableVSync = true;
        [SerializeField] private bool disableRealtimeReflectionProbes = false;
        [SerializeField] private bool disableRealtimeShadows = false;
        [SerializeField] private bool forceAnisotropicTextures = true;
        [SerializeField] private bool enableCameraMsaa = false;
        [SerializeField] private bool disableCameraHdr = false;
        [SerializeField] private RenderingPath cameraRenderingPath = RenderingPath.DeferredShading;
        [SerializeField] private bool enableDeferredPostAntialiasing = true;
        [SerializeField, Range(0f, 1f)] private float postAntialiasingSubpixelBlending = 0.72f;
        [SerializeField, Range(0.0312f, 0.333f)] private float postAntialiasingEdgeThreshold = 0.11f;
        [SerializeField, Range(0.0156f, 0.0833f)] private float postAntialiasingEdgeThresholdMin = 0.0312f;
        [SerializeField] private bool enableCinematicPostProcess = true;
        [SerializeField, Range(0.5f, 2.0f)] private float cinematicExposure = 0.94f;
        [SerializeField, Range(0.8f, 1.6f)] private float cinematicContrast = 1.06f;
        [SerializeField, Range(0f, 0.18f)] private float cinematicBlackPoint = 0.002f;
        [SerializeField, Range(0.7f, 1.4f)] private float cinematicSaturation = 1.04f;
        [SerializeField, Range(0f, 0.45f)] private float cinematicLocalContrast = 0.09f;
        [SerializeField, Range(0.5f, 3.5f)] private float cinematicLocalRadius = 1.7f;
        [SerializeField, Range(0f, 0.18f)] private float cinematicShadowLift = 0.13f;
        [SerializeField, Range(0.6f, 2.5f)] private float cinematicBloomThreshold = 1.15f;
        [SerializeField, Range(0f, 1.5f)] private float cinematicBloomIntensity = 0.28f;
        [SerializeField, Range(0f, 1f)] private float cinematicBloomKnee = 0.46f;
        [SerializeField, Range(0.7f, 2.4f)] private float cinematicBloomScatter = 1.35f;
        [SerializeField, Range(1, 5)] private int cinematicBloomIterations = 3;
        [SerializeField, Range(1, 4)] private int cinematicBloomDownsample = 2;
        [SerializeField, Range(0f, 0.35f)] private float cinematicVignette = 0.07f;
        [SerializeField, Range(0f, 0.35f)] private float cinematicWarmHighlights = 0.055f;
        [SerializeField, Range(0f, 0.2f)] private float cinematicCoolShadows = 0.045f;

        private void Awake()
        {
            Apply();
        }

        private void OnEnable()
        {
            Apply();
        }

        private void Start()
        {
            ApplyCameraSettings();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            targetFrameRate = Mathf.Max(60, targetFrameRate);
            qualityLevel = Mathf.Max(0, qualityLevel);
            maxPixelLights = Mathf.Max(0, maxPixelLights);
            antiAliasingSamples = NormalizeAntiAliasingSamples(antiAliasingSamples);
            cinematicBloomIterations = Mathf.Clamp(cinematicBloomIterations, 1, 5);
            cinematicBloomDownsample = Mathf.Clamp(cinematicBloomDownsample, 1, 4);
        }
#endif

        public void Apply()
        {
            int qualityCount = QualitySettings.names != null ? QualitySettings.names.Length : 0;
            if (qualityCount > 0)
            {
                int safeQualityLevel = Mathf.Clamp(qualityLevel, 0, qualityCount - 1);
                if (QualitySettings.GetQualityLevel() != safeQualityLevel)
                {
                    QualitySettings.SetQualityLevel(safeQualityLevel, true);
                }
            }

            if (disableVSync)
            {
                QualitySettings.vSyncCount = 0;
            }

            Application.targetFrameRate = Mathf.Max(60, targetFrameRate);
            QualitySettings.pixelLightCount = Mathf.Max(0, maxPixelLights);
            QualitySettings.antiAliasing = NormalizeAntiAliasingSamples(antiAliasingSamples);

            QualitySettings.realtimeReflectionProbes = !disableRealtimeReflectionProbes;

            if (disableRealtimeShadows)
            {
                QualitySettings.shadows = ShadowQuality.Disable;
                QualitySettings.shadowDistance = 0f;
            }
            else
            {
                QualitySettings.shadows = ShadowQuality.All;
                QualitySettings.shadowResolution = ShadowResolution.High;
                QualitySettings.shadowDistance = 42f;
            }

            if (forceAnisotropicTextures)
            {
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            }

            ApplyCameraSettings();
        }

        private void ApplyCameraSettings()
        {
            bool allowMsaa = enableCameraMsaa && QualitySettings.antiAliasing > 0;

            foreach (Camera camera in Camera.allCameras)
            {
                camera.renderingPath = cameraRenderingPath;

                if (disableCameraHdr)
                {
                    camera.allowHDR = false;
                }
                else
                {
                    camera.allowHDR = true;
                }

                camera.allowMSAA = allowMsaa;
                camera.useOcclusionCulling = true;
                ApplyPostAntialiasing(camera);
                ApplyCinematicPostProcess(camera);
            }
        }

        private void ApplyPostAntialiasing(Camera camera)
        {
            WolfDeferredPostAntiAliasing postAntialiasing = camera.GetComponent<WolfDeferredPostAntiAliasing>();
            if (!enableDeferredPostAntialiasing || cameraRenderingPath != RenderingPath.DeferredShading)
            {
                if (postAntialiasing != null)
                {
                    postAntialiasing.enabled = false;
                }

                return;
            }

            if (postAntialiasing == null)
            {
                postAntialiasing = camera.gameObject.AddComponent<WolfDeferredPostAntiAliasing>();
            }

            postAntialiasing.Configure(
                postAntialiasingSubpixelBlending,
                postAntialiasingEdgeThreshold,
                postAntialiasingEdgeThresholdMin);
            postAntialiasing.enabled = true;
        }

        private void ApplyCinematicPostProcess(Camera camera)
        {
            WolfCinematicPostProcess cinematicPostProcess = camera.GetComponent<WolfCinematicPostProcess>();
            if (!enableCinematicPostProcess)
            {
                if (cinematicPostProcess != null)
                {
                    cinematicPostProcess.enabled = false;
                }

                return;
            }

            if (cinematicPostProcess == null)
            {
                cinematicPostProcess = camera.gameObject.AddComponent<WolfCinematicPostProcess>();
            }

            cinematicPostProcess.Configure(
                cinematicExposure,
                cinematicContrast,
                cinematicBlackPoint,
                cinematicSaturation,
                cinematicLocalContrast,
                cinematicLocalRadius,
                cinematicShadowLift,
                cinematicBloomThreshold,
                cinematicBloomIntensity,
                cinematicBloomKnee,
                cinematicBloomScatter,
                cinematicBloomIterations,
                cinematicBloomDownsample,
                cinematicVignette,
                cinematicWarmHighlights,
                cinematicCoolShadows);
            cinematicPostProcess.enabled = true;
        }

        private static int NormalizeAntiAliasingSamples(int samples)
        {
            if (samples <= 0)
            {
                return 0;
            }

            if (samples <= 2)
            {
                return 2;
            }

            return samples <= 4 ? 4 : 8;
        }
    }
}
