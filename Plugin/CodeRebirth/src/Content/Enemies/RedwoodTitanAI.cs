using System.Collections;
using GameNetcodeStuff;
using UnityEngine;
using CodeRebirth.src.Util;
using Unity.Netcode;
using CodeRebirth.src.MiscScripts;
using System.Collections.Generic;
using System.Linq;

namespace CodeRebirth.src.Content.Enemies;
public class RedwoodTitanAI : CodeRebirthEnemyAI, IVisibleThreat
{
    public Material AlbinoCharredMaterial = null!;
    public Material NormalCharredMaterial = null!;
    public ParticleSystem[] BurnParticles = [];
    public AudioClip BurningSound = null!;
    public Collider[] DeathColliders = null!;
    public Collider CollisionFootR = null!;
    public Collider CollisionFootL = null!;
    public ParticleSystem DustParticlesLeft = null!;
    public ParticleSystem DustParticlesRight = null!;
    public ParticleSystem ForestKeeperParticles = null!;
    public ParticleSystem DriftwoodGiantParticles = null!;
    public ParticleSystem CactusBudlingParticles = null!;
    public ParticleSystem OldBirdParticles = null!;
    public ParticleSystem DeathParticles = null!;
    public AudioSource creatureSFXFar = null!;
    public AudioClip eatenSound = null!;
    public AudioClip roarSound = null!;
    public AudioClip crunchySquishSound = null!;
    public GameObject holdingBone = null!;
    public GameObject eatingArea = null!;
    public AnimationClip eating = null!;

    private Vector3 eatPosition = Vector3.zero;
    private bool sizeUp = false;
    private bool eatingEnemy = false;
    private float walkingSpeed = 0f;
    private float seeableDistance = 0f;
    private float distanceFromShip = 0f;
    private Transform shipBoundaries = null!;
    private Collider[] enemyColliders = null!;
    private static readonly int startEnrage = Animator.StringToHash("startEnrage"); // Trigger
    private static readonly int eatEnemyGiant = Animator.StringToHash("eatEnemyGiant"); // Trigger
    private static readonly int IsDeadAnimation = Animator.StringToHash("IsDead"); // Bool
    private static readonly int RunSpeedFloat = Animator.StringToHash("RunSpeedFloat"); // Float

