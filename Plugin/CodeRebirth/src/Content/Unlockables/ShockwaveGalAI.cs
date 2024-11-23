using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.Util.Extensions;
using GameNetcodeStuff;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using static CodeRebirth.src.Content.Unlockables.ShockwaveFaceController;

namespace CodeRebirth.src.Content.Unlockables;
public class ShockwaveGalAI : GalAI
{
    public ShockwaveFaceController RobotFaceController = null!;
    public SkinnedMeshRenderer FaceSkinnedMeshRenderer = null!;
    public Renderer FaceRenderer = null!;
    public InteractTrigger HeadPatTrigger = null!;
    public InteractTrigger ChestTrigger = null!;
    public List<InteractTrigger> GiveItemTrigger = new();
    public List<Transform> itemsHeldTransforms = new();
    public AnimationClip CatPoseAnim = null!;
    [NonSerialized] public Emotion galEmotion = Emotion.ClosedEye;
    public Transform LaserOrigin = null!;
    public AudioSource FlySource = null!;
    public AudioClip PatSound = null!;
    public AudioClip[] TakeDropItemSounds = [];

    private float sellingMovementSpeed = 6f;
    private bool isSellingItems = false;
    private List<GrabbableObject> itemsHeldList = new();
    private List<GrabbableObject> itemsToSell = new();
    private bool flying = false;
    private int maxItemsToHold = 4;
    private State galState = State.Inactive;
    private bool catPosing = false;
    private bool backFlipping = false;
    private Coroutine? headPatCoroutine = null;
    private bool onCompanyMoon = false;
    private DepositItemsDesk? depositItemsDesk = null;
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

    private void StartUpDelay()
    {
        ShockwaveCharger[] shockwaveChargers = FindObjectsByType<ShockwaveCharger>(FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID);
        if (shockwaveChargers.Length <= 0)
        {
            if (IsServer) NetworkObject.Despawn();
            Plugin.Logger.LogError($"ShockwaveCharger not found in scene. ShockwaveGalAI will not be functional.");
            return;
        }
        ShockwaveCharger shockwaveCharger = shockwaveChargers.OrderBy(x => Vector3.Distance(transform.position, x.transform.position)).First();;
        shockwaveCharger.GalAI = this;
        GalCharger = shockwaveCharger;
        HeadPatTrigger.onInteract.AddListener(OnHeadInteract);
        ChestTrigger.onInteract.AddListener(OnChestInteract);
        // Automatic activation if configured
        if (Plugin.ModConfig.ConfigShockwaveBotAutomatic.Value)
        {
            StartCoroutine(GalCharger.ActivateGalAfterLand());
        }

        // Adding listener for interaction trigger
        GalCharger.ActivateOrDeactivateTrigger.onInteract.AddListener(GalCharger.OnActivateGal);
        foreach (InteractTrigger trigger in GiveItemTrigger)
        {
            trigger.onInteract.AddListener(GrabItemInteract);
        }
        StartCoroutine(CheckForNearbyEnemiesToOwner());
        ResetToChargerStation(galState, galEmotion);
    }

    public override void ActivateGal(PlayerControllerB owner)
    {
        base.ActivateGal(owner);
        int activePlayerCount = StartOfRound.Instance.allPlayerScripts.Where(x => x.isPlayerControlled).Count();
        if (activePlayerCount == 1 || Plugin.ModConfig.ConfigShockwaveHoldsFourItems.Value)
        {
            maxItemsToHold = 4;
        }
        else
        {
            maxItemsToHold = 2;
        }
        depositItemsDesk = FindObjectOfType<DepositItemsDesk>();
        onCompanyMoon = RoundManager.Instance.currentLevel.levelID == 3;
        ResetToChargerStation(State.Active, Emotion.OpenEye);
    }

    private void ResetToChargerStation(State state, Emotion emotion)
    {
        if (!IsServer) return;
        if (Agent.enabled) Agent.Warp(GalCharger.ChargeTransform.position);
        else transform.position = GalCharger.ChargeTransform.position;
        transform.rotation = GalCharger.ChargeTransform.rotation;
        HandleStateAnimationSpeedChanges(state, emotion);
    }

