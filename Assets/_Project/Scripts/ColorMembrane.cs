using UnityEngine;

namespace PitchRush
{
    public class ColorMembrane : MonoBehaviour
    {
        [Tooltip("The color required to safely pass through this membrane.")]
        public Color requiredColor = Color.yellow;

        private void Start()
        {
            // Auto-color the laser gate at runtime (0 GC Alloc!)
            Renderer rend = GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                Material mat = rend.material;
                mat.color = requiredColor;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", requiredColor);
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", requiredColor * 1.5f); // Neon glow
                    mat.EnableKeyword("_EMISSION");
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                PlayerController player = other.GetComponent<PlayerController>();
                if (player != null)
                {
                    Color playerColor = player.currentSlimeColor;
                    
                    // Allow small threshold buffer for color comparisons
                    bool matches = Mathf.Abs(playerColor.r - requiredColor.r) < 0.1f &&
                                   Mathf.Abs(playerColor.g - requiredColor.g) < 0.1f &&
                                   Mathf.Abs(playerColor.b - requiredColor.b) < 0.1f;

                    if (matches)
                    {
                        Debug.Log("Passed through color membrane safely!");
                        // Play a tiny chime or ignore damage
                    }
                    else
                    {
                        Debug.Log("Wrong color! Membrane blocked player.");
                        GameManager.Instance.GameOver();
                    }
                }
            }
        }
    }
}
