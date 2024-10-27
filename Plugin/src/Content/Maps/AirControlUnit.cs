using System.Collections;
using GameNetcodeStuff;
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

    private float currentAngle = 0f;
    private float fireTimer = 3f;
    private GameObject projectilePrefab = null!;
    private PlayerControllerB? lastPlayerTargetted = null;

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
        Collider[] targets = Physics.OverlapSphere(turretTransform.position, detectionRange, LayerMask.GetMask("Player"), QueryTriggerInteraction.Collide);
        foreach (Collider target in targets)
        {
            if (Physics.Raycast(turretCannonTransform.position, target.gameObject.transform.position - turretCannonTransform.position, out RaycastHit _, detectionRange, StartOfRound.Instance.collidersAndRoomMask)) return;
            if (target.CompareTag("Player"))
            {
                PlayerControllerB playerControllerB = target.GetComponent<PlayerControllerB>();
                Rigidbody targetRigidbody = playerControllerB.playerRigidbody;
                if (targetRigidbody == null) continue;
                lastPlayerTargetted = playerControllerB;
                // Calculate the time needed for the projectile to reach the target
                float distanceToTarget = Vector3.Distance(turretTransform.position, target.transform.position);
                float timeToTarget = distanceToTarget / 50f; // Bullet speed is 100 but we overshootin cuz overshooting is good

                // Predict future position of the target based on its current velocity and time to target
                Vector3 futurePosition = target.transform.position + targetRigidbody.velocity * timeToTarget;

                // Calculate direction to the predicted position
                Vector3 directionToTarget = futurePosition - turretTransform.position;
                float angle = Vector3.Angle(turretTransform.up, directionToTarget);

                if (angle <= 60f) // Check if within 60 degrees
                {
                    currentAngle = angle;
                    Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                    targetRotation.z = 0f;
                    targetRotation.x = 0f;

                    // Rotate the base turret (left/right)
                    turretTransform.rotation = Quaternion.RotateTowards(turretTransform.rotation, targetRotation, rotationSpeed * Time.deltaTime * 5f);

                    // Set the pitch angle for the turret cannon
                    Vector3 currentLocalEulerAngles = turretCannonTransform.localEulerAngles;
                    turretCannonTransform.localEulerAngles = new Vector3(angle, currentLocalEulerAngles.y, currentLocalEulerAngles.z);
                }
            }
        }
    }

    private void FireProjectile()
    {
        if (lastPlayerTargetted == null) return;
        GameObject projectile = Instantiate(projectilePrefab, projectileSpawnPoint.position, projectileSpawnPoint.rotation);
        NetworkObject networkObject = projectile.GetComponent<NetworkObject>();
        networkObject?.Spawn();
        AirUnitProjectile projectileComponent = projectile.GetComponent<AirUnitProjectile>();
        projectileComponent.Initialize(damageAmount, currentAngle, lastPlayerTargetted);

        // Rattle the cannon's transform to emulate a shake effect
        StartCoroutine(RattleCannon());
    }

    private IEnumerator RattleCannon()
    {
        Vector3 originalPosition = turretCannonTransform.localPosition;
        float shakeDuration = 0.2f;
        float shakeIntensity = 0.5f;

        while (shakeDuration > 0f)
        {
            turretCannonTransform.localPosition = originalPosition + (Vector3)UnityEngine.Random.insideUnitCircle * shakeIntensity;
            shakeDuration -= Time.deltaTime;
            yield return null;
        }

        turretCannonTransform.localPosition = originalPosition;
    }
}