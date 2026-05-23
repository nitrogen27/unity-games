using UnityEngine;

namespace HelloWorldRoom
{
    [RequireComponent(typeof(Light))]
    public class FlickerLight : MonoBehaviour
    {
        [SerializeField] private float minIntensity = 0.4f;
        [SerializeField] private float maxIntensity = 3.5f;
        [SerializeField] private float minInterval = 0.04f;
        [SerializeField] private float maxInterval = 0.18f;
        [SerializeField] private float smoothing = 0.5f;
        [SerializeField] private float updateInterval = 0.08f;

        private Light targetLight;
        private float nextChangeTime;
        private float nextUpdateTime;
        private float currentTarget;

        private void Awake()
        {
            targetLight = GetComponent<Light>();
            currentTarget = targetLight.intensity;
        }

        private void Update()
        {
            float now = Time.time;
            if (now < nextUpdateTime)
            {
                return;
            }

            nextUpdateTime = now + Mathf.Max(0.02f, updateInterval);

            if (now >= nextChangeTime)
            {
                currentTarget = Random.Range(minIntensity, maxIntensity);
                nextChangeTime = now + Random.Range(minInterval, maxInterval);
            }

            float frameScale = Mathf.Max(updateInterval, Time.deltaTime) * 60f;
            targetLight.intensity = Mathf.Lerp(
                targetLight.intensity,
                currentTarget,
                1f - Mathf.Pow(1f - smoothing, frameScale));
        }
    }
}
