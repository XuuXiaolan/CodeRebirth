using System;
using System.Collections;
using GameNetcodeStuff;
using UnityEngine;
using CodeRebirth.src.Util;
using System.Linq;
using Unity.Netcode;
using CodeRebirth.src.Util.Extensions;
using CodeRebirthLib.Util;

namespace CodeRebirth.src.Content.Enemies;
public class DriftwoodMenaceAI : CodeRebirthEnemyAI, IVisibleThreat
{
    public GameObject grabArea = null!;
    public AnimationClip spawnAnimation = null!;
    public AnimationClip chestBangingAnimation = null!;
    public AudioClip screamSound = null!;
    public float seeingRange = 60f;
    public float awarenessLevel = 0.0f; // Giant's awareness level of the player
    public float maxAwarenessLevel = 100.0f; // Maximum awareness level
    public float awarenessDecreaseRate = 2.5f; // Rate of awareness decrease per second when the player is not seen
    public float awarenessIncreaseRate = 5.0f; // Base rate of awareness increase when the player is seen
    public float awarenessIncreaseMultiplier = 2.0f; // Multiplier for awareness increase based on proximity
    public Transform smashTransform = null!;

    private static Collider[] _cachedColliders = new Collider[24];
    private Vector3 enemyPositionBeforeDeath = Vector3.zero;
    private bool currentlyGrabbed = false;
    private bool canSmash = true;
    private EnemyAI? ScaryThing = null;

    ThreatType IVisibleThreat.type => ThreatType.ForestGiant;
    int IVisibleThreat.SendSpecialBehaviour(int id)
    {
        return 0;
    }

    int IVisibleThreat.GetThreatLevel(Vector3 seenByPosition)
    {
        return 18;
    }

    int IVisibleThreat.GetInterestLevel()
    {
        return 0;
    }

    Transform IVisibleThreat.GetThreatLookTransform()
    {
        return eye;
    }

    Transform IVisibleThreat.GetThreatTransform()
    {
        return transform;
    }

    Vector3 IVisibleThreat.GetThreatVelocity()
    {
        if (IsOwner)
        {
            return agent.velocity;
        }
        return Vector3.zero;
    }

    float IVisibleThreat.GetVisibility()
    {
        if (isEnemyDead)
        {
            return 0f;
        }
        if (agent.velocity.sqrMagnitude > 0f)
        {
            return 1f;
        }
        return 0.75f;
    }

	bool IVisibleThreat.IsThreatDead()
	{
		return this.isEnemyDead;
	}

	GrabbableObject? IVisibleThreat.GetHeldObject()
	{
		return null;
	}

    [HideInInspector] public static readonly int RunSpeedFloat = Animator.StringToHash("RunSpeed"); // Float
    [HideInInspector] public static readonly int GrabPlayerAnimation = Animator.StringToHash("GrabPlayer"); // Trigger
    [HideInInspector] public static readonly int DoAggroAnimation = Animator.StringToHash("DoAggro"); // Trigger
    [HideInInspector] public static readonly int DriftwoodSmashAnimation = Animator.StringToHash("DriftwoodSmash"); // Trigger
    [HideInInspector] public static readonly int EatEnemyAnimation = Animator.StringToHash("EatEnemy"); // Trigger
    [HideInInspector] public static readonly int GrabbedAnimation = Animator.StringToHash("Grabbed"); // Bool
    [HideInInspector] public static readonly int DeadAnimation = Animator.StringToHash("Dead"); // Bool

    public enum DriftwoodState
    {
        Spawn, // Spawning
        SearchingForPrey, // Wandering
        ChestBang,
        RunningToPrey, // Chasing
        SmashingPrey, // Driftwood Smash
        EatingPrey, // Eating
        PlayingWithPrey, // Playing with a player's body
        RunningAway, // Running away
        Grabbed, // This stuff would be handled on redwood giant's end
        Death,
    }

