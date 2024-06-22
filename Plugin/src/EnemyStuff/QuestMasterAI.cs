using System;
using System.Collections;
using System.Diagnostics;
using CodeRebirth.Misc;
using CodeRebirth.src.EnemyStuff;
using GameNetcodeStuff;
using Unity.Mathematics;
using CodeRebirth.Util.Extensions;
using UnityEngine;
using CodeRebirth.Util.Spawning;

namespace CodeRebirth.EnemyStuff;
public abstract class QuestMasterAI : CodeRebirthEnemyAI
{
    [Header("Quest Variables")]
    [Tooltip("How long a player has to complete the assigned quest")]
    [SerializeField]
    public float questTimer = 120f;
    [Tooltip("List of items' names the player needs to collect to complete the quest")]
    [SerializeField]
    public string[] questItems;
    [Tooltip("Name of the given quest")]
    [SerializeField]
    public string questName;
    [Tooltip("Chance of the player receiving another quest after completing the current one")]
    [SerializeField]
    public float questRepeatChance = 10f;
    [Tooltip("Number of possible quest repeats")]
    [SerializeField]
    public int questRepeats = 1;
    [Space(5f)]

    [Header("Animations")]
    [SerializeField]
    public AnimationClip spawnAnimation;
    [Space(5f)]

    [Header("Audio")]
    [SerializeField]
    public AudioSource creatureUltraVoice;
    [SerializeField]
    public AudioClip questGiveClip;
    [SerializeField]
    public AudioClip questSucceedClip;
    [SerializeField]
    public AudioClip questFailClip;
    [SerializeField]
    public AudioClip questGiveAgainClip;
    [SerializeField]
    public AudioClip questAfterFailClip;
    [Header("Behaviour")]
    [Tooltip("Detection Range")]
    [SerializeField]
    public float range = 20f;
    [Space(5f)]

