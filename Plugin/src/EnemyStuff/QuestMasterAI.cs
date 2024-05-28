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
public class QuestMasterAI : CodeRebirthEnemyAI
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
        startSucceedQuest,
    }
}
public class QuestItem : MonoBehaviour
{
}