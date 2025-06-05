using System;
using System.Collections;
using System.Collections.Generic;
using CodeRebirth.src.Util;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Enemies;

public class Pandora : CodeRebirthEnemyAI
{
    public AnimationClip deathAnimation = null!;
    public AudioClip deathSound = null!;

    // Fields for fixation escape counting
    private int fixationAttemptCount = 0;
    private const int maxFixationAttempts = 4; // After 3 escapes, Pandora loses interest
    private float currentTimerCooldown => GetTimerCooldown();
    private float currentTeleportTimer = 15f;
    private float showdownTimer = 0f;
    private int _playerDefaultSensitivity = 10;
    private float _retriggerTimer = 3f;

    private static readonly int RunSpeedFloat = Animator.StringToHash("RunSpeedFloat"); // Float
    private static readonly int ArmsRaisedAnimation = Animator.StringToHash("armsRaised"); // Trigger
    private static readonly int IsDeadAnimation = Animator.StringToHash("IsDead"); // Bool
    private static readonly int RandomIdleAnimation = Animator.StringToHash("randomIdle"); // Trigger

    public enum State
    {
        LookingForPlayer,
        ShowdownWithPlayer,
        Death,
    }

    public float GetTimerCooldown()
    {
        float timerMultiplier = Mathf.Clamp01(1 - TimeOfDay.Instance.normalizedTimeOfDay);
        timerMultiplier = 0.2f;
        return timerMultiplier * 55f + 5f;
    }

    public override void Start()
    {
        base.Start();
        _playerDefaultSensitivity = IngamePlayerSettings.Instance.settings.lookSensitivity;
        // Start Pandora's roaming/search routine.
        smartAgentNavigator.StartSearchRoutine(20);
        SwitchToBehaviourStateOnLocalClient((int)State.LookingForPlayer);
        // todo: Pandora is meant to spawn on snowy moons and only after 11am.
    }

    public override void Update()
    {
        base.Update();
        if (currentBehaviourStateIndex == (int)State.ShowdownWithPlayer && targetPlayer != null)
        {
            showdownTimer += Time.deltaTime;
            if (targetPlayer == GameNetworkManager.Instance.localPlayerController)
            {
                ForceTurnTowardsTarget();
                CodeRebirthUtils.Instance.StaticCloseEyeVolume.weight = Mathf.Clamp01(showdownTimer / 15f * 0.6f);
            }
            Plugin.ExtendedLogging($"Showdown timer: {showdownTimer}");

            // If the player breaks eye contact (escapes) after at least 0.5 seconds…
            if (showdownTimer >= 1f && !PlayerLookingAtEnemy(targetPlayer))
            {
                fixationAttemptCount++;
                _retriggerTimer = 2f;
                creatureAnimator.SetBool(ArmsRaisedAnimation, false);
                if (fixationAttemptCount >= maxFixationAttempts)
                {
                    // After too many escapes, Pandora loses interest and returns to roaming.
                    fixationAttemptCount = 0;
                    showdownTimer = 0f;
                    StartCoroutine(TemporarilyCripplePlayer(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, targetPlayer), false));
                    targetPlayer = null;
                    SwitchToBehaviourStateOnLocalClient((int)State.LookingForPlayer);
                    StartCoroutine(TeleportAndResetSearchRoutine());
                    return;
                }
                else
                {
                    // Reset the timer and try to re-fixate by teleporting closer.
                    showdownTimer = 0f;
                    StartCoroutine(TemporarilyCripplePlayer(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, targetPlayer), false));
                    SwitchToBehaviourStateOnLocalClient((int)State.LookingForPlayer);
                    TeleportNearbyTargetPlayer(targetPlayer);
                    return;
                }
            }

