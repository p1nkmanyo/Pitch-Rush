using UnityEngine;

namespace PitchRush
{
    public class PowerUp : MonoBehaviour
    {
        [Header("Power Up Settings")]
        public BallState powerUpState; // HeavyIron or LightPingPong
        public float duration = 10f;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                PlayerController player = other.GetComponent<PlayerController>();
                if (player != null)
                {
                    player.ChangeState(powerUpState);

                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.StartPowerUpTimer(player, duration);
                    }

                    // Add visual/audio effect here if desired
                    Destroy(gameObject);
                }
            }
        }
    }
}
