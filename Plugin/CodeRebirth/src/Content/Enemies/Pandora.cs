using System;
using System.Collections;
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
    private const int maxFixationAttempts = 3; // After 3 escapes, Pandora loses interest
    private float currentTimerCooldown => GetTimerCooldown();
    private float currentTeleportTimer = 0f;
    private float showdownTimer = 0f;
    private bool cantLosePlayer = false;

    private readonly static int RunSpeedFloat = Animator.StringToHash("RunSpeed"); // Float
    private readonly static int RaiseArmsAnimation = Animator.StringToHash("raiseArms"); // Trigger
    private readonly static int IsDeadAnimation = Animator.StringToHash("IsDead"); // Bool
    private readonly static int RandomIdleAnimation = Animator.StringToHash("randomIdle"); // Trigger

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
            showdownTimer += Time.deltaTime;
            if (targetPlayer == GameNetworkManager.Instance.localPlayerController)
            {
                CodeRebirthUtils.Instance.CloseEyeVolume.weight = Mathf.Clamp01(showdownTimer / 6f);
            }
            Plugin.ExtendedLogging($"Showdown timer: {showdownTimer}");

            // If the player breaks eye contact (escapes) after at least 1.5 seconds…
            if (showdownTimer >= 1.5f && !PlayerLookingAtEnemy(false))
            {
                fixationAttemptCount++;
                if (fixationAttemptCount >= maxFixationAttempts)
                {
                    // After too many escapes, Pandora loses interest and returns to roaming.
                    fixationAttemptCount = 0;
                    targetPlayer = null;
                    showdownTimer = 0f;
                    CodeRebirthUtils.Instance.CloseEyeVolume.weight = 0f;
                    TemporarilyCripplePlayerClientRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, targetPlayer), false);
                    SwitchToBehaviourStateOnLocalClient((int)State.LookingForPlayer);
                    if (IsServer) StartCoroutine(TeleportAndResetSearchRoutine());
                    return;
                }
                else
                {
                    // Reset the timer and try to re-fixate by teleporting closer.
                    showdownTimer = 0f;
                    if (IsServer) StartCoroutine(TeleportNearbyTargetPlayer(targetPlayer));
                    return;
                }
            }

            // If the player stares for too long (6 seconds), kill them.
            if (showdownTimer >= 6f)
            {
                fixationAttemptCount = 0;
                showdownTimer = 0f;
                SwitchToBehaviourStateOnLocalClient((int)State.LookingForPlayer);
                if (IsServer) StartCoroutine(TeleportAndResetSearchRoutine());
                if (targetPlayer.IsOwner)
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
                Plugin.ExtendedLogging($"Closest player is {player.playerUsername} at {distanceToPlayer} meters.");
            }
        }

        if (closestPlayer != null)
        {
            // If within fixation range (15 meters) try to check for direct gaze.
            if ((closestDistance <= 15f && PlayerLookingAtEnemy(false)) || closestDistance <= 2.5f)
            {
                // Direct line-of-sight within 15m triggers fixation.
                int playerIndex = Array.IndexOf(StartOfRound.Instance.allPlayerScripts, closestPlayer);
                SetTargetServerRpc(playerIndex);
                agent.velocity = Vector3.zero;
                fixationAttemptCount = 0; // Reset count for new target.
                // *** Animation placeholder ***
                // Trigger "Raise Arms" animation and glowing radiance here.
                TemporarilyCripplePlayerServerRpc(playerIndex, true);
                SwitchToBehaviourServerRpc((int)State.ShowdownWithPlayer);
                Plugin.ExtendedLogging($"Fixated on {closestPlayer.playerUsername}!");
                return;
            }
            smartAgentNavigator.StopSearchRoutine();
            smartAgentNavigator.DoPathingToDestination(closestPlayer.transform.position); 
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

    private bool PlayerLookingAtEnemy(bool triggerWhilstLookingAtGeneralDirection)
    {
        if (cantLosePlayer) return true;
        float dot = Vector3.Dot(targetPlayer.gameplayCamera.transform.forward, (eye.position - targetPlayer.gameplayCamera.transform.position).normalized);
        Plugin.ExtendedLogging($"Vector Dot: {dot}");
        if (dot <= 0.45f) return false;
        if (triggerWhilstLookingAtGeneralDirection) return true;
        if (Physics.Linecast(targetPlayer.gameplayCamera.transform.position, eye.position, out RaycastHit hit, CodeRebirthUtils.Instance.collidersAndRoomAndPlayersAndInteractableMask, QueryTriggerInteraction.Ignore))
        {
            Plugin.ExtendedLogging($"Linecast hit {hit.collider.name}");
            return false;
        }
        Plugin.ExtendedLogging($"Linecast did not hit anything");
        return true;
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

    private IEnumerator TeleportNearbyTargetPlayer(PlayerControllerB? targettedPlayer)
    {
        if (targettedPlayer == null)
        {
            Plugin.Logger.LogError($"TeleportNearbyTargetPlayer: targetPlayer is null");
            yield break;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void TemporarilyCripplePlayerServerRpc(int playerToCripple, bool cripple)
    {
        TemporarilyCripplePlayerClientRpc(playerToCripple, cripple);
    }

    [ClientRpc]
    private void TemporarilyCripplePlayerClientRpc(int playerToCrippleIndex, bool cripple)
    {
        PlayerControllerB playerToCripple = StartOfRound.Instance.allPlayerScripts[playerToCrippleIndex];
        if (cripple)
        {
            /*if (playerToCripple.IsOwner)
            {
                creatureVoice.PlayOneShot(AttackSounds[mistressRandom.Next(AttackSounds.Length)]);
            }*/
            playerToCripple.inAnimationWithEnemy = this;
            inSpecialAnimationWithPlayer = playerToCripple;
            playerToCripple.inSpecialInteractAnimation = true;
            playerToCripple.shockingTarget = eye;
            playerToCripple.inShockingMinigame = true;
            playerToCripple.movementSpeed /= 3;
        }
        else
        {
            /*if (playerToCripple.IsOwner)
            {
                creatureVoice.PlayOneShot(LoseSightSound, 0.75f);
            }*/
            skinnedMeshRenderers[0].enabled = false;
            playerToCripple.inAnimationWithEnemy = null;
            inSpecialAnimationWithPlayer = null;
            playerToCripple.inSpecialInteractAnimation = false;
            playerToCripple.shockingTarget = null;
            playerToCripple.inShockingMinigame = false;
            playerToCripple.movementSpeed *= 3;
            StartCoroutine(ResetVolumeWeightTo0(playerToCripple));
        }
        playerToCripple.disableLookInput = cripple;
    }

    private IEnumerator ResetVolumeWeightTo0(PlayerControllerB targetPlayer)
    {
        if (targetPlayer != GameNetworkManager.Instance.localPlayerController) yield break;
        while (CodeRebirthUtils.Instance.CloseEyeVolume.weight > 0f)
        {
            yield return null;
            CodeRebirthUtils.Instance.CloseEyeVolume.weight = Mathf.MoveTowards(CodeRebirthUtils.Instance.CloseEyeVolume.weight, 0f, Time.deltaTime);
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
}
