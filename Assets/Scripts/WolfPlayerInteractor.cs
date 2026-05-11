using UnityEngine;

namespace HelloWorldRoom
{
    public sealed class WolfPlayerInteractor : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float range = 2.4f;
        [SerializeField] private LayerMask interactionMask = ~0;

        private void Awake()
        {
            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }
        }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.E))
            {
                return;
            }

            TryUse();
        }

        private void TryUse()
        {
            if (playerCamera == null)
            {
                return;
            }

            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (!Physics.Raycast(ray, out RaycastHit hit, range, interactionMask, QueryTriggerInteraction.Collide))
            {
                return;
            }

            WolfDoor door = hit.collider.GetComponentInParent<WolfDoor>();
            if (door != null)
            {
                door.Use();
            }
        }
    }
}
