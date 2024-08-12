using System.Collections;
using GameNetcodeStuff;
using UnityEngine;
using System.Linq;
using UnityEngine.AI;
using CodeRebirth.src.Util;
using Unity.Netcode.Components;

namespace CodeRebirth.src.Content.Enemies;
public class RedwoodTitanAI : CodeRebirthEnemyAI, IVisibleThreat {
    
    public ParticleSystem DustParticlesLeft = null!;
    public Collider[] DeathColliders = null!;
    public ParticleSystem DustParticlesRight = null!;
    public ParticleSystem ForestKeeperParticles = null!;
    public ParticleSystem DriftwoodGiantParticles = null!;
    public ParticleSystem OldBirdParticles = null!;
    public ParticleSystem DeathParticles = null!;
    public Collider CollisionFootR = null!;
    public Collider CollisionFootL = null!;
    public AudioSource FootSource = null!;
    public AudioSource EnemyMouthSource = null!;
    public AnimationClip eating = null!;
    public AudioClip[] stompSounds = null!;
    public AudioClip eatenSound = null!;
    public AudioClip spawnSound = null!;
    public AudioClip roarSound = null!;
    public GameObject holdingBone = null!;
    public GameObject eatingArea = null!;
    private bool sizeUp = false;
    private bool eatingEnemy = false;
    private float walkingSpeed = 0f;
    private float seeableDistance = 0f;
    private float distanceFromShip = 0f;
    private Transform shipBoundaries = null!;
    private bool testBuild = false; 
    private LineRenderer line = null!;
    private Collider[] enemyColliders = null!;
    public NetworkAnimator networkAnimator = null!;
    private bool startedKick = false;

    #region ThreatType
    ThreatType IVisibleThreat.type => ThreatType.ForestGiant;
    int IVisibleThreat.SendSpecialBehaviour(int id) {
        return 0; 
    }
    int IVisibleThreat.GetThreatLevel(Vector3 seenByPosition) {
        return 18;
    }
    int IVisibleThreat.GetInterestLevel() {
        return 0;
    }
    Transform IVisibleThreat.GetThreatLookTransform() {
        return eye;
    }
    Transform IVisibleThreat.GetThreatTransform() {
        return base.transform;
    }
    Vector3 IVisibleThreat.GetThreatVelocity() {
        if (base.IsOwner) {
            return agent.velocity;
        }
        return Vector3.zero;
    }
    float IVisibleThreat.GetVisibility() {
        if (isEnemyDead) {
            return 0f;
        }
        if (agent.velocity.sqrMagnitude > 0f) {
            return 1f;
        }
        return 0.75f;
    }
    #endregion
    enum State {
        SpawnAnimation, // Roaring
        IdleAnimation, // Idling
        WanderingLand, // Wandering
        RunningToTarget, // Chasing
        EatingTargetGiant, // Eating
    }
    public override void Start() {
        base.Start();
#if DEBUG
        testBuild = true;
#endif

        walkingSpeed = Plugin.ModConfig.ConfigRedwoodSpeed.Value;
        distanceFromShip = Plugin.ModConfig.ConfigRedwoodShipPadding.Value;
        seeableDistance = Plugin.ModConfig.ConfigRedwoodEyesight.Value;
        shipBoundaries = StartOfRound.Instance.shipBounds.transform;

        enemyColliders = GetComponentsInChildren<Collider>();

        if (testBuild) {
            line = gameObject.AddComponent<LineRenderer>();
            line.widthMultiplier = 0.2f; // reduce width of the line
        }

        FootSource.PlayOneShot(spawnSound);
        EnemyMouthSource.PlayOneShot(roarSound);
        agent.speed = 0f;
        if (IsServer) networkAnimator.SetTrigger("spawnAnimation");
        SwitchToBehaviourStateOnLocalClient((int)State.SpawnAnimation);
        CodeRebirthPlayerManager.OnDoorStateChange += OnShipDoorStateChange;
        StartCoroutine(UpdateAudio());
    }

