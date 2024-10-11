using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.Util.Extensions;
using GameNetcodeStuff;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.AI;
using static CodeRebirth.src.Content.Unlockables.ShockwaveFaceController;

namespace CodeRebirth.src.Content.Unlockables;
public class ShockwaveGalAI : NetworkBehaviour, INoiseListener, IHittable
{
    public ShockwaveFaceController RobotFaceController = null!;
    public SkinnedMeshRenderer FaceSkinnedMeshRenderer = null!;
    public Renderer FaceRenderer = null!;
    public Animator Animator = null!;
    public NetworkAnimator NetworkAnimator = null!;
    public NavMeshAgent Agent = null!;
    public InteractTrigger HeadPatTrigger = null!;
    public List<InteractTrigger> GiveItemTrigger = new();
    public List<Transform> itemsHeldTransforms = new();
    public AnimationClip CatPoseAnim = null!;
    [NonSerialized] public Emotion galEmotion = Emotion.ClosedEye;
    [NonSerialized] public ShockwaveCharger ShockwaveCharger = null!;
    public Collider[] colliders = [];
    public Transform LaserOrigin = null!;
    public AudioSource FlySource = null!;
    public AudioSource GalVoice = null!;
    public AudioSource GalSFX = null!;
    public AudioClip ActivateSound = null!;
    public AudioClip GreetOwnerSound = null!;
    public AudioClip[] IdleSounds = null!;
    public AudioClip DeactivateSound = null!;
    public AudioClip[] HitSounds = null!;
    public AudioClip PatSound = null!;
    public AudioClip[] FootstepSounds = [];
    public AudioClip[] TakeDropItemSounds = [];
    public float doorOpeningSpeed = 1f;

    private bool boomboxPlaying = false;
    private List<GrabbableObject> itemsHeldList = new();
    private EnemyAI? targetEnemy;
    private bool flying = false;
    private bool isInside = false;
    private readonly int maxItemsToHold = 4;
    private State galState = State.Inactive;
    [NonSerialized] public PlayerControllerB? ownerPlayer;
    private List<string> enemyTargetBlacklist = new();
    [NonSerialized] public int chargeCount = 10;
    private int maxChargeCount;
    private bool currentlyAttacking = false;
    private float boomboxTimer = 0f;
    private bool physicsEnabled = true;
    private bool catPosing = false;
    private float idleNeededTimer = 10f;
    private float idleTimer = 0f;
    private bool backFlipping = false;
    private Vector3 pointToGo = Vector3.zero;
    [NonSerialized] public Vector3 positionOfPlayerBeforeTeleport = Vector3.zero;
    private EntranceTeleport lastUsedEntranceTeleport = null!;
    private Dictionary<EntranceTeleport, Transform[]> exitPoints = new();
    private System.Random galRandom = new();
    private readonly static int backFlipAnimation = Animator.StringToHash("startFlip");
    private readonly static int catAnimation = Animator.StringToHash("startCat");
    private readonly static int holdingItemAnimation = Animator.StringToHash("holdingItem"); // todo: figure out why this doesnt work
    private readonly static int attackModeAnimation = Animator.StringToHash("attackMode");
    private readonly static int danceAnimation = Animator.StringToHash("dancing");
    private readonly static int activatedAnimation = Animator.StringToHash("activated");
    private readonly static int pettingAnimation = Animator.StringToHash("startPet");
    private readonly static int runSpeedFloat = Animator.StringToHash("RunSpeed");

    public enum State
    {
        Inactive = 0,
        Active = 1,
        FollowingPlayer = 2,
        DeliveringItems = 3,
        Dancing = 4,
        AttackMode = 5,
    }

    public enum Emotion
    {
        Heart = 0,
        ClosedEye = 1,
        OpenEye = 2,
        Happy = 3
    }

    public void Start()
    {
        Plugin.Logger.LogInfo("Hi creator");
        galRandom = new System.Random(StartOfRound.Instance.randomMapSeed + 69);
        chargeCount = Plugin.ModConfig.ConfigShockwaveCharges.Value;
        maxChargeCount = chargeCount;
        Agent.enabled = false;
        FlySource.Pause();
        if (IsServer) this.transform.SetParent(ShockwaveCharger.transform, true);
        List<string> enemyBlacklist = Plugin.ModConfig.ConfigShockwaveBotEnemyBlacklist.Value.Split(',').ToList();
        foreach (string enemy in enemyBlacklist)
        {
            enemyTargetBlacklist.Add(enemy.Trim());
        }
        StartCoroutine(StartUpDelay());
    }


