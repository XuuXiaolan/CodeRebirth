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
    private PlayerControllerB? targetPlayerToNeedle = null;
    private static int instanceCount = 0;
    private PlayerControllerB? priorityPlayer = null;

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
        Idle,
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
        if (targetPlayer == null) return;
        GameObject? targetPlayerPuppet = playerPuppetMap[targetPlayer];
        if (targetPlayerPuppet == null) return;
        Vector3 direction = (targetPlayerPuppet.transform.position - transform.position).normalized;
        direction.y = 0f; // Flatten the direction so we only rotate on the Y-axis
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            // Tweak rotation speed as desired:
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, 5f * Time.deltaTime);
        }
    }

    public override void DoAIInterval()
    {
        base.DoAIInterval();
        if (isEnemyDead) return;
        creatureAnimator.SetFloat(RunSpeedFloat, agent.velocity.magnitude);
        switch (currentBehaviourStateIndex)
        {
            case (int)PuppeteerState.Spawn:
                // Maybe play a spawn animation, or a short delay before we start sneaking
                break;
            case (int)PuppeteerState.Idle:
                DoIdleUpdate();
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

    private void DoIdleUpdate()
    {
        PlayerControllerB? nearestPlayer = GetNearestPlayerWithinRange(detectionRange);
        if (nearestPlayer != null || priorityPlayer != null)
        {
            smartAgentNavigator.StopSearchRoutine();
            SwitchToBehaviourServerRpc((int)PuppeteerState.Sneaking);
            return;
        }
    }

    private void DoSneakingUpdate()
    {
        PlayerControllerB? nearestPlayer = GetNearestPlayerWithinRange(detectionRange+10);
        if (priorityPlayer != null)
        {
            nearestPlayer = priorityPlayer;
        }
        if (nearestPlayer == null)
        {
            smartAgentNavigator.StartSearchRoutine(this.transform.position, 40);
            SwitchToBehaviourServerRpc((int)PuppeteerState.Idle);
            return;
        }
        smartAgentNavigator.DoPathingToDestination(nearestPlayer.transform.position, nearestPlayer.isInsideFactory, true, nearestPlayer);
        if (agent.speed != 0 && Vector3.Distance(nearestPlayer.transform.position, this.transform.position) <= 3)
        {
            SetTargetNeedlePlayerClientRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, nearestPlayer));
            creatureSFX.PlayOneShot(stabSound); // todo: rpc this
            agent.speed = 0f;
            Plugin.ExtendedLogging("Grabbing player!");
            networkAnimator.SetTrigger(DoGrabPlayerAnimation);
        }
    }

    private void DoAttackingUpdate()
    {
        if (targetPlayer == null || targetPlayer.isPlayerDead || !targetPlayer.isPlayerControlled)
        {
            Plugin.ExtendedLogging("Target player is dead or not controlled, stopping attack.");
            creatureAnimator.SetBool(InCombatAnimation, false);
            smartAgentNavigator.StartSearchRoutine(this.transform.position, 40);
            SwitchToBehaviourServerRpc((int)PuppeteerState.Idle);
            return;
        }
        GameObject? targetPlayerPuppet = playerPuppetMap[targetPlayer];
        if (targetPlayerPuppet == null) return;
        if (isAttacking) return;
        smartAgentNavigator.DoPathingToDestination(targetPlayerPuppet.transform.position, true, false, null);

        float distance = Vector3.Distance(targetPlayerPuppet.transform.position, transform.position);
        if (distance <= 2f)
        {
            agent.speed = chaseSpeed/4;
            networkAnimator.SetTrigger(DoSwipeAnimation);
            isAttacking = true;
        }
        else if (distance <= 5f)
        {
            agent.speed = chaseSpeed/4;
            networkAnimator.SetTrigger(DoStabAnimation);
            isAttacking = true;
        }
    }

    private void CreatePlayerPuppet(PlayerControllerB player)
    {
        if (!playerPuppetMap.ContainsKey(player))
        {
            if (!IsServer) return;
            GameObject puppetObj = Instantiate(EnemyHandler.Instance.ManorLord.PuppeteerPuppetPrefab, needleAttachPoint.position, Quaternion.identity);
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

        if (!IsServer) return;
        Vector3 randomFarPoint = GetRandomFarPointInFacility(new List<Vector3> { transform.position });

        // Teleport
        agent.Warp(randomFarPoint);
        SwitchToBehaviourServerRpc((int)PuppeteerState.Idle);
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
                if (IsServer) creatureAnimator.SetBool(InCombatAnimation, true);
                smartAgentNavigator.StopSearchRoutine();
                SwitchToBehaviourStateOnLocalClient((int)PuppeteerState.Attacking);
                // enters combat mode.
            }
            else
            {
                priorityPlayer = playerWhoHit;
                return;
            }
        }
        else
        {
            if (playerPuppetMap.ContainsKey(playerWhoHit))
            {
                targetPlayer = playerWhoHit;
            }
            else
            {
                priorityPlayer = playerWhoHit;
                return;
            }
        }
        enemyHP -= force;
        if (IsServer) networkAnimator.SetTrigger(DoHitAnimation);
        if (enemyHP <= 0 && !isEnemyDead)
        {
            if (!enteredDefensiveModeOnce)
            {
                enteredDefensiveModeOnce = true;
                enemyHP++;
                creatureVoice.PlayOneShot(maskDefensiveSound);
                agent.speed = 0f;
                SwitchToBehaviourStateOnLocalClient((int)PuppeteerState.DefensiveMask);
                if (IsServer) creatureAnimator.SetBool(MaskPhaseAnimation, true);
                agent.speed = 0f;
                StartCoroutine(SwitchToStateAfterDelay(PuppeteerState.Attacking, 5f));
            }
            // Check if it's entered defensive mode before, if not, enter it and increase health and make em immune.
            if (IsOwner)
            {
                KillEnemyOnOwnerClient();
            }
            return;
        }
    }

    [ClientRpc]
    public void SetTargetNeedlePlayerClientRpc(int playerIndex)
    {
        targetPlayerToNeedle = StartOfRound.Instance.allPlayerScripts[playerIndex];
        targetPlayerToNeedle.disableMoveInput = true;
        targetPlayerToNeedle.disableLookInput = true;
        targetPlayerToNeedle.transform.position = playerStabPosition.position;
        targetPlayerToNeedle.transform.rotation = playerStabPosition.rotation;
    }

    private IEnumerator SwitchToStateAfterDelay(PuppeteerState state, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (state == PuppeteerState.Attacking) smartAgentNavigator.StopSearchRoutine();
        if (IsServer)
        {
            creatureAnimator.SetBool(InCombatAnimation, state == PuppeteerState.Attacking);
            creatureAnimator.SetBool(MaskPhaseAnimation, false);
        }
        agent.speed = chaseSpeed;
        SwitchToBehaviourStateOnLocalClient((int)state);
    }

    public override void KillEnemy(bool destroy = false)
    {
        if (enemyHP > 0) return;
        base.KillEnemy(destroy);
        // play death animation.
        creatureVoice.PlayOneShot(puppeteerDeathSound);

        // Make all puppets scrap
        agent.enabled = false;
        smartAgentNavigator.enabled = false;
        if (!IsServer) return;
        creatureAnimator.SetBool(DeadAnimation, true);
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
        targetPlayer = null;
        agent.speed = sneakSpeed;
        if (IsServer) smartAgentNavigator.StartSearchRoutine(transform.position, 40);
        SwitchToBehaviourStateOnLocalClient((int)PuppeteerState.Idle);
    }

    public void PuppetPlayerAnimEvent()
    {
        if (targetPlayerToNeedle == null)
        {
            Plugin.Logger.LogError("No player to grab???");
            return;
        }
        CreatePlayerPuppet(targetPlayerToNeedle);
    }

    public void ReleasePlayerAnimEvent()
    {
        if (targetPlayerToNeedle == null)
        {
            Plugin.Logger.LogError("No player to release???");
            return;
        }
        targetPlayerToNeedle.disableMoveInput = false;
        targetPlayerToNeedle.disableLookInput = false;
        if (targetPlayerToNeedle == priorityPlayer)
        {
            priorityPlayer = null;
        }
        targetPlayerToNeedle = null;
        agent.speed = sneakSpeed;
    }

    public void TeleportEnemyAnimEvent()
    {
        TeleportAway();
    }

    public void EndAttackAnimEvent()
    {
        if (currentBehaviourStateIndex == (int)PuppeteerState.Attacking)
        {
            agent.speed = chaseSpeed;
        }
        else
        {
            agent.speed = sneakSpeed;
        }
        isAttacking = false;
    }
    #endregion
}