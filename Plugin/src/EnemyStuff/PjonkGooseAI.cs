using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.Misc;
using CodeRebirth.ScrapStuff;
using CodeRebirth.src.EnemyStuff;
using CodeRebirth.Util.Extensions;
using CodeRebirth.Util.Spawning;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace CodeRebirth.EnemyStuff;
public class PjonkGooseAI : CodeRebirthEnemyAI
{
    private SimpleWanderRoutine currentWander;
    private PlayerControllerB playerWhoLastHit;
    private Coroutine wanderCoroutine;
    private const float WALKING_SPEED = 5f;
    private const float SPRINTING_SPEED = 20f;
    private bool isAggro = false;
    private int playerHits = 0;
    private bool carryingPlayerBody;
    private DeadBodyInfo bodyBeingCarried;
    public AudioClip GuardHyperVentilateClip;
    public AudioClip[] HonkSounds;
    public AudioClip StartStunSound;
    public AudioClip EndStunSound;
    public AudioClip SpawnSound;
    public AudioClip HissSound;
    public AudioClip EnrageSound;
    public AudioClip[] FootstepSounds;
    public AudioClip[] ViolentFootstepSounds;
    public AudioClip[] featherSounds;
    public AudioClip[] hitSounds;
    public AudioClip[] deathSounds;

    private GrabbableObject goldenEgg;
    private float timeSinceHittingLocalPlayer;
    private float timeSinceAction;
    private bool holdingEgg = false;
    public GameObject nest;
    private bool recentlyDamaged = false;
    private bool nestCreated = false;
    private bool isNestInside = true;
    private Coroutine recentlyDamagedCoroutine;
    private float collisionThresholdVelocity = SPRINTING_SPEED - 3.5f;
    private System.Random enemyRandom;
    private DoorLock[] doors;

    public enum State
    {
        Spawning,
        Wandering,
        Guarding,
        ChasingPlayer,
        ChasingEnemy,
        Death,
        Stunned,
        Idle,
        DragPlayerBodyToNest,
    }

    public override void Start()
    {
        base.Start();
        creatureVoice.pitch = 1.4f;
        doors = FindObjectsOfType(typeof(DoorLock)) as DoorLock[];
        enemyRandom = new System.Random(StartOfRound.Instance.randomMapSeed + 323);
        LogIfDebugBuild(RoundManager.Instance.currentLevel.maxOutsideEnemyPowerCount.ToString());
        LogIfDebugBuild(RoundManager.Instance.currentOutsideEnemyPower.ToString());
        timeSinceHittingLocalPlayer = 0;
        timeSinceAction = 0;
        creatureVoice.PlayOneShot(SpawnSound);
        if (!IsHost) return;
        ControlStateSpeedAnimationClientRpc(0f, (int)State.Spawning, false, false, false, -1, true, false);
        StartCoroutine(SpawnTimer());
    }

    public void PlayFootstepSound() {
        // creatureSFX.PlayOneShot(FootstepSounds[enemyRandom.Next(0, FootstepSounds.Length)]);
    } // Animation Event

    public void PlayViolentFootstepSound() {
        // creatureSFX.PlayOneShot(ViolentFootstepSounds[enemyRandom.Next(0, ViolentFootstepSounds.Length)]);
    } // Animation Event

    [ClientRpc]
    public void SpawnNestClientRpc() {
        if (IsHost) nest = Instantiate(nest, this.transform.position + Vector3.down * 0.02f, Quaternion.identity, RoundManager.Instance.mapPropsContainer.transform);
        if (IsHost) nest.GetComponent<NetworkObject>().Spawn(true);
        nestCreated = true;
        if (!isOutside) isNestInside = true;
        else isNestInside = false;
    }

    public void ApplyChasingSpeed() { // Animation Event
        this.ChangeSpeedOnLocalClient(SPRINTING_SPEED);
    }