    [ServerRpc(RequireOwnership = false)]
    public void RefillChargesServerRpc()
    {
        RefillChargesClientRpc();
    }

    [ClientRpc]
    private void RefillChargesClientRpc()
    {
        chargeCount = maxChargeCount;
    }

    private IEnumerator StartUpDelay()
    {
        yield return new WaitForSeconds(0.5f);
        this.transform.position = ShockwaveCharger.ChargeTransform.position;
        this.transform.rotation = ShockwaveCharger.ChargeTransform.rotation;
        HeadPatTrigger.onInteract.AddListener(OnHeadInteract);
        foreach (InteractTrigger trigger in GiveItemTrigger)
        {
            trigger.onInteract.AddListener(GrabItemInteract);
        }
        StartCoroutine(CheckForNearbyEnemiesToOwner());
    }

    public void ActivateShockwaveGal(PlayerControllerB owner)
    {
        ownerPlayer = owner;
        GalVoice.PlayOneShot(ActivateSound);
        positionOfPlayerBeforeTeleport = owner.transform.position;
        var exits = FindObjectsByType<EntranceTeleport>(FindObjectsSortMode.InstanceID);
        foreach (var exit in exits)
        {
            // todo: interior and exterior exits have their entrance and exit points mismatched, deal with this!!
            exitPoints.Add(exit, [exit.entrancePoint, exit.exitPoint]);
            if (exit.isEntranceToBuilding)
            {
                lastUsedEntranceTeleport = exit;
            }
            if (!exit.FindExitPoint())
            {
                Plugin.Logger.LogError("Something went wrong in the generation of the fire exits");
            }
        }
        if (IsServer)
        {
            Agent.Warp(ShockwaveCharger.ChargeTransform.position);
            this.transform.rotation = ShockwaveCharger.ChargeTransform.rotation;
            HandleStateAnimationSpeedChanges(State.Active, Emotion.OpenEye);
        }
    }

    public void DeactivateShockwaveGal()
    {
        ownerPlayer = null;
        GalVoice.PlayOneShot(DeactivateSound);
        exitPoints.Clear();
        if (IsServer)
        {
            Agent.Warp(ShockwaveCharger.ChargeTransform.position);
            this.transform.rotation = ShockwaveCharger.ChargeTransform.rotation;
            HandleStateAnimationSpeedChanges(State.Inactive, Emotion.ClosedEye);
        }
    }

