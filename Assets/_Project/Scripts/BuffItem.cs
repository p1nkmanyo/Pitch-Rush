using UnityEngine;

namespace PitchRush
{
    public class BuffItem : MonoBehaviour
    {
        public BuffSettings settings;
        public float rotateSpeed = 100f;

        private bool isCollected = false;

        private void OnEnable()
        {
            isCollected = false;
        }

        void Update()
        {
            // Rotate the item for visual effect
            transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isCollected) return;

            if (other.CompareTag("Player"))
            {
                BuffManager buffManager = other.GetComponent<BuffManager>();
                if (buffManager != null)
                {
                    isCollected = true;
                    buffManager.ApplyBuff(settings);

                    // Instead of destroying, disable it so it works with track recycling
                    gameObject.SetActive(false);
                }
            }
        }
    }
}
