using UnityEngine;

namespace PitchRush
{
    public class PowerUp : MonoBehaviour
    {
        public BallState stateToApply = BallState.HeavyIron;
        public float duration = 5f;
        public float rotationSpeed = 100f;

        void Update()
        {
            // Spin visual
            transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                PlayerController player = other.GetComponent<PlayerController>();
                if (player != null)
                {
                    player.ChangeState(stateToApply);

                    // Tell GameManager to reset state after duration
                    GameManager.Instance.StartPowerUpTimer(player, duration);
                }

                // Play particle effect/sound here
                Destroy(gameObject);
            }
        }
    }
}
