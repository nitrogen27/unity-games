using UnityEngine;

namespace WolfE1M1
{
    [RequireComponent(typeof(Collider))]
    public sealed class DemoDoor : MonoBehaviour
    {
        [SerializeField] private Vector3 slideDirection = Vector3.forward;
        [SerializeField] private float speed = 2.8f;

        private Vector3 closedPosition;
        private Vector3 openPosition;
        private bool isOpen;
        private Collider doorCollider;

        public void Initialize(Vector3 direction)
        {
            slideDirection = direction.sqrMagnitude > 0f ? direction.normalized : Vector3.forward;
            closedPosition = transform.localPosition;
            openPosition = closedPosition + slideDirection * WolfE1M1Constants.DoorTravel;
            doorCollider = GetComponent<Collider>();
        }

        private void Awake()
        {
            doorCollider = GetComponent<Collider>();
            closedPosition = transform.localPosition;
            openPosition = closedPosition + slideDirection.normalized * WolfE1M1Constants.DoorTravel;
        }

        private void Update()
        {
            Vector3 target = isOpen ? openPosition : closedPosition;
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, target, speed * Time.deltaTime);

            float opened = Vector3.Distance(transform.localPosition, closedPosition) / WolfE1M1Constants.DoorTravel;
            if (doorCollider != null)
            {
                doorCollider.enabled = opened < WolfE1M1Constants.DoorPassableOpenAmount;
            }
        }

        public void Toggle()
        {
            isOpen = !isOpen;
        }
    }
}
