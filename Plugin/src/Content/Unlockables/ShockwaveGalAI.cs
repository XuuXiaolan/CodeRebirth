using System;
using System.Collections;
using System.Collections.Generic;
using GameNetcodeStuff;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.AI;
using static CodeRebirth.src.Content.Unlockables.ShockwaveFaceController;

namespace CodeRebirth.src.Content.Unlockables;
public class ShockwaveGalAI : NetworkBehaviour, INoiseListener
{
    public ShockwaveFaceController RobotFaceController = null!;
    public SkinnedMeshRenderer FaceSkinnedMeshRenderer = null!;
    public Renderer FaceRenderer = null!;
    public Animator Animator = null!;
    public NetworkAnimator NetworkAnimator = null!;
    public NavMeshAgent Agent = null!;
    public InteractTrigger HeadPatTrigger = null!;
    public InteractTrigger CatPoseTrigger = null!;
    public List<InteractTrigger> GiveItemTrigger = new();
    public List<Transform> itemsHeldTransforms = new();
    public AnimationClip CatPoseAnim = null!;
    [NonSerialized] public Emotion galEmotion = Emotion.ClosedEye;
    [NonSerialized] public ShockwaveCharger ShockwaveCharger = null!;
    public Collider[] colliders = [];
    public Transform LaserOrigin = null!;

