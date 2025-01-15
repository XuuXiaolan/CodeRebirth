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

    public void Start()
    {
        hitLayers = LayerMask.GetMask("Enemies", "Room", "Player", "Colliders", "Vehicle", "Terrain");
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

        var raycastHits = Physics.RaycastAll(origin, direction, LaserRange, hitLayers, QueryTriggerInteraction.Collide);
        Array.Sort(raycastHits, (a, b) => a.distance.CompareTo(b.distance));

        Vector3 finalHitPoint = origin + (direction * LaserRange);

        foreach (var raycastHit in raycastHits)
        {
            bool hit = HandleHit(raycastHit);
            finalHitPoint = raycastHit.point; // Update final hit point
            Plugin.ExtendedLogging($"Raycast Hit: {raycastHit.collider.name}");
            Plugin.ExtendedLogging($"Final Hit Point: {finalHitPoint}");
            Plugin.ExtendedLogging($"Hit: {hit}");
            if (hit)
                break; // Stop if we have hit a target
        }

        yield return new WaitUntil(() => NetworkObject.IsSpawned);
        // Send hit info to clients only once with the final hit point
        FireLaserClientRpc(finalHitPoint, laserOrigin.position);

        // Wait for the laser duration
        yield return new WaitForSeconds(impactEffect.main.duration + LaserDuration + 5f);

        // Despawn the laser object on the server
        NetworkObject.Despawn();
    }

    [ClientRpc]
    private void FireLaserClientRpc(Vector3 hitPoint, Vector3 laserOrigin)
    {
        // Visual effects for clients
        if (LineRenderer != null)
        {
            AudioSource.gameObject.transform.position = laserOrigin;
            AudioSource.PlayOneShot(LaserSound);
            LineRenderer.SetPosition(0, laserOrigin);
            LineRenderer.SetPosition(1, hitPoint);
        }

        if (impactEffect != null)
        {
            impactEffect.transform.position = hitPoint;
            CRUtilities.CreateExplosion(hitPoint, false, 40, 1, 4, 2, null, null);
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

    private bool HandleEnemyHit(RaycastHit raycastHit)
    {
        Transform? parent = CRUtilities.TryFindRoot(raycastHit.collider.transform);
        if (parent == null || !parent.TryGetComponent(out EnemyAI enemyAI) || enemyAI == null) return false;

        KillEnemyFromOwnerClientRpc(new NetworkObjectReference(enemyAI.gameObject.GetComponent<NetworkObject>()));
        return true;
    }

    private bool HandlePlayerHit(RaycastHit raycastHit)
    {
        if (!raycastHit.collider.gameObject.TryGetComponent(out PlayerControllerB player) || player == null) return false;

        int damageAmount = player.health - 1;
        if (damageAmount > 0)
        {
            player.DamagePlayer(damageAmount, true, true);
        }
        return true;
    }

    private bool HandleTerrainOrGroundHit(RaycastHit raycastHit)
    {
        if (raycastHit.collider.gameObject.layer == LayerMask.NameToLayer("Terrain") || raycastHit.collider.gameObject.layer == LayerMask.NameToLayer("Room"))
        {
            return true;
        }
        return false;
    }

    private bool HandleHit(RaycastHit raycastHit)
    {
        return HandleEnemyHit(raycastHit) || HandlePlayerHit(raycastHit) || HandleTerrainOrGroundHit(raycastHit);
    }

    [ClientRpc]
    private void KillEnemyFromOwnerClientRpc(NetworkObjectReference networkObjectReference)
    {
        EnemyAI enemyAI = ((GameObject)networkObjectReference).GetComponent<EnemyAI>();
        enemyAI.KillEnemyOnOwnerClient();
    }
}