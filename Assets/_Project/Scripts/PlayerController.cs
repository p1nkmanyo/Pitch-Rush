using UnityEngine;
using UnityEngine.InputSystem;

namespace PitchRush
{
    public enum BallState { Normal, HeavyIron, LightPingPong }

    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float baseForwardSpeed = 10f;
        public float maxForwardSpeed = 30f;
        public float speedIncreaseRate = 0.1f; // How much speed increases per second
        public float horizontalSpeed = 20f;
        public float leftBoundary = -4.5f;
        public float rightBoundary = 4.5f;

        [Header("Jump & Physics Settings")]
        public float jumpVelocity = 8f; // Using velocity for consistent jump heights regardless of mass
        public float fallMultiplier = 2.5f; // Makes the ball fall faster (snappy jump)
        public float swipeUpThreshold = 50f;

        [Header("State Settings")]
        public BallState currentState = BallState.Normal;
        private Vector3 defaultScale;
        private float defaultMass;

        private Rigidbody rb;
        private bool isGrounded = true;
        private float currentForwardSpeed;

        // Pointer tracking logic
        private float targetX;
        private bool isHolding = false;
        private Vector2 pointerStartPos;

        void Start()
        {
            rb = GetComponent<Rigidbody>();
            targetX = transform.position.x;
            currentForwardSpeed = baseForwardSpeed;
            defaultScale = transform.localScale;
            defaultMass = rb.mass;
        }

        public void ChangeState(BallState newState)
        {
            currentState = newState;
            switch (currentState)
            {
                case BallState.Normal:
                    transform.localScale = defaultScale;
                    rb.mass = defaultMass;
                    break;
                case BallState.HeavyIron:
                    transform.localScale = defaultScale * 2f; // Big ball
                    rb.mass = defaultMass * 5f; // Heavy
                    break;
                case BallState.LightPingPong:
                    transform.localScale = defaultScale * 0.5f; // Small ball
                    rb.mass = defaultMass * 0.2f; // Light
                    break;
            }
        }

        void Update()
        {
            HandleInput();
            UpdateSpeed();
        }

        void FixedUpdate()
        {
            // Move forward automatically
            Vector3 forwardMovement = transform.forward * currentForwardSpeed * Time.fixedDeltaTime;

            // Smoothly move X position towards the target X
            float newX = Mathf.Lerp(rb.position.x, targetX, horizontalSpeed * Time.fixedDeltaTime);

            // Apply Custom Gravity (Fall Faster)
            if (rb.linearVelocity.y < 0)
            {
                rb.linearVelocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
            }

            // Combine forward and horizontal movement
            Vector3 newPosition = new Vector3(newX, rb.position.y, rb.position.z) + forwardMovement;
            rb.MovePosition(newPosition);
        }

        private void UpdateSpeed()
        {
            // Gradually increase speed over time up to max speed
            if (currentForwardSpeed < maxForwardSpeed)
            {
                currentForwardSpeed += speedIncreaseRate * Time.deltaTime;
            }
        }

        public float GetCurrentSpeed()
        {
            return currentForwardSpeed;
        }

        private void HandleInput()
        {
            // Read Pointer (Mouse click & drag, or Touch on mobile)
            if (Pointer.current != null)
            {
                if (Pointer.current.press.wasPressedThisFrame)
                {
                    isHolding = true;
                    pointerStartPos = Pointer.current.position.ReadValue();
                }
                else if (Pointer.current.press.wasReleasedThisFrame)
                {
                    isHolding = false;
                }

                if (isHolding)
                {
                    Vector2 currentPointerPos = Pointer.current.position.ReadValue();

                    // 1. HORIZONTAL TRACKING
                    float normalizedX = (currentPointerPos.x / Screen.width) * 2f - 1f;
                    targetX = Mathf.Lerp(leftBoundary, rightBoundary, (normalizedX + 1f) / 2f);
                    targetX = Mathf.Clamp(targetX, leftBoundary, rightBoundary);

                    // 2. JUMP TRACKING (Vertical Swipe)
                    float deltaY = currentPointerPos.y - pointerStartPos.y;
                    if (deltaY > swipeUpThreshold && isGrounded)
                    {
                        Jump();
                        pointerStartPos = currentPointerPos;
                    }
                }
            }

            // Keyboard fallback for jumping in editor
            if (Keyboard.current != null)
            {
                if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame)
                {
                    if (isGrounded)
                    {
                        Jump();
                    }
                }

                // Optional keyboard steering fallback
                float keyboardX = 0f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) keyboardX = -1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) keyboardX = 1f;

                if (Mathf.Abs(keyboardX) > 0.1f)
                {
                    targetX += keyboardX * horizontalSpeed * Time.deltaTime;
                    targetX = Mathf.Clamp(targetX, leftBoundary, rightBoundary);
                }
            }
        }

        private void Jump()
        {
            // Zero out vertical velocity before jumping to ensure consistent jump height
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpVelocity, ForceMode.VelocityChange);
            isGrounded = false;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Ground"))
            {
                isGrounded = true;
            }
        }
    }
}
