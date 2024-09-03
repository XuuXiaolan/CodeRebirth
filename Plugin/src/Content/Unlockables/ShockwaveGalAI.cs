using System;
using System.Collections.Generic;
using System.Diagnostics;
using GameNetcodeStuff;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.AI;

namespace CodeRebirth.src.Content.Unlockables;
public class ShockwaveGalAI : NetworkBehaviour
{
    public Animator animator = null!;
    public NavMeshAgent agent = null!;
    public InteractTrigger trigger = null!;

    public enum State {
        Inactive = 0,
        Active = 1,
        Idle = 2,
        FollowingPlayer = 3,
        DeliveringItems = 4,
        Dancing = 5,
        AttackMode = 6,

    }

    public enum Emotion {
        Inactive = 0,
        Happy = 1,
        LoveyDovey = 2,
        Normal = 3,
        AttackMode = 4,
    }

    private bool flying = false;
    private Emotion galEmotion = Emotion.Inactive;
    private State galState = State.Inactive;
    private PlayerControllerB? ownerPlayer;
    private bool holdingItems = false;
    private List<GrabbableObject> itemsHeld = new();
    private List<Transform> itemsHeldTransforms = new();

    public void Start() {
        Plugin.Logger.LogInfo("Hi creator");
    }

    public void Update() {


        if (!IsHost) return;
        
        HostSideUpdate();
    }

    private void HostSideUpdate()
    {
        switch (galState)
        {
            case State.Inactive:
                DoInactive();
                break;
            case State.Active:
                DoActive();
                break;
            case State.Idle:
                DoIdle();
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

    private void DoIdle()
    {
    }

    private void DoFollowingPlayer()
    {
    }

    private void DoDeliveringItems()
    {
    }

    private void DoDancing()
    {
    }

    private void DoAttackMode()
    {
    }

    private void HandleStateAnimationSpeedChanges(State state, Emotion emotion, float speed)
    {
        agent.speed = speed;
        SwitchStateOrEmotionServerRpc((int)state, (int)emotion);
        switch (state)
        {
            case State.Idle:
                break;
            case State.FollowingPlayer:
                break;
            case State.DeliveringItems:
                break;
            case State.Dancing:
                break;
            case State.AttackMode:
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

    private void SwitchStateOrEmotion(int state, int emotion)
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
                case State.Idle:
                    HandleStateIdleChange();
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
                case Emotion.Inactive:
                    HandleEmotionInactiveChange();
                    break;
                case Emotion.Happy:
                    HandleEmotionHappyChange();
                    break;
                case Emotion.LoveyDovey:
                    HandleEmotionLoveyDoveyChange();
                    break;
                case Emotion.Normal:
                    HandleEmotionNormalChange();
                    break;
                case Emotion.AttackMode:
                    HandleEmotionAttackModeChange();
                    break;
            }
        }
    }

    #region State Changes
    private void HandleStateInactiveChange()
    {
        flying = false;
        holdingItems = false;

        for (int i = 0; i < itemsHeldTransforms.Count; i++) {
            HandleDroppingItem(itemsHeld[i]);
        }

        itemsHeld.Clear();
        ownerPlayer = null;
        galState = State.Inactive;
    }

    private void HandleStateActiveChange()
    {
        galState = State.Active;
    }

    private void HandleStateIdleChange()
    {
        galState = State.Idle;
    }

    private void HandleStateFollowingPlayerChange()
    {
        galState = State.FollowingPlayer;
    }

    private void HandleStateDeliveringItemsChange()
    {
        galState = State.DeliveringItems;
    }

    private void HandleStateDancingChange()
    {
        galState = State.Dancing;
    }

    private void HandleStateAttackModeChange()
    {
        galState = State.AttackMode;
    }
    #endregion

    #region Emotion Changes
    private void HandleEmotionInactiveChange()
    {
        galEmotion = Emotion.Inactive;
    }

    private void HandleEmotionHappyChange()
    {
        galEmotion = Emotion.Happy;
    }

    private void HandleEmotionLoveyDoveyChange()
    {
        galEmotion = Emotion.LoveyDovey;
    }

    private void HandleEmotionNormalChange()
    {
        galEmotion = Emotion.Normal;
    }

    private void HandleEmotionAttackModeChange()
    {
        galEmotion = Emotion.AttackMode;
    }
    #endregion

    private void HandleGrabbingItem(GrabbableObject item, Transform heldTransform)
    {
        holdingItems = true;
        item.grabbable = false;
        item.isHeldByEnemy = true;
        item.parentObject = heldTransform;
        itemsHeld.Add(item);
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
    }
}