    internal static int instanceNumbers = 0;

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
            case 2:
                creatureSFX.PlayOneShot(crunchySquishSound);
                creatureSFXFar.PlayOneShot(crunchySquishSound);
                break;
            case 3:
                creatureVoice.PlayOneShot(roarSound);
                break;
        }
    }
    public override void DoAIInterval()
    {
        base.DoAIInterval();
        if (isEnemyDead || StartOfRound.Instance.allPlayersDead)
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
            PlayMiscSoundsClientRpc(3);
            SwitchToBehaviourServerRpc((int)State.RunningToTarget);
            StartCoroutine(SetSpeedForChasingGiant());
            return;
        } // Look for Giants
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
            enemy.HitEnemy(2 * (_burnGiantRoutine != null ? 2 : 1), null, false, -1);
        }
        else if (distanceFromEnemy <= 10f)
        {
            enemy.HitEnemy(1 * (_burnGiantRoutine != null ? 2 : 1), null, false, -1);
        }

        if (_burnGiantRoutine != null)
        {
            enemy.HitFromExplosion(distanceFromEnemy);
        }
    }


    public void ParticlesFromEatingForestKeeper(EnemyAI targetEnemy)
    {
        if (targetEnemy is ForestGiantAI)
        {
            ForestKeeperParticles.Play();
        }
        else if (targetEnemy is DriftwoodMenaceAI)
        {
            DriftwoodGiantParticles.Play();
        }
        else if (targetEnemy is CactusBudling)
        {
            CactusBudlingParticles.Play();
        }
        else if (targetEnemy is RadMechAI)
        {
            OldBirdParticles.Play();
        }

        targetEnemy.KillEnemyOnOwnerClient(overrideDestroy: true);
    }

    public bool FindClosestAliveGiantInRange(float range)
    {
        EnemyAI? closestEnemy = null;
        float minDistance = range;

        foreach (EnemyAI enemy in RoundManager.Instance.SpawnedEnemies)
        {
            if (enemy.isEnemyDead || (enemy is not DriftwoodMenaceAI && enemy is not ForestGiantAI && enemy is not CactusBudling))
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
            else if (targetEnemy is CactusBudling cactusBudling)
            {
                cactusBudling.SwitchToBehaviourServerRpc((int)CactusBudling.CactusBudlingState.Grabbed);
                cactusBudling.creatureAnimator.SetBool(CactusBudling.GrabbedAnimation, true);
                cactusBudling.creatureAnimator.SetBool(CactusBudling.RollingAnimation, false);
                cactusBudling.creatureAnimator.SetBool(CactusBudling.RootingAnimation, false);
            }
        }
        SwitchToBehaviourStateOnLocalClient((int)State.EatingTargetGiant);
        smartAgentNavigator.StopAgent();
        agent.speed = 0f;
        yield return new WaitForSeconds(eating.length + 5f);
        if (isEnemyDead)
        {
            yield break;
        }

        if (targetEnemy != null)
        {
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
        }

        agent.angularSpeed = 40f;
        agent.speed = walkingSpeed;
        if (IsServer)
        {
            smartAgentNavigator.StartSearchRoutine(50);
        }
        SwitchToBehaviourStateOnLocalClient((int)State.Wandering);
    }

    private Coroutine? _burnGiantRoutine = null;
    [ServerRpc(RequireOwnership = false)]
    private void BurnGiantServerRpc()
    {
        BurnGiantClientRpc();
    }

    [ClientRpc]
    private void BurnGiantClientRpc()
    {
        _burnGiantRoutine = StartCoroutine(BurnGiant());
    }

    private bool burnedOnce = false;
    private IEnumerator BurnGiant()
    {
        foreach (ParticleSystem particleSystem in BurnParticles)
        {
            particleSystem.Play();
        }

        creatureSFX.clip = BurningSound;
        creatureSFX.loop = true;
        creatureSFX.Play();

        yield return new WaitForSeconds(30f);
        creatureSFX.Stop();
        if (!burnedOnce)
        {
            if (skinnedMeshRenderers[0].sharedMaterials[3].name == "AlbinoBody")
            {
                List<Material> materials = skinnedMeshRenderers[0].sharedMaterials.ToList();
                materials[3] = AlbinoCharredMaterial;
                skinnedMeshRenderers[0].SetSharedMaterials(materials);
            }
            else
            {
                List<Material> materials = skinnedMeshRenderers[0].sharedMaterials.ToList();
                materials[3] = NormalCharredMaterial;
                skinnedMeshRenderers[0].SetSharedMaterials(materials);
            }
            burnedOnce = true;
        }
        HitEnemy(20, null, false, -1);
        _burnGiantRoutine = null;
    }

    public override void HitFromExplosion(float distance)
    {
        base.HitFromExplosion(distance);
        if (_burnGiantRoutine != null)
        {
            return;
        }

        BurnGiantServerRpc();
    }

    public override void OnCollideWithPlayer(Collider other)
    {
        if (isEnemyDead)
        {
            return;
        }

        PlayerControllerB player = MeetsStandardPlayerCollisionConditions(other);
        if (player == null)
        {
            return;
        }
        player.KillPlayer(player.velocityLastFrame, true, CauseOfDeath.Crushing, _burnGiantRoutine != null ? 6 : 0, default);
        // play player death particles.
    }

    public override void OnCollideWithEnemy(Collider other, EnemyAI collidedEnemy)
    {
        if (isEnemyDead)
        {
            return;
        }

        if (collidedEnemy == targetEnemy && !eatingEnemy && currentBehaviourStateIndex == (int)State.RunningToTarget && agent.velocity.magnitude >= 1f)
        {
            eatPosition = transform.position;
            eatingEnemy = true;
            collidedEnemy.agent.enabled = false;

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
        if (isEnemyDead)
        {
            return;
        }

        if (hitID == Plugin.BURN_HIT_ID && _burnGiantRoutine == null)
        {
            _burnGiantRoutine = StartCoroutine(BurnGiant());
        }

        if (force >= 6)
        {
            if (TargetClosestRadMech(seeableDistance) && currentBehaviourStateIndex == (int)State.Wandering && Plugin.ModConfig.ConfigRedwoodCanEatOldBirds.Value)
            {
                if (IsServer)
                {
                    smartAgentNavigator.StopSearchRoutine();
                    StartCoroutine(SetSpeedForChasingGiant());
                }
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
            else if (IsServer && targetEnemy is CactusBudling cactusBudling)
            {
                cactusBudling.SwitchToBehaviourServerRpc((int)CactusBudling.CactusBudlingState.Spawning);
                cactusBudling.GetNextRootPosition();
                cactusBudling.creatureAnimator.SetBool(CactusBudling.GrabbedAnimation, false);
            }
            targetEnemy.agent.enabled = true;
        }
    }

    public bool TargetClosestRadMech(float range)
    {
        EnemyAI? closestEnemy = null;
        float minDistance = float.MaxValue;

        foreach (EnemyAI enemy in RoundManager.Instance.SpawnedEnemies)
        {
            if (enemy.isEnemyDead || enemy is not RadMechAI)
            {
                continue;
            }

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
        if (targetEnemy == null || isEnemyDead)
        {
            return;
        }

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
                player.DamagePlayer(10 * (_burnGiantRoutine != null ? 3 : 1), causeOfDeath: CauseOfDeath.Crushing);
            }
        }
        List<EnemyAI> enemiesList = RoundManager.Instance.SpawnedEnemies;
        for (int i = enemiesList.Count - 1; i >= 0; i--)
        {
            if (enemiesList[i] == null || enemiesList[i].isEnemyDead || enemiesList[i] is RedwoodTitanAI) continue;
            float FootDistance = Vector3.Distance(foot.transform.position, enemiesList[i].transform.position);
            if (FootDistance <= 7.5f)
            {
                DealEnemyDamageFromShockwave(enemiesList[i], FootDistance);
            }
        }
    }
}