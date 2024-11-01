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
    public AudioSource ACUAudioSource = null!;
    public AudioClip ACUFireSound = null!;
    public AudioClip ACURechargeSound = null!;
    public AudioSource ACUClickingAudioSource = null!;
    public AudioSource DetectPlayerAudioSound = null!;
    public AudioClip ACUStartOrEndSound = null!;
    public AudioSource ACUTurnAudioSource = null!;
    public float playerHeadstart = 10f;
    public float maxAngle = 75f;

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
        playerHeadstart -= Time.deltaTime;
        if (StartOfRound.Instance.shipIsLeaving || playerHeadstart > 0) return;
        // Rotate the turret to look for targets
        FindAndAimAtTarget();

        // Handle firing logic
        fireTimer -= Time.deltaTime;
        if (fireTimer <= 0f)
        {
            FireProjectile();
            if (Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, this.transform.position) <= 70)
            {
                HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
                HUDManager.Instance.ShakeCamera(ScreenShakeType.Long);
            }
            fireTimer = fireRate;
        }
    }

    private bool IsPlayerNearGround(PlayerControllerB playerControllerB)
    {
        if (Physics.Raycast(playerControllerB.gameplayCamera.transform.position, -Vector3.up, out RaycastHit hit, 6f, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore))
        {
            return true;
        }
        return false;
    }

    private void FindAndAimAtTarget()
    {
        bool lockedOntoAPlayer = false;
        foreach (PlayerControllerB playerControllerB in StartOfRound.Instance.allPlayerScripts)
        {
            if (playerControllerB == null || playerControllerB.isPlayerDead || !playerControllerB.isPlayerControlled || StartOfRound.Instance.shipInnerRoomBounds.bounds.Contains(playerControllerB.transform.position) || IsPlayerNearGround(playerControllerB))
            {            
                continue;
            }

            Rigidbody targetRigidbody = playerControllerB.playerRigidbody;
            if (targetRigidbody == null) continue;

            Vector3 directionToPlayer = (playerControllerB.transform.position - turretCannonTransform.position).normalized;
            float distanceToPlayer = Vector3.Distance(turretCannonTransform.position, playerControllerB.transform.position);

            // Calculate the time needed for the projectile to reach the target
            float timeToTarget = distanceToPlayer / 50f; // Bullet speed is 100 but we overshootin cuz overshooting is good

            // Predict future position of the target based on its current velocity and time to target
            Vector3 futurePosition = playerControllerB.transform.position + targetRigidbody.velocity * timeToTarget;

            // Calculate direction to the predicted position
            Vector3 directionToTarget = futurePosition - turretTransform.position;
            float angle = Vector3.Angle(turretTransform.up, directionToTarget);

            // Check if player is within detection range and if there's line of sight
            if (distanceToPlayer <= detectionRange && angle <= maxAngle)
            {
                Plugin.ExtendedLogging($"Angle: {angle} | Distance: {distanceToPlayer}");
                if (Physics.Raycast(turretCannonTransform.position, directionToPlayer, out RaycastHit hit, detectionRange, StartOfRound.Instance.collidersRoomMaskDefaultAndPlayers, QueryTriggerInteraction.Collide)) // Check if within 75 degrees
                {
                    Plugin.ExtendedLogging($"Raycast hit: {hit.collider.name} | with layer: {hit.collider.gameObject.layer}");
                    if (hit.collider.gameObject.layer != 3) continue;
                    lockedOntoAPlayer = true;
                    lastPlayerTargetted = playerControllerB;
                    if (ACUClickingAudioSource.clip == null)
                    {
                        DetectPlayerAudioSound.volume = 1f;
                        ACUClickingAudioSource.Stop();
                        ACUClickingAudioSource.clip = ACUStartOrEndSound;
                        ACUClickingAudioSource.Play();
                    }
                    ACUTurnAudioSource.volume = 1f;
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
        if (!lockedOntoAPlayer)
        {
            lastPlayerTargetted = null;
            DetectPlayerAudioSound.volume = 0f;
            ACUClickingAudioSource.Stop();
            ACUClickingAudioSource.clip = null;
            ACUTurnAudioSource.volume = 0f;
        }
    }

    private void FireProjectile()
    {
        if (lastPlayerTargetted == null) return;
        // play shoot sound
        ACUAudioSource.PlayOneShot(ACUFireSound);
        if (IsServer)
        {
            GameObject projectile = Instantiate(projectilePrefab, projectileSpawnPoint.position, projectileSpawnPoint.rotation);
            NetworkObject networkObject = projectile.GetComponent<NetworkObject>();
            networkObject?.Spawn();
            AirUnitProjectile projectileComponent = projectile.GetComponent<AirUnitProjectile>();
            projectileComponent.Initialize(damageAmount, currentAngle, lastPlayerTargetted);

            // Rattle the cannon's transform to emulate a shake effect
            StartCoroutine(RattleCannon());
        }
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
        ACUAudioSource.PlayOneShot(ACURechargeSound);
    }
}