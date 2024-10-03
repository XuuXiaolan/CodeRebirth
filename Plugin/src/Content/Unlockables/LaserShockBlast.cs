using UnityEngine;
using Unity.Netcode;
using System.Collections;
using GameNetcodeStuff;
using System;

namespace CodeRebirth.src.Content.Unlockables;
public class LaserShockBlast : NetworkBehaviour
{
    [Header("Laser Settings")]
    public float laserRange = 20f; // Maximum range of the laser beam
    public float laserDuration = 0.2f; // Duration the laser stays active
    private LayerMask hitLayers; // Layers to check for collisions

    [Header("Visual Effects")]
    public LineRenderer lineRenderer = null!; // LineRenderer component for the laser beam
    public ParticleSystem impactEffect = null!; // Particle effect at the impact point
    [NonSerialized] public Transform laserOrigin = null!; // Origin point of the laser beam

    private void Start()
    {
        hitLayers = LayerMask.GetMask("Player", "Enemies");
        if (IsServer)
        {
            // Start the laser effect on the server
            StartCoroutine(FireLaser());
        }
    }

    private IEnumerator FireLaser()
    {
        Vector3 origin = laserOrigin.position;
        Vector3 direction = laserOrigin.forward;

        // Perform a raycast to detect hits
        if (Physics.Raycast(origin, direction, out RaycastHit hit, laserRange, hitLayers, QueryTriggerInteraction.Ignore))
        {
            // Handle hit on the server
            HandleHit(hit.collider);

            // Send hit info to clients for visual effects
            FireLaserClientRpc(hit.point);
        }
        else
        {
            // No hit detected, send laser to maximum range
            FireLaserClientRpc(origin + direction * laserRange);
        }

        // Wait for the laser duration
        yield return new WaitForSeconds(laserDuration);

        // Despawn the laser object on the server
        if (IsServer)
        {
            NetworkObject.Despawn();
        }
    }

    [ClientRpc]
    private void FireLaserClientRpc(Vector3 hitPoint)
    {
        // Visual effects for clients
        if (lineRenderer != null)
        {
            lineRenderer.SetPosition(0, laserOrigin.position);
            lineRenderer.SetPosition(1, hitPoint);
        }

        if (impactEffect != null)
        {
            impactEffect.transform.position = hitPoint;
            impactEffect.Play();
        }

        // Disable the laser after the duration
        StartCoroutine(DisableLaser());
    }

    private IEnumerator DisableLaser()
    {
        yield return new WaitForSeconds(laserDuration);
        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
    }

    private void HandleHit(Collider hitCollider)
    {
        // Check if the hit object is an enemy
        if (hitCollider.gameObject.layer == LayerMask.GetMask("Enemies"))
        {
            EnemyAI enemy = hitCollider.GetComponent<EnemyAI>();
            enemy?.KillEnemyOnOwnerClient();
        }
        // Check if the hit object is a player
        else if (hitCollider.gameObject.layer == LayerMask.GetMask("Player"))
        {
            PlayerControllerB player = hitCollider.GetComponent<PlayerControllerB>();
            if (player != null)
            {
                int damageAmount = player.health - 1;
                if (damageAmount > 0)
                {
                    player.DamagePlayer(damageAmount, true, true);
                }
            }
        }
    }
}