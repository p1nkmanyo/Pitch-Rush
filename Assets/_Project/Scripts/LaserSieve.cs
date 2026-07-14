using System.Collections;
using UnityEngine;

namespace PitchRush
{
    public class LaserSieve : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                PlayerController player = other.GetComponent<PlayerController>();
                if (player != null)
                {
                    if (player.CurrentForm == BlobForm.HeavyIron)
                    {
                        Debug.Log("Heavy Iron ball hit Laser Sieve and shattered!");
                        GameManager.Instance.GameOver();
                    }
                    else
                    {
                        // Squeeze visual model to slide through lasers (0 GC Alloc when using local cache)
                        Transform vis = player.visualModel;
                        if (vis != null)
                        {
                            StartCoroutine(SqueezePassCoroutine(vis));
                        }
                    }
                }
            }
        }

        private IEnumerator SqueezePassCoroutine(Transform visual)
        {
            Vector3 origScale = visual.localScale;
            
            // Flatten Z (forward axis) and stretch Y/X to simulate extrusion
            Vector3 squeezedScale = new Vector3(origScale.x * 1.5f, origScale.y * 1.5f, origScale.z * 0.08f);
            visual.localScale = squeezedScale;

            float elapsed = 0f;
            float duration = 0.5f;
            while (elapsed < duration)
            {
                // Smoothly ease back to standard scale
                visual.localScale = Vector3.MoveTowards(visual.localScale, origScale, 4f * Time.deltaTime);
                yield return null;
                elapsed += Time.deltaTime;
            }
            visual.localScale = origScale;
        }
    }
}
