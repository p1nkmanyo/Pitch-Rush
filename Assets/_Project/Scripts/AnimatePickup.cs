using UnityEngine;

namespace PitchRush
{
    public class AnimatePickup : MonoBehaviour
    {
        [Header("Hover Settings")]
        public float hoverSpeed = 2f;      // Speed of bobbing up and down
        public float hoverAmount = 0.15f;   // Amplitude of bobbing (height)

        [Header("Rotation Settings")]
        public Vector3 rotationSpeed = new Vector3(0f, 100f, 0f); // Direction and speed of rotation

        private Vector3 startPosition;
        private float randomOffset;
        private bool isInitialized = false;

        void OnEnable()
        {
            // Reset start position when enabled (critical for object pooled track segments)
            startPosition = transform.localPosition;
            randomOffset = Random.Range(0f, 100f);
            isInitialized = true;
        }

        void Start()
        {
            if (!isInitialized)
            {
                startPosition = transform.localPosition;
                randomOffset = Random.Range(0f, 100f);
                isInitialized = true;
            }
        }

        void Update()
        {
            // Smooth float up and down using sine wave
            float newY = startPosition.y + Mathf.Sin(Time.time * hoverSpeed + randomOffset) * hoverAmount;
            transform.localPosition = new Vector3(transform.localPosition.x, newY, transform.localPosition.z);

            // Smooth rotation
            transform.Rotate(rotationSpeed * Time.deltaTime);
        }
    }
}
