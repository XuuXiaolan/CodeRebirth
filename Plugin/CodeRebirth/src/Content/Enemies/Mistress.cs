using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.Content.Unlockables;
using CodeRebirth.src.Util;
using CodeRebirth.src.Util.Extensions;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace CodeRebirth.src.Content.Enemies;
public class Mistress : CodeRebirthEnemyAI
{

    public float teleportCooldown = 20f;
    public float killCooldown = 10f;
    public Transform HeadTransform = null!;

    private List<PlayerControllerB> previousTargetPlayers = new();
    private int playerPreviousSensitivity = 0;
    private System.Random mistressRandom = new();
    private float teleporterTimer = 20f;
    private float timeSpentInState = 0f;
    private float killTimer = 0f;
    private PlayerControllerB playerToKill;
    private static readonly int DoVanishAnimation = Animator.StringToHash("doVanish"); // Trigger
    private static readonly int IdleIntAnimation = Animator.StringToHash("idleInt"); // Int
    public enum State
    {
        Spawning,
        Stalking,
        Attack,
        Execution,
    }

    public override void Start()
    {
        base.Start();
        skinnedMeshRenderers[0].enabled = false;
        mistressRandom = new System.Random(StartOfRound.Instance.randomMapSeed + 69);
        StartCoroutine(ResetMistressToStalking());
    }

    public override void Update()
    {
        base.Update();
        if (currentBehaviourStateIndex != (int)State.Attack) return;

        killTimer += Time.deltaTime;
        PlayerControllerB localPlayer = GameNetworkManager.Instance.localPlayerController;
        if (localPlayer == targetPlayer)
        {
            CodeRebirthUtils.Instance.CloseEyeVolume.weight = Mathf.Clamp01(killTimer/killCooldown);
            ForceTurnTowardsTarget(localPlayer);
        }
        
        if (killTimer >= killCooldown)
        {
            playerToKill = targetPlayer;
            StartCoroutine(ResetVolumeWeightTo0(playerToKill));
            StartCoroutine(InitiateKillingSequence(playerToKill));
            SwitchToBehaviourStateOnLocalClient((int)State.Execution);
            return;
        }
    }

    public void LateUpdate()
    {
        if (targetPlayer == null) return;

        Vector3 direction = targetPlayer.gameplayCamera.transform.position - transform.position;
        direction.y = 0;
        
        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, 5 * Time.deltaTime);

