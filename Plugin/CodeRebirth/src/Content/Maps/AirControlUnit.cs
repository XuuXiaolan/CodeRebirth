using System.Collections;
using System.Collections.Generic;
using CodeRebirth.src.MiscScripts;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class AirControlUnit : CodeRebirthHazard
{
    public Transform turretTransform = null!;
    public Transform turretCannonTransform = null!;
    public Transform projectileSpawnPoint = null!;
    public float rotationSpeed = 45f;
    public float fireRate = 1f;
    public float detectionRange = 50f;
    public AudioSource ACUAudioSource = null!;
    public AudioClip ACUFireSound = null!;
    public AudioClip ACURechargeSound = null!;
    public AudioSource ACUClickingAudioSource = null!;
    public AudioSource DetectPlayerAudioSound = null!;
    public AudioClip ACUStartOrEndSound = null!;
    public AudioSource ACUTurnAudioSource = null!;
    private float playerHeadstart = 20f;
    public float maxAngle = 75f;

    [HideInInspector]
    public static List<ACUnitBounds> safeBounds = new();
    private float currentAngle = 0f;
    private float fireTimer = 3f;
    private GameObject projectilePrefab = null!;
    private PlayerControllerB? lastPlayerTargetted = null;

    public override void Start()
    {
        base.Start();
        projectilePrefab = MapObjectHandler.Instance.AirControlUnit!.ProjectilePrefab;
        lastPlayerTargetted = null;
        DetectPlayerAudioSound.volume = 0f;
        ACUClickingAudioSource.Stop();
        ACUClickingAudioSource.clip = null;
        ACUTurnAudioSource.volume = 0f;
    }

    private void Update()
    {
        playerHeadstart -= Time.deltaTime;
        if (StartOfRound.Instance.shipIsLeaving || playerHeadstart > 0) return;
        // Rotate the turret to look for targets
        FindAndAimAtTarget();

        if (lastPlayerTargetted == null)
        {
            fireTimer = fireRate;
            return;
        }

        // Handle firing logic
        fireTimer -= Time.deltaTime;
        if (fireTimer <= 0f)
        {
            FireProjectile();
            fireTimer = fireRate;
        }
        UpdateAudio();
    }

    private void UpdateAudio()
    {
        float volume = Plugin.ModConfig.ConfigACUVolume.Value;
        ACUAudioSource.volume = volume;
        ACUClickingAudioSource.volume = volume;
    }

    private bool IsPlayerNearGround(PlayerControllerB playerControllerB)
    {
        if (playerControllerB.isClimbingLadder)
            return true;

        return playerControllerB.IsPlayerNearGround();
    }

    private void FindAndAimAtTarget()
    {
        bool lockedOntoAPlayer = false;
        if (lastPlayerTargetted != null && !StartOfRound.Instance.shipInnerRoomBounds.bounds.Contains(lastPlayerTargetted.transform.position) && !IsPlayerNearGround(lastPlayerTargetted) && !TransformInSafeBounds(lastPlayerTargetted.transform))
        {
            HandleTargettingToPlayer(lastPlayerTargetted, ref lockedOntoAPlayer);
        }
        else
        {
            foreach (PlayerControllerB playerControllerB in StartOfRound.Instance.allPlayerScripts)
            {
                if (playerControllerB == null || playerControllerB.isPlayerDead || !playerControllerB.isPlayerControlled || StartOfRound.Instance.shipInnerRoomBounds.bounds.Contains(playerControllerB.transform.position) || IsPlayerNearGround(playerControllerB) || TransformInSafeBounds(playerControllerB.transform))
                {
                    continue;
                }

                HandleTargettingToPlayer(playerControllerB, ref lockedOntoAPlayer);
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

    private bool TransformInSafeBounds(Transform toKillTransform)
    {
        foreach (var bounds in safeBounds)
        {
            if (bounds.BoundsContainTransform(toKillTransform))
            {
                return true;
            }
        }
        return false;
    }

    private void HandleTargettingToPlayer(PlayerControllerB playerControllerB, ref bool lockedOntoAPlayer)
    {
        Rigidbody targetRigidbody = playerControllerB.playerRigidbody;
        if (targetRigidbody == null) return;

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
            if (!Physics.Linecast(turretCannonTransform.position, playerControllerB.transform.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
            {
                lockedOntoAPlayer = true;
                lastPlayerTargetted = playerControllerB;
                if (ACUClickingAudioSource.clip == null)
                {
                    DetectPlayerAudioSound.volume = Plugin.ModConfig.ConfigACUVolume.Value;
                    ACUClickingAudioSource.Stop();
                    ACUClickingAudioSource.clip = ACUStartOrEndSound;
                    ACUClickingAudioSource.Play();
                }
                ACUTurnAudioSource.volume = Plugin.ModConfig.ConfigACUVolume.Value;
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
    private void FireProjectile()
    {
        if (lastPlayerTargetted == null) return;
        // play shoot sound
        if (Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, this.transform.position) <= 70)
        {
            HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
            HUDManager.Instance.ShakeCamera(ScreenShakeType.Long);
        }
        ACUAudioSource.PlayOneShot(ACUFireSound);
        if (IsServer)
        {
            GameObject projectile = Instantiate(projectilePrefab, projectileSpawnPoint.position, projectileSpawnPoint.rotation);
            NetworkObject networkObject = projectile.GetComponent<NetworkObject>();
            networkObject.Spawn();
            AirUnitProjectile projectileComponent = projectile.GetComponent<AirUnitProjectile>();
            projectileComponent.Initialize(Plugin.ModConfig.ConfigAirControlUnitDamage.Value, currentAngle, lastPlayerTargetted);

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