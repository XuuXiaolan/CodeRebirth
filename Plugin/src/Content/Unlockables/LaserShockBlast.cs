using UnityEngine;
using Unity.Netcode;
using System.Collections;
using GameNetcodeStuff;
using System;
using CodeRebirth.src.MiscScripts;

namespace CodeRebirth.src.Content.Unlockables;
public class LaserShockBlast : NetworkBehaviour
{
    [Header("Laser Settings")]
    public float LaserRange = 100f; // Maximum range of the laser beam
    public float LaserDuration = 0.2f; // Duration the laser stays active
    private LayerMask hitLayers; // Layers to check for collisions

    [Header("Visual Effects")]
    public LineRenderer LineRenderer = null!; // LineRenderer component for the laser beam
    public GameObject ImpactEffectGameObject = null!; // Particle effect at the impact point

    [Header("Sound Effects")]
    public AudioSource AudioSource = null!; // AudioSource component for playing sound effects
    public AudioClip LaserSound = null!; // Sound effect of the laser
    public AudioClip ImpactEffectSound = null!; // Sound effect of the impact effect

    [NonSerialized] public Transform laserOrigin = null!; // Origin point of the laser beam
    private ParticleSystem impactEffect = null!; // Particle effect at the impact point

    private void Start()
    {
        hitLayers = LayerMask.GetMask("Player", "Enemies") | StartOfRound.Instance.collidersAndRoomMaskAndDefault;
        impactEffect = ImpactEffectGameObject.GetComponent<ParticleSystem>();
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
        var raycastHits = Physics.RaycastAll(origin, direction, LaserRange, hitLayers, QueryTriggerInteraction.Collide);
        bool hitSomething = false;
        foreach (var raycastHit in raycastHits)
        {
            Transform? parent = TryFindRoot(raycastHit.collider.transform);
            if (parent != null && parent.TryGetComponent<EnemyAI>(out EnemyAI enemyAI) && enemyAI != null)
            {
                // Handle hit on the server
                HandleHit(null, enemyAI);
                // Send hit info to clients for visual effects
                FireLaserClientRpc(raycastHit.point);
                hitSomething = true;
                break;
            }
            if (raycastHit.collider.gameObject.TryGetComponent<PlayerControllerB>(out var player) && player != null)
            {
                // Handle hit on the server
                HandleHit(player, null);
                // Send hit info to clients for visual effects
                FireLaserClientRpc(raycastHit.point);
                hitSomething = true;
                break;
            }

            // Send hit info to clients for visual effects
            FireLaserClientRpc(raycastHit.point);
            hitSomething = true;
        }

        if (!hitSomething)
        {
            // No hit detected, send laser to maximum range
            FireLaserClientRpc(origin + direction * LaserRange);
        }

        // Wait for the laser duration
        yield return new WaitForSeconds(impactEffect.main.duration + LaserDuration + 5f);

        // Despawn the laser object on the server
        NetworkObject.Despawn();
    }

    [ClientRpc]
    private void FireLaserClientRpc(Vector3 hitPoint)
    {
        // Visual effects for clients
        if (LineRenderer != null)
        {
            AudioSource.gameObject.transform.position = laserOrigin.position;
            AudioSource.PlayOneShot(LaserSound);
            LineRenderer.SetPosition(0, laserOrigin.position);
            LineRenderer.SetPosition(1, hitPoint);
        }

        if (impactEffect != null)
        {
            impactEffect.transform.position = hitPoint;
            CRUtilities.CreateExplosion(hitPoint, false, 40, 1, 4, 2, CauseOfDeath.Blast, null, null);
            impactEffect.Play();
            AudioSource.gameObject.transform.position = hitPoint;
            AudioSource.PlayOneShot(ImpactEffectSound);
        }

        // Disable the laser after the duration
        StartCoroutine(DisableLaser());
    }

    private IEnumerator DisableLaser()
    {
        yield return new WaitForSeconds(LaserDuration);
        if (LineRenderer != null)
        {
            LineRenderer.enabled = false;
        }
    }

    private void HandleHit(PlayerControllerB? player, EnemyAI? enemyAI)
    {
        // Check if the hit object is an enemy
        if (enemyAI != null)
        {
            KillEnemyFromOwnerClientRpc(new NetworkObjectReference(enemyAI.gameObject.GetComponent<NetworkObject>()));
        }
        // Check if the hit object is a player
        if (player != null)
        {
            int damageAmount = player.health - 1;
            if (damageAmount > 0)
            {
                player.DamagePlayer(damageAmount, true, true);
            }
        }
    }

    [ClientRpc]
    private void KillEnemyFromOwnerClientRpc(NetworkObjectReference networkObjectReference)
    {
        EnemyAI enemyAI = ((GameObject)networkObjectReference).GetComponent<EnemyAI>();
        enemyAI.KillEnemyOnOwnerClient();
    }

    public static Transform? TryFindRoot(Transform child)
    {
        Transform current = child;
        while (current != null)
        {
            if (current.GetComponent<NetworkObject>() != null)
            {
                return current;
            }
            current = current.transform.parent;
        }
        return null;
    }
}