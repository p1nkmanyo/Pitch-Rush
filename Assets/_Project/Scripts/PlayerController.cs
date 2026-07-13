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
        public float laneSwitchSpeed = 15f; // How fast the ball snaps to the target lane

        [Header("Jump & Input Settings")]
        public float jumpForce = 6f;
        public float swipeThreshold = 50f; // Minimum pixels to register a swipe

        [Header("Kill Zone")]
        [Tooltip("If the ball falls below this Y value, it's Game Over.")]
        public float killZoneY = -5f;

        private Rigidbody rb;
        private bool isGrounded = true;

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

            // Calculate horizontal velocity needed to reach target lane
            float xDifference = targetX - rb.position.x;
            float horizontalVelocity = xDifference * laneSwitchSpeed;

            // Set velocity directly: horizontal + forward are controlled, Y is left to physics (gravity + jump)
            rb.linearVelocity = new Vector3(horizontalVelocity, rb.linearVelocity.y, currentForwardSpeed);
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

            // 1. TOUCH & MOUSE SWIPE INPUT
            if (Pointer.current != null)
            {
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
                    swipeStartPos = Pointer.current.position.ReadValue();
                }
                else if (Pointer.current.press.wasReleasedThisFrame)
                {
                    if (isSwiping)
                    {
                        Vector2 swipeEndPos = Pointer.current.position.ReadValue();
                        ProcessSwipe(swipeStartPos, swipeEndPos);
                    }
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
                    if (isGrounded) Jump();
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
                if (delta.y > 0 && isGrounded)
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
            isGrounded = false;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!collision.gameObject.CompareTag("Ground")) return;

            // Verify that the contact normal points upward (dot product > 0.7 ≈ <45° from vertical)
            // This prevents wall-jump exploits when hitting the side of a ground-tagged platform
            foreach (ContactPoint contact in collision.contacts)
            {
                if (Vector3.Dot(contact.normal, Vector3.up) > 0.7f)
                {
                    isGrounded = true;
                    return;
                }
            }
        }
    }
}
