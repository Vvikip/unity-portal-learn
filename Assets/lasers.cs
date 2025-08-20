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

        List<Vector3> points = new List<Vector3>(2 + maxPortalPasses);
        points.Add(origin);

        int traversals = 0;
        bool continueTracing = true;
        while (continueTracing && remaining > 0f)
        {
            QueryTriggerInteraction qti = includeTriggerColliders ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore;
            if (Physics.Raycast(origin, dir, out RaycastHit hit, remaining, hitMask, qti))
            {
                points.Add(hit.point);
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
                            // Advance
                            origin = outOrigin;
                            dir = outDir;
                            traversals++;
                            // Insert the exit origin so the beam visually continues from the other portal
                            points.Add(outOrigin);
                            if (debugLogs)
                            {
                                Debug.Log($"[Laser] Traversed portal '{portal.name}' -> exit at {outOrigin}, dir {outDir}");
                            }
                            // Continue tracing from the exit portal
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
                points.Add(origin + dir * remaining);
                if (debugLogs)
                {
                    Debug.Log($"[Laser] No hit. Extending to {points[points.Count-1]}");
                }
                continueTracing = false;
            }
        }

        DrawBeam(points);
    }

    private void DrawBeam(List<Vector3> points)
    {
        if (!line.enabled) line.enabled = true;
        // Keep width updated in case changed at runtime
        if (!Mathf.Approximately(line.startWidth, lineWidth))
        {
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
        }
        if (points == null || points.Count < 2)
        {
            // Fallback to disable if something went wrong
            line.enabled = false;
            return;
        }
        line.positionCount = points.Count;
        for (int i = 0; i < points.Count; i++)
        {
            line.SetPosition(i, points[i]);
        }
    }
}
