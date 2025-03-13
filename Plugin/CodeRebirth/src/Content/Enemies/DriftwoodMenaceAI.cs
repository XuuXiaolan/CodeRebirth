using System;
using System.Collections;
using GameNetcodeStuff;
using UnityEngine;
using System.Linq;
using Unity.Netcode;
using CodeRebirth.src.Util;

namespace CodeRebirth.src.Content.Enemies;
public class DriftwoodMenaceAI : CodeRebirthEnemyAI, IVisibleThreat
{
    public GameObject grabArea = null!;
    public AnimationClip spawnAnimation = null!;
    public AnimationClip chestBangingAnimation = null!;
    public AudioClip spawnSound = null!;
    public AudioClip eatingSound = null!;
    public AudioClip screamSound = null!;
    public AudioClip throwSound = null!;
    public AudioClip smashSound = null!;
    public AudioClip[] hitSound = [];
    public AudioClip[] walkSounds = [];
    public AudioClip[] stompSounds = [];
    public float rangeOfSight = 50f;
    public float awarenessLevel = 0.0f; // Giant's awareness level of the player
    public float maxAwarenessLevel = 100.0f; // Maximum awareness level
    public float awarenessDecreaseRate = 2.5f; // Rate of awareness decrease per second when the player is not seen
    public float awarenessIncreaseRate = 5.0f; // Base rate of awareness increase when the player is seen
    public float awarenessIncreaseMultiplier = 2.0f; // Multiplier for awareness increase based on proximity

