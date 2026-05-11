using UnityEngine;

namespace HelloWorldRoom
{
    public sealed class WolfDoor : MonoBehaviour
    {
        [SerializeField] private Vector3 openOffset = Vector3.up * 2.2f;
        [SerializeField] private float openSpeed = 2.4f;
        [SerializeField] private float closeDelay = 3.5f;
        [SerializeField] private bool locked;
        [SerializeField] private AudioSource audioSource;

        private Vector3 closedPosition;
        private Vector3 targetPosition;
        private float closeAt = -1f;
        private bool initialized;

        public bool IsLocked => locked;
        public bool IsOpen => Vector3.Distance(transform.position, closedPosition + openOffset) < 0.03f;

        public void Configure(Vector3 doorOpenOffset, float doorOpenSpeed, float doorCloseDelay, bool startsLocked)
        {
            openOffset = doorOpenOffset;
            openSpeed = doorOpenSpeed;
            closeDelay = doorCloseDelay;
            locked = startsLocked;
        }

        private void Awake()
        {
            Initialize();
        }

        private void Update()
        {
            Initialize();

            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                openSpeed * Time.deltaTime);

            if (closeAt > 0f && Time.time >= closeAt)
            {
                Close();
            }
        }

        public void Use()
        {
            Initialize();

            if (locked)
            {
                return;
            }

            Open();
        }

        public void Unlock()
        {
            locked = false;
        }

        private void Open()
        {
            targetPosition = closedPosition + openOffset;
            closeAt = Time.time + closeDelay;
            PlayClick(1.1f);
        }

        private void Close()
        {
            targetPosition = closedPosition;
            closeAt = -1f;
            PlayClick(0.8f);
        }

        private void Initialize()
        {
            if (initialized)
            {
                return;
            }

            closedPosition = transform.position;
            targetPosition = closedPosition;
            initialized = true;
        }

        private void PlayClick(float pitch)
        {
            if (audioSource == null)
            {
                return;
            }

            audioSource.pitch = pitch;
            audioSource.Play();
        }
    }
}
