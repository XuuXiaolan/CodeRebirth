using System.Collections;
using GameNetcodeStuff;
using UnityEngine;
using System.Linq;
using CodeRebirth.src.Util;
using Unity.Netcode;
using CodeRebirth.src.MiscScripts;

namespace CodeRebirth.src.Content.Enemies;
public class RedwoodTitanAI : CodeRebirthEnemyAI, IVisibleThreat
{
    public Collider[] DeathColliders = null!;
    public Collider CollisionFootR = null!;
    public Collider CollisionFootL = null!;
    public ParticleSystem DustParticlesLeft = null!;
    public ParticleSystem DustParticlesRight = null!;
    public ParticleSystem ForestKeeperParticles = null!;
    public ParticleSystem DriftwoodGiantParticles = null!;
    public ParticleSystem OldBirdParticles = null!;
    public ParticleSystem DeathParticles = null!;
    public ParticleSystem BigSmokeEffect = null!;
    public AudioSource creatureSFXFar = null!;
    public AudioClip eatenSound = null!;
    public AudioClip roarSound = null!;
    public AudioClip jumpSound = null!;
    public AudioClip kickSound = null!;
    public AudioClip crunchySquishSound = null!;
    public GameObject holdingBone = null!;
    public GameObject eatingArea = null!;
    public AnimationClip eating = null!;
    public AnimationClip kickAnimation = null!;

    private Vector3 eatPosition = Vector3.zero;
    private bool sizeUp = false;
    private bool eatingEnemy = false;
    private float walkingSpeed = 0f;
    private float seeableDistance = 0f;
    private float distanceFromShip = 0f;
    private Transform shipBoundaries = null!;
    private Collider[] enemyColliders = null!;
    private PlayerControllerB? playerToKick = null;
    private static readonly int startKick = Animator.StringToHash("startKick"); // Trigger
    private static readonly int startJump = Animator.StringToHash("startJump"); // Trigger
    private static readonly int startEnrage = Animator.StringToHash("startEnrage"); // Trigger
    private static readonly int eatEnemyGiant = Animator.StringToHash("eatEnemyGiant"); // Trigger
    private static readonly int IsDeadAnimation = Animator.StringToHash("IsDead"); // Bool
    private static readonly int RunSpeedFloat = Animator.StringToHash("RunSpeedFloat"); // Float

    internal static int instanceNumbers = 0;
    internal bool kickingOut = false;
    internal bool jumping = false;

