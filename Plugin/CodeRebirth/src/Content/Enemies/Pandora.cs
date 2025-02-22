
using System;
using System.Collections;
using CodeRebirth.src.Util;
using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.Content.Enemies;
public class Pandora : CodeRebirthEnemyAI
{

    private float currentTimerCooldown => GetTimerCooldown();
    private float currentTeleportTimer = 0f;
    private float showdownTimer = 0f;
    public enum State
    {
        LookingForPlayer,
        ShowdownWithPlayer,
        Death,
    }

    public float GetTimerCooldown()
    {
        float timerMultiplier = Mathf.Clamp01(1 - TimeOfDay.Instance.normalizedTimeOfDay);
        return timerMultiplier * 55f + 5f;
    }

    public override void Start()
    {
        base.Start();
        smartAgentNavigator.StartSearchRoutine(this.transform.position, 20);
    }

    public override void Update()
    {
        base.Update();

        if (currentBehaviourStateIndex == (int)State.ShowdownWithPlayer && targetPlayer != null)
        {
            targetPlayer.inSpecialInteractAnimation = true;
            targetPlayer.shockingTarget = this.transform;
            targetPlayer.inShockingMinigame = true;

            showdownTimer += Time.deltaTime;
            if (targetPlayer == GameNetworkManager.Instance.localPlayerController)
            {
                CodeRebirthUtils.Instance.CloseEyeVolume.weight = Mathf.Clamp01(showdownTimer / 6f);
            }

            float dot = Vector3.Dot(targetPlayer.gameplayCamera.transform.forward, (this.transform.position - targetPlayer.gameplayCamera.transform.position).normalized);
            if (dot <= 0.45f && showdownTimer >= 1.5f)
            {
                targetPlayer = null;
                showdownTimer = 0f;
                if (IsServer) StartCoroutine(TeleportAndResetSearchRoutine());
                SwitchToBehaviourStateOnLocalClient((int)State.LookingForPlayer);
                return;
            }
            if (showdownTimer >= 6f)
            {
                // Probably kill the player
                showdownTimer = 0f;
                if (IsServer) StartCoroutine(TeleportAndResetSearchRoutine());
                SwitchToBehaviourStateOnLocalClient((int)State.LookingForPlayer);
                if (targetPlayer == GameNetworkManager.Instance.localPlayerController)
                {
                    CodeRebirthUtils.Instance.CloseEyeVolume.weight = 0f;
                    targetPlayer.KillPlayer(targetPlayer.velocityLastFrame, true, CauseOfDeath.Unknown, 0, default);
                }
                targetPlayer = null;
            }
        }
    }

    public override void DoAIInterval()
    {
        base.DoAIInterval();
        if (StartOfRound.Instance.allPlayersDead || isEnemyDead) return;

        switch (currentBehaviourStateIndex)
        {
            case (int)State.LookingForPlayer:
                DoLookingForPlayer();
                break;
            case (int)State.ShowdownWithPlayer:
                DoShowdownWithPlayer();
                break;
            case (int)State.Death:
                DoDeath();
                break;
        }
    }

    #region State Machines
    private void DoLookingForPlayer()
    {
        PlayerControllerB? closestPlayer = null;
        float closestDistance = float.MaxValue;
        foreach (var player in StartOfRound.Instance.allPlayerScripts)
        {
            if (player.isPlayerDead || !player.isPlayerControlled || !player.isInsideFactory) continue;
            float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
            if (distanceToPlayer < closestDistance) closestPlayer = player;
            if (Physics.Raycast(player.gameplayCamera.transform.position, (transform.position - player.gameplayCamera.transform.forward).normalized, out _, 20, StartOfRound.Instance.collidersAndRoomMaskAndDefault | LayerMask.GetMask("InteractableObject"), QueryTriggerInteraction.Ignore)) continue;
            currentTeleportTimer = currentTimerCooldown;
            if (distanceToPlayer <= 15 || distanceToPlayer <= 2.5f)
            {
                SetTargetServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player));
                agent.velocity = Vector3.zero;
                SwitchToBehaviourServerRpc((int)State.ShowdownWithPlayer);
                return;
            }
        }

        if (closestPlayer != null)
        {
            smartAgentNavigator.StopSearchRoutine();
            DoMovingToClosestPlayer(closestPlayer);
        }

        currentTeleportTimer -= AIIntervalTime;
        if (currentTeleportTimer <= 0)
        {
            currentTeleportTimer = currentTimerCooldown;
            StartCoroutine(TeleportAndResetSearchRoutine());
            return;
        }
    }

    private void DoShowdownWithPlayer()
    {
        // Cease Player and Pandora Movement
        // Stare at the player and make em stare at you.
        // Do the zap gun stuff to make the player stare at pandoras
    }

    private void DoDeath()
    {
        // Probably nothing
    }

    #endregion

    public override void KillEnemy(bool destroy = false)
    {
        base.KillEnemy(destroy);
        smartAgentNavigator.StopSearchRoutine();
        EnableEnemyMesh(false, true);
        SwitchToBehaviourStateOnLocalClient((int)State.Death);

        if (!IsServer) return;
        RoundManager.Instance.SpawnEnemyGameObject(RoundManager.Instance.GetRandomNavMeshPositionInRadiusSpherical(this.transform.position, 100, default), -1, -1, this.enemyType);
    }

    public override void HitEnemy(int force = 1, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
        if (isEnemyDead) return;
        enemyHP -= force;
        if (IsOwner && enemyHP <= 0)
        {
            KillEnemyOnOwnerClient();
        }
    }

    private void DoMovingToClosestPlayer(PlayerControllerB player)
    {
        smartAgentNavigator.DoPathingToDestination(player.transform.position);
    }

    private IEnumerator TeleportAndResetSearchRoutine()
    {
        smartAgentNavigator.StopSearchRoutine();
        Vector3 randomPosition = RoundManager.Instance.insideAINodes[UnityEngine.Random.Range(0, RoundManager.Instance.insideAINodes.Length)].transform.position;
        bool foundSuitablePosition = false;
        while (!foundSuitablePosition)
        {
            bool suitable = true;
            foreach (var player in StartOfRound.Instance.allPlayerScripts)
            {
                if (player.isPlayerDead || !player.isPlayerControlled || !player.isInsideFactory) continue;
                if (Vector3.Distance(randomPosition, player.transform.position) < 5)
                {
                    suitable = false;
                    break;
                }
            }
            if (suitable)
            {
                foundSuitablePosition = true;
            }
            else
            {
                randomPosition = RoundManager.Instance.insideAINodes[UnityEngine.Random.Range(0, RoundManager.Instance.insideAINodes.Length)].transform.position;
                yield return null;
            }
        }
        agent.Warp(randomPosition);
        smartAgentNavigator.StartSearchRoutine(this.transform.position, 20);
    }
}