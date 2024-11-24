using System;
using System.Collections;
using CodeRebirth.src.Content.Enemies;
using CodeRebirth.src.MiscScripts;
using GameNetcodeStuff;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.AI;

namespace CodeRebirth.src.Content.Items;
[RequireComponent(typeof(SmartAgentNavigator))]
public class ChildEnemyAI : GrabbableObject
{
    public NavMeshAgent agent = null!;
    public Animator animator = null!;
    public NetworkAnimator networkAnimator = null!;
    public SmartAgentNavigator smartAgentNavigator = null!;
    public float rangeOfDetection = 20f;

    [NonSerialized] public ParentEnemyAI parentEevee;
    [NonSerialized] public int health = 4;
    [NonSerialized] public bool mommyAlive = true;
    [NonSerialized] public float friendShipMeter = 0f;
    [NonSerialized] public float[] friendShipMeterGoals = new float[3] { 0f, 20f, 50f };
    public bool CloseToSpawn => Vector3.Distance(transform.position, parentEevee.spawnTransform.position) < 1.5f;
    private PlayerControllerB? nearbyPlayer = null;
    private static readonly int isChildDeadAnimation = Animator.StringToHash("isChildDead"); // bool
    private static readonly int childGrabbedAnimation = Animator.StringToHash("childGrabbed"); // bool
    private static readonly int isWalkingAnimation = Animator.StringToHash("isWalking"); // bool
    private static readonly int isGoofyAnimation = Animator.StringToHash("isGoofy"); // bool
    private static readonly int isRunningAnimation = Animator.StringToHash("isRunning"); // bool
    private static readonly int isScaredAnimation = Animator.StringToHash("isScared"); // bool
    private static readonly int isSittingAnimation = Animator.StringToHash("isSitting"); // bool
    private static readonly int isDancingAnimation = Animator.StringToHash("isDancing"); // bool
    private static readonly int doIdleGestureAnimation = Animator.StringToHash("doIdleGesture"); // trigger
    private static readonly int doSitGesture1Animation = Animator.StringToHash("doSitGesture1"); // trigger
    private static readonly int doSitGesture2Animation = Animator.StringToHash("doSitGesture2"); // trigger
    private State eeveeState = State.Spawning;
    private FriendState friendEeveeState = FriendState.Neutral;

    public enum FriendState
    {
        Neutral,
        Friendly,
        Tamed,
    }

    public enum State
    {
        Spawning,
        Wandering,
        FollowingPlayer,
        Scared,
        Dancing,
        Grabbed,
    }

    public override void Start()
    {
        base.Start();
        smartAgentNavigator.OnEnterOrExitElevator.AddListener(OnEnterOrExitElevator);
        smartAgentNavigator.OnUseEntranceTeleport.AddListener(OnUseEntranceTeleport);
        smartAgentNavigator.SetAllValues(parentEevee.isOutside);

        if (!IsServer) return;
        HandleStateAnimationSpeedChanges(State.Spawning);
    }

    public override void EquipItem()
    {
        base.EquipItem();
    }

    public override void DiscardItem()
    {
        base.DiscardItem();
    }

    public override void Update()
    {
        base.Update();

        if (!IsServer) return;
        DoHostSideUpdate();
    }

    private void DoHostSideUpdate()
    {
        switch (eeveeState)
        {
            case State.Spawning:
                DoSpawning();
                break;
            case State.Wandering:
                DoWandering();
                break;
            case State.FollowingPlayer:
                DoFollowingPlayer();
                break;
            case State.Scared:
                DoScared();
                break;
            case State.Dancing:
                DoDancing();
                break;
        }
        HandleFriendShipMeter();
    }

    private void DoSpawning()
    {

    }

    private void DoWandering()
    {
        
    }

    private void DoFollowingPlayer()
    {
        
    }

    private void DoScared()
    {

    }

    private void DoDancing()
    {

    }

    private void HandleFriendShipMeter()
    {
        switch (friendEeveeState)
        {
            case FriendState.Neutral:
                DoNeutralFriendShip();
                break;
            case FriendState.Friendly:
                DoFriendlyFriendShip();
                break;
            case FriendState.Tamed:
                DoTamedFriendShip();
                break;
        }
    }

    private void DoNeutralFriendShip()
    {
    }

    private void DoFriendlyFriendShip()
    {
    }

    private void DoTamedFriendShip()
    {
    }

