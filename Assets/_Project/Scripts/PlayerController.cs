using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace PitchRush
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(BuffManager))]
    [RequireComponent(typeof(InnerCoinVisuals))]
    [RequireComponent(typeof(BlobCustomizer))]
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

        [Header("Visual Deformation (Squash & Stretch)")]
        public Transform visualModel;
        public float squashSpeed = 5f;

        [Header("Mercury Split Settings")]
        public GameObject mercuryClonePrefab;
        private List<MercuryClone> activeClones = new List<MercuryClone>();
        private bool isMercuryActive = false;

        private Rigidbody rb;
        private float targetX;

        // Pointer tracking logic
        private bool isSwiping = false;
        private Vector2 swipeStartPos;

        // Blob & Roll variables
        public BlobForm CurrentForm { get; private set; } = BlobForm.Default;
        private bool isCeilingGravityActive = false;
        private float ceilingMagnetForce = 0f;
        private Vector3 originalScale;
        private bool wasGrounded = true;
        private float formJumpMultiplier = 1f;
        private Transform meshHolder;

        // Wall Run and Color state fields/properties
        public bool IsWallRunning => isWallRunning;
        public Vector3 WallRunNormal => wallRunNormal;
        public bool IsCeilingGravityActive => isCeilingGravityActive;

        [Header("Wall Run Settings")]
        public float wallGravityForce = 15f;
        private bool isWallRunning = false;
        private Vector3 wallRunNormal = Vector3.zero;

        [Header("Color Mixing System")]
        public Color defaultSlimeColor = new Color(1f, 0.2f, 0.2f, 0.8f); // Translucent Red Gel
        public Color currentSlimeColor = new Color(1f, 0.2f, 0.2f, 0.8f);

        private void Awake()
        {
            // Auto-restructure visual mesh if it is directly on the root Player object
            MeshFilter rootMeshFilter = GetComponent<MeshFilter>();
            MeshRenderer rootMeshRenderer = GetComponent<MeshRenderer>();

            if (visualModel == null && rootMeshFilter != null && rootMeshRenderer != null)
            {
                // Create parent visual container (remains upright, handles squash/stretch scale)
                GameObject visualObj = new GameObject("PlayerVisuals");
                visualObj.transform.SetParent(transform);
                visualObj.transform.localPosition = Vector3.zero;
                visualObj.transform.localRotation = Quaternion.identity;
                visualObj.transform.localScale = Vector3.one;

                // Create sub-child mesh holder (handles rolling rotation)
                GameObject meshHolderObj = new GameObject("MeshHolder");
                meshHolderObj.transform.SetParent(visualObj.transform);
                meshHolderObj.transform.localPosition = Vector3.zero;
                meshHolderObj.transform.localRotation = Quaternion.identity;
                meshHolderObj.transform.localScale = Vector3.one;

                // Copy MeshFilter
                MeshFilter childFilter = meshHolderObj.AddComponent<MeshFilter>();
                childFilter.sharedMesh = rootMeshFilter.sharedMesh;

                // Copy MeshRenderer
                MeshRenderer childRenderer = meshHolderObj.AddComponent<MeshRenderer>();
                childRenderer.sharedMaterials = rootMeshRenderer.sharedMaterials;

                // Clean up root components so we don't have duplicates
                Destroy(rootMeshRenderer);
                Destroy(rootMeshFilter);

                visualModel = visualObj.transform;
            }
        }

        void Start()
        {
            rb = GetComponent<Rigidbody>();
            targetX = transform.position.x;

            if (visualModel == null && transform.childCount > 0)
            {
                visualModel = transform.GetChild(0);
            }

            if (visualModel != null && visualModel.childCount > 0)
            {
                meshHolder = visualModel.GetChild(0);
            }

            if (visualModel != null)
            {
                originalScale = visualModel.localScale;
            }
            else
            {
                originalScale = Vector3.one;
            }

            // Initialize default form settings
            SetForm(BlobForm.Default);
        }

        /// <summary>
        /// Resets the lateral position (steering) to center of the road.
        /// Called by TrackManager after a turn segment completes.
        /// </summary>
        public void ResetLateralPosition()
        {
            targetX = 0f;
        }

        void Update()
        {
            HandleInput();
            CheckKillZone();
            HandleSquashStretch();
            HandleMeshRolling();
            HandleUprightVisualRotation();
        }

        void FixedUpdate()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsGameActive)
                return;

            // Apply custom upward gravity if snapped to ceiling
            if (isCeilingGravityActive && CurrentForm == BlobForm.HeavyIron)
            {
                rb.useGravity = false;
                rb.AddForce(Vector3.up * ceilingMagnetForce, ForceMode.Acceleration);
            }
            // Apply custom wall run gravity vector locally (0 GC Alloc!)
            else if (isWallRunning && CurrentForm == BlobForm.Default)
            {
                rb.useGravity = false;
                rb.AddForce(-wallRunNormal * wallGravityForce, ForceMode.Acceleration);
                rb.AddForce(Physics.gravity, ForceMode.Acceleration);
            }
            else
            {
                rb.useGravity = true;
            }

            float currentForwardSpeed = GameManager.Instance != null ? GameManager.Instance.CurrentSpeed : 10f;

            // Get current track direction from TrackManager (supports turns!)
            TrackManager tm = GameManager.Instance != null ? 
                GameManager.Instance.trackManager : FindAnyObjectByType<TrackManager>();

            Vector3 forwardDir = Vector3.forward;
            Vector3 lateralDir = Vector3.right;
            Vector3 pivot = Vector3.zero;

            if (tm != null)
            {
                forwardDir = tm.CurrentDirection;
                lateralDir = Vector3.Cross(Vector3.up, forwardDir).normalized;
                pivot = tm.PivotPoint;
            }

            // Project player position relative to the pivot point onto the lateral axis to calculate steering offset (0 GC Alloc!)
            Vector3 relativePos = rb.position - pivot;
            float currentLateral = Vector3.Dot(relativePos, lateralDir);
            float lateralDifference = targetX - currentLateral;
            float lateralVelocity = lateralDifference * horizontalSpeed;

            // Compose final velocity: forward + lateral + vertical (gravity/jump)
            Vector3 finalVelocity = forwardDir * currentForwardSpeed + lateralDir * lateralVelocity;
            finalVelocity.y = rb.linearVelocity.y;

            rb.linearVelocity = finalVelocity;

            // Smoothly rotate the player to face the current direction
            if (forwardDir != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(forwardDir, Vector3.up);
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, 8f * Time.fixedDeltaTime));
            }
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
                        // Register jump on swipe up (when not on ceiling)
                        // If on ceiling, swipe down to jump (let go)
                        float swipeDelta = isCeilingGravityActive ? -delta.y : delta.y;
                        if (swipeDelta > swipeThreshold && Mathf.Abs(delta.y) > Mathf.Abs(delta.x))
                        {
                            if (IsGrounded())
                            {
                                Jump();
                            }
                            isSwiping = false; // Prevent multiple jumps in one gesture
                        }
                    }

                    // B. Direct Screen-Position Steering (Smooth Continuous Drag)
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
            if (formJumpMultiplier <= 0f) return; // Heavy Iron cannot jump

            // Ground normal depends on gravity direction
            Vector3 jumpDir = isCeilingGravityActive ? Vector3.down : Vector3.up;

            // Zero out vertical velocity before jumping to ensure consistent jump height
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(jumpDir * (jumpForce * formJumpMultiplier), ForceMode.Impulse);

            // Synchronize jump for active Mercury clones
            if (isMercuryActive)
            {
                foreach (MercuryClone clone in activeClones)
                {
                    if (clone != null)
                    {
                        clone.CloneJump(jumpForce * formJumpMultiplier);
                    }
                }
            }

            // Instant stretch visual on jump
            if (CurrentForm == BlobForm.Default && visualModel != null)
            {
                visualModel.localScale = new Vector3(originalScale.x * 0.7f, originalScale.y * 1.5f, originalScale.z * 0.7f);
            }
        }

        public void SetForm(BlobForm form)
        {
            if (isMercuryActive) return; // Ignore gate form changes while split in Mercury mode
            CurrentForm = form;

            if (rb == null) rb = GetComponent<Rigidbody>();

            switch (form)
            {
                case BlobForm.Default:
                    rb.mass = 1f;
                    formJumpMultiplier = 1f;
                    rb.useGravity = true;
                    isCeilingGravityActive = false;
                    break;

                case BlobForm.HeavyIron:
                    rb.mass = 5f;
                    formJumpMultiplier = 0f; // Iron ball cannot jump
                    break;

                case BlobForm.LightPingPong:
                    rb.mass = 0.3f;
                    formJumpMultiplier = 1.8f; // Light ball jumps higher
                    rb.useGravity = true;
                    isCeilingGravityActive = false;
                    break;
            }

            // Restore scale of visual model
            if (visualModel != null)
            {
                visualModel.localScale = originalScale;
            }

            // Apply visuals from BlobCustomizer if attached
            BlobCustomizer customizer = GetComponent<BlobCustomizer>();
            if (customizer != null)
            {
                customizer.ApplyFormVisuals(form);
            }
        }

        public void SetCeilingGravity(bool active, float force)
        {
            if (CurrentForm != BlobForm.HeavyIron)
            {
                // Only Heavy Iron is magnetic enough to stay on ceiling
                isCeilingGravityActive = false;
                rb.useGravity = true;
                return;
            }

            isCeilingGravityActive = active;
            ceilingMagnetForce = force;
            rb.useGravity = !active;
        }

        private void HandleSquashStretch()
        {
            if (visualModel == null) return;

            bool grounded = IsGrounded();

            if (CurrentForm == BlobForm.Default)
            {
                if (grounded && !wasGrounded)
                {
                    // Just landed! Squash the slime ball
                    visualModel.localScale = new Vector3(originalScale.x * 1.35f, originalScale.y * 0.65f, originalScale.z * 1.35f);
                }
                else if (!grounded)
                {
                    // Stretch slime ball in flight based on vertical velocity
                    float yVel = rb.linearVelocity.y;
                    float stretchAmount = Mathf.Clamp(yVel * 0.04f, -0.25f, 0.25f);
                    visualModel.localScale = new Vector3(
                        originalScale.x * (1f - stretchAmount * 0.5f),
                        originalScale.y * (1f + stretchAmount),
                        originalScale.z * (1f - stretchAmount * 0.5f)
                    );
                }
                else
                {
                    // Smoothly recover to standard shape when rolling
                    visualModel.localScale = Vector3.MoveTowards(visualModel.localScale, originalScale, Time.deltaTime * squashSpeed);
                }
            }
            else
            {
                // Non-slime forms do not deform physically
                visualModel.localScale = Vector3.MoveTowards(visualModel.localScale, originalScale, Time.deltaTime * squashSpeed * 2f);
            }

            wasGrounded = grounded;
        }

        private void HandleMeshRolling()
        {
            if (meshHolder == null) return;

            // Roll the visual mesh forward based on current movement speed
            float forwardSpeed = GameManager.Instance != null ? GameManager.Instance.CurrentSpeed : 10f;
            float sphereRadius = 0.5f;

            // Angular velocity: speed / radius (converted to degrees per second)
            float rollSpeedDegrees = (forwardSpeed / sphereRadius) * Mathf.Rad2Deg;

            // Rotate visual mesh around X-axis
            meshHolder.Rotate(Vector3.right * rollSpeedDegrees * Time.deltaTime, Space.Self);
        }

        private void HandleUprightVisualRotation()
        {
            if (visualModel == null) return;

            float targetZAngle = 0f;

            if (isWallRunning && CurrentForm == BlobForm.Default)
            {
                // Align visual rotation perpendicular to the wall normal
                // If normal is pointing right (X > 0), player is on the left wall -> tilt -90 degrees.
                // If normal is pointing left (X < 0), player is on the right wall -> tilt 90 degrees.
                targetZAngle = wallRunNormal.x > 0.1f ? -90f : (wallRunNormal.x < -0.1f ? 90f : 0f);
            }
            else
            {
                targetZAngle = isCeilingGravityActive ? 180f : 0f;
            }
            
            float xVel = rb.linearVelocity.x;
            float tiltAngle = Mathf.Clamp(-xVel * 1.6f, -12f, 12f); // Bank into turns

            // Smooth local rotation transition
            Quaternion targetRot = Quaternion.Euler(0f, 0f, targetZAngle + tiltAngle);
            visualModel.localRotation = Quaternion.Slerp(visualModel.localRotation, targetRot, 8f * Time.deltaTime);
        }

        private bool IsGrounded()
        {
            SphereCollider sphereCollider = GetComponent<SphereCollider>();
            float radius = sphereCollider != null ? sphereCollider.radius * transform.localScale.y : 0.5f;
            float castDistance = 0.18f; // Small buffer beneath (or above) the ball

            // Cast direction depends on gravity (down for ground, up for ceiling)
            Vector3 castDir = isCeilingGravityActive ? Vector3.up : Vector3.down;

            RaycastHit hit;
            if (Physics.SphereCast(transform.position, radius * 0.85f, castDir, out hit, radius + castDistance))
            {
                if (hit.collider.CompareTag("Ground"))
                {
                    return true;
                }
            }
            return false;
        }

        public void StartMercurySplit()
        {
            if (isMercuryActive) return;
            isMercuryActive = true;

            // Apply chrome material to main player visually to match the clones (0 GC Alloc!)
            Renderer mainRend = visualModel != null ? visualModel.GetComponentInChildren<Renderer>() : null;
            if (mainRend != null)
            {
                Material chromeMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                chromeMat.color = new Color(0.85f, 0.85f, 0.85f);
                if (chromeMat.HasProperty("_Metallic")) chromeMat.SetFloat("_Metallic", 1f);
                if (chromeMat.HasProperty("_Smoothness")) chromeMat.SetFloat("_Smoothness", 0.9f);
                mainRend.material = chromeMat;
            }

            // Reposition player to center lane (X = 0)
            targetX = 0f;
            transform.position = new Vector3(0f, transform.position.y, transform.position.z);
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, rb.linearVelocity.z);

            // Spawn left and right clones
            SpawnMercuryClone(-2.5f);
            SpawnMercuryClone(2.5f);
        }

        private void SpawnMercuryClone(float offsetX)
        {
            GameObject cloneObj = null;
            if (mercuryClonePrefab != null)
            {
                cloneObj = Instantiate(mercuryClonePrefab, transform.position + new Vector3(offsetX, 0f, 0f), Quaternion.identity);
            }
            else
            {
                // Fallback chrome sphere clone
                cloneObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                cloneObj.name = "MercuryClone";
                cloneObj.transform.position = transform.position + new Vector3(offsetX, 0f, 0f);
                cloneObj.tag = "Player"; // Tag as Player so it triggers collectibles/coins

                Renderer rend = cloneObj.GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    rend.material.color = new Color(0.85f, 0.85f, 0.85f);
                    if (rend.material.HasProperty("_Metallic")) rend.material.SetFloat("_Metallic", 1f);
                    if (rend.material.HasProperty("_Smoothness")) rend.material.SetFloat("_Smoothness", 0.9f);
                }

                Rigidbody cloneRb = cloneObj.AddComponent<Rigidbody>();
                cloneRb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
            }

            MercuryClone cloneScript = cloneObj.GetComponent<MercuryClone>();
            if (cloneScript == null)
            {
                cloneScript = cloneObj.AddComponent<MercuryClone>();
            }

            cloneScript.Initialize(this, offsetX);
            activeClones.Add(cloneScript);
        }

        public void StopMercuryMerge()
        {
            if (!isMercuryActive) return;
            isMercuryActive = false;

            // Pull remaining clones back to center player position and merge
            foreach (MercuryClone clone in activeClones)
            {
                if (clone != null)
                {
                    clone.SplashAndDestroy();
                }
            }
            activeClones.Clear();

            // Restore original form material on main player
            BlobCustomizer customizer = GetComponent<BlobCustomizer>();
            if (customizer != null)
            {
                customizer.ApplyFormVisuals(CurrentForm);
            }
            else
            {
                SetSlimeColor(currentSlimeColor); // Fallback color
            }
        }

        public void OnCloneDestroyed(MercuryClone clone)
        {
            if (activeClones.Contains(clone))
            {
                activeClones.Remove(clone);
            }
        }

        public bool ShiftToSurvivingClone()
        {
            if (isMercuryActive && activeClones.Count > 0)
            {
                MercuryClone survivor = null;
                foreach (MercuryClone clone in activeClones)
                {
                    if (clone != null)
                    {
                        survivor = clone;
                        break;
                    }
                }

                if (survivor != null)
                {
                    // Snap player to survivor position
                    transform.position = survivor.transform.position;
                    targetX = survivor.transform.position.x;
                    rb.linearVelocity = survivor.GetComponent<Rigidbody>().linearVelocity;

                    // Destroy survivor since player is in its place now
                    activeClones.Remove(survivor);
                    survivor.SplashAndDestroy();

                    // Temporary invincibility
                    BuffManager buffManager = GetComponent<BuffManager>();
                    if (buffManager != null)
                    {
                        buffManager.StartCoroutine(buffManager.TemporaryInvincibilityCoroutine(1.5f));
                    }

                    Debug.Log("Saved by shifting to surviving Mercury clone!");
                    return true;
                }
            }
            return false;
        }

        private bool CompareTagSafe(GameObject obj, string tagName)
        {
            if (obj == null) return false;

            // Only check Unity tags if they are standard/pre-registered in this project
            if (tagName == "Untagged" || tagName == "Player" || tagName == "Ground" || tagName == "Obstacle")
            {
                try
                {
                    return obj.CompareTag(tagName);
                }
                catch
                {
                    // Fallback to name check
                }
            }

            // Fallback for custom tags: safe name comparison (never spams console!)
            return obj.name.Contains(tagName);
        }

        private void OnCollisionStay(Collision collision)
        {
            if (CompareTagSafe(collision.gameObject, "StickyWall") && CurrentForm == BlobForm.Default)
            {
                isWallRunning = true;
                wallRunNormal = collision.contacts[0].normal;
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            if (CompareTagSafe(collision.gameObject, "StickyWall"))
            {
                isWallRunning = false;
                wallRunNormal = Vector3.zero;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (CompareTagSafe(other.gameObject, "ColorPickup"))
            {
                // In Unity, the color pickup can have a simple ColorContainer script that stores its color
                ColorContainer cc = other.GetComponent<ColorContainer>();
                if (cc != null)
                {
                    SetSlimeColor(cc.color);
                }
                other.gameObject.SetActive(false); // Hide pickup (0 GC Alloc!)
            }
            else if (CompareTagSafe(other.gameObject, "WashPuddle"))
            {
                // Wash away the paint and return to default slime color
                SetSlimeColor(defaultSlimeColor);
            }
        }

        public void SetSlimeColor(Color newColor)
        {
            currentSlimeColor = newColor;

            // Apply color to visual model renderer
            if (visualModel != null)
            {
                Renderer rend = visualModel.GetComponentInChildren<Renderer>();
                if (rend != null)
                {
                    rend.material.color = currentSlimeColor;
                    if (rend.material.HasProperty("_BaseColor"))
                    {
                        rend.material.SetColor("_BaseColor", currentSlimeColor);
                    }
                }
            }
        }
    }
}
