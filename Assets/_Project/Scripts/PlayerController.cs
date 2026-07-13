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
        public float horizontalSpeed = 20f; // Speed of lane changing

        [Header("Jump & Input Settings")]
        public float jumpForce = 6f;
        public float swipeThreshold = 50f; // Minimum pixels to register a swipe

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
        }

        void FixedUpdate()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsGameActive)
                return;

            // Calculate forward movement based on GameManager's progression speed
            float currentForwardSpeed = GameManager.Instance != null ? GameManager.Instance.CurrentSpeed : 10f;
            Vector3 forwardMovement = transform.forward * currentForwardSpeed * Time.fixedDeltaTime;

            // Calculate target X position based on current lane
            // Center is 0. Left is -laneDistance. Right is +laneDistance.
            float targetX = (currentLane - 1) * laneDistance;

            // Smoothly move X position towards target using Mathf.MoveTowards for snappy but smooth lane changes
            float newX = Mathf.MoveTowards(rb.position.x, targetX, horizontalSpeed * Time.fixedDeltaTime);

            // Apply movement safely using MovePosition
            Vector3 newPosition = new Vector3(newX, rb.position.y, rb.position.z) + forwardMovement;
            rb.MovePosition(newPosition);
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
            // Simple check to see if we hit the ground
            if (collision.gameObject.CompareTag("Ground"))
            {
                isGrounded = true;
            }
        }
    }
}