    [ClientRpc]
    public void ControlStateSpeedAnimationClientRpc(float speed, int state, bool startSearch, bool running, bool guarding, int playerWhoStunnedIndex = -1, bool delaySpeed = true, bool _isAggro = false) {
        isAggro = _isAggro;
        if ((state == (int)State.ChasingPlayer || state == (int)State.ChasingEnemy) && delaySpeed) {
            this.ChangeSpeedOnLocalClient(0);
            this.agent.velocity = Vector3.zero; // rpc this
        }
        else this.ChangeSpeedOnLocalClient(speed);
        if (state == (int)State.Stunned) SetEnemyStunned(true, 5.317f, playerWhoStunnedIndex == -1 ? null : StartOfRound.Instance.allPlayerScripts[playerWhoStunnedIndex]); // need to rpc this
        this.SetFloatAnimationOnLocalClient("MoveZ", speed);
        this.SetBoolAnimationOnLocalClient("Running", running);
        this.SetBoolAnimationOnLocalClient("Guarding", guarding);
        this.SetBoolAnimationOnLocalClient("Aggro", isAggro);
        SwitchToBehaviourStateOnLocalClient((int)state);
        if (!IsHost) return;
        if (startSearch) {
            StartWandering(nest.transform.position);
        } else {
            StopWandering(this.currentWander);
        }
    }
    public IEnumerator SpawnTimer()
    {
        yield return new WaitForSeconds(1f);
        if (!nestCreated) SpawnNestClientRpc();
        if (goldenEgg == null) SpawnEggInNestClientRpc();
        yield return new WaitForSeconds(2f);
        if (currentBehaviourStateIndex == (int)State.Stunned || currentBehaviourStateIndex == (int)State.ChasingPlayer || currentBehaviourStateIndex == (int)State.ChasingEnemy || currentBehaviourStateIndex == (int)State.Death) yield break;
        ControlStateSpeedAnimationClientRpc(WALKING_SPEED, (int)State.Wandering, true, false, false, -1, true, false);
    }

    public EnemyAI FindYippeeHoldingEgg() {
        foreach (var enemy in RoundManager.Instance.SpawnedEnemies) {
            LogIfDebugBuild($"checking enemy: {enemy.enemyType.enemyName}");
            if (enemy.enemyType.enemyName == "HoarderBug") {
                HoarderBugAI hoarderBugAI = enemy as HoarderBugAI;
                if (hoarderBugAI.heldItem.itemGrabbableObject == goldenEgg) {
                    return enemy;
                }
            }
        }
        return null;
    }
    
    public override void Update()
    {
        base.Update();
        timeSinceAction += Time.deltaTime;
        timeSinceHittingLocalPlayer += Time.deltaTime;

        if (creatureAnimator.GetBool("Guarding")) {
            var targetPlayer = StartOfRound.Instance.allPlayerScripts
                .OrderBy(player => Vector3.Distance(player.transform.position, this.transform.position))
                .First();

            Vector3 targetPosition = targetPlayer.transform.position;
            Vector3 direction = (targetPosition - this.transform.position).normalized;
            direction.y = 0; // Keep the y component zero to prevent vertical rotation

            if (direction != Vector3.zero) {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                this.transform.rotation = Quaternion.Slerp(this.transform.rotation, lookRotation, Time.deltaTime * 5f);
            }
        }


        if (!IsHost) return;
        CheckWallCollision();
    }

    private void CheckWallCollision()
    {
        if (targetPlayer == null) return;
        if (currentBehaviourStateIndex == (int)State.ChasingPlayer || currentBehaviourStateIndex == (int)State.ChasingEnemy)
        {
            float velocity = agent.velocity.magnitude;

            if (velocity >= collisionThresholdVelocity)
            {
                LogIfDebugBuild("Velocity too high: " + velocity.ToString());
                // Check for wall collision
                RaycastHit hit;
                int layerMask = StartOfRound.Instance.collidersAndRoomMask;
                if (Physics.Raycast(transform.position, transform.forward, out hit, 1f, layerMask))
                {
                    LogIfDebugBuild("Wall collision detected");
                    StunGoose(targetPlayer, false);
                    if (recentlyDamagedCoroutine != null)
                    {
                        StopCoroutine(recentlyDamagedCoroutine);
                    }
                    recentlyDamagedCoroutine = StartCoroutine(RecentlyDamagedCooldown(targetPlayer));
                }
            }
        }
    }

    public void PlayStartStunSound() { // Animation Event
        creatureSFX.PlayOneShot(StartStunSound);
    }

    public void PlayEndStunSound() { // Animation Event
        creatureSFX.PlayOneShot(EndStunSound);
    }

