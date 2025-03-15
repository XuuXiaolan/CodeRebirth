using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.MiscScripts;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class GunslingerGreg : CodeRebirthHazard
{
    public Transform gregBase = null!;
    public Transform gregCannon = null!;
    public Transform[] projectileSpawnPoints = [];
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
    public Dictionary<Transform, bool> rocketsReady = new();
    public Transform[] rocketTransforms = [];

    private List<Bounds> safeBounds = new();
    private float rechargeRocketTimer = 30f;
    private System.Random gregRandom = new();
    private float currentAngle = 0f;
    private float fireTimer = 1f;
    private GameObject projectilePrefab = null!;
    private PlayerControllerB? lastPlayerTargetted = null;

    public override void Start()
    {
        base.Start();
        foreach (var boundsDefiner in FindObjectsOfType<BoundsDefiner>())
        {
            if (boundsDefiner.boundColor == new Color(1, 0, 1, 1))
            {
                Plugin.ExtendedLogging($"Found valid bounds for greg");
                safeBounds.AddItem(boundsDefiner.bounds);
            }
        }
        foreach (var transform in rocketTransforms)
        {
            rocketsReady.Add(transform, true);
            if (IsServer) SpawnImmobileRocket(transform);
        }
        gregRandom = new System.Random(StartOfRound.Instance.randomMapSeed + 69);
        projectilePrefab = MapObjectHandler.Instance.AirControlUnit.ProjectilePrefab;
        lastPlayerTargetted = null;
        DetectPlayerAudioSound.volume = 0f;
        GregTurningSound.volume = 0f;
    }

    private void Update()
    {
        playerHeadstart -= Time.deltaTime;
        if (rocketsReady.ContainsValue(false))
        {
            RechargeRocket(rocketsReady.First(x => x.Value == false).Key);
        }
        if (StartOfRound.Instance.shipIsLeaving || playerHeadstart > 0 || !rocketsReady.ContainsValue(true)) return;
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
        UpdateAudio();
    }

    private void UpdateAudio()
    {
        float volume = Plugin.ModConfig.ConfigACUVolume.Value;
        GregSource.volume = volume;
    }

    private void SpawnImmobileRocket(Transform rocketTransform)
    {
        var rocket = Instantiate(projectilePrefab, rocketTransform.position, Quaternion.identity, rocketTransform);
        NetworkObject networkObject = rocket.GetComponent<NetworkObject>();
        networkObject.Spawn(true);
    }

    private void RechargeRocket(Transform rocketTransform)
    {
        rechargeRocketTimer -= Time.deltaTime;
        if (rechargeRocketTimer > 0) return;
        rocketsReady[rocketTransform] = true;
        SpawnImmobileRocket(rocketTransform);
    }

    private bool IsPlayerNearGround(PlayerControllerB playerControllerB)
    {
        Ray ray = new Ray(playerControllerB.transform.position, -Vector3.up);
        if (Physics.Raycast(ray, out _, 6f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
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
                if (playerControllerB == null || playerControllerB.isPlayerDead || !playerControllerB.isPlayerControlled || StartOfRound.Instance.shipInnerRoomBounds.bounds.Contains(playerControllerB.transform.position) || IsPlayerNearGround(playerControllerB))
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
            GregTurningSound.volume = 0f;
        }
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

        // Check if player is within detection range and if there's line of sight
        if (distanceToPlayer <= detectionRange && angle <= maxAngle)
        {
            if (!Physics.Linecast(gregCannon.position, playerControllerB.transform.position, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Collide))
            {
                lockedOntoAPlayer = true;
                lastPlayerTargetted = playerControllerB;
                DetectPlayerAudioSound.volume = Plugin.ModConfig.ConfigACUVolume.Value;
                currentAngle = angle;
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                targetRotation.z = 0f;
                targetRotation.x = 0f;

                // Rotate the base turret (left/right)
                gregBase.rotation = Quaternion.RotateTowards(gregBase.rotation, targetRotation, rotationSpeed * Time.deltaTime * 5f);

                // Set the pitch angle for the turret cannon
                Vector3 currentLocalEulerAngles = gregCannon.localEulerAngles;
                gregCannon.localEulerAngles = new Vector3(angle, currentLocalEulerAngles.y, currentLocalEulerAngles.z);
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
        GregSource.PlayOneShot(GregFireSounds[UnityEngine.Random.Range(0, GregFireSounds.Length)]);
        if (IsServer)
        {
            Transform[] viableRockets = rocketsReady.Where(kvp => kvp.Value == true).Select(kvp => kvp.Key).ToArray();
            Transform randomRocket = viableRockets[UnityEngine.Random.Range(0, viableRockets.Length)];
            // Activate rockets via rpc similar to code below
            // AirUnitProjectile projectileComponent = projectile.GetComponent<AirUnitProjectile>();
            // projectileComponent.Initialize(Plugin.ModConfig.ConfigAirControlUnitDamage.Value, currentAngle, lastPlayerTargetted);

            // Rattle the cannon's transform to emulate a shake effect
            StartCoroutine(RattleCannon());
        }
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
        GregSource.PlayOneShot(GregResupplySounds[UnityEngine.Random.Range(0, GregResupplySounds.Length)]);
    }
}