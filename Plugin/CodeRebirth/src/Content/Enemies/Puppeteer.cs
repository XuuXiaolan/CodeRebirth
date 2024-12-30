using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.MiscScripts;
using CodeRebirth.src.Util;
using GameNetcodeStuff;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace CodeRebirth.src.Content.Enemies;
public class Puppeteer : CodeRebirthEnemyAI // todo: unity animation events
{
    /*
        * Puppeteer :
        * - Spawns in and starts to sneak up on players like a bracken
        * - Once near player, stabs them with the needle dealing no damage 
        *   but creating a puppet of said player, then teleports away.
        * - The player's puppet follows their paired player; each point of damage
        *   the puppet takes deals 20-30 dmg to the player.
        * - The puppet can be kicked around like a football (for fun).
        * - The puppeteer can be killed, a bit tankier than a butler. Once attacked:
        *   1. Creates a puppet of the attacker and vanishes if a non-puppet 
        *      player hits them.
        *   2. Then tries to kill the puppet (cannot directly harm the player).
        *   Has 2 attacks:
        *      - Lunge (striking with the needle)
        *      - Defensive mask that reflects all damage (cannot attack during mask).
        * - On death: 
        *   - All current puppets become scrap 
        *   - Players are freed from the voodoo effect 
        *   - Drops pin needle weapon 
        */

    [Header("General Configuration")]
    public PuppetDamageDealer puppetDamageDealer = null!;
    public Transform playerStabPosition = null!;

    [Header("Puppeteer Configuration")]
    public float sneakSpeed = 1.5f;
    public float chaseSpeed = 3.0f;
    public float detectionRange = 20f;
    public int puppetDamageToPlayerMultiplier = 20;
    public GameObject puppetPrefab = null!;   // Assign in the inspector
    public Transform needleAttachPoint = null!; // Where puppet spawns or is pinned

    [Header("Audio & Animation")]
    public NetworkAnimator networkAnimator = null!;
    public AudioClip stabSound = null!;
    public AudioClip teleportSound = null!;
    public AudioClip maskDefensiveSound = null!;
    public AudioClip puppetSpawnSound = null!;
    public AudioClip puppeteerDeathSound = null!;
    public AudioClip reflectSound = null!;

    [HideInInspector] public bool isAttacking = false;
    private bool enteredDefensiveModeOnce = false;
    private PlayerControllerB? currentlyGraspedPlayer = null;
    private static int instanceCount = 0;

    private Dictionary<PlayerControllerB, GameObject> playerPuppetMap = new();
    private static readonly int DoStabAnimation = Animator.StringToHash("doStab"); // Trigger
    private static readonly int DoSwipeAnimation = Animator.StringToHash("doSwipe"); // Trigger
    private static readonly int DoHitAnimation = Animator.StringToHash("isHit"); // Trigger
    private static readonly int DoGrabPlayerAnimation = Animator.StringToHash("doGrabPlayer"); // Trigger
    private static readonly int MaskPhaseAnimation = Animator.StringToHash("maskPhase"); // Bool
    private static readonly int InCombatAnimation = Animator.StringToHash("inCombat"); // Bool
    private static readonly int DeadAnimation = Animator.StringToHash("isDead"); // Bool
    private static readonly int RunSpeedFloat = Animator.StringToHash("RunSpeed"); // Float

    private enum PuppeteerState
    {
        Spawn,
        Sneaking,
        Attacking,
        DefensiveMask,
        Dead
    }

    public override void Start()
    {
        base.Start();
        instanceCount++;

        agent.speed = 0f;
        SwitchToBehaviourStateOnLocalClient((int)PuppeteerState.Spawn);
    }

    public override void Update()
    {
        base.Update();
        if (isEnemyDead) return;
    }

    public override void DoAIInterval()
    {
        base.DoAIInterval();
        if (isEnemyDead) return;

        switch (currentBehaviourStateIndex)
        {
            case (int)PuppeteerState.Spawn:
                // Maybe play a spawn animation, or a short delay before we start sneaking
                break;
            case (int)PuppeteerState.Sneaking:
                DoSneakingUpdate();
                break;
            case (int)PuppeteerState.Attacking:
                DoAttackingUpdate();
                break;
            case (int)PuppeteerState.DefensiveMask:
                // Possibly reflect incoming damage or block it
                break;
            case (int)PuppeteerState.Dead:
                // Do Nothing
                break;
        }
    }

