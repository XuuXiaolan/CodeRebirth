using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.Content.Items;
using CodeRebirth.src.MiscScripts;
using CodeRebirth.src.MiscScripts.DissolveEffect;
using CodeRebirth.src.Util;
using CodeRebirthLib.ContentManagement.Enemies;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Enemies;
public class Puppeteer : CodeRebirthEnemyAI
{
    [Header("General Configuration")]
    public PuppetDamageDealer puppetDamageDealer = null!;
    public Transform playerStabPosition = null!;

    [Header("Puppeteer Configuration")]
    public InteractiveEffect[] interactiveEffects = [];
    public float sneakSpeed = 1.5f;
    public float chaseSpeed = 3.0f;
    public float detectionRange = 20f;
    public int puppetDamageToPlayerMultiplier = 20;
    public Transform needleAttachPoint = null!; // Where puppet spawns or is pinned

    [Header("Audio & Animation")]
    public AudioClip[] normalFootstepSounds = [];
    public AudioClip[] combatFootstepSounds = [];
    public AudioClip grabPlayerSound = null!;
    public AudioClip makePuppetSound = null!;
    public AudioClip stabSound = null!;
    public AudioClip swipeSound = null!;
    public AudioClip maskDefensiveSound = null!;
    public AudioClip[] reflectSounds = null!;

