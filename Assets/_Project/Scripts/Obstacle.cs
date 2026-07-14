using UnityEngine;

namespace PitchRush
{
    public class Obstacle : MonoBehaviour
    {
        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                PlayerController player = collision.gameObject.GetComponent<PlayerController>();
                if (player != null && player.CurrentForm == BlobForm.HeavyIron)
                {
                    BreakObstacle(collision);
                    return;
                }

                // Notify GameManager that the player hit an obstacle (Game Over)
                GameManager.Instance.GameOver();
            }
        }

        private void BreakObstacle(Collision collision)
        {
            // Remove the obstacle tag so it doesn't trigger future collisions
            gameObject.tag = "Untagged";

            // Add Rigidbody dynamically if missing
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }

            rb.isKinematic = false;
            rb.useGravity = true;

            // Turn collider into trigger so player rolls through it smoothly
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            // Blast the obstacle forward and upward away from the player
            Vector3 contactPoint = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;
            Vector3 forceDirection = (transform.position - collision.transform.position).normalized;
            forceDirection.y = 0.5f; // Blast slightly upward for cinematic effect

            rb.AddForce(forceDirection * 25f, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * 15f, ForceMode.Impulse);

            // Trigger screen shake for heavy impact juice
            CameraFollow cam = Camera.main.GetComponent<CameraFollow>();
            if (cam != null)
            {
                cam.TriggerShake(0.3f, 0.22f);
            }

            // Optional: destroy the object after 2 seconds to keep the hierarchy clean
            Destroy(gameObject, 2f);
            
            Debug.Log("Obstacle smashed by Heavy Iron ball!");
        }
    }
}
