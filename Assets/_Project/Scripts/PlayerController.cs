using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace PitchRush
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float leftBoundary = -4.5f;
        public float rightBoundary = 4.5f;
        public float horizontalSpeed = 15f; // Damping speed for steering

        [Header("Jump & Input Settings")]
        public float jumpForce = 6f;
        public float swipeThreshold = 50f; // Minimum pixels to register a swipe for jumping

        [Header("Kill Zone")]
        [Tooltip("If the ball falls below this Y value, it's Game Over.")]
        public float killZoneY = -5f;

        private Rigidbody rb;
        private float targetX;

        // Pointer tracking logic
        private bool isSwiping = false;
        private Vector2 swipeStartPos;

        void Start()
        {
            rb = GetComponent<Rigidbody>();
            targetX = transform.position.x;
        }

        void Update()
        {
            HandleInput();
            CheckKillZone();
        }

        void FixedUpdate()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsGameActive)
                return;

            // Calculate forward speed from GameManager's progression
            float currentForwardSpeed = GameManager.Instance != null ? GameManager.Instance.CurrentSpeed : 10f;

            // Calculate horizontal velocity needed to reach target X
            float xDifference = targetX - rb.position.x;
            float targetVelocityX = xDifference * horizontalSpeed;

            // Set velocity directly: horizontal + forward are controlled, Y is left to physics (gravity + jump)
            rb.linearVelocity = new Vector3(targetVelocityX, rb.linearVelocity.y, currentForwardSpeed);
        }

        private void CheckKillZone()
        {
            // If the ball has fallen below the kill zone threshold, trigger Game Over
            if (transform.position.y < killZoneY)
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.GameOver();
                }
            }
        }

        private void HandleInput()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsGameActive)
                return;

            // 1. TOUCH & MOUSE INPUT (Smooth steering + vertical swipe for jump)
            if (Pointer.current != null)
            {
                Vector2 currentPointerPos = Pointer.current.position.ReadValue();

                if (Pointer.current.press.wasPressedThisFrame)
                {
                    // Prevent input bleed through UI
                    if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                        return;

                    if (EventSystem.current != null && Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
                    {
                        if (EventSystem.current.IsPointerOverGameObject(Touchscreen.current.touches[0].touchId.ReadValue()))
                            return;
                    }

                    isSwiping = true;
                    swipeStartPos = currentPointerPos;
                }

                if (Pointer.current.press.isPressed)
                {
                    Vector2 delta = currentPointerPos - swipeStartPos;
                    bool isVerticalSwipeIntent = isSwiping && Mathf.Abs(delta.y) > Mathf.Abs(delta.x) && delta.y > 10f;

                    // A. Vertical Swipe Detection for Jump (instant during drag)
                    if (isSwiping)
                    {
                        // We only register jump if they swipe UP specifically
                        if (delta.y > swipeThreshold && Mathf.Abs(delta.y) > Mathf.Abs(delta.x))
                        {
                            if (IsGrounded())
                            {
                                Jump();
                            }
                            isSwiping = false; // Prevent multiple jumps in one gesture
                        }
                    }

                    // B. Direct Screen-Position Steering (Smooth Continuous Drag)
                    // Map screen X coordinate (0 to Screen.width) to world boundaries (leftBoundary to rightBoundary)
                    // Skip steering updates during a vertical swipe gesture to prevent horizontal drift/centering
                    if (!isVerticalSwipeIntent)
                    {
                        float normalizedX = currentPointerPos.x / Screen.width;
                        targetX = Mathf.Lerp(leftBoundary, rightBoundary, normalizedX);
                        targetX = Mathf.Clamp(targetX, leftBoundary, rightBoundary);
                    }
                }

                if (Pointer.current.press.wasReleasedThisFrame)
                {
                    isSwiping = false;
                }
            }

            // 2. KEYBOARD INPUT (Fallback/Testing)
            if (Keyboard.current != null)
            {
                // Steering fallback
                float keyboardX = 0f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) keyboardX = -1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) keyboardX = 1f;

                if (Mathf.Abs(keyboardX) > 0.1f)
                {
                    targetX += keyboardX * horizontalSpeed * Time.deltaTime;
                    targetX = Mathf.Clamp(targetX, leftBoundary, rightBoundary);
                }

                // Jump fallback
                if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame)
                {
                    if (IsGrounded()) Jump();
                }
            }
        }

        private void Jump()
        {
            // Zero out vertical velocity before jumping to ensure consistent jump height
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        private bool IsGrounded()
        {
            SphereCollider sphereCollider = GetComponent<SphereCollider>();
            float radius = sphereCollider != null ? sphereCollider.radius * transform.localScale.y : 0.5f;
            float castDistance = 0.15f; // Small buffer beneath the ball

            // SphereCast down to detect the "Ground" tag, ignoring the player's own collider
            RaycastHit hit;
            if (Physics.SphereCast(transform.position, radius * 0.9f, Vector3.down, out hit, radius + castDistance))
            {
                if (hit.collider.CompareTag("Ground"))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