    public override void Start()
    {
        base.Start();
        SwitchToBehaviourStateOnLocalClient((int)DriftwoodState.Spawn);
        StartCoroutine(SpawnAnimationCooldown());
    }

    public override void Update()
    {
        base.Update();
        if (isEnemyDead) return;

        _idleTimer -= Time.deltaTime;
        if (_idleTimer <= 0 && targetPlayer == null)
        {
            _idleTimer = enemyRandom.NextFloat(_idleAudioClips.minTime, _idleAudioClips.maxTime);
            creatureVoice.PlayOneShot(_idleAudioClips.audioClips[enemyRandom.Next(0, _idleAudioClips.audioClips.Length)]);
        }

        // Plugin.ExtendedLogging($"Awareness: {awarenessLevel}");
        PlayerControllerB localPlayer = GameNetworkManager.Instance.localPlayerController;
        if (currentlyGrabbed && targetPlayer != null)
        {
            if (Vector3.Distance(targetPlayer.transform.position, grabArea.transform.position) > 10f)
            {
                // If the target player is too far away or null, we can't grab them.
                Plugin.ExtendedLogging("Target player is too far away or null, cannot grab.");
                currentlyGrabbed = false;
                targetPlayer.inAnimationWithEnemy = null;
                targetPlayer = null;
                SwitchToBehaviourStateOnLocalClient((int)DriftwoodState.ChestBang);
                if (!IsServer) return;
                agent.speed = 0f;
                StartCoroutine(ChestBangPause((int)DriftwoodState.SearchingForPrey, 7f));
                return;
            }
            targetPlayer.transform.position = grabArea.transform.position;
            targetPlayer.ResetFallGravity();
            return;
        }

        if (localPlayer.isPlayerDead || !localPlayer.isPlayerControlled || localPlayer.isInsideFactory || localPlayer.isInHangarShipRoom) return;

        if (EnemyHasLineOfSightToPosition(localPlayer.transform.position, 60f, seeingRange, 5))
        {
            DriftwoodGiantSeePlayerEffect(localPlayer);
        }

        if (currentBehaviourStateIndex == (int)DriftwoodState.SearchingForPrey)
        {
            UpdateAwareness();
        }
    }

    /*public void LateUpdate()
    {
        if (isEnemyDead) return;
        if (currentlyGrabbed)
        {
            targetPlayer.transform.position = grabArea.transform.position;
            targetPlayer.ResetFallGravity();
        }
    }*/

    #region StateMachine
    public override void DoAIInterval()
    {
        base.DoAIInterval();
        if (isEnemyDead || StartOfRound.Instance.allPlayersDead) return;
        Plugin.ExtendedLogging($"Current Behaviour State: {currentBehaviourStateIndex}");
        creatureAnimator.SetFloat(RunSpeedFloat, agent.velocity.magnitude / 2);
        switch (currentBehaviourStateIndex)
        {
            case (int)DriftwoodState.Spawn:
                DoSpawn();
                break;
            case (int)DriftwoodState.SearchingForPrey:
                DoSearchingForPrey();
                break;
            case (int)DriftwoodState.ChestBang:
                DoChestBang();
                break;
            case (int)DriftwoodState.RunningToPrey:
                DoRunningToPrey();
                break;
            case (int)DriftwoodState.SmashingPrey:
                DoSmashingPrey();
                break;
            case (int)DriftwoodState.EatingPrey:
                DoEatingPrey();
                break;
            case (int)DriftwoodState.PlayingWithPrey:
                DoPlayingWithPrey();
                break;
            case (int)DriftwoodState.RunningAway:
                DoRunningAway();
                break;
            case (int)DriftwoodState.Grabbed:
                DoGrabbed();
                break;
            case (int)DriftwoodState.Death:
                DoDeath();
                break;
        }
    }

    public void DoSpawn()
    {
        // Do Nothing.
    }

