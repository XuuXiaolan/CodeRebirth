using System;
using System.Collections;
using System.Linq;
using CodeRebirth.src.Util;
using CodeRebirth.src.Util.Extensions;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Enemies;
public class Nancy : CodeRebirthEnemyAI
{
    [Header("Audio")]
    [SerializeField]
    private AudioSource _rollingSource = null!;

    [Header("Voicelines")]
    [SerializeField]
    private AudioClip[] _detectInjuredPlayerVoicelines = [];

    [SerializeField]
    private AudioClip[] _specialDetectInjuredPlayerVoicelines = [];

    [SerializeField]
    private ulong[] _baldPlayerSteamIds = [];

    [SerializeField]
    private AudioClip[] _healFailVoicelines = [];

    [SerializeField]
    private AudioClip[] _healSuccessVoiceline = [];

    [Header("Sound")]
    [SerializeField]
    private AudioSource _healDuringSource = null!;

    private float checkLengthTimer = 2f;
    private float checkTimer = 0f;
    private Vector3 playersLastPosition = Vector3.zero;
    private float healTimer = 1f;
    private float failTimer = 1f;

    private static readonly int HealModeAnimation = Animator.StringToHash("HealMode"); // Bool
    private static readonly int HealingPlayerAnimation = Animator.StringToHash("HealingPlayer"); // Bool
    private static readonly int FailHealAnimation = Animator.StringToHash("FailHeal"); // Trigger
    private static readonly int RunSpeedFloat = Animator.StringToHash("RunSpeed"); // Float

    public enum NancyState
    {
        Wandering,
        ChasingHealTarget,
        HealingTarget,
    }

    public override void Start()
    {
        base.Start();
        smartAgentNavigator.StartSearchRoutine(30f);
    }

    #region StateMachine