    private IEnumerator UpdateAudio() {
        while (!isEnemyDead) {
            if (GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom) {
                FootSource.volume = Plugin.ModConfig.ConfigRedwoodNormalVolume.Value * Plugin.ModConfig.ConfigRedwoodInShipVolume.Value;
                EnemyMouthSource.volume = Plugin.ModConfig.ConfigRedwoodNormalVolume.Value * Plugin.ModConfig.ConfigRedwoodInShipVolume.Value;
                creatureSFX.volume = Plugin.ModConfig.ConfigRedwoodNormalVolume.Value * Plugin.ModConfig.ConfigRedwoodInShipVolume.Value;
                creatureVoice.volume = Plugin.ModConfig.ConfigRedwoodNormalVolume.Value * Plugin.ModConfig.ConfigRedwoodInShipVolume.Value;
            } else {
                creatureSFX.volume = Plugin.ModConfig.ConfigRedwoodNormalVolume.Value;
                creatureVoice.volume = Plugin.ModConfig.ConfigRedwoodNormalVolume.Value;
                FootSource.volume = Plugin.ModConfig.ConfigRedwoodNormalVolume.Value;
                EnemyMouthSource.volume = Plugin.ModConfig.ConfigRedwoodNormalVolume.Value;
            }
            yield return new WaitForSeconds(0.5f);
        }
    }
    private void OnShipDoorStateChange(object sender, bool shipDoorClosed)
    {
        if (shipDoorClosed) {
            foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts) {
                if (player.isInHangarShipRoom) {
                    foreach (Collider playerCollider in player.GetCRPlayerData().playerColliders!) {
                        if (playerCollider == null) continue;
                        foreach (Collider enemyCollider in enemyColliders) {
                            Physics.IgnoreCollision(playerCollider, enemyCollider, true);
                        }
                    }
                }
            }
        } else {
            foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts) {
                foreach (Collider playerCollider in player.GetCRPlayerData().playerColliders!) {
                    foreach (Collider enemyCollider in enemyColliders) {
                        Physics.IgnoreCollision(playerCollider, enemyCollider, false);
                    }
                }
            }
        }
    }

    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();
        CodeRebirthPlayerManager.OnDoorStateChange -= OnShipDoorStateChange;
    }

    public override void EnableEnemyMesh(bool enable, bool overrideDoNotSet = false) {
        base.EnableEnemyMesh(enable);
    }

    public void LateUpdate() {
        if (currentBehaviourStateIndex == (int)State.EatingTargetGiant && targetEnemy != null) {
            var grabPosition = holdingBone.transform.position;
            targetEnemy.transform.position = grabPosition + new Vector3(0, -1f, 0);
            targetEnemy.transform.LookAt(eatingArea.transform.position);
            targetEnemy.transform.position = grabPosition + new Vector3(0, -5.5f, 0);
            // Scale targetEnemy's transform down by 0.9995 everytime Update runs in this if statement
            if (!sizeUp) {
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
    }

    public void WanderAroundAnimEvent() {
        if (IsServer) networkAnimator.SetTrigger("startWalk");
        Plugin.ExtendedLogging("Start Walking Around");
        StartSearch(this.transform.position);
        agent.speed = walkingSpeed;
        SwitchToBehaviourStateOnLocalClient((int)State.WanderingLand);
    }

    public void DoWanderingLand() {
        if (FindClosestAliveGiantInRange(seeableDistance)) {
            networkAnimator.SetTrigger("startChase");
            Plugin.ExtendedLogging("Start Target Giant");
            StopSearch(currentSearch);
            SwitchToBehaviourServerRpc((int)State.RunningToTarget);
            agent.speed = walkingSpeed * 4;
        } // Look for Forest Keeper
        var closestPlayer = StartOfRound.Instance.allPlayerScripts
            .Where(x => x.IsSpawned && x.isPlayerControlled && !x.isPlayerDead)
            .Select(player => new {
                Player = player,
                Distance = Vector3.Distance(player.transform.position, transform.position)
            })
            .Where(x => x.Distance < seeableDistance)
            .OrderBy(x => x.Distance)
            .FirstOrDefault(x => RWHasLineOfSightToPosition(x.Player.transform.position, 120, seeableDistance, 5));
        if (closestPlayer != null) {
            DoFunnyThingWithNearestPlayer(closestPlayer.Player);
        }
    }

    public void DoFunnyThingWithNearestPlayer(PlayerControllerB closestPlayer) {
        var distanceToClosestPlayer = Vector3.Distance(transform.position, closestPlayer.transform.position);
        if (distanceToClosestPlayer <= 5f && UnityEngine.Random.Range(0f, 100f) <= 1f) {
            JumpInPlace();   
        } else if ((distanceToClosestPlayer <= 10f && UnityEngine.Random.Range(0f, 100f) <= 1f) || startedKick) {
            DoKickTargetPlayer(closestPlayer);
        }
    }

    public void OnLandAnimEvent() {
        foreach (var player in StartOfRound.Instance.allPlayerScripts) {
            if (Vector3.Distance(transform.position, player.transform.position) < 0.5f) {
                player.KillPlayer(player.velocityLastFrame, true, CauseOfDeath.Crushing, 0, default);
            }
        }
    }
    public void JumpInPlace() {
        if (IsServer) networkAnimator.SetTrigger("startJump");    
    }

    public void DoKickTargetPlayer(PlayerControllerB closestPlayer) {
        if (!startedKick) {
            startedKick = true;
            agent.speed = 1f;
            StartCoroutine(KickTimer());
            if (IsServer) networkAnimator.SetTrigger("startKick");
            Plugin.ExtendedLogging("Start Kick");
        }
        Vector3 targetPosition = closestPlayer.transform.position;
        Vector3 direction = (targetPosition - this.transform.position).normalized;
        direction.y = 0; // Keep the y component zero to prevent vertical rotation

        if (direction != Vector3.zero) {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            this.transform.rotation = Quaternion.Slerp(this.transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }

    public IEnumerator KickTimer() {
        yield return new WaitUntil(() => creatureAnimator.GetCurrentAnimatorClipInfo(0)[0].clip.name != "startKick");
        
        startedKick = false;
        Plugin.ExtendedLogging("Kick ended");
        SwitchToBehaviourServerRpc((int)State.WanderingLand);
    }
    public void DoRunningToTarget() {
        // Keep targetting closest Giant, unless they are over 20 units away and we can't see them.
        if (targetEnemy == null) {
            Plugin.ExtendedLogging("Stop Target Giant");
            networkAnimator.SetTrigger("startWalk");
            StartSearch(this.transform.position);
            agent.speed = walkingSpeed;
            SwitchToBehaviourServerRpc((int)State.WanderingLand);
            return;
        }
        if (Vector3.Distance(transform.position, targetEnemy.transform.position) > seeableDistance+10 && !RWHasLineOfSightToPosition(targetEnemy.transform.position, 120, seeableDistance, 5)) {
            Plugin.ExtendedLogging("Stop Target Giant");
            networkAnimator.SetTrigger("startWalk");
            StartSearch(this.transform.position);
            agent.speed = walkingSpeed;
            SwitchToBehaviourServerRpc((int)State.WanderingLand);
            return;
        }
        SetDestinationToPosition(targetEnemy.transform.position, checkForPath: true);
    }

    public override void DoAIInterval() {
        if (testBuild) { 
            StartCoroutine(DrawPath(line, agent));
        }
        base.DoAIInterval();
        if (isEnemyDead || StartOfRound.Instance.allPlayersDead || !IsHost) {
            return;
        }
        switch(currentBehaviourStateIndex) {
            case (int)State.SpawnAnimation:
                break;
            case (int)State.IdleAnimation:
                break;
            case (int)State.WanderingLand:
                DoWanderingLand();
                break;
            case (int)State.RunningToTarget:
                DoRunningToTarget();
                break;
            case (int)State.EatingTargetGiant:
                break;
            default:
                Plugin.Logger.LogWarning("This Behavior State doesn't exist!");
                break;
        }
    }
    public static IEnumerator DrawPath(LineRenderer line, NavMeshAgent agent) {
        if (!agent.enabled) yield break;
        yield return new WaitForEndOfFrame();
        line.SetPosition(0, agent.transform.position); //set the line's origin

        line.positionCount = agent.path.corners.Length; //set the array of positions to the amount of corners
        for (var i = 1; i < agent.path.corners.Length; i++)
        {
            line.SetPosition(i, agent.path.corners[i]); //go through each corner and set that to the line renderer's position
        }
    }
    public void DealEnemyDamageFromShockwave(EnemyAI enemy, string foot, float distanceFromEnemy) {
        Transform chosenFoot = (foot == "LeftFoot") ? CollisionFootL.transform : CollisionFootR.transform;

        // Apply damage based on distance
        if (distanceFromEnemy <= 3f) {
            enemy.HitEnemy(2, null, false, -1);
        } else if (distanceFromEnemy <= 10f) {
            enemy.HitEnemy(1, null, false, -1);
        }

        // Optional: Log the distance and remaining HP for debugging
        // Plugin.ExtendedLogging($"Distance: {distanceFromEnemy} HP: {enemy.enemyHP}");
    }

    public void LeftFootStepInteractions() {
        DustParticlesLeft.Play(); // Play the particle system with the updated color
        FootSource.PlayOneShot(stompSounds[UnityEngine.Random.Range(0, stompSounds.Length)]);
        PlayerControllerB player = GameNetworkManager.Instance.localPlayerController;
        if (player.IsSpawned && player.isPlayerControlled && !player.isPlayerDead) {
            float distance = Vector3.Distance(CollisionFootL.transform.position, player.transform.position);
            if (distance <= 10f && !player.isInHangarShipRoom) {
                player.DamagePlayer(10, causeOfDeath: CauseOfDeath.Crushing);
            }
        }
        var enemiesList = RoundManager.Instance.SpawnedEnemies;
        foreach (var enemy in enemiesList) {
            if (enemy == null) continue;
            var LeftFootDistance = Vector3.Distance(CollisionFootL.transform.position, enemy.transform.position);
            if (LeftFootDistance <= 7.5f) {
                DealEnemyDamageFromShockwave(enemy, "LeftFoot", LeftFootDistance);
            }
        }
    }
    public void RightFootStepInteractions() {
        DustParticlesRight.Play(); // Play the particle system with the updated color
        FootSource.PlayOneShot(stompSounds[UnityEngine.Random.Range(0, stompSounds.Length)]);
        PlayerControllerB player = GameNetworkManager.Instance.localPlayerController;
        if (player.IsSpawned && player.isPlayerControlled && !player.isPlayerDead) {
            float distance = Vector3.Distance(CollisionFootR.transform.position, player.transform.position);
            if (distance <= 10f && !player.isInHangarShipRoom) {
                player.DamagePlayer(10, causeOfDeath: CauseOfDeath.Crushing);
            }
        }
        var enemiesList = RoundManager.Instance.SpawnedEnemies;
        foreach (var enemy in enemiesList) {
            if (enemy == null) continue;
            var RightFootDistance = Vector3.Distance(CollisionFootR.transform.position, enemy.transform.position);
            if (RightFootDistance <= 7.5f) {
                DealEnemyDamageFromShockwave(enemy, "RightFoot", RightFootDistance);
            }
        }
    }

    public void ParticlesFromEatingForestKeeper(EnemyAI targetEnemy) {
        if (targetEnemy.enemyType.enemyName == "ForestGiant") {
            ForestKeeperParticles.Play(); // Also make them be affected by the world for proper fog stuff?
        } else if (targetEnemy.enemyType.enemyName == "DriftWoodGiant") {
            DriftwoodGiantParticles.Play();
        } else if (targetEnemy.enemyType.enemyName == "RadMech") {
            OldBirdParticles.Play();
        }
        Plugin.ExtendedLogging(targetEnemy.enemyType.enemyName);
        targetEnemy.KillEnemyOnOwnerClient(overrideDestroy: true);
    }
    
    public void ShakePlayerCamera() {
            float distance = Vector3.Distance(transform.position, GameNetworkManager.Instance.localPlayerController.transform.position);
            switch (distance) {
                case < 10f:
                
                    HUDManager.Instance.ShakeCamera(ScreenShakeType.Long);
                    HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
                    HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);

                    HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
                    HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
                    break;
                case < 20 and >= 10:
                    HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
                    HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
                    HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
                    break;
                case < 50f and >= 20:
                    HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
                    HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
                    break;
            }
    }
    public bool FindClosestAliveGiantInRange(float range) {
        EnemyAI? closestEnemy = null;
        float minDistance = float.MaxValue;

        foreach (EnemyAI enemy in RoundManager.Instance.SpawnedEnemies) {
            string enemyName = enemy.enemyType.enemyName;
            if ((enemyName == "ForestGiant" || enemyName == "DriftWoodGiant") && !enemy.isEnemyDead) {
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance < range && distance < minDistance && Vector3.Distance(enemy.transform.position, shipBoundaries.position) > distanceFromShip) {
                    minDistance = distance;
                    closestEnemy = enemy;
                }
            }
        }
        if (closestEnemy != null) {
            SetEnemyTargetServerRpc(RoundManager.Instance.SpawnedEnemies.IndexOf(closestEnemy));
            return true;
        }
        return false;
    }

    public IEnumerator EatTargetEnemy(EnemyAI targetEnemy) {
        targetEnemy.SetEnemyStunned(true, 10f);
        foreach (AudioSource audioSource in targetEnemy.GetComponentsInChildren<AudioSource>()) {
            audioSource.mute = true;
        }
        EnemyMouthSource.PlayOneShot(eatenSound, 1);
        networkAnimator.SetTrigger("eatForestKeeper");
        SwitchToBehaviourStateOnLocalClient((int)State.EatingTargetGiant);
        agent.speed = 0f;
        yield return new WaitForSeconds(eating.length);
        if (isEnemyDead) yield break;
        foreach (RadMechAI enemy in RoundManager.Instance.SpawnedEnemies)
        {
            if (enemy == null) continue;
            RadMechAI rad = enemy;
            rad.TryGetComponent(out IVisibleThreat threat);
            if (threat != null && rad.focusedThreatTransform == threat.GetThreatTransform())
            {
                Plugin.ExtendedLogging("Stuff is happening!!");
                rad.targetedThreatCollider = null;
                rad.CheckSightForThreat();
            }
        }
        if (IsServer) networkAnimator.SetTrigger("startWalk");
        SwitchToBehaviourStateOnLocalClient((int)State.WanderingLand);
    }

    public void EatingTargetGiant() {
        if (targetEnemy == null) return;
        ParticlesFromEatingForestKeeper(targetEnemy);
        eatingEnemy = false;
        sizeUp = false;
    }

    public override void OnCollideWithEnemy(Collider other, EnemyAI collidedEnemy)  {
        if (isEnemyDead) return;
        if (collidedEnemy == targetEnemy && !eatingEnemy && currentBehaviourStateIndex == (int)State.RunningToTarget) {
            eatingEnemy = true;
            foreach (Collider enemyCollider in targetEnemy.GetComponentsInChildren<Collider>()) {
                enemyCollider.enabled = false;
            }
            StartCoroutine(EatTargetEnemy(targetEnemy));
        }
    }

    public bool RWHasLineOfSightToPosition(Vector3 pos, float width = 120f, float range = 60, float proximityAwareness = -1f) {
        if (Vector3.Distance(eye.position, pos) < range && !Physics.Linecast(eye.position, pos, StartOfRound.Instance.collidersAndRoomMaskAndDefault)) {
            Vector3 to = pos - eye.position;
            if (Vector3.Angle(eye.forward, to) < width || Vector3.Distance(transform.position, pos) < proximityAwareness) {
                return true;
            }
        }
        return false;
    }

    public override void HitEnemy(int force = 1, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1) {
        base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
        if (isEnemyDead) return;
        if (force >= 6) {
            enemyHP -= force;
            if (TargetClosestRadMech(seeableDistance+25) && currentBehaviourStateIndex == (int)State.WanderingLand && Plugin.ModConfig.ConfigRedwoodCanEatOldBirds.Value) {
                if (IsServer) networkAnimator.SetTrigger("startChase");
                Plugin.ExtendedLogging("Start Target Giant");
                StopSearch(currentSearch);
                SwitchToBehaviourStateOnLocalClient((int)State.RunningToTarget);
            }
        } else {
            enemyHP -= force;
        }

        if (enemyHP <= 0 && !isEnemyDead) {
            if (currentBehaviourStateIndex == (int)State.EatingTargetGiant) {
                enemyHP++;
            } else {
                if (IsOwner) {
                    KillEnemyOnOwnerClient();
                }
            }
        }
        Plugin.ExtendedLogging(enemyHP.ToString());
    }

    public override void KillEnemy(bool destroy = false) { 
        base.KillEnemy(destroy);
        transform.Find("Armature").Find("Bone.008.L.002").Find("Bone.008.L.002_end").Find("CollisionFootL").GetComponent<BoxCollider>().enabled = false;
        transform.Find("Armature").Find("Bone.008.R.001").Find("Bone.008.R.001_end").Find("CollisionFootR").GetComponent<BoxCollider>().enabled = false;
        if (IsServer) networkAnimator.SetTrigger("startDeath");
        SpawnHeartOnDeath(transform.position);
    }

    public bool TargetClosestRadMech(float range) {
        EnemyAI? closestEnemy = null;
        float minDistance = float.MaxValue;

        foreach (EnemyAI enemy in RoundManager.Instance.SpawnedEnemies) {
            string enemyName = enemy.enemyType.enemyName;
            if (enemyName == "RadMech" && !enemy.isEnemyDead) {
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance < range && distance < minDistance) {
                    minDistance = distance;
                    closestEnemy = enemy;
                }
            }
        }
        if (closestEnemy != null) {
            targetEnemy = closestEnemy;
            return true;
        }
        return false;
    }

    public void EnableDeathColliders() {
        foreach (Collider deathCollider in DeathColliders) {
            deathCollider.enabled = true;
        }
    }

    public void DisableDeathColliders() {
        DeathParticles.Play();
        foreach (Collider deathCollider in DeathColliders) {
            deathCollider.enabled = false;
        }
    }

    public void SpawnHeartOnDeath(Vector3 position) {
        if (Plugin.ModConfig.ConfigRedwoodHeartEnabled.Value && IsHost && !Plugin.LGUIsOn) {
            CodeRebirthUtils.Instance.SpawnScrapServerRpc("RedWoodGiant", position);
        }
    }
}