    public void DoSearchingForPrey()
    {
        if (DetectScaryThings())
        {
            RunFarAway();
            return;
        }

        if (FindClosestTargetEnemyInRange(seeingRange) || (awarenessLevel >= 25f && FindClosestPlayerInRange(seeingRange)))
        {
            smartAgentNavigator.StopSearchRoutine();
            StartCoroutine(ChestBangPause((int)DriftwoodState.RunningToPrey, 20f));
            agent.speed = 0f;
            SwitchToBehaviourServerRpc((int)DriftwoodState.ChestBang);
        }
    }

    public void DoChestBang()
    {
        // Do Nothing.
    }

    public void DoRunningToPrey()
    {
        if ((targetEnemy == null || targetEnemy.isEnemyDead) && (targetPlayer == null || targetPlayer.isPlayerDead))
        {
            Plugin.ExtendedLogging("If you see this, something went wrong, likely an enemy or player randomly died.");
            Plugin.ExtendedLogging("Resettings state to Scream Animation");
            SetTargetServerRpc(-1);
            SetEnemyTargetServerRpc(-1);
            StartCoroutine(ChestBangPause((int)DriftwoodState.SearchingForPrey, 7f));
            agent.speed = 0f;
            SwitchToBehaviourServerRpc((int)DriftwoodState.ChestBang);
            return;
        }
        // Keep targetting target enemy, unless they are over 20 units away and we can't see them.
        if (targetEnemy != null)
        {
            if (Vector3.Distance(transform.position, targetEnemy.transform.position) > seeingRange + 10f && !EnemyHasLineOfSightToPosition(targetEnemy.transform.position))
            {
                Plugin.ExtendedLogging("Stop chasing target enemy");
                SetEnemyTargetServerRpc(-1);
                StartCoroutine(ChestBangPause((int)DriftwoodState.SearchingForPrey, 7f));
                agent.speed = 0f;
                SwitchToBehaviourServerRpc((int)DriftwoodState.ChestBang);
                return;
            }
            smartAgentNavigator.DoPathingToDestination(targetEnemy.transform.position);
        }
        else if (targetPlayer != null)
        {
            if (Vector3.Distance(transform.position, targetPlayer.transform.position) > seeingRange + 10f && !EnemyHasLineOfSightToPosition(targetPlayer.transform.position) || StartOfRound.Instance.shipBounds.bounds.Contains(targetPlayer.transform.position))
            {
                Plugin.ExtendedLogging("Stop chasing target player");
                SetTargetServerRpc(-1);
                StartCoroutine(ChestBangPause((int)DriftwoodState.SearchingForPrey, 7f));
                agent.speed = 0f;
                SwitchToBehaviourServerRpc((int)DriftwoodState.ChestBang);
                return;
            }
            smartAgentNavigator.DoPathingToDestination(targetPlayer.transform.position);
        }
    }

    public void DoSmashingPrey()
    {
        if (targetEnemy == null || targetEnemy.isEnemyDead && canSmash)
        {
            Plugin.ExtendedLogging("If you see this, something went wrong, likely an enemy randomly died.");
            SetEnemyTargetServerRpc(-1);
            StartCoroutine(ChestBangPause((int)DriftwoodState.SearchingForPrey, 7f));
            agent.speed = 0f;
            SwitchToBehaviourServerRpc((int)DriftwoodState.ChestBang);
            return;
        }

        float distanceToEnemy = Vector3.Distance(transform.position, targetEnemy.transform.position);

        if (distanceToEnemy > seeingRange + 10f && !EnemyHasLineOfSightToPosition(targetEnemy.transform.position))
        {
            SetEnemyTargetServerRpc(-1);
            StartCoroutine(ChestBangPause((int)DriftwoodState.SearchingForPrey, 7f));
            agent.speed = 0f;
            SwitchToBehaviourServerRpc((int)DriftwoodState.ChestBang);
            return;
        }

        if (!canSmash) return;

        Plugin.ExtendedLogging($"Distance to enemy: {distanceToEnemy}, Stopping distance: {agent.stoppingDistance + 1.0f}");
        if (distanceToEnemy < agent.stoppingDistance + 1.0f)
        {
            creatureNetworkAnimator.SetTrigger(DriftwoodSmashAnimation);
            canSmash = false;
            StartCoroutine(SmashCooldown());
        }
        else
        {
            agent.speed = 20f;
            SwitchToBehaviourServerRpc((int)DriftwoodState.RunningToPrey);
        }
    }

