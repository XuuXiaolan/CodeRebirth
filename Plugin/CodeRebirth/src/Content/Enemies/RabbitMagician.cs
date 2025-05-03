using System;
using System.Collections;
using CodeRebirth.src.Content.Enemies;
using CodeRebirth.src.Util;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class RabbitMagician : CodeRebirthEnemyAI
{
    [SerializeField]
    private AnimationClip _spawnAnimation = null!;
    [SerializeField]
    private AnimationClip _spottedAnimation = null!;
    [SerializeField]
    private AnimationClip _latchOnAnimation = null!;

    [SerializeField]
    private Vector3 _offsetPosition = new Vector3(0.031f, -0.109f, -0.471f);

    private Coroutine? _killRoutine = null;
    private Coroutine? _attachRoutine = null;
    private Coroutine? _idleRoutine = null;
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

    public override void Start()
    {
        base.Start();
        _offsetPosition = new Vector3(0.031f, -0.109f, -0.471f);
        foreach (var enemyCollider in this.GetComponentsInChildren<Collider>())
        {
            foreach (var playerCollider in GameNetworkManager.Instance.localPlayerController.GetCRPlayerData().playerColliders)
            {
                Physics.IgnoreCollision(enemyCollider, playerCollider, true);
            }
        }
        SwitchToBehaviourStateOnLocalClient((int)RabbitMagicianState.Spawn);
        if (!IsServer) return;
        _idleRoutine = StartCoroutine(SwitchToIdle());
    }

    public void LateUpdate()
    {
        if (currentBehaviourStateIndex != (int)RabbitMagicianState.Attached)
            return;

        if (_attachRoutine != null)
            return;

        if (targetPlayer == null)
            return;

        _targetPlayerSpine3 = targetPlayer.upperSpine;
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
            if (player.isPlayerDead || !player.isPlayerControlled)
                continue;
            if (!PlayerLookingAtEnemy(player, 0.2f))
                continue;
            _attachRoutine = StartCoroutine(AttachToPlayer(player));
            // do animation
            // attach to a player bone
            return;
        }
    }

    public void DoAttached()
    {
        if (targetPlayer == null || targetPlayer.isPlayerDead)
        {
            if (_killRoutine != null)
                return;
            if (_idleRoutine != null)
                return;

            _idleRoutine = StartCoroutine(SwitchToIdle());
            return;
        }
        foreach (var player in StartOfRound.Instance.allPlayerScripts)
        {
            if (player.isPlayerDead || !player.isPlayerControlled)
                continue;
            if (player == targetPlayer)
                continue;
            if (!PlayerLookingAtEnemy(player, 0.2f))
                continue;
            if (Vector3.Dot(player.gameplayCamera.transform.forward, targetPlayer.gameplayCamera.transform.forward) <= 0.6f)
                continue;

            _killRoutine = StartCoroutine(KillPlayerAndSwitchTarget(player));
        }
    }

    public void DoSwitchingTarget()
    {

    }
    #endregion

    #region Misc Functions

    private IEnumerator KillPlayerAndSwitchTarget(PlayerControllerB newTargetPlayer)
    {
        SwitchToBehaviourServerRpc((int)RabbitMagicianState.SwitchingTarget);
        // UnhideRendererClientRpc();
        targetPlayer.DamagePlayerFromOtherClientServerRpc(9999, this.transform.position, Array.IndexOf(StartOfRound.Instance.allPlayerScripts, newTargetPlayer));
        _attachRoutine = StartCoroutine(AttachToPlayer(newTargetPlayer));
        yield return _attachRoutine;
    }

    /*[ClientRpc]
    public void UnhideRendererClientRpc()
    {
        skinnedMeshRenderers[0].enabled = true;
    }*/

    private IEnumerator SwitchToIdle()
    {
        yield return new WaitForSeconds(_spawnAnimation.length);
        this.transform.localScale = Vector3.one;
        smartAgentNavigator.enabled = true;
        agent.enabled = true;
        SwitchToBehaviourServerRpc((int)RabbitMagicianState.Idle);
        smartAgentNavigator.StartSearchRoutine(20);
        _idleRoutine = null;
    }

    public IEnumerator AttachToPlayer(PlayerControllerB playerToAttachTo)
    {
        smartAgentNavigator.StopSearchRoutine();
        SetTargetServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerToAttachTo));
        smartAgentNavigator.enabled = false;
        if (!agent.enabled)
        {
            if (Physics.Raycast(this.transform.position, Vector3.down, out RaycastHit hit, 20, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
            {
                this.transform.position = hit.point;
            }
            yield return null;
        }
        agent.enabled = false;
        creatureNetworkAnimator.SetTrigger(SpottedAnimation);
        yield return new WaitForSeconds(_spottedAnimation.length);
        if (playerToAttachTo == null || playerToAttachTo.isPlayerDead)
        {
            _idleRoutine = StartCoroutine(SwitchToIdle());
            _attachRoutine = null;
            yield break;
        }

        bool lookedAt = true;
        while (lookedAt)
        {
            yield return new WaitForSeconds(0.5f);
            lookedAt = PlayerLookingAtEnemy(playerToAttachTo, 0.2f);
        }

        creatureNetworkAnimator.SetTrigger(LatchOnAnimation);
        this.transform.localScale = new Vector3(0.8915618f, 0.8915618f, 0.8915618f);
        SwitchToBehaviourServerRpc((int)RabbitMagicianState.Attached);
        StartCoroutine(HideForTargetPlayer());
        _attachRoutine = null;
    }

    public IEnumerator HideForTargetPlayer()
    {
        yield return new WaitForSeconds(_latchOnAnimation.length);
        if (targetPlayer == null || targetPlayer.isPlayerDead || currentBehaviourStateIndex != (int)RabbitMagicianState.Attached)
            yield break;

        // HideModelForTargetPlayerServerRpc();
    }

    /*[ServerRpc(RequireOwnership = false)]
    private void HideModelForTargetPlayerServerRpc()
    {
        HideModelForTargetPlayerClientRpc();
    }*/

    /*[ClientRpc]
    private void HideModelForTargetPlayerClientRpc()
    {
        skinnedMeshRenderers[0].enabled = true;
        if (GameNetworkManager.Instance.localPlayerController != targetPlayer)
            return;

        skinnedMeshRenderers[0].enabled = false;
    }*/
    #endregion
}