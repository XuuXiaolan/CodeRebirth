using System;
using System.Collections;
using System.Collections.Generic;
using CodeRebirth.src.MiscScripts;
using CodeRebirth.src.Util;
using CodeRebirthLib.ContentManagement.Items;
using GameNetcodeStuff;
using Unity.Netcode;
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
    public AnimationClip spawnAnimation = null!;
    [Space(5f)]

    [Header("Audio")]
    [SerializeField]
    public AudioSource creatureUltraVoice = null!;
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
    public Material[] bloodyMaterials = new Material[3];
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
    public List<GameObject> spawnedBodies = new();
    [HideInInspector]
    public float internalQuestTimer = 0f;
    [HideInInspector]
    public int questsFailed = 0;
    [HideInInspector]
    public List<GameObject> questItemsList = new();
    [HideInInspector]
    public List<GameObject> deadBodiesList = new();
    [HideInInspector]
    public bool changingState = false;

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

    private static readonly int startWalkAnimation = Animator.StringToHash("startWalk"); // Trigger
    private static readonly int startGiveQuestAnimation = Animator.StringToHash("startGiveQuest"); // Trigger
    private static readonly int startQuestAnimation = Animator.StringToHash("startQuest"); // Trigger
    private static readonly int startFailQuestAnimation = Animator.StringToHash("startFailQuest"); // Trigger
    private static readonly int startSucceedQuestAnimation = Animator.StringToHash("startSucceedQuest"); // Trigger
    private static readonly int isTalkingAnimation = Animator.StringToHash("isTalking"); // Bool
    private static readonly int isSittingAnimation = Animator.StringToHash("isSitting"); // Bool
    private static readonly int runSpeedFloat = Animator.StringToHash("RunSpeed"); // Float

    public override void Start()
    {
        base.Start();
        if (!IsHost) return;
        agent.speed = spawnSpeed;

        StartCoroutine(DoSpawning());
        SwitchToBehaviourClientRpc((int)State.Spawning);
    }

    protected virtual IEnumerator DoSpawning()
    {
        yield return new WaitForSeconds(spawnAnimation.length);
        smartAgentNavigator.StartSearchRoutine(40);
        agent.speed = walkSpeed;
        creatureNetworkAnimator.SetTrigger(startWalkAnimation);
        SwitchToBehaviourClientRpc((int)State.Wandering);
    }

    protected virtual void DoWandering()
    {
        if (changingState || !FindClosestPlayerInRange(range)) return;
        SetTargetServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, targetPlayer));
        StartCoroutine(ChangingToApproaching(questCompleted.Value));
    }

    protected virtual IEnumerator ChangingToApproaching(bool doDelay)
    {
        if (doDelay)
        {
            changingState = true;
            yield return new WaitForSeconds(1f);
            changingState = false;
        }
        agent.speed = approachSpeed;
        smartAgentNavigator.StopSearchRoutine();
        SwitchToBehaviourClientRpc((int)State.Approaching);
    }

    protected virtual void DoApproaching()
    {
        if (Vector3.Distance(transform.position, targetPlayer.transform.position) < 3f && !questStarted.Value)
        {
            questStarted.Value = true;
            creatureNetworkAnimator.SetTrigger(startGiveQuestAnimation);
            StartCoroutine(DoGiveQuest());
        }
        smartAgentNavigator.DoPathingToDestination(targetPlayer.transform.position);
    }

    [ClientRpc]
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
        SetDuckTextManuallyClientRpc("");
        if (questCompleted.Value)
        {
            PlayMiscSoundsClientRpc(0);
            SetDuckStartTalkingClientRpc("And one more thing for you!", 0.13f, Array.IndexOf(StartOfRound.Instance.allPlayerScripts, targetPlayer), false, false);
        }
        else
        {
            PlayMiscSoundsClientRpc(1);
        }
        creatureAnimator.SetBool(isTalkingAnimation, true);
        yield return new WaitUntil(() => !creatureSFX.isPlaying);
        creatureAnimator.SetBool(isTalkingAnimation, false);
        creatureNetworkAnimator.SetTrigger(startQuestAnimation);
        questStarted.Value = true;
        agent.speed = questSpeed;
        Vector3 randomSpawnPosition = this.transform.position;
        if (RoundManager.Instance.insideAINodes.Length != 0)
        {
            randomSpawnPosition = RoundManager.Instance.insideAINodes[UnityEngine.Random.Range(0, RoundManager.Instance.insideAINodes.Length)].transform.position;
        }
        SetDuckUIVisibleClientRpc(true);
        SetDuckStartTalkingClientRpc($"Find the {questItems[Math.Clamp(questOrder.Value, 0, questItems.Length - 1)]}!!!", 0.12f, Array.IndexOf(StartOfRound.Instance.allPlayerScripts, targetPlayer), false, false);
        StartCoroutine(QuestTimer(randomSpawnPosition, 0.5f));
    }

    protected virtual IEnumerator QuestTimer(Vector3 randomSpawnPosition, float delay = 5f)
    {
        SetDuckUIItemUIPlayerClientRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, targetPlayer));
        yield return new WaitForSeconds(delay / 5);
        if (!Plugin.Mod.ItemRegistry().TryGetFromItemName(questItems[Math.Clamp(questOrder.Value, 0, questItems.Length - 1)], out CRItemDefinition? itemDefinition))
            yield break;

        NetworkObjectReference item = CodeRebirthUtils.Instance.SpawnScrap(itemDefinition.Item, randomSpawnPosition, true, true, 0);
        questItemsList.Add(item);
        currentQuestOrder.Value = Math.Clamp(questOrder.Value, 0, questItems.Length - 1);
        questOrder.Value++;
        yield return new WaitForSeconds(delay / 5 * 4);
        SwitchToBehaviourClientRpc((int)State.OngoingQuest);
        while (internalQuestTimer <= questTimer)
        {
            internalQuestTimer += Time.deltaTime;
            yield return null;
        }
        questTimedOut.Value = true;
    }

    [ClientRpc]
    public void SetDuckUIItemUIPlayerClientRpc(int playerIndex)
    {
        DuckUI.Instance.itemUI.player = StartOfRound.Instance.allPlayerScripts[playerIndex];
    }

    protected virtual void DoOngoingQuest()
    {
        if (targetPlayer == null || targetPlayer.isPlayerDead || !targetPlayer.IsSpawned || !targetPlayer.isPlayerControlled)
        {
            DoCompleteQuest(QuestCompletion.Null);
            return;
        }
        if (questTimedOut.Value)
        {
            SetDuckStartTalkingClientRpc("Too bad!!!", 0.05f, Array.IndexOf(StartOfRound.Instance.allPlayerScripts, targetPlayer), false, true);
            DoCompleteQuest(QuestCompletion.TimedOut);
            return;
        }
        if (Vector3.Distance(targetPlayer.transform.position, transform.position) < 5f && targetPlayer.currentlyHeldObjectServer != null && targetPlayer.currentlyHeldObjectServer.itemProperties.itemName == questItems[currentQuestOrder.Value])
        {
            if (!questItemsList.Contains(targetPlayer.currentlyHeldObjectServer.gameObject))
            {
                SetDuckStartTalkingClientRpc("Too bad!!!", 0.05f, Array.IndexOf(StartOfRound.Instance.allPlayerScripts, targetPlayer), false, true);
                DoCompleteQuest(QuestCompletion.TimedOut);
                return;
            }
            TryCompleteQuest();
            return;
        }
        smartAgentNavigator.DoPathingToDestination(targetPlayer.transform.position);
    }

    protected virtual void TryCompleteQuest()
    {
        if (targetPlayer != null && targetPlayer.currentlyHeldObjectServer != null && targetPlayer.currentlyHeldObjectServer.itemProperties.itemName == questItems[currentQuestOrder.Value])
        {
            Plugin.ExtendedLogging("completed!");
            SetDuckStartTalkingClientRpc("Good Job!!", 0.05f, Array.IndexOf(StartOfRound.Instance.allPlayerScripts, targetPlayer), false, true);
            DoCompleteQuest(QuestCompletion.Completed);
            questStarted.Value = false;
        }
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
                    PlayMiscSoundsClientRpc(2);
                    StartCoroutine(QuestFailSequence(targetPlayer));
                    break;
                }
        }
        questItemsList.Clear();
        if (UnityEngine.Random.Range(0, 100) < questRepeatChance && reason == QuestCompletion.Completed && questCompletionTimes.Value <= questRepeats)
        {
            questStarted.Value = false;
            questTimedOut.Value = false;
            questCompleted.Value = true;
            SwitchToBehaviourClientRpc((int)State.Wandering);
            return;
        }
        else if (reason == QuestCompletion.Completed)
        {
            PlayMiscSoundsClientRpc(3);
        }
        questCompleted.Value = true;
        agent.speed = docileSpeed;
        SwitchToBehaviourClientRpc((int)State.Docile);
        float redoTimer = UnityEngine.Random.Range(45, 61);
        if (reason == QuestCompletion.Completed)
        {
            redoTimer = UnityEngine.Random.Range(180, 301);
        }
        StartCoroutine(RestartEnemy(redoTimer));
        smartAgentNavigator.StartSearchRoutine(40);
    }

    protected virtual IEnumerator RestartEnemy(float timer)
    {
        yield return new WaitForSeconds(timer);
        ResetEverything();
    }

    protected virtual void ResetEverything()
    {
        questCompletionTimes.Value = 0;
        currentQuestOrder.Value = 0;
        questTimedOut.Value = false;
        questCompleted.Value = false;
        questStarted.Value = false;
        questOrder.Value = 0;
        internalQuestTimer = 0f;
        agent.speed = spawnSpeed;
        StartCoroutine(DoSpawning());
        SwitchToBehaviourClientRpc((int)State.Spawning);
    }

    protected virtual IEnumerator QuestSucceedSequence()
    {
        yield return StartAnimation(startSucceedQuestAnimation);
    }

    protected virtual IEnumerator QuestFailSequence(PlayerControllerB? failure)
    {
        yield return new WaitUntil(() => !creatureSFX.isPlaying);
        creatureNetworkAnimator.SetTrigger(startFailQuestAnimation);
        yield return new WaitForSeconds(1f);
        var bodyIndexToSpawn = UnityEngine.Random.Range(0, deadBodies.Count);
        if (failure != null) SpawnDeadBodyClientRpc(bodyIndexToSpawn, failure.transform.position, Array.IndexOf(StartOfRound.Instance.allPlayerScripts, failure));
        PlayMiscSoundsClientRpc(4);
        yield return null;
    }

    [ClientRpc]
    public void SpawnDeadBodyClientRpc(int bodyIndexToSpawn, Vector3 deadPosition, int failureIndex)
    {
        questsFailed++;
        var bodyToSpawn = deadBodies[bodyIndexToSpawn];
        var body = GameObject.Instantiate(bodyToSpawn, deadPosition, default, null);
        spawnedBodies.Add(body);
        body.gameObject.SetActive(true);
        StartOfRound.Instance.allPlayerScripts[failureIndex].KillPlayer(Vector3.zero, spawnBody: false, CauseOfDeath.Unknown, 1);
        Plugin.ExtendedLogging($"Spawning Deadbody, with questsFailed: {questsFailed} and this many stages of bloodiness {bloodyMaterials.Length}");
        if (questsFailed > bloodyMaterials.Length) return;
        skinnedMeshRenderers[0].SetMaterial(bloodyMaterials[questsFailed - 1]);
    }

    protected IEnumerator StartAnimation(int animationInt, int layerIndex = 0, string stateName = "Walking Animation")
    {
        yield return new WaitUntil(() => !creatureSFX.isPlaying);
        creatureNetworkAnimator.SetTrigger(animationInt);
        yield return new WaitUntil(() => creatureAnimator.GetCurrentAnimatorStateInfo(layerIndex).IsName(stateName));
    }

    protected virtual void DoDocile()
    {
        // Generic behaviour stuff for any type of quest giver when docile
    }

    protected virtual void OnDisable()
    {
        DuckUI.Instance.SetUIVisible(false);
        DuckUI.Instance.SetTextManually("");
        foreach (var body in spawnedBodies)
        {
            Destroy(body);
        }
        // delete all the items with the Quest component
    }

    public override void Update()
    {
        base.Update();
        if (creatureSFX.isPlaying || currentBehaviourStateIndex == (int)State.Wandering || currentBehaviourStateIndex == (int)State.Docile)
        {
            creatureVoice.volume = 0;
            return;
        }
        else
        {
            creatureVoice.volume = 0.39f;
        }
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
        if (!IsServer) return;
        if (currentBehaviourStateIndex == (int)State.OngoingQuest)
        {
            internalQuestTimer += force;
        }
    }

    [ClientRpc]
    public void SetDuckTextManuallyClientRpc(string text)
    {
        if (GameNetworkManager.Instance.localPlayerController == targetPlayer) DuckUI.Instance.SetTextManually(text);
    }

    [ClientRpc]
    public void SetDuckStartTalkingClientRpc(string text, float talkspeed, int targetPlayerIndex, bool isGlobal, bool setUIInvisibleAfter)
    {
        PlayerControllerB _targetPlayer = StartOfRound.Instance.allPlayerScripts[targetPlayerIndex];
        if (setUIInvisibleAfter)
        {
            DuckUI.Instance.StartTalking(text, talkspeed, _targetPlayer, isGlobal, delegate
            {
                DuckUI.Instance.SetUIVisible(false);
            });
        }
        else
        {
            DuckUI.Instance.StartTalking(text, talkspeed, _targetPlayer, isGlobal, null);
        }
    }

    [ClientRpc]
    public void SetDuckUIVisibleClientRpc(bool visible)
    {
        if (GameNetworkManager.Instance.localPlayerController == targetPlayer) DuckUI.Instance.SetUIVisible(visible);
    }
}

public class QuestItem : MonoBehaviour
{
}