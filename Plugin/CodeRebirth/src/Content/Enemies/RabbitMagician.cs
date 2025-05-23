using System;
using System.Collections;
using CodeRebirth.src.Content.Enemies;
using CodeRebirth.src.MiscScripts;
using CodeRebirth.src.Util;
using CodeRebirth.src.Util.Extensions;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class RabbitMagician : CodeRebirthEnemyAI
{
    [Header("Audio")]
    [SerializeField]
    private AudioClip _spawnAudioClip = null!;
    [SerializeField]
    private AudioClip _spottedAudioClip = null!;
    [SerializeField]
    private AudioClip[] _fallingAudioClips = null!;
    [SerializeField]
    private AudioClipsWithTime _idleAudioClips = null!;

    [Header("Animation")]
    [SerializeField]
    private AnimationClip _spawnAnimation = null!;
    [SerializeField]
    private AnimationClip _spottedAnimation = null!;
    [Header("Misc")]
    [SerializeField]
    private Collider[] _collidersToDisable = null!;

    [SerializeField]
    private Vector3 _offsetPosition = new Vector3(0.031f, -0.109f, -0.471f);

    private Coroutine? _killRoutine = null;
    private Coroutine? _attachRoutine = null;
    private Coroutine? _idleRoutine = null;
    private Transform? _targetPlayerSpine3 = null;
    private float _idleTimer = 1f;

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
        _idleTimer = enemyRandom.NextFloat(_idleAudioClips.minTime, _idleAudioClips.maxTime);
        // creatureSFX.PlayOneShot(_spawnAudioClip);
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

    public override void Update()
    {
        base.Update();
        _idleTimer -= Time.deltaTime;

        if (_idleTimer > 0)
            return;

        _idleTimer = enemyRandom.NextFloat(_idleAudioClips.minTime, _idleAudioClips.maxTime);
        creatureVoice.PlayOneShot(_idleAudioClips.audioClips[enemyRandom.Next(_idleAudioClips.audioClips.Length)]);
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

            _attachRoutine = StartCoroutine(AttachToPlayer(player, null));
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

            if (Vector3.Dot(player.gameplayCamera.transform.forward, targetPlayer.gameplayCamera.transform.forward) <= 0.45f)
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
        FallSoundServerRpc(newTargetPlayer);
        targetPlayer.DamagePlayerFromOtherClientServerRpc(9999, this.transform.position, Array.IndexOf(StartOfRound.Instance.allPlayerScripts, newTargetPlayer));
        _attachRoutine = StartCoroutine(AttachToPlayer(newTargetPlayer, targetPlayer));
        yield return _attachRoutine;
    }

    [ServerRpc(RequireOwnership = false)]
    private void FallSoundServerRpc(PlayerControllerReference newTargetPlayer)
    {
        FallSoundClientRpc(newTargetPlayer);
    }

    [ClientRpc]
    private void FallSoundClientRpc(PlayerControllerReference newTargetPlayer)
    {
        // add a local player check
        if (newTargetPlayer != GameNetworkManager.Instance.localPlayerController)
            return;

        creatureSFX.PlayOneShot(_fallingAudioClips[enemyRandom.Next(_fallingAudioClips.Length)]);
    }

    private IEnumerator SwitchToIdle()
    {
        yield return new WaitForSeconds(_spawnAnimation.length);
        smartAgentNavigator.enabled = true;
        agent.enabled = true;
        SwitchToBehaviourServerRpc((int)RabbitMagicianState.Idle);
        smartAgentNavigator.StartSearchRoutine(20);
        _idleRoutine = null;
    }

    public IEnumerator AttachToPlayer(PlayerControllerB playerToAttachTo, PlayerControllerB? previouslyAttachedToPlayer)
    {
        smartAgentNavigator.StopSearchRoutine();
        SetTargetServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerToAttachTo));
        smartAgentNavigator.enabled = false;
        if (!agent.enabled)
        {
            if (Physics.Raycast(this.transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 20, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
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

        AttachedPlayerHandleLOSClientRpc(true);
    }

    [ClientRpc]
    private void AttachedPlayerHandleLOSClientRpc(bool firstSpotting)
    {
        foreach (var collider in _collidersToDisable)
        {
            collider.enabled = false;
        }
        if (!firstSpotting && targetPlayer == GameNetworkManager.Instance.localPlayerController)
            creatureSFX.PlayOneShot(_spottedAudioClip);

        if (GameNetworkManager.Instance.localPlayerController != targetPlayer)
            return;

        StartCoroutine(HandleLOS());
    }

    private IEnumerator HandleLOS()
    {
        bool lookedAt = true;
        while (lookedAt)
        {
            yield return new WaitForSeconds(0.25f);
            if (targetPlayer == null || targetPlayer.isPlayerDead)
            {
                BackToIdleServerRpc();
                yield break;
            }
            lookedAt = PlayerLookingAtEnemy(targetPlayer, 0.2f);
        }

        StartAttachedServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void BackToIdleServerRpc()
    {
        _attachRoutine = null;
        _idleRoutine = StartCoroutine(SwitchToIdle());
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartAttachedServerRpc()
    {
        _attachRoutine = null;
        creatureNetworkAnimator.SetTrigger(LatchOnAnimation);
        SwitchToBehaviourServerRpc((int)RabbitMagicianState.Attached);
    }
    #endregion
}