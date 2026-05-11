using UnityEngine;

namespace HelloWorldRoom
{
    public sealed class WolfBillboard : MonoBehaviour
    {
        [SerializeField] private bool lockPitch = true;

        private Transform target;

        private void LateUpdate()
        {
            if (target == null)
            {
                Camera main = Camera.main;
                if (main == null)
                {
                    return;
                }

                target = main.transform;
            }

            Vector3 lookDirection = transform.position - target.position;
            if (lockPitch)
            {
                lookDirection.y = 0f;
            }

            if (lookDirection.sqrMagnitude < 0.001f)
            {
                return;
            }

            transform.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        }
    }
}