    public void DoEatingPrey()
    {
        // Do Nothing.
    }

    public void DoPlayingWithPrey()
    {
        // Do Nothing.
    }

    public void DoRunningAway()
    {
        if (ScaryThing is RadMechAI)
        {
            smartAgentNavigator.DoPathingToDestination(ChooseFarthestNodeFromPosition(transform.position, false).position);
        }
        else if (ScaryThing is RedwoodTitanAI)
        {
            smartAgentNavigator.DoPathingToDestination(ChooseFarthestNodeFromPosition(ScaryThing.transform.position, false).position);
        }
    }

    public void DoGrabbed()
    {
        // Do nothing
    }

    public void DoDeath()
    {
        // Do nothing
    }

    #endregion

    #region Animation Events
    public void DriftwoodChestBangAnimEvent()
    { // run this multiple times in one scream animation
        PlayerControllerB player = GameNetworkManager.Instance.localPlayerController;
        if (!player.isPlayerControlled || player.isPlayerDead || player.isInHangarShipRoom) return;
        float distance = Vector3.Distance(transform.position, player.transform.position);

        if (distance <= 20)
        {
            player.DamagePlayer(5, true, true, CauseOfDeath.Suffocation, 0, false, default);
        }
    }

    public void ParticlesFromEatingPreyAnimEvent()
    {
        // Use enemyPositionBeforeDeath
        // Make some like, red, steaming hot particles come out of the enemy corpses.
        // Also colour the hands a bit red.
    }

    public void ThrowPlayerAnimEvent()
    {
        if (targetPlayer == null)
        {
            Plugin.ExtendedLogging("No player to throw, This is a bug, please report this");
            return;
        }
        targetPlayer.inAnimationWithEnemy = null;
        currentlyGrabbed = false;

        // Calculate the throwing direction with an upward angle
        Vector3 backDirection = -transform.forward.normalized * 60f;
        Vector3 upDirection = Vector3.up.normalized * 30f;
        // Creating a direction that is 45 degrees upwards from the back direction
        Vector3 throwingDirection = (backDirection + Quaternion.AngleAxis(55, transform.right) * upDirection).normalized;

        // Calculate the throwing force
        float throwForceMagnitude = 150f;
        // Throw the player
        Plugin.ExtendedLogging("Launching Player");
        targetPlayer.externalForceAutoFade += throwingDirection * throwForceMagnitude;
        targetPlayer.SetFlingingAway(true);
        targetPlayer.SetFlung(true);
        StartCoroutine(StopFlingingPlayer(targetPlayer));
        targetPlayer = null;
        SwitchToBehaviourStateOnLocalClient((int)DriftwoodState.ChestBang);
        if (!IsServer) return;
        agent.speed = 0f;
        StartCoroutine(ChestBangPause((int)DriftwoodState.SearchingForPrey, 7f));
    }

    public void SmashPlayerOntoGroundAnimEvent()
    {
        if (targetPlayer == null)
        {
            Plugin.Logger.LogError("No player to smash onto the ground, This is a bug, please report this");
            return;
        }

        if (Vector3.Distance(targetPlayer.transform.position, grabArea.transform.position) > 10f)
        {
            // If the target player is too far away or null, we can't grab them.
            Plugin.ExtendedLogging("Target player is too far away or null, cannot grab.");
            return;
        }
        targetPlayer.DamagePlayer(5, true, false, CauseOfDeath.Gravity, 0, false, default);
    }

