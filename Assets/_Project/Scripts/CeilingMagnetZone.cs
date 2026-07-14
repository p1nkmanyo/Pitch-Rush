using UnityEngine;

namespace PitchRush
{
    public class CeilingMagnetZone : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("The upward force applied to snap the Heavy Iron ball to the ceiling.")]
        public float magnetForce = 25f;

        private void OnTriggerStay(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                PlayerController controller = other.GetComponent<PlayerController>();
                if (controller != null)
                {
                    // Magnet only works on the HeavyIron ball form
                    if (controller.CurrentForm == BlobForm.HeavyIron)
                    {
                        controller.SetCeilingGravity(true, magnetForce);
                    }
                    else
                    {
                        // Other forms lose grip and fall back to the ground
                        controller.SetCeilingGravity(false, 0f);
                    }
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                PlayerController controller = other.GetComponent<PlayerController>();
                if (controller != null)
                {
                    controller.SetCeilingGravity(false, 0f);
                }
            }
        }
    }
}
