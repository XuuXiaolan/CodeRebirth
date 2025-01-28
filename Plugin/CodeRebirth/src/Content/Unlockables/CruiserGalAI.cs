using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Unlockables;
public class CruiserGalAI : GalAI
{
    public InteractTrigger ChestCollisionToggleTrigger = null!;
    public InteractTrigger RadioTrigger = null!;
    public InteractTrigger WheelDumpScrapTrigger = null!;
    public InteractTrigger LeverThrowPlayerTrigger = null!;
    public List<Transform> ItemsHeldTransforms = new();
    public AudioSource RadioAudioSource = null!;
    public AudioClip[] TakeDropItemSounds = [];
    public AudioClip ChestCollisionToggleSound = null!;
    public AudioClip[] RadioSounds = [];

    private bool carryingPlayer = false;
    private PlayerControllerB? carriedPlayer = null;
    private List<GrabbableObject> itemsHeldList => GetHeldItemsList();
    private bool flying = false;
    private State galState = State.Inactive;
    private Coroutine? chestCollisionToggleCoroutine = null;
    private readonly static int chestCollisionToggleAnimation = Animator.StringToHash("hitBumper"); // Trigger
    private readonly static int pullLevelAnimation = Animator.StringToHash("pullLever"); // Trigger
    private readonly static int spinWheelAnimation = Animator.StringToHash("spinWheel"); // Trigger
    private readonly static int randomAnimation = Animator.StringToHash("doRandomAnimation"); // Trigger
    private readonly static int grabPlayerOntoSeatAnimation = Animator.StringToHash("putPlayerOnSeat"); // Trigger
    private readonly static int throwPlayerAnimation = Animator.StringToHash("throwPlayer"); // Trigger
    private readonly static int danceAnimation = Animator.StringToHash("dancing"); // Bool
    private readonly static int activatedAnimation = Animator.StringToHash("activated"); // Bool
    private readonly static int flyAnimation = Animator.StringToHash("flying"); // Bool
    private readonly static int runSpeedFloat = Animator.StringToHash("RunSpeed"); // Float

    public enum State
    {
        Inactive = 0,
        Active = 1,
        FollowingPlayer = 2,
        DeliveringPlayer = 3,
        Dancing = 4,
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
        ChestCollisionToggleTrigger.onInteract.AddListener(OnChestCollisionToggleInteract);
        RadioTrigger.onInteract.AddListener(OnRadioInteract);
        WheelDumpScrapTrigger.onInteract.AddListener(OnWheelDumpScrapInteract);
        // Automatic activation if configured
        if (Plugin.ModConfig.ConfigCruiserGalAutomatic.Value)
        {
            StartCoroutine(GalCharger.ActivateGalAfterLand());
        }

        // Adding listener for interaction trigger
        GalCharger.ActivateOrDeactivateTrigger.onInteract.AddListener(GalCharger.OnActivateGal);
    }

    public override void ActivateGal(PlayerControllerB owner)
    {
        base.ActivateGal(owner);
        ResetToChargerStation(State.Active);
        if (GalCharger is CruiserCharger cruiserCharger)
        {
            cruiserCharger.animator.SetBool(CruiserCharger.isActivatedAnimation, true);
        }
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
        if (GalCharger is CruiserCharger cruiserCharger)
        {
            cruiserCharger.animator.SetBool(CruiserCharger.isActivatedAnimation, false);
        }
    }

    private void OnWheelDumpScrapInteract(PlayerControllerB playerInteracting)
    {
        if (playerInteracting != GameNetworkManager.Instance.localPlayerController || playerInteracting != ownerPlayer) return;
        if (itemsHeldList.Count == 0) return;
        DropAllHeldItemsServerRpc();
    }

