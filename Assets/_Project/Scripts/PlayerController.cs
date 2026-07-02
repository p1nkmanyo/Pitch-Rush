using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace PitchRush
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        public float forwardSpeed = 10f;
        public float horizontalSpeed = 15f;
        public float jumpForce = 5f;
        public float leftBoundary = -4.5f;
        public float rightBoundary = 4.5f;

        private Rigidbody rb;
        private bool isGrounded = true;

        // Touch input variables
        private Vector2 touchStartPos;
        private bool isSwiping = false;

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }

        private void OnDisable()
        {
            EnhancedTouchSupport.Disable();
        }

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
            // Move forward automatically
            Vector3 forwardMovement = transform.forward * forwardSpeed * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + forwardMovement);
        }

        private void HandleInput()
        {
            // Handling Touch Input via EnhancedTouch
            if (Touch.activeTouches.Count > 0)
            {
                Touch touch = Touch.activeTouches[0];

                if (touch.phase == TouchPhase.Began)
                {
                    isSwiping = true;
                    touchStartPos = touch.screenPosition;
                }
                else if (touch.phase == TouchPhase.Moved && isSwiping)
                {
                    // Calculate horizontal movement based on delta
                    float deltaX = touch.delta.x;
                    // Normalize delta slightly to make movement smooth across different screen sizes
                    float moveAmount = (deltaX / Screen.width) * horizontalSpeed;

                    Vector3 newPos = transform.position + new Vector3(moveAmount, 0, 0);
                    // Clamp to boundaries
                    newPos.x = Mathf.Clamp(newPos.x, leftBoundary, rightBoundary);
                    transform.position = newPos;

                    // Check for jump (vertical swipe)
                    Vector2 touchDelta = touch.screenPosition - touchStartPos;
                    if (touchDelta.y > 50f && isGrounded) // Threshold for jump
                    {
                        Jump();
                        isSwiping = false; // Prevent multiple jumps from one swipe
                    }
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    isSwiping = false;
                }
            }
            else
            {
                // Keyboard fallback for testing in editor using New Input System
                if (Keyboard.current != null)
                {
                    float horizontalInput = 0f;

                    if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                    {
                        horizontalInput = -1f;
                    }
                    else if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                    {
                        horizontalInput = 1f;
                    }

                    if (Mathf.Abs(horizontalInput) > 0.01f)
                    {
                        Vector3 newPos = transform.position + new Vector3(horizontalInput * horizontalSpeed * Time.deltaTime, 0, 0);
                        newPos.x = Mathf.Clamp(newPos.x, leftBoundary, rightBoundary);
                        transform.position = newPos;
                    }

                    if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame)
                    {
                        if (isGrounded)
                        {
                            Jump();
                        }
                    }
                }
            }
        }

        private void Jump()
        {
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
