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

    public KeyCode teleportKey = KeyCode.T; // Key to trigger teleportation 

    [Header("Optional Overrides")]

    private float lastTeleportTime = -999f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Do nothing
    }   

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(teleportKey)) 
        {
            if (linkedPortal == null)
            {
                Debug.LogWarning($"[Portal] {name}: linkedPortal is not set.");
                return;
            }

            // Find player
            Transform player = GameObject.FindGameObjectWithTag(teleportTag)?.transform;

            if (player == null)
            {
                Debug.LogWarning($"[Portal] {name}: No player found. Set 'playerOverride' or ensure a GameObject is tagged '{teleportTag}'.");
                return;
            }

            // Resolve the root object that actually moves (controller or rigidbody owner)
            Transform playerRoot = ResolvePlayerRoot(player);

            Debug.Log($"[Portal] {name}: Teleporting '{playerRoot.name}' to {linkedPortal.name}");
            TeleportPlayer(playerRoot);
        } 
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

        Transform testPortal = GameObject.FindGameObjectWithTag("testPortal")?.transform;


        // Compute exit pose (a bit in front of the linked portal)
        Vector3 destPos = testPortal.transform.position + testPortal.transform.forward * exitOffset;
        Quaternion destRot = testPortal.transform.rotation;

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

        cc.transform.SetPositionAndRotation(destPos, destRot);

        if (cc != null)
        {
            cc.enabled = ccWasEnabled;
        }

        lastTeleportTime = Time.time;
    }

}
