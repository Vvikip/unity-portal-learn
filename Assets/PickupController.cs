using UnityEngine;

public class PickupController : MonoBehaviour
{
    [Header("Pickup Settings")]
    public Camera aimCamera; // assign player camera; falls back to Camera.main
    public float pickupRange = 3.0f;
    public float holdDistance = 2.0f;
    public bool alignWithCamera = true;
    [Tooltip("Key to pick up / drop objects")]
    public KeyCode pickupKey = KeyCode.E;

    [Header("Held Object Physics Override")]
    public bool makeKinematicWhileHeld = true;
    public bool disableGravityWhileHeld = true;

    [Header("Filters")]
    [Tooltip("Only pick objects on these layers. Leave empty (Everything) to allow all.")]
    public LayerMask pickableLayers = ~0; // default: Everything

    [Tooltip("Optional: require a Rigidbody to be present to pick up.")]
    public bool requireRigidbody = true;

    public bool IsHolding => heldBody != null;

    private Rigidbody heldBody;
    private bool prevUseGravity;
    private bool prevIsKinematic;
    private Transform holdAnchor;

    void Awake()
    {
        if (aimCamera == null)
            aimCamera = Camera.main;

        // Create a hold anchor in front of the camera if not present
        GameObject anchor = new GameObject("HoldAnchor");
        holdAnchor = anchor.transform;
        if (aimCamera != null)
        {
            holdAnchor.SetParent(aimCamera.transform, false);
            holdAnchor.localPosition = new Vector3(0f, 0f, holdDistance);
            holdAnchor.localRotation = Quaternion.identity;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(pickupKey))
        {
            if (IsHolding)
                Drop();
            else
                TryPickup();
        }

        if (IsHolding)
        {
            // Keep held at anchor each frame
            heldBody.MovePosition(holdAnchor.position);
            if (alignWithCamera)
                heldBody.MoveRotation(holdAnchor.rotation);
        }
    }

    private void TryPickup()
    {
        if (aimCamera == null) return;

        Ray ray = new Ray(aimCamera.transform.position, aimCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange, pickableLayers, QueryTriggerInteraction.Ignore))
        {
            Rigidbody rb = hit.rigidbody;
            if (rb == null && !requireRigidbody)
            {
                rb = hit.collider != null ? hit.collider.GetComponent<Rigidbody>() : null;
            }
            if (rb == null) return;

            heldBody = rb;
            prevUseGravity = heldBody.useGravity;
            prevIsKinematic = heldBody.isKinematic;

            if (disableGravityWhileHeld) heldBody.useGravity = false;
            if (makeKinematicWhileHeld) heldBody.isKinematic = true;

            // Snap to anchor immediately
            heldBody.MovePosition(holdAnchor.position);
            if (alignWithCamera)
                heldBody.MoveRotation(holdAnchor.rotation);
        }
    }

    public void Drop()
    {
        if (!IsHolding) return;

        // Restore physics
        heldBody.useGravity = prevUseGravity;
        heldBody.isKinematic = prevIsKinematic;
        heldBody = null;
    }
}
