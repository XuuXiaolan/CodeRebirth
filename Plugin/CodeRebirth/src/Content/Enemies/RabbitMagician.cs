using System;
using System.Collections;
using CodeRebirth.src.Content.Enemies;
using CodeRebirth.src.Util;
using GameNetcodeStuff;
using UnityEngine;

public class RabbitMagician : CodeRebirthEnemyAI
{
    [SerializeField]
    private AnimationClip _spawnAnimation = null!;
    [SerializeField]
    private AnimationClip _spottedAnimation = null!;
    [SerializeField]
    private Vector3 _offsetPosition = new Vector3(0.031f, -0.109f, -0.471f);

    private Coroutine? _attachRoutine = null;
    private Transform? _targetPlayerSpine3 = null;

    private static readonly int RunSpeedFloat = Animator.StringToHash("RunSpeed"); // Float
    private static readonly int LatchOnAnimation = Animator.StringToHash("LatchOn"); // Trigger
    private static readonly int SpottedAnimation = Animator.StringToHash("Spotted"); // Trigger

    public enum RabbitMagicianState
    {
        Spawn,
        Idle,
        Attached,
        SwitchingTarget
    }
    // todo: spawn animation, wanders around slowly.
    // todo: when spotted, stops moving, does spot animation, attaches to player back once player looks away.
    // todo: sits on the players back until someone spots the rabbit.
    // todo: if seen, kills current player and transfers to next player.
    // todo: if player dies, goes back to spawning phase.
    public override void Start()
    {
        base.Start();

        foreach (var enemyCollider in this.GetComponentsInChildren<Collider>())
        {
            foreach (var playerCollider in GameNetworkManager.Instance.localPlayerController.GetCRPlayerData().playerColliders)
            {
                Physics.IgnoreCollision(enemyCollider, playerCollider, true);
            }
        }
        SwitchToBehaviourStateOnLocalClient((int)RabbitMagicianState.Spawn);
        if (!IsServer) return;
        StartCoroutine(SwitchToIdle());
    }

    public override void Update()
    {
        base.Update();

        if (currentBehaviourStateIndex != (int)RabbitMagicianState.Attached || _targetPlayerSpine3 == null) return;
        // Rotate the offset vector by the pivot's rotation and add it to the pivot's position.
        Vector3 worldOffset = _targetPlayerSpine3.rotation * _offsetPosition;
        this.transform.position = _targetPlayerSpine3.position + worldOffset;
        this.transform.rotation = _targetPlayerSpine3.rotation;
    }

    #region State Machines
    public override void DoAIInterval()
    {
        base.DoAIInterval();
        switch (currentBehaviourStateIndex)
        {
            case (int)RabbitMagicianState.Spawn:
                DoSpawn();
                break;
            case (int)RabbitMagicianState.Idle:
                DoIdle();
                break;
            case (int)RabbitMagicianState.Attached:
                DoAttached();
                break;
            case (int)RabbitMagicianState.SwitchingTarget:
                DoSwitchingTarget();
                break;
        }
    }

    public void DoSpawn()
    {
        // Do Nothing
    }

    public void DoIdle()
    {
        if (_attachRoutine != null)
            return;
        creatureAnimator.SetFloat(RunSpeedFloat, agent.velocity.magnitude / 3f);
        foreach (var player in StartOfRound.Instance.allPlayerScripts)
        {
            if (player.isPlayerDead || !player.isPlayerControlled) continue;
            Vector3 directionToRabbit = (transform.position - player.gameObject.transform.position).normalized;
            if (Vector3.Dot(player.gameplayCamera.transform.forward, directionToRabbit) < 0.2f) continue;
            if (Physics.Linecast(transform.position, player.transform.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore)) continue;
            smartAgentNavigator.StopSearchRoutine();
            SetTargetServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player));
            SwitchToBehaviourServerRpc((int)RabbitMagicianState.Attached);
            smartAgentNavigator.enabled = false;
            agent.enabled = false;
            _attachRoutine = StartCoroutine(AttachToPlayer(player));
            // do animation
            // attach to a player bone
            return;
        }
    }

    public void DoAttached()
    {

    }

    public void DoSwitchingTarget()
    {

    }
    #endregion

    #region Misc Functions
    private IEnumerator SwitchToIdle()
    {
        yield return new WaitForSeconds(_spawnAnimation.length);
        SwitchToBehaviourServerRpc((int)RabbitMagicianState.Idle);
        smartAgentNavigator.StartSearchRoutine(transform.position, 20);
    }

    public IEnumerator AttachToPlayer(PlayerControllerB playerToAttachTo)
    {
        creatureNetworkAnimator.SetTrigger(SpottedAnimation);
        yield return new WaitForSeconds(_spottedAnimation.length);
        bool lookedAt = true;
        while (lookedAt)
        {
            yield return new WaitForSeconds(0.5f);
            Vector3 directionToRabbit = (transform.position - playerToAttachTo.gameObject.transform.position).normalized;
            if (Vector3.Dot(playerToAttachTo.gameplayCamera.transform.forward, directionToRabbit) < 0.2f)
            {
                lookedAt = false;
                continue;
            }
            if (Physics.Linecast(transform.position, playerToAttachTo.transform.position, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore))
            {
                lookedAt = false;
                continue;
            }
        }

        creatureNetworkAnimator.SetTrigger(LatchOnAnimation);
        _targetPlayerSpine3 = playerToAttachTo.upperSpine;
        this.transform.localScale = new Vector3(0.8915618f, 0.8915618f, 0.8915618f);
        SwitchToBehaviourServerRpc((int)RabbitMagicianState.Attached);
        _attachRoutine = null;
    }
    #endregion
}