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

        // Gravity & Jump (snappier, more earth-like)
        bool jumpPressed = Input.GetButton("Jump");
        if (controller.isGrounded)
        {
            // Small downward force to keep grounded (a bit stronger for stability)
            if (verticalVelocity < 0f)
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
}