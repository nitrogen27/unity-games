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

        private Light targetLight;
        private float nextChangeTime;
        private float currentTarget;

        private void Awake()
        {
            targetLight = GetComponent<Light>();
            currentTarget = targetLight.intensity;
        }

        private void Update()
        {
            if (Time.time >= nextChangeTime)
            {
                currentTarget = Random.Range(minIntensity, maxIntensity);
                nextChangeTime = Time.time + Random.Range(minInterval, maxInterval);
            }

            targetLight.intensity = Mathf.Lerp(
                targetLight.intensity,
                currentTarget,
                1f - Mathf.Pow(1f - smoothing, Time.deltaTime * 60f));
        }
    }
}
