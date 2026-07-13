using UnityEngine;

namespace PitchRush
{
    public class Collectible : MonoBehaviour
    {
        public int coinValue = 1;
        public float rotateSpeed = 100f;

        void Update()
        {
            // Rotate the coin for visual effect
            transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
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
