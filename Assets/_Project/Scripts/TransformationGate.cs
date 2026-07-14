using UnityEngine;

namespace PitchRush
{
    public class TransformationGate : MonoBehaviour
    {
        [Header("Settings")]
        public BlobForm targetForm;
        public float visualRotateSpeed = 45f;
        public GameObject gateCoreVisual;

        private void Update()
        {
            // Spin the inner ring or core of the gate for aesthetic look
            if (gateCoreVisual != null)
            {
                gateCoreVisual.transform.Rotate(Vector3.up * visualRotateSpeed * Time.deltaTime);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                PlayerController controller = other.GetComponent<PlayerController>();
                if (controller != null)
                {
                    controller.SetForm(targetForm);
                    PlayGateFX(other.transform.position);
                }
            }
        }

        private void PlayGateFX(Vector3 playerPos)
        {
            // Optional: Play a sound or particles on gate pass
            Debug.Log($"Player morphed into: {targetForm}");
        }
    }
}
