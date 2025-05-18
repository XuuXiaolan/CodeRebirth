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
    private const int maxFixationAttempts = 4; // After 3 escapes, Pandora loses interest
    private float currentTimerCooldown => GetTimerCooldown();
    private float currentTeleportTimer = 0f;
    private float resistanceTimer = 0f;

    private readonly static int RunSpeedFloat = Animator.StringToHash("RunSpeedFloat"); // Float
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
        smartAgentNavigator.StartSearchRoutine(20);
        SwitchToBehaviourStateOnLocalClient((int)State.LookingForPlayer);
        // todo: Pandora is meant to spawn on snowy moons and only after 11am.
    }

    public override void Update()
    {
        base.Update();
        if (currentBehaviourStateIndex == (int)State.ShowdownWithPlayer && targetPlayer != null)
        {
            resistanceTimer += Time.deltaTime;
            if (targetPlayer == GameNetworkManager.Instance.localPlayerController)
            {
                CodeRebirthUtils.Instance.StaticCloseEyeVolume.weight = Mathf.Clamp01(resistanceTimer / 6f * 0.3f);
                HandlePlayerLookingAtEnemy(targetPlayer, 1 - (resistanceTimer / 6));
            }
            Plugin.ExtendedLogging($"Showdown timer: {resistanceTimer}");

            // If the player breaks eye contact (escapes) after at least 1.5 seconds…
            if (resistanceTimer >= 1.5f && !PlayerLookingAtEnemy(targetPlayer, false))
            {
                fixationAttemptCount++;
                if (fixationAttemptCount >= maxFixationAttempts)
                {
                    // After too many escapes, Pandora loses interest and returns to roaming.
                    fixationAttemptCount = 0;
                    resistanceTimer = 0f;
                    CodeRebirthUtils.Instance.StaticCloseEyeVolume.weight = 0f;
                    TemporarilyCripplePlayer(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, targetPlayer), false);
                    targetPlayer = null;
                    SwitchToBehaviourStateOnLocalClient((int)State.LookingForPlayer);
                    if (IsServer) StartCoroutine(TeleportAndResetSearchRoutine());
                    return;
                }
                else
                {
                    // Reset the timer and try to re-fixate by teleporting closer.
                    resistanceTimer = 0f;
                    TemporarilyCripplePlayer(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, targetPlayer), false);
                    SwitchToBehaviourStateOnLocalClient((int)State.LookingForPlayer);
                    if (IsServer) StartCoroutine(TeleportNearbyTargetPlayer(targetPlayer));
                    return;
                }
            }

            // If the player stares for too long (6 seconds), kill them.
            if (resistanceTimer >= 6f)
            {
                fixationAttemptCount = 0;
                resistanceTimer = 0f;
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
            // If within fixation range (15 meters) try to check for direct gaze.
            if (closestDistance <= 2.5f || (closestDistance <= 15f && PlayerLookingAtEnemy(closestPlayer, false)))
            {
                // Direct line-of-sight within 15m triggers fixation.
                int playerIndex = Array.IndexOf(StartOfRound.Instance.allPlayerScripts, closestPlayer);
                SetTargetServerRpc(playerIndex);
                agent.velocity = Vector3.zero;
                fixationAttemptCount = 0; // Reset count for new target.
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

    // Serialized fields to tweak pull behavior
    [SerializeField] private float horizontalPullStrength = 2000f;
    [SerializeField] private float verticalPullStrength = 25f;
    [SerializeField] private Vector2 horizontalDeadzone = new Vector2(0.46f, 0.54f); // x=left, y=right thresholds
    [SerializeField] private Vector2 verticalDeadzone = new Vector2(0.35f, 0.6f);   // x=lower, y=upper thresholds
    [SerializeField] private float cameraLerpSpeed = 20f;
    [SerializeField] private bool forceAlign = true;       // snap camera fully toward target when in deadzone
    [SerializeField] private float forceAlignMinPull = 0.01f; // minimum pull to consider for forcing

    /// <summary>
    /// Adjusts the player's camera and body to look toward the target, with configurable pull strength.
    /// </summary>
    private void HandlePlayerLookingAtEnemy(PlayerControllerB player, float normalizedStrength)
    {
        // HORIZONTAL AIMING ------------------------------------------------
        // Get target position in compass viewport
        player.targetScreenPos = player.turnCompassCamera.WorldToViewportPoint(this.transform.position);
        // Offset from screen center
        player.shockMinigamePullPosition = player.targetScreenPos.x - 0.5f;
        
        float dt = Mathf.Clamp(Time.deltaTime, 0f, 0.1f);
        float absPull = Mathf.Abs(player.shockMinigamePullPosition);
        float horizStrength = horizontalPullStrength * normalizedStrength;

        // If outside horizontal deadzone, rotate compass and play animations
        if (player.targetScreenPos.x > horizontalDeadzone.y || player.targetScreenPos.x < horizontalDeadzone.x)
        {
            float dir = Mathf.Sign(player.shockMinigamePullPosition);
            float rotationAmount = dir * horizStrength * dt * absPull;
            player.turnCompass.Rotate(Vector3.up * rotationAmount);

            // Animations
            player.playerBodyAnimator.SetBool("PullingCameraRight", dir > 0f);
            player.playerBodyAnimator.SetBool("PullingCameraLeft", dir < 0f);
        }
        else
        {
            // Inside deadzone: reset animations
            player.playerBodyAnimator.SetBool("PullingCameraLeft", false);
            player.playerBodyAnimator.SetBool("PullingCameraRight", false);

            // Optionally snap compass to face the target directly
            if (forceAlign && absPull < forceAlignMinPull)
            {
                Vector3 lookDir = (this.transform.position - player.turnCompass.position).normalized;
                Quaternion targetRot = Quaternion.LookRotation(lookDir, Vector3.up);
                player.turnCompass.rotation = Quaternion.Slerp(
                    player.turnCompass.rotation,
                    targetRot,
                    dt * cameraLerpSpeed * normalizedStrength
                );
            }
        }

        // VERTICAL AIMING --------------------------------------------------
        // Slightly offset target upward for vertical aiming
        player.targetScreenPos = player.gameplayCamera.WorldToViewportPoint(this.transform.position + Vector3.up * 0.35f);
        float vertOffset = player.targetScreenPos.y - 0.5f;
        float absVert = Mathf.Abs(vertOffset);
        float vertStrength = verticalPullStrength * normalizedStrength;

        if (player.targetScreenPos.y > verticalDeadzone.y)
        {
            player.cameraUp = Mathf.Clamp(
                Mathf.Lerp(player.cameraUp, player.cameraUp - vertStrength, vertStrength * dt * absVert),
                -89f, 89f
            );
        }
        else if (player.targetScreenPos.y < verticalDeadzone.x)
        {
            player.cameraUp = Mathf.Clamp(
                Mathf.Lerp(player.cameraUp, player.cameraUp + vertStrength, vertStrength * dt * absVert),
                -89f, 89f
            );
        }

        // Apply vertical rotation
        var euler = player.gameplayCamera.transform.localEulerAngles;
        player.gameplayCamera.transform.localEulerAngles = new Vector3(player.cameraUp, euler.y, euler.z);

        // PLAYER BODY ALIGNMENT --------------------------------------------
        Vector3 bodyEuler = Vector3.up * player.turnCompass.eulerAngles.y;
        player.thisPlayerBody.rotation = Quaternion.Lerp(
            player.thisPlayerBody.rotation,
            Quaternion.Euler(bodyEuler),
            dt * cameraLerpSpeed * (1f - absPull)
        );
    }

    private bool PlayerLookingAtEnemy(PlayerControllerB player, bool triggerWhilstLookingAtGeneralDirection)
    {
        float dot = Vector3.Dot(player.gameplayCamera.transform.forward, (eye.position - player.gameplayCamera.transform.position).normalized);
        Plugin.ExtendedLogging($"Vector Dot: {dot}");
        if (dot <= 0.65f) return false;
        if (triggerWhilstLookingAtGeneralDirection) return true;
        if (Physics.Linecast(player.gameplayCamera.transform.position, eye.position, out RaycastHit hit, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
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
        Vector3 randomPosition = this.transform.position;
        // RoundManager.Instance.insideAINodes[UnityEngine.Random.Range(0, RoundManager.Instance.insideAINodes.Length)].transform.position;
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
        smartAgentNavigator.StartSearchRoutine(20);
    }

    private IEnumerator TeleportNearbyTargetPlayer(PlayerControllerB? targettedPlayer)
    {
        if (targettedPlayer == null)
        {
            Plugin.Logger.LogError($"TeleportNearbyTargetPlayer: targetPlayer is null");
            yield break;
        }
        smartAgentNavigator.DoPathingToDestination(targettedPlayer.transform.position);
        yield return new WaitForSeconds(1f);
        if (currentBehaviourStateIndex == (int)State.ShowdownWithPlayer) yield break;
        agent.Warp(RoundManager.Instance.GetRandomNavMeshPositionInRadius(this.transform.position, 9, default));
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
        TemporarilyCripplePlayer(playerToCrippleIndex, cripple);
    }

    private void TemporarilyCripplePlayer(int playerToCrippleIndex, bool cripple)
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
            // playerToCripple.movementSpeed /= 3;
        }
        else
        {
            /*if (playerToCripple.IsOwner)
            {
                creatureVoice.PlayOneShot(LoseSightSound, 0.75f);
            }*/
            playerToCripple.inAnimationWithEnemy = null;
            inSpecialAnimationWithPlayer = null;
            StartCoroutine(ResetVolumeWeightTo0(playerToCripple));
        }
        playerToCripple.disableMoveInput = cripple;
    }

    private IEnumerator ResetVolumeWeightTo0(PlayerControllerB targetPlayer)
    {
        if (targetPlayer != GameNetworkManager.Instance.localPlayerController) yield break;
        while (CodeRebirthUtils.Instance.StaticCloseEyeVolume.weight > 0f)
        {
            yield return null;
            CodeRebirthUtils.Instance.StaticCloseEyeVolume.weight = Mathf.MoveTowards(CodeRebirthUtils.Instance.StaticCloseEyeVolume.weight, 0f, Time.deltaTime);
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
