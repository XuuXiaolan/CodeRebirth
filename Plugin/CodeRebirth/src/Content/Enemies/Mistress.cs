using System;
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
        mistressRandom = new System.Random(StartOfRound.Instance.randomMapSeed + 69);
        currentBehaviourStateIndex = (int)State.Spawning;
        if (IsServer) PickATargetPlayer();
        SwitchToBehaviourStateOnLocalClient((int)State.Stalking);
    }

    public override void Update()
    {
        base.Update();
        if (targetPlayer == GameNetworkManager.Instance.localPlayerController || playerToKill == GameNetworkManager.Instance.localPlayerController)
        {
            skinnedMeshRenderers[0].enabled = true;
        }
        else
        {
            skinnedMeshRenderers[0].enabled = false;
        }
        if (currentBehaviourStateIndex != (int)State.Attack) return;

        killTimer += Time.deltaTime;
        PlayerControllerB localPlayer = GameNetworkManager.Instance.localPlayerController;
        if (localPlayer == targetPlayer)
        {
            CodeRebirthUtils.Instance.CloseEyeVolume.weight = Mathf.Clamp01(killTimer/killCooldown);
        }
        
        if (killTimer >= killCooldown)
        {
            playerToKill = targetPlayer;
            targetPlayer = null;
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
        if (GameNetworkManager.Instance.localPlayerController == targetPlayer) HandleTargetPlayerRotations();
    }

    #region State Machine
    public override void DoAIInterval()
    {
        base.DoAIInterval();
        if (StartOfRound.Instance.allPlayersDead || isEnemyDead) return;

        switch (currentBehaviourStateIndex)
        {
            case (int)State.Spawning:
                DoSpawning();
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

    private void DoSpawning()
    {
    }

    private void DoStalking()
    {
        if (teleporterTimer > 500) return;
        teleporterTimer -= AIIntervalTime;
        timeSpentInState += AIIntervalTime;
        if (teleporterTimer <= 0)
        {
            creatureAnimator.SetInteger(IdleIntAnimation, 0);
            creatureNetworkAnimator.SetTrigger(DoVanishAnimation);
            teleporterTimer = 99999;
            return;
        }

        float distance = Vector3.Distance(transform.position, targetPlayer.transform.position);
        if (distance > 20)
        {
            return;
        }

        bool LookedAt = !Physics.Raycast(transform.position, targetPlayer.gameplayCamera.transform.forward - transform.position, out RaycastHit hit, distance, StartOfRound.Instance.collidersAndRoomMaskAndDefault | LayerMask.GetMask("InteractableObject"), QueryTriggerInteraction.Ignore);
        Plugin.ExtendedLogging($"LookedAt in stalking phase: {LookedAt}");
        if (LookedAt)
        {
            if (timeSpentInState > 15f)
            {
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
        float distance = Vector3.Distance(transform.position, targetPlayer.transform.position);
        if (distance > 20)
        {
            TemporarilyCripplePlayerServerRpc(false);
            SwitchToBehaviourServerRpc((int)State.Spawning);
            PickATargetPlayer();
            return;
        }

        bool LookedAt = !Physics.Raycast(transform.position, targetPlayer.gameplayCamera.transform.forward - transform.position, out RaycastHit hit, distance, StartOfRound.Instance.collidersAndRoomMask | LayerMask.GetMask("InteractableObject"), QueryTriggerInteraction.Ignore);
        Plugin.ExtendedLogging($"LookedAt in attack phase: {LookedAt}");
        if (LookedAt) return;
        TemporarilyCripplePlayerServerRpc(false);
        creatureAnimator.SetInteger(IdleIntAnimation, 0);
        SwitchToBehaviourServerRpc((int)State.Spawning);
        PickATargetPlayer();
    }

    private void DoExecution()
    {
        Plugin.ExtendedLogging($"Executing player {playerToKill}!");
        playerToKill.inSpecialInteractAnimation = false;
        playerToKill.inShockingMinigame = false;
        playerToKill.shockingTarget = null;
        CodeRebirthUtils.Instance.CloseEyeVolume.weight = 0f;
        // Begin the execution.
        // Once player dies, goes back to spawning phase.
    }
    #endregion

    #region Misc Functions

    private void HandleTargetPlayerRotations()
    {
        if (targetPlayer == null)
            return;

        targetPlayer.inSpecialInteractAnimation = true;
        targetPlayer.inShockingMinigame = true;
        targetPlayer.shockingTarget = HeadTransform;
        /*Vector3 directionToTarget = transform.position - camTransform.position;

        // --- Yaw (Horizontal Rotation) Calculation ---
        // Remove the vertical component so we only have the horizontal direction.
        Vector3 horizontalDirection = directionToTarget;
        horizontalDirection.y = 0f;
        if (horizontalDirection.sqrMagnitude < 0.001f)
            return;  // Too close to compute a reliable direction.
        horizontalDirection.Normalize();

        // Determine the desired yaw angle (in degrees) based on the horizontal direction.
        float desiredYaw = Mathf.Atan2(horizontalDirection.x, horizontalDirection.z) * Mathf.Rad2Deg;
        // Use the gameplayCamera's current yaw.
        float currentYaw = camTransform.eulerAngles.y;
        // Compute the shortest angle difference.
        float yawDelta = Mathf.DeltaAngle(currentYaw, desiredYaw);

        // --- Pitch (Vertical Rotation) Calculation ---
        // Find the horizontal distance (ignoring vertical differences).
        float horizontalDistance = new Vector2(directionToTarget.x, directionToTarget.z).magnitude;
        // Calculate the desired pitch angle.
        float desiredPitch = -Mathf.Atan2(directionToTarget.y, horizontalDistance) * Mathf.Rad2Deg;

        // Get the current pitch from the camera's local rotation.
        float currentPitch = camTransform.localEulerAngles.x;
        // Convert the angle from [0, 360] to [-180, 180] for proper delta calculation.
        if (currentPitch > 180f)
            currentPitch -= 360f;
        float pitchDelta = Mathf.DeltaAngle(currentPitch, desiredPitch);

        // --- Simulated Mouse Delta ---
        // Here we combine the yaw and pitch differences into a Vector2,
        // similar to a mouse delta input.
        Vector2 simulatedMouseDelta = new Vector2(yawDelta, pitchDelta);

        // --- Apply Rotations ---
        float turnSpeed = 0.1f;
        // Calculate the rotation amounts.
        float turnAmountYaw = simulatedMouseDelta.x * turnSpeed;
        // For pitch, an additional multiplier is applied (adjust as needed).
        float turnAmountPitch = simulatedMouseDelta.y * turnSpeed * 0.8f;

        // Apply vertical (pitch) rotation.
        // This method is assumed to update the camera's local X rotation.
        Vector2 inputVector = new Vector2(0, turnAmountPitch);
        CalculateVerticalLookingInput(inputVector);

        // Apply horizontal (yaw) rotation.
        // Rotating around the world Y-axis ensures the camera rotates correctly regardless of its current tilt.
        camTransform.Rotate(Vector3.up, turnAmountYaw, Space.World);*/
    }

    /*private void CalculateVerticalLookingInput(Vector2 inputVector)
    {
        if (!targetPlayer.smoothLookEnabledLastFrame)
        {
            targetPlayer.smoothLookEnabledLastFrame = true;
            targetPlayer.smoothLookTurnCompass.rotation = targetPlayer.gameplayCamera.transform.rotation;
            targetPlayer.smoothLookTurnCompass.SetParent(null);
        }

        targetPlayer.cameraUp -= inputVector.y;
        targetPlayer.cameraUp = Mathf.Clamp(targetPlayer.cameraUp, -80f, 60f);
        targetPlayer.smoothLookTurnCompass.localEulerAngles = new Vector3(targetPlayer.cameraUp, targetPlayer.smoothLookTurnCompass.localEulerAngles.y, targetPlayer.smoothLookTurnCompass.localEulerAngles.z);
        targetPlayer.gameplayCamera.transform.localEulerAngles = new Vector3(Mathf.LerpAngle(targetPlayer.gameplayCamera.transform.localEulerAngles.x, targetPlayer.cameraUp, targetPlayer.smoothLookMultiplier * Time.deltaTime), targetPlayer.gameplayCamera.transform.localEulerAngles.y, targetPlayer.gameplayCamera.transform.localEulerAngles.z);
    }*/

    private void PickATargetPlayer()
    {
        Dictionary<PlayerControllerB, int> playersWithPriorityDict = new();
        foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
        {
            if (player == null || player.isPlayerDead || !player.isPlayerControlled) continue;
            playersWithPriorityDict.Add(player, 0);

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
        Vector3 pos = targetPlayer.transform.position + new Vector3(mistressRandom.NextFloat(-10, 10), mistressRandom.NextFloat(-3, 3), mistressRandom.NextFloat(-10, 10));
        if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 10, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return Vector3.zero;
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
        /*if (cripple)
        {
            targetPlayer.lookSensitivity /= 2;
        }
        else
        {
            targetPlayer.lookSensitivity *= 2;
        }*/
        targetPlayer.disableMoveInput = cripple;
    }
    #endregion

    #region Anim Events

    public void TeleportAnimEvent()
    {
        teleporterTimer = mistressRandom.NextFloat(teleportCooldown - 5, teleportCooldown + 5);
        Vector3 teleportPoint = ChooseNewTeleportPoint();
        if (teleportPoint == Vector3.zero)
        {
            Plugin.Logger.LogError("Could not find a good teleport position");
            teleporterTimer = 0f;
            return;
        }
        Plugin.ExtendedLogging($"Teleporting to: {teleportPoint}");
        agent.Warp(teleportPoint);
    }
    #endregion
}