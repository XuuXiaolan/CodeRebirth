using System;
using System.Collections;
using System.Diagnostics;
using CodeRebirth.Misc;
using CodeRebirth.src;
using CodeRebirth.src.EnemyStuff;
using GameNetcodeStuff;
using Unity.Mathematics;
using UnityEngine;

namespace CodeRebirth.EnemyStuff;
public class Duck : QuestMasterAI
{
    public new enum State {
        Spawning,
        Wandering,
        Approaching,
        OngoingQuest,
        Docile,
    }

    public new enum QuestCompletion
    {
        TimedOut,
        Completed,
        Null,
    }

    public new enum Animations
    {
        startSpawn,
        startWalk,
        startApproach,
        startGiveQuest,
        startQuest,
        startFailQuest,
        startSucceedQuest,
    }

    public override void Start() { // Animations and sounds arent here yet so you might get bugs probably lol.
        base.Start();
        if (!IsHost) return;
        LogIfDebugBuild(this.enemyType.enemyName + " Spawned.");
        creatureVoice.volume = 0.5f;
        ChangeSpeedClientRpc(spawnSpeed);
        DoAnimationClientRpc(Animations.startSpawn.ToAnimationName());
        StartCoroutine(DoSpawning());
        this.SwitchToBehaviourStateOnLocalClient(State.Spawning);
    }

    private IEnumerator DoSpawning() {
        creatureUltraVoice.Play();
        yield return new WaitForSeconds(spawnAnimation.length);
        StartSearch(transform.position);
        ChangeSpeedClientRpc(walkSpeed);
        DoAnimationClientRpc(Animations.startWalk.ToAnimationName());
        this.SwitchToBehaviourStateOnLocalClient(State.Wandering);
    }

    private void DoWandering() {
        if (!FindClosestPlayerInRange(range)) return;
        DoAnimationClientRpc(Animations.startApproach.ToAnimationName());
        ChangeSpeedClientRpc(approachSpeed);
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
        yield return new WaitUntil(() => !creatureSFX.isPlaying);
        DoAnimationClientRpc(Animations.startQuest.ToAnimationName());
        questStarted = true;
        ChangeSpeedClientRpc(questSpeed);
        if (RoundManager.Instance.allEnemyVents.Length == 0) {
            DoCompleteQuest(QuestCompletion.Null);
            yield break;
        }
        CodeRebirthUtils.Instance.SpawnScrapServerRpc(questItems[Math.Clamp(questOrder, 0, questItems.Length - 1)], RoundManager.Instance.insideAINodes[UnityEngine.Random.Range(0, RoundManager.Instance.insideAINodes.Length)].transform.position);
        currentQuestOrder = Math.Clamp(questOrder, 0, questItems.Length - 1);
        questOrder++;
        StartCoroutine(QuestTimer());
    }
    private IEnumerator QuestTimer() {
        yield return new WaitForSeconds(5f);
        this.SwitchToBehaviourStateOnLocalClient(State.OngoingQuest);
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
        if (Vector3.Distance(targetPlayer.transform.position, transform.position) < 5f && targetPlayer.currentlyHeldObjectServer != null && targetPlayer.currentlyHeldObjectServer.itemProperties.itemName == questItems[currentQuestOrder]) {
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
                    StartCoroutine(QuestFailSequence(targetPlayer));
                    break;
                }
            case QuestCompletion.Completed:
                {
                    creatureSFX.PlayOneShot(questSucceedClip);
                    StartCoroutine(QuestSucceedSequence());
                    questCompletionTimes++;
                    break;
                }
            case QuestCompletion.Null:
                {
                    LogIfDebugBuild("Target Player or Enemy vents is null?");
                    break;
                }
        }
        if (IsHost && UnityEngine.Random.Range(0, 100) < questRepeatChance && reason == QuestCompletion.Completed && questCompletionTimes <= 1) {
            questStarted = false;
            questTimedOut = false;
            this.SwitchToBehaviourStateOnLocalClient(State.Wandering);
            return;
        }
        questCompleted = true;
        ChangeSpeedClientRpc(docileSpeed);
        this.SwitchToBehaviourStateOnLocalClient(State.Docile);
        creatureVoice.volume = 0.25f;
        StartSearch(transform.position);
    }
    private IEnumerator QuestSucceedSequence() {
        yield return StartAnimation(Animations.startSucceedQuest);
    }
    private IEnumerator QuestFailSequence(PlayerControllerB failure)
    {
        yield return StartAnimation(Animations.startFailQuest);
        failure.DamagePlayer(500, true, true, CauseOfDeath.Strangulation, 0, false, default);
        creatureSFX.PlayOneShot(questAfterFailClip);
    }

    IEnumerator StartAnimation(Animations animation, int layerIndex = 0, string stateName = "Walking Animation")
    {
        yield return new WaitUntil(() => !creatureSFX.isPlaying);
        DoAnimationClientRpc(animation.ToAnimationName());
        yield return new WaitUntil(() => creatureAnimator.GetCurrentAnimatorStateInfo(layerIndex).IsName(stateName));
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
}