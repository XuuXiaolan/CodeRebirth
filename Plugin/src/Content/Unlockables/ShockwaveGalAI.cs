using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GameNetcodeStuff;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.AI;
using static CodeRebirth.src.Content.Unlockables.ShockwaveFaceController;

namespace CodeRebirth.src.Content.Unlockables;
public class ShockwaveGalAI : NetworkBehaviour, INoiseListener //todo: buy the charger, which is inexpensive, but it spawns the shockwave gal automatically.
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
    [NonSerialized] public Emotion galEmotion = Emotion.ClosedEye;
    [NonSerialized] public ShockwaveCharger ShockwaveCharger = null!;

    private bool boomboxPlaying = false;
    private Dictionary<int, GrabbableObject?> itemsHeldDict = new();
    private EnemyAI? targetEnemy;
    private bool flying = false;
    private bool isInside = false;
    private readonly int maxItemsToHold = 2;
    private State galState = State.Inactive;
    private PlayerControllerB? ownerPlayer;
    private bool holdingItems = false;
    private int ItemCount => itemsHeldDict.Where(x => x.Value != null).Count();
    private List<string> enemyTargetBlacklist = new();
    private int chargeCount = 3;
    private bool currentlyAttacking = false;
    private float boomboxTimer = 0f;

    public enum State {
        Inactive = 0,
        Active = 1,
        FollowingPlayer = 2,
        DeliveringItems = 3,
        Dancing = 4,
        AttackMode = 5,
    }

    public enum Emotion {
        Heart = 0,
        ClosedEye = 1,
        OpenEye = 2,
        Happy = 3
    }

    public void Start() {
        Plugin.Logger.LogInfo("Hi creator");
        Agent.enabled = galState != State.Inactive;
        StartCoroutine(StartUpDelay());
        int count = 0;
        foreach (Transform transform in itemsHeldTransforms)
        {
            itemsHeldDict.Add(count, null);
            count++;
        }
    }

    private IEnumerator StartUpDelay() {
        yield return new WaitForSeconds(0.5f);
        this.transform.position = ShockwaveCharger.ChargeTransform.position;
        HeadPatTrigger.onInteract.AddListener(OnHeadInteract);
        foreach (InteractTrigger trigger in GiveItemTrigger)
        {
            trigger.onInteract.AddListener(GrabItemInteract);
        }
    }

    public void ActivateShockwaveGal(PlayerControllerB owner)
    {
        ownerPlayer = owner;
        if (IsServer)
        {
            Agent.Warp(ShockwaveCharger.ChargeTransform.position);
            this.transform.rotation = ShockwaveCharger.ChargeTransform.rotation;
            HandleStateAnimationSpeedChanges(State.Active, Emotion.OpenEye, 0f);
        }
    }

    public void DeactivateShockwaveGal()
    {
        ownerPlayer = null;
        if (IsServer)
        {
            Agent.Warp(ShockwaveCharger.ChargeTransform.position);
            this.transform.rotation = ShockwaveCharger.ChargeTransform.rotation;
            HandleStateAnimationSpeedChanges(State.Inactive, Emotion.ClosedEye, 0f);
        }
    }

    private void OnHeadInteract(PlayerControllerB playerInteracting)
    {
        if (playerInteracting != GameNetworkManager.Instance.localPlayerController || playerInteracting != ownerPlayer) return;
        StartPetAnimationServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartPetAnimationServerRpc()
    {
        NetworkAnimator.SetTrigger("startPet");
    }

    public void Update() {
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
        AdjustSpeedOnDistanceOnTargetPosition();
        Animator.SetFloat("RunSpeed", Agent.velocity.magnitude / 3);
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

    private void DoActive()
    {
        if (ownerPlayer == null)
        {
            // do something like turning it inactive.
            return;
        }
        if (Vector3.Distance(this.transform.position, ownerPlayer.transform.position) > 3f)
        {
            HandleStateAnimationSpeedChanges(State.FollowingPlayer, Emotion.OpenEye, 3f);
        }
    }

    private void DoFollowingPlayer()
    {
        if (ownerPlayer == null)
        {
            HandleStateAnimationSpeedChanges(State.Inactive, Emotion.ClosedEye, 0f);
            Agent.Warp(ShockwaveCharger.ChargeTransform.position);

            // return to charger and be inactive, or maybe just TP.
            return;
        }
        if ((!isInside && ownerPlayer.isInsideFactory) || (isInside && !ownerPlayer.isInsideFactory))
        {
            GoThroughEntrance();
            return;
        }
        if (Agent.pathStatus == NavMeshPathStatus.PathInvalid || Agent.pathStatus == NavMeshPathStatus.PathPartial)
        {
            // figure out a partial path or something, then tp maybe??
        }
        
        if (CheckForNearbyEnemiesToOwner())
        {
            HandleStateAnimationSpeedChanges(State.AttackMode, Emotion.OpenEye, 3f);
            return;
        }

        if (boomboxPlaying)
        {
            HandleStateAnimationSpeedChanges(State.Dancing, Emotion.Heart, 3f);
            StartCoroutine(StopDancingDelay());
            return;
        }

        Agent.SetDestination(ownerPlayer.transform.position);
    }

    private void DoDeliveringItems()
    {
        if (!holdingItems)
        {
            if (ownerPlayer != null)
            {
                HandleStateAnimationSpeedChangesServerRpc((int)State.FollowingPlayer, (int)Emotion.OpenEye, 3f);
            }
            else
            {
                ShockwaveCharger.ActivateGirlServerRpc(-1);
            }
        }

        if (!isInside)
        { // setup destination validation and whatnot with partial paths.
            Agent.SetDestination(ShockwaveCharger.ChargeTransform.position);
            if (Agent.pathStatus == NavMeshPathStatus.PathComplete)
            {
                for (int i = 0; i < ItemCount; i++) {
                    HandleDroppingItemServerRpc(i);
                }
                holdingItems = false;
            }
        }
        else
        {
            GoThroughEntrance();
        }
    }

    private void DoAttackMode()
    {
        if (targetEnemy == null || targetEnemy.isEnemyDead)
        {
            if (ownerPlayer != null && chargeCount > 0)
            {
                HandleStateAnimationSpeedChanges(State.FollowingPlayer, Emotion.OpenEye, 3f);
            }
            else
            {
                HandleStateAnimationSpeedChanges(State.Inactive, Emotion.OpenEye, 3f);
            }
            return;
        }

        if (Vector3.Distance(this.transform.position, targetEnemy.transform.position) >= 3f)
        {
            Agent.SetDestination(targetEnemy.transform.position);
        }
        else
        {
            Vector3 targetPosition = targetEnemy.transform.position;
            Vector3 direction = (targetPosition - this.transform.position).normalized;
            direction.y = 0; // Keep the y component zero to prevent vertical rotation

            if (direction != Vector3.zero) {
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
        HandleStateAnimationSpeedChanges(State.FollowingPlayer, Emotion.Heart, 3f);
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
        if (targetEnemy == null || targetEnemy.isEnemyDead || !IsOwner) return;
        RaycastHit[] raycastHits = Physics.RaycastAll(transform.position, transform.forward, 7.5f, LayerMask.GetMask("Enemies", "Player"), QueryTriggerInteraction.Ignore); 
        if (raycastHits.Length == 0) return;
        foreach (RaycastHit hit in raycastHits)
        {
            if (targetEnemy == null || targetEnemy.isEnemyDead) return;
            if (hit.transform == targetEnemy.transform)
            {
                if (IsOwner) targetEnemy.KillEnemyOnOwnerClient(false);
            }
            if (hit.transform.TryGetComponent(out PlayerControllerB? player))
            {
                if (player == null || player.isPlayerDead) return;
                player.DamagePlayer(player.health - (player.health - 1), true, true, CauseOfDeath.Blast, 0, false, (player.transform.position - this.transform.position).normalized * 50);
            }
        }
    }

    private void EndAttackAnimEvent()
    {
        currentlyAttacking = false;
        chargeCount--;
    }

    private void AdjustSpeedOnDistanceOnTargetPosition()
    {
        float distanceFromOwner = Agent.remainingDistance;

        // Define min and max distance thresholds
        float minDistance = 0f;
        float maxDistance = 40f;

        // Define min and max speed values
        float minSpeed = 0f; // Speed when closest
        float maxSpeed = 20f; // Speed when farthest

        // Clamp the distance within the range to avoid negative values or distances greater than maxDistance
        float clampedDistance = Mathf.Clamp(distanceFromOwner, minDistance, maxDistance);

        // Normalize the distance to a 0-1 range (minDistance to maxDistance maps to 0 to 1)
        float normalizedDistance = (clampedDistance - minDistance) / (maxDistance - minDistance);

        // Linearly interpolate the speed between minSpeed and maxSpeed based on normalized distance
        float currentSpeed = Mathf.Lerp(minSpeed, maxSpeed, normalizedDistance);
        Agent.speed = currentSpeed;
        // Apply the calculated speed (you would replace this with your actual movement logic)
        // Plugin.ExtendedLogging($"Speed based on distance: {currentSpeed}");
    }

    [ServerRpc(RequireOwnership = false)]
    private void HandleStateAnimationSpeedChangesServerRpc(int state, int emotion, float speed)
    {
        HandleStateAnimationSpeedChanges((State)state, (Emotion)emotion, speed);
    }

    private void HandleStateAnimationSpeedChanges(State state, Emotion emotion, float speed) // This is for host
    {
        Agent.speed = speed;
        SwitchStateOrEmotionServerRpc((int)state, (int)emotion);
        switch (state)
        {
            case State.Inactive:
                Animator.SetBool("attackMode", false);
                Animator.SetBool("dancing", false);
                Animator.SetBool("activated", false);
                break;
            case State.Active:
                Animator.SetBool("attackMode", false);
                Animator.SetBool("dancing", false);
                Animator.SetBool("activated", true);
                break;
            case State.FollowingPlayer:
                Animator.SetBool("attackMode", false);
                Animator.SetBool("dancing", false);
                Animator.SetBool("activated", true);
                break;
            case State.DeliveringItems:
                Animator.SetBool("attackMode", false);
                Animator.SetBool("dancing", false);
                Animator.SetBool("activated", true);
                // in host side of things, set destination to ship.
                break;
            case State.Dancing:
                Animator.SetBool("attackMode", false);
                Animator.SetBool("dancing", true);
                Animator.SetBool("activated", true);
                break;
            case State.AttackMode:
                Animator.SetBool("attackMode", true);
                Animator.SetBool("dancing", false);
                Animator.SetBool("activated", true);
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
        if (state != -1) {
            switch (stateToSwitchTo) {
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

        if (emotion != -1) {
            switch (emotionToSwitchTo) {
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

        for (int i = 0; i < ItemCount; i++) {
            HandleDroppingItem(itemsHeldDict[i]);
        }

        ownerPlayer = null;
        if (galState == State.AttackMode) RobotFaceController.SetMode(RobotMode.Normal);
        galState = State.Inactive;
    }

    private void HandleStateActiveChange()
    {
        if (galState == State.AttackMode) RobotFaceController.SetMode(RobotMode.Normal);
        RobotFaceController.SetFaceState(Emotion.OpenEye, 100);
        galState = State.Active;
    }

    private void HandleStateFollowingPlayerChange()
    {
        if (galState == State.AttackMode) RobotFaceController.SetMode(RobotMode.Normal);
        galState = State.FollowingPlayer;
    }

    private void HandleStateDeliveringItemsChange()
    {
        if (galState == State.AttackMode) RobotFaceController.SetMode(RobotMode.Normal);
        galState = State.DeliveringItems;
    }

    private void HandleStateDancingChange()
    {
        if (galState == State.AttackMode) RobotFaceController.SetMode(RobotMode.Normal);
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
        if (player != ownerPlayer || player.currentlyHeldObjectServer == null) return;
        if (ItemCount >= maxItemsToHold)
        {
            HandleStateAnimationSpeedChangesServerRpc((int)State.DeliveringItems, (int)Emotion.OpenEye, 0f);
        }
        else
        {
            GrabItemOwnerHoldingServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player));            
        }
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
        HandleGrabbingItem(player.currentlyHeldObjectServer, itemsHeldTransforms[ItemCount]);
    }

    private void HandleGrabbingItem(GrabbableObject item, Transform heldTransform)
    {
        item.playerHeldBy.DiscardHeldObject();
        holdingItems = true;
        item.grabbable = false;
        item.isHeldByEnemy = true;
        item.hasHitGround = false;
        item.parentObject = heldTransform;
        item.EnablePhysics(false);
        itemsHeldDict[ItemCount] = item;
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
        HandleDroppingItem(itemsHeldDict[itemIndex]);
        itemsHeldDict[itemIndex] = null;
    }

    private void HandleDroppingItem(GrabbableObject? item)
    {
        if (item == null)
        { // this got logged, fuck.
            Plugin.Logger.LogError("Item was null in HandleDroppingItem");
            return;
        }
        item.parentObject = null;
        if (IsServer) item.transform.SetParent(StartOfRound.Instance.propsContainer, true);
        item.EnablePhysics(true);
        item.fallTime = 0f;
        item.startFallingPosition = item.transform.parent.InverseTransformPoint(item.transform.position);
        item.targetFloorPosition = item.transform.parent.InverseTransformPoint(item.GetItemFloorPosition(default(Vector3)));
        item.floorYRot = -1;
        item.DiscardItemFromEnemy();
        item.grabbable = true;
        item.isHeldByEnemy = false;
        item.transform.rotation = Quaternion.Euler(item.itemProperties.restingRotation);
        if (ItemCount == 0) holdingItems = false;
    }

    public void GoThroughEntrance() {
        var insideEntrancePosition = RoundManager.FindMainEntrancePosition(true, false);
        var outsideEntrancePosition = RoundManager.FindMainEntrancePosition(true, true);
        if (!isInside) {
            Agent.SetDestination(outsideEntrancePosition);
            
            if (Vector3.Distance(transform.position, outsideEntrancePosition) <= 3f) {
                Agent.Warp(insideEntrancePosition);
                SetShockwaveGalOutsideServerRpc(false);
            }
        } else {
            Agent.SetDestination(insideEntrancePosition);
            if (Vector3.Distance(transform.position, insideEntrancePosition) <= 3f) {
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
    public void SetShockwaveGalOutsideClientRpc(bool setOutside) {
        isInside = !setOutside;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetEnemyTargetServerRpc(int enemyID) {
        SetEnemyTargetClientRpc(enemyID);
    }

    [ClientRpc]
    public void SetEnemyTargetClientRpc(int enemyID) {
        if (enemyID == -1) {
            targetEnemy = null;
            Plugin.ExtendedLogging($"Clearing Enemy target on {this}");
            return;
        }
        if (RoundManager.Instance.SpawnedEnemies[enemyID] == null) {
            Plugin.ExtendedLogging($"Enemy Index invalid! {this}");
            return;
        }
        targetEnemy = RoundManager.Instance.SpawnedEnemies[enemyID];
        Plugin.ExtendedLogging($"{this} setting target to: {targetEnemy.enemyType.enemyName}");
    }
}