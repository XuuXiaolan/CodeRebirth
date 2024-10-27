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
            if (target.CompareTag("Player"))
            {
                Rigidbody targetRigidbody = target.GetComponent<PlayerControllerB>().playerRigidbody;
                if (targetRigidbody == null) continue;

                float distanceToTarget = Vector3.Distance(turretTransform.position, target.transform.position);
                float predictionFactor = Mathf.Clamp(distanceToTarget / 5f, 0.5f, 5f); // Increase prediction based on distance
                Vector3 futurePosition = target.transform.position + targetRigidbody.velocity * predictionFactor;
                Vector3 directionToTarget = futurePosition - turretTransform.position;
                float angle = Vector3.Angle(turretTransform.up, directionToTarget);
                Plugin.ExtendedLogging($"Angle: {angle}");
                if (angle <= 45f) // Check if within 45 degrees
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
        GameObject projectile = Instantiate(projectilePrefab, projectileSpawnPoint.position, projectileSpawnPoint.rotation);
        NetworkObject networkObject = projectile.GetComponent<NetworkObject>();
        networkObject?.Spawn();
        AirUnitProjectile projectileComponent = projectile.GetComponent<AirUnitProjectile>();
        projectileComponent.Initialize(damageAmount, currentAngle);

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