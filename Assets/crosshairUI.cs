using UnityEngine;

public class CrosshairUI : MonoBehaviour
{
    [Header("Source (optional but recommended)")]
    [Tooltip("If assigned, the crosshair will use this shooter's camera, distance and layer mask to indicate valid aim.")]
    public PortalShooter shooter;  // Drag the same object that has PortalShooter

    [Header("Appearance")]
    public float size = 8f;                    // Crosshair size (pixels)
    public Color validColor = Color.cyan;      // When aiming at a valid surface
    public Color invalidColor = Color.red;     // When not aiming at a valid surface
    public float edgeThickness = 2f;           // Thickness of the ring/border

    private Texture2D _tex;

    private void Awake()
    {
        // 1x1 white texture for GUI drawing
        _tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        _tex.SetPixel(0, 0, Color.white);
        _tex.Apply();

        if (shooter == null)
        {
            shooter = FindObjectOfType<PortalShooter>();
        }
    }

    private void OnGUI()
    {
        if (!Application.isPlaying) return;

        // Determine if aim is valid
        bool isValid = false;
        if (shooter != null && shooter.aimCamera != null)
        {
            Ray ray = new Ray(shooter.aimCamera.transform.position, shooter.aimCamera.transform.forward);
            isValid = Physics.Raycast(ray, out _, shooter.maxShootDistance, shooter.placementMask, QueryTriggerInteraction.Ignore);
        }

        // Choose color
        Color c = isValid ? validColor : invalidColor;
        var prev = GUI.color;
        GUI.color = c;

        // Center position
        float x = (Screen.width - size) * 0.5f;
        float y = (Screen.height - size) * 0.5f;

        // Draw a simple dot with a subtle border (two rectangles)
        // Border
        Rect outer = new Rect(x - edgeThickness * 0.5f, y - edgeThickness * 0.5f, size + edgeThickness, size + edgeThickness);
        GUI.DrawTexture(outer, _tex);

        // Core (slightly darker for contrast)
        GUI.color = new Color(c.r * 0.8f, c.g * 0.8f, c.b * 0.8f, c.a);
        Rect inner = new Rect(x, y, size, size);
        GUI.DrawTexture(inner, _tex);

        GUI.color = prev;
    }
}