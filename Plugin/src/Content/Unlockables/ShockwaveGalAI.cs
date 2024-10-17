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
    public InteractTrigger ChestTrigger = null!;
    public List<InteractTrigger> GiveItemTrigger = new();
    public List<Transform> itemsHeldTransforms = new();
    public AnimationClip CatPoseAnim = null!;
    [NonSerialized] public Emotion galEmotion = Emotion.ClosedEye;
    public ShockwaveCharger ShockwaveCharger = null!;
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
    public float DoorOpeningSpeed = 1f;
    public Transform DroneHead = null!;

    private bool isSellingItems = false;
    private bool boomboxPlaying = false;
    private float staringTimer = 0f;
    private const float stareThreshold = 2f; // Set the threshold to 2 seconds, or adjust as needed
    private List<GrabbableObject> itemsHeldList = new();
    private List<GrabbableObject> itemsToSell = new();
    private EnemyAI? targetEnemy;
    private bool flying = false;
    private bool isInside = false;
    private int maxItemsToHold = 4;
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
    private MineshaftElevatorController? elevatorScript = null;
    private DepositItemsDesk? depositItemsDesk = null;
    private bool onCompanyMoon = false;
    private bool usingElevator = false;
    private readonly static int backFlipAnimation = Animator.StringToHash("startFlip");
    private readonly static int catAnimation = Animator.StringToHash("startCat");
    private readonly static int holdingItemAnimation = Animator.StringToHash("holdingItem");
    private readonly static int attackModeAnimation = Animator.StringToHash("attackMode");
    private readonly static int startAttackAnimation = Animator.StringToHash("startAttack");
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
        SellingItems = 6,
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
        foreach (string enemy in Plugin.ModConfig.ConfigShockwaveBotEnemyBlacklist.Value.Split(','))
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
        transform.position = ShockwaveCharger.ChargeTransform.position;
        transform.rotation = ShockwaveCharger.ChargeTransform.rotation;
        HeadPatTrigger.onInteract.AddListener(OnHeadInteract);
        ChestTrigger.onInteract.AddListener(OnChestInteract);
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
        exitPoints = new();
        foreach (var exit in FindObjectsByType<EntranceTeleport>(FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID))
        {
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
        int activePlayerCount = StartOfRound.Instance.allPlayerScripts.Where(x => x.isPlayerControlled).Count();
        if (activePlayerCount == 1 || Plugin.ModConfig.ConfigShockwaveHoldsFourItems.Value)
        {
            maxItemsToHold = 4;
        }
        else
        {
            maxItemsToHold = 2;
        }
        elevatorScript = FindObjectOfType<MineshaftElevatorController>();
        depositItemsDesk = FindObjectOfType<DepositItemsDesk>();
        onCompanyMoon = RoundManager.Instance.currentLevel.levelID == 3;
        ResetToChargerStation(State.Active, Emotion.OpenEye);
    }

    private void ResetToChargerStation(State state, Emotion emotion)
    {
        if (!IsServer) return;
        if (Agent.enabled) Agent.Warp(ShockwaveCharger.ChargeTransform.position);
        else transform.position = ShockwaveCharger.ChargeTransform.position;
        transform.rotation = ShockwaveCharger.ChargeTransform.rotation;
        HandleStateAnimationSpeedChanges(state, emotion);
    }

    public void DeactivateShockwaveGal()
    {
        ownerPlayer = null;
        GalVoice.PlayOneShot(DeactivateSound);
        elevatorScript = null;
        depositItemsDesk = null;
        onCompanyMoon = false;
        ResetToChargerStation(State.Inactive, Emotion.ClosedEye);
    }

    private void OnChestInteract(PlayerControllerB playerInteracting)
    {
        if (playerInteracting != GameNetworkManager.Instance.localPlayerController || playerInteracting != ownerPlayer) return;
        DropAllHeldItemsServerRpc();
    }

    private void OnHeadInteract(PlayerControllerB playerInteracting)
    {
        if (playerInteracting != GameNetworkManager.Instance.localPlayerController || playerInteracting != ownerPlayer) return;
        if (UnityEngine.Random.Range(0f, 1f) < 0.9f || catPosing) StartPetAnimationServerRpc();
        else StartCatPoseAnimationServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void DropAllHeldItemsServerRpc()
    {
        DropAllHeldItemsClientRpc();
    }

    [ClientRpc]
    private void DropAllHeldItemsClientRpc()
    {
        DropAllHeldItems();
    }

    private void DropAllHeldItems()
    {
        int heldItemCount = itemsHeldList.Count;
        Plugin.ExtendedLogging($"Items held: {heldItemCount}");
        for (int i = heldItemCount - 1; i >= 0; i--)
        {
            HandleDroppingItem(itemsHeldList[i]);
        }
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
        StartCoroutine(SetFaceToHearts());
        GalVoice.PlayOneShot(PatSound);
    }

    private IEnumerator SetFaceToHearts()
    {
        var currentState = galState;
        var currentEmotion = galEmotion;
        RobotFaceController.SetFaceState(Emotion.Heart, 100);
        yield return new WaitForSeconds(PatSound.length);
        if (currentState != galState) yield break;
        RobotFaceController.SetFaceState(currentEmotion, 100);

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

    private void InteractTriggersUpdate()
    {
        bool interactable = galState != State.Inactive && (ownerPlayer != null && GameNetworkManager.Instance.localPlayerController == ownerPlayer);
        bool idleInteractable = galState != State.DeliveringItems && galState != State.AttackMode && interactable;
        HeadPatTrigger.interactable = interactable;
        ChestTrigger.interactable = idleInteractable && itemsHeldList.Count > 0;

        foreach (InteractTrigger trigger in GiveItemTrigger)
        {
            trigger.interactable = idleInteractable && itemsHeldList.Count > 0;
        }
    }

    private void BoomboxUpdate()
    {
        if (!boomboxPlaying) return;

        boomboxTimer += Time.deltaTime;
        if (boomboxTimer >= 2f)
        {
            boomboxTimer = 0f;
            boomboxPlaying = false;
        }
    }

    private void StoppingDistanceUpdate()
    {
        Agent.stoppingDistance = galState == State.AttackMode ? 6f : 3f;
    }

    private void IdleUpdate()
    {
        idleTimer += Time.deltaTime;
        if (idleTimer <= idleNeededTimer) return;

        idleTimer = 0f;
        idleNeededTimer = galRandom.NextFloat(5f, 10f);
        GalSFX.PlayOneShot(IdleSounds[galRandom.NextInt(0, IdleSounds.Length - 1)]);
    }

    public void Update()
    {
        if (galState == State.Inactive) return;
        FlySource.volume = Plugin.ModConfig.ConfigShockwaveBotPropellerVolume.Value;
        if (ownerPlayer != null && ownerPlayer.isPlayerDead) ownerPlayer = null;
        InteractTriggersUpdate();
        BoomboxUpdate();
        StoppingDistanceUpdate();
        IdleUpdate();

        if (!IsHost) return;
        HostSideUpdate();
    }

    private void HostSideUpdate()
    {
        if (StartOfRound.Instance.shipIsLeaving || !StartOfRound.Instance.shipHasLanded || StartOfRound.Instance.inShipPhase)
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
                DoDancing();
                break;
            case State.AttackMode:
                DoAttackMode();
                break;
            case State.SellingItems:
                DoSellingItems();
                break;
        }
    }

    private bool GoToChargerAndDeactivate()
    {
        if (Agent.enabled)
        {
            if (isInside)
            {
                GoThroughEntrance(false);
            }
            else
            {
                if (DetermineIfNeedToDisableAgent(ShockwaveCharger.ChargeTransform.position))
                {
                    return false;
                }
                Agent.SetDestination(ShockwaveCharger.ChargeTransform.position);
            }
            if (Vector3.Distance(transform.position, ShockwaveCharger.ChargeTransform.position) <= Agent.stoppingDistance)
            {
                if (!Agent.hasPath || Agent.velocity.sqrMagnitude <= 0.01f)
                {
                    ShockwaveCharger.ActivateGirlServerRpc(-1);
                    return true;
                }
            }
        }
        else
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(ShockwaveCharger.ChargeTransform.position - transform.position), Time.deltaTime * 5f);
            transform.position = Vector3.MoveTowards(transform.position, ShockwaveCharger.ChargeTransform.position, Agent.speed * Time.deltaTime);
            if (Vector3.Distance(transform.position, ShockwaveCharger.ChargeTransform.position) <= 0.1f)
            {
                ShockwaveCharger.ActivateGirlServerRpc(-1);
                return true;
            }
        }
        return false;
    }

    private void DoStaringAtOwner(PlayerControllerB ownerPlayer)
    {
        // Check if owner is staring
        Vector3 directionToDrone = (DroneHead.position - ownerPlayer.gameplayCamera.transform.position).normalized;
        float dotProduct = Vector3.Dot(ownerPlayer.gameplayCamera.transform.forward, directionToDrone);

        if (dotProduct > 0.8f) // Owner is staring at the drone (adjust threshold as needed)
        {
            staringTimer += Time.deltaTime;
            if (staringTimer >= stareThreshold)
            {
                // Gradually rotate to face the owner
                Vector3 lookDirection = (ownerPlayer.gameplayCamera.transform.position - transform.position).normalized;
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 2f); // Adjust rotation speed as needed
                if (staringTimer >= stareThreshold + 1.5f)
                {
                    staringTimer = 0f;
                }
            }
        }
        else
        {
            staringTimer = 0f;
        }
    }

    private bool DetermineIfNeedToDisableAgent(Vector3 destination)
    {
        NavMeshPath path = new NavMeshPath();
        if ((!Agent.CalculatePath(destination, path) || path.status == NavMeshPathStatus.PathPartial) && Vector3.Distance(transform.position, destination) > 7f)
        {
            Agent.SetDestination(Agent.pathEndPosition);
            if (Vector3.Distance(Agent.transform.position, Agent.pathEndPosition) <= Agent.stoppingDistance)
            {
                Agent.SetDestination(destination);
                if (!Agent.CalculatePath(destination, path) || path.status != NavMeshPathStatus.PathComplete)
                {
                    Vector3 nearbyPoint;
                    if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                    {
                        nearbyPoint = hit.position;
                        pointToGo = nearbyPoint;
                        Animator.SetBool(attackModeAnimation, true);
                        Agent.enabled = false;
                    }
                }
            }
            return true;
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
        if (onCompanyMoon)
        {
            HandleStateAnimationSpeedChanges(State.SellingItems, Emotion.OpenEye);
            return;
        }
        if (Vector3.Distance(transform.position, ownerPlayer.transform.position) > 3f)
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

        DoStaringAtOwner(ownerPlayer);

        if (isInside && elevatorScript != null && !usingElevator)
        {
            bool galCloserToTop = Vector3.Distance(transform.position, elevatorScript.elevatorTopPoint.position) < Vector3.Distance(transform.position, elevatorScript.elevatorBottomPoint.position);
            bool ownerCloserToTop = Vector3.Distance(ownerPlayer.transform.position, elevatorScript.elevatorTopPoint.position) < Vector3.Distance(ownerPlayer.transform.position, elevatorScript.elevatorBottomPoint.position);
            if (galCloserToTop != ownerCloserToTop)
            {
                UseTheElevator(elevatorScript);
                return;
            }
        }
        bool playerIsInElevator = elevatorScript != null && !elevatorScript.elevatorFinishedMoving && Vector3.Distance(ownerPlayer.transform.position, elevatorScript.elevatorInsidePoint.position) < 3f;
        if (!usingElevator && !playerIsInElevator && DetermineIfNeedToDisableAgent(ownerPlayer.transform.position))
        {
            return;
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

        if (!backFlipping && UnityEngine.Random.Range(0f, 25000f) <= 1f && Agent.velocity.sqrMagnitude <= 0.01f && Vector3.Distance(Agent.transform.position, ownerPlayer.transform.position) <= 5f)
        {
            DoBackFliplol();
            return;
        }
        if (!usingElevator) Agent.SetDestination(ownerPlayer.transform.position);
        else if (usingElevator && elevatorScript != null) this.transform.position = elevatorScript.elevatorInsidePoint.position;
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
            if (DetermineIfNeedToDisableAgent(ShockwaveCharger.ChargeTransform.position))
            {
                return;
            }
            Agent.SetDestination(ShockwaveCharger.ChargeTransform.position);
            if (Vector3.Distance(this.transform.position, ShockwaveCharger.ChargeTransform.position) <= Agent.stoppingDistance)
            {
                if (!Agent.hasPath || Agent.velocity.sqrMagnitude == 0f)
                {
                    DropAllHeldItemsServerRpc();
                }
            }
        }
        else
        {
            GoThroughEntrance(false);
        }
    }

    private void DoDancing()
    {
        if (itemsHeldList.Count >= maxItemsToHold)
        {
            HandleStateAnimationSpeedChangesServerRpc((int)State.DeliveringItems, (int)Emotion.OpenEye);
            return;
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
        if (Vector3.Distance(transform.position, targetEnemy.transform.position) <= Agent.stoppingDistance || currentlyAttacking)
        {
            Vector3 targetPosition = targetEnemy.transform.position;
            Vector3 direction = (targetPosition - this.transform.position).normalized;
            direction.y = 0; // Keep the y component zero to prevent vertical rotation

            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
            }
            if (!currentlyAttacking)
            {
                currentlyAttacking = true;
                NetworkAnimator.SetTrigger(startAttackAnimation);
            }
        }
    }


    private void DoSellingItems()
    {
        // Stop if the quota is fulfilled or no desk is available
        if (TimeOfDay.Instance.quotaFulfilled >= TimeOfDay.Instance.profitQuota || depositItemsDesk == null)
        {
            GoToChargerAndDeactivate();
            return;
        }

        // Initialize the selling list if it's empty and not already in selling mode
        if (itemsToSell.Count == 0 && !isSellingItems)
        {
            isSellingItems = true; // Set the flag to indicate the AI is actively selling items
            float sellPercentage = StartOfRound.Instance.companyBuyingRate;
            int quota = TimeOfDay.Instance.profitQuota;
            int currentlySoldAmount = TimeOfDay.Instance.quotaFulfilled;
            List<GrabbableObject> itemsOnShip = GetItemsOnShip();
            itemsToSell = GetItemsToSell(itemsOnShip, quota, sellPercentage, currentlySoldAmount);
            
            // Validate if we have enough items to fulfill the quota
            int totalValue = itemsToSell.Sum(item => item.scrapValue);
            if ((totalValue * sellPercentage + currentlySoldAmount) < quota)
            {
                // Not enough value to fulfill quota, clear the list and reset the flag
                itemsToSell.Clear();
                isSellingItems = false;
                depositItemsDesk = null;
                return;
            }

            // Mark items as non-grabbable to reserve them for selling
            foreach (GrabbableObject item in itemsToSell)
            {
                SetItemAsGrabbableClientRpc(new NetworkObjectReference(item.NetworkObject), false);
            }
        }

        // Proceed to grab items from the selling list if there are items to sell
        if (itemsHeldList.Count < maxItemsToHold && itemsToSell.Count > 0)
        {
            GrabbableObject itemToGrab = itemsToSell[0];
            if (Vector3.Distance(transform.position, itemToGrab.transform.position) <= 1f)
            {
                HandleGrabbingItemClientRpc(new NetworkObjectReference(itemToGrab.NetworkObject), itemsHeldList.Count);
                itemsToSell.RemoveAt(0);
            }
            else
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(itemToGrab.transform.position - transform.position), Time.deltaTime * 5f);
                transform.position = Vector3.MoveTowards(transform.position, itemToGrab.transform.position, Agent.speed * Time.deltaTime);
            }
        }
        else if (itemsHeldList.Count > 0)
        {
            // Sell the items to the deposit desk by doing a distance check
            if (Vector3.Distance(transform.position, depositItemsDesk.deskObjectsContainer.transform.position) <= 5f)
            {
                int heldItemCount = itemsHeldList.Count;
                for (int i = heldItemCount - 1; i >= 0; i--)
                {
                    GrabbableObject grabbableObject = itemsHeldList[i];
                    HandleDroppingItemClientRpc(i);
                    depositItemsDesk.AddObjectToDeskServerRpc(new NetworkObjectReference(grabbableObject.NetworkObject));
                    Vector3 dropPosition = GetRandomPointOnDesk(depositItemsDesk, grabbableObject);
                    SetPositionOfItemsClientRpc(dropPosition, new NetworkObjectReference(grabbableObject.NetworkObject));
                    grabbableObject.transform.position = dropPosition;
                }

                if (itemsToSell.Count == 0)
                {
                    // Selling is complete, reset the selling state
                    depositItemsDesk.SellItemsOnServer();
                    depositItemsDesk = null;
                    isSellingItems = false;
                }
            }
            else
            {
                Vector3 targetPosition = depositItemsDesk.deskObjectsContainer.transform.position;
                float distance = Vector3.Distance(transform.position, targetPosition);
                    
                if (distance > 5f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(targetPosition - transform.position), Time.deltaTime * 5f);
                    transform.position = Vector3.MoveTowards(transform.position, targetPosition, Agent.speed * Time.deltaTime);
                }
            }
        }
    }

    [ClientRpc]
    private void SetPositionOfItemsClientRpc(Vector3 position, NetworkObjectReference networkObjectReference)
    {
        GameObject gameObject = networkObjectReference;
        gameObject.transform.position = position;
    }

    private Vector3 GetRandomPointOnDesk(DepositItemsDesk depositItemsDesk, GrabbableObject grabbableObject)
    {
        Vector3 vector = RoundManager.RandomPointInBounds(depositItemsDesk.triggerCollider.bounds);
        vector.y = depositItemsDesk.triggerCollider.bounds.min.y;
        RaycastHit raycastHit;
        if (Physics.Raycast(new Ray(vector + Vector3.up * 3f, Vector3.down), out raycastHit, 8f, 1048640, QueryTriggerInteraction.Collide))
        {
            vector = raycastHit.point;
        }
        vector.y += grabbableObject.itemProperties.verticalOffset;
        vector = depositItemsDesk.deskObjectsContainer.transform.InverseTransformPoint(vector);
        return vector;
    }
    private List<GrabbableObject> GetItemsToSell(List<GrabbableObject> itemsOnShip, int quota, float sellPercentage, int currentSoldAmount)
    {
        // Get the items that fulfill the quota with minimal excess value
        itemsOnShip = itemsOnShip.OrderBy(item => item.scrapValue).ToList();
        List<GrabbableObject> itemsToSell = new List<GrabbableObject>();
        int accumulatedValue = currentSoldAmount;

        foreach (GrabbableObject item in itemsOnShip)
        {
            if (accumulatedValue >= quota)
            {
                break;
            }

            itemsToSell.Add(item);
            accumulatedValue += Mathf.RoundToInt(item.scrapValue * sellPercentage);
        }

        return itemsToSell;
    }

    private List<GrabbableObject> GetItemsOnShip()
    {
        return FindObjectsByType<GrabbableObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID).Where(item => item.scrapValue > 0 && item.itemProperties.isScrap).ToList();
    }

    [ClientRpc]
    private void SetItemAsGrabbableClientRpc(NetworkObjectReference networkObjectReference, bool grabbable)
    {
        GrabbableObject grabbableObject = ((GameObject)networkObjectReference).GetComponent<GrabbableObject>();
        grabbableObject.grabbable = grabbable;
    }

    private void DoBackFliplol()
    {
        NetworkAnimator.SetTrigger(backFlipAnimation);
    }

    private IEnumerator StopDancingDelay()
    {
        yield return new WaitUntil(() => !boomboxPlaying || galState != State.Dancing);
        if (galState != State.Dancing) yield break;  
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

            if (galState != State.FollowingPlayer || ownerPlayer == null || !Agent.enabled || chargeCount <= 0 || isInside && !ownerPlayer.isInsideFactory || !isInside && ownerPlayer.isInsideFactory) continue;

            // Use OverlapSphereNonAlloc to reduce garbage collection
            Collider[] hitColliders = new Collider[20];  // Size accordingly to expected max enemies
            int numHits = Physics.OverlapSphereNonAlloc(ownerPlayer.gameplayCamera.transform.position, 15, hitColliders, LayerMask.GetMask("Enemies"), QueryTriggerInteraction.Collide);

            for (int i = 0; i < numHits; i++)
            {
                Collider collider = hitColliders[i];
                if (!collider.TryGetComponent(out EnemyAI enemy) && collider.GetComponent<NetworkObject>() == null)
                {
                    NetworkObject networkObject = collider.GetComponentInParent<NetworkObject>();
                    if (networkObject == null || !networkObject.TryGetComponent(out EnemyAI enemy2))
                        continue;
                        
                    enemy = enemy2;
                }

                if (enemy == null || enemy.isEnemyDead || !enemy.enemyType.canDie || enemyTargetBlacklist.Contains(enemy.enemyType.enemyName))
                    continue;

                // First, do a simple direction check to see if the enemy is in front of the player
                Vector3 directionToEnemy = (collider.transform.position - ownerPlayer.gameplayCamera.transform.position).normalized;
                // Then check if there's a clear line of sight
                if (!Physics.Raycast(ownerPlayer.gameplayCamera.transform.position, directionToEnemy, out RaycastHit hit, 15, StartOfRound.Instance.collidersAndRoomMaskAndDefault | LayerMask.GetMask("Enemies"), QueryTriggerInteraction.Collide))
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
            laserShockBlack.laserOrigin = LaserOrigin;

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
        SetFlying(true);
    }

    private void StopFlyingAnimEvent()
    {
        SetFlying(false);
    }

    private void SetFlying(bool flying)
    {
        this.flying = flying;
        backFlipping = flying;
        if (flying) FlySource.UnPause();
        else FlySource.Pause();
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
        if ((backFlipping && targetEnemy == null) || catPosing) currentSpeed = 0f;
        Agent.speed = currentSpeed;
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
                SetAnimatorBools(holding: false, attack: false, dance: false, activated: false);
                break;
            case State.Active:
            case State.FollowingPlayer:
                SetAnimatorBools(holding: false, attack: false, dance: false, activated: true);
                break;
            case State.DeliveringItems:
                SetAnimatorBools(holding: true, attack: false, dance: false, activated: true);
                break;
            case State.Dancing:
                SetAnimatorBools(holding: false, attack: false, dance: true, activated: true);
                break;
            case State.AttackMode:
            case State.SellingItems:
                SetAnimatorBools(holding: false, attack: true, dance: false, activated: true);
                break;
        }
    }

    void SetAnimatorBools(bool holding, bool attack, bool dance, bool activated)
    {
        Animator.SetBool(holdingItemAnimation, holding);
        Animator.SetBool(attackModeAnimation, attack);
        Animator.SetBool(danceAnimation, dance);
        Animator.SetBool(activatedAnimation, activated);
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
            RobotMode mode;
            switch (stateToSwitchTo)
            {
                case State.Inactive:
                    mode = HandleStateInactiveChange();
                    break;
                case State.Active:
                    mode = HandleStateActiveChange();
                    break;
                case State.FollowingPlayer:
                    mode = HandleStateFollowingPlayerChange();
                    break;
                case State.DeliveringItems:
                    mode = HandleStateDeliveringItemsChange();
                    break;
                case State.Dancing:
                    mode = HandleStateDancingChange();
                    break;
                case State.AttackMode:
                    mode = HandleStateAttackModeChange();
                    break;
                case State.SellingItems:
                    mode = HandleStateSellingItemsChange();
                    break;
                default: mode = RobotMode.Normal; break;
            }
            RobotFaceController.SetMode(mode);
            galState = stateToSwitchTo;
        }

        if (emotion != -1)
        {
            RobotFaceController.SetFaceState(emotionToSwitchTo, 100);
        }
    }

    #region State Changes
    private RobotMode HandleStateInactiveChange()
    {
        DropAllHeldItems();

        ownerPlayer = null;
        Agent.enabled = false;
        return RobotMode.Normal;
    }

    private RobotMode HandleStateActiveChange()
    {
        RobotFaceController.SetFaceState(Emotion.OpenEye, 100);
        if (!onCompanyMoon) Agent.enabled = true;
        return RobotMode.Normal;
    }

    private RobotMode HandleStateFollowingPlayerChange()
    {
        GalVoice.PlayOneShot(GreetOwnerSound);
        return RobotMode.Normal;
    }

    private RobotMode HandleStateDeliveringItemsChange()
    {
        return RobotMode.Normal;
    }

    private RobotMode HandleStateDancingChange()
    {
        return RobotMode.Normal;
    }

    private RobotMode HandleStateAttackModeChange()
    {
        DropAllHeldItems();
        return RobotMode.Combat;
    }

    private RobotMode HandleStateSellingItemsChange()
    {
        Agent.enabled = false;
        return RobotMode.Normal;
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
        Animator.SetBool(holdingItemAnimation, true);
        GrabItemOwnerHoldingClientRpc(indexOfOwner);
    }

    [ClientRpc]
    private void GrabItemOwnerHoldingClientRpc(int indexOfOwner)
    {
        PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[indexOfOwner];
        StartCoroutine(HandleGrabbingItem(player.currentlyHeldObjectServer, itemsHeldTransforms[itemsHeldList.Count]));
    }

    [ClientRpc]
    private void HandleGrabbingItemClientRpc(NetworkObjectReference networkObjectReference, int heldTransform)
    {
        StartCoroutine(HandleGrabbingItem(((GameObject)networkObjectReference).GetComponent<GrabbableObject>(), itemsHeldTransforms[heldTransform]));
    }

    private IEnumerator HandleGrabbingItem(GrabbableObject item, Transform heldTransform)
    {
        yield return new WaitForSeconds(0.2f);
        item.isInElevator = false;
        item.isInShipRoom = false;
        item.playerHeldBy?.DiscardHeldObject();
        yield return new WaitForSeconds(0.2f);
        item.grabbable = false;
        item.isHeldByEnemy = true;
        item.hasHitGround = false;
        item.parentObject = heldTransform;
        item.EnablePhysics(false);
        itemsHeldList.Add(item);
        if (heldTransform.gameObject.name.EndsWith("L"))
        {
            item.transform.localRotation = Quaternion.Euler(0, -100, 0);
        }
        else
        {
            item.transform.localRotation = Quaternion.Euler(0, 100, 0);
        }
        GalVoice.PlayOneShot(TakeDropItemSounds[galRandom.NextInt(0, TakeDropItemSounds.Length - 1)]);
        HoarderBugAI.grabbableObjectsInMap.Remove(item.gameObject);
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
        if (!isSellingItems)
        {
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
        }
        itemsHeldList.Remove(item);
        GalVoice.PlayOneShot(TakeDropItemSounds[galRandom.NextInt(0, TakeDropItemSounds.Length - 1)]);
        if (itemsHeldList.Count == 0 && IsServer)
        {
            Animator.SetBool(holdingItemAnimation, false);
        }
        if (ownerPlayer != null && Vector3.Distance(this.transform.position, ShockwaveCharger.ChargeTransform.position) < 5)
        {
            ownerPlayer.SetItemInElevator(true, true, item);
        }
        else if (!isSellingItems)
        {
            item.transform.SetParent(StartOfRound.Instance.propsContainer, true);
        }
    }

    public void GoThroughEntrance(bool followingPlayer)
    {
        Vector3 destination = Vector3.zero;
        Vector3 destinationAfterTeleport = Vector3.zero;
        EntranceTeleport entranceTeleportToUse = null!;

        if (followingPlayer)
        {
            // Find the closest entrance to the player
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

        if (elevatorScript != null && NeedsElevator(destination, entranceTeleportToUse, elevatorScript))
        {
            UseTheElevator(elevatorScript);
            return;
        }

        if (Vector3.Distance(transform.position, destination) <= 3f)
        {
            lastUsedEntranceTeleport = entranceTeleportToUse;
            Agent.Warp(destinationAfterTeleport);
            EnablePhysicsClientRpc(false);
            SetShockwaveGalOutsideOrInsideServerRpc();
        }
        else
        {
            Agent.SetDestination(destination);
        }
    }

    private bool NeedsElevator(Vector3 destination, EntranceTeleport entranceTeleportToUse, MineshaftElevatorController elevatorScript)
    {
        // Determine if the elevator is needed based on destination proximity and current position
        bool nearMainEntrance = Vector3.Distance(destination, RoundManager.FindMainEntrancePosition(true, false)) < Vector3.Distance(destination, entranceTeleportToUse.transform.position);
        bool closerToTop = Vector3.Distance(transform.position, elevatorScript.elevatorTopPoint.position) < Vector3.Distance(transform.position, elevatorScript.elevatorBottomPoint.position);
        return isInside && (nearMainEntrance && !closerToTop) || (!nearMainEntrance && closerToTop);
    }

    private void UseTheElevator(MineshaftElevatorController elevatorScript)
    {
        // Determine if we need to go up or down based on current position and destination
        bool goUp = Vector3.Distance(transform.position, elevatorScript.elevatorBottomPoint.position) < Vector3.Distance(transform.position, elevatorScript.elevatorTopPoint.position);
        // Check if the elevator is finished moving
        if (elevatorScript.elevatorFinishedMoving)
        {
            if (elevatorScript.elevatorDoorOpen)
            {
                // If elevator is not called yet and is at the wrong level, call it
                if (NeedToCallElevator(elevatorScript, goUp))
                {
                    elevatorScript.CallElevatorOnServer(goUp);
                    MoveToWaitingPoint(elevatorScript, goUp);
                    return;
                }
                // Move to the inside point of the elevator if not already there
                if (Vector3.Distance(transform.position, elevatorScript.elevatorInsidePoint.position) > 1f)
                {
                    if (physicsEnabled) EnablePhysicsClientRpc(false);
                    Agent.SetDestination(elevatorScript.elevatorInsidePoint.position);
                }
                else if (!usingElevator)
                {
                    // Press the button to start moving the elevator
                    elevatorScript.PressElevatorButtonOnServer(true);
                    StartCoroutine(StopUsingElevator(elevatorScript));
                }
            }
        }
        else
        {
            MoveToWaitingPoint(elevatorScript, goUp);
        }
    }

    private IEnumerator StopUsingElevator(MineshaftElevatorController elevatorScript)
    {
        usingElevator = true;
        yield return new WaitForSeconds(2f);
        yield return new WaitUntil(() => elevatorScript.elevatorDoorOpen && elevatorScript.elevatorFinishedMoving);
        Plugin.ExtendedLogging("Stopped using elevator");
        usingElevator = false;
    }

    private bool NeedToCallElevator(MineshaftElevatorController elevatorScript, bool needToGoUp)
    {
        return !elevatorScript.elevatorCalled && ((!elevatorScript.elevatorIsAtBottom && needToGoUp) || (elevatorScript.elevatorIsAtBottom && !needToGoUp));
    }

    private void MoveToWaitingPoint(MineshaftElevatorController elevatorScript, bool needToGoUp)
    {
        // Elevator is currently moving
        // Move to the appropriate waiting point (bottom or top)
        if (Vector3.Distance(transform.position, elevatorScript.elevatorInsidePoint.position) > 1f)
        {
            Agent.SetDestination(needToGoUp ? elevatorScript.elevatorBottomPoint.position : elevatorScript.elevatorTopPoint.position);
        }
        else
        {
            // Wait at the inside point for the elevator to arrive
            Agent.SetDestination(elevatorScript.elevatorInsidePoint.position);
        }
    }

	public void DetectNoise(Vector3 noisePosition, float noiseLoudness, int timesPlayedInOneSpot = 0, int noiseID = 0)
	{
        if (galState == State.Inactive) return;
		if (noiseID == 5 && !Physics.Linecast(transform.position, noisePosition, StartOfRound.Instance.collidersAndRoomMask))
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