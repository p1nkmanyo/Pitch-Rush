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

        private Vector3 currentCameraUp = Vector3.up;

        private void Start()
        {
            currentCameraUp = Vector3.up;
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

            // Smoothly calculate target up vector (for Wall Run or Ceiling Gravity)
            Vector3 targetUp = Vector3.up;
            PlayerController player = target.GetComponent<PlayerController>();
            if (player != null)
            {
                if (player.IsWallRunning)
                {
                    // Tilt by 45 degrees towards the wall normal for dynamic racing roll
                    targetUp = (Vector3.up + player.WallRunNormal).normalized;
                }
                else if (player.IsCeilingGravityActive)
                {
                    // Invert completely
                    targetUp = Vector3.down;
                }
            }

            // Interpolate camera up vector to avoid sudden jarring rotation
            currentCameraUp = Vector3.Slerp(currentCameraUp, targetUp, 4f * Time.deltaTime);

            // Target position based on offset, rotated by current track direction (supports turns!)
            Vector3 dynamicOffset = offset;
            if (player != null && player.IsCeilingGravityActive)
            {
                dynamicOffset.y = -offset.y;
            }

            // Rotate offset to follow behind the player even after turns
            TrackManager tm = FindAnyObjectByType<TrackManager>();
            Quaternion trackRot = (tm != null) ? tm.CurrentRotation : Quaternion.identity;
            Vector3 rotatedOffset = trackRot * dynamicOffset;

            Vector3 desiredPosition = target.position + rotatedOffset;

            // Smoothly move the camera towards that desired position and add the shake offset
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime) + shakeOffset;
            transform.position = smoothedPosition;

            if (lookAtTarget)
            {
                // Smooth look at with dynamic up vector
                Vector3 lookDirection = target.position - transform.position;
                if (lookDirection != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(lookDirection, currentCameraUp);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, smoothSpeed * Time.deltaTime);
                }
            }
            else
            {
                // Maintain rotation relative to custom up vector
                transform.rotation = Quaternion.Euler(defaultRotation);
            }
        }
    }
}