    private float smashingRange = 6f;
    private Vector3 enemyPositionBeforeDeath = Vector3.zero;
    private bool currentlyGrabbed = false;
    private bool canSmash = false;
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
        // creatureSFX.PlayOneShot(spawnSound);
        SwitchToBehaviourStateOnLocalClient((int)DriftwoodState.Spawn);
        StartCoroutine(SpawnAnimationCooldown());
    }

    public override void Update()
    {
        base.Update();
        if (isEnemyDead) return;

        Plugin.ExtendedLogging($"Awareness: {awarenessLevel}");
        PlayerControllerB localPlayer = GameNetworkManager.Instance.localPlayerController;
        if (targetPlayer == localPlayer && currentlyGrabbed)
        {
            targetPlayer.transform.position = grabArea.transform.position;
        }
        
        if (localPlayer.isPlayerDead || !localPlayer.isPlayerControlled || localPlayer.isInsideFactory || localPlayer.isInHangarShipRoom) return;

        if (EnemyHasLineOfSightToPosition(localPlayer.transform.position, 60f, rangeOfSight, 5))
        {
            DriftwoodGiantSeePlayerEffect(localPlayer);
        }

        if (currentBehaviourStateIndex == (int)DriftwoodState.SearchingForPrey)
        {
            UpdateAwareness();
        }
    }

    #region StateMachine
    public override void DoAIInterval()
    {
        base.DoAIInterval();
        if (isEnemyDead || StartOfRound.Instance.allPlayersDead) return;
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

        if (FindClosestTargetEnemyInRange(rangeOfSight) || (awarenessLevel >= 25f && FindClosestPlayerInRange(rangeOfSight)))
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
            if (Vector3.Distance(transform.position, targetEnemy.transform.position) > rangeOfSight+10f && !EnemyHasLineOfSightToPosition(targetEnemy.transform.position))
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
            if (Vector3.Distance(transform.position, targetPlayer.transform.position) > rangeOfSight+10f && !EnemyHasLineOfSightToPosition(targetPlayer.transform.position) || StartOfRound.Instance.shipBounds.bounds.Contains(targetPlayer.transform.position)) {
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
        if (targetEnemy == null || targetEnemy.isEnemyDead)
        {
            SetEnemyTargetServerRpc(-1);
            StartCoroutine(ChestBangPause((int)DriftwoodState.SearchingForPrey, 7f));
            agent.speed = 0f;
            SwitchToBehaviourServerRpc((int)DriftwoodState.ChestBang);
            return;
        }

        float distanceToEnemy = Vector3.Distance(transform.position, targetEnemy.transform.position);

        if (distanceToEnemy > rangeOfSight + 10f && !EnemyHasLineOfSightToPosition(targetEnemy.transform.position))
        {
            SetEnemyTargetServerRpc(-1);
            StartCoroutine(ChestBangPause((int)DriftwoodState.SearchingForPrey, 7f));
            agent.speed = 0f;
            SwitchToBehaviourServerRpc((int)DriftwoodState.ChestBang);
            return;
        }

        if (!canSmash) return;

        if (distanceToEnemy < smashingRange + 1.0f)
        {
            // creatureSFX.PlayOneShot(smashSound);
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
        if (ScaryThing == null)
        {
            SwitchToBehaviourServerRpc((int)DriftwoodState.SearchingForPrey);
            return;
        }
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
    public void ShakePlayerCameraOnDistanceAnimEvent()
    {
        float distance = Vector3.Distance(transform.position, GameNetworkManager.Instance.localPlayerController.transform.position);
        switch (distance)
        {
            case < 4f:
                HUDManager.Instance.ShakeCamera(ScreenShakeType.Long);
                HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
                HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
                HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
                HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
                break;
            case < 15 and >= 5:
                HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
                HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
                HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
                break;
            case < 25f and >= 15:
                HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
                HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
                break;
        }
    }

    public void DriftwoodChestBangAnimEvent()
    { // run this multiple times in one scream animation
        PlayerControllerB player = GameNetworkManager.Instance.localPlayerController;
        if (!player.isPlayerControlled || player.isPlayerDead || player.isInHangarShipRoom) return;
        float distance = Vector3.Distance(transform.position, player.transform.position);
        if (distance <= 12)
        {
            HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
            player.DamagePlayer(5, true, true, CauseOfDeath.Suffocation, 0, false, default);
        }
    }

    public void ParticlesFromEatingPreyAnimEvent()
    {
        // Use enemyPositionBeforeDeath
        // Make some like, red, steaming hot particles come out of the enemy corpses.
        // Also colour the hands a bit red.
    }

    public void PlayRunFootstepsAnimEvent()
    {
        // creatureVoice.PlayOneShot(stompSounds[UnityEngine.Random.Range(0, stompSounds.Length)]);
    }

    public void PlayWalkFootstepsAnimEvent()
    {
        // creatureVoice.PlayOneShot(walkSounds[UnityEngine.Random.Range(0, walkSounds.Length)]);
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
        Vector3 backDirection = transform.TransformDirection(Vector3.back).normalized * 3f;
        Vector3 upDirection = transform.TransformDirection(Vector3.up).normalized * 30f;
        // Creating a direction that is 45 degrees upwards from the back direction
        Vector3 throwingDirection = (backDirection + Quaternion.AngleAxis(55, transform.right) * upDirection).normalized;

        // Calculate the throwing force
        float throwForceMagnitude = 125f;
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
        targetPlayer.DamagePlayer(5, true, false, CauseOfDeath.Gravity, 0, false, default);
    }

    public void GrabPlayerAnimEvent()
    {
        currentlyGrabbed = true;
        targetPlayer.inAnimationWithEnemy = this;
    }

    public void SmashEnemyAnimEvent()
    {
        if (targetEnemy == null)
        {
            Plugin.Logger.LogError($"No target enemy to smash");
            return;
        }

        // Slowly turn towards the target enemy
        Vector3 targetDirection = (targetEnemy.transform.position - transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection, transform.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 3f);
        
        targetEnemy.HitEnemy(1, null, false, -1);
        if (targetEnemy.enemyHP <= 0)
        {
            enemyPositionBeforeDeath = targetEnemy.transform.position;
            agent.speed = 0f;
            SwitchToBehaviourStateOnLocalClient((int)DriftwoodState.EatingPrey);
            // creatureVoice.PlayOneShot(eatingSound);
            targetEnemy = null;
            if (!IsServer) return;
            smartAgentNavigator.DoPathingToDestination(enemyPositionBeforeDeath);
            transform.LookAt(enemyPositionBeforeDeath);
            creatureNetworkAnimator.SetTrigger(EatEnemyAnimation);
        }
    }

    public void FinishedFeedingOnEnemyAnimEvent()
    {
        SwitchToBehaviourStateOnLocalClient((int)DriftwoodState.SearchingForPrey);
        if (!IsServer) return;
        smartAgentNavigator.StartSearchRoutine(this.transform.position, 50f);
    }

    #endregion
    // Methods that aren't called during AnimationEvents

    #region Misc Functions
    public IEnumerator SpawnAnimationCooldown()
    {
        yield return new WaitForSeconds(spawnAnimation.length);
        smartAgentNavigator.StartSearchRoutine(this.transform.position, 50f);
        SwitchToBehaviourStateOnLocalClient((int)DriftwoodState.SearchingForPrey);
        if (!IsServer) yield break;
        agent.speed = 7f;
    }

    public IEnumerator ScareCooldown()
    {
        yield return new WaitForSeconds(7.5f);
    }

    public IEnumerator ChestBangPause(int nextStateIndex, float agentSpeed)
    {
        creatureNetworkAnimator.SetTrigger(DoAggroAnimation);
        yield return new WaitForSeconds(chestBangingAnimation.length);
        if (nextStateIndex == (int)DriftwoodState.SearchingForPrey) 
        {
            smartAgentNavigator.StartSearchRoutine(transform.position, 50f);    
        }
        agent.speed = agentSpeed;
        SwitchToBehaviourServerRpc(nextStateIndex);
    }

    public void RunFarAway()
    {
        StartCoroutine(ScareCooldown());
        agent.speed = 7f;
        SwitchToBehaviourServerRpc((int)DriftwoodState.RunningAway);
    }

    public bool DetectScaryThings()
    {
        EnemyAI? closestScaryThing = null;
        float minDistance = 25f;

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
        bool playerSeen = false;
        float closestPlayerDistance = float.MaxValue;

        foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
        {
            if (EnemyHasLineOfSightToPosition(player.transform.position))
            {
                playerSeen = true;
                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance < closestPlayerDistance)
                {
                    closestPlayerDistance = distance;
                }
            }
        }

        if (playerSeen)
        {
            // Increase awareness more quickly for closer players
            float distanceFactor = Mathf.Clamp01((rangeOfSight - closestPlayerDistance) / rangeOfSight);
            awarenessLevel += awarenessIncreaseRate * Time.deltaTime * (1 + distanceFactor * awarenessIncreaseMultiplier);
            awarenessLevel = Mathf.Min(awarenessLevel, maxAwarenessLevel);
        }
        else
        {
            // Decrease awareness over time if no player is seen
            awarenessLevel -= awarenessDecreaseRate * Time.deltaTime;
            awarenessLevel = Mathf.Max(awarenessLevel, 0.0f);
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
            if (enemy is RedwoodTitanAI || enemy is DriftwoodMenaceAI) continue;
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
            // creatureSFX.PlayOneShot(throwSound);
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

        // creatureVoice.PlayOneShot(hitSound[UnityEngine.Random.Range(0, hitSound.Length)]);

        if (IsServer && force == 6 && currentBehaviourStateIndex != (int)DriftwoodState.RunningAway)
        {
            RunFarAway();
        }

        enemyHP -= force;
        Plugin.ExtendedLogging("Enemy HP: " + enemyHP);
        if (IsOwner && enemyHP <= 0)
        {
            KillEnemyOnOwnerClient();
        }
    }

    public override void KillEnemy(bool destroy = false)
    {
        base.KillEnemy(destroy);
        // creatureVoice.PlayOneShot(dieSFX);
        SwitchToBehaviourStateOnLocalClient((int)DriftwoodState.Death);

        if (!IsServer) return;
        creatureAnimator.SetBool(DeadAnimation, true);
    }

    #endregion
}