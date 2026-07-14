using UnityEngine;

namespace PitchRush
{
    public class BuffItem : MonoBehaviour
    {
        public BuffSettings settings;
        public float rotateSpeed = 100f;

        private bool isCollected = false;

        private void OnEnable()
        {
            isCollected = false;
        }

        private void Start()
        {
            // Auto-assign color and gloss based on BuffType at runtime
            Renderer rend = GetComponentInChildren<Renderer>();
            if (rend != null && settings != null)
            {
                Material mat = rend.material; // Instances the material safely
                Color targetColor = Color.white;

                switch (settings.buffType)
                {
                    case BuffType.Magnet:
                        targetColor = new Color(0f, 0.5f, 1f); // Cyan/Blue
                        break;
                    case BuffType.Shield:
                        targetColor = new Color(0.1f, 0.9f, 0.2f); // Emerald Green
                        break;
                    case BuffType.SpeedBoost:
                        targetColor = new Color(1f, 0.7f, 0f); // Golden Yellow
                        break;
                    case BuffType.ChronoRewind:
                        targetColor = new Color(0.65f, 0.1f, 1f); // Neon Purple
                        break;
                }

                mat.color = targetColor;
                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", targetColor);
                }

                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.8f);
                else if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.8f);
            }
        }

        void Update()
        {
            // Rotate the item for visual effect
            transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isCollected) return;

            if (other.CompareTag("Player"))
            {
                BuffManager buffManager = other.GetComponent<BuffManager>();
                if (buffManager != null)
                {
                    isCollected = true;
                    buffManager.ApplyBuff(settings);

                    // Instead of destroying, disable it so it works with track recycling
                    gameObject.SetActive(false);
                }
            }
        }
    }
}
