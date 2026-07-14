using UnityEngine;

namespace PitchRush
{
    public class ColorContainer : MonoBehaviour
    {
        [Tooltip("The color that this pickup will apply to the slime.")]
        public Color color = Color.yellow;

        private void Start()
        {
            // Auto-color the visual mesh of the pickup at runtime (0 GC Alloc!)
            Renderer rend = GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                Material mat = rend.material;
                mat.color = color;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.7f);
            }
        }
    }
}