    #region ThreatType
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
        return base.transform;
    }
    Vector3 IVisibleThreat.GetThreatVelocity()
    {
        if (base.IsOwner)
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

    #endregion
    enum State
    {
        Spawn, // Roaring
        Idle, // Idling
        Wandering, // Wandering
        RunningToTarget, // Chasing
        EatingTargetGiant, // Eating
    }

    public override void Start()
    {
        base.Start();
        instanceNumbers++;

        walkingSpeed = Plugin.ModConfig.ConfigRedwoodSpeed.Value;
        distanceFromShip = Plugin.ModConfig.ConfigRedwoodShipPadding.Value;
        seeableDistance = Plugin.ModConfig.ConfigRedwoodEyesight.Value;
        shipBoundaries = StartOfRound.Instance.shipBounds.transform;

        enemyColliders = GetComponentsInChildren<Collider>();

        creatureSFXFar.PlayOneShot(spawnSound);
        creatureVoice.PlayOneShot(roarSound);

        smartAgentNavigator.StopAgent();
        SwitchToBehaviourStateOnLocalClient((int)State.Spawn);
        CodeRebirthPlayerManager.OnDoorStateChange += OnShipDoorStateChange;
        StartCoroutine(UpdateAudio());
    }

    private IEnumerator UpdateAudio()
    {
        while (!isEnemyDead)
        {
            if (GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom)
            {
                creatureSFX.volume = Plugin.ModConfig.ConfigRedwoodNormalVolume.Value * Plugin.ModConfig.ConfigRedwoodInShipVolume.Value;
                creatureSFXFar.volume = Plugin.ModConfig.ConfigRedwoodNormalVolume.Value * Plugin.ModConfig.ConfigRedwoodInShipVolume.Value;
                creatureVoice.volume = Plugin.ModConfig.ConfigRedwoodNormalVolume.Value * Plugin.ModConfig.ConfigRedwoodInShipVolume.Value;
            }
            else
            {
                creatureSFX.volume = Plugin.ModConfig.ConfigRedwoodNormalVolume.Value;
                creatureSFXFar.volume = Plugin.ModConfig.ConfigRedwoodNormalVolume.Value;
                creatureVoice.volume = Plugin.ModConfig.ConfigRedwoodNormalVolume.Value;
            }
            yield return new WaitForSeconds(1);
        }
    }

    private void OnShipDoorStateChange(object sender, bool shipDoorClosed)
    {
        foreach (PlayerControllerB? player in StartOfRound.Instance.allPlayerScripts)
        {
            if (player == null || player.isPlayerDead || !player.isPlayerControlled || !player.ContainsCRPlayerData())
                continue;

            foreach (Collider? playerCollider in player.GetPlayerColliders())
            {
                if (playerCollider == null)
                    continue;

                foreach (Collider? enemyCollider in enemyColliders)
                {
                    if (enemyCollider == null)
                        continue;

                    Physics.IgnoreCollision(playerCollider, enemyCollider, shipDoorClosed && player.isInHangarShipRoom);
                }
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        CodeRebirthPlayerManager.OnDoorStateChange -= OnShipDoorStateChange;
        instanceNumbers--;
    }

    public override void Update()
    {
        base.Update();
        if (isEnemyDead)
            return;

        KickingPlayerUpdate();
    }

    public void KickingPlayerUpdate()
    {
        if (playerToKick == null)
            return;

        if (!kickingOut)
        {
            // Calculate the direction towards the player
            Vector3 directionToPlayer = playerToKick.transform.position - transform.position;
            Quaternion lookRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime);
        }
        else
        {
            // Assuming you want to rotate towards the right direction of the object
            Vector3 rightDirection = this.transform.right;
            Quaternion lookRotation = Quaternion.LookRotation(rightDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime);
        }
    }

    public void LateUpdate()
    {
        if (isEnemyDead)
            return;

        EatTargetGiantLateUpdate();
    }

    public void EatTargetGiantLateUpdate()
    {
        if (currentBehaviourStateIndex != (int)State.EatingTargetGiant || targetEnemy == null)
            return;

        transform.position = eatPosition;
        var grabPosition = holdingBone.transform.position;
        targetEnemy.transform.position = grabPosition + new Vector3(0, -1f, 0);
        targetEnemy.transform.LookAt(eatingArea.transform.position);
        targetEnemy.transform.position = grabPosition + new Vector3(0, -5.5f, 0);
        // Scale targetEnemy's transform down by 0.9995 everytime Update runs in this if statement
        if (!sizeUp)
        {
            targetEnemy.transform.position = grabPosition + new Vector3(0, -1f, 0);
            targetEnemy.transform.LookAt(eatingArea.transform.position);
            targetEnemy.transform.position = grabPosition + new Vector3(0, -6f, 0);
            var newScale = targetEnemy.transform.localScale;
            newScale.x *= 1.4f;
            newScale.y *= 1.3f;
            newScale.z *= 1.4f;
            targetEnemy.transform.localScale = newScale;
            sizeUp = true;
        }
        targetEnemy.transform.position += new Vector3(0, 0.02f, 0);
        Vector3 currentScale = targetEnemy.transform.localScale;
        currentScale *= 0.9995f;
        targetEnemy.transform.localScale = Vector3.Lerp(targetEnemy.transform.localScale, currentScale, Time.deltaTime);
    }

    public void DoFunnyThingWithNearestPlayer(PlayerControllerB closestPlayer, Vector3 closestFootPosition)
    {
        float distanceToClosestPlayer = Vector3.Distance(closestFootPosition, closestPlayer.transform.position);
        float randomFloat = UnityEngine.Random.Range(0f, 100f);
        if (distanceToClosestPlayer <= 16f && randomFloat <= 5f)
        {
            JumpInPlace();
        }
        else if (distanceToClosestPlayer <= 16f && randomFloat <= 10f)
        {
            DoKickTargetPlayer(closestPlayer);
        }
    }

    public void JumpInPlace()
    {
        creatureNetworkAnimator.SetTrigger(startJump);
        PlayMiscSoundsServerRpc(0);
        jumping = true;
        creatureAnimator.SetFloat(RunSpeedFloat, 0f);
        smartAgentNavigator.StopAgent();
        agent.speed = 0.5f;
        Plugin.ExtendedLogging("Start Jump");
    }

    public void DoKickTargetPlayer(PlayerControllerB closestPlayer)
    {
        smartAgentNavigator.StopAgent();
        agent.speed = 0.5f;
        creatureNetworkAnimator.SetTrigger(startKick);
        playerToKick = closestPlayer;
        creatureAnimator.SetFloat(RunSpeedFloat, 0f);
        PlayMiscSoundsServerRpc(1);
        Plugin.ExtendedLogging("Start Kick");
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlayMiscSoundsServerRpc(int soundID)
    {
        PlayMiscSoundsClientRpc(soundID);
    }

    [ClientRpc]
    private void PlayMiscSoundsClientRpc(int soundID)
    {
        switch (soundID)
        {
            case 0:
                creatureSFX.PlayOneShot(jumpSound);
                creatureSFXFar.PlayOneShot(jumpSound);
                break;
            case 1:
                creatureSFX.PlayOneShot(kickSound);
                creatureSFXFar.PlayOneShot(kickSound);
                break;
            case 2:
                creatureSFX.PlayOneShot(crunchySquishSound);
                creatureSFXFar.PlayOneShot(crunchySquishSound);
                break;
        }
    }
    public override void DoAIInterval()
    {
        base.DoAIInterval();
        if (isEnemyDead || StartOfRound.Instance.allPlayersDead || playerToKick != null || jumping)
        {
            return;
        }

        creatureAnimator.SetFloat(RunSpeedFloat, agent.velocity.magnitude);
        switch (currentBehaviourStateIndex)
        {
            case (int)State.Spawn:
                break;
            case (int)State.Idle:
                break;
            case (int)State.Wandering:
                DoWandering();
                break;
            case (int)State.RunningToTarget:
                DoRunningToTarget();
                break;
            case (int)State.EatingTargetGiant:
                break;
        }
    }

    public void DoWandering()
    {
        if (FindClosestAliveGiantInRange(seeableDistance))
        {
            Plugin.ExtendedLogging("Start Target Giant");
            smartAgentNavigator.StopSearchRoutine();
            SwitchToBehaviourServerRpc((int)State.RunningToTarget);
            StartCoroutine(SetSpeedForChasingGiant());
            return;
        } // Look for Giants
        PlayerControllerB? closestPlayer = GetClosestPlayerToRedwood();
        if (closestPlayer == null)
            return;

        Vector3 closestFootPosition = GetClosestFootPositionToPlayer(closestPlayer);
        DoFunnyThingWithNearestPlayer(closestPlayer, closestFootPosition);
    }

    public Vector3 GetClosestFootPositionToPlayer(PlayerControllerB player)
    {
        float distanceToRightFoot = Vector3.Distance(CollisionFootR.transform.position, player.transform.position);
        float distanceToLeftFoot = Vector3.Distance(CollisionFootL.transform.position, player.transform.position);
        Vector3 closestFootPosition;
        if (distanceToRightFoot > distanceToLeftFoot)
        {
            closestFootPosition = CollisionFootL.transform.position;
        }
        else
        {
            closestFootPosition = CollisionFootR.transform.position;
        }
        return closestFootPosition;
    }

    public IEnumerator SetSpeedForChasingGiant()
    {
        agent.speed = 0f;
        smartAgentNavigator.StopAgent();
        creatureNetworkAnimator.SetTrigger(startEnrage);
        yield return new WaitForSeconds(6.9f);
        if (currentBehaviourStateIndex != (int)State.RunningToTarget)
        {
            Plugin.Logger.LogWarning($"Redwood Not running to target with speed: {agent.speed}, plus is dead: {isEnemyDead}");
            if (agent.speed < 0.5f && !isEnemyDead)
            {
                agent.angularSpeed = 40f;
                agent.speed = walkingSpeed;
            }
            yield break;
        }
        agent.angularSpeed = 100f;
        agent.speed = walkingSpeed * 4;
    }

    public void DoRunningToTarget()
    {
        // Keep targetting closest Giant, unless they are over 20 units away and we can't see them.
        if (targetEnemy == null || targetEnemy.isEnemyDead)
        {
            ClearEnemyTargetServerRpc();
            Plugin.ExtendedLogging("Stop Target Giant");
            agent.angularSpeed = 40f;
            smartAgentNavigator.StartSearchRoutine(50);
            agent.speed = walkingSpeed;
            SwitchToBehaviourServerRpc((int)State.Wandering);
            return;
        }
        else if (Vector3.Distance(transform.position, targetEnemy.transform.position) >= seeableDistance + 10 && !RWHasLineOfSightToPosition(targetEnemy.transform.position, 120, seeableDistance, 5))
        {
            Plugin.ExtendedLogging("Stop Target Giant");
            agent.angularSpeed = 40f;
            smartAgentNavigator.StartSearchRoutine(50);
            agent.speed = walkingSpeed;
            SwitchToBehaviourServerRpc((int)State.Wandering);
            return;
        }
        smartAgentNavigator.DoPathingToDestination(targetEnemy.transform.position);
    }

    public void DealEnemyDamageFromShockwave(EnemyAI enemy, float distanceFromEnemy)
    {
        // Apply damage based on distance
        if (distanceFromEnemy <= 3f)
        {
            enemy.HitEnemy(2, null, false, -1);
        }
        else if (distanceFromEnemy <= 10f)
        {
            enemy.HitEnemy(1, null, false, -1);
        }

        // Optional: Log the distance and remaining HP for debugging
        Plugin.ExtendedLogging($"Distance: {distanceFromEnemy} HP: {enemy.enemyHP}");
    }

    public PlayerControllerB? GetClosestPlayerToRedwood()
    {
        return StartOfRound.Instance.allPlayerScripts
            .Where(player => player.IsSpawned && player.isPlayerControlled && !player.isPlayerDead)
            .OrderBy(x => Vector3.Distance(x.transform.position, transform.position) <= 11)
            .FirstOrDefault(x => RWHasLineOfSightToPosition(x.transform.position, 120, seeableDistance, 5));
    }

    public void ParticlesFromEatingForestKeeper(EnemyAI targetEnemy)
    {
        if (targetEnemy is ForestGiantAI)
        {
            ForestKeeperParticles.Play();
        }
        else if (targetEnemy is DriftwoodMenaceAI || targetEnemy.enemyType.enemyName == "DriftWoodGiant")
        {
            DriftwoodGiantParticles.Play();
        }
        else if (targetEnemy is RadMechAI)
        {
            OldBirdParticles.Play();
        }
        Plugin.ExtendedLogging("Ate: " + targetEnemy.enemyType.enemyName);
        targetEnemy.KillEnemyOnOwnerClient(overrideDestroy: true);
    }

    public bool FindClosestAliveGiantInRange(float range)
    {
        EnemyAI? closestEnemy = null;
        float minDistance = range;

        foreach (EnemyAI enemy in RoundManager.Instance.SpawnedEnemies)
        {
            if (enemy.isEnemyDead || (enemy is not DriftwoodMenaceAI && enemy.enemyType.enemyName != "DriftWoodGiant" && enemy.enemyType.enemyName != "ForestGiant"))
                continue;

            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance < minDistance && Vector3.Distance(enemy.transform.position, shipBoundaries.position) > distanceFromShip)
            {
                minDistance = distance;
                closestEnemy = enemy;
            }
        }
        if (closestEnemy != null)
        {
            SetEnemyTargetServerRpc(new NetworkBehaviourReference(closestEnemy));
            return true;
        }
        return false;
    }

    public IEnumerator EatTargetEnemy(EnemyAI targetEnemy)
    {
        foreach (AudioSource audioSource in targetEnemy.GetComponentsInChildren<AudioSource>())
        {
            audioSource.mute = true;
        }
        creatureVoice.PlayOneShot(eatenSound, 1);
        if (IsServer)
        {
            creatureNetworkAnimator.SetTrigger(eatEnemyGiant);
            if (targetEnemy is DriftwoodMenaceAI driftwoodMenaceAI)
            {
                driftwoodMenaceAI.SwitchToBehaviourServerRpc((int)DriftwoodMenaceAI.DriftwoodState.Grabbed);
                driftwoodMenaceAI.creatureAnimator.SetBool(DriftwoodMenaceAI.GrabbedAnimation, true);
            }
        }
        SwitchToBehaviourStateOnLocalClient((int)State.EatingTargetGiant);
        smartAgentNavigator.StopAgent();
        agent.speed = 0f;
        yield return new WaitForSeconds(eating.length + 5f);
        if (isEnemyDead)
            yield break;

        foreach (EnemyAI enemy in RoundManager.Instance.SpawnedEnemies)
        {
            if (enemy is not RadMechAI radMech)
                continue;

            if (radMech.focusedThreatTransform == targetEnemy.transform)
            {
                radMech.targetedThreatCollider = null;
                radMech.CheckSightForThreat();
            }
        }
        agent.angularSpeed = 40f;
        agent.speed = walkingSpeed;
        if (IsServer)
        {
            smartAgentNavigator.StartSearchRoutine(50);
        }
        SwitchToBehaviourStateOnLocalClient((int)State.Wandering);
    }

    public override void OnCollideWithPlayer(Collider other)
    {
        if (isEnemyDead) return;
        PlayerControllerB player = MeetsStandardPlayerCollisionConditions(other);
        if (player == null) return;
        player.KillPlayer(player.velocityLastFrame, true, CauseOfDeath.Crushing, 0, default);
        // play player death particles.
    }

    public override void OnCollideWithEnemy(Collider other, EnemyAI collidedEnemy)
    {
        if (isEnemyDead) return;
        if (collidedEnemy == targetEnemy && !eatingEnemy && currentBehaviourStateIndex == (int)State.RunningToTarget && agent.velocity.magnitude >= 1f)
        {
            eatPosition = transform.position;
            eatingEnemy = true;
            collidedEnemy.agent.enabled = false;
            foreach (Collider enemyCollider in collidedEnemy.GetComponents<Collider>())
            {
                enemyCollider.enabled = false;
            }
            foreach (Collider enemyCollider in collidedEnemy.GetComponentsInChildren<Collider>())
            {
                enemyCollider.enabled = false;
            }
            Plugin.ExtendedLogging("Eating Giant");
            StartCoroutine(EatTargetEnemy(collidedEnemy));
        }
    }

    public bool RWHasLineOfSightToPosition(Vector3 pos, float width, float range, float proximityAwareness)
    {
        if (Vector3.Distance(eye.position, pos) < range && !Physics.Linecast(eye.position, pos, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
        {
            Vector3 to = pos - eye.position;
            if (Vector3.Angle(eye.forward, to) < width || Vector3.Distance(transform.position, pos) < proximityAwareness)
            {
                return true;
            }
        }
        return false;
    }

    public override void HitEnemy(int force = 1, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
        if (isEnemyDead) return;
        if (force >= 6)
        {
            // Set on fire and stuff
            if (TargetClosestRadMech(seeableDistance) && currentBehaviourStateIndex == (int)State.Wandering && Plugin.ModConfig.ConfigRedwoodCanEatOldBirds.Value)
            {
                if (IsServer)
                {
                    smartAgentNavigator.StopSearchRoutine();
                    StartCoroutine(SetSpeedForChasingGiant());
                }
                Plugin.ExtendedLogging("Start Target Giant");
                SwitchToBehaviourStateOnLocalClient((int)State.RunningToTarget);
            }
        }

        enemyHP -= force;

        if (enemyHP <= 0 && !isEnemyDead)
        {
            if (currentBehaviourStateIndex == (int)State.EatingTargetGiant)
            {
                enemyHP = 1;
            }
            else
            {
                if (IsOwner)
                {
                    KillEnemyOnOwnerClient();
                }
            }
        }
        Plugin.ExtendedLogging(enemyHP.ToString());
    }

    public override void KillEnemy(bool destroy = false)
    {
        base.KillEnemy(destroy);
        CollisionFootL.enabled = false;
        CollisionFootR.enabled = false;
        if (IsServer)
        {
            smartAgentNavigator.StopSearchRoutine();
            creatureAnimator.SetBool(IsDeadAnimation, true);
        }
        if (targetEnemy != null && targetEnemy.agent != null)
        {
            if (IsServer && targetEnemy is DriftwoodMenaceAI driftwoodMenaceAI)
            {
                driftwoodMenaceAI.SwitchToBehaviourServerRpc((int)DriftwoodMenaceAI.DriftwoodState.SearchingForPrey);
                driftwoodMenaceAI.creatureAnimator.SetBool(DriftwoodMenaceAI.GrabbedAnimation, false);
            }
            targetEnemy.agent.enabled = true;
        }
        //SpawnHeartOnDeath(transform.position);
    }

    public bool TargetClosestRadMech(float range)
    {
        EnemyAI? closestEnemy = null;
        float minDistance = float.MaxValue;

        foreach (EnemyAI enemy in RoundManager.Instance.SpawnedEnemies)
        {
            if (enemy.isEnemyDead || enemy is not RadMechAI) continue;

            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance < range && distance < minDistance)
            {
                minDistance = distance;
                closestEnemy = enemy;
            }
        }
        if (closestEnemy != null)
        {
            targetEnemy = closestEnemy;
            return true;
        }
        return false;
    }

    public void FinishKicking()
    { // AnimEvent
        kickingOut = false;
        Plugin.ExtendedLogging("Kick ended");
        playerToKick = null;
        agent.speed = walkingSpeed;
    }

    public void KickingOutStart()
    { // AnimEvent
        kickingOut = true;
    }

    public void WanderAroundAfterSpawnAnimation()
    { // AnimEvent
        if (IsServer)
        {
            smartAgentNavigator.StartSearchRoutine(50);
        }
        Plugin.ExtendedLogging("Start Walking Around");
        agent.speed = walkingSpeed;
        SwitchToBehaviourStateOnLocalClient((int)State.Wandering);
    }

    public void OnLandFromJump()
    { // AnimEvent
        var localPlayer = GameNetworkManager.Instance.localPlayerController;
        if (localPlayer.isPlayerDead || !localPlayer.isPlayerControlled || localPlayer.isInHangarShipRoom) return;
        if (Vector3.Distance(CollisionFootL.transform.position, localPlayer.transform.position) <= 20f || Vector3.Distance(CollisionFootR.transform.position, localPlayer.transform.position) <= 20f)
        {
            localPlayer.DamagePlayer(200, false, true, CauseOfDeath.Crushing, 0, false, localPlayer.velocityLastFrame);
            PlayMiscSoundsServerRpc(2);
        }
        jumping = false;
        BigSmokeEffect.Play();
        Plugin.ExtendedLogging("End Jump");
        agent.speed = walkingSpeed;
    }

    public void EnableDeathColliders()
    { // AnimEvent
        ToggleDeathColliders(true);
    }

    public void DisableDeathColliders()
    { // AnimEvent
        DeathParticles.Play();
        ToggleDeathColliders(false);
    }

    private void ToggleDeathColliders(bool enable)
    {
        foreach (Collider deathCollider in DeathColliders)
        {
            deathCollider.gameObject.GetComponent<BetterCooldownTrigger>().enabledScript = enable;
        }
    }

    public void EatingTargetGiant()
    { // AnimEvent
        if (targetEnemy == null || isEnemyDead) return;
        ParticlesFromEatingForestKeeper(targetEnemy);
        eatingEnemy = false;
        sizeUp = false;
        targetEnemy = null;
    }

    public void LeftFootStepInteractions()
    { // AnimEvent
        FootstepInteractions(ref CollisionFootL, ref DustParticlesLeft);
    }

    public void RightFootStepInteractions()
    { // AnimEvent
        FootstepInteractions(ref CollisionFootR, ref DustParticlesRight);
    }

    public void FootstepInteractions(ref Collider foot, ref ParticleSystem footParticles)
    {
        footParticles.Play(); // Play the particle system with the updated color
        PlayerControllerB player = GameNetworkManager.Instance.localPlayerController;
        if (player.IsSpawned && player.isPlayerControlled && !player.isPlayerDead && !player.isInHangarShipRoom)
        {
            float distance = Vector3.Distance(foot.transform.position, player.transform.position);
            if (distance <= 10f)
            {
                player.DamagePlayer(10, causeOfDeath: CauseOfDeath.Crushing);
            }
        }
        var enemiesList = RoundManager.Instance.SpawnedEnemies; //todo: change this to a spherecast
        for (int i = enemiesList.Count - 1; i >= 0; i--)
        {
            if (enemiesList[i] == null || enemiesList[i].isEnemyDead || enemiesList[i] is RedwoodTitanAI) continue;
            var FootDistance = Vector3.Distance(foot.transform.position, enemiesList[i].transform.position);
            if (FootDistance <= 7.5f)
            {
                DealEnemyDamageFromShockwave(enemiesList[i], FootDistance);
            }
        }
    }

    public void SpawnHeartOnDeath(Vector3 position)
    {
        if (Plugin.ModConfig.ConfigRedwoodHeartEnabled.Value && IsHost)
        {
            CodeRebirthUtils.Instance.SpawnScrapServerRpc("RedwoodHeart", position);
        }
    }
}