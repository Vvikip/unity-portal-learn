using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class Portals : MonoBehaviour
{
    [Header("Portal Settings")]
    public Portals linkedPortal;              // Set this in Inspector
    public float exitOffset = 1.5f;           // Distance placed in front of exit
    public string teleportTag = "Player";     // Tag to filter (optional)
    public float reenterBlockTime = 0.25f;    // Time to ignore collisions post-teleport
    public bool requireTagMatch = false;      // If true, only teleport when tag matches

    private float lastTeleportTime = -999f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Do nothing
    }   

    // Update is called once per frame
    void Update()
    {
        // Input-based teleport disabled; using trigger-based teleport in OnTriggerEnter
    }

    private void OnTriggerEnter(Collider other)
    {
        if (linkedPortal == null)
        {
            Debug.LogWarning($"[Portal] {name}: linkedPortal is not set.");
            return;
        }

        // Cooldown to avoid immediate re-entry loop
        if (Time.time - lastTeleportTime < reenterBlockTime)
            return;
        if (Time.time - linkedPortal.lastTeleportTime < linkedPortal.reenterBlockTime)
            return;

        // Optional tag filter
        if (requireTagMatch && !other.CompareTag(teleportTag))
            return;

        Transform target = ResolvePlayerRoot(other.transform);
        if (target == null)
            return;

        Debug.Log($"[Portal] {name}: Triggered by '{other.name}'. Teleporting '{target.name}' to {linkedPortal.name}");
        TeleportPlayer(target);

        // Set cooldown on both portals to prevent ping-pong
        lastTeleportTime = Time.time;
        linkedPortal.lastTeleportTime = Time.time;
    }

    private Transform ResolvePlayerRoot(Transform t)
    {
        // Prefer the transform that owns the CharacterController or Rigidbody
        var cc = t.GetComponentInParent<CharacterController>();
        if (cc != null) return cc.transform;

        var rb = t.GetComponentInParent<Rigidbody>();
        if (rb != null) return rb.transform;

        return t.root != null ? t.root : t; // fallback to top-level
    }

    private void TeleportPlayer(Transform target)
    {
        if (linkedPortal == null || target == null)
            return;

        // Compute exit pose (a bit in front of the linked portal)
        Vector3 destPos = linkedPortal.transform.position + linkedPortal.transform.forward * exitOffset;
        Quaternion destRot = linkedPortal.transform.rotation;

        // Handle common controllers safely
        var cc = target.GetComponent<CharacterController>();
        var rb = target.GetComponent<Rigidbody>();

        // Temporarily disable CC to set position cleanly
        bool ccWasEnabled = false;
        if (cc != null)
        {
            ccWasEnabled = cc.enabled;
            cc.enabled = false;
        }

        // IMPORTANT: Prefer CharacterController path if present (common FPS setup)
        if (cc != null)
        {
            // CharacterController path (set on the CC owner transform)
            cc.transform.SetPositionAndRotation(destPos, destRot);
        }
        else if (rb != null)
        {
            // Rigidbody path
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;      // only valid for non-kinematic bodies
                rb.angularVelocity = Vector3.zero;
            }
            rb.MovePosition(destPos);
            rb.MoveRotation(destRot);
        }
        else
        {
            // Fallback direct transform move
            target.SetPositionAndRotation(destPos, destRot);
        }

        if (cc != null)
        {
            cc.enabled = ccWasEnabled;
            // Nudge CC to update its internal state post-teleport
            cc.Move(Vector3.zero);
        }

        // Ensure physics and transforms are up to date after the snap
        Physics.SyncTransforms();

        // Align player upright after exiting the portal (preserve yaw from exit)
        var movement = target.GetComponentInParent<PlayerMovement>();
        if (movement != null)
        {
            movement.AlignAfterPortalExit(destRot);
        }
        else
        {
            Debug.LogWarning($"[Portal] No PlayerMovement found on '{target.name}' or its parents. Upright alignment was skipped.");
        }
    }
}