    private void OnRadioInteract(PlayerControllerB playerInteracting)
    {
        if (playerInteracting != GameNetworkManager.Instance.localPlayerController || playerInteracting != ownerPlayer) return;
        StartRadioServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartRadioServerRpc()
    {
        StartRadioClientRpc();
    }

    [ClientRpc]
    private void StartRadioClientRpc()
    {
        StartOrStopRadio();
    }

    private void StartOrStopRadio()
    {
        if (RadioAudioSource.isPlaying)
        {
            RadioAudioSource.Stop();
        }
        else
        {
            RadioAudioSource.clip = RadioSounds[galRandom.Next(0, RadioSounds.Length)];
            RadioAudioSource.Play();
        }
    }

    private void OnChestCollisionToggleInteract(PlayerControllerB playerInteracting)
    {
        if (playerInteracting != GameNetworkManager.Instance.localPlayerController || playerInteracting != ownerPlayer) return;
        if (chestCollisionToggleCoroutine == null) StartCollisionAnimationServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartCollisionAnimationServerRpc() // todo: do timing stuff with animation here
    {
        NetworkAnimator.SetTrigger(chestCollisionToggleAnimation);
        StartCollisionAnimationClientRpc();
    }

    [ClientRpc]
    private void StartCollisionAnimationClientRpc()
    {
        StartCoroutine(StartCollisionAnimation());
    }

    private IEnumerator StartCollisionAnimation()
    {
        GalVoice.PlayOneShot(ChestCollisionToggleSound);
        EnablePhysics(!physicsEnabled);
        yield break;
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
        bool interactable = !inActive && ownerPlayer != null && GameNetworkManager.Instance.localPlayerController == ownerPlayer;
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
            galRandom = new System.Random(StartOfRound.Instance.randomMapSeed + 69);
            Agent.enabled = false;
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
        StoppingDistanceUpdate();

        if (!IsHost) return;
        HostSideUpdate();
    }

    private float GetCurrentSpeedMultiplier()
    {
        float speedMultiplier = (galState == State.FollowingPlayer ? 2f : 1f);

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
            case State.DeliveringPlayer:
                DoDeliveringPlayer();
                break;
            case State.Dancing:
                DoDancing();
                break;
        }
    }

    public override void OnEnableOrDisableAgent(bool agentEnabled)
    {
        base.OnEnableOrDisableAgent(agentEnabled);
        Animator.SetBool(flyAnimation, !agentEnabled);
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

        if (boomboxPlaying)
        {
            HandleStateAnimationSpeedChanges(State.Dancing);
            StartCoroutine(StopDancingDelay());
            return;
        }
    }

    private void DoDeliveringPlayer()
    {
        if (!carryingPlayer) // Trigger an animation to set all this stuff.
        {
            if (ownerPlayer == null)
            {
                HandleStateAnimationSpeedChangesServerRpc((int)State.FollowingPlayer);
            }
            else if (carriedPlayer == ownerPlayer)
            {
                carryingPlayer = true;
            }
            return;
        }

        Vector3 destination = RoundManager.FindMainEntrancePosition(true, smartAgentNavigator.isOutside);
        smartAgentNavigator.DoPathingToDestination(destination);
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
                SetAnimatorBools(dance: false, activated: false);
                break;
            case State.Active:
            case State.FollowingPlayer:
                SetAnimatorBools(dance: false, activated: true);
                break;
            case State.DeliveringPlayer:
                SetAnimatorBools(dance: false, activated: true);
                break;
            case State.Dancing:
                SetAnimatorBools(dance: true, activated: true);
                break;
        }
    }

    private void SetAnimatorBools(bool dance, bool activated)
    {
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
                case State.Inactive:
                    HandleStateInactiveChange();
                    break;
                case State.Active: 
                    HandleStateActiveChange();
                    break;
                case State.FollowingPlayer: 
                    HandleStateFollowingPlayerChange();
                    break;
                case State.DeliveringPlayer: 
                    HandleStateDeliveringPlayerChange();
                    break;
                case State.Dancing: 
                    HandleStateDancingChange();
                    break;
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

    private void HandleStateDeliveringPlayerChange()
    {
    }

    private void HandleStateDancingChange()
    {
    }
    #endregion

    private List<GrabbableObject> GetHeldItemsList()
    {
        return transform.GetComponentsInChildren<GrabbableObject>().ToList();
    }

    private void HandleDroppingItem(GrabbableObject item)
    {
        item.parentObject = null;
        bool droppedItemIntoShip = StartOfRound.Instance.shipInnerRoomBounds.bounds.Contains(transform.position);
        if (droppedItemIntoShip)
        {
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
        item.fallTime = 0f;
        item.startFallingPosition = item.transform.parent.InverseTransformPoint(item.transform.position);
        item.targetFloorPosition = item.transform.parent.InverseTransformPoint(item.GetItemFloorPosition(default(Vector3)));
        item.floorYRot = -1;
        item.transform.rotation = Quaternion.Euler(item.itemProperties.restingRotation);
        // Make the grabbableobject play a sound GalVoice.PlayOneShot(TakeDropItemSounds[galRandom.Next(0, TakeDropItemSounds.Length)]);
    }

    public override void OnUseEntranceTeleport(bool setOutside)
    {
        base.OnUseEntranceTeleport(setOutside);
        for (int i = 0; i < itemsHeldList.Count; i++)
        {
            itemsHeldList[i].isInFactory = setOutside;
            itemsHeldList[i].transform.position = ItemsHeldTransforms[i].position;
            StartCoroutine(SetItemPhysics(itemsHeldList[i]));
        }
    }

    private IEnumerator SetItemPhysics(GrabbableObject grabbableObject)
    {
        yield return new WaitForSeconds(0.1f);
        grabbableObject.EnablePhysics(false);
    }

    #region Anim Events
    public void OnThrowPlayerAnimEvent()
    {
        if (carriedPlayer == null) return;
        carriedPlayer.externalForceAutoFade += Vector3.up * 150f;
        carriedPlayer = null;
        carryingPlayer = false;
    }
    #endregion
}