    private void DoSneakingUpdate()
    {
        PlayerControllerB? nearestPlayer = GetNearestPlayerWithinRange(detectionRange);
        if (nearestPlayer == null) return;
        // Move toward that player
        smartAgentNavigator.DoPathingToDestination(nearestPlayer.transform.position, nearestPlayer.isInsideFactory, true, nearestPlayer);
        if (Vector3.Distance(nearestPlayer.transform.position, this.transform.position) <= 5)
        {
            // "Stab" (no damage, but spawn puppet)
            creatureSFX.PlayOneShot(stabSound);

            CreatePlayerPuppet(nearestPlayer);
            TeleportAway();
        }
    }

    private void DoAttackingUpdate()
    {
        if (isAttacking) return;
        if (targetPlayer == null || targetPlayer.isPlayerDead || !targetPlayer.isPlayerControlled)
        {
            Plugin.ExtendedLogging("Target player is dead or not controlled, stopping attack.");
            SwitchToBehaviourStateOnLocalClient((int)PuppeteerState.Sneaking);
            return;
        }
        // Once we get within a certain distance of the player, "stab" them
        GameObject? targetPlayerPuppet = playerPuppetMap[targetPlayer];
        if (targetPlayerPuppet == null) return;
        smartAgentNavigator.DoPathingToDestination(targetPlayerPuppet.transform.position, true, false, null);
        if (Vector3.Distance(targetPlayerPuppet.transform.position, this.transform.position) <= 2)
        {
            networkAnimator.SetTrigger(DoSwipeAnimation);
            isAttacking = true;
        }
        else if (Vector3.Distance(targetPlayerPuppet.transform.position, this.transform.position) <= 5)
        {
            networkAnimator.SetTrigger(DoStabAnimation);
            isAttacking = true;
        }
    }

