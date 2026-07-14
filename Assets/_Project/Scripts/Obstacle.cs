using UnityEngine;

namespace PitchRush
{
    public class Obstacle : MonoBehaviour
    {
        [Header("Prefractured Visual Optimization")]
        public GameObject intactVisual;
        public GameObject fracturedVisual;

        private Vector3[] shardLocalPositions;
        private Quaternion[] shardLocalRotations;
        private Collider mainCollider;

        private void Awake()
        {
            mainCollider = GetComponent<Collider>();

            // Cache original shard positions for reuse in object pooling
            if (fracturedVisual != null)
            {
                int childCount = fracturedVisual.transform.childCount;
                shardLocalPositions = new Vector3[childCount];
                shardLocalRotations = new Quaternion[childCount];
                for (int i = 0; i < childCount; i++)
                {
                    Transform child = fracturedVisual.transform.GetChild(i);
                    shardLocalPositions[i] = child.localPosition;
                    shardLocalRotations[i] = child.localRotation;

                    // Ensure shard rigidbodies exist
                    Rigidbody shardRb = child.GetComponent<Rigidbody>();
                    if (shardRb == null)
                    {
                        shardRb = child.gameObject.AddComponent<Rigidbody>();
                    }

                    // Shards should ignore collision with player to optimize physics
                    Collider shardCol = child.GetComponent<Collider>();
                    if (shardCol != null && GameManager.Instance != null && GameManager.Instance.playerTransform != null)
                    {
                        Collider playerCol = GameManager.Instance.playerTransform.GetComponent<Collider>();
                        if (playerCol != null) Physics.IgnoreCollision(shardCol, playerCol);
                    }
                }
            }
        }

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

            // Disable main collider to instantly drop it from physics scene queries
            if (mainCollider != null)
            {
                mainCollider.enabled = false;
            }

            // Trigger screen shake for heavy impact juice
            CameraFollow cam = Camera.main.GetComponent<CameraFollow>();
            if (cam != null)
            {
                cam.TriggerShake(0.3f, 0.22f);
            }

            Vector3 forceDirection = (transform.position - collision.transform.position).normalized;
            forceDirection.y = 0.5f; // Blast upward

            if (intactVisual != null && fracturedVisual != null)
            {
                // Visual swap (Prefractured Toggle - 0 GC Alloc!)
                intactVisual.SetActive(false);
                fracturedVisual.SetActive(true);

                // Add physical force to each shard
                int childCount = fracturedVisual.transform.childCount;
                for (int i = 0; i < childCount; i++)
                {
                    Transform child = fracturedVisual.transform.GetChild(i);
                    Rigidbody shardRb = child.GetComponent<Rigidbody>();
                    if (shardRb != null)
                    {
                        shardRb.isKinematic = false;
                        shardRb.AddForce(forceDirection * Random.Range(15f, 25f), ForceMode.Impulse);
                        shardRb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.Impulse);
                    }
                }
            }
            else
            {
                // Fallback: dynamic physics on whole object if prefractured visual is not assigned
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = gameObject.AddComponent<Rigidbody>();
                }
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.AddForce(forceDirection * 25f, ForceMode.Impulse);
            }

            Debug.Log("Obstacle smashed by Heavy Iron ball!");
        }

        public void ResetObstacle()
        {
            gameObject.tag = "Obstacle";

            if (mainCollider != null)
            {
                mainCollider.enabled = true;
                mainCollider.isTrigger = false;
            }

            if (intactVisual != null)
            {
                intactVisual.SetActive(true);
            }

            if (fracturedVisual != null)
            {
                fracturedVisual.SetActive(false);

                // Reset position, rotation, and velocities of shards
                int childCount = fracturedVisual.transform.childCount;
                for (int i = 0; i < childCount; i++)
                {
                    Transform child = fracturedVisual.transform.GetChild(i);
                    child.localPosition = shardLocalPositions[i];
                    child.localRotation = shardLocalRotations[i];

                    Rigidbody shardRb = child.GetComponent<Rigidbody>();
                    if (shardRb != null)
                    {
                        shardRb.linearVelocity = Vector3.zero;
                        shardRb.angularVelocity = Vector3.zero;
                        shardRb.isKinematic = true;
                    }
                }
            }
            else
            {
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    transform.localRotation = Quaternion.identity;
                }
            }
        }
    }
}
