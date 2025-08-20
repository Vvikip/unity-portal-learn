using UnityEngine;

public class PortalIndicatorUI : MonoBehaviour
{
    [Header("Source (optional but recommended)")]
    [Tooltip("If assigned, uses this shooter's camera and mask for raycasts.")]
    public PortalShooter shooter; // drag the same object with PortalShooter

    [Header("Camera & Mask (fallback if shooter is null)")]
    public Camera cam;
    public LayerMask occlusionMask = ~0; // everything by default

    [Header("Portal identification")]
    [Tooltip("If set, we consider any collider with this tag as a portal and ignore it in occlusion checks.")]
    public string portalTag = "Portal";

    [Header("Indicator appearance")]
    public float squareSize = 80f;     // total size of the square in pixels
    public float cornerLen = 14f;      // length of each corner arm
    public float thickness = 3f;       // line thickness
    public Color occludedColor = new Color(0f, 1f, 1f, 0.9f);
    public Color offscreenColor = new Color(0f, 1f, 1f, 0.5f);

    [Header("Behavior")]
    [Tooltip("Only show when the portal is occluded by world geometry.")]
    public bool onlyWhenOccluded = true;
    [Tooltip("Max angle (deg) between look forward and direction to portal to consider it 'looked at'.")]
    public float maxLookAngle = 40f;
    [Tooltip("Clamp indicators to screen edges when the portal is off-screen.")]
    public bool clampToScreen = true;

    private Texture2D _tex;

    void Awake()
    {
        if (shooter == null) shooter = FindObjectOfType<PortalShooter>();
        if (cam == null && shooter != null) cam = shooter.aimCamera;
        if (cam == null) cam = Camera.main;

        _tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        _tex.SetPixel(0, 0, Color.white);
        _tex.Apply();
    }

    void OnGUI()
    {
        if (!Application.isPlaying) return;
        if (cam == null) return;

        var portals = FindObjectsOfType<Portals>();
        if (portals == null || portals.Length == 0) return;

        foreach (var p in portals)
        {
            var t = p.transform;
            Vector3 worldPos = t.position; // use portal plane center

            // Direction and angle check
            Vector3 toPortal = (worldPos - cam.transform.position);
            float dist = toPortal.magnitude;
            if (dist < Mathf.Epsilon) continue;
            Vector3 dir = toPortal / dist;
            float angle = Vector3.Angle(cam.transform.forward, dir);
            if (angle > maxLookAngle) continue;

            // Project to screen
            Vector3 vp = cam.WorldToViewportPoint(worldPos);
            if (vp.z <= 0f) continue; // behind camera

            bool inViewport = vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f;

            // Occlusion test: if a ray hits something before reaching the portal, we deem it occluded
            bool occluded = false;
            if (onlyWhenOccluded || !inViewport)
            {
                if (Physics.Raycast(cam.transform.position, dir, out RaycastHit hit, dist * 0.999f, occlusionMask, QueryTriggerInteraction.Ignore))
                {
                    // Ignore hits on objects tagged as portal
                    if (!(hit.collider != null && hit.collider.CompareTag(portalTag)))
                        occluded = true;
                }
            }

            if (onlyWhenOccluded && !occluded)
            {
                // Skip drawing when portal is visible
                continue;
            }

            // Determine screen position
            float sx = vp.x * Screen.width;
            float sy = (1f - vp.y) * Screen.height; // GUI Y is top-down

            if (!inViewport && clampToScreen)
            {
                sx = Mathf.Clamp(sx, squareSize * 0.5f, Screen.width - squareSize * 0.5f);
                sy = Mathf.Clamp(sy, squareSize * 0.5f, Screen.height - squareSize * 0.5f);
            }

            // Pick color tinted by portal's material color
            var prev = GUI.color;
            Color portalTint = GetPortalTint(p, occludedColor);
            if (inViewport)
            {
                // When occluded and in view, use occluded alpha with portal tint
                portalTint.a = occludedColor.a;
                GUI.color = portalTint;
            }
            else
            {
                // Offscreen: same tint, offscreen alpha
                portalTint.a = offscreenColor.a;
                GUI.color = portalTint;
            }

            // Draw corners around (sx, sy)
            DrawCorners(new Vector2(sx, sy), squareSize, cornerLen, thickness);

            GUI.color = prev;
        }
    }

    // Try to pull a representative color from the portal's renderer material
    Color GetPortalTint(Portals portal, Color fallback)
    {
        if (portal == null) return fallback;
        // Look for a renderer on the portal or its children
        Renderer r = portal.GetComponentInChildren<Renderer>();
        if (r != null && r.sharedMaterial != null)
        {
            var m = r.sharedMaterial;
            if (m.HasProperty("_BaseColor"))
            {
                return m.GetColor("_BaseColor");
            }
            if (m.HasProperty("_Color"))
            {
                return m.GetColor("_Color");
            }
        }
        return fallback;
    }

    void DrawCorners(Vector2 center, float size, float len, float thick)
    {
        float half = size * 0.5f;
        float x = center.x - half;
        float y = center.y - half;

        // Top-Left corner
        DrawRect(new Rect(x, y, len, thick));                     // horizontal
        DrawRect(new Rect(x, y, thick, len));                     // vertical

        // Top-Right corner
        DrawRect(new Rect(x + size - len, y, len, thick));        // horizontal
        DrawRect(new Rect(x + size - thick, y, thick, len));      // vertical

        // Bottom-Left corner
        DrawRect(new Rect(x, y + size - thick, len, thick));      // horizontal
        DrawRect(new Rect(x, y + size - len, thick, len));        // vertical

        // Bottom-Right corner
        DrawRect(new Rect(x + size - len, y + size - thick, len, thick)); // horizontal
        DrawRect(new Rect(x + size - thick, y + size - len, thick, len)); // vertical
    }

    void DrawRect(Rect r)
    {
        GUI.DrawTexture(r, _tex);
    }
}
