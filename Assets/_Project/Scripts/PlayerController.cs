using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace PitchRush
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float forwardSpeed = 10f;
        public float horizontalSpeed = 20f; // Increased for snappier follow
        public float leftBoundary = -4.5f;
        public float rightBoundary = 4.5f;

        [Header("Jump Settings")]
        public float jumpForce = 6f;
        public float swipeUpThreshold = 50f; // How far to swipe up to jump

        private Rigidbody rb;
        private bool isGrounded = true;

        // Pointer tracking logic
        private float targetX;
        private bool isHolding = false;
        private Vector2 pointerStartPos;

        void Start()
        {
            rb = GetComponent<Rigidbody>();
            targetX = transform.position.x;
        }

        void Update()
        {
            HandleInput();
        }

        void FixedUpdate()
        {
            // Move forward automatically
            Vector3 forwardMovement = transform.forward * forwardSpeed * Time.fixedDeltaTime;

            // Smoothly move X position towards the target X
            float newX = Mathf.Lerp(rb.position.x, targetX, horizontalSpeed * Time.fixedDeltaTime);

            // Combine forward and horizontal movement
            Vector3 newPosition = new Vector3(newX, rb.position.y, rb.position.z) + forwardMovement;
            rb.MovePosition(newPosition);
        }

        private void HandleInput()
        {
            // Read Pointer (Mouse click & drag, or Touch on mobile)
            if (Pointer.current != null)
            {
                if (Pointer.current.press.wasPressedThisFrame)
                {
                    // Check if pointer is over UI element
                    if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                    {
                        return;
                    }

                    // For touch input, we can rely on Pointer.current.pointerId with IsPointerOverGameObject
                    // (IsPointerOverGameObject without params checks the primary pointer/mouse,
                    // passing pointerId checks the specific touch)
                    if (EventSystem.current != null && Pointer.current is UnityEngine.InputSystem.Controls.TouchControl touch)
                    {
                        if (EventSystem.current.IsPointerOverGameObject(touch.touchId.ReadValue()))
                        {
                            return;
                        }
                    }
                    else if (EventSystem.current != null && Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
                    {
                        if (EventSystem.current.IsPointerOverGameObject(Touchscreen.current.touches[0].touchId.ReadValue()))
                        {
                            return;
                        }
                    }

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
                    // Convert screen X coordinate (0 to Screen.width) to a normalized value (-1 to 1)
                    float normalizedX = (currentPointerPos.x / Screen.width) * 2f - 1f;

                    // Map the normalized value to our world boundaries
                    // If pointer is at left edge, target is leftBoundary. If at right edge, rightBoundary.
                    targetX = Mathf.Lerp(leftBoundary, rightBoundary, (normalizedX + 1f) / 2f);
                    // Ensure we don't go out of bounds
                    targetX = Mathf.Clamp(targetX, leftBoundary, rightBoundary);

                    // 2. JUMP TRACKING (Vertical Swipe)
                    float deltaY = currentPointerPos.y - pointerStartPos.y;
                    if (deltaY > swipeUpThreshold && isGrounded)
                    {
                        Jump();
                        // Reset start pos so it doesn't jump multiple times from one long swipe
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
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
        }

        private void OnCollisionEnter(Collision collision)
        {
            // Simple check to see if we hit the ground
            if (collision.gameObject.CompareTag("Ground"))
            {
                isGrounded = true;
            }
        }
    }
}
