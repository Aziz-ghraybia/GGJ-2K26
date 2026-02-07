using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GGJ.Controllers
{
    public class PlayerMovement : MonoBehaviour
    {
        private PlayerDataManager dataManager;
        public int speed;
        public int dashes;
        public float baseSpeed = 4.5f;
        public bool prayer;
        public int slots;

        [Header("Movement")]
        public float walkSpeed = 3f;
        public float runSpeed;
        [Tooltip("If true, load speed level from PlayerData; otherwise use Inspector 'speed' value")]
        public bool usePlayerDataSpeed = true;

        [Header("Dashing")]
        public float dashingPower = 20f;
        public float dashTime = 0.2f;
        public float dashCooldown = 5f;
        private bool canDash = true;
        private bool isDashing = false;
        private int currentDashCount; // Tracks remaining dashes

        // New: keep player afloat after dash to allow sequence air-dashes
        [Tooltip("How long gravity is suppressed after a dash to allow chaining air dashes")]
        public float postDashFloatTime = 0.25f;
        [Tooltip("Upward velocity applied after a dash to help keep the player in the air (not applied by default)")]
        public float postDashUpwardBoost = 0f;

        [Header("Jumping")]
        public float jumpForce = 7f;
        public float jumpHoldForce = 10f;
        public float maxJumpTime = 0.3f;

        [Header("Ground Check")]
        public LayerMask groundLayer;

        private Rigidbody rb;
        private bool isGrounded;
        private float currentSpeed;
        private float moveInput;
        private bool isRunning;
        private int groundContactCount = 0;
        private bool isJumping;
        private float jumpTimeCounter;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                Debug.LogError("[PlayerMovement] Rigidbody missing. Disabling script.");
                enabled = false;
                return;
            }

            rb.constraints = RigidbodyConstraints.FreezePositionZ |
                             RigidbodyConstraints.FreezeRotation;
            rb.linearDamping = 0f; // correct API

            dataManager = new PlayerDataManager();
            var pd = dataManager.LoadPlayerData();

            // Respect user's preference: either use PlayerData level or Inspector value
            if (usePlayerDataSpeed)
            {
                speed = pd.sprintSpeed;
            }
            // else keep Inspector-provided `speed` value

            // single dash as requested
            dashes = 1;
            prayer = pd.prayerUnlocked;
            slots = pd.slotNumbers;

            // Preserve original vision/formula using speed as a level:
            runSpeed = (float)(speed * 1.25 + baseSpeed);

            currentDashCount = dashes; // Initialize dash count
            Debug.Log($"Player Data Loaded: speedLevel={speed}, baseSpeed={baseSpeed}, runSpeed={runSpeed}, dashes={dashes}");
        }

        void Update()
        {
            if (isDashing) return; // Don't allow other actions while dashing

            HandleMovement();
            HandleJump();
            HandleDash();
            // Preserve original vision/formula using speed as a level:
            runSpeed = (float)(speed * 1.25 + baseSpeed);
        }

        void HandleMovement()
        {
            moveInput = 0f;

            bool right = false;
            bool left = false;
            bool sprintKey = false;

            if (Keyboard.current != null)
            {
                right = Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed;
                left = Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed;
                sprintKey = Keyboard.current.leftShiftKey.isPressed;
            }
            else
            {
                // Fallback for when new Input System isn't active
                right = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
                left = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
                sprintKey = Input.GetKey(KeyCode.LeftShift);
            }

            if (right) moveInput = 1f;
            else if (left) moveInput = -1f;

            // Use original runSpeed variable computed from PlayerData / Inspector
            if (sprintKey != isRunning)
            {
                isRunning = sprintKey;
                Debug.Log($"[PlayerMovement] Sprint {(isRunning ? "started" : "stopped")} - speed now {(isRunning ? runSpeed : walkSpeed)}");
            }

            currentSpeed = isRunning ? runSpeed : walkSpeed;

            // correct Rigidbody API
            rb.linearVelocity = new Vector3(moveInput * currentSpeed, rb.linearVelocity.y, 0);

            if (moveInput != 0)
            {
                transform.localScale = new Vector3(Mathf.Sign(moveInput), 1, 1);
            }
        }

        void HandleDash()
        {
            bool dashPressed = false;
            if (Keyboard.current != null)
                dashPressed = Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.leftCtrlKey.wasPressedThisFrame;
            else
                dashPressed = Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.LeftControl);

            if (dashPressed)
            {
                Debug.Log("[PlayerMovement] Dash input detected (E / LeftCtrl).");
            }

            if (dashPressed && canDash && currentDashCount > 0)
            {
                StartCoroutine(Dash());
            }

            // Reset dashes when grounded
            if (isGrounded && currentDashCount < dashes)
            {
                currentDashCount = dashes;
                Debug.Log($"Dashes recharged! ({currentDashCount}/{dashes})");
            }
        }

        IEnumerator Dash()
        {
            canDash = false;           // prevent starting another dash while performing this dash
            isDashing = true;
            currentDashCount--;

            bool originalUseGravity = rb.useGravity;
            rb.useGravity = false;

            float dashDirection = Mathf.Sign(transform.localScale.x != 0 ? transform.localScale.x : 1f);

            // strictly horizontal dash
            rb.linearVelocity = new Vector3(dashDirection * dashingPower, 0f, 0f);

            Debug.Log($"DASH! Direction: {dashDirection}, Remaining: {currentDashCount}/{dashes}");

            yield return new WaitForSeconds(dashTime);

            if (postDashFloatTime > 0f)
            {
                Debug.Log($"[PlayerMovement] Post-dash float for {postDashFloatTime} seconds to enable chaining.");
                yield return new WaitForSeconds(postDashFloatTime);
            }

            rb.useGravity = originalUseGravity;
            isDashing = false;

            // Always enforce cooldown after dash (prevents ground-chaining)
            Debug.Log($"[PlayerMovement] Starting dash cooldown: {dashCooldown} seconds.");
            yield return new WaitForSeconds(dashCooldown);
            canDash = true;
            Debug.Log("[PlayerMovement] Dash cooldown ended. canDash=true");
        }

        void HandleJump()
        {
            bool spacePressed = (Keyboard.current != null) ? Keyboard.current.spaceKey.wasPressedThisFrame : Input.GetKeyDown(KeyCode.Space);
            bool spaceHeld = (Keyboard.current != null) ? Keyboard.current.spaceKey.isPressed : Input.GetKey(KeyCode.Space);
            bool spaceReleased = (Keyboard.current != null) ? Keyboard.current.spaceKey.wasReleasedThisFrame : Input.GetKeyUp(KeyCode.Space);

            if (spacePressed && isGrounded)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, 0);
                isJumping = true;
                jumpTimeCounter = 0f;
            }

            if (spaceHeld && isJumping)
            {
                if (jumpTimeCounter < maxJumpTime)
                {
                    rb.AddForce(Vector3.up * jumpHoldForce, ForceMode.Force);
                    jumpTimeCounter += Time.deltaTime;
                }
                else
                {
                    isJumping = false;
                }
            }

            if (spaceReleased)
            {
                isJumping = false;

                if (rb.linearVelocity.y > 0)
                {
                    rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f, 0);
                }
            }
        }

        void OnCollisionEnter(Collision collision)
        {
            if (IsGroundLayer(collision.gameObject))
            {
                groundContactCount++;
                isGrounded = true;
                isJumping = false;
            }
        }

        void OnCollisionStay(Collision collision)
        {
            if (IsGroundLayer(collision.gameObject) && groundContactCount == 0)
            {
                groundContactCount++;
                isGrounded = true;
            }
        }

        void OnCollisionExit(Collision collision)
        {
            if (IsGroundLayer(collision.gameObject))
            {
                groundContactCount--;
                if (groundContactCount <= 0)
                {
                    groundContactCount = 0;
                    isGrounded = false;
                }
            }
        }

        bool IsGroundLayer(GameObject obj)
        {
            return ((1 << obj.layer) & groundLayer) != 0;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
        }
    }
}
