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
    [Tooltip("Animations")]
    [SerializeField]
    private AnimationClip spawnAnimation;

    [Tooltip("Audio")]
    [SerializeField]
    private AudioSource creatureUltraVoice;
    [SerializeField]
    private AudioClip questGiveClip;
    
    private bool questTimedOut = false;
    private bool questCompleted = false;
    private bool questStarted = false;
    private float questLength = 120f;
    private float range = 20f;
    public PlayerControllerB DuckTargetPlayer
    {
        get;
        set;
    }
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
        LogIfDebugBuild("Duck Spawned.");
        ChangeSpeedClientRpc(1f);
        DoAnimationClientRpc(Animations.startSpawn.ToAnimationName());
        StartCoroutine(DoSpawning());
        SwitchToBehaviourClientRpc((int)State.Spawning);
    }

    private IEnumerator DoSpawning() {
        // Play spawning sound on the audio source's awake
        creatureUltraVoice.Play();
        yield return new WaitForSeconds(2f); // I dont have a spawn animation embedded yet.
        StartSearch(transform.position);
        ChangeSpeedClientRpc(3f);
        DoAnimationClientRpc(Animations.startWalk.ToAnimationName());
        SwitchToBehaviourClientRpc((int)State.Wandering);
        // play a sound for wandering
    }

    private void DoWandering() {
        // Play the ambient karaoke song version
        if (!FindClosestPlayerInRange(range)) return;
        DoAnimationClientRpc(Animations.startApproach.ToAnimationName());
        ChangeSpeedClientRpc(6f);
        StopSearch(currentSearch); // might have to rpc this?
        SwitchToBehaviourClientRpc((int)State.Approaching);
        // play a sound for approaching
    }

    private void DoApproaching() {
        if (Vector3.Distance(transform.position, DuckTargetPlayer.transform.position) < 3f && !questStarted) {
            questStarted = true;
            DoAnimationClientRpc(Animations.startGiveQuest.ToAnimationName());
            StartCoroutine(DoGiveQuest());
        }
        SetDestinationToPosition(DuckTargetPlayer.transform.position); // might have to rpc this?
        // approach and keep up with the player
    }

    private IEnumerator DoGiveQuest() {
        // Finishes approaching the target player and gives a quest
        yield return new WaitForSeconds(5f); // don't got a questGiveClip length
        DoAnimationClientRpc(Animations.startQuest.ToAnimationName());
        questStarted = true;
        ChangeSpeedClientRpc(6f);
        // pick a vent and get it's position and infront of the vent.
        CodeRebirthUtils.Instance.SpawnScrapServerRpc("Meteorite", RoundManager.Instance.allEnemyVents[UnityEngine.Random.Range(0, RoundManager.Instance.allEnemyVents.Length)].transform.position + transform.forward * 5f); // I don't have a grape scrap yet so spawn meteorite.
        StartCoroutine(QuestTimer());
        SwitchToBehaviourClientRpc((int)State.OngoingQuest);
    }
    private IEnumerator QuestTimer() {
        yield return new WaitForSeconds(questLength);
        questTimedOut = true; // won't have to rpc this
    }
    private void DoOngoingQuest() {
        // Chase the player around as they try to find the grape/scrap that was spawned.
        if (DuckTargetPlayer == null || DuckTargetPlayer.isPlayerDead || !DuckTargetPlayer.IsSpawned || !DuckTargetPlayer.isPlayerControlled) {
            DoCompleteQuest(QuestCompletion.Null);
            return;
        }
        if (questTimedOut) {
            DoCompleteQuest(QuestCompletion.TimedOut);
        }
        if (DuckTargetPlayer.currentlyHeldObject != null && DuckTargetPlayer.currentlyHeldObject.itemProperties.itemName == "Grape") {
            DoCompleteQuest(QuestCompletion.Completed);
        }
        SetDestinationToPosition(DuckTargetPlayer.transform.position); // might have to rpc this?
    }

    private void DoCompleteQuest(QuestCompletion reason) {
        // Decide if quest was completed correctly and Decide whether to end the quest for grape or 10% chance to keep going once more for lemonade.
        switch(reason)
        {
            case QuestCompletion.TimedOut:
                {
                    DuckTargetPlayer.DamagePlayer(500, true, true, CauseOfDeath.Strangulation, 0, false, default);
                    return;
                }
            case QuestCompletion.Completed: // Means quest was successful
                {
                    // Play the audio for success
                    break;
                }
            case QuestCompletion.Null:
                {
                    LogIfDebugBuild("Target Player is null?");
                    // play confused audio on where the player went.
                    break;
                }
        }
        questCompleted = true;
        DoAnimationClientRpc(Animations.startWalk.ToAnimationName());

        if (UnityEngine.Random.Range(0, 100) < 10 && IsHost && reason == QuestCompletion.Completed) {
            questStarted = false;
            questTimedOut = false;
            SwitchToBehaviourClientRpc((int)State.Wandering);
            DoAnimationClientRpc("startWalk");
            return;
        }
        ChangeSpeedClientRpc(4f);
        SwitchToBehaviourClientRpc((int)State.Docile);
        StartSearch(transform.position); // might have to rpc this?
        DoAnimationClientRpc("startWalk");
    }

    private void DoDocile() {
        // Completely docile, plays the song in the background kinda quieter and just does the same as wandering but without targetting a player.

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
            bool onSight = player.IsSpawned && player.isPlayerControlled && !player.isPlayerDead && !player.isInHangarShipRoom && DuckHasLineOfSightToPosition(player.transform.position, 45f, range);
            if (!onSight) continue;

            float distance = Vector3.Distance(transform.position, player.transform.position);
            bool closer = distance < minDistance;
            if (!closer) continue;

            minDistance = distance;
            closestPlayer = player;
        }
        if (closestPlayer == null) return false;

        DuckTargetPlayer = closestPlayer;
        return true;
    }

    private bool DuckHasLineOfSightToPosition(Vector3 pos, float width = 60f, float range = 20f, float proximityAwareness = 3f) {
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