    public void GrabPlayerAnimEvent()
    {
        if (targetPlayer == null)
        {
            Plugin.Logger.LogError("No player to grab, This is a bug, please report this");
            return;
        }

        currentlyGrabbed = true;
        targetPlayer.inAnimationWithEnemy = this;
        targetPlayer.transform.position = grabArea.transform.position;
        targetPlayer.ResetFallGravity();
    }

    public void SmashEnemyAnimEvent()
    {
        int numHits = Physics.OverlapSphereNonAlloc(smashTransform.position, 8f, _cachedColliders, MoreLayerMasks.EnemiesMask, QueryTriggerInteraction.Collide);
        for (int i = 0; i < numHits; i++)
        {
            if (!_cachedColliders[i].gameObject.TryGetComponent(out IHittable iHittable))
                continue;

            if (iHittable is EnemyAICollisionDetect enemyAICollisionDetect && enemyAICollisionDetect.mainScript is DriftwoodMenaceAI)
                continue;

            iHittable.Hit(6, smashTransform.position, null, true, -1);
        }

        if (targetEnemy != null)
        {
            // Slowly turn towards the target enemy
            Vector3 targetDirection = (targetEnemy.transform.position - transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection, transform.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 3f);
            if (targetEnemy.enemyHP <= 0)
            {
                if (RoundManager.Instance.SpawnedEnemies.Any(x => !x.isEnemyDead && x is not DriftwoodMenaceAI && Vector3.Distance(x.transform.position, this.transform.position) < 10))
                {
                    SwitchToBehaviourStateOnLocalClient((int)DriftwoodState.SearchingForPrey);
                    if (!IsServer) return;
                    smartAgentNavigator.StartSearchRoutine(50f);
                    return;
                }
                enemyPositionBeforeDeath = targetEnemy.transform.position;
                agent.speed = 0f;
                SwitchToBehaviourStateOnLocalClient((int)DriftwoodState.EatingPrey);
                targetEnemy = null;
                if (!IsServer) return;
                smartAgentNavigator.DoPathingToDestination(enemyPositionBeforeDeath);
                transform.LookAt(enemyPositionBeforeDeath);
                creatureNetworkAnimator.SetTrigger(EatEnemyAnimation);
            }
        }
    }

    public void FinishedFeedingOnEnemyAnimEvent()
    {
        if (isEnemyDead) return;
        enemyHP += 2;
        SwitchToBehaviourStateOnLocalClient((int)DriftwoodState.SearchingForPrey);
        if (!IsServer) return;
        agent.speed = 7f;
        smartAgentNavigator.StartSearchRoutine(50f);
    }

    #endregion
    // Methods that aren't called during AnimationEvents

    #region Misc Functions
    public IEnumerator SpawnAnimationCooldown()
    {
        yield return new WaitForSeconds(spawnAnimation.length);
        SwitchToBehaviourStateOnLocalClient((int)DriftwoodState.SearchingForPrey);
        if (!IsServer) yield break;
        smartAgentNavigator.StartSearchRoutine(50f);
        agent.speed = 7f;
    }

    public IEnumerator ScareCooldown()
    {
        yield return new WaitForSeconds(7.5f);
        agent.speed = 7f;
        SwitchToBehaviourServerRpc((int)DriftwoodState.SearchingForPrey);
    }

    public IEnumerator ChestBangPause(int nextStateIndex, float agentSpeed)
    {
        creatureNetworkAnimator.SetTrigger(DoAggroAnimation);
        PlayScreamSoundServerRpc();
        yield return new WaitForSeconds(chestBangingAnimation.length);
        if (nextStateIndex == (int)DriftwoodState.SearchingForPrey)
        {
            smartAgentNavigator.StartSearchRoutine(50f);
        }
        agent.speed = agentSpeed;
        SwitchToBehaviourServerRpc(nextStateIndex);
    }

