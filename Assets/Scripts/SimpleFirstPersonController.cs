using UnityEngine;

namespace HelloWorldRoom
{
    [RequireComponent(typeof(CharacterController))]
    public class SimpleFirstPersonController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 4f;
        [SerializeField] private float runSpeed = 7f;
        [SerializeField] private float jumpHeight = 1.1f;
        [SerializeField] private float gravity = -18f;

        [Header("Look")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float mouseSensitivity = 2.2f;
        [SerializeField] private float maxPitch = 82f;

        private CharacterController controller;
        private float pitch;
        private float verticalVelocity;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();

            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }
        }

        private void Start()
        {
            LockCursor();
        }

        private void Update()
        {
            HandleCursor();
            HandleLook();
            HandleMovement();
        }

        private void HandleCursor()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (Input.GetMouseButtonDown(0))
            {
                LockCursor();
            }
        }

        private void HandleLook()
        {
            if (playerCamera == null || Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            transform.Rotate(Vector3.up * mouseX);
            pitch = Mathf.Clamp(pitch - mouseY, -maxPitch, maxPitch);
            playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void HandleMovement()
        {
            float inputX = Input.GetAxisRaw("Horizontal");
            float inputZ = Input.GetAxisRaw("Vertical");
            Vector3 input = Vector3.ClampMagnitude(new Vector3(inputX, 0f, inputZ), 1f);

            float speed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
            Vector3 movement = (transform.right * input.x + transform.forward * input.z) * speed;

            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            if (controller.isGrounded && Input.GetKeyDown(KeyCode.Space))
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            verticalVelocity += gravity * Time.deltaTime;
            movement.y = verticalVelocity;

            controller.Move(movement * Time.deltaTime);
        }

        private static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
