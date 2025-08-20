using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float mouseSensitivity = 2f;
    public float gravity = -9.81f;
    public float jumpHeight = 2f; // How high the player jumps
    public float fallMultiplier = 2.5f; // Stronger gravity when falling
    public float lowJumpMultiplier = 2f; // Shorter jump when jump key released early
    public float terminalVelocity = -53f; // ~Terminal velocity (m/s)

    public Transform cameraTransform; // Assign your camera here

    private CharacterController controller;
    private float verticalVelocity; // For gravity & jumping
    private float cameraPitch = 0f; // For looking up/down

    public bool alignUprightOnPortalExit = true;
    public bool zeroAngularOnAlign = true;

    // Portal momentum preservation (CharacterController)
    [Header("Portal Momentum")]
    [Tooltip("Time to preserve injected momentum after exiting a portal (seconds)")]
    public float portalMomentumPreserveTime = 0.15f;
    private float portalMomentumTimer = 0f;
    private Vector3 portalHorizVel = Vector3.zero; // world-space horizontal velocity to add during timer

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked; // Hide and lock cursor
    }

    void Update()
    {
        HandleMouseLook();
        HandleMovement();
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Rotate player left/right
        transform.Rotate(Vector3.up * mouseX);

        // Rotate camera up/down
        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -80f, 80f);
        cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    void HandleMovement()
    {
        float moveX = Input.GetAxis("Horizontal"); // A/D
        float moveZ = Input.GetAxis("Vertical");   // W/S

        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        move *= moveSpeed;

        // Apply preserved horizontal velocity from portal while timer is active
        if (portalMomentumTimer > 0f)
        {
            Vector3 extra = new Vector3(portalHorizVel.x, 0f, portalHorizVel.z);
            move += extra;
            portalMomentumTimer -= Time.deltaTime;
            if (portalMomentumTimer <= 0f)
            {
                portalHorizVel = Vector3.zero;
            }
        }

        // Gravity & Jump (snappier, more earth-like)
        bool jumpPressed = Input.GetButton("Jump");
        if (controller.isGrounded)
        {
            // Avoid immediately killing post-portal vertical momentum
            if (verticalVelocity < 0f && portalMomentumTimer <= 0f)
            {
                verticalVelocity = -2f;
            }

            if (Input.GetButtonDown("Jump")) // Spacebar by default
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }
        else
        {
            // Apply stronger gravity when falling, and cut jump short if button released
            float g = gravity;
            if (verticalVelocity < 0f)
            {
                g *= fallMultiplier;
            }
            else if (!jumpPressed)
            {
                g *= lowJumpMultiplier;
            }

            verticalVelocity += g * Time.deltaTime;

            // Clamp to terminal velocity so the fall doesn't feel floaty
            if (verticalVelocity < terminalVelocity)
            {
                verticalVelocity = terminalVelocity;
            }
        }

        move.y = verticalVelocity;

        controller.Move(move * Time.deltaTime);
    }

    /// <summary>
    /// Called by a portal to inject a new world-space velocity when using CharacterController.
    /// Preserves horizontal momentum and sets vertical component, blending with next update.
    /// </summary>
    public void ReceivePortalMomentum(Vector3 worldVelocity)
    {
        // Project world velocity into our local space to maintain player-relative input feeling
        Vector3 local = transform.InverseTransformDirection(worldVelocity);

        // Set vertical velocity directly so gravity continues from this new value
        verticalVelocity = worldVelocity.y;

        // Start preservation window for horizontal speed
        portalMomentumTimer = portalMomentumPreserveTime;
        portalHorizVel = new Vector3(worldVelocity.x, 0f, worldVelocity.z);

        // Immediate nudge to sync controller's internal velocity reference this frame
        Vector3 horizWorld = transform.TransformDirection(new Vector3(local.x, 0f, local.z));
        Vector3 delta = horizWorld * Time.deltaTime;
        delta.y += 0f; // vertical already handled via verticalVelocity in next Update
        controller.Move(delta);
    }

    /// <summary>
    /// Aligns this object so that Up = Vector3.up and Forward = (exit forward flattened on the horizontal plane).
    /// Call this immediately after teleporting out of a portal.
    /// </summary>
    public void AlignAfterPortalExit(Quaternion exitRotation)
    {
        if (!alignUprightOnPortalExit) return;

        // Determine desired horizontal forward based on exit orientation
        Vector3 exitForward = exitRotation * Vector3.forward;
        Vector3 flatForward = Vector3.ProjectOnPlane(exitForward, Vector3.up);
        if (flatForward.sqrMagnitude < 1e-4f)
        {
            // Fallback: use current forward flattened
            flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            if (flatForward.sqrMagnitude < 1e-4f)
                flatForward = Vector3.forward; // absolute fallback
        }
        flatForward.Normalize();

        Quaternion upright = Quaternion.LookRotation(flatForward, Vector3.up);

        var cc = GetComponent<CharacterController>();
        bool ccWasEnabled = false;
        if (cc != null)
        {
            ccWasEnabled = cc.enabled;
            cc.enabled = false;
        }

        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            if (!rb.isKinematic && zeroAngularOnAlign)
            {
                rb.angularVelocity = Vector3.zero;
            }
            rb.MoveRotation(upright);
        }
        else
        {
            transform.rotation = upright;
        }

        if (cc != null)
        {
            cc.enabled = ccWasEnabled;
        }
    }

    // Align upright and also reset camera pitch so the view is straight after exit
    public void AlignAfterPortalExitAndResetView(Quaternion exitRotation)
    {
        AlignAfterPortalExit(exitRotation);
        // Reset vertical look so you are looking straight ahead
        cameraPitch = 0f;
        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        }
    }
}