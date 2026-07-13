using UnityEngine;

namespace PitchRush
{
    public class Collectible : MonoBehaviour
    {
        public int coinValue = 1;
        public float rotateSpeed = 100f;

        private bool isCollected = false;

        // Reset flag when re-enabled from the object pool
        private void OnEnable()
        {
            isCollected = false;
        }

        void Update()
        {
            // Rotate the coin for visual effect
            transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            // Guard against double-collection within the same physics frame
            if (isCollected) return;

            if (other.CompareTag("Player"))
            {
                isCollected = true;

                // Add coin value
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.AddCoin(coinValue);
                }

                // Play sound effect or particle effect here if available

                // Instead of destroying, we disable it so it can be reused in the object pool
                gameObject.SetActive(false);
            }
        }
    }
}
