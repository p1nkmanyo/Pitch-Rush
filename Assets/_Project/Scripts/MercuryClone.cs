using UnityEngine;

namespace PitchRush
{
    public class MercuryClone : MonoBehaviour
    {
        private Rigidbody rb;
        private PlayerController mainController;
        private float laneOffset; // -3f for left, 3f for right
        private bool isGrounded = true;

        public void Initialize(PlayerController controller, float offsetX)
        {
            mainController = controller;
            laneOffset = offsetX;
            rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.mass = 0.5f;
            }
        }

        private void FixedUpdate()
        {
            if (mainController == null || rb == null) return;

            // Follow main player forward position and speed
            float forwardSpeed = GameManager.Instance != null ? GameManager.Instance.CurrentSpeed : 10f;
            
            // Align target X relative to player's center lane offset
            float targetX = mainController.transform.position.x + laneOffset;

            // Clamp target X to track boundaries
            targetX = Mathf.Clamp(targetX, mainController.leftBoundary, mainController.rightBoundary);

            float xDiff = targetX - rb.position.x;
            float targetVelX = xDiff * 15f; // Damp steering

            rb.linearVelocity = new Vector3(targetVelX, rb.linearVelocity.y, forwardSpeed);
        }

        private void Update()
        {
            isGrounded = CheckGrounded();
        }

        public void CloneJump(float force)
        {
            if (rb == null || !isGrounded) return;
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * force, ForceMode.Impulse);
        }

        private bool CheckGrounded()
        {
            SphereCollider sphereCollider = GetComponent<SphereCollider>();
            float radius = sphereCollider != null ? sphereCollider.radius * transform.localScale.y : 0.25f;
            float castDistance = 0.15f;

            RaycastHit hit;
            if (Physics.SphereCast(transform.position, radius * 0.85f, Vector3.down, out hit, radius + castDistance))
            {
                if (hit.collider.CompareTag("Ground"))
                {
                    return true;
                }
            }
            return false;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Obstacle"))
            {
                SplashAndDestroy();
            }
        }

        public void SplashAndDestroy()
        {
            // Play splash VFX / scale down
            GameObject splash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            splash.transform.position = transform.position;
            splash.transform.localScale = Vector3.one * 0.4f;
            
            Renderer rend = splash.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                rend.material.color = new Color(0.8f, 0.8f, 0.8f); // Chrome color
                if (rend.material.HasProperty("_Metallic")) rend.material.SetFloat("_Metallic", 1f);
                if (rend.material.HasProperty("_Smoothness")) rend.material.SetFloat("_Smoothness", 0.9f);
            }

            Destroy(splash, 0.5f);

            // Notify main controller
            if (mainController != null)
            {
                mainController.OnCloneDestroyed(this);
            }

            Destroy(gameObject);
        }
    }
}