    private void DetectNearbyPlayer()
    {
        foreach (var player in StartOfRound.Instance.allPlayerScripts)
        {
            if (LineOfSightAvailable(player))
            {
                nearbyPlayer = player;
                return;
            }
        }
    }

    private bool LineOfSightAvailable(PlayerControllerB player)
    {
        float distanceToPlayer = Vector3.Distance(player.transform.position, this.transform.position);
        if (distanceToPlayer >= rangeOfDetection) return false;
        if (Physics.Raycast(player.transform.position, (player.transform.position - this.transform.position).normalized, distanceToPlayer, StartOfRound.Instance.collidersAndRoomMaskAndDefault | LayerMask.GetMask("Terrain", "InteractableObject"), QueryTriggerInteraction.Ignore))
        {
            return false;
        }
        return true;
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
            case State.Spawning:
                SetAnimatorBools(isWalking: false, isRunning: false, isScared: false, isSitting: true, isDancing: false, isGrabbed: false);
                break;
            case State.Wandering:
                SetAnimatorBools(isWalking: true, isRunning: false, isScared: false, isSitting: false, isDancing: false, isGrabbed: false);
                break;
            case State.FollowingPlayer:
                SetAnimatorBools(isWalking: true, isRunning: true, isScared: false, isSitting: false, isDancing: false, isGrabbed: false);
                break;
            case State.Scared:
                SetAnimatorBools(isWalking: animator.GetBool(isWalkingAnimation), isRunning: animator.GetBool(isRunningAnimation), isScared: true, isSitting: false, isDancing: false, isGrabbed: animator.GetBool(childGrabbedAnimation));
                break;
            case State.Dancing:
                SetAnimatorBools(isWalking: false, isRunning: false, isScared: false, isSitting: false, isDancing: true, isGrabbed: false);
                break;
            case State.Grabbed:
                SetAnimatorBools(isWalking: false, isRunning: false, isScared: animator.GetBool(isScaredAnimation), isSitting: false, isDancing: false, isGrabbed: true);
                break;
        }
    }

    private void SetAnimatorBools(bool isWalking, bool isRunning, bool isScared, bool isSitting, bool isDancing, bool isGrabbed)
    {
        animator.SetBool(isWalkingAnimation, isWalking);
        animator.SetBool(isRunningAnimation, isRunning);
        animator.SetBool(isScaredAnimation, isScared);
        animator.SetBool(isSittingAnimation, isSitting);
        animator.SetBool(isDancingAnimation, isDancing);
        animator.SetBool(childGrabbedAnimation, isGrabbed);
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
                case State.Spawning:
                    HandleStateSpawningChange();
                    break;
                case State.Wandering:
                    HandleStateWanderingChange();
                    break;
                case State.FollowingPlayer:
                    HandleStateFollowingPlayerChange();
                    break;
                case State.Scared:
                    HandleStateScaredChange();
                    break;
                case State.Dancing:
                    HandleStateDancingChange();
                    break;
                case State.Grabbed:
                    HandleStateGrabbedChange();
                    break;
            }
            eeveeState = stateToSwitchTo;
        }
    }

    private IEnumerator SwitchToStateAfterDelay(State state, float delay)
    {
        yield return new WaitForSeconds(delay);
        HandleStateAnimationSpeedChanges(state);
    }

    #region State Changes
    private void HandleStateSpawningChange()
    {
        StartCoroutine(SwitchToStateAfterDelay(State.Wandering, 2f));
    }

    private void HandleStateWanderingChange()
    {
    }

    private void HandleStateFollowingPlayerChange()
    {
    }

    private void HandleStateScaredChange()
    {
    }

    private void HandleStateDancingChange()
    {
    }

    private void HandleStateGrabbedChange()
    {
    }
    #endregion

    public void OnUseEntranceTeleport(bool setOutside)
    {
    }

    public void OnEnterOrExitElevator(bool enteredElevator)
    {
    }

    public bool Hit(int force, Vector3 hitDirection, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        health--;
        // logic for hitting
        if (health <= 0)
        {
            animator.SetBool(isChildDeadAnimation, true);
            smartAgentNavigator.OnEnterOrExitElevator.RemoveListener(OnEnterOrExitElevator);
            smartAgentNavigator.OnUseEntranceTeleport.RemoveListener(OnUseEntranceTeleport);
            smartAgentNavigator.ResetAllValues();
        }
        return true;
    }
}