    public override void Update()
    {
        base.Update();
        if (isEnemyDead)
            return;

        _idleTimer -= Time.deltaTime;
        if (_idleTimer <= 0f)
        {
            PlayVoiceline(_idleAudioClips.audioClips);
            _idleTimer = enemyRandom.NextFloat(_idleAudioClips.minTime, _idleAudioClips.maxTime);
        }

        _rollingSource.volume = creatureAnimator.GetFloat(RunSpeedFloat) > 0.01 ? 1 : 0;

        checkTimer -= Time.deltaTime;
        if (targetPlayer != null || currentBehaviourStateIndex != (int)NancyState.Wandering || checkTimer > 0)
            return;

        checkTimer = checkLengthTimer;
        PlayerControllerB localPlayer = GameNetworkManager.Instance.localPlayerController;
        if (localPlayer.isPlayerDead || !localPlayer.isPlayerControlled)
            return;

        if (localPlayer.health >= 100)
            return;

        if ((localPlayer.isInsideFactory && isOutside) || (!isOutside && !localPlayer.isInsideFactory))
            return;

        float distance = Vector3.Distance(transform.position, localPlayer.transform.position);
        if (distance > 30)
            return;

        float pathDistance = smartAgentNavigator.CanPathToPoint(this.transform.position, localPlayer.transform.position); // todo: switch this to the async version
        if (pathDistance == 0 || pathDistance > 20f)
            return;

        SetTargetServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, localPlayer));
        DoBoolAnimationServerRpc(HealModeAnimation, true);
        SwitchToBehaviourServerRpc((int)NancyState.ChasingHealTarget);
    }

    public override void DoAIInterval()
    {
        base.DoAIInterval();
        if (StartOfRound.Instance.allPlayersDead || isEnemyDead) return;

        failTimer -= AIIntervalTime;
        creatureAnimator.SetFloat(RunSpeedFloat, agent.velocity.magnitude);
        if (targetPlayer != null && targetPlayer.isPlayerDead)
        {
            creatureAnimator.SetBool(HealModeAnimation, false);
            creatureAnimator.SetBool(HealingPlayerAnimation, false);
            SetTargetServerRpc(-1);
            smartAgentNavigator.StartSearchRoutine(30f);
            SwitchToBehaviourServerRpc((int)NancyState.Wandering);
            return;
        }

        switch (currentBehaviourStateIndex)
        {
            case (int)NancyState.Wandering:
                DoWandering();
                break;
            case (int)NancyState.ChasingHealTarget:
                DoChasingHealTarget();
                break;
            case (int)NancyState.HealingTarget:
                DoHealingTarget();
                break;
        }
    }

    public void DoWandering()
    {

    }

    public void DoChasingHealTarget()
    {
        float distanceToPlayer = Vector3.Distance(this.transform.position, targetPlayer.transform.position);
        if (distanceToPlayer > 35)
        {
            creatureAnimator.SetBool(HealModeAnimation, false);
            SetTargetServerRpc(-1);
            smartAgentNavigator.StartSearchRoutine(30f);
            SwitchToBehaviourServerRpc((int)NancyState.Wandering);
            return;
        }
        smartAgentNavigator.DoPathingToDestination(targetPlayer.transform.position);

        if (Vector3.Distance(this.transform.position, targetPlayer.transform.position) <= agent.stoppingDistance)
        {
            CrippleTargetServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, targetPlayer));
            StartHealingPlayerServerRpc();
            SwitchToBehaviourServerRpc((int)NancyState.HealingTarget);
            playersLastPosition = targetPlayer.transform.position;
            agent.velocity = Vector3.zero;
            creatureAnimator.SetBool(HealingPlayerAnimation, true);
        }
    }

    public void DoHealingTarget()
    {
        healTimer -= AIIntervalTime;
        float distanceToPlayer = Vector3.Distance(this.transform.position, targetPlayer.transform.position);
        int currentHealth = targetPlayer.health;
        if (currentHealth >= 100)
        {
            creatureAnimator.SetBool(HealModeAnimation, false);
            creatureAnimator.SetBool(HealingPlayerAnimation, false);
            // HealPlayerSuccessServerRpc();
            SetTargetServerRpc(-1);
            smartAgentNavigator.StartSearchRoutine(30f);
            SwitchToBehaviourServerRpc((int)NancyState.Wandering);
            return;
        }

        float distanceFromLastAIInterval = Vector3.Distance(targetPlayer.transform.position, playersLastPosition);
        playersLastPosition = targetPlayer.transform.position;
        Plugin.ExtendedLogging($"Distance from last AI interval: {distanceFromLastAIInterval}");
        if (distanceFromLastAIInterval > 0.25f && failTimer <= 0)
        {
            failTimer = 1f;
            creatureNetworkAnimator.SetTrigger(FailHealAnimation);
            if (targetPlayer.IsOwner)
            {
                targetPlayer.DamagePlayer(20, true, true, CauseOfDeath.Stabbing, 0, false, default);
            }
            else
            {
                CodeRebirthUtils.Instance.DamagePlayerOnOwnerServerRpc(targetPlayer, 20, true, (int)CauseOfDeath.Stabbing, 0, false, default);
            }
        }
        else if (currentHealth < 100 && healTimer <= 0)
        {
            healTimer = 1f;
            if (targetPlayer.IsOwner)
            {
                targetPlayer.DamagePlayer(-10, false, true, CauseOfDeath.Unknown, 0, false, default);
            }
            else
            {
                CodeRebirthUtils.Instance.DamagePlayerOnOwnerServerRpc(targetPlayer, -10, false, (int)CauseOfDeath.Unknown, 0, false, default);
            }
        }

        if (distanceToPlayer > 35)
        {
            creatureAnimator.SetBool(HealModeAnimation, false);
            creatureAnimator.SetBool(HealingPlayerAnimation, false);
            SetTargetServerRpc(-1);
            smartAgentNavigator.StartSearchRoutine(30f);
            SwitchToBehaviourServerRpc((int)NancyState.Wandering);
            return;
        }
        else if (distanceToPlayer > 2.5f)
        {
            creatureAnimator.SetBool(HealingPlayerAnimation, false);
            SwitchToBehaviourServerRpc((int)NancyState.ChasingHealTarget);
            return;
        }
    }

    #endregion

    #region Misc Functions
    [ServerRpc(RequireOwnership = false)]
    private void StartHealingPlayerServerRpc()
    {
        StartHealingPlayerClientRpc();
    }

    [ClientRpc]
    private void StartHealingPlayerClientRpc()
    {
        _healDuringSource.Play();
    }

    [ServerRpc(RequireOwnership = false)]
    private void HealPlayerSuccessServerRpc()
    {
        HealPlayerSuccessClientRpc();
    }

    [ClientRpc]
    private void HealPlayerSuccessClientRpc()
    {
        // PlayVoiceline(_healSuccessVoiceline);
    }

    [ServerRpc(RequireOwnership = false)]
    private void DoBoolAnimationServerRpc(int animationHash, bool value)
    {
        creatureAnimator.SetBool(animationHash, value);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CrippleTargetServerRpc(int playerIndex)
    {
        CrippleTargetClientRpc(playerIndex);
    }

    [ClientRpc]
    private void CrippleTargetClientRpc(int playerIndex)
    {
        PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[playerIndex];
        player.disableMoveInput = true;
        StartCoroutine(ReEnableMoveInput(player));
    }

    private IEnumerator ReEnableMoveInput(PlayerControllerB player)
    {
        yield return new WaitForSeconds(0.15f);
        player.disableMoveInput = false;
    }

    private void PlayVoiceline(AudioClip[] voicelines)
    {
        creatureVoice.Stop();
        AudioClip voiceLine = voicelines[enemyRandom.Next(voicelines.Length)];
        creatureVoice.clip = voiceLine;
        creatureVoice.Play();
    }
    #endregion

    #region Animation Events

    public void FailHealAnimationEvent()
    {
        PlayVoiceline(_healFailVoicelines);
    }

    public void DetectPlayerAnimationEvent()
    {
        if (targetPlayer != null && _baldPlayerSteamIds.Contains(targetPlayer.playerSteamId))
        {
            PlayVoiceline(_specialDetectInjuredPlayerVoicelines);
            return;
        }
        PlayVoiceline(_detectInjuredPlayerVoicelines);
    }

    public void EndHealingAnimationEvent()
    {
        _healDuringSource.Stop();
    }
    #endregion
}