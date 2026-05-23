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
        [SerializeField] private int antiAliasingSamples = 4;
        [SerializeField] private bool disableVSync = true;
        [SerializeField] private bool disableRealtimeReflectionProbes = false;
        [SerializeField] private bool disableRealtimeShadows = false;
        [SerializeField] private bool forceAnisotropicTextures = true;
        [SerializeField] private bool enableCameraMsaa = true;
        [SerializeField] private bool disableCameraHdr = false;

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
            }
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
