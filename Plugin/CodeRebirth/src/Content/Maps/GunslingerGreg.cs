using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.MiscScripts;
using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class GunslingerGreg : CodeRebirthHazard
{
    public Transform gregBase = null!;
    public Transform gregCannon = null!;
    public float rotationSpeed = 45f;
    public float fireRate = 1f;
    public float detectionRange = 50f;
    public AudioSource GregSource = null!;
    public AudioClip[] GregFireSounds = [];
    public AudioClip[] GregResupplySounds = [];
    public AudioSource DetectPlayerAudioSound = null!;
    public AudioSource GregTurningSound = null!;
    public float playerHeadstart = 5f;
    public float maxAngle = 90f;
    public Queue<GunslingerMissile> rockets = new();
    public Transform[] rocketTransforms = [];

    private GunslingerMissile? missileToRecharge = null;
    public static List<GregBounds> safeBounds = new();
    private float rechargeRocketTimer = 30f;
    private float fireTimer = 1f;
    private GameObject MissilePrefab = null!;
    private PlayerControllerB? lastPlayerTargetted = null;

    public override void Start()
    {
        base.Start();
        MissilePrefab = MapObjectHandler.Instance.GunslingerGreg!.MissilePrefab;
        foreach (var transform in rocketTransforms)
        {
            rockets.Enqueue(SpawnImmobileRocket(transform));
        }
        lastPlayerTargetted = null;
        // DetectPlayerAudioSound.volume = 0f;
        // GregTurningSound.volume = 0f;
    }

    private void Update()
    {
        playerHeadstart -= Time.deltaTime;
        if (missileToRecharge == null)
        {
            foreach (var rocket in rockets)
            {
                if (rocket.ready) continue;
                missileToRecharge = rocket;
                break;
            }
        }
        else
        {
            RechargeRocket(missileToRecharge);
        }

        if (Plugin.ModConfig.ConfigDebugMode.Value) return;
        if (StartOfRound.Instance.shipIsLeaving || playerHeadstart > 0 || rockets.Where(x => x.ready == true).Count() <= 0) return;
        // Rotate the turret to look for targets
        FindAndAimAtTarget();

        if (lastPlayerTargetted == null) return;
        // Handle firing logic
        fireTimer -= Time.deltaTime;
        if (fireTimer <= 0f)
        {
            FireProjectile();
            fireTimer = fireRate;
        }
        // UpdateAudio();
    }

    private void UpdateAudio()
    {
        float volume = Plugin.ModConfig.ConfigACUVolume.Value;
        GregSource.volume = volume;
    }

    private GunslingerMissile SpawnImmobileRocket(Transform rocketTransform)
    {
        GameObject rocket = Instantiate(MissilePrefab, rocketTransform.position, rocketTransform.rotation, rocketTransform);
        var rocketScript = rocket.GetComponent<GunslingerMissile>();
        rocketScript.mainTransform = rocketTransform;
        rocketScript.ready = true;
        return rocketScript;
    }

    private void RechargeRocket(GunslingerMissile rocket)
    {
        rechargeRocketTimer -= Time.deltaTime;
        if (rechargeRocketTimer > 0) return;
        rechargeRocketTimer = 30f;
        rocket.ready = true;
        rocket.gameObject.SetActive(true);
        missileToRecharge = null;
    }

    private bool IsPlayerNearGround(PlayerControllerB playerControllerB)
    {
        Ray ray = new Ray(playerControllerB.transform.position, -Vector3.up);
        if (Physics.Raycast(ray, 8f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
        {
            return true;
        }
        return false;
    }

    private void FindAndAimAtTarget()
    {
        bool lockedOntoAPlayer = false;
        if (lastPlayerTargetted != null)
        {
            HandleTargettingToPlayer(lastPlayerTargetted, ref lockedOntoAPlayer);
        }
        else
        {
            foreach (PlayerControllerB playerControllerB in StartOfRound.Instance.allPlayerScripts)
            {
                if (playerControllerB == null || playerControllerB.isPlayerDead || !playerControllerB.isPlayerControlled || playerControllerB.isInHangarShipRoom || StartOfRound.Instance.shipInnerRoomBounds.bounds.Contains(playerControllerB.transform.position) || IsPlayerNearGround(playerControllerB) || PlayerInSafeBounds(playerControllerB))
                {
                    continue;
                }

                HandleTargettingToPlayer(playerControllerB, ref lockedOntoAPlayer);
            }
        }
        if (!lockedOntoAPlayer)
        {
            lastPlayerTargetted = null;
            // DetectPlayerAudioSound.volume = 0f;
            // GregTurningSound.volume = 0f;
        }
    }

    private bool PlayerInSafeBounds(PlayerControllerB playerControllerB)
    {
        foreach (var bounds in safeBounds)
        {
            if (bounds.BoundsContainTransform(playerControllerB.transform))
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

        float distanceToPlayer = Vector3.Distance(gregCannon.position, playerControllerB.transform.position);

        // Calculate the time needed for the projectile to reach the target
        float timeToTarget = distanceToPlayer / 50f; // Bullet speed is 100 but we overshootin cuz overshooting is good

        // Predict future position of the target based on its current velocity and time to target
        Vector3 futurePosition = playerControllerB.transform.position + targetRigidbody.velocity * timeToTarget;

        // Calculate direction to the predicted position
        Vector3 directionToTarget = futurePosition - gregBase.position;
        float angle = Vector3.Angle(gregBase.up, directionToTarget);

        // Plugin.ExtendedLogging($"Angle: {angle} Distance: {distanceToPlayer} Locked: {lockedOntoAPlayer}");
        // Check if player is within detection range and if there's line of sight
        if (distanceToPlayer <= detectionRange && angle <= maxAngle)
        {
            if (!Physics.Linecast(gregCannon.position, playerControllerB.transform.position, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore))
            {
                lockedOntoAPlayer = true;
                lastPlayerTargetted = playerControllerB;
                // DetectPlayerAudioSound.volume = Plugin.ModConfig.ConfigACUVolume.Value;
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                targetRotation.z = 0f;
                targetRotation.x = 0f;

                // Rotate the base turret (left/right)
                gregBase.rotation = Quaternion.RotateTowards(gregBase.rotation, targetRotation, rotationSpeed * Time.deltaTime * 5f);

                // Set the pitch angle for the turret cannon
                Vector3 currentLocalEulerAngles = gregCannon.localEulerAngles;
                gregCannon.localEulerAngles = new Vector3(-(maxAngle - angle), currentLocalEulerAngles.y, currentLocalEulerAngles.z);
            }
        }
    }

    private void FireProjectile()
    {
        if (lastPlayerTargetted == null) return;
        // play shoot sound
        if (Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, this.transform.position) <= 50)
        {
            HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
            HUDManager.Instance.ShakeCamera(ScreenShakeType.Long);
        }

        // GregSource.PlayOneShot(GregFireSounds[UnityEngine.Random.Range(0, GregFireSounds.Length)]);
        GunslingerMissile rocket = rockets.Dequeue();
        rocket.Initialize(lastPlayerTargetted, this);

        // Activate rockets via rpc similar to code below
        // AirUnitProjectile projectileComponent = projectile.GetComponent<AirUnitProjectile>();
        // projectileComponent.Initialize(Plugin.ModConfig.ConfigAirControlUnitDamage.Value, currentAngle, lastPlayerTargetted);

        // Rattle the cannon's transform to emulate a shake effect
        StartCoroutine(RattleCannon());
    }

    private IEnumerator RattleCannon()
    {
        Vector3 originalPosition = gregCannon.localPosition;
        float shakeDuration = 0.2f;
        float shakeIntensity = 0.5f;

        while (shakeDuration > 0f)
        {
            gregCannon.localPosition = originalPosition + (Vector3)UnityEngine.Random.insideUnitCircle * shakeIntensity;
            shakeDuration -= Time.deltaTime;
            yield return null;
        }

        gregCannon.localPosition = originalPosition;
        // GregSource.PlayOneShot(GregResupplySounds[UnityEngine.Random.Range(0, GregResupplySounds.Length)]);
    }
}