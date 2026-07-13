using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace PitchRush
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Lane Settings")]
        [Tooltip("0 = Left, 1 = Center, 2 = Right")]
        public int currentLane = 1;
        public float laneDistance = 3f; // Distance between lanes
        public float laneSwitchSpeed = 15f; // Constant speed for lane transition (caps velocity step)

        [Header("Jump & Input Settings")]
        public float jumpForce = 6f;
        public float swipeThreshold = 50f; // Minimum pixels to register a swipe

        [Header("Kill Zone")]
        [Tooltip("If the ball falls below this Y value, it's Game Over.")]
        public float killZoneY = -5f;

        private Rigidbody rb;

        // Pointer tracking logic
        private bool isSwiping = false;
        private Vector2 swipeStartPos;

        void Start()
        {
            rb = GetComponent<Rigidbody>();
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

            // Calculate target X position based on current lane
            // Center is 0. Left is -laneDistance. Right is +laneDistance.
            float targetX = (currentLane - 1) * laneDistance;

            // Calculate horizontal velocity needed to reach target lane with a constant speed snap
            float xDifference = targetX - rb.position.x;
            
            // To prevent sluggish deceleration, we move with constant max speed (laneSwitchSpeed)
            // but slow down in the final frame to avoid overshoot.
            float targetVelocityX = Mathf.Sign(xDifference) * Mathf.Min(Mathf.Abs(xDifference) / Time.fixedDeltaTime, laneSwitchSpeed);

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

            // 1. TOUCH & MOUSE SWIPE / DRAG INPUT
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
                    // A. Instant Swipe Detection (during the drag, not waiting for release)
                    if (isSwiping)
                    {
                        Vector2 delta = currentPointerPos - swipeStartPos;
                        if (delta.magnitude >= swipeThreshold)
                        {
                            ProcessSwipe(swipeStartPos, currentPointerPos);
                            isSwiping = false; // Prevent multiple swipes in one continuous drag gesture
                        }
                    }

                    // B. Direct Screen-Position Steering (Smooth Drag)
                    // Allows mobile players to drag their finger (or PC mouse) to steer smoothly across lanes
                    float screenRatio = currentPointerPos.x / Screen.width;
                    if (screenRatio < 0.35f)
                    {
                        currentLane = 0; // Left Lane
                    }
                    else if (screenRatio > 0.65f)
                    {
                        currentLane = 2; // Right Lane
                    }
                    else
                    {
                        currentLane = 1; // Center Lane
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
                if (Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame)
                {
                    ChangeLane(-1);
                }
                else if (Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame)
                {
                    ChangeLane(1);
                }

                if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame)
                {
                    if (IsGrounded()) Jump();
                }
            }
        }

        private void ProcessSwipe(Vector2 start, Vector2 end)
        {
            Vector2 delta = end - start;

            if (delta.magnitude < swipeThreshold)
                return;

            // Determine swipe direction
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                // Horizontal Swipe
                if (delta.x > 0)
                {
                    ChangeLane(1); // Swipe Right
                }
                else
                {
                    ChangeLane(-1); // Swipe Left
                }
            }
            else
            {
                // Vertical Swipe
                if (delta.y > 0 && IsGrounded())
                {
                    Jump(); // Swipe Up
                }
            }
        }

        private void ChangeLane(int direction)
        {
            currentLane += direction;
            currentLane = Mathf.Clamp(currentLane, 0, 2);
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
