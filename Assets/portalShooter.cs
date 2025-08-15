using UnityEngine;

public class PortalShooter : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Camera used for aiming the portal shots")] 
    public Camera aimCamera;               // Assign main camera

    [Tooltip("The first portal (e.g., Blue)")] 
    public Portals portalA;                // Drag the Portal A GameObject

    [Tooltip("The second portal (e.g., Orange)")] 
    public Portals portalB;                // Drag the Portal B GameObject

    [Header("Placement Settings")]
    public float maxShootDistance = 200f;
    [Tooltip("Slight offset from the hit surface to avoid z-fighting/overlap")] 
    public float surfaceOffset = 0.02f;
    [Tooltip("Layers valid for portal placement")] 
    public LayerMask placementMask = ~0;   // Default: everything

    [Header("Visuals (optional)")]
    public bool autoEnablePortalObjects = true; // If true, SetActive(true) when placing

    private void Awake()
    {
        if (aimCamera == null)
        {
            aimCamera = Camera.main;
        }

        if (portalA != null && portalB != null)
        {
            portalA.linkedPortal = portalB;
            portalB.linkedPortal = portalA;
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryPlacePortal(portalA);
        }
        else if (Input.GetMouseButtonDown(1))
        {
            TryPlacePortal(portalB);
        }
    }

    private void TryPlacePortal(Portals portal)
    {
        if (portal == null || aimCamera == null)
            return;

        Ray ray = new Ray(aimCamera.transform.position, aimCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxShootDistance, placementMask, QueryTriggerInteraction.Ignore))
        {
            // Keep portal visually upright relative to camera while aligning forward to surface normal
            Vector3 alignNormal = hit.normal;
            Vector3 up = Vector3.ProjectOnPlane(aimCamera.transform.up, alignNormal).normalized;
            if (up.sqrMagnitude < 1e-4f) up = Vector3.up; // fallback
            Quaternion rot = Quaternion.LookRotation(alignNormal, up);

            Vector3 pos = hit.point + alignNormal * surfaceOffset;

            // Place and orient
            portal.transform.SetPositionAndRotation(pos, rot);

            // Make sure both portals link to each other if both assigned
            if (portalA != null && portalB != null)
            {
                portalA.linkedPortal = portalB;
                portalB.linkedPortal = portalA;
            }

            // Ensure portal GameObject is active and its collider is trigger
            if (autoEnablePortalObjects && !portal.gameObject.activeSelf)
            {
                portal.gameObject.SetActive(true);
            }

            var col = portal.GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            Debug.Log($"[PortalShooter] Placed '{portal.name}' at {pos} with normal {alignNormal}.");
        }
        else
        {
            Debug.Log("[PortalShooter] No valid surface hit.");
        }
    }
}