            // If the player stares for too long (6 seconds), kill them.
            if (showdownTimer >= 15f)
            {
                fixationAttemptCount = 0;
                showdownTimer = 0f;
                SwitchToBehaviourStateOnLocalClient((int)State.LookingForPlayer);
                if (IsServer) StartCoroutine(TeleportAndResetSearchRoutine());
                if (targetPlayer.IsOwner)
                {
                    CodeRebirthUtils.Instance.StaticCloseEyeVolume.weight = 0f;
                    targetPlayer.KillPlayer(targetPlayer.velocityLastFrame, true, CauseOfDeath.Unknown, 0, default);
                }
                targetPlayer = null;
            }
        }
    }

    public void LateUpdate()
    {
        if (targetPlayer == null || isEnemyDead || currentBehaviourStateIndex != (int)State.ShowdownWithPlayer) return;

        Vector3 direction = targetPlayer.gameplayCamera.transform.position - transform.position;
        direction.y = 0;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, 5 * Time.deltaTime);
    }

    public override void DoAIInterval()
    {
        base.DoAIInterval();
        if (StartOfRound.Instance.allPlayersDead || isEnemyDead) return;

        creatureAnimator.SetFloat(RunSpeedFloat, agent.velocity.magnitude / 2);
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
        _retriggerTimer -= AIIntervalTime;
        if (_retriggerTimer <= 0)
        {
            PlayerControllerB? closestPlayer = null;
            float closestDistance = 30f;
            foreach (var player in StartOfRound.Instance.allPlayerScripts)
            {
                if (player.isPlayerDead || !player.isPlayerControlled || !player.isInsideFactory) continue;
                float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
                if (distanceToPlayer < closestDistance)
                {
                    closestPlayer = player;
                    closestDistance = distanceToPlayer;
                }
            }

            if (closestPlayer != null)
            {
                Plugin.ExtendedLogging($"Closest player is {closestPlayer.playerUsername} at {closestDistance} meters.");
                // If within fixation range (20 meters) try to check for direct gaze.
                if (closestDistance <= 5f || (closestDistance <= 20f && PlayerLookingAtEnemy(closestPlayer)))
                {
                    // Direct line-of-sight within 15m triggers fixation.
                    int playerIndex = Array.IndexOf(StartOfRound.Instance.allPlayerScripts, closestPlayer);
                    SetTargetServerRpc(playerIndex);
                    agent.velocity = Vector3.zero;
                    fixationAttemptCount = 0; // Reset count for new target.
                    creatureAnimator.SetBool(ArmsRaisedAnimation, true);
                    // *** Animation placeholder ***
                    // Trigger "Raise Arms" animation and glowing radiance here.
                    TemporarilyCripplePlayerServerRpc(playerIndex, true);
                    smartAgentNavigator.StopSearchRoutine();
                    smartAgentNavigator.StopAgent();
                    SwitchToBehaviourServerRpc((int)State.ShowdownWithPlayer);
                    Plugin.ExtendedLogging($"Fixated on {closestPlayer.playerUsername}!");
                    return;
                }
                smartAgentNavigator.StopSearchRoutine();
                smartAgentNavigator.DoPathingToDestination(closestPlayer.transform.position);
            }
        }

        // Countdown for teleport if no player is within detection range.
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
        // In targetPlayer state, Pandora and her target remain stationary.
        // The Update() method handles the countdown, fixation, and eventual outcome.
    }

    private void DoDeath()
    {
        // In the Death state no actions are required.
        // The respawn logic is handled in KillEnemy().
    }
    #endregion

    public override void KillEnemy(bool destroy = false)
    {
        base.KillEnemy(destroy);
        StartCoroutine(DoDeathStuffAfterDeath());
        smartAgentNavigator.StopSearchRoutine();
        agent.velocity = Vector3.zero;
        agent.path = null;
    }

    private IEnumerator DoDeathStuffAfterDeath()
    {
        if (IsServer)
        {
            creatureAnimator.SetBool(IsDeadAnimation, true);
        }
        yield return new WaitForSeconds(deathAnimation.length);
        EnableEnemyMesh(false, true);
        SwitchToBehaviourStateOnLocalClient((int)State.Death);
        creatureVoice.PlayOneShot(deathSound);

        if (!IsServer) yield break;
        // When Pandora is killed, she respawns behind the nearest corner.
        // *** Audio placeholder: Play "Perhaps your eyes deceived you?" here.
        RoundManager.Instance.SpawnEnemyGameObject(RoundManager.Instance.GetRandomNavMeshPositionInRadiusSpherical(this.transform.position, 100, default), -1, -1, this.enemyType);
        // yield return new WaitUntil(() => !creatureVoice.isPlaying);
        this.NetworkObject.Despawn();
    }

    public override void HitEnemy(int force = 1, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
        if (isEnemyDead)
            return;
        enemyHP -= force;
        if (IsOwner && enemyHP <= 0)
        {
            KillEnemyOnOwnerClient();
        }
    }

    private bool PlayerLookingAtEnemy(PlayerControllerB player)
    {
        float dot = Vector3.Dot(player.gameplayCamera.transform.forward, (eye.position - player.gameplayCamera.transform.position).normalized);
        Plugin.ExtendedLogging($"Vector Dot: {dot}");
        if (dot <= 0.5f) return false;
        if (Physics.Linecast(player.gameplayCamera.transform.position, eye.position, out RaycastHit hit, StartOfRound.Instance.collidersAndRoomMaskAndDefault | LayerMask.GetMask("InteractableObject"), QueryTriggerInteraction.Ignore))
        {
            Plugin.ExtendedLogging($"Linecast hit {hit.collider.name}");
            return false;
        }
        Plugin.ExtendedLogging($"Linecast did not hit anything");
        return true;
    }

    private IEnumerator TeleportAndResetSearchRoutine()
    {
        if (!IsServer)
            yield break;

        smartAgentNavigator.StopSearchRoutine();
        Vector3 randomPosition = this.transform.position;
        // RoundManager.Instance.insideAINodes[UnityEngine.Random.Range(0, RoundManager.Instance.insideAINodes.Length)].transform.position;
        bool foundSuitablePosition = false;
        while (!foundSuitablePosition)
        {
            bool suitable = true;
            foreach (var player in StartOfRound.Instance.allPlayerScripts)
            {
                if (player.isPlayerDead || !player.isPlayerControlled || !player.isInsideFactory) continue;
                if (Vector3.Distance(randomPosition, player.transform.position) < 8f)
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
        smartAgentNavigator.StartSearchRoutine(20);
    }

    private void TeleportNearbyTargetPlayer(PlayerControllerB? targettedPlayer)
    {
        if (!IsServer)
            return;

        if (targettedPlayer == null)
        {
            Plugin.Logger.LogError($"TeleportNearbyTargetPlayer: targetPlayer is null");
            return;
        }

        agent.Warp(RoundManager.Instance.GetRandomNavMeshPositionInRadius(this.transform.position, 14f, default));
        smartAgentNavigator.DoPathingToDestination(targettedPlayer.transform.position);
    }

    [ServerRpc(RequireOwnership = false)]
    private void TemporarilyCripplePlayerServerRpc(int playerToCripple, bool cripple)
    {
        TemporarilyCripplePlayerClientRpc(playerToCripple, cripple);
    }

    [ClientRpc]
    private void TemporarilyCripplePlayerClientRpc(int playerToCrippleIndex, bool cripple)
    {
        StartCoroutine(TemporarilyCripplePlayer(playerToCrippleIndex, cripple));
    }

    private IEnumerator TemporarilyCripplePlayer(int playerToCrippleIndex, bool cripple)
    {
        PlayerControllerB playerToCripple = StartOfRound.Instance.allPlayerScripts[playerToCrippleIndex];
        playerToCripple.disableMoveInput = cripple;
        playerToCripple.inAnimationWithEnemy = null;
        inSpecialAnimationWithPlayer = null;
        playerToCripple.inSpecialInteractAnimation = false;
        playerToCripple.shockingTarget = null;
        SetMouseSensitivity(playerToCripple, _playerDefaultSensitivity);
        if (!cripple)
        {
            StartCoroutine(ResetVolumeWeightTo0(playerToCripple));
            yield break;
        }
        SetMouseSensitivity(playerToCripple, 1);
        playerToCripple.inAnimationWithEnemy = this;
        inSpecialAnimationWithPlayer = playerToCripple;
        playerToCripple.inSpecialInteractAnimation = true;
        playerToCripple.shockingTarget = eye;
        playerToCripple.inShockingMinigame = true;
        yield return new WaitForSeconds(0.5f);
        playerToCripple.inShockingMinigame = false;
    }

    private IEnumerator ResetVolumeWeightTo0(PlayerControllerB targetPlayer)
    {
        if (targetPlayer != GameNetworkManager.Instance.localPlayerController) yield break;
        while (CodeRebirthUtils.Instance.StaticCloseEyeVolume.weight > 0f)
        {
            yield return null;
            CodeRebirthUtils.Instance.StaticCloseEyeVolume.weight = Mathf.MoveTowards(CodeRebirthUtils.Instance.StaticCloseEyeVolume.weight, 0f, Time.deltaTime * 2f);
        }
    }

    private void TeleportPlayerAway(PlayerControllerB player)
    {
        if (!player.IsOwner) return;
        // Teleport the player to a designated location outside the main entrance.
        Vector3 destination = RoundManager.Instance.outsideAINodes[UnityEngine.Random.Range(0, RoundManager.Instance.outsideAINodes.Length)].transform.position;
        // With a small chance, teleport the player onto the ship instead.
        if (UnityEngine.Random.value < 0.05f)
        {
            destination = StartOfRound.Instance.shipDoorNode.position;
        }
        player.transform.position = destination;
        // *** Audio placeholder: Play "Have a nice journey" and optionally show subtitle with player's name.
    }

    private IEnumerator SetMouseSensitivity(PlayerControllerB player, int sensitivity)
    {
        if (!player.IsOwner)
            yield break;

        if (sensitivity == IngamePlayerSettings.Instance.settings.lookSensitivity)
            yield break;

        float duration = 0.5f;
        while (duration > 0f)
        {
            duration -= Time.deltaTime;
            IngamePlayerSettings.Instance.settings.lookSensitivity = (int)Mathf.Lerp(_playerDefaultSensitivity, sensitivity, duration * 2f);
            player.lookSensitivity = IngamePlayerSettings.Instance.settings.lookSensitivity;
            yield return null;
        }
        IngamePlayerSettings.Instance.settings.lookSensitivity = sensitivity;
        player.lookSensitivity = IngamePlayerSettings.Instance.settings.lookSensitivity;
    }

    public void ForceTurnTowardsTarget()
    {
        if (targetPlayer.inSpecialInteractAnimation && targetPlayer.shockingTarget != null)
        {
            targetPlayer.targetScreenPos = targetPlayer.turnCompassCamera.WorldToViewportPoint(targetPlayer.shockingTarget.position);
            targetPlayer.shockMinigamePullPosition = targetPlayer.targetScreenPos.x - 0.5f;
            float num = Mathf.Clamp(Time.deltaTime, 0f, 0.1f);
            if (targetPlayer.targetScreenPos.x > 0.52f)
            {
                targetPlayer.turnCompass.Rotate(Vector3.up * 400 * num * Mathf.Abs(targetPlayer.shockMinigamePullPosition));
            }
            else if (targetPlayer.targetScreenPos.x < 0.48f)
            {
                targetPlayer.turnCompass.Rotate(Vector3.up * -400 * num * Mathf.Abs(targetPlayer.shockMinigamePullPosition));
            }

            targetPlayer.targetScreenPos = targetPlayer.gameplayCamera.WorldToViewportPoint(targetPlayer.shockingTarget.position + Vector3.up * 0.35f);
            if (targetPlayer.targetScreenPos.y > 0.55f)
            {
                targetPlayer.cameraUp = Mathf.Clamp(Mathf.Lerp(targetPlayer.cameraUp, targetPlayer.cameraUp - 25f, 25f * num * Mathf.Abs(targetPlayer.targetScreenPos.y - 0.5f)), -89f, 89f);
            }
            else if (targetPlayer.targetScreenPos.y < 0.45f)
            {
                targetPlayer.cameraUp = Mathf.Clamp(Mathf.Lerp(targetPlayer.cameraUp, targetPlayer.cameraUp + 25f, 25f * num * Mathf.Abs(targetPlayer.targetScreenPos.y - 0.5f)), -89f, 89f);
            }
            targetPlayer.gameplayCamera.transform.localEulerAngles = new Vector3(targetPlayer.cameraUp, targetPlayer.gameplayCamera.transform.localEulerAngles.y, targetPlayer.gameplayCamera.transform.localEulerAngles.z);
            Vector3 zero = Vector3.zero;
            zero.y = targetPlayer.turnCompass.eulerAngles.y;
            targetPlayer.thisPlayerBody.rotation = Quaternion.Lerp(targetPlayer.thisPlayerBody.rotation, Quaternion.Euler(zero), Time.deltaTime * 20f * (1f - Mathf.Abs(targetPlayer.shockMinigamePullPosition)));
        }
    }
}