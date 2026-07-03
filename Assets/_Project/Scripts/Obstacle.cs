using UnityEngine;

namespace PitchRush
{
    public class Obstacle : MonoBehaviour
    {
        public bool isDestructibleByIron = true;

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                PlayerController player = collision.gameObject.GetComponent<PlayerController>();
                if (player != null && player.currentState == BallState.HeavyIron && isDestructibleByIron)
                {
                    // Iron ball destroys the obstacle!
                    // Play explosion effect here
                    Destroy(gameObject);
                }
                else
                {
                    // Normal or PingPong ball dies
                    GameManager.Instance.GameOver();
                }
            }
        }
    }
}