    private bool boomboxPlaying = false;
    private List<GrabbableObject> itemsHeldList = new();
    private EnemyAI? targetEnemy;
    private bool flying = false;
    private bool isInside = false;
    private readonly int maxItemsToHold = 4;
    private State galState = State.Inactive;
    private PlayerControllerB? ownerPlayer;
    private List<string> enemyTargetBlacklist = new();
    private int chargeCount = 3;
    private int maxChargeCount;
    private bool currentlyAttacking = false;
    private float boomboxTimer = 0f;
    private bool physicsEnabled = true;
    private bool catPosing = false;
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
        maxChargeCount = chargeCount;
        Agent.enabled = galState != State.Inactive;
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
        CatPoseTrigger.onInteract.AddListener(OnChestInteract);
        foreach (InteractTrigger trigger in GiveItemTrigger)
        {
            trigger.onInteract.AddListener(GrabItemInteract);
        }
    }

    public void ActivateShockwaveGal(PlayerControllerB owner)
    {
        if (chargeCount <= 0) return;
        ownerPlayer = owner;
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
        StartPetAnimationServerRpc();
    }

    private void OnChestInteract(PlayerControllerB playerInteracting)
    {
        if (playerInteracting != GameNetworkManager.Instance.localPlayerController || playerInteracting != ownerPlayer) return;
        StartCatPoseAnimationServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartPetAnimationServerRpc()
    {
        NetworkAnimator.SetTrigger(pettingAnimation);
        EnablePhysicsClientRpc(!physicsEnabled);
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
        Agent.enabled = galState != State.Inactive;
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
        if (!IsHost) return;
        
        HostSideUpdate();
    }

    private void HostSideUpdate()
    {
        if ((StartOfRound.Instance.shipIsLeaving || !StartOfRound.Instance.shipHasLanded || StartOfRound.Instance.inShipPhase) && Agent.enabled)
        {
            Agent.Warp(ShockwaveCharger.ChargeTransform.position);
            this.transform.rotation = ShockwaveCharger.ChargeTransform.rotation;
            HandleStateAnimationSpeedChanges(State.Inactive, Emotion.ClosedEye);
            return;
        }
        AdjustSpeedOnDistanceOnTargetPosition();
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
        if (StartOfRound.Instance.shipIsLeaving || !StartOfRound.Instance.shipHasLanded || StartOfRound.Instance.inShipPhase)
        {
            Agent.Warp(ShockwaveCharger.ChargeTransform.position);
            this.transform.rotation = ShockwaveCharger.ChargeTransform.rotation;
            HandleStateAnimationSpeedChanges(State.Inactive, Emotion.ClosedEye);
            return true;
        }
        Agent.SetDestination(ShockwaveCharger.ChargeTransform.position);
        if (Vector3.Distance(this.transform.position, ShockwaveCharger.ChargeTransform.position) < Agent.stoppingDistance)
        {
            if (!Agent.hasPath || Agent.velocity.sqrMagnitude <= 0.01f)
            {
                Agent.Warp(ShockwaveCharger.ChargeTransform.position);
                this.transform.rotation = ShockwaveCharger.ChargeTransform.rotation;
                HandleStateAnimationSpeedChanges(State.Inactive, Emotion.ClosedEye);
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
        if ((!isInside && ownerPlayer.isInsideFactory) || (isInside && !ownerPlayer.isInsideFactory))
        {
            GoThroughEntrance();
            return;
        }
        if (Agent.pathStatus == NavMeshPathStatus.PathInvalid || Agent.pathStatus == NavMeshPathStatus.PathPartial)
        {
            if (Agent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                if (NavMesh.SamplePosition(ownerPlayer.transform.position, out NavMeshHit hit, 10f, NavMesh.AllAreas))
                {
                    Agent.Warp(ownerPlayer.transform.position);
                }
            }
            else
            {
                Agent.SetDestination(Agent.pathEndPosition);
            }
            return;
        }
        
        if (itemsHeldList.Count >= maxItemsToHold)
        {
            HandleStateAnimationSpeedChangesServerRpc((int)State.DeliveringItems, (int)Emotion.OpenEye);
            return;
        }

        if (CheckForNearbyEnemiesToOwner())
        {
            HandleStateAnimationSpeedChanges(State.AttackMode, Emotion.OpenEye);
            return;
        }

        if (boomboxPlaying)
        {
            HandleStateAnimationSpeedChanges(State.Dancing, Emotion.Heart);
            StartCoroutine(StopDancingDelay());
            return;
        }

        Agent.SetDestination(ownerPlayer.transform.position);
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
        { // setup destination validation and whatnot with partial paths.
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
            GoThroughEntrance();
        }
    }

    private void DoAttackMode()
    {
        if (targetEnemy == null || targetEnemy.isEnemyDead || chargeCount <= 0 || ownerPlayer == null)
        {
            if (ownerPlayer != null && chargeCount > 0)
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
            Agent.SetDestination(targetEnemy.transform.position);
        }
        if (Agent.remainingDistance <= Agent.stoppingDistance || currentlyAttacking)
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
                NetworkAnimator.SetTrigger("startAttack");
            }
        }
    }

    private IEnumerator StopDancingDelay()
    {
        yield return new WaitUntil(() => !boomboxPlaying);  
        HandleStateAnimationSpeedChanges(State.FollowingPlayer, Emotion.Heart);
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

    private bool CheckForNearbyEnemiesToOwner()
    {
        if (ownerPlayer == null) return false;
        Collider[] hitColliders = Physics.OverlapSphere(ownerPlayer.transform.position, 6f, LayerMask.GetMask("Enemies"));
        foreach (Collider collider in hitColliders)
        {
            if (collider.TryGetComponent(out EnemyAI? enemy))
            {
                if (enemy == null || enemy.isEnemyDead || !enemy.enemyType.canDie) continue;

                SetEnemyTargetServerRpc(RoundManager.Instance.SpawnedEnemies.IndexOf(enemy));
                return true;
            }
        }
        return false;
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

    private void AdjustSpeedOnDistanceOnTargetPosition()
    {
        float distanceToDestination = Agent.remainingDistance;

        // Define min and max distance thresholds
        float minDistance = 0f;
        float maxDistance = 40f;

        // Define min and max speed values
        float minSpeed = 0f; // Speed when closest
        float maxSpeed = galState == State.FollowingPlayer ? 20f : 10f; // Speed when farthest

        // Clamp the distance within the range to avoid negative values or distances greater than maxDistance
        float clampedDistance = Mathf.Clamp(distanceToDestination, minDistance, maxDistance);

        // Normalize the distance to a 0-1 range (minDistance to maxDistance maps to 0 to 1)
        float normalizedDistance = (clampedDistance - minDistance) / (maxDistance - minDistance);

        // Linearly interpolate the speed between minSpeed and maxSpeed based on normalized distance
        float currentSpeed = Mathf.Lerp(minSpeed, maxSpeed, normalizedDistance);
        if (!catPosing) Agent.speed = currentSpeed;
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
        SwitchStateOrEmotionServerRpc((int)state, (int)emotion);
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
        flying = false;
        
        int heldItemCount = itemsHeldList.Count;
        for (int i = heldItemCount - 1; i >= 0; i--)
        {
            HandleDroppingItem(itemsHeldList[i]);
        }

        ownerPlayer = null;
        RobotFaceController.SetMode(RobotMode.Normal);
        galState = State.Inactive;
    }

    private void HandleStateActiveChange()
    {
        RobotFaceController.SetMode(RobotMode.Normal);
        RobotFaceController.SetFaceState(Emotion.OpenEye, 100);
        galState = State.Active;
    }

    private void HandleStateFollowingPlayerChange()
    {
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
        if (itemsHeldList.Count == 0 && IsServer)
        {
            Animator.SetBool(holdingItemAnimation, false);
        }
    }

    public void GoThroughEntrance()
    {
        var insideEntrancePosition = RoundManager.FindMainEntrancePosition(true, false);
        var outsideEntrancePosition = RoundManager.FindMainEntrancePosition(true, true);
        if (!isInside)
        {
            Agent.SetDestination(outsideEntrancePosition);
            
            if (Vector3.Distance(transform.position, outsideEntrancePosition) <= 3f)
            {
                Agent.Warp(insideEntrancePosition);
                SetShockwaveGalOutsideServerRpc(false);
            }
        }
        else
        {
            Agent.SetDestination(insideEntrancePosition);
            if (Vector3.Distance(transform.position, insideEntrancePosition) <= 3f)
            {
                Agent.Warp(outsideEntrancePosition);
                SetShockwaveGalOutsideServerRpc(true);
            }
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
    private void SetShockwaveGalOutsideServerRpc(bool setOutside)
    {
        SetShockwaveGalOutsideClientRpc(setOutside);
    }

    [ClientRpc]
    public void SetShockwaveGalOutsideClientRpc(bool setOutside)
    {
        for (int i = 0; i < itemsHeldList.Count; i++)
        {
            itemsHeldList[i].isInFactory = !setOutside;
            itemsHeldList[i].transform.position = itemsHeldTransforms[i].position;
            StartCoroutine(SetItemPhysics(itemsHeldList[i]));
        }
        isInside = !setOutside;
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
}