    public override void DeactivateGal()
    {
        base.DeactivateGal();
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
        if ((UnityEngine.Random.Range(0f, 1f) < 0.9f || catPosing) && headPatCoroutine == null) StartPetAnimationServerRpc();
        else if (!catPosing) StartCatPoseAnimationServerRpc();
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
        headPatCoroutine = StartCoroutine(SetFaceToHearts());
        GalVoice.PlayOneShot(PatSound);
    }

    private IEnumerator SetFaceToHearts()
    {
        var currentState = galState;
        RobotFaceController.SetFaceState(Emotion.Heart, 100);
        yield return new WaitForSeconds(PatSound.length);
        if (currentState != galState) yield break;
        RobotFaceController.SetFaceState(Emotion.OpenEye, 100);
        headPatCoroutine = null;
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
        bool interactable = !inActive && (ownerPlayer != null && GameNetworkManager.Instance.localPlayerController == ownerPlayer);
        bool idleInteractable = galState != State.AttackMode && interactable;
        HeadPatTrigger.interactable = interactable;
        ChestTrigger.interactable = idleInteractable && itemsHeldList.Count > 0;

        foreach (InteractTrigger trigger in GiveItemTrigger)
        {
            trigger.interactable = idleInteractable && ownerPlayer != null && ownerPlayer.currentlyHeldObjectServer != null && galState != State.DeliveringItems;
        }
    }

    private void StoppingDistanceUpdate()
    {
        Agent.stoppingDistance = galState == State.AttackMode ? 6f : 3f;
    }

    private void SetIdleDefaultStateForEveryone()
    {
        if (GalCharger == null)
        {
            Plugin.Logger.LogInfo("Syncing for client");
            FlySource.Play();
            galRandom = new System.Random(StartOfRound.Instance.randomMapSeed + 69);
            chargeCount = Plugin.ModConfig.ConfigShockwaveCharges.Value;
            maxChargeCount = chargeCount;
            Agent.enabled = false;
            FlySource.volume = 0f;
            foreach (string enemy in Plugin.ModConfig.ConfigShockwaveBotEnemyBlacklist.Value.Split(','))
            {
                enemyTargetBlacklist.Add(enemy.Trim());
            }
            StartUpDelay();
        }
    }

    public override void InActiveUpdate()
    {
        base.InActiveUpdate();
        inActive = galState == State.Inactive;
    }

    public override void Update()
    {
        base.Update();
        SetIdleDefaultStateForEveryone();
        InteractTriggersUpdate();
        if (galState == State.Inactive && GalCharger != null)
        {
            this.transform.position = GalCharger.transform.position;
            this.transform.rotation = GalCharger.transform.rotation;
            return;
        }
        if (flying) FlySource.volume = Plugin.ModConfig.ConfigShockwaveBotPropellerVolume.Value;
        StoppingDistanceUpdate();

        if (!IsHost) return;
        HostSideUpdate();
    }

    private float GetCurrentSpeedMultiplier()
    {
        float speedMultiplier = (galState == State.FollowingPlayer || galState == State.AttackMode) ? 2f : 1f; // Speed when farthest
        if ((backFlipping && targetEnemy == null) || catPosing) speedMultiplier = 0f;

        return speedMultiplier;
    }

