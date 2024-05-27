using System;
using System.Collections;
using System.Diagnostics;
using CodeRebirth.Misc;
using CodeRebirth.src;
using CodeRebirth.src.EnemyStuff;
using GameNetcodeStuff;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;
using UnityEngine.Yoga;

namespace CodeRebirth.EnemyStuff;
public class Duck : CodeRebirthEnemyAI
{
    [Tooltip("Quest Variables")]
    [SerializeField]
    private float questTimer = 120f;
    [SerializeField]
    private string[] questItems;
    [SerializeField]
    private string questName;
    [Space(5f)]

    [Tooltip("Animations")]
    [SerializeField]
    private AnimationClip spawnAnimation;
    [Space(5f)]

    [Tooltip("Audio")]
    [SerializeField]
    private AudioSource creatureUltraVoice;
    [SerializeField]
    private AudioClip questGiveClip;
    [SerializeField]
    private AudioClip questSucceedClip;
    [SerializeField]
    private AudioClip questFailClip;
    [SerializeField]
    private AudioClip questGiveAgainClip;
    
    private bool questTimedOut = false;
    private bool questCompleted = false;
    private bool questStarted = false;
    private float range = 20f;
    public enum State {
        Spawning,
        Wandering,
        Approaching,
        OngoingQuest,
        Docile,
    }

    public enum QuestCompletion
    {
        TimedOut,
        Completed,
        Null,
    }

    public enum Animations
    {
        startSpawn,
        startWalk,
        startApproach,
        startGiveQuest,
        startQuest,
    }

    public override void Start() { // Animations and sounds arent here yet so you might get bugs probably lol.
        base.Start();
        if (!IsHost) return;
        LogIfDebugBuild(this.enemyType.enemyName + " Spawned.");
        ChangeSpeedClientRpc(1f);
        DoAnimationClientRpc(Animations.startSpawn.ToAnimationName());
        StartCoroutine(DoSpawning());
        this.SwitchToBehaviourStateOnLocalClient(State.Spawning);
    }

    private IEnumerator DoSpawning() {

        creatureUltraVoice.Play();
        yield return new WaitForSeconds(spawnAnimation.length);
        StartSearch(transform.position);
        ChangeSpeedClientRpc(3f);
        DoAnimationClientRpc(Animations.startWalk.ToAnimationName());
        creatureVoice.volume = 0.5f;
        creatureVoice.Play();
        this.SwitchToBehaviourStateOnLocalClient(State.Wandering);
    }

    private void DoWandering() {
        if (!FindClosestPlayerInRange(range)) return;
        DoAnimationClientRpc(Animations.startApproach.ToAnimationName());
        ChangeSpeedClientRpc(8f);
        StopSearch(currentSearch);
        this.SwitchToBehaviourStateOnLocalClient(State.Approaching);
    }

    private void DoApproaching() {
        if (Vector3.Distance(transform.position, targetPlayer.transform.position) < 3f && !questStarted) {
            questStarted = true;
            DoAnimationClientRpc(Animations.startGiveQuest.ToAnimationName());
            StartCoroutine(DoGiveQuest());
        }
        SetDestinationToPosition(targetPlayer.transform.position);

    }

    private IEnumerator DoGiveQuest() {
        LogIfDebugBuild("Starting Quest: " + questName);
        if (!questCompleted) creatureSFX.PlayOneShot(questGiveClip);
        if (questCompleted) creatureSFX.PlayOneShot(questGiveAgainClip);
        yield return new WaitUntil(() => creatureSFX.isPlaying == false);
        DoAnimationClientRpc(Animations.startQuest.ToAnimationName());
        questStarted = true;
        ChangeSpeedClientRpc(8f);
        if (RoundManager.Instance.allEnemyVents.Length == 0) {
            DoCompleteQuest(QuestCompletion.Null);
            yield break;
        }
        CodeRebirthUtils.Instance.SpawnScrapServerRpc(questItems[UnityEngine.Random.Range(0, questItems.Length)], RoundManager.Instance.insideAINodes[UnityEngine.Random.Range(0, RoundManager.Instance.insideAINodes.Length)].transform.position);
        StartCoroutine(QuestTimer());
        this.SwitchToBehaviourStateOnLocalClient(State.OngoingQuest);
    }
    private IEnumerator QuestTimer() {
        yield return new WaitForSeconds(questTimer);
        questTimedOut = true;
    }
    private void DoOngoingQuest() {
        if (targetPlayer == null || targetPlayer.isPlayerDead || !targetPlayer.IsSpawned || !targetPlayer.isPlayerControlled) {
            DoCompleteQuest(QuestCompletion.Null);
            return;
        }
        if (questTimedOut) {
            DoCompleteQuest(QuestCompletion.TimedOut);
            return;
        }
        if (targetPlayer.currentlyHeldObjectServer != null && targetPlayer.currentlyHeldObjectServer.itemProperties.itemName == "Meteorite") {
            LogIfDebugBuild("completed!");
            targetPlayer.DespawnHeldObject();
            DoCompleteQuest(QuestCompletion.Completed);
            return;
        }
        SetDestinationToPosition(targetPlayer.transform.position, true);
    }

