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

        // Screen Shake variables
        private float shakeDuration = 0f;
        private float shakeMagnitude = 0.1f;
        private Vector3 shakeOffset = Vector3.zero;

        private void Start()
        {
            if (!lookAtTarget)
            {
                transform.rotation = Quaternion.Euler(defaultRotation);
            }
        }

        public void TriggerShake(float duration, float magnitude)
        {
            shakeDuration = duration;
            shakeMagnitude = magnitude;
        }

        private void LateUpdate()
        {
            if (target == null)
                return;

            // Handle Screen Shake
            if (shakeDuration > 0f)
            {
                shakeOffset = Random.insideUnitSphere * shakeMagnitude;
                shakeDuration -= Time.deltaTime;
            }
            else
            {
                shakeOffset = Vector3.zero;
            }

            // Target position based on offset
            Vector3 desiredPosition = target.position + offset;

            // Smoothly move the camera towards that desired position and add the shake offset
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime) + shakeOffset;
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
                transform.rotation = Quaternion.Euler(defaultRotation);
            }
        }
    }
}
