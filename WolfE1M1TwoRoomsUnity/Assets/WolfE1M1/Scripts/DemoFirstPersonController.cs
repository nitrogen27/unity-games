using UnityEngine;

namespace WolfE1M1
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class DemoFirstPersonController : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private float runMultiplier = 1.45f;
        [SerializeField] private float mouseSensitivity = 2.2f;
        [SerializeField] private float doorRange = 2.35f;

        private CharacterController characterController;
        private float pitch;
        private float verticalVelocity;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            characterController.height = WolfE1M1Constants.EyeHeight * 2f;
            characterController.center = Vector3.up * WolfE1M1Constants.EyeHeight;
            characterController.radius = 0.35f;

            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }

            if (playerCamera != null)
            {
                playerCamera.transform.localPosition = Vector3.up * WolfE1M1Constants.EyeHeight;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (Input.GetMouseButtonDown(0))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            Look();
            Move();

            if (Input.GetKeyDown(KeyCode.E))
            {
                TryToggleDoor();
            }
        }

        private void Look()
        {
            if (Cursor.lockState != CursorLockMode.Locked || playerCamera == null)
            {
                return;
            }

            float yaw = Input.GetAxis("Mouse X") * mouseSensitivity;
            float pitchDelta = Input.GetAxis("Mouse Y") * mouseSensitivity;

            transform.Rotate(Vector3.up, yaw, Space.World);
            pitch = Mathf.Clamp(pitch - pitchDelta, -78f, 78f);
            playerCamera.transform.localEulerAngles = new Vector3(pitch, 0f, 0f);
        }

        private void Move()
        {
            float speed = Input.GetKey(KeyCode.LeftShift) ? moveSpeed * runMultiplier : moveSpeed;
            Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            input = Vector3.ClampMagnitude(input, 1f);

            Vector3 move = (transform.right * input.x + transform.forward * input.z) * speed;

            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -1f;
            }

            verticalVelocity += Physics.gravity.y * Time.deltaTime;
            move.y = verticalVelocity;
            characterController.Move(move * Time.deltaTime);
        }

        private void TryToggleDoor()
        {
            if (playerCamera == null)
            {
                return;
            }

            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, doorRange))
            {
                DemoDoor door = hit.collider.GetComponentInParent<DemoDoor>();
                if (door != null)
                {
                    door.Toggle();
                }
            }
        }
    }
}
