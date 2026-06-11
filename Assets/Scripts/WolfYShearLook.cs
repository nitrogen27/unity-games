using UnityEngine;

namespace HelloWorldRoom
{
    /// <summary>
    /// Wolf3D-style look-up/down. Instead of rotating the camera (which makes
    /// vertical wall edges converge toward the zenith), the camera stays level
    /// and the projection matrix is sheared vertically — the same y-shearing the
    /// original raycaster used, so walls always stay perfectly vertical on screen.
    /// Requires the paired SimpleFirstPersonController to have RotateCameraPitch
    /// disabled.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class WolfYShearLook : MonoBehaviour
    {
        private Camera shearCamera;
        private SimpleFirstPersonController controller;

        private void Awake()
        {
            shearCamera = GetComponent<Camera>();
            controller = GetComponentInParent<SimpleFirstPersonController>();
        }

        private void OnDisable()
        {
            if (shearCamera != null)
            {
                shearCamera.ResetProjectionMatrix();
            }
        }

        private void LateUpdate()
        {
            if (controller == null || controller.RotateCameraPitch)
            {
                return;
            }

            shearCamera.ResetProjectionMatrix();

            // Constant NDC shift: positive pitch (looking down) moves the image
            // up, negative pitch (looking up) moves it down, scaled so a shear of
            // one half-screen equals the same view change a real rotation gives.
            float halfFov = shearCamera.fieldOfView * 0.5f * Mathf.Deg2Rad;
            float shift = Mathf.Tan(-controller.Pitch * Mathf.Deg2Rad) / Mathf.Tan(halfFov);

            Matrix4x4 projection = shearCamera.projectionMatrix;
            projection[1, 2] += shift;
            shearCamera.projectionMatrix = projection;
        }
    }
}
