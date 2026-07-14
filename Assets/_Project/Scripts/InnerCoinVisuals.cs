using System.Collections.Generic;
using UnityEngine;

namespace PitchRush
{
    public class InnerCoinVisuals : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("The prefab used to represent a coin inside the ball.")]
        public GameObject miniCoinPrefab;
        [Tooltip("Maximum radius inside the ball where coins can orbit (sphere radius is 0.5).")]
        public float maxOrbitRadius = 0.38f;
        public float orbitSpeedMin = 60f;
        public float orbitSpeedMax = 120f;

        private GameObject container;
        private List<MiniCoinInstance> miniCoins = new List<MiniCoinInstance>();

        private struct MiniCoinInstance
        {
            public GameObject obj;
            public Vector3 orbitAxis;
            public float orbitSpeed;

            public MiniCoinInstance(GameObject obj, Vector3 orbitAxis, float orbitSpeed)
            {
                this.obj = obj;
                this.orbitAxis = orbitAxis;
                this.orbitSpeed = orbitSpeed;
            }
        }

        private void Start()
        {
            // Create a container that follows the player but does not rotate physically
            container = new GameObject("InnerCoinsContainer");
            container.transform.SetParent(transform);
            container.transform.localPosition = Vector3.zero;
            container.transform.localRotation = Quaternion.identity;
        }

        private void LateUpdate()
        {
            // Decouple container's rotation from the rolling player sphere
            if (container != null)
            {
                container.transform.rotation = Quaternion.identity;
            }
        }

        public void AddCoinVisual()
        {
            if (container == null) return;

            GameObject miniCoin = null;

            if (miniCoinPrefab != null)
            {
                miniCoin = Instantiate(miniCoinPrefab, container.transform);
            }
            else
            {
                // Fallback: Create a small golden sphere if no prefab is assigned
                miniCoin = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                miniCoin.transform.SetParent(container.transform);
                
                Renderer rend = miniCoin.GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    rend.material.color = new Color(1f, 0.84f, 0f); // Gold color
                }
                
                // Remove collider so it doesn't mess with physics
                Collider col = miniCoin.GetComponent<Collider>();
                if (col != null) Destroy(col);
            }

            // Set random position inside the sphere shell
            Vector3 randomDirection = Random.onUnitSphere;
            float randomRadius = Random.Range(0.1f, maxOrbitRadius);
            miniCoin.transform.localPosition = randomDirection * randomRadius;

            // Make it very small (e.g. 15% of player size)
            miniCoin.transform.localScale = Vector3.one * 0.15f;

            // Generate unique random orbit direction and speed
            Vector3 orbitAxis = Random.onUnitSphere;
            float orbitSpeed = Random.Range(orbitSpeedMin, orbitSpeedMax);

            miniCoins.Add(new MiniCoinInstance(miniCoin, orbitAxis, orbitSpeed));
        }

        private void Update()
        {
            if (container == null) return;

            // Orbit the coins inside the container around the container's center (player transform)
            for (int i = miniCoins.Count - 1; i >= 0; i--)
            {
                MiniCoinInstance coin = miniCoins[i];
                if (coin.obj != null)
                {
                    coin.obj.transform.RotateAround(container.transform.position, coin.orbitAxis, coin.orbitSpeed * Time.deltaTime);
                }
                else
                {
                    miniCoins.RemoveAt(i);
                }
            }
        }
    }
}