    private void OnHeadInteract(PlayerControllerB playerInteracting)
    {
        if (playerInteracting != GameNetworkManager.Instance.localPlayerController || playerInteracting != ownerPlayer) return;
        if (UnityEngine.Random.Range(0f, 1f) < 0.5f) StartPetAnimationServerRpc();
        else if (!catPosing) StartCatPoseAnimationServerRpc();
        else StartPetAnimationServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartPetAnimationServerRpc()
    {
        NetworkAnimator.SetTrigger(pettingAnimation);
        PlayPatSoundClientRpc();
        EnablePhysicsClientRpc(!physicsEnabled);
    }

    [ClientRpc]
    private void PlayPatSoundClientRpc()
    {
        GalVoice.PlayOneShot(PatSound);
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartCatPoseAnimationServerRpc()
    {
        NetworkAnimator.SetTrigger(catAnimation);
        StartCoroutine(ResetSpeedBackToNormal());
    }

    private IEnumerator ResetSpeedBackToNormal()
    {
        catPosing = true;
        yield return new WaitForSeconds(CatPoseAnim.length);
        catPosing = false;
    }

    public void Update()
    {
        // Agent.enabled = galState != State.Inactive;
        if (galState == State.Inactive) return;
        if (ownerPlayer != null && ownerPlayer.isPlayerDead) ownerPlayer = null;
        HeadPatTrigger.enabled = galState != State.AttackMode && galState != State.Inactive;
        foreach (InteractTrigger trigger in GiveItemTrigger)
        {
            trigger.enabled = galState != State.AttackMode && galState != State.Inactive;
        }
        if (boomboxPlaying)
        {
            boomboxTimer += Time.deltaTime;
            if (boomboxTimer >= 2f)
            {
                boomboxTimer = 0f;
                boomboxPlaying = false;
            }
        }
        if (galState == State.AttackMode)
        {
            Agent.stoppingDistance = 7f;
        }
        else
        {
            Agent.stoppingDistance = 3f;
        }
        idleTimer += Time.deltaTime;
        if (idleTimer > idleNeededTimer)
        {
            idleTimer = 0f;
            idleNeededTimer = galRandom.NextFloat(5f, 10f);
            GalSFX.PlayOneShot(IdleSounds[galRandom.NextInt(0, IdleSounds.Length - 1)]);
        }
        if (!IsHost) return;
        HostSideUpdate();
    }

    private void HostSideUpdate()
    {
        if ((StartOfRound.Instance.shipIsLeaving || !StartOfRound.Instance.shipHasLanded || StartOfRound.Instance.inShipPhase) && Agent.enabled)
        {
            ShockwaveCharger.ActivateGirlServerRpc(-1);
            return;
        }
        if (Agent.enabled) AdjustSpeedOnDistanceOnTargetPosition();
        Animator.SetFloat(runSpeedFloat, Agent.velocity.magnitude / 3);
        switch (galState)
        {
            case State.Inactive:
                break;
            case State.Active:
                DoActive();
                break;
            case State.FollowingPlayer:
                DoFollowingPlayer();
                break;
            case State.DeliveringItems:
                DoDeliveringItems();
                break;
            case State.Dancing:
                break;
            case State.AttackMode:
                DoAttackMode();
                break;
        }
    }

    private bool GoToChargerAndDeactivate()
    {
        if (isInside)
        {
            GoThroughEntrance(false);
        }
        else
        {
            Agent.SetDestination(ShockwaveCharger.ChargeTransform.position);
        }
        if (Vector3.Distance(this.transform.position, ShockwaveCharger.ChargeTransform.position) <= Agent.stoppingDistance)
        {
            if (!Agent.hasPath || Agent.velocity.sqrMagnitude <= 0.01f)
            {
                ShockwaveCharger.ActivateGirlServerRpc(-1);
                return true;
            }
        }
        return false;
    }

    private void DoActive()
    {
        if (ownerPlayer == null)
        {
            GoToChargerAndDeactivate();
            return;
        }
        if (Vector3.Distance(this.transform.position, ownerPlayer.transform.position) > 3f)
        {
            HandleStateAnimationSpeedChanges(State.FollowingPlayer, Emotion.OpenEye);
        }
    }

    private void DoFollowingPlayer()
    {
        if (ownerPlayer == null)
        {
            GoToChargerAndDeactivate();
            return;
        }

        if (!Agent.enabled)
        {
            Vector3 targetPosition = pointToGo;
            float moveSpeed = 6f;  // Increased speed for a faster approach
            float arcHeight = 10f;  // Adjusted arc height for a more pronounced arc
            float distanceToTarget = Vector3.Distance(transform.position, targetPosition);

            // Calculate the new position in an arcing motion
            float normalizedDistance = Mathf.Clamp01(Vector3.Distance(transform.position, targetPosition) / distanceToTarget);
            Vector3 newPosition = Vector3.MoveTowards(transform.position, targetPosition, Time.deltaTime * moveSpeed);
            newPosition.y += Mathf.Sin(normalizedDistance * Mathf.PI) * arcHeight;

            transform.position = newPosition;
            transform.rotation = Quaternion.LookRotation(targetPosition - transform.position);
            if (Vector3.Distance(transform.position, targetPosition) <= 1f)
            {
                Animator.SetBool(attackModeAnimation, false);
                Agent.enabled = true;
            }
            return;
        }

        if ((!isInside && ownerPlayer.isInsideFactory) || (isInside && !ownerPlayer.isInsideFactory))
        {
            GoThroughEntrance(true);
            return;
        }

        NavMeshPath path = new NavMeshPath();
        if (!Agent.CalculatePath(ownerPlayer.transform.position, path) || path.status == NavMeshPathStatus.PathPartial)
        {
            if (Vector3.Distance(transform.position, ownerPlayer.transform.position) > 7f)
            {
                Agent.SetDestination(Agent.pathEndPosition);
                if (Vector3.Distance(Agent.transform.position, Agent.pathEndPosition) <= Agent.stoppingDistance)
                {
                    Agent.SetDestination(ownerPlayer.transform.position);
                    if (!Agent.CalculatePath(ownerPlayer.transform.position, path) || path.status != NavMeshPathStatus.PathComplete)
                    {
                        Vector3 nearbyPoint;
                        if (NavMesh.SamplePosition(ownerPlayer.transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                        {
                            nearbyPoint = hit.position;
                            pointToGo = nearbyPoint;
                            Animator.SetBool(attackModeAnimation, true);
                            Agent.enabled = false;
                        }
                        return;
                    }
                }
            }
        }

        if (itemsHeldList.Count >= maxItemsToHold)
        {
            HandleStateAnimationSpeedChangesServerRpc((int)State.DeliveringItems, (int)Emotion.OpenEye);
            return;
        }

        if (boomboxPlaying)
        {
            HandleStateAnimationSpeedChanges(State.Dancing, Emotion.Happy);
            StartCoroutine(StopDancingDelay());
            return;
        }
        Agent.SetDestination(ownerPlayer.transform.position);
        if (!backFlipping && UnityEngine.Random.Range(0f, 25000f) <= 1f && Agent.velocity.sqrMagnitude <= 0.01f && Vector3.Distance(Agent.transform.position, ownerPlayer.transform.position) <= 5f)
        {
            DoBackFliplol();
        }
    }

    private void DoDeliveringItems()
    {
        if (itemsHeldList.Count == 0)
        {
            if (ownerPlayer != null)
            {
                HandleStateAnimationSpeedChangesServerRpc((int)State.FollowingPlayer, (int)Emotion.OpenEye);
            }
            else
            {
                ShockwaveCharger.ActivateGirlServerRpc(-1);
            }
        }

        if (!isInside)
        {
            Agent.SetDestination(ShockwaveCharger.ChargeTransform.position);
            if (Vector3.Distance(this.transform.position, ShockwaveCharger.ChargeTransform.position) <= Agent.stoppingDistance)
            {
                if (!Agent.hasPath || Agent.velocity.sqrMagnitude == 0f)
                {
                    int heldItemCount = itemsHeldList.Count;
                    Plugin.ExtendedLogging($"Items held: {heldItemCount}");
                    for (int i = heldItemCount - 1; i >= 0; i--)
                    {
                        HandleDroppingItemServerRpc(i);
                    }
                }
            }
        }
        else
        {
            GoThroughEntrance(false);
        }
    }

    private void DoAttackMode()
    {
        if (targetEnemy == null || targetEnemy.isEnemyDead || chargeCount <= 0 || ownerPlayer == null)
        {
            if (targetEnemy != null && targetEnemy.isEnemyDead) SetEnemyTargetServerRpc(-1);
            if (ownerPlayer != null)
            {
                HandleStateAnimationSpeedChanges(State.FollowingPlayer, Emotion.OpenEye);
            }
            else
            {
                GoToChargerAndDeactivate();
            }
            return;
        }

        if (!currentlyAttacking)
        {
            if (isInside && targetEnemy.isOutside || !isInside && !targetEnemy.isOutside)
            {
                GoThroughEntrance(true);
                return;
            }
            Agent.SetDestination(targetEnemy.transform.position);
        }
        if (Vector3.Distance(this.transform.position, targetEnemy.transform.position) <= Agent.stoppingDistance || currentlyAttacking)
        {
            Vector3 targetPosition = targetEnemy.transform.position;
            Vector3 direction = (targetPosition - this.transform.position).normalized;
            direction.y = 0; // Keep the y component zero to prevent vertical rotation

            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                this.transform.rotation = Quaternion.Slerp(this.transform.rotation, lookRotation, Time.deltaTime * 5f);
            }
            if (!currentlyAttacking)
            {
                currentlyAttacking = true;
                NetworkAnimator.SetTrigger(attackModeAnimation);
            }
        }
    }

    private void DoBackFliplol()
    {
        NetworkAnimator.SetTrigger(backFlipAnimation);
    }

    private IEnumerator StopDancingDelay()
    {
        yield return new WaitUntil(() => !boomboxPlaying);  
        HandleStateAnimationSpeedChanges(State.FollowingPlayer, Emotion.OpenEye);
    }

    [ClientRpc]
    private void EnablePhysicsClientRpc(bool enablePhysics)
    {
        EnablePhysics(enablePhysics);
    }

    private void EnablePhysics(bool enablePhysics)
    {
        foreach (Collider collider in colliders)
        {
            collider.enabled = enablePhysics;
        }
        physicsEnabled = enablePhysics;
    }

    private IEnumerator CheckForNearbyEnemiesToOwner()
    {
        if (!IsServer) yield break;

        var delay = new WaitForSeconds(1f);
        while (true)
        {
            yield return delay;

            if (galState != State.FollowingPlayer || ownerPlayer == null) continue;

            // Use OverlapSphereNonAlloc to reduce garbage collection
            Collider[] hitColliders = new Collider[20];  // Size accordingly to expected max enemies
            int numHits = Physics.OverlapSphereNonAlloc(ownerPlayer.gameplayCamera.transform.position, 25f, hitColliders, LayerMask.GetMask("Enemies"), QueryTriggerInteraction.Collide);

            for (int i = 0; i < numHits; i++)
            {
                Collider collider = hitColliders[i];
                if (!collider.TryGetComponent(out EnemyAI enemy))
                {
                    if (collider.GetComponent<NetworkObject>() == null)
                    {
                        NetworkObject networkObject = collider.GetComponentInParent<NetworkObject>();
                        if (networkObject == null || !networkObject.TryGetComponent(out EnemyAI enemy2))
                            continue;
                        
                        enemy = enemy2;
                    }
                }

                if (enemy == null || enemy.isEnemyDead || !enemy.enemyType.canDie || enemyTargetBlacklist.Contains(enemy.enemyType.enemyName))
                    continue;

                // First, do a simple direction check to see if the enemy is in front of the player
                Vector3 directionToEnemy = (collider.transform.position - ownerPlayer.gameplayCamera.transform.position).normalized;
                // Then check if there's a clear line of sight
                if (!Physics.Raycast(ownerPlayer.gameplayCamera.transform.position, directionToEnemy, out RaycastHit hit, 25f, StartOfRound.Instance.collidersAndRoomMask | LayerMask.GetMask("Enemies"), QueryTriggerInteraction.Collide))
                    continue;

                // Make sure the hit belongs to the same GameObject as the enemy
                if (hit.collider.gameObject != enemy.gameObject && !hit.collider.transform.IsChildOf(enemy.transform))
                    continue;

                SetEnemyTargetServerRpc(RoundManager.Instance.SpawnedEnemies.IndexOf(enemy));
                HandleStateAnimationSpeedChanges(State.AttackMode, Emotion.OpenEye);
                break;  // Exit loop after targeting one enemy, depending on game logic
            }
        }
    }

    private void CheckIfEnemyIsHitAnimEvent()
    {
        if (targetEnemy == null || targetEnemy.isEnemyDead) return;

        // Instantiate the laser beam on the server
        if (IsServer)
        {
            GameObject laserPrefab = UnlockableHandler.Instance.ShockwaveBot.LasetShockBlast;
            GameObject laserInstance = Instantiate(laserPrefab, LaserOrigin.position, LaserOrigin.rotation);
            LaserShockBlast laserShockBlack = laserInstance.GetComponent<LaserShockBlast>();
            
            // Set the origin of the laser
            laserShockBlack.laserOrigin = this.LaserOrigin;

            // Spawn the laser over the network
            laserShockBlack.NetworkObject.Spawn();
        }
    }

    private void EndAttackAnimEvent()
    {
        currentlyAttacking = false;
        chargeCount--;
    }

    private void PlayFootstepSoundAnimEvent()
    {
        GalSFX.PlayOneShot(FootstepSounds[galRandom.NextInt(0, FootstepSounds.Length - 1)]);
    }

    private void StartFlyingAnimEvent()
    {
        flying = true;
        backFlipping = true;
        FlySource.UnPause();
    }

    private void StopFlyingAnimEvent()
    {
        flying = false;
        backFlipping = false;
        FlySource.Pause();
    }

    private void AdjustSpeedOnDistanceOnTargetPosition()
    {
        float distanceToDestination = Agent.remainingDistance;

        // Define min and max distance thresholds
        float minDistance = 0f;
        float maxDistance = 40f;

        // Define min and max speed values
        float minSpeed = 0f; // Speed when closest
        float maxSpeed = (galState == State.FollowingPlayer || galState == State.AttackMode) ? 20f : 10f; // Speed when farthest

        // Clamp the distance within the range to avoid negative values or distances greater than maxDistance
        float clampedDistance = Mathf.Clamp(distanceToDestination, minDistance, maxDistance);

        // Normalize the distance to a 0-1 range (minDistance to maxDistance maps to 0 to 1)
        float normalizedDistance = (clampedDistance - minDistance) / (maxDistance - minDistance);

        // Linearly interpolate the speed between minSpeed and maxSpeed based on normalized distance
        float currentSpeed = Mathf.Lerp(minSpeed, maxSpeed, normalizedDistance);
        if (!catPosing && !backFlipping) Agent.speed = currentSpeed;
        else Agent.speed = 0;
        // Apply the calculated speed (you would replace this with your actual movement logic)
    }

    [ServerRpc(RequireOwnership = false)]
    private void HandleStateAnimationSpeedChangesServerRpc(int state, int emotion)
    {
        HandleStateAnimationSpeedChanges((State)state, (Emotion)emotion);
    }

    private void HandleStateAnimationSpeedChanges(State state, Emotion emotion) // This is for host
    {
        SwitchStateOrEmotionClientRpc((int)state, (int)emotion);
        switch (state)
        {
            case State.Inactive:
                Animator.SetBool(holdingItemAnimation, false);
                Animator.SetBool(attackModeAnimation, false);
                Animator.SetBool(danceAnimation, false);
                Animator.SetBool(activatedAnimation, false);
                break;
            case State.Active:
                Animator.SetBool(holdingItemAnimation, false);
                Animator.SetBool(attackModeAnimation, false);
                Animator.SetBool(danceAnimation, false);
                Animator.SetBool(activatedAnimation, true);
                break;
            case State.FollowingPlayer:
                Animator.SetBool(holdingItemAnimation, false);
                Animator.SetBool(attackModeAnimation, false);
                Animator.SetBool(danceAnimation, false);
                Animator.SetBool(activatedAnimation, true);
                break;
            case State.DeliveringItems:
                Animator.SetBool(holdingItemAnimation, true);
                Animator.SetBool(attackModeAnimation, false);
                Animator.SetBool(danceAnimation, false);
                Animator.SetBool(activatedAnimation, true);
                break;
            case State.Dancing:
                Animator.SetBool(holdingItemAnimation, false);
                Animator.SetBool(attackModeAnimation, false);
                Animator.SetBool(danceAnimation, true);
                Animator.SetBool(activatedAnimation, true);
                break;
            case State.AttackMode:
                Animator.SetBool(holdingItemAnimation, false);
                Animator.SetBool(attackModeAnimation, true);
                Animator.SetBool(danceAnimation, false);
                Animator.SetBool(activatedAnimation, true);
                break;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SwitchStateOrEmotionServerRpc(int state, int emotion)
    {
        SwitchStateOrEmotionClientRpc(state, emotion);
    }

    [ClientRpc]
    private void SwitchStateOrEmotionClientRpc(int state, int emotion)
    {
        SwitchStateOrEmotion(state, emotion);
    }

    private void SwitchStateOrEmotion(int state, int emotion) // this is for everyone.
    {
        State stateToSwitchTo = (State)state;
        Emotion emotionToSwitchTo = (Emotion)emotion;
        if (state != -1)
        {
            switch (stateToSwitchTo)
            {
                case State.Inactive:
                    HandleStateInactiveChange();
                    break;
                case State.Active:
                    HandleStateActiveChange();
                    break;
                case State.FollowingPlayer:
                    HandleStateFollowingPlayerChange();
                    break;
                case State.DeliveringItems:
                    HandleStateDeliveringItemsChange();
                    break;
                case State.Dancing:
                    HandleStateDancingChange();
                    break;
                case State.AttackMode:
                    HandleStateAttackModeChange();
                    break;
            }
        }

        if (emotion != -1)
        {
            switch (emotionToSwitchTo)
            {
                case Emotion.ClosedEye:
                    HandleEmotionInactiveChange();
                    break;
                case Emotion.Happy:
                    HandleEmotionHappyChange();
                    break;
                case Emotion.Heart:
                    HandleEmotionLoveyDoveyChange();
                    break;
                case Emotion.OpenEye:
                    HandleEmotionNormalChange();
                    break;
            }
        }
    }

    #region State Changes
    private void HandleStateInactiveChange()
    {
        int heldItemCount = itemsHeldList.Count;
        for (int i = heldItemCount - 1; i >= 0; i--)
        {
            HandleDroppingItem(itemsHeldList[i]);
        }

        ownerPlayer = null;
        RobotFaceController.SetMode(RobotMode.Normal);
        Agent.enabled = false;
        galState = State.Inactive;
    }

    private void HandleStateActiveChange()
    {
        RobotFaceController.SetMode(RobotMode.Normal);
        RobotFaceController.SetFaceState(Emotion.OpenEye, 100);
        Agent.enabled = true;
        galState = State.Active;
    }

    private void HandleStateFollowingPlayerChange()
    {
        GalVoice.PlayOneShot(GreetOwnerSound);
        RobotFaceController.SetMode(RobotMode.Normal);
        galState = State.FollowingPlayer;
    }

    private void HandleStateDeliveringItemsChange()
    {
        RobotFaceController.SetMode(RobotMode.Normal);
        galState = State.DeliveringItems;
    }

    private void HandleStateDancingChange()
    {
        RobotFaceController.SetMode(RobotMode.Normal);
        galState = State.Dancing;
    }

    private void HandleStateAttackModeChange()
    {
        galState = State.AttackMode;
        RobotFaceController.SetMode(RobotMode.Combat);
    }
    #endregion

    #region Emotion Changes
    private void HandleEmotionInactiveChange()
    {
        RobotFaceController.SetFaceState(Emotion.ClosedEye, 100);
    }

    private void HandleEmotionHappyChange()
    {
        RobotFaceController.SetFaceState(Emotion.Happy, 100);
    }

    private void HandleEmotionLoveyDoveyChange()
    {
        RobotFaceController.SetFaceState(Emotion.Heart, 100);
    }

    private void HandleEmotionNormalChange()
    {
        RobotFaceController.SetFaceState(Emotion.OpenEye, 100);
    }
    #endregion

    private void GrabItemInteract(PlayerControllerB player)
    {
        if (player != ownerPlayer || player.currentlyHeldObjectServer == null || itemsHeldList.Count >= maxItemsToHold) return;
        GrabItemOwnerHoldingServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player));
    }

    [ServerRpc(RequireOwnership = false)]
    private void GrabItemOwnerHoldingServerRpc(int indexOfOwner)
    {
        GrabItemOwnerHoldingClientRpc(indexOfOwner);
    }

    [ClientRpc]
    private void GrabItemOwnerHoldingClientRpc(int indexOfOwner)
    {
        PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[indexOfOwner];
        StartCoroutine(HandleGrabbingItem(player.currentlyHeldObjectServer, itemsHeldTransforms[itemsHeldList.Count]));
    }

    private IEnumerator HandleGrabbingItem(GrabbableObject item, Transform heldTransform)
    {
        item.isInElevator = false;
        item.isInShipRoom = false;
        item.playerHeldBy.DiscardHeldObject();
        yield return new WaitForSeconds(0.1f);
        item.grabbable = false;
        item.isHeldByEnemy = true;
        item.hasHitGround = false;
        item.parentObject = heldTransform;
        item.EnablePhysics(false);
        itemsHeldList.Add(item);
        GalVoice.PlayOneShot(TakeDropItemSounds[galRandom.NextInt(0, TakeDropItemSounds.Length - 1)]);
        HoarderBugAI.grabbableObjectsInMap.Remove(item.gameObject);
    }

    [ServerRpc(RequireOwnership = false)]
    private void HandleDroppingItemServerRpc(int itemIndex)
    {
        HandleDroppingItemClientRpc(itemIndex);
    }

    [ClientRpc]
    private void HandleDroppingItemClientRpc(int itemIndex)
    {
        HandleDroppingItem(itemsHeldList[itemIndex]);
    }

    private void HandleDroppingItem(GrabbableObject? item)
    {
        if (item == null)
        {
            Plugin.Logger.LogError("Item was null in HandleDroppingItem");
            return;
        }
        item.parentObject = null;
        if (IsServer) item.transform.SetParent(StartOfRound.Instance.propsContainer, true);
        item.isInShipRoom = true;
        item.isInElevator = true;
        item.EnablePhysics(true);
        item.fallTime = 0f;
        item.startFallingPosition = item.transform.parent.InverseTransformPoint(item.transform.position);
        item.targetFloorPosition = item.transform.parent.InverseTransformPoint(item.GetItemFloorPosition(default(Vector3)));
        item.floorYRot = -1;
        item.DiscardItemFromEnemy();
        item.grabbable = true;
        item.isHeldByEnemy = false;
        item.transform.rotation = Quaternion.Euler(item.itemProperties.restingRotation);
        itemsHeldList.Remove(item);
        GalVoice.PlayOneShot(TakeDropItemSounds[galRandom.NextInt(0, TakeDropItemSounds.Length - 1)]);
        if (itemsHeldList.Count == 0 && IsServer)
        {
            Animator.SetBool(holdingItemAnimation, false);
        }
    }

    public void GoThroughEntrance(bool followingPlayer)
    {
        Vector3 destination = Vector3.zero;
        Vector3 destinationAfterTeleport = Vector3.zero;
        EntranceTeleport entranceTeleportToUse = null!;
        if (followingPlayer)
        {
            EntranceTeleport? closestExitPoint = null;
            foreach (var exitpoint in exitPoints.Keys)
            {
                if (closestExitPoint == null || Vector3.Distance(positionOfPlayerBeforeTeleport, exitpoint.transform.position) < Vector3.Distance(positionOfPlayerBeforeTeleport, closestExitPoint.transform.position))
                {
                    closestExitPoint = exitpoint;
                }
            }
            if (closestExitPoint != null)
            {
                entranceTeleportToUse = closestExitPoint;
                destination = closestExitPoint.entrancePoint.transform.position;
                destinationAfterTeleport = closestExitPoint.exitPoint.transform.position;
            }
        }
        else
        {
            entranceTeleportToUse = lastUsedEntranceTeleport;
            destination = isInside ? lastUsedEntranceTeleport.exitPoint.transform.position : lastUsedEntranceTeleport.entrancePoint.transform.position;
            destinationAfterTeleport = isInside ? lastUsedEntranceTeleport.entrancePoint.transform.position : lastUsedEntranceTeleport.exitPoint.transform.position;
        }

        Plugin.ExtendedLogging($"Going through entrance: {destination}");
        Agent.SetDestination(destination);
        if (Vector3.Distance(transform.position, destination) <= 3f)
        {
            Plugin.ExtendedLogging($"Warping through entrance: {destinationAfterTeleport}");
            lastUsedEntranceTeleport = entranceTeleportToUse;
            Agent.Warp(destinationAfterTeleport);
            SetShockwaveGalOutsideOrInsideServerRpc();
        }
    }

	public void DetectNoise(Vector3 noisePosition, float noiseLoudness, int timesPlayedInOneSpot = 0, int noiseID = 0)
	{
        if (galState == State.Inactive) return;
		if (noiseID == 5 && !Physics.Linecast(base.transform.position, noisePosition, StartOfRound.Instance.collidersAndRoomMask))
		{
            boomboxTimer = 0f;
			boomboxPlaying = true;
		}
	}

    [ServerRpc(RequireOwnership = false)]
    private void SetShockwaveGalOutsideOrInsideServerRpc()
    {
        SetShockwaveGalOutsideOrInsideClientRpc();
    }

    [ClientRpc]
    public void SetShockwaveGalOutsideOrInsideClientRpc()
    {
        for (int i = 0; i < itemsHeldList.Count; i++)
        {
            itemsHeldList[i].isInFactory = !isInside;
            itemsHeldList[i].transform.position = itemsHeldTransforms[i].position;
            StartCoroutine(SetItemPhysics(itemsHeldList[i]));
        }
        isInside = !isInside;
    }

    private IEnumerator SetItemPhysics(GrabbableObject grabbableObject)
    {
        yield return new WaitForSeconds(0.1f);
        grabbableObject.EnablePhysics(false);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetEnemyTargetServerRpc(int enemyID)
    {
        SetEnemyTargetClientRpc(enemyID);
    }

    [ClientRpc]
    public void SetEnemyTargetClientRpc(int enemyID)
    {
        if (enemyID == -1)
        {
            targetEnemy = null;
            Plugin.ExtendedLogging($"Clearing Enemy target on {this}");
            return;
        }
        if (RoundManager.Instance.SpawnedEnemies[enemyID] == null)
        {
            Plugin.ExtendedLogging($"Enemy Index invalid! {this}");
            return;
        }
        targetEnemy = RoundManager.Instance.SpawnedEnemies[enemyID];
        Plugin.ExtendedLogging($"{this} setting target to: {targetEnemy.enemyType.enemyName}");
    }

    public bool Hit(int force, Vector3 hitDirection, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        if (galState == State.Inactive) return false;
        GalVoice.PlayOneShot(HitSounds[galRandom.NextInt(0, HitSounds.Length - 1)]);
        return true;
    }
}