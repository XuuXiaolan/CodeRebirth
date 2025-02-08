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
            ForceTurnTowardsTarget(localPlayer);
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

        bool LookedAt = PlayerLookingAtEnemy(distance);
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

        bool LookedAt = PlayerLookingAtEnemy(distance);
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
        /*playerToKill.inSpecialInteractAnimation = false;
        playerToKill.inShockingMinigame = false;
        playerToKill.shockingTarget = null;*/
        CodeRebirthUtils.Instance.CloseEyeVolume.weight = 0f;
        // Begin the execution.
        // Once player dies, goes back to spawning phase.
    }
    #endregion

    #region Misc Functions

    private bool PlayerLookingAtEnemy(float distance)
    {
        float dot = Vector3.Dot(targetPlayer.gameplayCamera.transform.forward, (HeadTransform.position - targetPlayer.gameplayCamera.transform.position).normalized);
        Plugin.ExtendedLogging($"Vector Dot: {dot}");
        if (dot <= 0) return false;
        if (Physics.Raycast(targetPlayer.gameplayCamera.transform.position, (HeadTransform.forward - targetPlayer.gameplayCamera.transform.position).normalized, distance, StartOfRound.Instance.collidersAndRoomMaskAndDefault | LayerMask.GetMask("InteractableObject"), QueryTriggerInteraction.Ignore)) return false;
        return true;
    }

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

    public void ForceTurnTowardsTarget(PlayerControllerB player)
    {
        player.targetScreenPos = player.turnCompassCamera.WorldToViewportPoint(HeadTransform.position);
        player.shockMinigamePullPosition = player.targetScreenPos.x - 0.5f;
        float num = Mathf.Clamp(Time.deltaTime, 0f, 0.1f);
        if (player.targetScreenPos.x > 0.54f)
        {
            player.turnCompass.Rotate(Vector3.up * 500f * num * Mathf.Abs(player.shockMinigamePullPosition));
            player.playerBodyAnimator.SetBool("PullingCameraRight", false);
            player.playerBodyAnimator.SetBool("PullingCameraLeft", true);
        }
        else if (player.targetScreenPos.x < 0.46f)
        {
            player.turnCompass.Rotate(Vector3.up * -500f * num * Mathf.Abs(player.shockMinigamePullPosition));
            player.playerBodyAnimator.SetBool("PullingCameraLeft", false);
            player.playerBodyAnimator.SetBool("PullingCameraRight", true);
        }
        else
        {
            player.playerBodyAnimator.SetBool("PullingCameraLeft", false);
            player.playerBodyAnimator.SetBool("PullingCameraRight", false);
        }
        player.targetScreenPos = player.gameplayCamera.WorldToViewportPoint(HeadTransform.position + Vector3.up * 0.35f);
        if (player.targetScreenPos.y > 0.6f)
        {
            player.cameraUp = Mathf.Clamp(Mathf.Lerp(player.cameraUp, player.cameraUp - 25f, 25f * num * Mathf.Abs(player.targetScreenPos.y - 0.5f)), -89f, 89f);
        }
        else if (player.targetScreenPos.y < 0.35f)
        {
            player.cameraUp = Mathf.Clamp(Mathf.Lerp(player.cameraUp, player.cameraUp + 25f, 25f * num * Mathf.Abs(player.targetScreenPos.y - 0.5f)), -89f, 89f);
        }
        player.gameplayCamera.transform.localEulerAngles = new Vector3(player.cameraUp, player.gameplayCamera.transform.localEulerAngles.y, player.gameplayCamera.transform.localEulerAngles.z);
        Vector3 zero = Vector3.zero;
        zero.y = player.turnCompass.eulerAngles.y;
        player.thisPlayerBody.rotation = Quaternion.Lerp(player.thisPlayerBody.rotation, Quaternion.Euler(zero), Time.deltaTime * 20f * (1f - Mathf.Abs(player.shockMinigamePullPosition)));
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
            AIIntervalTime = 0.05f;
        }
        else
        {
            AIIntervalTime = 0.5f;
        }
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