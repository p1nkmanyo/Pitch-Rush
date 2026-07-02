using UnityEngine;

namespace PitchRush
{
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target to Follow")]
        public Transform target;

        [Header("Offset & Positioning")]
        public Vector3 offset = new Vector3(0f, 5f, -8f);
        public float smoothSpeed = 10f;

        [Header("Rotation Settings")]
        public bool lookAtTarget = true;
        public Vector3 defaultRotation = new Vector3(25f, 0f, 0f);

        private void Start()
        {
            if (!lookAtTarget)
            {
                transform.rotation = Quaternion.Euler(defaultRotation);
            }
        }

        private void LateUpdate()
        {
            if (target == null)
                return;

            // Target position based on offset
            Vector3 desiredPosition = target.position + offset;

            // Smoothly move the camera towards that desired position
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
            transform.position = smoothedPosition;

            if (lookAtTarget)
            {
                // Smooth look at
                Quaternion targetRotation = Quaternion.LookRotation(target.position - transform.position);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, smoothSpeed * Time.deltaTime);
            }
            else
            {
                // Maintain fixed rotation relative to the target's forward (or just world axis)
                // We'll keep it simple: just maintain the default pitch rotation.
                transform.rotation = Quaternion.Euler(defaultRotation);
            }
        }
    }
}