    private void CreatePlayerPuppet(PlayerControllerB player)
    {
        if (!playerPuppetMap.ContainsKey(player))
        {
            if (!IsServer) return;
            GameObject puppetObj = Instantiate(puppetPrefab, needleAttachPoint.position, Quaternion.identity);
            puppetObj.GetComponent<NetworkObject>()?.Spawn(true);
            CreatePlayerPuppetServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player), new NetworkObjectReference(puppetObj));
        }
        else
        {
            Plugin.Logger.LogError("Tried to create puppet of already puppeted player???");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void CreatePlayerPuppetServerRpc(int playerIndex, NetworkObjectReference netObj)
    {
        CreatePlayerPuppetClientRpc(playerIndex, netObj);
    }

    [ClientRpc]
    private void CreatePlayerPuppetClientRpc(int playerIndex, NetworkObjectReference netObj)
    {
        PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[playerIndex];
        creatureVoice.PlayOneShot(puppetSpawnSound);

        // Link puppet to player
        GameObject puppetObj = (GameObject)netObj;
        PuppeteersVoodoo puppetComp = puppetObj.GetComponent<PuppeteersVoodoo>();
        puppetComp?.Init(player, this, puppetDamageToPlayerMultiplier);

        playerPuppetMap[player] = puppetObj;
    }

    private void TeleportAway()
    {
        creatureSFX.PlayOneShot(teleportSound);

        // Simple approach: get a random far point in facility
        Vector3 randomFarPoint = GetRandomFarPointInFacility(new List<Vector3> { transform.position });

        // Teleport
        agent.Warp(randomFarPoint);

        SwitchToBehaviourServerRpc((int)PuppeteerState.Sneaking);
    }

    public Vector3 GetRandomFarPointInFacility(List<Vector3> pointsToStayAwayFrom)
    {
        List<Vector3> possibleDestinations = new();
        foreach (var gameObject in RoundManager.Instance.insideAINodes)
        {
            Vector3 nodePos = gameObject.transform.position;
            bool isFar = pointsToStayAwayFrom.All(pt => Vector3.Distance(pt, nodePos) > 30f);
            if (isFar)
            {
                possibleDestinations.Add(nodePos);
            }
        }
        if (possibleDestinations.Count == 0)
        {
            return transform.position;
        }
        return possibleDestinations[UnityEngine.Random.Range(0, possibleDestinations.Count)];
    }

    public override void HitEnemy(int force = 1, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
        if (isEnemyDead || playerWhoHit == null) return;

        if (currentBehaviourStateIndex == (int)PuppeteerState.DefensiveMask)
        {
            // Reflect incoming damage
            creatureSFX.PlayOneShot(reflectSound);
            playerWhoHit.DamagePlayer(force * 25, true, false, CauseOfDeath.Unknown, 0, false, default);
            return;
        }
        if (currentBehaviourStateIndex != (int)PuppeteerState.Attacking)
        {
            if (playerPuppetMap.ContainsKey(playerWhoHit))
            {
                targetPlayer = playerWhoHit;
                agent.speed = chaseSpeed;
                SwitchToBehaviourStateOnLocalClient((int)PuppeteerState.Attacking);
                // enters combat mode.
            }
            else
            {
                // Do the grab animation and puppet em.
                networkAnimator.SetTrigger(DoGrabPlayerAnimation);
                currentlyGraspedPlayer = playerWhoHit;
                currentlyGraspedPlayer.disableMoveInput = true;
                currentlyGraspedPlayer.disableLookInput = true;
                currentlyGraspedPlayer.transform.position = playerStabPosition.position;
                currentlyGraspedPlayer.transform.rotation = playerStabPosition.rotation;
                return;
            }
        }
        else
        {
            targetPlayer = playerWhoHit;
        }
        enemyHP -= force;
        if (enemyHP <= 0 && !isEnemyDead)
        {
            if (!enteredDefensiveModeOnce)
            {
                enteredDefensiveModeOnce = true;
                enemyHP++;
                creatureVoice.PlayOneShot(maskDefensiveSound);
                agent.speed = 0f;
                SwitchToBehaviourStateOnLocalClient((int)PuppeteerState.DefensiveMask);
                StartCoroutine(SwitchToStateAfterDelay(PuppeteerState.Attacking, 2f));
            }
            // Check if it's entered defensive mode before, if not, enter it and increase health and make em immune.
            if (IsOwner)
            {
                KillEnemyOnOwnerClient();
            }
            return;
        }
    }

    private IEnumerator SwitchToStateAfterDelay(PuppeteerState state, float delay)
    {
        yield return new WaitForSeconds(delay);
        agent.speed = chaseSpeed;
        SwitchToBehaviourStateOnLocalClient((int)state);
    }

    public override void KillEnemy(bool destroy = false)
    {
        base.KillEnemy(destroy);
        // play death animation.
        creatureVoice.PlayOneShot(puppeteerDeathSound);

        // Make all puppets scrap
        if (!IsServer) return;
        foreach (var kvp in playerPuppetMap)
        {
            // We can simply destroy them or spawn scrap items
            GameObject puppetObj = kvp.Value;
            if (puppetObj != null && puppetObj.TryGetComponent(out PuppeteersVoodoo puppetComp))
            {
                StartCoroutine(puppetComp.BreakDoll()); // remove link
            }
        }
        playerPuppetMap.Clear();
        CodeRebirthUtils.Instance.SpawnScrapServerRpc("PuppeteerNeedle", transform.position);
    }

    private PlayerControllerB? GetNearestPlayerWithinRange(float range)
    {
        float minDistance = float.MaxValue;
        PlayerControllerB? target = null;
        foreach (var player in StartOfRound.Instance.allPlayerScripts)
        {
            if (!player.isPlayerControlled || player.isPlayerDead || playerPuppetMap.ContainsKey(player)) 
                continue;

            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance < range && distance < minDistance)
            {
                minDistance = distance;
                target = player;
            }
        }
        return target;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        instanceCount--;
    }

    #region Animation Events
    public void SpawnAnimationTransitionAnimEvent()
    {
        agent.speed = sneakSpeed;
        SwitchToBehaviourStateOnLocalClient((int)PuppeteerState.Sneaking);
    }

    public void PuppetPlayerAnimEvent()
    {
        if (currentlyGraspedPlayer == null)
        {
            Plugin.Logger.LogError("No player to grab???");
            return;
        }
        CreatePlayerPuppet(currentlyGraspedPlayer);
    }

    public void ReleasePlayerAnimEvent()
    {
        if (currentlyGraspedPlayer == null)
        {
            Plugin.Logger.LogError("No player to release???");
            return;
        }
        currentlyGraspedPlayer.disableMoveInput = false;
        currentlyGraspedPlayer.disableLookInput = false;
        currentlyGraspedPlayer = null;
    }

    public void TeleportEnemyAnimEvent()
    {
        TeleportAway();
    }
    #endregion
}