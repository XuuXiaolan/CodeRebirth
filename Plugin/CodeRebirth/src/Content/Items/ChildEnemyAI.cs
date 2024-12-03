using System;
using System.Collections;
using System.Collections.Generic;
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
    [NonSerialized] public float[] friendShipMeterGoals = new float[3] { 0f, 20f, 50f };
    [NonSerialized] public Dictionary<PlayerControllerB, float> friendShipMeterPlayers = new Dictionary<PlayerControllerB, float>();
    public bool CloseToSpawn => Vector3.Distance(transform.position, parentEevee.spawnTransform.position) < 1.5f;
    private bool isSitting = false;
    private float sittingTimer = 20f;
    private float observationCheckTimer = 2f;
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
    private static readonly int RunSpeedFloat = Animator.StringToHash("RunSpeed"); // float
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
        HandleStateAnimationSpeedChangesServerRpc((int)State.Grabbed);
    }

    public override void DiscardItem()
    {
        base.DiscardItem();
        // HandleStateAnimationSpeedChangesServerRpc((int)State.Scared);
    }

    public override void Update()
    {
        base.Update();

        if (nearbyPlayer != null && (nearbyPlayer.isPlayerDead || !nearbyPlayer.isPlayerControlled || (nearbyPlayer.isInHangarShipRoom && playerHeldBy != nearbyPlayer)))
        {
            nearbyPlayer = null;
        }
        if (playerHeldBy != null && isHeld)
        {
            friendShipMeterPlayers[playerHeldBy] += Time.deltaTime * 2f;
        }

        if (!IsServer) return;
        DoHostSideUpdate();
    }

    private float GetCurrentMultiplierBoost()
    {
        if (isSitting) return 0f;
        return 0.5f;
    }

    private void DoHostSideUpdate()
    {
        if (agent.enabled) animator.SetFloat(RunSpeedFloat, agent.velocity.magnitude / 2);
        smartAgentNavigator.AdjustSpeedBasedOnDistance(GetCurrentMultiplierBoost());
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

        if (!isSitting && sittingTimer <= 0f)
        {
            animator.SetBool(isSittingAnimation, true);
            sittingTimer = UnityEngine.Random.Range(20, 30);
            int random = UnityEngine.Random.Range(0, 2);
            if (random == 0)
            {
                networkAnimator.SetTrigger(doSitGesture1Animation);
            }
            else
            {
                networkAnimator.SetTrigger(doSitGesture2Animation);
            }
        }
    }

    private void DoSpawning()
    {

    }

    private void DoWandering()
    {
        if (!isSitting) sittingTimer -= Time.deltaTime;
        observationCheckTimer -= Time.deltaTime;

        if (observationCheckTimer > 0) return;
        observationCheckTimer = 2f;
        PlayerControllerB? playerToFollow = DetectNearbyPlayer();
        if (playerToFollow != null)
        {
            SetTargetPlayerServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerToFollow));
            HandleStateAnimationSpeedChanges(State.FollowingPlayer);
        }

        foreach (var enemy in RoundManager.Instance.SpawnedEnemies)
        {
            if (LineOfSightAvailable(enemy.transform))
            {
                HandleStateAnimationSpeedChanges(State.Scared);
            }
        }
    }

    private void DoFollowingPlayer()
    {
        if (nearbyPlayer == null)
        {
            SetTargetPlayerServerRpc(-1);
            HandleStateAnimationSpeedChanges(State.Wandering);
            return;
        }
        if (Vector3.Distance(transform.position, parentEevee.spawnTransform.position) <= 25f)
        {
            smartAgentNavigator.DoPathingToDestination(nearbyPlayer.transform.position, nearbyPlayer.isInsideFactory, true, nearbyPlayer);
        }
        else
        {
            smartAgentNavigator.DoPathingToDestination(parentEevee.spawnTransform.position, parentEevee.isSpawnInside, false, null);
        }
    }

    private void DoScared()
    {
        smartAgentNavigator.DoPathingToDestination(parentEevee.spawnTransform.position, parentEevee.isSpawnInside, false, null);
        if (Vector3.Distance(transform.position, parentEevee.spawnTransform.position) < 5f)
        {
            
        }
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
        if (nearbyPlayer == null) return;
        if (friendShipMeterPlayers[nearbyPlayer] >= friendShipMeterGoals[1])
        {
            SwitchFriendShipStateServerRpc((int)FriendState.Friendly);
        }
    }

    private void DoFriendlyFriendShip()
    {
        if (nearbyPlayer == null) return;
        if (friendShipMeterPlayers[nearbyPlayer] >= friendShipMeterGoals[2])
        {
            SwitchFriendShipStateServerRpc((int)FriendState.Tamed);
        }
    }

    private void DoTamedFriendShip()
    {
    }

    private PlayerControllerB? DetectNearbyPlayer()
    {
        if (isHeld && playerHeldBy != null)
        {
            nearbyPlayer = playerHeldBy;
            return nearbyPlayer;
        }
        foreach (var player in StartOfRound.Instance.allPlayerScripts)
        {
            if (LineOfSightAvailable(player.transform))
            {
                nearbyPlayer = player;
                return nearbyPlayer;
            }
        }
        return null;
    }

    private bool LineOfSightAvailable(Transform PlayerOrEnemy)
    {
        float distanceToPlayer = Vector3.Distance(PlayerOrEnemy.position, this.transform.position);
        if (distanceToPlayer >= rangeOfDetection) return false;
        if (Physics.Raycast(PlayerOrEnemy.position, (PlayerOrEnemy.position - this.transform.position).normalized, distanceToPlayer, StartOfRound.Instance.collidersAndRoomMaskAndDefault | LayerMask.GetMask("Terrain", "InteractableObject"), QueryTriggerInteraction.Ignore))
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
                SetAnimatorBools(isWalking: false, isRunning: false, isScared: false, isDancing: false, isGrabbed: false);
                break;
            case State.Wandering:
                SetAnimatorBools(isWalking: true, isRunning: false, isScared: false, isDancing: false, isGrabbed: false);
                break;
            case State.FollowingPlayer:
                SetAnimatorBools(isWalking: true, isRunning: true, isScared: false, isDancing: false, isGrabbed: false);
                break;
            case State.Scared:
                SetAnimatorBools(isWalking: animator.GetBool(isWalkingAnimation), isRunning: animator.GetBool(isRunningAnimation), isScared: true, isDancing: false, isGrabbed: animator.GetBool(childGrabbedAnimation));
                break;
            case State.Dancing:
                SetAnimatorBools(isWalking: false, isRunning: false, isScared: false, isDancing: true, isGrabbed: false);
                break;
            case State.Grabbed:
                SetAnimatorBools(isWalking: false, isRunning: false, isScared: animator.GetBool(isScaredAnimation), isDancing: false, isGrabbed: true);
                break;
        }
    }

    private void SetAnimatorBools(bool isWalking, bool isRunning, bool isScared, bool isDancing, bool isGrabbed)
    {
        animator.SetBool(isWalkingAnimation, isWalking);
        animator.SetBool(isRunningAnimation, isRunning);
        animator.SetBool(isScaredAnimation, isScared);
        animator.SetBool(isDancingAnimation, isDancing);
        animator.SetBool(childGrabbedAnimation, isGrabbed);
        animator.SetBool(isSittingAnimation, false);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SwitchFriendShipStateServerRpc(int state)
    {
        SwitchFriendShipStateClientRpc(state);
    }

    [ClientRpc]
    private void SwitchFriendShipStateClientRpc(int state)
    {
        SwitchFriendShipState(state);
    }

    private void SwitchFriendShipState(int state)
    {
        friendEeveeState = (FriendState)state;
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
        if (state == -1) return;
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

    [ServerRpc(RequireOwnership = false)]
    private void SetTargetPlayerServerRpc(int playerToFollowIndex)
    {
        SetTargetPlayerClientRpc(playerToFollowIndex);
    }

    [ClientRpc]
    private void SetTargetPlayerClientRpc(int playerToFollowIndex)
    {
        if (playerToFollowIndex == -1)
        {
            nearbyPlayer = null;
            return;
        }
        nearbyPlayer = StartOfRound.Instance.allPlayerScripts[playerToFollowIndex];
    }
}