    private void DoCompleteQuest(QuestCompletion reason) {
        switch(reason)
        {
            case QuestCompletion.TimedOut:
                {
                    creatureSFX.PlayOneShot(questFailClip);
                    targetPlayer.DamagePlayer(500, true, true, CauseOfDeath.Strangulation, 0, false, default);
                    break;
                }
            case QuestCompletion.Completed:
                {
                    creatureSFX.PlayOneShot(questSucceedClip);
                    break;
                }
            case QuestCompletion.Null:
                {
                    LogIfDebugBuild("Target Player or Enemy vents is null?");
                    break;
                }
        }
        DoAnimationClientRpc(Animations.startWalk.ToAnimationName());
        if (IsHost && UnityEngine.Random.Range(0, 100) < 100 && reason == QuestCompletion.Completed && !questCompleted) {
            questStarted = false;
            questTimedOut = false;
            questCompleted = true;
            this.SwitchToBehaviourStateOnLocalClient(State.Wandering);
            DoAnimationClientRpc(Animations.startWalk.ToAnimationName());
            return;
        }
        questCompleted = true;
        ChangeSpeedClientRpc(4f);
        this.SwitchToBehaviourStateOnLocalClient(State.Docile);
        creatureVoice.volume = 0.25f;
        creatureVoice.Play();
        StartSearch(transform.position);
        DoAnimationClientRpc(Animations.startWalk.ToAnimationName());
    }

    private void DoDocile() {
    }
    public override void DoAIInterval() {
        base.DoAIInterval();
        if (isEnemyDead || StartOfRound.Instance.allPlayersDead) return;
        if (!IsHost) return;

        switch(currentBehaviourStateIndex.ToDuckState()) {
            case State.Spawning:
                break;
            case State.Wandering:
                DoWandering();
                break;
            case State.Approaching:
                DoApproaching();
                break;
            case State.OngoingQuest:
                DoOngoingQuest();
                break;
            case State.Docile:
                DoDocile();
                break;
            default:
                LogIfDebugBuild("This Behavior State doesn't exist!");
                break;
        }
    }

    private bool FindClosestPlayerInRange(float range) {
        PlayerControllerB closestPlayer = null;
        float minDistance = float.MaxValue;

        foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts) {
            bool onSight = player.IsSpawned && player.isPlayerControlled && !player.isPlayerDead && !player.isInHangarShipRoom && EnemyHasLineOfSightToPosition(player.transform.position, 60f, range);
            if (!onSight) continue;

            float distance = Vector3.Distance(transform.position, player.transform.position);
            bool closer = distance < minDistance;
            if (!closer) continue;

            minDistance = distance;
            closestPlayer = player;
        }
        if (closestPlayer == null) return false;

        targetPlayer = closestPlayer;
        return true;
    }

    private bool EnemyHasLineOfSightToPosition(Vector3 pos, float width = 60f, float range = 20f, float proximityAwareness = 5f) {
        if (eye == null) {
            _ = transform;
        } else {
            _ = eye;
        }

        if (Vector3.Distance(eye.position, pos) >= range || Physics.Linecast(eye.position, pos, StartOfRound.Instance.collidersAndRoomMaskAndDefault)) return false;

        Vector3 to = pos - eye.position;
        return Vector3.Angle(eye.forward, to) < width || Vector3.Distance(transform.position, pos) < proximityAwareness;
    }
}

public class QuestItem {
}