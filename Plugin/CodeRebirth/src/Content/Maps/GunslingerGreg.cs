using System.Collections.Generic;
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
    public AudioClip GregLockOnSound = null!;
    public AudioClip[] GregFireSounds = [];
    public AudioClip[] GregResupplySounds = [];
    public AudioSource DetectPlayerAudioSound = null!;
    public float playerHeadstart = 5f;
    public float maxAngle = 90f;
    public Queue<GunslingerMissile> rockets = new();
    public Transform[] rocketTransforms = [];

    private GunslingerMissile? missileToRecharge = null;
    public static List<GregBounds> safeBounds = new();
    private float rechargeRocketTimer = 30f;
    private float fireTimer = 1f;
    private GameObject MissilePrefab = null!;
    private Transform? lastTransformTargetted = null;

    public override void Start()
    {
        base.Start();
        MissilePrefab = MapObjectHandler.Instance.GunslingerGreg!.MissilePrefab;
        foreach (var transform in rocketTransforms)
        {
            rockets.Enqueue(SpawnImmobileRocket(transform));
        }
        lastTransformTargetted = null;
        DetectPlayerAudioSound.volume = 0f;
    }

    private void Update()
    {
        playerHeadstart -= Time.deltaTime;
        if (missileToRecharge == null)
        {
            foreach (var rocket in rockets)
            {
                if (rocket.ready)
                    continue;

                missileToRecharge = rocket;
                break;
            }
        }
        else
        {
            RechargeRocket(missileToRecharge);
        }

        if (Plugin.ModConfig.ConfigDebugMode.Value)
            return;

        if (StartOfRound.Instance.shipIsLeaving || playerHeadstart > 0 || rockets.Count <= 0)
        {
            DetectPlayerAudioSound.volume = 0f;
            return;
        }

        FindAndAimAtTarget();

        if (lastTransformTargetted == null)
            return;

        fireTimer -= Time.deltaTime;
        if (fireTimer <= 0f)
        {
            FireProjectile();
            fireTimer = fireRate;
        }
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
        if (rechargeRocketTimer > 0)
            return;

        rechargeRocketTimer = 30f;
        rocket.ready = true;
        rocket.gameObject.SetActive(true);
        missileToRecharge = null;
    }

    private bool IsTransformNearGround(Transform toKillTransform)
    {
        Ray ray = new(toKillTransform.position, -Vector3.up);
        if (Physics.Raycast(ray, 20f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
        {
            return true;
        }
        return false;
    }

    private void FindAndAimAtTarget()
    {
        bool lockedOntoATransform = false;
        if (lastTransformTargetted != null && !StartOfRound.Instance.shipInnerRoomBounds.bounds.Contains(lastTransformTargetted.position) && !IsTransformNearGround(lastTransformTargetted) && !TransformInSafeBounds(lastTransformTargetted))
        {
            HandleTargettingToTransform(lastTransformTargetted, ref lockedOntoATransform);
        }
        else
        {
            foreach (PlayerControllerB playerControllerB in StartOfRound.Instance.allPlayerScripts)
            {
                if (playerControllerB == null || playerControllerB.isPlayerDead || !playerControllerB.isPlayerControlled || playerControllerB.isInHangarShipRoom || playerControllerB.isClimbingLadder || IsTransformNearGround(playerControllerB.transform) || TransformInSafeBounds(playerControllerB.transform))
                {
                    continue;
                }

                HandleTargettingToTransform(playerControllerB.transform, ref lockedOntoATransform);
            }

            foreach (EnemyAI enemyAI in RoundManager.Instance.SpawnedEnemies)
            {
                if (enemyAI is not RadMechAI radmech)
                    continue;

                if (radmech.isEnemyDead)
                    continue;

                if (radmech.creatureAnimator == null)
                    continue;

                Transform flyThingie = radmech.creatureAnimator.transform.Find("metarig");
                if (TransformInSafeBounds(flyThingie))
                    continue;

                HandleTargettingToTransform(flyThingie, ref lockedOntoATransform);
            }
        }

        if (!lockedOntoATransform)
        {
            lastTransformTargetted = null;
            DetectPlayerAudioSound.volume = 0f;
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

    private void HandleTargettingToTransform(Transform toKilltransform, ref bool lockedOntoATransform)
    {
        float distanceToTransform = Vector3.Distance(gregCannon.position, toKilltransform.position);

        Vector3 futurePosition = toKilltransform.position;

        Vector3 directionToTarget = futurePosition - gregBase.position;
        float angle = Vector3.Angle(gregBase.up, directionToTarget);

        // Plugin.ExtendedLogging($"Angle: {angle} Distance: {distanceToTransform} Locked: {lockedOntoATransform}");
        if (distanceToTransform <= detectionRange && angle <= maxAngle)
        {
            if (!Physics.Linecast(gregCannon.position, toKilltransform.position, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore))
            {
                lockedOntoATransform = true;
                if (lastTransformTargetted == null)
                    GregSource.PlayOneShot(GregLockOnSound);

                lastTransformTargetted = toKilltransform;
                DetectPlayerAudioSound.volume = 1f;
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                targetRotation.z = 0f;
                targetRotation.x = 0f;
                gregBase.rotation = Quaternion.RotateTowards(gregBase.rotation, targetRotation, rotationSpeed * Time.deltaTime * 5f);
                Vector3 currentLocalEulerAngles = gregCannon.localEulerAngles;
                gregCannon.localEulerAngles = new Vector3(-(maxAngle - angle), currentLocalEulerAngles.y, currentLocalEulerAngles.z);
            }
        }
    }

    private void FireProjectile()
    {
        if (lastTransformTargetted == null)
            return;

        // play shoot sound
        if (Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, this.transform.position) <= 50)
        {
            HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
            HUDManager.Instance.ShakeCamera(ScreenShakeType.Long);
        }

        GregSource.PlayOneShot(GregFireSounds[UnityEngine.Random.Range(0, GregFireSounds.Length)]);
        GunslingerMissile rocket = rockets.Dequeue();
        rocket.Initialize(lastTransformTargetted.transform, this);
    }
}