    [HideInInspector] public bool isAttacking = false;
    private bool enteredDefensiveModeOnce = false;
    private PlayerControllerB? targetPlayerToNeedle = null;
    private PlayerControllerB? priorityPlayer = null;
    private float timeSinceLastTakenDamage = 0f;
    private bool teleporting = false;

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
        agent.speed = 0f;
        SwitchToBehaviourStateOnLocalClient((int)PuppeteerState.Spawn);
    }

    public override void Update()
    {
        base.Update();
        if (isEnemyDead) return;
        timeSinceLastTakenDamage += Time.deltaTime;
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
        PlayerControllerB? nearestPlayer = GetNearestPlayerWithinRange(detectionRange + 10);
        if (priorityPlayer != null)
        {
            nearestPlayer = priorityPlayer;
        }
        if (nearestPlayer == null)
        {
            smartAgentNavigator.StartSearchRoutine(40);
            SwitchToBehaviourServerRpc((int)PuppeteerState.Idle);
            return;
        }
        smartAgentNavigator.DoPathingToDestination(nearestPlayer.transform.position);
        if (agent.speed != 0 && Vector3.Distance(nearestPlayer.transform.position, this.transform.position) <= 3)
        {
            SetTargetNeedlePlayerClientRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, nearestPlayer));
            agent.speed = 0f;
            agent.velocity = Vector3.zero;
            Plugin.ExtendedLogging("Grabbing player!");
            creatureNetworkAnimator.SetTrigger(DoGrabPlayerAnimation);
            StartCoroutine(FixPlayerJustIncase(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, nearestPlayer)));
        }
    }

    private IEnumerator FixPlayerJustIncase(int playerIndex)
    {
        yield return new WaitForSeconds(4f);
        UnSetTargetNeedlePlayerClientRpc(playerIndex);
    }

    private void DoAttackingUpdate()
    {
        if (targetPlayer == null || targetPlayer.isPlayerDead || !targetPlayer.isPlayerControlled)
        {
            Plugin.ExtendedLogging("Target player is dead or not controlled, stopping attack.");
            creatureAnimator.SetBool(InCombatAnimation, false);
            smartAgentNavigator.StartSearchRoutine(40);
            SwitchToBehaviourServerRpc((int)PuppeteerState.Idle);
            return;
        }
        GameObject? targetPlayerPuppet = playerPuppetMap[targetPlayer];
        if (targetPlayerPuppet == null) return;
        if (isAttacking) return;
        smartAgentNavigator.DoPathingToDestination(targetPlayerPuppet.transform.position);

        float distance = Vector3.Distance(targetPlayerPuppet.transform.position, transform.position);
        if (distance <= 2f)
        {
            agent.speed = chaseSpeed / 4;
            PlayMiscSoundClientRpc(1);
            creatureNetworkAnimator.SetTrigger(DoSwipeAnimation);
            isAttacking = true;
        }
        else if (distance <= 5f)
        {
            agent.speed = chaseSpeed / 4;
            PlayMiscSoundClientRpc(0);
            creatureNetworkAnimator.SetTrigger(DoStabAnimation);
            isAttacking = true;
        }
    }

    [ClientRpc]
    private void PlayMiscSoundClientRpc(int index)
    {
        switch (index)
        {
            case 0:
                creatureSFX.PlayOneShot(stabSound);
                break;
            case 1:
                creatureSFX.PlayOneShot(swipeSound);
                break;
        }
    }

    private void CreatePlayerPuppet(PlayerControllerB player)
    {
        if (!playerPuppetMap.ContainsKey(player))
        {
            creatureSFX.PlayOneShot(makePuppetSound);
            if (!IsServer) return;
            if (EnemyHandler.Instance.ManorLord == null) return;
            GameObject puppetObj = Instantiate(EnemyHandler.Instance.ManorLord.PuppeteerPuppetPrefab, needleAttachPoint.position, Quaternion.identity);
            puppetObj.GetComponent<NetworkObject>().Spawn(true);
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

        // Link puppet to player
        GameObject puppetObj = (GameObject)netObj;
        puppetObj.transform.localScale = this.transform.localScale;
        PuppeteersVoodoo puppetComp = puppetObj.GetComponent<PuppeteersVoodoo>();
        puppetComp.transform.SetParent(StartOfRound.Instance.propsContainer);
        puppetComp.Init(player, this, puppetDamageToPlayerMultiplier);

        playerPuppetMap[player] = puppetObj;
    }

    private void TeleportAway()
    {
        if (IsServer)
        {
            Vector3 randomFarPoint = GetRandomFarPointInFacility(new List<Vector3> { transform.position });

            // Teleport
            agent.Warp(randomFarPoint);
            SwitchToBehaviourServerRpc((int)PuppeteerState.Idle);
        }
        teleporting = false;
    }

    public Vector3 GetRandomFarPointInFacility(List<Vector3> pointsToStayAwayFrom)
    {
        List<Vector3> possibleDestinations = new();
        foreach (var gameObject in RoundManager.Instance.insideAINodes)
        {
            Vector3 nodePos = gameObject.transform.position;
            bool isFar = pointsToStayAwayFrom.All(pt => Vector3.Distance(pt, nodePos) > 30f);
            if (!isFar)
                continue;

            possibleDestinations.Add(nodePos);
        }
        if (possibleDestinations.Count == 0)
        {
            foreach (var gameObject in RoundManager.Instance.outsideAINodes)
            {
                Vector3 nodePos = gameObject.transform.position;
                bool isFar = pointsToStayAwayFrom.All(pt => Vector3.Distance(pt, nodePos) > 30f);
                if (!isFar)
                    continue;

                possibleDestinations.Add(nodePos);
            }
            if (possibleDestinations.Count == 0)
                return transform.position;
        }
        return possibleDestinations[UnityEngine.Random.Range(0, possibleDestinations.Count)];
    }

    public override void HitEnemy(int force = 1, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
        if (isEnemyDead || playerWhoHit == null || teleporting) return;
        if (timeSinceLastTakenDamage <= 1f) return;
        if (currentBehaviourStateIndex == (int)PuppeteerState.DefensiveMask)
        {
            // Reflect incoming damage
            creatureSFX.PlayOneShot(reflectSounds[enemyRandom.Next(reflectSounds.Length)]);
            playerWhoHit.DamagePlayer(force * 25, true, false, CauseOfDeath.Unknown, 0, false, default);
            return;
        }
        force = 1;
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
        creatureVoice.PlayOneShot(_hitBodySounds[enemyRandom.Next(_hitBodySounds.Length)]);
        if (IsServer && currentBehaviourStateIndex != (int)PuppeteerState.Attacking) creatureNetworkAnimator.SetTrigger(DoHitAnimation);
        if (enemyHP <= 0 && !isEnemyDead)
        {
            timeSinceLastTakenDamage = 1f;
            // Check if it's entered defensive mode before, if not, enter it and increase health and make em immune.
            if (IsOwner)
            {
                KillEnemyOnOwnerClient();
            }
        }
    }

    [ClientRpc]
    public void SetTargetNeedlePlayerClientRpc(int playerIndex)
    {
        teleporting = true;
        targetPlayerToNeedle = StartOfRound.Instance.allPlayerScripts[playerIndex];
        if (targetPlayerToNeedle == GameNetworkManager.Instance.localPlayerController) creatureSFX.PlayOneShot(grabPlayerSound);
        targetPlayerToNeedle.disableMoveInput = true;
        targetPlayerToNeedle.disableLookInput = true;
        targetPlayerToNeedle.inAnimationWithEnemy = this;
        targetPlayerToNeedle.transform.SetPositionAndRotation(playerStabPosition.position, playerStabPosition.rotation);
    }

    [ClientRpc]
    public void UnSetTargetNeedlePlayerClientRpc(int playerIndex)
    {
        PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[playerIndex];
        if (targetPlayerToNeedle != player) return;
        targetPlayerToNeedle.disableMoveInput = false;
        targetPlayerToNeedle.disableLookInput = false;
        if (targetPlayerToNeedle.inAnimationWithEnemy == this) targetPlayerToNeedle.inAnimationWithEnemy = null;
        Plugin.ExtendedLogging($"{this} unsetting target player {player.playerUsername}");
        targetPlayerToNeedle = null;
    }

    private IEnumerator SwitchToStateAfterDelay(PuppeteerState state, float delay)
    {
        int randomNumber = enemyRandom.Next(100);
        Plugin.ExtendedLogging($"Random Number: {randomNumber}");
        if (IsServer)
        {
            if (randomNumber < 1)
            {
                RoundManager.Instance.SpawnEnemyGameObject(this.transform.position, -1, -1, CodeRebirthUtils.EnemyTypes.Where(x => x.enemyName == "Jester").FirstOrDefault());
            }
            else if (randomNumber < 20)
            {
                RoundManager.Instance.SpawnEnemyGameObject(this.transform.position, -1, -1, CodeRebirthUtils.EnemyTypes.Where(x => x.enemyName == "Nutcracker").FirstOrDefault());
            }
            else if (randomNumber < 30)
            {
                RoundManager.Instance.SpawnEnemyGameObject(this.transform.position, -1, -1, CodeRebirthUtils.EnemyTypes.Where(x => x.enemyName == "Butler").FirstOrDefault());
            }
            else
            {
                RoundManager.Instance.SpawnEnemyGameObject(this.transform.position, -1, -1, CodeRebirthUtils.EnemyTypes.Where(x => x.enemyName == "Masked").FirstOrDefault());
            }
            RoundManager.Instance.SpawnEnemyGameObject(this.transform.position, -1, -1, CodeRebirthUtils.EnemyTypes.Where(x => x.enemyName == "Masked").FirstOrDefault());
        }
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
        Plugin.ExtendedLogging("Puppeteer killed?");
        if (!enteredDefensiveModeOnce)
        {
            timeSinceLastTakenDamage = 0f;
            enteredDefensiveModeOnce = true;
            enemyHP++;
            creatureVoice.PlayOneShot(maskDefensiveSound);
            SwitchToBehaviourStateOnLocalClient((int)PuppeteerState.DefensiveMask);
            if (IsServer) creatureAnimator.SetBool(MaskPhaseAnimation, true);
            agent.speed = 0f;
            StartCoroutine(SwitchToStateAfterDelay(PuppeteerState.Attacking, maskDefensiveSound.length));
            return;
        }
        if (timeSinceLastTakenDamage < 0.5f) return;
        if (currentBehaviourStateIndex == (int)PuppeteerState.DefensiveMask)
        {
            CREnemyAdditionalData enemyAdditionalData = CREnemyAdditionalData.CreateOrGet(this);
            enemyAdditionalData.PlayerThatLastHit?.KillPlayer(enemyAdditionalData.PlayerThatLastHit.velocityLastFrame, true, CauseOfDeath.Burning, 6, default);
            return;
        }
        base.KillEnemy(destroy);
        // play death animation.
        creatureVoice.PlayOneShot(dieSFX);

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
                puppetComp.breakDollRoutine = StartCoroutine(puppetComp.BreakDoll()); // remove link
            }
        }
        playerPuppetMap.Clear();
        if (EnemyHandler.Instance.ManorLord == null) return;
        CodeRebirthUtils.Instance.SpawnScrapServerRpc(EnemyHandler.Instance.ManorLord.ItemDefinitions.GetCRItemDefinitionWithItemName("Needle")?.item.itemName, transform.position);
    }

    private PlayerControllerB? GetNearestPlayerWithinRange(float range)
    {
        float minDistance = float.MaxValue;
        PlayerControllerB? target = null;
        foreach (var player in StartOfRound.Instance.allPlayerScripts)
        {
            if (!player.isPlayerControlled || player.isPlayerDead || player.inAnimationWithEnemy != null || playerPuppetMap.ContainsKey(player))
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

    #region Animation Events
    public void ReverseDissolvingAnimEvent()
    {
        foreach (var interactiveEffect in interactiveEffects)
        {
            interactiveEffect.ResetEffect();
        }
    }

    public void StartDissolvingAnimEvent(float duration)
    {
        foreach (var interactiveEffect in interactiveEffects)
        {
            interactiveEffect.duration = duration;
            interactiveEffect.PlayEffect();
        }
    }

    public void PlayFootstepSoundAnimEvent()
    {
        if (currentBehaviourStateIndex == (int)PuppeteerState.Attacking)
        {
            creatureVoice.PlayOneShot(combatFootstepSounds[enemyRandom.Next(combatFootstepSounds.Length)]);
        }
        else
        {
            creatureVoice.PlayOneShot(normalFootstepSounds[enemyRandom.Next(normalFootstepSounds.Length)]);
        }
    }

    public void SpawnAnimationTransitionAnimEvent()
    {
        targetPlayer = null;
        agent.speed = sneakSpeed;
        if (IsServer) smartAgentNavigator.StartSearchRoutine(40);
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
        if (targetPlayerToNeedle.inAnimationWithEnemy == this) targetPlayerToNeedle.inAnimationWithEnemy = null;
        if (targetPlayerToNeedle == priorityPlayer)
        {
            priorityPlayer = null;
        }
        Plugin.ExtendedLogging($"{this} unsetting target player {targetPlayerToNeedle.playerUsername}");
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