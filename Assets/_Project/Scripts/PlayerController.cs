using UnityEngine;

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
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);

                if (touch.phase == TouchPhase.Began)
                {
                    isSwiping = true;
                    touchStartPos = touch.position;
                }
                else if (touch.phase == TouchPhase.Moved && isSwiping)
                {
                    // Calculate horizontal movement based on delta
                    float deltaX = touch.deltaPosition.x;
                    // Normalize delta slightly to make movement smooth across different screen sizes
                    float moveAmount = (deltaX / Screen.width) * horizontalSpeed;

                    Vector3 newPos = transform.position + new Vector3(moveAmount, 0, 0);
                    // Clamp to boundaries
                    newPos.x = Mathf.Clamp(newPos.x, leftBoundary, rightBoundary);
                    transform.position = newPos;

                    // Check for jump (vertical swipe)
                    Vector2 touchDelta = touch.position - touchStartPos;
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
                // Keyboard fallback for testing in editor
                float horizontalInput = Input.GetAxis("Horizontal");
                if (Mathf.Abs(horizontalInput) > 0.01f)
                {
                    Vector3 newPos = transform.position + new Vector3(horizontalInput * horizontalSpeed * Time.deltaTime, 0, 0);
                    newPos.x = Mathf.Clamp(newPos.x, leftBoundary, rightBoundary);
                    transform.position = newPos;
                }

                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow))
                {
                    if (isGrounded)
                    {
                        Jump();
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
            // In a real game, checking collision normal or using raycast is better
            if (collision.gameObject.CompareTag("Ground"))
            {
                isGrounded = true;
            }
        }
    }
}
