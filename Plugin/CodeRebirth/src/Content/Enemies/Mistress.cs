using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.Content.Unlockables;
using CodeRebirth.src.Util;
using Dawn.Internal;
using Dawn.Utils;
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
    public AnimationCurve BlackOutAnimationCurve = null!;

    private HashSet<PlayerControllerB> previousTargetPlayers = new();
    private PlayerControllerB? playerToKill;

    private float teleporterTimer = 20f;
    private float timeSpentInState = 69f;

    private readonly NetworkVariable<float> killTimerNet = new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private int localLookCount = 0;

    private bool serverConfirmLook = false;

    private float nextLookReportTime = 0f;

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

        if (IsServer)
        {
            UpdateServerAuthoritative();
        }

        UpdateLocalTargetClient();

        if (killTimerNet.Value >= killCooldown)
        {
            if (IsServer)
            {
                killTimerNet.Value = 0f;
            }

            playerToKill = targetPlayer;
            playerToKill.inSpecialInteractAnimation = false;
            playerToKill.shockingTarget = null;
            playerToKill.inShockingMinigame = false;
            playerToKill.disableLookInput = false;
            Plugin.ExtendedLogging($"Executing player so please work please please {playerToKill}");
            StartCoroutine(ResetVolumeWeightTo0(playerToKill));
            StartCoroutine(InitiateKillingSequence(playerToKill));
            SwitchToBehaviourStateOnLocalClient((int)State.Execution);
            return;
        }

        UpdateFacing();
    }

    private void UpdateFacing()
    {
        if (!IsServer || targetPlayer == null)
        {
            return;
        }

        Vector3 direction = targetPlayer.gameplayCamera.transform.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, 5f * Time.deltaTime);
    }

    #region Authoritative server update (NO client checks here)

    private void UpdateServerAuthoritative()
    {
        if (StartOfRound.Instance.allPlayersDead || isEnemyDead || targetPlayer == null)
            return;

        if (currentBehaviourStateIndex == (int)State.Attack && StartOfRound.Instance.shipIsLeaving)
        {
            if (targetPlayer != null)
            {
                ResetMistressStalkingServerRpc(targetPlayer);
                targetPlayer = null;
            }
            return;
        }

        if (targetPlayer.isPlayerDead)
        {
            StartCoroutine(ResetMistressToStalking(targetPlayer));
            return;
        }

        if (currentBehaviourStateIndex == (int)State.Stalking)
        {
            teleporterTimer -= Time.deltaTime;
            timeSpentInState -= Time.deltaTime;

            if (teleporterTimer <= 0f)
            {
                teleporterTimer = enemyRandom.NextFloat(teleportCooldown - 5f, teleportCooldown + 5f);
                TeleportRoutineServer();
                return;
            }

            if (serverConfirmLook)
            {
                serverConfirmLook = false;

                if (timeSpentInState <= 0f)
                {
                    timeSpentInState = UnityEngine.Random.Range(50f, 80f);

                    TemporarilyCripplePlayerServerRpc(targetPlayer, true);
                    SwitchToBehaviourServerRpc((int)State.Attack);
                }
                else
                {
                    teleporterTimer = 0f;
                }
            }
        }

        if (currentBehaviourStateIndex == (int)State.Attack)
        {
            if (targetPlayer == null)
            {
                killTimerNet.Value = 0f;
                return;
            }

            killTimerNet.Value += Time.deltaTime;
        }
        else
        {
            if (killTimerNet.Value != 0f)
            {
                killTimerNet.Value = 0f;
            }
        }
    }

    private void TeleportRoutineServer()
    {
        SyncRendererMistressClientRpc();

        Vector3 teleportPoint = ChooseNewTeleportPoint();
        if (teleportPoint == Vector3.zero)
        {
            Plugin.Logger.LogError("Could not find a good teleport position");
            teleporterTimer = 0f;
            return;
        }

        Plugin.ExtendedLogging($"Teleporting to: {teleportPoint}");
        creatureAnimator.SetInteger(IdleIntAnimation, UnityEngine.Random.Range(0, 3));
        agent.Warp(teleportPoint);
    }

    #endregion

    #region Local target client update (LOS + VFX only for local target)

    private void UpdateLocalTargetClient()
    {
        if (targetPlayer == null)
        {
            return;
        }

        if (!targetPlayer.IsLocalPlayer())
        {
            return;
        }

        if (currentBehaviourStateIndex == (int)State.Attack && playerToKill == null)
        {
            targetPlayer.JumpToFearLevel(0.7f);
            float time = Mathf.Clamp01(killTimerNet.Value / killCooldown);
            CodeRebirthUtils.Instance.CloseEyeVolume.weight = BlackOutAnimationCurve.Evaluate(time);
        }

        bool lookedAtEnemy = targetPlayer.HasLineOfSightToPosition(HeadTransform.position, 45f, 50, -1);
        if (currentBehaviourStateIndex == (int)State.Stalking)
        {
            localLookCount = lookedAtEnemy ? localLookCount + 1 : 0;

            // TryReportLookStateToServer(lookedAtEnemy);

            if (localLookCount >= 20)
            {
                localLookCount = 0;
                ConfirmLookThresholdServerRpc();
            }
        }
        else if (currentBehaviourStateIndex == (int)State.Attack)
        {
            localLookCount = Mathf.Clamp(localLookCount + (lookedAtEnemy ? 1 : -1), -10, 0);

            // TryReportLookStateToServer(lookedAtEnemy);

            if (localLookCount <= -10)
            {
                localLookCount = 0;
                ResetMistressStalkingServerRpc(targetPlayer);
            }
        }
        else
        {
            localLookCount = 0;
        }
    }

    private void TryReportLookStateToServer(bool lookedAtEnemy)
    {
        if (Time.unscaledTime < nextLookReportTime)
            return;

        nextLookReportTime = Time.unscaledTime + 0.16f;
        ReportLookStateServerRpc(lookedAtEnemy);
    }

    #endregion

    #region State Machine (Host-only AI interval; do not put client LOS here)

    public override void DoAIInterval()
    {
        base.DoAIInterval();
        if (targetPlayer == null) return;
        if (StartOfRound.Instance.allPlayersDead || isEnemyDead) return;
        if (targetPlayer.isPlayerDead)
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

    private void DoStalking() { }
    private void DoAttack() { }
    private void DoExecution() { }

    #endregion

    #region Misc Functions

    private IEnumerator InitiateKillingSequence(PlayerControllerB playerToExecute)
    {
        yield return null;
        Physics.Raycast(Vector3.zero + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 100, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore);
        if (playerToExecute.isInsideFactory && playerToExecute.IsLocalPlayer())
        {
            EntranceTeleport? entrance = DawnNetworker.EntrancePoints.FirstOrDefault(e => !e.isEntranceToBuilding);
            entrance?.TeleportPlayer();
        }
        playerToExecute.DropAllHeldItems();

        if (!IsServer)
        {
            yield break;
        }

        GameObject guillotineGO = Instantiate(EnemyHandler.Instance.Mistress!.GuillotinePrefab, hit.point, Quaternion.Euler(-90, 0, 0), RoundManager.Instance.mapPropsContainer.transform);

        var netObj = guillotineGO.GetComponent<NetworkObject>();
        netObj.Spawn(false);

        var guillotine = guillotineGO.GetComponent<Guillotine>();
        yield return new WaitUntil(() => netObj.IsSpawned);

        guillotine.SyncGuillotineServerRpc(playerToExecute, new NetworkBehaviourReference(this));
        yield return new WaitUntil(() => guillotine.sequenceFinished);

        StartCoroutine(ResetMistressToStalking(playerToExecute));

        yield return new WaitForSeconds(20f);
        guillotine.NetworkObject.Despawn();
    }

    private IEnumerator ResetMistressToStalking(PlayerControllerB? lastTargetPlayer)
    {
        Plugin.ExtendedLogging("Resetting mistress to stalking phase!");
        if (targetPlayer != null)
        {
            previousTargetPlayers.Add(targetPlayer);
            targetPlayer = null;
        }

        playerToKill = null;
        localLookCount = 0;
        serverConfirmLook = false;

        if (!IsServer)
        {
            yield break;
        }

        killTimerNet.Value = 0f;
        PlayVanishClientRpc();

        yield return new WaitForSeconds(1f);

        if (lastTargetPlayer != null)
        {
            TemporarilyCripplePlayerServerRpc(lastTargetPlayer, false);
        }
        else if (currentBehaviourStateIndex != (int)State.Spawning)
        {
            Plugin.Logger.LogError("Target player was not supposed to be null with this state: " + currentBehaviourStateIndex);
        }

        SwitchToBehaviourServerRpc((int)State.Spawning);
        teleporterTimer = 0f;

        yield return new WaitForSeconds(20f);

        timeSpentInState = UnityEngine.Random.Range(50f, 80f);
        SwitchToBehaviourServerRpc((int)State.Stalking);
        PickATargetPlayer();
    }

    private Vector3 ChooseNewTeleportPoint()
    {
        if (targetPlayer == null)
            return Vector3.zero;

        for (int i = 0; i < 10; i++)
        {
            Vector3 randomDirection = new(UnityEngine.Random.Range(-1f, 1f), 0f, UnityEngine.Random.Range(-1f, 1f));

            if (randomDirection.sqrMagnitude < 0.001f)
                continue;

            randomDirection.Normalize();

            float distance = UnityEngine.Random.Range(10f, 30f);
            Vector3 candidatePos = targetPlayer.transform.position + randomDirection * distance;
            candidatePos.y += UnityEngine.Random.Range(-3f, 3f);

            if (NavMesh.SamplePosition(candidatePos, out NavMeshHit hit, 10f, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }

        return Vector3.zero;
    }

    private void PickATargetPlayer()
    {
        if (!IsServer)
            return;

        Dictionary<PlayerControllerB, int> playersWithPriority = new();

        foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
        {
            if (player == null || player.isPlayerDead || !player.isPlayerControlled || player.IsPseudoDead())
                continue;

            int priority = 0;

            if (previousTargetPlayers.Contains(player))
                priority += 200;

            if (player.isInsideFactory)
                priority += 1;

            if (player.isPlayerAlone)
                priority += 1;

            playersWithPriority[player] = priority;
        }

        if (playersWithPriority.Count == 0)
        {
            Plugin.Logger.LogError("Mistress target selection found no valid players. Aborting.");
            SwitchToBehaviourServerRpc((int)State.Spawning);
            return;
        }

        foreach (var gal in GalAI.Instances)
        {
            if (gal == null || gal.ownerPlayer == null || gal.ownerPlayer.isPlayerDead || !gal.ownerPlayer.isPlayerControlled || gal.ownerPlayer.IsPseudoDead())
                continue;

            if (playersWithPriority.ContainsKey(gal.ownerPlayer))
                playersWithPriority[gal.ownerPlayer] += 1;
        }

        int maxPriority = playersWithPriority.Values.Max();
        List<PlayerControllerB> topCandidates = playersWithPriority.Where(kvp => kvp.Value == maxPriority).Select(kvp => kvp.Key).ToList();

        PlayerControllerB chosen = topCandidates[UnityEngine.Random.Range(0, topCandidates.Count)];
        SetPlayerTargetServerRpc(chosen);
    }

    private IEnumerator ResetVolumeWeightTo0(PlayerControllerB player)
    {
        if (!player.IsLocalPlayer())
        {
            yield break;
        }

        while (CodeRebirthUtils.Instance.CloseEyeVolume.weight > 0f)
        {
            yield return null;
            CodeRebirthUtils.Instance.CloseEyeVolume.weight = BlackOutAnimationCurve.Evaluate(Mathf.MoveTowards(CodeRebirthUtils.Instance.CloseEyeVolume.weight, 0f, Time.deltaTime * 0.5f));
        }
    }

    private IEnumerator UnHideMistress()
    {
        yield return new WaitForSeconds(1f);
        skinnedMeshRenderers[0].enabled = true;
    }

    #endregion

    #region RPCs

    [ServerRpc(RequireOwnership = false)]
    private void ReportLookStateServerRpc(bool lookedAtEnemy)
    {
        // You can log / debug here if you want.
        // Avoid driving gameplay directly off this unless you also validate sender == targetPlayer
    }

    [ServerRpc(RequireOwnership = false)]
    private void ConfirmLookThresholdServerRpc(ServerRpcParams rpcParams = default)
    {
        if (targetPlayer == null)
            return;

        if (currentBehaviourStateIndex != (int)State.Stalking)
            return;

        serverConfirmLook = true;
    }

    [ServerRpc(RequireOwnership = false)]
    private void ResetMistressStalkingServerRpc(PlayerControllerReference playerRef)
    {
        ResetMistressStalkingClientRpc(playerRef);
    }

    [ClientRpc]
    private void ResetMistressStalkingClientRpc(PlayerControllerReference playerRef)
    {
        StartCoroutine(ResetMistressToStalking(playerRef));
    }

    [ClientRpc]
    private void SyncRendererMistressClientRpc()
    {
        if (skinnedMeshRenderers != null && skinnedMeshRenderers.Length > 0)
            skinnedMeshRenderers[0].enabled = false;

        if (GameNetworkManager.Instance.localPlayerController == targetPlayer)
            StartCoroutine(UnHideMistress());
    }

    [ClientRpc]
    private void PlayVanishClientRpc()
    {
        // Play on everyone so presentation matches.
        creatureNetworkAnimator.SetTrigger(DoVanishAnimation);
    }

    [ServerRpc(RequireOwnership = false)]
    private void TemporarilyCripplePlayerServerRpc(PlayerControllerReference playerReference, bool cripple)
    {
        TemporarilyCripplePlayerClientRpc(playerReference, cripple);
    }

    [ClientRpc]
    private void TemporarilyCripplePlayerClientRpc(PlayerControllerReference playerReference, bool cripple)
    {
        PlayerControllerB player = playerReference;

        if (cripple)
        {
            if (player.IsLocalPlayer())
                creatureVoice.PlayOneShot(AttackSounds[UnityEngine.Random.Range(0, AttackSounds.Length)]);

            player.inAnimationWithEnemy = this;
            inSpecialAnimationWithPlayer = player;
            player.inSpecialInteractAnimation = true;
            player.shockingTarget = HeadTransform;
            player.inShockingMinigame = true;
            player.isMovementHindered++;
            player.sprintMeter = 0f;
        }
        else
        {
            if (player.IsLocalPlayer())
                creatureVoice.PlayOneShot(LoseSightSound, 0.75f);

            if (skinnedMeshRenderers != null && skinnedMeshRenderers.Length > 0)
                skinnedMeshRenderers[0].enabled = false;

            player.inAnimationWithEnemy = null;
            inSpecialAnimationWithPlayer = null;
            player.inSpecialInteractAnimation = false;
            player.shockingTarget = null;
            player.inShockingMinigame = false;
            player.isMovementHindered--;
            StartCoroutine(ResetVolumeWeightTo0(player));
        }

        player.disableLookInput = cripple;
    }
    #endregion
}