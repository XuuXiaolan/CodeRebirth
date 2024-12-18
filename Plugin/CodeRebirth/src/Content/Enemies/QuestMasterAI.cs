using System;
using System.Collections;
using System.Collections.Generic;
using CodeRebirth.src.MiscScripts;
using CodeRebirth.src.Util;
using GameNetcodeStuff;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace CodeRebirth.src.Content.Enemies;
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
    public float questRepeatChance = 30f;
    [Tooltip("Number of possible quest repeats")]
    [SerializeField]
    public int questRepeats = 1;
    [Space(5f)]

    [Header("Animations")]
    [SerializeField]
    public NetworkAnimator networkAnimator = null!;
    [SerializeField]
    public AnimationClip spawnAnimation = null!;
    [Space(5f)]

    [Header("Audio")]
    [SerializeField]
    public AudioSource creatureUltraVoice = null!;
    [SerializeField]
    public AudioSource KarokeSource = null!;
    [SerializeField]
    public AudioClip questGiveClip = null!;
    [SerializeField]
    public AudioClip questSucceedClip = null!;
    [SerializeField]
    public AudioClip questFailClip = null!;
    [SerializeField]
    public AudioClip questGiveAgainClip = null!;
    [SerializeField]
    public AudioClip questAfterFailClip = null!;

    [Header("Behaviour")]
    [Tooltip("Detection Range")]
    [SerializeField]
    public float range = 20f;
    [Tooltip("Dead Body Prefabs")]
    public List<GameObject> deadBodies = new();
    [Tooltip("Bloody Duck Mat")]
    public Material bloodyMaterial = null!;
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
    public NetworkVariable<int> questCompletionTimes = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [HideInInspector]
    public NetworkVariable<int> currentQuestOrder = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [HideInInspector]
    public NetworkVariable<bool> questTimedOut = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [HideInInspector]
    public NetworkVariable<bool> questCompleted = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [HideInInspector]
    public NetworkVariable<bool> questStarted = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [HideInInspector]
    public NetworkVariable<int> questOrder = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [HideInInspector]
    public NetworkVariable<bool> notFirstSpawn = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [HideInInspector]
    public List<GameObject> spawnedBodies = new();
    [HideInInspector]
    public float internalQuestTimer = 0f;

    public enum State
    {
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

    private readonly static int startWalkAnimation = Animator.StringToHash("startWalk"); // Trigger
    private readonly static int startGiveQuestAnimation = Animator.StringToHash("startGiveQuest"); // Trigger
    private readonly static int startQuestAnimation = Animator.StringToHash("startQuest"); // Trigger
    private static readonly int startFailQuestAnimation = Animator.StringToHash("startFailQuest"); // Trigger
    private static readonly int startSucceedQuestAnimation = Animator.StringToHash("startSucceedQuest"); // Trigger
    private static readonly int isTalkingAnimation = Animator.StringToHash("isTalking"); // Bool
    private static readonly int isSittingAnimation = Animator.StringToHash("isSitting"); // Bool
    private static readonly int runSpeedFloat = Animator.StringToHash("RunSpeed"); // Float

    public override void Start()
    { // Animations and sounds arent here yet so you might get bugs probably lol.
        base.Start();

        smartAgentNavigator.SetAllValues(this.isOutside);
        if (!IsHost) return;
        ChangeSpeedClientRpc(spawnSpeed);

        StartCoroutine(DoSpawning());
        SwitchToBehaviourClientRpc((int)State.Spawning);
    }

    protected virtual IEnumerator DoSpawning()
    {
        if (!notFirstSpawn.Value) creatureUltraVoice.Play();
        yield return new WaitForSeconds(spawnAnimation.length);
        smartAgentNavigator.StartSearchRoutine(transform.position, 40);
        ChangeSpeedClientRpc(walkSpeed);
        networkAnimator.SetTrigger(startWalkAnimation);
        SwitchToBehaviourClientRpc((int)State.Wandering);
    }

    protected virtual void DoWandering()
    {
        if (!FindClosestPlayerInRange(range)) return;
        ChangeSpeedClientRpc(approachSpeed);
        smartAgentNavigator.StopSearchRoutine();
        SwitchToBehaviourClientRpc((int)State.Approaching);
    }

    protected virtual void DoApproaching()
    {
        if (Vector3.Distance(transform.position, targetPlayer.transform.position) < 3f && !questStarted.Value)
        {
            questStarted.Value = true;
            networkAnimator.SetTrigger(startGiveQuestAnimation);
            StartCoroutine(DoGiveQuest());
        }
        smartAgentNavigator.DoPathingToDestination(targetPlayer.transform.position, targetPlayer.isInsideFactory, true, targetPlayer);
    }

    public void PlayMiscSoundsClientRpc(int soundIndex)
    {
        AudioClip? soundToPlay = null;
        switch (soundIndex)
        {
            case 0:
                soundToPlay = questGiveAgainClip;
                break;
            case 1:
                soundToPlay = questGiveClip;
                break;
            case 2:
                soundToPlay = questFailClip;
                break;
            case 3:
                soundToPlay = questSucceedClip;
                break;
            case 4:
                soundToPlay = questAfterFailClip;
                break;
            default:
                break;
        }
        creatureSFX.PlayOneShot(soundToPlay);
    }

    protected virtual IEnumerator DoGiveQuest()
    {
        Plugin.ExtendedLogging("Starting Quest: " + questName);
        DuckUI.Instance.SetTextManually("");
        if (questCompleted.Value)
        {
            PlayMiscSoundsClientRpc(0);
            DuckUI.Instance.StartTalking("And one more thing for you!", 0.13f, targetPlayer);
        }
        else
        {
            PlayMiscSoundsClientRpc(1);
        }
        creatureAnimator.SetBool(isTalkingAnimation, true);
        yield return new WaitUntil(() => !creatureSFX.isPlaying);
        creatureAnimator.SetBool(isTalkingAnimation, false);
        networkAnimator.SetTrigger(startQuestAnimation);
        questStarted.Value = true;
        ChangeSpeedClientRpc(questSpeed);
        Vector3 randomSpawnPosition = this.transform.position;
        if (RoundManager.Instance.insideAINodes.Length != 0)
        {
            randomSpawnPosition = RoundManager.Instance.insideAINodes[UnityEngine.Random.Range(0, RoundManager.Instance.insideAINodes.Length-1)].transform.position;
        }
        DuckUI.Instance.SetUIVisible(true);
        DuckUI.Instance.StartTalking($"Find the {questItems[Math.Clamp(questOrder.Value, 0, questItems.Length - 1)]}!!!", 0.12f, targetPlayer);
        NetworkObjectReference item = CodeRebirthUtils.Instance.SpawnScrap(Plugin.samplePrefabs[questItems[Math.Clamp(questOrder.Value, 0, questItems.Length - 1)]], randomSpawnPosition, true, true, 0);

        KarokeSource.Play();
        currentQuestOrder.Value = Math.Clamp(questOrder.Value, 0, questItems.Length - 1);
        questOrder.Value++;
        StartCoroutine(QuestTimer(0.5f));
    }

    protected virtual IEnumerator QuestTimer(float delay = 5f)
    {
        yield return new WaitForSeconds(delay);
        SwitchToBehaviourClientRpc((int)State.OngoingQuest);
        while (internalQuestTimer <= questTimer)
        {
            internalQuestTimer += Time.deltaTime;
            yield return null;
        }
        questTimedOut.Value = true;
    }

    private Coroutine? completionRoutine = null;
    protected virtual void DoOngoingQuest()
    {
        if (targetPlayer == null || targetPlayer.isPlayerDead || !targetPlayer.IsSpawned || !targetPlayer.isPlayerControlled)
        {
            DoCompleteQuest(QuestCompletion.Null);
            return;
        }
        if (questTimedOut.Value)
        {
            DuckUI.Instance.StartTalking("Too bad!!!", 0.05f, targetPlayer, onFinishTalking: delegate
            {
                DuckUI.Instance.SetUIVisible(false);
            });
            DoCompleteQuest(QuestCompletion.TimedOut);
            return;
        }
        if (Vector3.Distance(targetPlayer.transform.position, transform.position) < 5f && targetPlayer.currentlyHeldObjectServer != null && targetPlayer.currentlyHeldObjectServer.itemProperties.itemName == questItems[currentQuestOrder.Value])
        {
            completionRoutine ??= StartCoroutine(TryCompleteQuest());
            return;
        }
        smartAgentNavigator.DoPathingToDestination(targetPlayer.transform.position, targetPlayer.isInsideFactory, true, targetPlayer);
    }

    protected virtual IEnumerator TryCompleteQuest()
    {
        yield return new WaitForSeconds(0.2f);
        if (targetPlayer != null && targetPlayer.currentlyHeldObjectServer != null && targetPlayer.currentlyHeldObjectServer.itemProperties.itemName == questItems[currentQuestOrder.Value])
        {
            targetPlayer.DespawnHeldObject();
            Plugin.ExtendedLogging("completed!");
            DoCompleteQuest(QuestCompletion.Completed);
            questStarted.Value = false;
            DuckUI.Instance.StartTalking("Good Job!!", 0.05f, targetPlayer, onFinishTalking: delegate
            {
                DuckUI.Instance.SetUIVisible(false);
            });
        }
        completionRoutine = null;
    }
    protected virtual void DoCompleteQuest(QuestCompletion reason)
    {
        switch (reason)
        {
            case QuestCompletion.TimedOut:
                {
                    PlayMiscSoundsClientRpc(2);
                    StartCoroutine(QuestFailSequence(targetPlayer));
                    break;
                }
            case QuestCompletion.Completed:
                {
                    StartCoroutine(QuestSucceedSequence());
                    questCompletionTimes.Value++;
                    break;
                }
            case QuestCompletion.Null:
                {
                    Plugin.ExtendedLogging("Target Player or Enemy vents is null?");
                    break;
                }
        }
        bool doOtherQuest = false;
        if (IsHost && UnityEngine.Random.Range(0, 100) < questRepeatChance && reason == QuestCompletion.Completed && questCompletionTimes.Value <= questRepeats)
        {
            doOtherQuest = true;
            questStarted.Value = false;
            questTimedOut.Value = false;
            questCompleted.Value = true;
            SwitchToBehaviourClientRpc((int)State.Wandering);
            return;
        }
        else if (reason  == QuestCompletion.Completed && !doOtherQuest)
        {
            PlayMiscSoundsClientRpc(3);
        }
        questCompleted.Value = true;
        ChangeSpeedClientRpc(docileSpeed);
        SwitchToBehaviourClientRpc((int)State.Docile);
        smartAgentNavigator.StartSearchRoutine(transform.position, 40);
    }

    protected virtual IEnumerator QuestSucceedSequence()
    {
        yield return StartAnimation(startSucceedQuestAnimation);
    }

    protected virtual IEnumerator QuestFailSequence(PlayerControllerB failure)
    {
        yield return new WaitUntil(() => !creatureSFX.isPlaying);
        networkAnimator.SetTrigger(startFailQuestAnimation);
        yield return new WaitForSeconds(1f);
        var bodyIndexToSpawn = UnityEngine.Random.Range(0, deadBodies.Count-1);
        SpawnDeadBodyClientRpc(bodyIndexToSpawn, failure.transform.position);
        failure.KillPlayer(Vector3.zero, spawnBody: false, CauseOfDeath.Unknown, 1);
        PlayMiscSoundsClientRpc(4);
        yield return null;
    }

    [ClientRpc]
    public void SpawnDeadBodyClientRpc(int bodyIndexToSpawn, Vector3 deadPosition)
    {
        var bodyToSpawn = deadBodies[bodyIndexToSpawn];
        var body = GameObject.Instantiate(bodyToSpawn, deadPosition, default, null);
        spawnedBodies.Add(body);
        body.gameObject.SetActive(true);
        skinnedMeshRenderers[0].SetMaterial(bloodyMaterial);
    }

    protected IEnumerator StartAnimation(int animationInt, int layerIndex = 0, string stateName = "Walking Animation")
    {
        yield return new WaitUntil(() => !creatureSFX.isPlaying);
        networkAnimator.SetTrigger(animationInt);
        yield return new WaitUntil(() => creatureAnimator.GetCurrentAnimatorStateInfo(layerIndex).IsName(stateName));
    }

    protected virtual void DoDocile()
    {
        // Todo: Kill the enemy and respawn em at the same position but don't play the spawn music to reset it after a cooldown of 1 or 5 minutes, set notFirstSpawn = true to other duck;
        // Generic behaviour stuff for any type of quest giver when docile
    }

    protected virtual void OnDisable()
    {
        DuckUI.Instance.SetUIVisible(false);
        DuckUI.Instance.SetTextManually("");
        if (!IsHost) return;
        // delete all the items with the Quest component
    }

    public override void DoAIInterval()
    {
        base.DoAIInterval();
        if (isEnemyDead || StartOfRound.Instance.allPlayersDead) return;
        if (!IsHost) return;

        creatureAnimator.SetFloat(runSpeedFloat, agent.velocity.magnitude / 2f);
        switch (currentBehaviourStateIndex)
        {
            case (int)State.Spawning:
                break;
            case (int)State.Wandering:
                DoWandering();
                break;
            case (int)State.Approaching:
                DoApproaching();
                break;
            case (int)State.OngoingQuest:
                DoOngoingQuest();
                break;
            case (int)State.Docile:
                DoDocile();
                break;
            default:
                Plugin.ExtendedLogging("This Behavior State doesn't exist!");
                break;
        }
    }

    public override void HitEnemy(int force = 1, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
        if (currentBehaviourStateIndex == (int)State.OngoingQuest)
        {
            internalQuestTimer += force;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
    }
}

public class QuestItem : MonoBehaviour
{
}