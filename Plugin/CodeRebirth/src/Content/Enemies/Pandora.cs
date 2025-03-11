using System;
using System.Collections;
using CodeRebirth.src.Util;
using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.Content.Enemies;
public class Pandora : CodeRebirthEnemyAI
{
    // Fields for fixation escape counting
    private int fixationAttemptCount = 0;
    private const int maxFixationAttempts = 3; // After 3 escapes, Pandora loses interest

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
        // Start Pandora's roaming/search routine.
        smartAgentNavigator.StartSearchRoutine(this.transform.position, 20);
        // Note: Pandora is meant to spawn on snowy moons and only after 11am.
        // This spawn check should be handled elsewhere in your spawning system.
    }

    public override void Update()
    {
        base.Update();

        if (currentBehaviourStateIndex == (int)State.ShowdownWithPlayer && targetPlayer != null)
        {
            // Freeze movement on both sides while Pandora and the player establish eye contact.
            targetPlayer.inSpecialInteractAnimation = true;
            targetPlayer.shockingTarget = this.transform;
            targetPlayer.inShockingMinigame = true;

            // *** Animation placeholder ***
            // Trigger "Raise Arms" animation and glowing radiance here.

            showdownTimer += Time.deltaTime;
            if (targetPlayer == GameNetworkManager.Instance.localPlayerController)
            {
                CodeRebirthUtils.Instance.CloseEyeVolume.weight = Mathf.Clamp01(showdownTimer / 6f);
            }

            float dot = Vector3.Dot(targetPlayer.gameplayCamera.transform.forward, (this.transform.position - targetPlayer.gameplayCamera.transform.position).normalized);

            // If the player breaks eye contact (escapes) after at least 1.5 seconds…
            if (dot <= 0.45f && showdownTimer >= 1.5f)
            {
                fixationAttemptCount++;
                if (fixationAttemptCount >= maxFixationAttempts)
                {
                    // After too many escapes, Pandora loses interest and returns to roaming.
                    fixationAttemptCount = 0;
                    targetPlayer = null;
                    showdownTimer = 0f;
                    if (IsServer) StartCoroutine(TeleportAndResetSearchRoutine());
                    SwitchToBehaviourStateOnLocalClient((int)State.LookingForPlayer);
                    return;
                }
                else
                {
                    // Reset the timer and try to re-fixate by teleporting closer.
                    showdownTimer = 0f;
                    if (IsServer) StartCoroutine(TeleportAndResetSearchRoutine());
                    return;
                }
            }

            // If the player stares for too long (6 seconds), kill them.
            if (showdownTimer >= 6f)
            {
                fixationAttemptCount = 0;
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
            if (player.isPlayerDead || !player.isPlayerControlled || !player.isInsideFactory) 
                continue;
            float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
            if (distanceToPlayer < closestDistance)
                closestPlayer = player;
            
            // If within fixation range (15 meters) try to check for direct gaze.
            if (distanceToPlayer <= 15f)
            {
                // Use a raycast to check line-of-sight.
                if (Physics.Raycast(player.gameplayCamera.transform.position, (transform.position - player.gameplayCamera.transform.position).normalized, out RaycastHit hit, 20, StartOfRound.Instance.collidersAndRoomMaskAndDefault | LayerMask.GetMask("InteractableObject"), QueryTriggerInteraction.Ignore))
                {
                    // If hit something, assume the player isn’t directly looking at Pandora.
                    currentTeleportTimer = currentTimerCooldown;
                    continue;
                }

                // Direct line-of-sight within 15m triggers fixation.
                SetTargetServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player));
                agent.velocity = Vector3.zero;
                fixationAttemptCount = 0; // Reset count for new target.
                SwitchToBehaviourServerRpc((int)State.ShowdownWithPlayer);
                return;
            }
        }

        if (closestPlayer != null)
        {
            smartAgentNavigator.StopSearchRoutine();
            DoMovingToClosestPlayer(closestPlayer);
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
        // In this state, Pandora and her target remain stationary.
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
        smartAgentNavigator.StopSearchRoutine();
        EnableEnemyMesh(false, true);
        SwitchToBehaviourStateOnLocalClient((int)State.Death);

        if (!IsServer)
            return;
        // When Pandora is killed, she respawns behind the nearest corner.
        // *** Audio placeholder: Play "Perhaps your eyes deceived you?" here.
        RoundManager.Instance.SpawnEnemyGameObject(
            RoundManager.Instance.GetRandomNavMeshPositionInRadiusSpherical(this.transform.position, 100, default),
            -1, -1, this.enemyType);
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
                if (Vector3.Distance(randomPosition, player.transform.position) < 5f)
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

    // AoE effect: During a chase, if any non-target player comes too close,
    // teleport them away (and optionally play a "Have a nice journey" audio).
    private void OnTriggerEnter(Collider other)
    {
        if (currentBehaviourStateIndex == (int)State.ShowdownWithPlayer)
        {
            if (other.TryGetComponent<PlayerControllerB>(out var player))
            {
                // Only affect players that are not the current target.
                if (targetPlayer == null || player != targetPlayer)
                {
                    TeleportPlayerAway(player);
                }
            }
        }
    }

    private void TeleportPlayerAway(PlayerControllerB player)
    {
        // Teleport the player to a designated location outside the main entrance.
        /*Vector3 destination = RoundManager.Instance.GetMainEntrancePosition();
        // With a small chance, teleport the player onto the ship instead.
        if (UnityEngine.Random.value < 0.05f)
        {
            destination = RoundManager.Instance.GetRandomShipPosition();
        }
        player.Teleport(destination);*/
        // *** Audio placeholder: Play "Have a nice journey" and optionally show subtitle with player's name.
    }

    // Optional: If Pandora collides directly with a player (e.g., within 1-2 meters),
    // immediately trigger fixation.
    private void OnCollisionEnter(Collision collision)
    {
        if (currentBehaviourStateIndex == (int)State.ShowdownWithPlayer) return;
        if (collision.gameObject.TryGetComponent(out PlayerControllerB player) && player.IsOwner)
        {
            if (Vector3.Distance(transform.position, player.transform.position) > 2f) return;
            SetTargetServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player));
            agent.velocity = Vector3.zero;
            fixationAttemptCount = 0;
            SwitchToBehaviourServerRpc((int)State.ShowdownWithPlayer);
        }
    }
}