        if (currentBehaviourStateIndex != (int)State.Attack) return;
    }

    #region State Machine
    public override void DoAIInterval()
    {
        base.DoAIInterval();
        if (StartOfRound.Instance.allPlayersDead || isEnemyDead) return;

        switch (currentBehaviourStateIndex)
        {
            case (int)State.Spawning:
                break;
            case (int)State.Stalking:
                DoStalking();
                break;
            case (int)State.Attack:
                DoAttack();
                break;
            case (int)State.Execution:
                DoExecution();
                break;
        }
    }

    private void DoStalking()
    {
        if (teleporterTimer > 500) return;
        teleporterTimer -= AIIntervalTime;
        timeSpentInState += AIIntervalTime;
        if (teleporterTimer <= 0)
        {
            StartCoroutine(TeleportRoutine());
            teleporterTimer = 99999;
            return;
        }

        float distance = Vector3.Distance(transform.position, targetPlayer.transform.position);
        if (distance > 20)
        {
            return;
        }

        bool LookedAt = PlayerLookingAtEnemy(distance);
        Plugin.ExtendedLogging($"LookedAt in stalking phase: {LookedAt}");
        if (LookedAt)
        {
            if (timeSpentInState > 15f)
            {
                timeSpentInState = 0f;
                TemporarilyCripplePlayerServerRpc(true);
                SwitchToBehaviourServerRpc((int)State.Attack);
            }
            else
            {
                teleporterTimer = 0f;
            }
            return;
        }
    }

    private void DoAttack()
    {
        float distance = Vector3.Distance(HeadTransform.position, targetPlayer.transform.position);
        if (distance > 20)
        {
            StartCoroutine(ResetMistressToStalking());
            return;
        }

        bool LookedAt = PlayerLookingAtEnemy(distance);
        Plugin.ExtendedLogging($"LookedAt in attack phase: {LookedAt}");
        if (LookedAt) return;
        StartCoroutine(ResetMistressToStalking());
    }

    private void DoExecution()
    {
        Plugin.ExtendedLogging($"Executing player {playerToKill}!");
        // Begin the execution.
        // Once player dies, goes back to spawning phase.
    }
    #endregion

    #region Misc Functions

    private IEnumerator InitiateKillingSequence(PlayerControllerB playerToExecute)
    {
        Physics.Raycast(Vector3.zero, Vector3.down, out RaycastHit hit, 10, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore);
        GameObject GuillotineGO = GameObject.Instantiate(EnemyHandler.Instance.Mistress.GuillotinePrefab, hit.point, Quaternion.Euler(-90, 0, 0), RoundManager.Instance.mapPropsContainer.transform);
        var Guillotine = GuillotineGO.GetComponent<Guillotine>();
        Guillotine.playerToKill = playerToExecute;
        yield return new WaitUntil(() => Guillotine.sequenceFinished); // Have the player stick to guillotine script.
        if (playerToExecute == GameNetworkManager.Instance.localPlayerController)
        {
            IngamePlayerSettings.Instance.settings.lookSensitivity = playerPreviousSensitivity;
        }
        StartCoroutine(ResetMistressToStalking());
        yield return new WaitForSeconds(30f);
        Destroy(GuillotineGO);
        // Find Valid spot to spawn guillotine.
        // parent player to the bone on a late update sequence, start teh guillotine animation, then kill em.
        // If we killed them, then spawn the talking head with player.
        // If not, then oh well.
    }

    private IEnumerator ResetMistressToStalking()
    {
        if (!IsServer) yield break;
        creatureNetworkAnimator.SetTrigger(DoVanishAnimation);
        yield return new WaitForSeconds(0.5f);
        TemporarilyCripplePlayerServerRpc(false);
        SwitchToBehaviourServerRpc((int)State.Spawning);
        previousTargetPlayers.Add(targetPlayer);
        killTimer = 0f;
        teleporterTimer = 0f;
        targetPlayer = null;
        yield return new WaitForSeconds(20f);
        SwitchToBehaviourServerRpc((int)State.Stalking);
        PickATargetPlayer();
    }

    public IEnumerator TeleportRoutine()
    {
        yield return new WaitForSeconds(0.5f);
        skinnedMeshRenderers[0].enabled = false;
        if (GameNetworkManager.Instance.localPlayerController == targetPlayer)
        {
            StartCoroutine(UnHideMistress());
        }
        if (!IsServer) yield break;
        teleporterTimer = mistressRandom.NextFloat(teleportCooldown - 5, teleportCooldown + 5);
        Vector3 teleportPoint = ChooseNewTeleportPoint();
        if (teleportPoint == Vector3.zero)
        {
            Plugin.Logger.LogError("Could not find a good teleport position");
            teleporterTimer = 0f;
            yield break;
        }
        Plugin.ExtendedLogging($"Teleporting to: {teleportPoint}");
        creatureAnimator.SetInteger(IdleIntAnimation, mistressRandom.Next(0, 3));
        agent.Warp(teleportPoint);
    }

    private bool PlayerLookingAtEnemy(float distance)
    {
        if (!skinnedMeshRenderers[0].enabled) return false;
        float dot = Vector3.Dot(targetPlayer.gameplayCamera.transform.forward, (HeadTransform.position - targetPlayer.gameplayCamera.transform.position).normalized);
        Plugin.ExtendedLogging($"Vector Dot: {dot}");
        if (dot <= 0.35f) return false;
        if (Physics.Raycast(targetPlayer.gameplayCamera.transform.position, (HeadTransform.position - targetPlayer.gameplayCamera.transform.position).normalized, distance, StartOfRound.Instance.collidersAndRoomMask | LayerMask.GetMask("InteractableObject"), QueryTriggerInteraction.Ignore)) return false;
        return true;
    }

    private void PickATargetPlayer()
    {
        Dictionary<PlayerControllerB, int> playersWithPriorityDict = new();
        foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
        {
            if (player == null || player.isPlayerDead || !player.isPlayerControlled) continue;
            playersWithPriorityDict.Add(player, 0);

            if (previousTargetPlayers.Contains(player))
            {
                playersWithPriorityDict[player] -= 2;
            } // Decrease priority if was already a previous target.

            if (player.isInsideFactory)
            {
                playersWithPriorityDict[player] += 1;
            } // Increase priority if currently inside.

            if (player.isPlayerAlone)
            {
                playersWithPriorityDict[player] += 1;
            } // Increase priority if currently alone.
        }

        if (playersWithPriorityDict.Count == 0)
        {
            Plugin.Logger.LogError("Something went wrong with mistress target selection. No players found. Aborting.");
            SwitchToBehaviourServerRpc((int)State.Spawning);
            return;
        }

        foreach (var gal in GalAI.Instances)
        {
            if (gal == null || gal.ownerPlayer == null) continue;
            playersWithPriorityDict[gal.ownerPlayer] += 1;
        } // Increase priority for each gal a player owns.
        
        IEnumerable<PlayerControllerB> orderedPlayerList = playersWithPriorityDict.OrderByDescending(kvp => kvp.Value).Select(kvp => kvp.Key);

        SetTargetServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, orderedPlayerList.First()));
    }

    private Vector3 ChooseNewTeleportPoint()
    {
        const float MIN_DISTANCE = 10f;
        const float MAX_DISTANCE = 30f;
        
        // Try a few times to find a valid point
        for (int i = 0; i < 10; i++)
        {
            Vector3 randomDirection = new Vector3(mistressRandom.NextFloat(-1f, 1f), 0f, mistressRandom.NextFloat(-1f, 1f));
            if (randomDirection.sqrMagnitude < 0.001f)
            {
                continue;
            }
            randomDirection.Normalize();

            float distance = mistressRandom.NextFloat(MIN_DISTANCE, MAX_DISTANCE);
            Vector3 candidatePos = targetPlayer.transform.position + randomDirection * distance;
            
            candidatePos.y += mistressRandom.NextFloat(-3f, 3f);

            if (NavMesh.SamplePosition(candidatePos, out NavMeshHit hit, 10f, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }
        
        return Vector3.zero;
    }

    public void ForceTurnTowardsTarget(PlayerControllerB player)
    {
        // ----- Horizontal (Yaw) Adjustment -----
        // Convert target world position to viewport coordinates using the turn compass camera.
        player.targetScreenPos = player.turnCompassCamera.WorldToViewportPoint(HeadTransform.position);
        player.shockMinigamePullPosition = player.targetScreenPos.x - 0.5f;
        float dt = Mathf.Clamp(Time.deltaTime, 0f, 0.1f);

        // If the target is to the right of center (beyond 0.54), rotate left.
        if (player.targetScreenPos.x > 0.54f)
        {
            player.turnCompass.Rotate(Vector3.up * 2000f * dt * Mathf.Abs(player.shockMinigamePullPosition));
            player.playerBodyAnimator.SetBool("PullingCameraRight", false);
            player.playerBodyAnimator.SetBool("PullingCameraLeft", true);
        }
        // If the target is to the left of center (below 0.46), rotate right.
        else if (player.targetScreenPos.x < 0.46f)
        {
            player.turnCompass.Rotate(Vector3.up * -2000f * dt * Mathf.Abs(player.shockMinigamePullPosition));
            player.playerBodyAnimator.SetBool("PullingCameraLeft", false);
            player.playerBodyAnimator.SetBool("PullingCameraRight", true);
        }
        else
        {
            // In the dead zone, no horizontal pulling animation.
            player.playerBodyAnimator.SetBool("PullingCameraLeft", false);
            player.playerBodyAnimator.SetBool("PullingCameraRight", false);
        }

        // ----- Vertical (Pitch) Adjustment -----
        // Offset the target position slightly upward (if needed).
        Vector3 targetOffsetPos = HeadTransform.position + Vector3.up * 0.35f;
        player.targetScreenPos = player.gameplayCamera.WorldToViewportPoint(targetOffsetPos);

        // Instead of applying a fixed offset, calculate the desired pitch.
        // Determine the normalized direction from the camera to the target.
        Vector3 directionToTarget = (targetOffsetPos - player.gameplayCamera.transform.position).normalized;
        // Calculate the desired pitch (in degrees) using Atan2.
        float desiredPitch = Mathf.Atan2(directionToTarget.y, new Vector2(directionToTarget.x, directionToTarget.z).magnitude) * Mathf.Rad2Deg;

        // Smoothly interpolate between the current pitch and the desired pitch.
        // Adjust verticalRotationSpeed as needed (here set to 100 for a strong, yet smooth adjustment).
        float verticalRotationSpeed = 10;
        player.cameraUp = Mathf.LerpAngle(player.cameraUp, desiredPitch, verticalRotationSpeed * dt);

        // Clamp the pitch so that it never exceeds reasonable limits.
        player.cameraUp = Mathf.Clamp(player.cameraUp, -89f, 89f);

        // Apply the new pitch to the gameplay camera's local rotation while preserving its Y and Z angles.
        Vector3 currentEuler = player.gameplayCamera.transform.localEulerAngles;
        player.gameplayCamera.transform.localEulerAngles = new Vector3(player.cameraUp, currentEuler.y, currentEuler.z);

        // ----- Player Body Rotation -----
        // Use the turnCompass's Y rotation to smoothly rotate the player's body.
        Vector3 targetEuler = Vector3.zero;
        targetEuler.y = player.turnCompass.eulerAngles.y;
        player.thisPlayerBody.rotation = Quaternion.Lerp(
            player.thisPlayerBody.rotation,
            Quaternion.Euler(targetEuler),
            Time.deltaTime * 20f * (1f - Mathf.Abs(player.shockMinigamePullPosition))
        );
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

    private IEnumerator UnHideMistress()
    {
        yield return new WaitForSeconds(1);
        skinnedMeshRenderers[0].enabled = true;
    }
    #endregion

    #region RPC's

    [ServerRpc(RequireOwnership = false)]
    private void TemporarilyCripplePlayerServerRpc(bool cripple)
    {
        TemporarilyCripplePlayerClientRpc(cripple);
    }

    [ClientRpc]
    private void TemporarilyCripplePlayerClientRpc(bool cripple)
    {
        if (targetPlayer == null) return;
        if (cripple)
        {
            Plugin.ExtendedLogging($"Sensitivity: {IngamePlayerSettings.Instance.settings.lookSensitivity}");
            playerPreviousSensitivity = IngamePlayerSettings.Instance.settings.lookSensitivity;
            IngamePlayerSettings.Instance.settings.lookSensitivity = 1;
            AIIntervalTime = 0.05f;
            targetPlayer.inAnimationWithEnemy = this;
            inSpecialAnimationWithPlayer = targetPlayer;
        }
        else
        {
            skinnedMeshRenderers[0].enabled = false;
            Plugin.ExtendedLogging($"Sensitivity: {IngamePlayerSettings.Instance.settings.lookSensitivity}");
            IngamePlayerSettings.Instance.settings.lookSensitivity = playerPreviousSensitivity;
            AIIntervalTime = 0.5f;
            targetPlayer.inAnimationWithEnemy = null;
            StartCoroutine(ResetVolumeWeightTo0(targetPlayer));
        }
        targetPlayer.disableMoveInput = cripple;
    }
    #endregion
}