    [Header("Speeds")]
    [Tooltip("Speed for the spawning state")]
    [SerializeField]
    public float spawnSpeed;
    [Tooltip("Speed for the walking state")]
    [SerializeField]
    public float walkSpeed;
    [Tooltip("Speed for the approach state")]
    [SerializeField]
    public float approachSpeed;
    [Tooltip("Speed for the quest state")]
    [SerializeField]
    public float questSpeed;
    [Tooltip("Speed for the docile state")]
    [SerializeField]
    public float docileSpeed;
    [HideInInspector]
    public int questCompletionTimes = 0;
    [HideInInspector]
    public int currentQuestOrder = 0;
    [HideInInspector]
    public bool questTimedOut = false;
    [HideInInspector]
    public bool questCompleted = false;
    [HideInInspector]
    public bool questStarted = false;
    [HideInInspector]
    public int questOrder = 0;
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
        startFailQuest,
        startSucceedQuest
    }
    
    public override void Start()
    { // Animations and sounds arent here yet so you might get bugs probably lol.
        base.Start();
        if (!IsHost) return;
        ChangeSpeedClientRpc(spawnSpeed);
        TriggerAnimationClientRpc(Animations.startSpawn.ToAnimationName());
        StartCoroutine(DoSpawning());
        this.SwitchToBehaviourStateOnLocalClient(State.Spawning);
    }
    protected virtual IEnumerator DoSpawning()
    {
        creatureUltraVoice.Play();
        yield return new WaitForSeconds(spawnAnimation.length);
        StartSearch(transform.position);
        ChangeSpeedClientRpc(walkSpeed);
        TriggerAnimationClientRpc(Animations.startWalk.ToAnimationName());
        this.SwitchToBehaviourStateOnLocalClient(State.Wandering);
    }
    protected virtual void DoWandering()
    {
        if (!FindClosestPlayerInRange(range)) return;
        TriggerAnimationClientRpc(Animations.startApproach.ToAnimationName());
        ChangeSpeedClientRpc(approachSpeed);
        StopSearch(currentSearch);
        this.SwitchToBehaviourStateOnLocalClient(State.Approaching);
    }
    protected virtual void DoApproaching()
    {
        if (Vector3.Distance(transform.position, targetPlayer.transform.position) < 3f && !questStarted)
        {
            questStarted = true;
            TriggerAnimationClientRpc(Animations.startGiveQuest.ToAnimationName());
            StartCoroutine(DoGiveQuest());
        }
        SetDestinationToPosition(targetPlayer.transform.position);
    }
    protected virtual IEnumerator DoGiveQuest()
    {
        LogIfDebugBuild("Starting Quest: " + questName);
        if (!questCompleted) creatureSFX.PlayOneShot(questGiveClip);
        if (questCompleted) creatureSFX.PlayOneShot(questGiveAgainClip);
        yield return new WaitUntil(() => !creatureSFX.isPlaying);
        TriggerAnimationClientRpc(Animations.startQuest.ToAnimationName());
        questStarted = true;
        ChangeSpeedClientRpc(questSpeed);
        if (RoundManager.Instance.allEnemyVents.Length == 0)
        {
            DoCompleteQuest(QuestCompletion.Null);
            yield break;
        }
        CodeRebirthUtils.Instance.SpawnScrapServerRpc(questItems[Math.Clamp(questOrder, 0, questItems.Length - 1)], RoundManager.Instance.insideAINodes[UnityEngine.Random.Range(0, RoundManager.Instance.insideAINodes.Length)].transform.position);
        currentQuestOrder = Math.Clamp(questOrder, 0, questItems.Length - 1);
        questOrder++;
        StartCoroutine(QuestTimer());
    }
    protected virtual IEnumerator QuestTimer(float delay = 5f)
    {
        yield return new WaitForSeconds(delay);
        this.SwitchToBehaviourStateOnLocalClient(State.OngoingQuest);
        yield return new WaitForSeconds(questTimer);
        questTimedOut = true;
    }
    protected virtual void DoOngoingQuest()
    {
        if (targetPlayer == null || targetPlayer.isPlayerDead || !targetPlayer.IsSpawned || !targetPlayer.isPlayerControlled)
        {
            DoCompleteQuest(QuestCompletion.Null);
            return;
        }
        if (questTimedOut)
        {
            DoCompleteQuest(QuestCompletion.TimedOut);
            return;
        }
        if (Vector3.Distance(targetPlayer.transform.position, transform.position) < 5f && targetPlayer.currentlyHeldObjectServer != null && targetPlayer.currentlyHeldObjectServer.itemProperties.itemName == questItems[currentQuestOrder] && targetPlayer.currentlyHeldObjectServer.TryGetComponent<QuestItem>(out QuestItem questItem))
        {
            LogIfDebugBuild("completed!");
            targetPlayer.DespawnHeldObject();
            DoCompleteQuest(QuestCompletion.Completed);
            return;
        }
        SetDestinationToPosition(targetPlayer.transform.position, true);
    }
    protected virtual void DoCompleteQuest(QuestCompletion reason)
    {
        switch (reason)
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
        if (IsHost && UnityEngine.Random.Range(0, 100) < questRepeatChance && reason == QuestCompletion.Completed && questCompletionTimes <= questRepeats)
        {
            questStarted = false;
            questTimedOut = false;
            this.SwitchToBehaviourStateOnLocalClient(State.Wandering);
            return;
        }
        questCompleted = true;
        ChangeSpeedClientRpc(docileSpeed);
        this.SwitchToBehaviourStateOnLocalClient(State.Docile);
        StartSearch(transform.position);
    }
    protected virtual IEnumerator QuestSucceedSequence()
    {
        yield return StartAnimation(Animations.startSucceedQuest);
    }
    protected virtual IEnumerator QuestFailSequence(PlayerControllerB failure)
    {
        yield return StartAnimation(Animations.startFailQuest);
        failure.DamagePlayer(500, true, true, CauseOfDeath.Strangulation, 0, false, default);
        creatureSFX.PlayOneShot(questAfterFailClip);
    }
    protected IEnumerator StartAnimation(Animations animation, int layerIndex = 0, string stateName = "Walking Animation")
    {
        yield return new WaitUntil(() => !creatureSFX.isPlaying);
        TriggerAnimationClientRpc(animation.ToAnimationName());
        yield return new WaitUntil(() => creatureAnimator.GetCurrentAnimatorStateInfo(layerIndex).IsName(stateName));
    }
    protected virtual void DoDocile()
    {
        // Generic behaviour stuff for any type of quest giver when docile
    }
    protected virtual void OnDisable() {
        if (!IsHost) return;
        // delete all the items with the Quest component
    }
    public override void DoAIInterval()
    {
        base.DoAIInterval();
        if (isEnemyDead || StartOfRound.Instance.allPlayersDead) return;
        if (!IsHost) return;

        switch (currentBehaviourStateIndex.ToQuestMasterAIState())
        {
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
}
public class QuestItem : MonoBehaviour
{
}

public static class AnimationsMethods { // this is so cooked, why does c# not have proper enum mehotds
    public static string ToAnimationName(this QuestMasterAI.Animations animation) {
        return animation.ToString();
    }
}