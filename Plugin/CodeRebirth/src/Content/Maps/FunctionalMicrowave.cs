using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.Content.Items;
using CodeRebirth.src.MiscScripts;
using CodeRebirth.src.Util;
using CodeRebirthLib.ContentManagement.Achievements;
using CodeRebirthLib.ContentManagement.Items;
using CodeRebirthLib.Extensions;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace CodeRebirth.src.Content.Maps;
public class FunctionalMicrowave : CodeRebirthHazard
{
    public float microwaveOpeningTimer = 15f;
    public float microwaveClosingTimer = 7.5f;
    public Collider mainCollider = null!;
    public float hinderedMultiplier = 1.5f;
    public int damageAmount = 3;
    public float damageTimer = 0.1f;
    public Animator animator = null!;
    public NavMeshAgent agent = null!;
    public float Speed = 3f;
    public float TurnSpeed = 10f;
    public AudioSource microwaveAudioSource = null!;
    public AudioClip microwaveOpenSound = null!;
    public AudioClip microwaveCloseSound = null!;
    public Transform scrapSpawnPoint = null!;

    private int originalDamageAmount = 0;
    private bool spawnedWithScrap = false;
    private GrabbableObject? scrapSpawned = null;
    private float movingTimer = 30f;
    private bool movingForAWhile = false;
    private float microwaveOpening = 0f;
    private float microwaveClosing = 0f;
    private bool isOpen = true;
    private float damageTimerDecrease = 0f;
    private Vector3 newDestination = default;
    private List<PlayerControllerB> playersAffected = new();
    private System.Random microwaveRandom = new System.Random();

    public override void Start()
    {
        base.Start();
        Plugin.ExtendedLogging("Functional Microwave initialized");
        microwaveClosing = microwaveClosingTimer;
        microwaveOpening = microwaveOpeningTimer;
        animator.SetBool("isActivated", isOpen);
        agent.speed = Speed;
        agent.acceleration = 5f;
        agent.angularSpeed = TurnSpeed;
        Item? scrapToSpawn = ChooseRandomMicrowaveScrap();
        if (scrapToSpawn == null) return;
        spawnedWithScrap = true;
        originalDamageAmount = damageAmount;
        damageAmount = 10;
        if (IsServer)
        {
            NetworkObjectReference spawnedScrap = CodeRebirthUtils.Instance.SpawnScrap(scrapToSpawn, scrapSpawnPoint.position, false, false, 0);
            scrapSpawned = ((GameObject)spawnedScrap).GetComponent<GrabbableObject>();
            scrapSpawned.grabbable = false;
            scrapSpawned.parentObject = scrapSpawnPoint;
            newDestination = RoundManager.Instance.insideAINodes[Random.Range(0, RoundManager.Instance.insideAINodes.Length)].transform.position;
            SyncScrapStuffClientRpc(spawnedScrap);
        }
    }

    [ClientRpc]
    private void SyncScrapStuffClientRpc(NetworkObjectReference spawnedScrap)
    {
        scrapSpawned = ((GameObject)spawnedScrap).GetComponent<GrabbableObject>();
        scrapSpawned.grabbable = false;
        scrapSpawned.parentObject = scrapSpawnPoint;
    }

    private Item? ChooseRandomMicrowaveScrap()
    {
        int result = microwaveRandom.Next(5);
        var itemRegistry = Plugin.Mod.ItemRegistry();
        if (result == 0)
        {
            itemRegistry.TryGetFromItemName("Spork", out CRItemDefinition? sporkItemDefinition);
            return sporkItemDefinition?.Item;
        }
        else if (result == 1)
        {
            itemRegistry.TryGetFromItemName("Fork", out CRItemDefinition? forkItemDefinition);
            return forkItemDefinition?.Item;
        }
        else if (result == 2)
        {
            itemRegistry.TryGetFromItemName("Baby", out CRItemDefinition? charredItemDefinition);
            return charredItemDefinition?.Item;
        }
        else if (result == 3)
        {
            itemRegistry.TryGetFromItemName("Sapsucker Omelette", out CRItemDefinition? normalItemDefinition);
            return normalItemDefinition?.Item;
        }
        return null;
    }

    public void Update()
    {
        UpdateAudio();
        damageTimerDecrease -= Time.deltaTime;
        movingTimer -= Time.deltaTime;
        if (scrapSpawned != null && (scrapSpawned.isHeld || scrapSpawned.playerHeldBy != null))
        {
            scrapSpawned.grabbable = true;
            if (scrapSpawned.playerHeldBy != null && scrapSpawned.playerHeldBy.IsLocalPlayer())
            {
                Plugin.Mod.AchievementRegistry().TryDiscoverMoreProgressAchievement("Lunch is Served", scrapSpawned.itemProperties.itemName);
            }
            damageAmount = originalDamageAmount;
            scrapSpawned = null;
        }
        if (movingTimer < 0f)
        {
            movingForAWhile = true;
            movingTimer = 30f;
        }
        if (!isOpen)
        {
            microwaveOpening += Time.deltaTime;
            if (microwaveOpening >= microwaveOpeningTimer)
            {
                if (scrapSpawned != null) scrapSpawned.grabbable = true;
                microwaveOpening = 0f;
                isOpen = true;
                mainCollider.enabled = true;
                microwaveAudioSource.PlayOneShot(microwaveOpenSound);
                animator.SetBool("isActivated", isOpen);
                if (scrapSpawned != null) CRUtilities.CreateExplosion(scrapSpawned.transform.position, true, 20, 0, 3, 2, null, null, 5f);
            }
        }
        else
        {
            microwaveClosing += Time.deltaTime;
            if (microwaveClosing >= microwaveClosingTimer)
            {
                if (scrapSpawned != null) scrapSpawned.grabbable = false;
                microwaveClosing = 0f;
                isOpen = false;
                mainCollider.enabled = false;
                microwaveAudioSource.PlayOneShot(microwaveCloseSound);
                foreach (PlayerControllerB player in playersAffected)
                {
                    player.movementSpeed *= hinderedMultiplier;
                }
                playersAffected.Clear();
                animator.SetBool("isActivated", isOpen);
            }
        }

        if (!IsServer) return;
        if (agent.remainingDistance < 1f || movingForAWhile)
        {
            movingForAWhile = false;
            agent.SetDestination(newDestination);
            newDestination = RoundManager.Instance.insideAINodes[Random.Range(0, RoundManager.Instance.insideAINodes.Length)].transform.position;
        }
    }

    private void UpdateAudio()
    {
        microwaveAudioSource.volume = Plugin.ModConfig.ConfigMicrowaveVolume.Value;
    }

    public void OnColliderEnter(Collider other)
    {
        if (other.TryGetComponent(out PlayerControllerB playerControllerB))
        {
            if (!playersAffected.Contains(playerControllerB))
            {
                playersAffected.Add(playerControllerB);
                playerControllerB.movementSpeed /= hinderedMultiplier;
            }
        }
    }

    public void OnColliderStay(Collider other)
    {
        if (other.TryGetComponent(out PlayerControllerB playerControllerB))
        {
            if (damageTimerDecrease <= 0f)
            {
                damageTimerDecrease = damageTimer;
                playerControllerB.DamagePlayer(damageAmount, true, true, CauseOfDeath.Burning, 0, false, default);
            }
        }
    }

    public void OnColliderExit(Collider other)
    {
        if (other.TryGetComponent(out PlayerControllerB playerControllerB))
        {
            if (playersAffected.Contains(playerControllerB))
            {
                playersAffected.Remove(playerControllerB);
                playerControllerB.movementSpeed *= hinderedMultiplier;
            }
        }
    }
}