using UnityEngine;

namespace PitchRush
{
    public class Obstacle : MonoBehaviour
    {
        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                // Notify GameManager that the player hit an obstacle (Game Over)
                GameManager.Instance.GameOver();
            }
        }
    }
}
