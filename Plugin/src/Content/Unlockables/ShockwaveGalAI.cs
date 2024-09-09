using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using GameNetcodeStuff;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.AI;
using static CodeRebirth.src.Content.Unlockables.ShockwaveFaceController;

namespace CodeRebirth.src.Content.Unlockables;
public class ShockwaveGalAI : NetworkBehaviour //todo: buy the charger, which is inexpensive, but it spawns the shockwave gal automatically.
{
    public ShockwaveFaceController RobotFaceController = null!;
    [NonSerialized] public ShockwaveCharger ShockwaveCharger = null!;
    public SkinnedMeshRenderer FaceSkinnedMeshRenderer = null!;
    public Renderer FaceRenderer = null!;
    public Animator Animator = null!;
    public NetworkAnimator NetworkAnimator = null!;
    public NavMeshAgent Agent = null!;
    public InteractTrigger HeadPatTrigger = null!;
    public List<InteractTrigger> GiveItemTrigger = new();
    public List<Transform> itemsHeldTransforms = new();

    private bool flying = false;
    private bool isInside = false;
    private int maxItemsToHold = 2;
    private int nextEmptyHeldSlot = 0;
    [NonSerialized] public Emotion galEmotion = Emotion.ClosedEye;
    private State galState = State.Inactive;
    private PlayerControllerB? ownerPlayer;
    private bool holdingItems = false;
    private List<GrabbableObject> itemsHeld = new();

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
        StartCoroutine(StartUpDelay());
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
            HandleStateAnimationSpeedChanges(State.Active, Emotion.OpenEye, 0f);
        }
    }

    public void DeactivateShockwaveGal()
    {
        ownerPlayer = null;
        if (IsServer)
        {
            Agent.Warp(ShockwaveCharger.ChargeTransform.position);
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
        if (galState == State.Inactive) return;
        HeadPatTrigger.enabled = (galState != State.AttackMode && galState != State.Inactive);
        foreach (InteractTrigger trigger in GiveItemTrigger)
        {
            trigger.enabled = (galState != State.AttackMode && galState != State.Inactive);
        }
        if (!IsHost) return;
        
        HostSideUpdate();
    }

    private void HostSideUpdate()
    {
        Animator.SetFloat("RunSpeed", Agent.velocity.magnitude / 3);
        switch (galState)
        {
            case State.Inactive:
                DoInactive();
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

    private void DoInactive()
    {
    }

    private void DoActive()
    {
    }

    private void DoFollowingPlayer()
    {
        if (ownerPlayer == null)
        {
            // return to charger and be inactive, or maybe just TP.
            return;
        }
        if ((!isInside && ownerPlayer.isInsideFactory) || (isInside && !ownerPlayer.isInsideFactory))
        {
            GoThroughEntrance();
            return;
        }
        Agent.SetDestination(ownerPlayer.transform.position);
        if (Agent.pathStatus == NavMeshPathStatus.PathInvalid || Agent.pathStatus == NavMeshPathStatus.PathPartial)
        {
            // figure out a partial path or something, then tp maybe??
        }
    }

    private void DoDeliveringItems() // todo: move where the charger is at a position closer to center of ship for navmesh reasons.
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
                for (int i = 0; i < itemsHeld.Count; i++) {
                    if (itemsHeld[i] == null) continue;
                    HandleDroppingItemServerRpc(i);
                    nextEmptyHeldSlot--;
                }
                holdingItems = false;
            }            
        }
        else
        {
            GoThroughEntrance();
        }
    }

    private void DoDancing()
    {
    }

    private void DoAttackMode()
    {
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
        holdingItems = false;

        foreach (GrabbableObject item in itemsHeld) {
            if (item == null) continue;
            HandleDroppingItem(item);
            nextEmptyHeldSlot--;
        }

        itemsHeld.Clear();
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
        if (itemsHeld.Count >= maxItemsToHold)
        {
            HandleStateAnimationSpeedChangesServerRpc((int)State.DeliveringItems, (int)Emotion.OpenEye, 0f);
        }
        else
        {
            StartCoroutine(player.waitToEndOfFrameToDiscard());
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
        HandleGrabbingItem(player.currentlyHeldObjectServer, itemsHeldTransforms[nextEmptyHeldSlot]);
    }

    private void HandleGrabbingItem(GrabbableObject item, Transform heldTransform)
    {
        holdingItems = true;
        item.grabbable = false;
        item.isHeldByEnemy = true;
        item.hasHitGround = false;
        item.parentObject = heldTransform;
        item.EnablePhysics(false);
        itemsHeld.Add(item);
        HoarderBugAI.grabbableObjectsInMap.Remove(item.gameObject);
        nextEmptyHeldSlot++;
    }

    [ServerRpc(RequireOwnership = false)]
    private void HandleDroppingItemServerRpc(int itemIndex)
    {
        HandleDroppingItemClientRpc(itemIndex);
    }

    [ClientRpc]
    private void HandleDroppingItemClientRpc(int itemIndex)
    {
        HandleDroppingItem(itemsHeld[itemIndex]);
    }

    private void HandleDroppingItem(GrabbableObject item)
    {
        item.parentObject = null;
        if (IsServer) item.transform.SetParent(StartOfRound.Instance.propsContainer, true);
        item.EnablePhysics(true);
        item.fallTime = 0f;
        Plugin.ExtendedLogging("Dropping Item");
        Plugin.ExtendedLogging($"Item Position: {item.transform.position}");
        Plugin.ExtendedLogging($"Item Parent: {item.transform.parent}");
        item.startFallingPosition = item.transform.parent.InverseTransformPoint(item.transform.position);
        item.targetFloorPosition = item.transform.parent.InverseTransformPoint(item.GetItemFloorPosition(default(Vector3)));
        item.floorYRot = -1;
        item.DiscardItemFromEnemy();
        item.grabbable = true;
        item.isHeldByEnemy = false;
        item.transform.rotation = Quaternion.Euler(item.itemProperties.restingRotation);
        itemsHeld.Remove(item);
    }

    public void GoThroughEntrance() {
        var insideEntrancePosition = RoundManager.FindMainEntrancePosition(true, false);
        var outsideEntrancePosition = RoundManager.FindMainEntrancePosition(true, true);
        if (!isInside) {
            Agent.SetDestination(outsideEntrancePosition);
            
            if (Vector3.Distance(transform.position, outsideEntrancePosition) < 1f) {
                Agent.Warp(insideEntrancePosition);
                SetShockwaveGalOutsideServerRpc(false);
            }
        } else {
            Agent.SetDestination(insideEntrancePosition);
            if (Vector3.Distance(transform.position, insideEntrancePosition) < 1f) {
                Agent.Warp(outsideEntrancePosition);
                SetShockwaveGalOutsideServerRpc(true);
            }
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

}