    private void StunGoose(PlayerControllerB playerWhoStunned = null, bool delaySpeed = true)
    {
        TriggerAnimationClientRpc("Stunned");
        ControlStateSpeedAnimationClientRpc(0f, (int)State.Stunned, false, false, false, Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerWhoStunned), delaySpeed, true);
        if (holdingEgg) {
            DropEggClientRpc();
        }
        if (carryingPlayerBody) {
            DropPlayerBodyClientRpc();
        }
        StartCoroutine(StunCooldown(playerWhoStunned, false));
    }
    public override void DoAIInterval()
    {
        base.DoAIInterval();
        if (isEnemyDead || StartOfRound.Instance.allPlayersDead || !IsHost) return;
        
        switch (currentBehaviourStateIndex.ToPjonkGooseState())
        {
            case State.Spawning:
                DoSpawning();
                break;
            case State.Wandering:
                DoWandering();
                break;
            case State.Guarding:
                DoGuarding();
                break;
            case State.ChasingPlayer:
                DoChasingPlayer();
                break;
            case State.ChasingEnemy:
                DoChasingEnemy();
                break;
            case State.Death:
                DoDeath();
                break;
            case State.Stunned:
                DoStunned();
                break;
            case State.Idle:
                DoIdle();
                break;
            case State.DragPlayerBodyToNest:
                DoDragPlayerBodyToNest();
                break;
            default:
                LogIfDebugBuild("This Behavior State doesn't exist!");
                break;
        }
    }

    public void DoSpawning() {
    }

    public void DoWandering()
    {
        if (!isAggro)
        {
            if (timeSinceAction >= UnityEngine.Random.Range(10f, 20f)) {
                timeSinceAction = 0;
                ControlStateSpeedAnimationClientRpc(0f, (int)State.Idle, false, false, false, -1, true, false);
                TriggerAnimationClientRpc("PerformIdleAction");
                StartCoroutine(SpawnTimer());
                return;
            }
            if (UnityEngine.Random.Range(1, 101) <= 7.5f) {
                HonkAnimationClientRpc();
            }
            if (HandleEnemyOrPlayerGrabbingEgg()) {
                return;
            }
            if (wanderCoroutine == null)
            {
                if ((isOutside && !isNestInside) || (!isOutside && isNestInside)) {
                    StartWandering(nest.transform.position);
                } else if ((isOutside && isNestInside) || (!isOutside && !isNestInside)) {
                    GoThroughEntrance();
                }
            }
        }
    }

    private bool HandleEnemyOrPlayerGrabbingEgg()
    {
        if (goldenEgg.isHeldByEnemy && !holdingEgg) {
            LogIfDebugBuild("An Enemy grabbed the egg");
            ControlStateSpeedAnimationClientRpc(SPRINTING_SPEED, (int)State.ChasingEnemy, false, true, true, -1, true, true);
            SetTargetClientRpc(-1);
            var yippeeHoldingEgg = FindYippeeHoldingEgg();
            if (yippeeHoldingEgg == null) 
            {      
                LogIfDebugBuild("enemy holding egg could not be found");
                return false;
            }
            SetEnemyTargetClientRpc(Array.IndexOf(RoundManager.Instance.SpawnedEnemies.ToArray(), yippeeHoldingEgg));
            return true;
        } else if (goldenEgg.isHeld) {
            LogIfDebugBuild("Someone grabbed the egg");
            SetTargetClientRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, goldenEgg.playerHeldBy));
            PlayMiscSoundsClientRpc(2);
            ControlStateSpeedAnimationClientRpc(SPRINTING_SPEED, (int)State.ChasingPlayer, false, true, true, -1, true, true);
            return true;
        }
        return false;
    }

    [ClientRpc]
    public void HonkAnimationClientRpc() { // Animation Event
        creatureVoice.PlayOneShot(HonkSounds[enemyRandom.Next(0, HonkSounds.Length)]);
        TriggerAnimationOnLocalClient("Honk");
    }
    public void DoGuarding()
    {
        HoversNearNest();
    }

    public void DoChasingPlayer()
    {
        if (UnityEngine.Random.Range(1, 101) <= 7f && agent.speed >= 1f) {
            HonkAnimationClientRpc();
        }
        // If the golden egg is held by the player, keep chasing the player until the egg is dropped
        if (goldenEgg == null) {
            LogIfDebugBuild("Golden egg is null");
            ControlStateSpeedAnimationClientRpc(WALKING_SPEED, (int)State.Wandering, true, false, false, -1, true, false);
            SetTargetClientRpc(-1);
            return;
        }
        foreach (DoorLock door in doors)
        {
            if (door == null) continue;
            if (door.isDoorOpened) continue;
            if (!door.GetComponent<Rigidbody>() && Vector3.Distance(door.transform.position, transform.position) < 3f && agent.velocity.magnitude > 5f)
            {
                ExplodeDoorClientRpc(Array.IndexOf(doors, door));
                StunGoose(targetPlayer, false);
                if (recentlyDamagedCoroutine != null)
                {
                    StopCoroutine(recentlyDamagedCoroutine);
                }
                recentlyDamagedCoroutine = StartCoroutine(RecentlyDamagedCooldown(targetPlayer));
            }
        }
        if (targetPlayer == null && recentlyDamaged) {
            SetTargetClientRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerWhoLastHit));
        }
        // Prioritize recently damaged logic
        if (recentlyDamaged && targetPlayer != null) {
            LogIfDebugBuild("Chasing player because recently damaged");
            if (holdingEgg) {
                DropEggClientRpc();
            }
            if (carryingPlayerBody) {
                DropPlayerBodyClientRpc();
            }
            SetDestinationToPosition(targetPlayer.transform.position, false);
            return;
        }

        if (targetPlayer != null && goldenEgg.playerHeldBy == targetPlayer) {
            LogIfDebugBuild("Chasing player holding the egg");
            if (this.isOutside && goldenEgg.playerHeldBy.isInsideFactory) {
                GoThroughEntrance();
            } else if (!this.isOutside && !goldenEgg.playerHeldBy.isInsideFactory) {
                GoThroughEntrance();
            } else if ((this.isOutside && !goldenEgg.playerHeldBy.isInsideFactory) || (!this.isOutside && goldenEgg.playerHeldBy.isInsideFactory)) {
                SetDestinationToPosition(targetPlayer.transform.position, false);
            }
            return;
        } else if (goldenEgg.playerHeldBy != null && targetPlayer != goldenEgg.playerHeldBy) {
            LogIfDebugBuild("Changing target to player holding the egg");
            SetTargetClientRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, goldenEgg.playerHeldBy));
            return;
        } else if (goldenEgg.playerHeldBy == null) {
            LogIfDebugBuild("Egg is not held by any player");
            // If not recently hit and egg is not held, go to the egg
            if (Vector3.Distance(this.transform.position, goldenEgg.transform.position) < 1f) {
                GrabEggClientRpc();
                return;
            }
            SetDestinationToPosition(goldenEgg.transform.position, false);
            return;
        }

        if (!recentlyDamaged && goldenEgg.playerHeldBy != null && targetPlayer != goldenEgg.playerHeldBy) {
            LogIfDebugBuild("Switching to chase player holding the egg");
            SetTargetClientRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, goldenEgg.playerHeldBy));
        }
    } // todo: everywhere i use setdestinationtoposition, i need to make a navmeshpath check to even see if it's possible to go there.

    public void DoChasingEnemy()
    {
        if (goldenEgg == null) {
            LogIfDebugBuild("Golden egg is null");
            ControlStateSpeedAnimationClientRpc(WALKING_SPEED, (int)State.Wandering, true, false, false, -1, true, false);
            SetEnemyTargetClientRpc(-1);
            return;
        }

        if (targetEnemy != null && goldenEgg.isHeldByEnemy)
        {
            if (this.isOutside && !targetEnemy.isOutside) {
                // Go inside, path to enemy
                GoThroughEntrance();
            } else if (!this.isOutside && targetEnemy.isOutside) {
                // Go outside, path to enemy
                GoThroughEntrance();
            } else if ((this.isOutside && targetEnemy.isOutside) || (!this.isOutside && !targetEnemy.isOutside)) {
                SetDestinationToPosition(targetEnemy.transform.position, false);
            }
            return;
        }
        
        if (targetEnemy == null || !goldenEgg.isHeldByEnemy)
        {
            LogIfDebugBuild("Target enemy is null or not holding the egg");
            if (Vector3.Distance(goldenEgg.transform.position, nest.transform.position) >= 0.75f) {
                if (Vector3.Distance(this.transform.position, goldenEgg.transform.position) < 1f) {
                    GrabEggClientRpc();
                    return;
                }
                SetDestinationToPosition(goldenEgg.transform.position, false);
            } else {
                ControlStateSpeedAnimationClientRpc(WALKING_SPEED, (int)State.Wandering, true, false, false, -1, true, false);
            }
            return;
        }
        
        SetDestinationToPosition(targetEnemy.transform.position, false);
    }

    public void DoDeath() {
    }

    public void DoStunned() {
    }

    public void DoIdle() {
    }

    public void DoDragPlayerBodyToNest()
    {
        DragBodiesToNest();
    }

    public void StartWandering(Vector3 nestPosition, SimpleWanderRoutine newWander = null)
    {
        this.StopWandering(this.currentWander, true);
        if (newWander == null)
        {
            this.currentWander = new SimpleWanderRoutine();
            newWander = this.currentWander;
        }
        else
        {
            this.currentWander = newWander;
        }
        this.currentWander.NestPosition = nestPosition;
        this.currentWander.unvisitedNodes = this.allAINodes.ToList();
        this.wanderCoroutine = StartCoroutine(this.WanderCoroutine());
        this.currentWander.inProgress = true;
    }

    public void StopWandering(SimpleWanderRoutine wander, bool clear = true)
    {
        if (wander != null)
        {
            if (this.wanderCoroutine != null)
            {
                StopCoroutine(this.wanderCoroutine);
            }
            wander.inProgress = false;
            if (clear)
            {
                wander.unvisitedNodes = this.allAINodes.ToList();
                wander.currentTargetNode = null;
                wander.nextTargetNode = null;
            }
        }
    }

    private IEnumerator WanderCoroutine()
    {
        yield return null;
        while (this.wanderCoroutine != null && IsOwner)
        {
            yield return null;
            if (this.currentWander.unvisitedNodes.Count <= 0)
            {
                this.currentWander.unvisitedNodes = this.allAINodes.ToList();
                yield return new WaitForSeconds(1f);
            }

            if (this.currentWander.unvisitedNodes.Count > 0)
            {
                // Choose a random node within the radius
                if (!isOutside) {
                this.currentWander.currentTargetNode = this.currentWander.unvisitedNodes
                                    .Where(node => Vector3.Distance(this.currentWander.NestPosition, node.transform.position) <= this.currentWander.wanderRadius)
                                    .OrderBy(node => UnityEngine.Random.value)
                                    .FirstOrDefault();
                } else {
                    this.currentWander.currentTargetNode = this.currentWander.unvisitedNodes
                                    .Where(node => Vector3.Distance(this.transform.position, node.transform.position) <= this.currentWander.wanderRadius)
                                    .OrderBy(node => UnityEngine.Random.value)
                                    .FirstOrDefault();
                }
                
                if (this.currentWander.currentTargetNode != null)
                {
                    this.SetWanderDestinationToPosition(this.currentWander.currentTargetNode.transform.position, false);
                    this.currentWander.unvisitedNodes.Remove(this.currentWander.currentTargetNode);

                    // Wait until reaching the target
                    yield return new WaitUntil(() => Vector3.Distance(transform.position, this.currentWander.currentTargetNode.transform.position) < this.currentWander.searchPrecision + UnityEngine.Random.Range(-2f, 2f));
                }
            }
        }
        yield break;
    }

    private void SetWanderDestinationToPosition(Vector3 position, bool stopCurrentPath)
    {
        if (position == Vector3.zero) {
            LogIfDebugBuild("Attempted to set destination to Vector3.zero");
            return; // Optionally set a default or backup destination
        }
        this.SetDestinationToPosition(position);
    }

    [ClientRpc]
    public void DropEggClientRpc() {
        StartCoroutine(DelayDroppingEgg());
    }

    public IEnumerator DelayDroppingEgg() {
        yield return new WaitForSeconds(0.25f);
        DropEgg();
    }

    public void DropEgg() {
        goldenEgg.parentObject = null;
        if (IsServer) goldenEgg.transform.SetParent(StartOfRound.Instance.propsContainer, true);
        goldenEgg.EnablePhysics(true);
        goldenEgg.fallTime = 0f;
        goldenEgg.startFallingPosition = goldenEgg.transform.parent.InverseTransformPoint(goldenEgg.transform.position);
        goldenEgg.targetFloorPosition = goldenEgg.transform.parent.InverseTransformPoint(goldenEgg.GetItemFloorPosition(default(Vector3)));
        goldenEgg.floorYRot = -1;
        goldenEgg.DiscardItemFromEnemy();
        if (IsServer) goldenEgg.grabbable = true;
        if (IsServer) goldenEgg.grabbableToEnemies = true;
        goldenEgg.isHeldByEnemy = false;
        holdingEgg = false;
        goldenEgg.transform.rotation = Quaternion.Euler(goldenEgg.itemProperties.restingRotation);
    }

    public override void KillEnemy(bool destroy = false) {
        base.KillEnemy(destroy);
        if (IsHost) {
            TriggerAnimationClientRpc("Death");
            ControlStateSpeedAnimationClientRpc(0, (int)State.Death, false, false, false, -1, true, false);
            if (holdingEgg) {
                DropEggClientRpc();
            }
            if (carryingPlayerBody) {
                DropPlayerBodyClientRpc();
            }
        }
        var FeatherPS = this.transform.Find("GooseRig").Find("Root").Find("Torso").Find("Torso.001").Find("Feather PS").GetComponent<ParticleSystem>().main;
        FeatherPS.loop = false;
        creatureVoice.PlayOneShot(deathSounds[enemyRandom.Next(0, deathSounds.Length)]);
    }
    public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
        if (isEnemyDead || currentBehaviourStateIndex == (int)State.Death) return;
        // creatureVoice.PlayOneShot(hitSounds[enemyRandom.Next(0, hitSounds.Length)]);
        enemyHP -= force;

        LogIfDebugBuild($"Player who hit: {playerWhoHit}");

        if (playerWhoHit != null && currentBehaviourStateIndex != (int)State.Stunned) {
            PlayerHitEnemy(playerWhoHit);
        }
        
        if (recentlyDamagedCoroutine != null)
        {
            StopCoroutine(recentlyDamagedCoroutine);
        }
        recentlyDamagedCoroutine = StartCoroutine(RecentlyDamagedCooldown(playerWhoHit));
        LogIfDebugBuild($"Enemy HP: {enemyHP}");
        LogIfDebugBuild($"Hit with force {force}");

        if (IsOwner && enemyHP <= 0 && !isEnemyDead) {
            KillEnemyOnOwnerClient();
        }
    }

    public void GoThroughEntrance() {
        var pathToTeleport = new NavMeshPath();
        var insideEntrancePosition = RoundManager.FindMainEntrancePosition(true, false);
        var outsideEntrancePosition = RoundManager.FindMainEntrancePosition(true, true);
        if (isOutside) {
            NavMesh.CalculatePath(this.transform.position, outsideEntrancePosition, this.agent.areaMask, pathToTeleport);
            if (pathToTeleport.status != NavMeshPathStatus.PathComplete) {
                LogIfDebugBuild("Failed to find path to outside entrance");
                ControlStateSpeedAnimationClientRpc(WALKING_SPEED, (int)State.Wandering, true, false, false, -1, false, false);
                return;
            }
            SetDestinationToPosition(outsideEntrancePosition);
            
            if (Vector3.Distance(transform.position, outsideEntrancePosition) < 1f) {
                this.agent.Warp(insideEntrancePosition);
                SetEnemyOutsideClientRpc(false);
            }
        } else {
            if (NavMesh.CalculatePath(this.transform.position, insideEntrancePosition, this.agent.areaMask, pathToTeleport)) {
                SetDestinationToPosition(insideEntrancePosition);
            }
            if (Vector3.Distance(transform.position, insideEntrancePosition) < 1f) {
                this.agent.Warp(outsideEntrancePosition);
                SetEnemyOutsideClientRpc(true);
            }
        }
    }

    [ClientRpc]
    public void SetEnemyOutsideClientRpc(bool setOutisde) {
        this.SetEnemyOutside(setOutisde);
    }
    public IEnumerator RecentlyDamagedCooldown(PlayerControllerB playerWhoHit) {
        recentlyDamaged = true;
        playerWhoLastHit = playerWhoHit;
        if (IsHost) SetTargetClientRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerWhoHit));
        yield return new WaitForSeconds(enemyRandom.Next(10, 15));
        if (!IsHost && !(currentBehaviourStateIndex != (int)State.Death || currentBehaviourStateIndex != (int)State.Stunned)) PlayMiscSoundsClientRpc(1);
        recentlyDamaged = false;
    }
    
    public void PlayerHitEnemy(PlayerControllerB playerWhoStunned = null)
    {
        LogIfDebugBuild($"PlayerHitEnemy called. Current hits: {playerHits}, Current State: {currentBehaviourStateIndex}");
        playerHits += 1;
        if (playerHits >= 7 && playerWhoStunned != null && currentBehaviourStateIndex != (int)State.Stunned)
        {
            playerHits = 0;
            if (!IsHost) return; 
            LogIfDebugBuild("Stunning Goose");
            StunGoose(playerWhoStunned, true);
        }
        else if (currentBehaviourStateIndex != (int)State.ChasingPlayer)
        {
            LogIfDebugBuild("Aggro on hit");
            AggroOnHit(playerWhoStunned);
        }
    }

    public void AggroOnHit(PlayerControllerB playerWhoStunned)
    {
        if (!IsHost) return;
        LogIfDebugBuild($"AggroOnHit called. Targeting player: {playerWhoStunned}");
        SetTargetClientRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerWhoStunned));
        PlayMiscSoundsClientRpc(2);
        ControlStateSpeedAnimationClientRpc(SPRINTING_SPEED, (int)State.ChasingPlayer, false, true, false, -1, true, true);
    }

    public void PlayFeatherSound() // Animation Event
    {
        // creatureVoice.PlayOneShot(featherSounds[enemyRandom.Next(0, featherSounds.Length)]);
    }

    [ClientRpc]
    public void PlayMiscSoundsClientRpc(int soundID) {
        switch (soundID) {
            case 0:
                creatureVoice.PlayOneShot(GuardHyperVentilateClip);
                break;
            case 1:
                creatureVoice.PlayOneShot(HissSound);
                break;
            case 2:
                creatureVoice.PlayOneShot(EnrageSound);
                break;
            default:
                LogIfDebugBuild($"Invalid sound ID: {soundID}");
                break;
        }
    }
    public void DragBodiesToNest()
    {
        if ((isOutside && !isNestInside) || (!isOutside && isNestInside)) {
            SetDestinationToPosition(nest.transform.position, false);
        } else if ((isOutside && isNestInside) || (!isOutside && !isNestInside)) {
            GoThroughEntrance();
        }
        if (HandleEnemyOrPlayerGrabbingEgg() && carryingPlayerBody) {
            DropPlayerBodyClientRpc();
        }
        if (Vector3.Distance(this.transform.position, nest.transform.position) > 1f) {
            DropPlayerBodyClientRpc();
        }
    }

    [ClientRpc]
    public void SpawnEggInNestClientRpc()
    {
        if (IsHost) CodeRebirthUtils.Instance.SpawnScrapServerRpc("GoldenEgg", nest.transform.position, false, true); // todo: for some reason value doesn't sync on clients
        StartCoroutine(DelayFindingGoldenEgg());
        return;
    }

    public IEnumerator DelayFindingGoldenEgg()
    {
        yield return new WaitForSeconds(0.5f);
        goldenEgg = FindObjectsOfType<GoldenEgg>().Where(egg => Vector3.Distance(egg.transform.position, this.transform.position) < 2f).FirstOrDefault();
        if (IsServer) goldenEgg.transform.SetParent(StartOfRound.Instance.propsContainer, true);
        LogIfDebugBuild($"Found egg in nest: {goldenEgg.itemProperties.itemName}");
    }

    public void HoversNearNest()
    {
        // Logic for hovering near nest
        if (Vector3.Distance(this.transform.position, nest.transform.position) < 0.75f)
        {
            DropEggClientRpc();
            ControlStateSpeedAnimationClientRpc(0f, (int)State.Idle, false, false, true, -1, true, false);
            StartCoroutine(SpawnTimer());
        } else {
            if ((isOutside && !isNestInside) || (!isOutside && isNestInside)) {
                SetDestinationToPosition(nest.transform.position, false);
            } else if ((isOutside && isNestInside) || (!isOutside && !isNestInside)) {
                GoThroughEntrance();
            }
        }
    }

    public void KillPlayerWithEgg()
    {
        if (targetPlayer == null) return;
        if (!IsHost) return;
        LogIfDebugBuild("Player killed"); // todo: fix animations when walking back to nest
        targetPlayer.KillPlayer(targetPlayer.velocityLastFrame * 5f, true, CauseOfDeath.Bludgeoning);
        if (Vector3.Distance(goldenEgg.transform.position, nest.transform.position) <= 0.75f) {
            CarryingDeadPlayerClientRpc();
            ControlStateSpeedAnimationClientRpc(SPRINTING_SPEED, (int)State.DragPlayerBodyToNest, false, false, true, -1, true, false);
        } else {
            SetTargetClientRpc(-1);
            SetDestinationToPosition(goldenEgg.transform.position, false);
        }
    }

    [ClientRpc]
    private void GrabEggClientRpc()
    {
        holdingEgg = true;
        goldenEgg.isHeldByEnemy = true;
        if (IsServer) goldenEgg.grabbable = false;
        if (IsServer) goldenEgg.grabbableToEnemies = false;
        goldenEgg.parentObject = this.transform;
        goldenEgg.transform.position = this.transform.position + transform.up * 4f;
        if (!IsHost) return;
        PlayMiscSoundsClientRpc(0);
        SetTargetClientRpc(-1);
        ControlStateSpeedAnimationClientRpc(WALKING_SPEED, (int)State.Guarding, false, false, true, -1, true, false);
    }

    [ClientRpc]
    public void CarryingDeadPlayerClientRpc()
    {
        carryingPlayerBody = true;
        bodyBeingCarried = targetPlayer.deadBody;
        if (IsHost) {
            SetTargetClientRpc(-1);
        }
    }

    [ClientRpc]
    public void DropPlayerBodyClientRpc() {
        DropPlayerBody();
    }
    private IEnumerator StunCooldown(PlayerControllerB playerWhoStunned, bool delaySpeed = true)
    {
        yield return new WaitUntil(() => this.stunNormalizedTimer <= 0);
        if (isEnemyDead) yield break;

        SetTargetClientRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerWhoStunned));
        ControlStateSpeedAnimationClientRpc(SPRINTING_SPEED, (int)State.ChasingPlayer, false, true, false, Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerWhoStunned), delaySpeed, true);
        this.HitEnemyClientRpc(0, Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerWhoStunned), false, -1);
    }

    public override void OnCollideWithPlayer(Collider other)
    {
        if (isEnemyDead) return;
        if (other.GetComponent<PlayerControllerB>().isPlayerDead) return;
        if (targetPlayer != null && currentBehaviourStateIndex == (int)State.ChasingPlayer)
        {
            KillPlayerWithEgg();
        } else if ((int)State.ChasingPlayer == currentBehaviourStateIndex && timeSinceHittingLocalPlayer >= 1f) {
            timeSinceHittingLocalPlayer = 0;
            other.GetComponent<PlayerControllerB>().DamagePlayer(20);
        }
    }
    
    private void DropPlayerBody()
	{
		if (!this.carryingPlayerBody)
		{
			return;
		}
		this.carryingPlayerBody = false;
		this.bodyBeingCarried.matchPositionExactly = false;
		this.bodyBeingCarried.attachedTo = null;
		this.bodyBeingCarried = null;
        if (currentBehaviourStateIndex == (int)State.DragPlayerBodyToNest && IsHost) {
            ControlStateSpeedAnimationClientRpc(0f, (int)State.Wandering, true, false, false, -1, false, false);
        }
	}

    public override void OnCollideWithEnemy(Collider other, EnemyAI collidedEnemy) {
        if (isEnemyDead) return;
        if (targetEnemy != null && collidedEnemy == targetEnemy && currentBehaviourStateIndex == (int)State.ChasingEnemy) {
            targetEnemy.HitEnemy(2, null, true, -1);
            if (targetEnemy.isEnemyDead) {
                SetEnemyTargetClientRpc(-1);
            }
        }
    }

    [ClientRpc]
    public void ExplodeDoorClientRpc(int DoorIndex) {
        LogIfDebugBuild("Exploding door: " + DoorIndex);
        DoorLock door = doors[DoorIndex];
        Utilities.CreateExplosion(door.transform.position, true, 25, 0, 4, 0, CauseOfDeath.Blast, null);
        Destroy(door.transform.parent.gameObject);
        // remove the door from the array
        doors = doors.Where((d, i) => i != DoorIndex).ToArray();
    }
}