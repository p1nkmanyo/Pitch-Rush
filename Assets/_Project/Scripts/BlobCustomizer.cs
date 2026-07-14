using UnityEngine;

namespace PitchRush
{
    public class BlobCustomizer : MonoBehaviour
    {
        [Header("Materials for Forms")]
        [Tooltip("Material for the default squishy Blob/Slime form.")]
        public Material defaultMaterial;
        [Tooltip("Material for the Heavy Iron ball form.")]
        public Material heavyIronMaterial;
        [Tooltip("Material for the Light Ping-Pong ball form.")]
        public Material lightPingPongMaterial;

        [Header("Trails for Forms")]
        [Tooltip("Trail object or particle system for the default form.")]
        public GameObject defaultTrailPrefab;
        [Tooltip("Trail object or particle system for the Heavy Iron form.")]
        public GameObject heavyTrailPrefab;
        [Tooltip("Trail object or particle system for the Light Ping-Pong form.")]
        public GameObject lightTrailPrefab;

        private Renderer playerRenderer;
        private GameObject currentTrailInstance;

        private void Awake()
        {
            // Cache the renderer from the visual model
            PlayerController controller = GetComponent<PlayerController>();
            if (controller != null && controller.visualModel != null)
            {
                playerRenderer = controller.visualModel.GetComponent<Renderer>();
            }
            else
            {
                playerRenderer = GetComponentInChildren<Renderer>();
            }
        }

        public void ApplyFormVisuals(BlobForm form)
        {
            if (playerRenderer == null)
            {
                // Fallback cache if not resolved in Awake
                playerRenderer = GetComponentInChildren<Renderer>();
            }

            // Apply Material
            if (playerRenderer != null)
            {
                switch (form)
                {
                    case BlobForm.Default:
                        if (defaultMaterial != null) playerRenderer.material = defaultMaterial;
                        break;
                    case BlobForm.HeavyIron:
                        if (heavyIronMaterial != null) playerRenderer.material = heavyIronMaterial;
                        break;
                    case BlobForm.LightPingPong:
                        if (lightPingPongMaterial != null) playerRenderer.material = lightPingPongMaterial;
                        break;
                }
            }

            // Apply Trail/VFX
            UpdateTrail(form);
        }

        private void UpdateTrail(BlobForm form)
        {
            // Destroy current trail instance
            if (currentTrailInstance != null)
            {
                Destroy(currentTrailInstance);
            }

            GameObject trailPrefab = null;

            switch (form)
            {
                case BlobForm.Default:
                    trailPrefab = defaultTrailPrefab;
                    break;
                case BlobForm.HeavyIron:
                    trailPrefab = heavyTrailPrefab;
                    break;
                case BlobForm.LightPingPong:
                    trailPrefab = lightTrailPrefab;
                    break;
            }

            if (trailPrefab != null)
            {
                currentTrailInstance = Instantiate(trailPrefab, transform.position, Quaternion.identity, transform);
                
                // Position offset: place slightly at the bottom/back of the ball
                currentTrailInstance.transform.localPosition = new Vector3(0f, -0.2f, -0.2f);
            }
        }
    }
}
