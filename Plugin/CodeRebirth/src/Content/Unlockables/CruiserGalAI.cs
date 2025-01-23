using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.Util.Extensions;
using GameNetcodeStuff;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace CodeRebirth.src.Content.Unlockables;
public class CruiserGalAI : GalAI
{
    public SkinnedMeshRenderer FaceSkinnedMeshRenderer = null!;
    public Renderer FaceRenderer = null!;
    public List<InteractTrigger> GiveItemTrigger = new();
    public List<Transform> itemsHeldTransforms = new();
    public AudioSource FlySource = null!;
    public AudioClip[] TakeDropItemSounds = [];

    private List<GrabbableObject> itemsHeldList = new();
    private bool flying = false;
    private int maxItemsToHold = 99;
    private State galState = State.Inactive;
    private readonly static int holdingItemAnimation = Animator.StringToHash("holdingItem");
    private readonly static int danceAnimation = Animator.StringToHash("dancing");
    private readonly static int activatedAnimation = Animator.StringToHash("activated");
    private readonly static int runSpeedFloat = Animator.StringToHash("RunSpeed");

    public enum State
    {
        Inactive = 0,
        Active = 1,
        FollowingPlayer = 2,
        DeliveringItems = 3,
        Dancing = 4,
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer) return;

        NetworkObject.TrySetParent(GalCharger.transform, false);
        ResetToChargerStation(galState);
    }

    private void StartUpDelay()
    {
        List<CruiserCharger> CruiserChargers = new();
        foreach (var charger in Charger.Instances)
        {
            if (charger is CruiserCharger CruiserCharger1)
            {
                CruiserChargers.Add(CruiserCharger1);
            }
        }
        if (CruiserChargers.Count <= 0)
        {
            if (IsServer) NetworkObject.Despawn();
            Plugin.Logger.LogError($"CruiserCharger not found in scene. CruiserGalAI will not be functional.");
            return;
        }
        CruiserCharger CruiserCharger = CruiserChargers.OrderBy(x => Vector3.Distance(transform.position, x.transform.position)).First();;
        CruiserCharger.GalAI = this;
        GalCharger = CruiserCharger;
        // Automatic activation if configured
        if (Plugin.ModConfig.ConfigCruiserBotAutomatic.Value)
        {
            StartCoroutine(GalCharger.ActivateGalAfterLand());
        }

        // Adding listener for interaction trigger
        GalCharger.ActivateOrDeactivateTrigger.onInteract.AddListener(GalCharger.OnActivateGal);
        foreach (InteractTrigger trigger in GiveItemTrigger)
        {
            trigger.onInteract.AddListener(GrabItemInteract);
        }
    }

    public override void ActivateGal(PlayerControllerB owner)
    {
        base.ActivateGal(owner);
        ResetToChargerStation(State.Active);
    }

    private void ResetToChargerStation(State state)
    {
        if (!IsServer) return;
        if (Agent.enabled) Agent.Warp(GalCharger.ChargeTransform.position);
        else transform.position = GalCharger.ChargeTransform.position;
        transform.rotation = GalCharger.ChargeTransform.rotation;
        HandleStateAnimationSpeedChanges(state);
    }

    public override void DeactivateGal()
    {
        base.DeactivateGal();
        ResetToChargerStation(State.Inactive);
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

    private void InteractTriggersUpdate()
    {
        bool interactable = !inActive && (ownerPlayer != null && GameNetworkManager.Instance.localPlayerController == ownerPlayer);
        bool idleInteractable = interactable;

        foreach (InteractTrigger trigger in GiveItemTrigger)
        {
            trigger.interactable = idleInteractable && ownerPlayer.currentlyHeldObjectServer != null && galState != State.DeliveringItems;
        }
    }

    private void StoppingDistanceUpdate()
    {
        Agent.stoppingDistance = 3f;
    }

    private void SetIdleDefaultStateForEveryone()
    {
        if (GalCharger == null || (IsServer && !doneOnce))
        {
            doneOnce = true;
            Plugin.Logger.LogInfo("Syncing for client");
            FlySource.Play();
            galRandom = new System.Random(StartOfRound.Instance.randomMapSeed + 69);
            Agent.enabled = false;
            FlySource.volume = 0f;
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
        if (flying) FlySource.volume = Plugin.ModConfig.ConfigCruiserBotPropellerVolume.Value;
        StoppingDistanceUpdate();

        if (!IsHost) return;
        HostSideUpdate();
    }

    private float GetCurrentSpeedMultiplier()
    {
        float speedMultiplier = (galState == State.FollowingPlayer) ? 2f : 1f;

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
        }
    }

    public override void OnEnableOrDisableAgent(bool agentEnabled)
    {
        base.OnEnableOrDisableAgent(agentEnabled);
        Animator.SetBool(, !agentEnabled);
    }

    private void DoActive()
    {
        if (ownerPlayer == null)
        {
            GoToChargerAndDeactivate();
            return;
        }
        else
        {
            HandleStateAnimationSpeedChanges(State.FollowingPlayer);
        }
    }

    private void DoFollowingPlayer()
    {
        if (ownerPlayer == null)
        {
            GoToChargerAndDeactivate();
            return;
        }

        if (smartAgentNavigator.DoPathingToDestination(ownerPlayer.transform.position))
        {
            return;
        }

        DoStaringAtOwner(ownerPlayer);

        if (itemsHeldList.Count >= maxItemsToHold)
        { // change this to a trigger
            HandleStateAnimationSpeedChangesServerRpc((int)State.DeliveringItems, (int)Emotion.OpenEye);
            return;
        }

        if (boomboxPlaying)
        {
            HandleStateAnimationSpeedChanges(State.Dancing, Emotion.Happy);
            StartCoroutine(StopDancingDelay());
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

        smartAgentNavigator.DoPathingToDestination(GalCharger.ChargeTransform.position);
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

    private IEnumerator StopDancingDelay()
    {
        yield return new WaitUntil(() => !boomboxPlaying || galState != State.Dancing);
        if (galState != State.Dancing) yield break;  
        HandleStateAnimationSpeedChanges(State.FollowingPlayer);
    }

    private void PlayFootstepSoundAnimEvent()
    {
        GalSFX.PlayOneShot(FootstepSounds[galRandom.Next(0, FootstepSounds.Length)]);
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

    [ServerRpc(RequireOwnership = false)]
    private void HandleStateAnimationSpeedChangesServerRpc(int state)
    {
        HandleStateAnimationSpeedChanges((State)state);
    }

    private void HandleStateAnimationSpeedChanges(State state) // This is for host
    {
        SwitchStateClientRpc((int)state);
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
    private void SwitchStateServerRpc(int state)
    {
        SwitchStateClientRpc(state);
    }

    [ClientRpc]
    private void SwitchStateClientRpc(int state)
    {
        SwitchState(state);
    }

    private void SwitchState(int state) // this is for everyone.
    {
        State stateToSwitchTo = (State)state;
        if (state != -1)
        {
            switch (stateToSwitchTo)
            {
                State.Inactive => HandleStateInactiveChange(),
                State.Active => HandleStateActiveChange(),
                State.FollowingPlayer => HandleStateFollowingPlayerChange(),
                State.DeliveringItems => HandleStateDeliveringItemsChange(),
                State.Dancing => HandleStateDancingChange(),
            };
            galState = stateToSwitchTo;
        }
    }

    #region State Changes
    private void HandleStateInactiveChange()
    {
        DropAllHeldItems();

        ownerPlayer = null;
        Agent.enabled = false;
    }

    private void HandleStateActiveChange()
    {
        Agent.enabled = true;
    }

    private void HandleStateFollowingPlayerChange()
    {
        GalVoice.PlayOneShot(GreetOwnerSound);
    }

    private void HandleStateDeliveringItemsChange()
    {
    }

    private void HandleStateDancingChange()
    {
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
        GalVoice.PlayOneShot(TakeDropItemSounds[galRandom.Next(0, TakeDropItemSounds.Length)]);
        HoarderBugAI.grabbableObjectsInMap.Remove(item.gameObject);
    }

    [ServerRpc(RequireOwnership = false)]
    private void HandleDroppingItemServerRpc(int itemIndex, Vector3 dropPosition)
    {
        HandleDroppingItemClientRpc(itemIndex, dropPosition);
    }

    [ClientRpc]
    private void HandleDroppingItemClientRpc(int itemIndex, Vector3 dropPosition)
    {
        HandleDroppingItem(itemsHeldList[itemIndex], dropPosition);
    }

    private void HandleDroppingItem(GrabbableObject? item, Vector3 dropPosition)
    {
        if (item == null)
        {
            Plugin.Logger.LogError("Item was null in HandleDroppingItem");
            return;
        }

        item.parentObject = null;
        bool droppedItemIntoShip = StartOfRound.Instance.shipInnerRoomBounds.bounds.Contains(transform.position);
        if (droppedItemIntoShip)
        {
            if ((itemsHeldList.Count - 1) == 0)
            {
                HUDManager.Instance.DisplayTip("Scrap Delivered", "Cruiser Gal has delivered the items given to her to the ship!", false);
            }
            Plugin.ExtendedLogging($"Dropping item in ship room: {item}");
            item.transform.SetParent(GameNetworkManager.Instance.localPlayerController.playersManager.elevatorTransform, true);
        }
        else
        {
            item.transform.SetParent(StartOfRound.Instance.propsContainer, true);
        }

        item.isInShipRoom = droppedItemIntoShip;
        item.isInElevator = droppedItemIntoShip;
        item.transform.localScale = item.originalScale;

        item.isHeld = false;
        item.isPocketed = false;
        item.EnablePhysics(true);
        item.EnableItemMeshes(true);
        item.fallTime = 0f;
        item.startFallingPosition = item.transform.parent.InverseTransformPoint(item.transform.position);
        item.targetFloorPosition = item.transform.parent.InverseTransformPoint(item.GetItemFloorPosition(default(Vector3)));
        item.floorYRot = -1;
        item.grabbable = true;
        item.isHeldByEnemy = false;
        item.transform.rotation = Quaternion.Euler(item.itemProperties.restingRotation);

        itemsHeldList.Remove(item);
        GalVoice.PlayOneShot(TakeDropItemSounds[galRandom.Next(0, TakeDropItemSounds.Length)]);
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