    private void HostSideUpdate()
    {
        if (StartOfRound.Instance.shipIsLeaving || !StartOfRound.Instance.shipHasLanded || StartOfRound.Instance.inShipPhase)
        {
            GalCharger.ActivateGirlServerRpc(-1);
            return;
        }
        if (Agent.enabled) smartAgentNavigator.AdjustSpeedBasedOnDistance(GetCurrentSpeedMultiplier());
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

    public override void OnEnableOrDisableAgent(bool agentEnabled)
    {
        base.OnEnableOrDisableAgent(agentEnabled);
        Animator.SetBool(attackModeAnimation, !agentEnabled);
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

        if (smartAgentNavigator.DoPathingToDestination(ownerPlayer.transform.position, ownerPlayer.isInsideFactory, true, ownerPlayer))
        {
            return;
        }

        DoStaringAtOwner(ownerPlayer);

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

        if (!backFlipping && UnityEngine.Random.Range(0f, 25000f) <= 2f && Agent.velocity.sqrMagnitude <= 0.01f && Vector3.Distance(Agent.transform.position, ownerPlayer.transform.position) <= 5f)
        {
            DoBackFliplol();
            return;
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
                GalCharger.ActivateGirlServerRpc(-1);
            }
        }

        smartAgentNavigator.DoPathingToDestination(GalCharger.ChargeTransform.position, false, false, null);
        if (Vector3.Distance(this.transform.position, GalCharger.ChargeTransform.position) <= Agent.stoppingDistance)
        {
            if (!Agent.hasPath || Agent.velocity.sqrMagnitude == 0f)
            {
                DropAllHeldItemsServerRpc();
            }
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
            smartAgentNavigator.DoPathingToDestination(targetEnemy.transform.position, !targetEnemy.isOutside, true, ownerPlayer);
            if (!smartAgentNavigator.CurrentPathIsValid() || (Vector3.Distance(this.transform.position, ownerPlayer.transform.position) > 15 && Plugin.ModConfig.ConfigDontTargetFarEnemies.Value))
            {
                HandleStateAnimationSpeedChanges(State.FollowingPlayer, Emotion.OpenEye);
                return;
            }
        }
        if (Vector3.Distance(transform.position, targetEnemy.transform.position) <= (Agent.stoppingDistance + 5f) || currentlyAttacking)
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
        if ((!Plugin.ModConfig.ConfigGalBypassQuota.Value && TimeOfDay.Instance.quotaFulfilled >= TimeOfDay.Instance.profitQuota) || depositItemsDesk == null)
        {
            if (isInHangarShipRoom)
            {
                Vector3 targetPosition = GalCharger.ChargeTransform.position;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(targetPosition - transform.position), Time.deltaTime * 5f);
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, sellingMovementSpeed * Time.deltaTime);
            }
            else
            {
                Vector3 targetPosition = StartOfRound.Instance.shipDoorNode.position - Vector3.up * 0.7f;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(targetPosition - transform.position), Time.deltaTime * 5f);
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, sellingMovementSpeed * Time.deltaTime);
            }
            if (Vector3.Distance(transform.position, GalCharger.ChargeTransform.position) <= Agent.stoppingDistance)
            {
                GalCharger.ActivateGirlServerRpc(-1);
            }
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
            if (itemToGrab == null)
            {
                Plugin.Logger.LogError("Item held/being targetted by gal became null somehow");
                depositItemsDesk = null;
                return;
            }
            if (Vector3.Distance(transform.position, itemToGrab.transform.position) <= 1f)
            {
                HandleGrabbingItemClientRpc(new NetworkObjectReference(itemToGrab.NetworkObject), itemsHeldList.Count);
                itemsToSell.RemoveAt(0);
            }
            else
            {
                bool bothInShip = isInHangarShipRoom && StartOfRound.Instance.shipInnerRoomBounds.bounds.Contains(itemToGrab.transform.position);
                bool bothNotInShip = !isInHangarShipRoom && !StartOfRound.Instance.shipInnerRoomBounds.bounds.Contains(itemToGrab.transform.position);
                if (bothInShip || bothNotInShip)
                {
                    Vector3 targetPosition = itemToGrab.transform.position;
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(targetPosition - transform.position), Time.deltaTime * 5f);
                    transform.position = Vector3.MoveTowards(transform.position, targetPosition, sellingMovementSpeed * Time.deltaTime);
                }
                else if (isInHangarShipRoom || StartOfRound.Instance.shipInnerRoomBounds.bounds.Contains(itemToGrab.transform.position))
                {
                    Vector3 targetPosition = StartOfRound.Instance.shipDoorNode.position - Vector3.up * 0.7f;
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(targetPosition - transform.position), Time.deltaTime * 5f);
                    transform.position = Vector3.MoveTowards(transform.position, targetPosition, sellingMovementSpeed * Time.deltaTime);
                }
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
                    Vector3 dropPosition = GetRandomPointOnDesk(depositItemsDesk, grabbableObject);
                    HandleDroppingItemClientRpc(i, dropPosition);
                    depositItemsDesk.AddObjectToDeskServerRpc(new NetworkObjectReference(grabbableObject.NetworkObject));
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
                if (isInHangarShipRoom)
                {
                    Vector3 targetPosition = (StartOfRound.Instance.shipDoorNode.position - Vector3.up * 0.7f + (-StartOfRound.Instance.shipDoorNode.right * 6f));
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(targetPosition - transform.position), Time.deltaTime * 5f);
                    transform.position = Vector3.MoveTowards(transform.position, targetPosition, sellingMovementSpeed * Time.deltaTime);
                }
                else
                {
                    Vector3 targetPosition = depositItemsDesk.deskObjectsContainer.transform.position;
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(targetPosition - transform.position), Time.deltaTime * 5f);
                    transform.position = Vector3.MoveTowards(transform.position, targetPosition, sellingMovementSpeed * Time.deltaTime);
                }
            }
        }
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
        if (Plugin.ModConfig.ConfigGalBypassQuota.Value)
        {
            return itemsOnShip;
        }

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
        if (grabbableObject.playerHeldBy != null)
        {
            grabbableObject.playerHeldBy.DropAllHeldItems();
        }
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

    private IEnumerator CheckForNearbyEnemiesToOwner()
    {
        if (!IsServer) yield break;

        var delay = new WaitForSeconds(1f);
        while (true)
        {
            yield return delay;

            if (galState != State.FollowingPlayer || ownerPlayer == null || !Agent.enabled || chargeCount <= 0 || !smartAgentNavigator.isOutside && !ownerPlayer.isInsideFactory || smartAgentNavigator.isOutside && ownerPlayer.isInsideFactory) continue;

            // Use OverlapSphereNonAlloc to reduce garbage collection
            Collider[] hitColliders = new Collider[20];  // Size accordingly to expected max enemies
            int numHits = Physics.OverlapSphereNonAlloc(ownerPlayer.gameplayCamera.transform.position, 15, hitColliders, LayerMask.GetMask("Enemies"), QueryTriggerInteraction.Collide);

            for (int i = 0; i < numHits; i++)
            {
                Collider collider = hitColliders[i];
                if (!collider.gameObject.activeSelf) continue;
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
        StartCoroutine(FlyAnimationDelay());
    }

    private IEnumerator FlyAnimationDelay()
    {
        smartAgentNavigator.cantMove = true;
        yield return new WaitForSeconds(0.5f);
        smartAgentNavigator.cantMove = false;
    }

    private void StopFlyingAnimEvent()
    {
        SetFlying(false);
    }

    private void SetFlying(bool flying)
    {
        this.flying = flying;
        backFlipping = flying;
        if (flying) FlySource.volume = Plugin.ModConfig.ConfigShockwaveBotPropellerVolume.Value;
        else FlySource.volume = 0f;
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

    private void SetAnimatorBools(bool holding, bool attack, bool dance, bool activated)
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
            var mode = stateToSwitchTo switch
            {
                State.Inactive => HandleStateInactiveChange(),
                State.Active => HandleStateActiveChange(),
                State.FollowingPlayer => HandleStateFollowingPlayerChange(),
                State.DeliveringItems => HandleStateDeliveringItemsChange(),
                State.Dancing => HandleStateDancingChange(),
                State.AttackMode => HandleStateAttackModeChange(),
                State.SellingItems => HandleStateSellingItemsChange(),
                _ => RobotMode.Normal,
            };
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
    private void HandleDroppingItemClientRpc(int itemIndex, Vector3 dropPosition = default)
    {
        HandleDroppingItem(itemsHeldList[itemIndex], dropPosition);
    }

    private void HandleDroppingItem(GrabbableObject? item, Vector3 dropPosition = default)
    {
        if (item == null)
        {
            Plugin.Logger.LogError("Item was null in HandleDroppingItem");
            return;
        }
        if (!isSellingItems)
        {
            item.parentObject = null;
            if (StartOfRound.Instance.shipInnerRoomBounds.bounds.Contains(transform.position))
            {
                if ((itemsHeldList.Count - 1) == 0)
                {
                    HUDManager.Instance.DisplayTip("Scrap Delivered", "Shockwave Gal has delivered the items given to her to the ship!", false);
                }
                Plugin.ExtendedLogging($"Dropping item in ship room: {item}");
                item.isInShipRoom = true;
                item.isInElevator = true;
                item.transform.SetParent(GameNetworkManager.Instance.localPlayerController.playersManager.elevatorTransform, true);
                item.EnablePhysics(true);
                item.EnableItemMeshes(true);
                item.transform.localScale = item.originalScale;
                item.isHeld = false;
                item.isPocketed = false;
                item.fallTime = 0f;
                item.startFallingPosition = item.transform.parent.InverseTransformPoint(item.transform.position);
                Vector3 vector2 = item.GetItemFloorPosition(default(Vector3));
                item.targetFloorPosition = GameNetworkManager.Instance.localPlayerController.playersManager.elevatorTransform.InverseTransformPoint(vector2);
                item.floorYRot = -1;
                item.grabbable = true;
                item.isHeldByEnemy = false;
                item.transform.rotation = Quaternion.Euler(item.itemProperties.restingRotation);
            }
            else
            {
                item.isInShipRoom = false;
                item.isInElevator = false;
                item.EnablePhysics(true);
                item.fallTime = 0f;
                item.startFallingPosition = item.transform.parent.InverseTransformPoint(item.transform.position);
                item.targetFloorPosition = item.transform.parent.InverseTransformPoint(item.GetItemFloorPosition(default(Vector3)));
                item.floorYRot = -1;
                item.DiscardItemFromEnemy();
                item.grabbable = true;
                item.isHeldByEnemy = false;
                item.transform.rotation = Quaternion.Euler(item.itemProperties.restingRotation);
                item.transform.SetParent(StartOfRound.Instance.propsContainer, true);
            }
        }
        else if (dropPosition != default && depositItemsDesk != null)
        {
            Plugin.ExtendedLogging($"Dropping item in deposit: {item} at position: {dropPosition}");
            item.parentObject = depositItemsDesk.deskObjectsContainer.transform;
            item.isInShipRoom = false;
            item.isInElevator = false;
            item.EnablePhysics(true);
            item.fallTime = 0f;
            item.startFallingPosition = item.transform.parent.InverseTransformPoint(item.transform.position);
            item.targetFloorPosition = item.transform.parent.InverseTransformPoint(dropPosition);
            item.floorYRot = -1;
            item.DiscardItemFromEnemy();
            item.grabbable = false;
            item.isHeldByEnemy = false;
            item.transform.rotation = Quaternion.Euler(item.itemProperties.restingRotation);
        }

        itemsHeldList.Remove(item);
        GalVoice.PlayOneShot(TakeDropItemSounds[galRandom.NextInt(0, TakeDropItemSounds.Length - 1)]);
        if (itemsHeldList.Count == 0 && IsServer)
        {
            Animator.SetBool(holdingItemAnimation, false);
        }
    }

    public override void OnUseEntranceTeleport(bool setOutside)
    {
        base.OnUseEntranceTeleport(setOutside);
        for (int i = 0; i < itemsHeldList.Count; i++)
        {
            itemsHeldList[i].isInFactory = setOutside;
            itemsHeldList[i].transform.position = itemsHeldTransforms[i].position;
            StartCoroutine(SetItemPhysics(itemsHeldList[i]));
        }
    }

    private IEnumerator SetItemPhysics(GrabbableObject grabbableObject)
    {
        yield return new WaitForSeconds(0.1f);
        grabbableObject.EnablePhysics(false);
    }
}