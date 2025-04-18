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
    public AudioClip LoseSightSound = null!;
    public AudioClip[] AttackSounds = null!;

    private List<PlayerControllerB> previousTargetPlayers = new();
    private int _seeingCount = 0;
    private float teleporterTimer = 20f;
    private float timeSpentInState = 0f;
    private float killTimer = 0f;
    private bool cantLosePlayer = false;
    private PlayerControllerB? playerToKill;
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
        AIIntervalTime = 0.1f;
        skinnedMeshRenderers[0].enabled = false;
        StartCoroutine(ResetMistressToStalking(null));
    }

    public override void Update()
    {
        base.Update();
        if (currentBehaviourStateIndex != (int)State.Attack || targetPlayer == null) return;

        killTimer += Time.deltaTime;
        PlayerControllerB localPlayer = GameNetworkManager.Instance.localPlayerController;
        if (localPlayer == targetPlayer)
        {
            localPlayer.JumpToFearLevel(0.7f);
            CodeRebirthUtils.Instance.CloseEyeVolume.weight = Mathf.Clamp01(killTimer / killCooldown);
        }

        if (killTimer >= killCooldown)
        {
            killTimer = 0f;
            playerToKill = targetPlayer;
            playerToKill.inSpecialInteractAnimation = false;
            playerToKill.shockingTarget = null;
            playerToKill.inShockingMinigame = false;
            playerToKill.disableLookInput = false;
            Plugin.ExtendedLogging($"Executing player so please work please please {playerToKill}");
            StartCoroutine(ResetVolumeWeightTo0(playerToKill));
            StartCoroutine(InitiateKillingSequence(playerToKill));
            SwitchToBehaviourStateOnLocalClient((int)State.Execution);
        }
    }


    public void LateUpdate()
    {
        if (targetPlayer == null) return;

        Vector3 direction = targetPlayer.gameplayCamera.transform.position - transform.position;
        direction.y = 0;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, 5 * Time.deltaTime); // todo: make the mistress head rotate up and down
    }

    #region State Machine
    public override void DoAIInterval()
    {
        base.DoAIInterval();
        if (targetPlayer == null) return;
        if (StartOfRound.Instance.allPlayersDead || isEnemyDead) return;
        if (targetPlayer != null && targetPlayer.isPlayerDead)
        {
            StartCoroutine(ResetMistressToStalking(targetPlayer));
            return;
        }
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

        bool LookedAt = PlayerLookingAtEnemy();
        if (LookedAt)
        {
            _seeingCount++;
        }
        else
        {
            _seeingCount = 0;
        }
        // Plugin.ExtendedLogging($"LookedAt in stalking phase: {LookedAt}");
        if (!LookedAt || _seeingCount < 3)
            return;

        _seeingCount = 0;
        if (timeSpentInState > 0)
        {
            timeSpentInState = 0f;
            TemporarilyCripplePlayerServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, targetPlayer), true);
            StartCoroutine(UpdatePlayerLossVision());
            SwitchToBehaviourServerRpc((int)State.Attack);
        }
        else
        {
            teleporterTimer = 0f;
        }
    }

    private void DoAttack()
    {
        bool LookedAt = PlayerLookingAtEnemy();
        // Plugin.ExtendedLogging($"LookedAt in attack phase: {LookedAt}");
        if (LookedAt)
        {
            _seeingCount = 0;
        }
        else
        {
            _seeingCount++;
        }

        if (_seeingCount < 3) // variable name is really not great here since i'm doing hte direct opposite of it really
            return;

        _seeingCount = 0;
        StartCoroutine(ResetMistressToStalking(targetPlayer));
    }

    private void DoExecution()
    {
        // Plugin.ExtendedLogging($"Executing player {playerToKill}!");
        // Begin the execution.
        // Once player dies, goes back to spawning phase.
    }
    #endregion

    #region Misc Functions

    private IEnumerator UpdatePlayerLossVision()
    {
        cantLosePlayer = true;
        yield return new WaitForSeconds(1f);
        cantLosePlayer = false;
    }

    private IEnumerator InitiateKillingSequence(PlayerControllerB playerToExecute)
    {
        Physics.Raycast(Vector3.zero + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 100, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore);
        if (playerToExecute.isInsideFactory && playerToExecute == GameNetworkManager.Instance.localPlayerController)
        {
            var entrance = CodeRebirthUtils.entrancePoints.Where(entrance => !entrance.isEntranceToBuilding).FirstOrDefault();
            entrance?.TeleportPlayer();
        }
        if (!IsServer) yield break;
        GameObject GuillotineGO = GameObject.Instantiate(EnemyHandler.Instance.Mistress.GuillotinePrefab, hit.point, Quaternion.Euler(-90, 0, 0), RoundManager.Instance.mapPropsContainer.transform);
        var netObj = GuillotineGO.GetComponent<NetworkObject>();
        netObj.Spawn(false);
        var Guillotine = GuillotineGO.GetComponent<Guillotine>();
        yield return new WaitUntil(() => netObj.IsSpawned);
        Guillotine.SyncGuillotineServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerToKill));
        yield return new WaitUntil(() => Guillotine.sequenceFinished); // Have the player stick to guillotine script.
        StartCoroutine(ResetMistressToStalking(targetPlayer));
        yield return new WaitForSeconds(20f);
        Guillotine.NetworkObject.Despawn();
        // todo: Find Valid spot to spawn guillotine.
    }

    private IEnumerator ResetMistressToStalking(PlayerControllerB? lastTargetPlayer)
    {
        Plugin.ExtendedLogging($"Resetting mistress to stalking phase!");
        killTimer = 0f;
        previousTargetPlayers.Add(targetPlayer);
        targetPlayer = null;
        if (!IsServer) yield break;
        playerToKill = null;
        SetTargetServerRpc(-1);
        creatureNetworkAnimator.SetTrigger(DoVanishAnimation);
        yield return new WaitForSeconds(1f);
        if (lastTargetPlayer != null)
        {
            TemporarilyCripplePlayerServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, lastTargetPlayer), false);
        }
        else if (currentBehaviourStateIndex != 0)
        {
            Plugin.Logger.LogError("Target player was not supposed to be null with this state: " + currentBehaviourStateIndex);
        }
        SwitchToBehaviourServerRpc((int)State.Spawning);
        teleporterTimer = 0f;
        yield return new WaitForSeconds(20f);
        timeSpentInState = 0f;
        SwitchToBehaviourServerRpc((int)State.Stalking);
        PickATargetPlayer();
    }

    public IEnumerator TeleportRoutine()
    {
        yield return new WaitForSeconds(0.5f);
        SyncRendererMistressServerRpc();
        teleporterTimer = enemyRandom.NextFloat(teleportCooldown - 5, teleportCooldown + 5);
        Vector3 teleportPoint = ChooseNewTeleportPoint();
        if (teleportPoint == Vector3.zero)
        {
            Plugin.Logger.LogError("Could not find a good teleport position");
            teleporterTimer = 0f;
            yield break;
        }
        Plugin.ExtendedLogging($"Teleporting to: {teleportPoint}");
        creatureAnimator.SetInteger(IdleIntAnimation, enemyRandom.Next(3));
        agent.Warp(teleportPoint);
    }

    private bool PlayerLookingAtEnemy()
    {
        if (cantLosePlayer) return true;
        float dot = Vector3.Dot(targetPlayer.gameplayCamera.transform.forward, (HeadTransform.position - targetPlayer.gameplayCamera.transform.position).normalized);
        // Plugin.ExtendedLogging($"Vector Dot: {dot}");
        if (dot <= 0.45f) return false;
        if (Physics.Linecast(targetPlayer.gameplayCamera.transform.position, HeadTransform.position, out _, CodeRebirthUtils.Instance.collidersAndRoomAndInteractableAndRailingAndEnemiesAndTerrainAndHazardAndVehicleMask, QueryTriggerInteraction.Ignore))
        {
            // Plugin.ExtendedLogging($"Linecast hit {hit.collider.name}");
            return false;
        }
        // Plugin.ExtendedLogging($"Linecast did not hit anything");
        return true;
    }

    private void PickATargetPlayer()
    {
        Dictionary<PlayerControllerB, int> playersWithPriorityDict = new();
        foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
        {
            if (player == null || player.isPlayerDead || !player.isPlayerControlled || player.IsPseudoDead()) continue;
            playersWithPriorityDict.Add(player, 0);

            if (previousTargetPlayers.Contains(player))
            {
                playersWithPriorityDict[player] += 200;
            } // Increase priority if was already a previous target.

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
            if (gal == null || gal.ownerPlayer == null || gal.ownerPlayer.isPlayerDead || !gal.ownerPlayer.isPlayerControlled || gal.ownerPlayer.IsPseudoDead()) continue;
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
            Vector3 randomDirection = new Vector3(enemyRandom.NextFloat(-1f, 1f), 0f, enemyRandom.NextFloat(-1f, 1f));
            if (randomDirection.sqrMagnitude < 0.001f)
            {
                continue;
            }
            randomDirection.Normalize();

            float distance = enemyRandom.NextFloat(MIN_DISTANCE, MAX_DISTANCE);
            Vector3 candidatePos = targetPlayer.transform.position + randomDirection * distance;

            candidatePos.y += enemyRandom.NextFloat(-3f, 3f);

            if (NavMesh.SamplePosition(candidatePos, out NavMeshHit hit, 10f, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }

        return Vector3.zero;
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
    private void SyncRendererMistressServerRpc()
    {
        SyncRendererMistressClientRpc();
    }

    [ClientRpc]
    private void SyncRendererMistressClientRpc()
    {
        skinnedMeshRenderers[0].enabled = false;
        if (GameNetworkManager.Instance.localPlayerController == targetPlayer)
        {
            StartCoroutine(UnHideMistress());
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
            if (playerToCripple.IsOwner)
            {
                creatureVoice.PlayOneShot(AttackSounds[enemyRandom.Next(AttackSounds.Length)]);
            }
            playerToCripple.inAnimationWithEnemy = this;
            inSpecialAnimationWithPlayer = playerToCripple;
            playerToCripple.inSpecialInteractAnimation = true;
            playerToCripple.shockingTarget = HeadTransform;
            playerToCripple.inShockingMinigame = true;
            playerToCripple.isMovementHindered++;
            playerToCripple.hinderedMultiplier *= 2f;
            playerToCripple.sprintMeter = 0f;
        }
        else
        {
            if (playerToCripple.IsOwner)
            {
                creatureVoice.PlayOneShot(LoseSightSound, 0.75f);
            }
            skinnedMeshRenderers[0].enabled = false;
            playerToCripple.inAnimationWithEnemy = null;
            inSpecialAnimationWithPlayer = null;
            playerToCripple.inSpecialInteractAnimation = false;
            playerToCripple.shockingTarget = null;
            playerToCripple.inShockingMinigame = false;
            playerToCripple.isMovementHindered--;
            playerToCripple.hinderedMultiplier /= 2;
            StartCoroutine(ResetVolumeWeightTo0(playerToCripple));
        }
        playerToCripple.disableLookInput = cripple;
    }
    #endregion
}