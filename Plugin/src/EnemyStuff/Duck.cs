using System;
using System.Collections;
using System.Diagnostics;
using CodeRebirth.src;
using GameNetcodeStuff;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;
using UnityEngine.Yoga;

namespace CodeRebirth.EnemyStuff;
public class Duck : EnemyAI
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
    [NonSerialized]
    private NetworkVariable<NetworkBehaviourReference> _playerNetVar = new();
    public PlayerControllerB DuckTargetPlayer
    {
        get
        {
            return (PlayerControllerB)_playerNetVar.Value;
        }
        set 
        {
            if (value == null)
            {
                _playerNetVar.Value = null;
            }
            else
            {
                _playerNetVar.Value = new NetworkBehaviourReference(value);
            }
        }
    }
    public enum State {
        Spawning,
        Wandering,
        Approaching,
        OngoingQuest,
        Docile,
    }

    [Conditional("DEBUG")]
    public void LogIfDebugBuild(string text) {
        Plugin.Logger.LogInfo(text);
    }

    public override void Start() {
        base.Start();
        LogIfDebugBuild("Duck Spawned.");
        agent.speed = 1f;
        DoAnimationClientRpc("startSpawn");
        DoSpawning();
        SwitchToBehaviourClientRpc((int)State.Spawning);
    }

    public override void Update() {
        base.Update();
        if (isEnemyDead) return;
    }

    private IEnumerator DoSpawning() {
        // Play spawning sound on the audio source's awake
        creatureUltraVoice.Play();
        yield return new WaitForSeconds(spawnAnimation.length);
        StartSearch(transform.position);
        DoAnimationClientRpc("startWalk");
        agent.speed = 3f;
        SwitchToBehaviourClientRpc((int)State.Wandering);
        // play a sound for wandering
    }

    private void DoWandering() {
        // Play the ambient karaoke song version
        if (FindClosestPlayerInRange(range)) {
            DoAnimationClientRpc("startApproach");
            agent.speed = 6f;
            SwitchToBehaviourClientRpc((int)State.Approaching);
            // play a sound for approaching
        }
    }

    private void DoApproaching() {
        if (Vector3.Distance(transform.position, DuckTargetPlayer.transform.position) < 3f && !questStarted) {
            questStarted = true;
            DoAnimationClientRpc("startGiveQuest");
            StartCoroutine(DoGiveQuest());
        }
        SetDestinationToPosition(DuckTargetPlayer.transform.position);
        // approach and keep up with the player
    }

    private IEnumerator DoGiveQuest() {
        // Finishes approaching the target player and gives a quest
        yield return new WaitForSeconds(questGiveClip.length);
        DoAnimationClientRpc("startQuest");
        questStarted = true;
        agent.speed = 6f;
        // pick a vent and get it's position and infront of the vent.
        CodeRebirthUtils.Instance.SpawnScrapServerRpc("Grape", RoundManager.Instance.allEnemyVents[UnityEngine.Random.Range(0, RoundManager.Instance.allEnemyVents.Length)].transform.position + transform.forward * 5f);
        StartCoroutine(QuestTimer());
        SwitchToBehaviourClientRpc((int)State.OngoingQuest);
    }
    private IEnumerator QuestTimer() {
        yield return new WaitForSeconds(questLength);
        questTimedOut = true;
    }
    private void DoOngoingQuest() {
        // Chase the player around as they try to find the grape/scrap that was spawned.
        if (DuckTargetPlayer == null || DuckTargetPlayer.isPlayerDead || !DuckTargetPlayer.IsSpawned || !DuckTargetPlayer.isPlayerControlled) {
            DoCompleteQuest("null");
            return;
        }
        if (questTimedOut) {
            DoCompleteQuest("timed out");
        }
        if (DuckTargetPlayer.currentlyHeldObject.itemProperties.itemName == "Grape") {
            DoCompleteQuest("success");
        }
        SetDestinationToPosition(DuckTargetPlayer.transform.position);
    }

    private void DoCompleteQuest(string reason) {
        // Decide if quest was completed correctly and Decide whether to end the quest for grape or 10% chance to keep going once more for lemonade.
        if (reason == "timed out") {
            // Kill the player and play the audio for the time out
            DuckTargetPlayer.DamagePlayer(500, true, true, CauseOfDeath.Strangulation, 0, false, default);
            return;
        } else if (reason == "success") { // Means quest was successful
            // Play the audio for success
        } else if (reason == "null") {
            LogIfDebugBuild("Target Player is null?");
            // play confused audio on where the player went.
        }
        if (UnityEngine.Random.Range(0, 100) < 10 && IsHost && !questCompleted && reason == "success") {
            questCompleted = true;
            questStarted = false;
            questTimedOut = false;
            SwitchToBehaviourClientRpc((int)State.Wandering);
            DoAnimationClientRpc("startWalk");
            return;
        }
        questCompleted = true;
        agent.speed = 4f;
        SwitchToBehaviourClientRpc((int)State.Docile);
        DoAnimationClientRpc("startWalk");
    }

    private void DoDocile() {
        // Completely docile, plays the song in the background kinda quieter etc.

    }
    public override void DoAIInterval() {
        base.DoAIInterval();
        if (isEnemyDead || StartOfRound.Instance.allPlayersDead) return;

        switch(currentBehaviourStateIndex) {
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
                LogIfDebugBuild("This Behavior State doesn't exist!");
                break;
        }
    }

    private bool FindClosestPlayerInRange(float range) {
        PlayerControllerB closestPlayer = null;
        float minDistance = float.MaxValue;

        foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts) {
            if (player.IsSpawned && player.isPlayerControlled && !player.isPlayerDead && !player.isInHangarShipRoom && DuckHasLineOfSightToPosition(player.transform.position, 45f, range)) {
                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance < minDistance) {
                    minDistance = distance;
                    closestPlayer = player;
                }
            }
        }
        if (closestPlayer != null) {
            DuckTargetPlayer = closestPlayer;
            return true;
        }
        return false;
    }

    private bool DuckHasLineOfSightToPosition(Vector3 pos, float width = 60f, float range = 20f, float proximityAwareness = 3f) {
        if (eye == null) {
            _ = transform;
        } else {
            _ = eye;
        }

        if (Vector3.Distance(eye.position, pos) < range && !Physics.Linecast(eye.position, pos, StartOfRound.Instance.collidersAndRoomMaskAndDefault)) {
            Vector3 to = pos - eye.position;
            if (Vector3.Angle(eye.forward, to) < width || Vector3.Distance(transform.position, pos) < proximityAwareness) {
                return true;
            }
        }
        return false;
    }
    [ClientRpc]
    private void DoAnimationClientRpc(string animationName) {
        LogIfDebugBuild(animationName);
        creatureAnimator.SetTrigger(animationName);
    }
}