    [ServerRpc(RequireOwnership = false)]
    public void PlayScreamSoundServerRpc()
    {
        PlayScreamSoundClientRpc();
    }
    [ClientRpc]
    public void PlayScreamSoundClientRpc()
    {
        creatureVoice.PlayOneShot(screamSound);
    }

    public void RunFarAway()
    {
        StartCoroutine(ScareCooldown());
        agent.speed = 20f;
        SwitchToBehaviourServerRpc((int)DriftwoodState.RunningAway);
    }

    public bool DetectScaryThings()
    {
        EnemyAI? closestScaryThing = null;
        float minDistance = 35f;

        foreach (EnemyAI enemy in RoundManager.Instance.SpawnedEnemies)
        {
            if (!enemy.isEnemyDead && (enemy is RedwoodTitanAI || enemy is RadMechAI))
            {
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestScaryThing = enemy;
                }
            }
        }
        if (closestScaryThing != null)
        {
            ScaryThing = closestScaryThing;
            return true;
        }
        return false;
    }


    public void DriftwoodGiantSeePlayerEffect(PlayerControllerB localPlayer)
    {
        if (currentBehaviourStateIndex == (int)DriftwoodState.RunningToPrey && localPlayer == targetPlayer)
        {
            localPlayer.IncreaseFearLevelOverTime(1.4f);
            return;
        }

        if (Vector3.Distance(base.transform.position, localPlayer.transform.position) < 15f)
        {
            localPlayer.JumpToFearLevel(0.7f);
        }
        else
        {
            localPlayer.JumpToFearLevel(0.4f);
        }
    }

    public void UpdateAwareness()
    {
        PlayerControllerB? playerSeen = null;
        float closestPlayerDistance = float.MaxValue;

        foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
        {
            if (EnemyHasLineOfSightToPosition(player.transform.position))
            {
                playerSeen = player;
                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance < closestPlayerDistance)
                {
                    closestPlayerDistance = distance;
                }
            }
        }

        float minimumAwareness = 0;
        minimumAwareness += (RedwoodTitanAI.instanceNumbers > 0 ? 5 : 0);
        awarenessLevel = Mathf.Max(awarenessLevel, minimumAwareness);
        if (playerSeen != null)
        {
            bool playerHoldingWeapon = playerSeen.currentlyHeldObjectServer != null && playerSeen.currentlyHeldObjectServer.itemProperties.isDefensiveWeapon;
            // Increase awareness more quickly for closer players
            float distanceFactor = Mathf.Clamp01((seeingRange - closestPlayerDistance) / seeingRange);
            awarenessLevel += awarenessIncreaseRate * Time.deltaTime * (1 + distanceFactor * awarenessIncreaseMultiplier) + (playerHoldingWeapon ? 0.1f : 0f);
            awarenessLevel = Mathf.Min(awarenessLevel, maxAwarenessLevel);
        }
        else
        {
            // Decrease awareness over time if no player is seen
            awarenessLevel -= awarenessDecreaseRate * Time.deltaTime;
        }
    }

    public IEnumerator SmashCooldown()
    {
        yield return new WaitForSeconds(1.5f);
        canSmash = true;
    }

    public bool FindClosestTargetEnemyInRange(float range)
    {
        EnemyAI? closestEnemy = null;
        float minDistance = range;

        foreach (EnemyAI enemy in RoundManager.Instance.SpawnedEnemies)
        {
            if (enemy is RedwoodTitanAI || enemy is DriftwoodMenaceAI || enemy is ForestGiantAI || enemy is CactusBudling || enemy is Puppeteer || enemy is CutieFlyAI || enemy is DocileLocustBeesAI || enemy is RedLocustBees) continue;
            if (!enemy.enemyType.canDie || enemy.isEnemyDead || enemy.enemyHP <= 0) continue;
            if (EnemyHasLineOfSightToPosition(enemy.transform.position, 75f, range))
            {
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestEnemy = enemy;
                }
            }
        }
        if (closestEnemy != null)
        {
            SetEnemyTargetServerRpc(RoundManager.Instance.SpawnedEnemies.IndexOf(closestEnemy));
            return true;
        }
        return false;
    }

    public bool FindClosestPlayerInRange(float range)
    {
        PlayerControllerB? closestPlayer = null;
        float minDistance = range;

        foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
        {
            if (!player.isPlayerControlled || player.isPlayerDead || player.isInHangarShipRoom) continue;
            if (EnemyHasLineOfSightToPosition(player.transform.position, 45f, range))
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestPlayer = player;
                }
            }
        }
        if (closestPlayer != null)
        {
            SetTargetServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, closestPlayer));
            return true;
        }
        return false;
    }

    public IEnumerator StopFlingingPlayer(PlayerControllerB player)
    {
        yield return new WaitForSeconds(10f);
        player.SetFlingingAway(false);
    }

    #endregion

    #region Zeekerss Callbacks

    public override void OnCollideWithEnemy(Collider other, EnemyAI collidedEnemy)
    {
        if (isEnemyDead || targetEnemy == null) return;
        if (collidedEnemy == targetEnemy)
        {
            SwitchToBehaviourStateOnLocalClient((int)DriftwoodState.SmashingPrey);
            if (!IsServer) return;
            agent.speed = 0f;
        }
    }

    public override void OnCollideWithPlayer(Collider other)
    {
        if (isEnemyDead) return;

        PlayerControllerB? collidedPlayer = other.GetComponent<PlayerControllerB>();
        if (collidedPlayer == null) return;
        awarenessLevel += 10f;

        if (collidedPlayer == targetPlayer && currentBehaviourStateIndex == (int)DriftwoodState.RunningToPrey)
        {
            SwitchToBehaviourStateOnLocalClient((int)DriftwoodState.PlayingWithPrey);
            if (!IsServer) return;
            agent.speed = 0f;
            creatureNetworkAnimator.SetTrigger(GrabPlayerAnimation);
        }
    }

    public override void HitEnemy(int force = 1, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
        if (isEnemyDead) return;

        enemyHP -= force;
        Plugin.ExtendedLogging("Enemy HP: " + enemyHP);
        if (IsOwner && enemyHP <= 0)
        {
            KillEnemyOnOwnerClient();
            return;
        }

        if (enemyHP <= 0 || isEnemyDead) return;

        if (IsServer && force == 6 && currentBehaviourStateIndex != (int)DriftwoodState.RunningAway)
        {
            RunFarAway();
        }
        else if (playerWhoHit != null && targetPlayer == null)
        {
            targetPlayer = playerWhoHit;
            smartAgentNavigator.StopSearchRoutine();
            if (IsServer)
            {
                StartCoroutine(ChestBangPause((int)DriftwoodState.RunningToPrey, 20f));
                agent.speed = 0f;
                SwitchToBehaviourServerRpc((int)DriftwoodState.ChestBang);
            }
        }
        else if (force > 0 && currentBehaviourStateIndex == (int)DriftwoodState.EatingPrey)
        {
            SwitchToBehaviourStateOnLocalClient((int)DriftwoodState.SearchingForPrey);
            if (IsServer)
            {
                smartAgentNavigator.StartSearchRoutine(50f);
            }
        }
    }

    public override void KillEnemy(bool destroy = false)
    {
        base.KillEnemy(destroy);
        smartAgentNavigator.StopSearchRoutine();
        SwitchToBehaviourStateOnLocalClient((int)DriftwoodState.Death);

        if (!IsServer) return;
        creatureAnimator.SetBool(DeadAnimation, true);
    }

    #endregion
}