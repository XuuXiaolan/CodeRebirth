using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class AirControlUnit : NetworkBehaviour
{
    public Transform turretTransform = null!;
    public Transform turretCannonTransform = null!;
    public Transform projectileSpawnPoint = null!;
    public float rotationSpeed = 45f;
    public float fireRate = 1f;
    public float detectionRange = 50f;
    public float damageAmount = 50f;

    private float fireTimer = 0f;
    private GameObject projectilePrefab = null!;

    private void Start()
    {
        projectilePrefab = MapObjectHandler.Instance.AirControlUnit.ProjectilePrefab;
    }

    private void Update()
    {
        if (!IsServer) return;

        // Rotate the turret to look for targets
        FindAndAimAtTarget();
        
        // Handle firing logic
        fireTimer -= Time.deltaTime;
        if (fireTimer <= 0f)
        {
            FireProjectile();
            fireTimer = fireRate;
        }
    }

    private void FindAndAimAtTarget()
    {
        Collider[] targets = Physics.OverlapSphere(turretTransform.position, detectionRange, StartOfRound.Instance.collidersAndRoomMaskAndPlayers, QueryTriggerInteraction.Collide);
        foreach (Collider target in targets)
        {
            if (target.CompareTag("Player"))
            {
                Vector3 directionToTarget = target.transform.position - turretTransform.position;
                float angle = Vector3.Angle(turretTransform.up, directionToTarget);
                Plugin.ExtendedLogging($"Angle: {angle}");
                if (angle <= 45f) // Check if within 45 degrees
                {
                    Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                    targetRotation.z = 0f;
                    targetRotation.x = 0f;
                    turretTransform.rotation = Quaternion.RotateTowards(turretTransform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

                    // Rotate the turret cannon to aim at the target
                    Vector3 cannonDirection = target.transform.position - turretCannonTransform.position;
                    Quaternion cannonTargetRotation = Quaternion.LookRotation(cannonDirection);
                    cannonTargetRotation.y = turretCannonTransform.rotation.y; // Keep y rotation fixed
                    cannonTargetRotation.z = 0f; // Only allow up/down rotation
                    turretCannonTransform.rotation = Quaternion.RotateTowards(turretCannonTransform.rotation, cannonTargetRotation, rotationSpeed * Time.deltaTime);
                }
            }
        }
    }

    private void FireProjectile()
    {
        GameObject projectile = Instantiate(projectilePrefab, projectileSpawnPoint.position, projectileSpawnPoint.rotation);
        NetworkObject networkObject = projectile.GetComponent<NetworkObject>();
        networkObject?.Spawn();
        AirUnitProjectile projectileComponent = projectile.GetComponent<AirUnitProjectile>();
        projectileComponent.Initialize(damageAmount, this);
    }
}