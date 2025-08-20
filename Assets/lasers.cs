using UnityEngine;
using System.Collections.Generic;

public class lasers : MonoBehaviour
{
    [Header("Emitter & Firing")]
    [Tooltip("If left empty, this GameObject's transform is used as the emitter.")]
    public Transform emitter;
    [Tooltip("Laser max distance in world units.")]
    public float maxDistance = 50f;
    [Tooltip("Fire continuously without input.")]
    public bool continuous = true;
    [Tooltip("Hold this mouse button to fire when 'continuous' is false. 0=Left, 1=Right, 2=Middle")]
    public int fireMouseButton = 0;

    [Header("Collision Filtering")]
    [Tooltip("Layers the laser can hit.")]
    public LayerMask hitMask = ~0; // Everything by default
    [Tooltip("If true, rays can hit Trigger colliders (useful if portal surfaces are triggers).")]
    public bool includeTriggerColliders = true;

    [Header("Visuals")]
    public LineRenderer line;
    [Tooltip("Width of the laser beam.")]
    public float lineWidth = 0.03f;

    [Header("Portals Integration")]
    [Tooltip("Maximum number of times the laser can traverse portals in a single frame.")]
    public int maxPortalPasses = 4;
    [Tooltip("Small offset forward from the exit portal plane to avoid immediate self-hit.")]
    public float portalEpsilon = 0.02f;
    [Tooltip("Tag used to identify portal surfaces when a Portals component isn't found.")]
    public string portalTag = "portal";

    [Header("Debugging")]
    [Tooltip("If true, logs laser hits and portal traversals to the Console.")]
    public bool debugLogs = false;

    // Pool of LineRenderers to render discontinuous segments (to avoid connecting line between portals)
    private readonly List<LineRenderer> segmentLines = new List<LineRenderer>();

    void Awake()
    {
        if (emitter == null) emitter = transform;

        if (line == null)
        {
            line = GetComponent<LineRenderer>();
            if (line == null)
            {
                line = gameObject.AddComponent<LineRenderer>();
            }
        }

        // Basic LineRenderer setup
        line.enabled = false;
        line.positionCount = 2;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        // Use a default material if none assigned (Unity will auto-assign a Line material in most pipelines)
        if (line.sharedMaterial == null)
        {
            // Fallback to a simple material with built-in shader if available
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = Color.red;
            line.sharedMaterial = mat;
        }
        line.startColor = Color.red;
        line.endColor = Color.red;

        // Initialize segment pool with the primary line
        if (segmentLines.Count == 0)
            segmentLines.Add(line);
    }

    void Update()
    {
        bool shouldFire = continuous || Input.GetMouseButton(fireMouseButton);
        if (shouldFire)
        {
            FireLaser();
        }
        else
        {
            if (line.enabled) line.enabled = false;
        }
    }

    private void FireLaser()
    {
        if (emitter == null) return;

        Vector3 origin = emitter.position;
        Vector3 dir = emitter.forward;
        float remaining = maxDistance;

        // Build segments as start->end pairs
        List<Vector3> segStarts = new List<Vector3>(1 + maxPortalPasses);
        List<Vector3> segEnds = new List<Vector3>(1 + maxPortalPasses);

        int traversals = 0;
        bool continueTracing = true;
        while (continueTracing && remaining > 0f)
        {
            QueryTriggerInteraction qti = includeTriggerColliders ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore;
            if (Physics.Raycast(origin, dir, out RaycastHit hit, remaining, hitMask, qti))
            {
                // Add segment to the hit point (stops exactly at the portal or object)
                segStarts.Add(origin);
                segEnds.Add(hit.point);
                if (debugLogs)
                {
                    Debug.Log($"[Laser] Hit '{hit.collider.name}' at {hit.point} dist={hit.distance:F2} layer={hit.collider.gameObject.layer}");
                }

                // Portal check: prefer component, fallback to tag
                Portals portal = hit.collider.GetComponentInParent<Portals>();
                bool isTaggedPortal = !string.IsNullOrEmpty(portalTag) && hit.collider.CompareTag(portalTag);
                if (portal != null || isTaggedPortal)
                {
                    if (portal == null)
                    {
                        // Try to locate a Portals component on the same object or its parents
                        portal = hit.collider.GetComponentInParent<Portals>();
                    }

                    if (portal != null && traversals < maxPortalPasses)
                    {
                        Vector3 outOrigin, outDir;
                        if (portal.TransformRayThroughPortal(hit.point, dir, portalEpsilon, out outOrigin, out outDir))
                        {
                            // Reduce remaining by traveled segment
                            remaining -= Vector3.Distance(origin, hit.point);
                            // Start next segment from the exit portal
                            origin = outOrigin;
                            dir = outDir;
                            traversals++;
                            if (debugLogs)
                            {
                                Debug.Log($"[Laser] Traversed portal '{portal.name}' -> exit at {outOrigin}, dir {outDir}");
                            }
                            // Continue tracing from the exit portal without drawing a connector
                            continue;
                        }
                    }
                }

                // Hit a normal object or cannot traverse further
                continueTracing = false;
            }
            else
            {
                // No hit: extend to max
                segStarts.Add(origin);
                segEnds.Add(origin + dir * remaining);
                if (debugLogs)
                {
                    Debug.Log($"[Laser] No hit. Extending to {segEnds[segEnds.Count-1]}");
                }
                continueTracing = false;
            }
        }

        DrawSegments(segStarts, segEnds);
    }

    private void DrawSegments(List<Vector3> starts, List<Vector3> ends)
    {
        int segmentCount = (starts != null && ends != null) ? Mathf.Min(starts.Count, ends.Count) : 0;

        // Ensure pool size
        while (segmentLines.Count < segmentCount)
        {
            segmentLines.Add(CreateLineLike(line));
        }

        // Update active segments
        for (int i = 0; i < segmentCount; i++)
        {
            var lr = segmentLines[i];
            if (lr == null) continue;
            if (!lr.enabled) lr.enabled = true;
            if (!Mathf.Approximately(lr.startWidth, lineWidth))
            {
                lr.startWidth = lineWidth;
                lr.endWidth = lineWidth;
            }
            lr.positionCount = 2;
            lr.SetPosition(0, starts[i]);
            lr.SetPosition(1, ends[i]);
        }

        // Disable any unused renderers
        for (int i = segmentCount; i < segmentLines.Count; i++)
        {
            if (segmentLines[i] != null && segmentLines[i].enabled)
                segmentLines[i].enabled = false;
        }
    }

    private LineRenderer CreateLineLike(LineRenderer template)
    {
        GameObject go = new GameObject("LaserSegment");
        go.transform.SetParent(transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.textureMode = LineTextureMode.Stretch;
        lr.alignment = LineAlignment.View;
        lr.sharedMaterial = template != null ? template.sharedMaterial : new Material(Shader.Find("Sprites/Default"));
        lr.startColor = template != null ? template.startColor : Color.red;
        lr.endColor = template != null ? template.endColor : Color.red;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.enabled = false;
        return lr;
    }
}
