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
    private Coroutine wanderCoroutine;
    private const float WALKING_SPEED = 6f;
    private const float SPRINTING_SPEED = 18f;
    private bool isAggro = false;
    private int playerHits = 0;
    private bool carryingPlayerBody;
    public DeadBodyInfo bodyBeingCarried;
    public AudioClip[] FootstepSounds;
    public AudioClip[] ViolentFootstepSounds;
    public AudioClip[] jumpScareSounds;
    public AudioClip[] featherSounds;
    public AudioClip[] hitSounds;
    public AudioClip[] deathSounds;

    private GrabbableObject goldenEgg;
    private float timeSinceHittingLocalPlayer;
    private bool holdingEgg;
    public GameObject nest;
    private bool recentlyDamaged = false;
    private bool nestCreated = false;
    private bool isNestInside = true;
    private Coroutine recentlyDamagedCoroutine;
    private float collisionThresholdVelocity = SPRINTING_SPEED - 2;

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
        LogIfDebugBuild(RoundManager.Instance.currentLevel.maxOutsideEnemyPowerCount.ToString());
        LogIfDebugBuild(RoundManager.Instance.currentOutsideEnemyPower.ToString());
        timeSinceHittingLocalPlayer = 0;
        if (!IsHost) return;
        isAggro = false;
        ControlStateSpeedAnimation(0f, State.Spawning, false, false, false);
        StartCoroutine(SpawnTimer());
    }

    [ClientRpc]
    public void SpawnNestClientRpc() {
        nest = Instantiate(nest, this.transform.position + Vector3.down * 0.02f, Quaternion.identity, RoundManager.Instance.mapPropsContainer.transform);
        if (!isOutside) isNestInside = true;
    }

    public IEnumerator DelayedSpeed(float speed) {
        this.ChangeSpeedClientRpc(0f);
        yield return new WaitForSeconds(2.317f);
        LogIfDebugBuild("Delayed Speed");
        this.ChangeSpeedClientRpc(speed);
    }
    public void ControlStateSpeedAnimation(float speed, State state, bool startSearch, bool running, bool guarding, PlayerControllerB playerWhoStunned = null, bool delaySpeed = true) {
        if ((state == State.ChasingPlayer || state == State.ChasingEnemy) && delaySpeed) StartCoroutine(DelayedSpeed(speed));
        else this.ChangeSpeedClientRpc(speed);
        if (state == State.Stunned) SetEnemyStunned(true, 5.317f, playerWhoStunned);
        this.SetFloatAnimationClientRpc("MoveZ", speed);
        this.SetBoolAnimationClientRpc("Running", running);
        this.SetBoolAnimationClientRpc("Guarding", guarding);
        this.SetBoolAnimationClientRpc("Aggro", isAggro);
        SwitchToBehaviourClientRpc((int)state);
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
        nestCreated = true;
        if (goldenEgg == null) SpawnEggInNest();
        yield return new WaitForSeconds(2f);
        if (currentBehaviourStateIndex == (int)State.Stunned || currentBehaviourStateIndex == (int)State.ChasingPlayer || currentBehaviourStateIndex == (int)State.ChasingEnemy || currentBehaviourStateIndex == (int)State.Death) yield break;
        isAggro = false;
        ControlStateSpeedAnimation(WALKING_SPEED, State.Wandering, true, false, false);
    }

    public EnemyAI FindYippeeHoldingEgg() {
        foreach (var enemy in RoundManager.Instance.SpawnedEnemies) {
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
        timeSinceHittingLocalPlayer += Time.deltaTime;

        if (targetPlayer != null && currentBehaviourStateIndex == (int)State.Guarding) {
               this.transform.LookAt(targetPlayer.transform.position);
        } else if (currentBehaviourStateIndex == (int)State.Guarding) {
            this.transform.LookAt(StartOfRound.Instance.allPlayerScripts.OrderBy(player => Vector3.Distance(player.transform.position, this.transform.position)).First().transform.position);
        }

        if (!IsHost) return;
        CheckWallCollision();
    }

    private void CheckWallCollision()
    {
        if (currentBehaviourStateIndex == (int)State.ChasingPlayer || currentBehaviourStateIndex == (int)State.ChasingEnemy)
        {
            float velocity = agent.velocity.magnitude;

            if (velocity >= collisionThresholdVelocity)
            {
                LogIfDebugBuild("Velocity too high: " + velocity.ToString());
                // Check for wall collision
                RaycastHit hit;
                int layerMask = StartOfRound.Instance.collidersAndRoomMaskAndDefault;
                if (Physics.Raycast(transform.position, transform.forward, out hit, 1f, layerMask))
                {
                    LogIfDebugBuild("Wall collision detected");
                    StunGoose();
                }
            }
        }
    }

    private void StunGoose()
    {
        TriggerAnimationClientRpc("Stunned");
        ControlStateSpeedAnimation(0f, State.Stunned, false, false, false, null, false);
        if (holdingEgg) {
            DropEggClientRpc();
        }
        if (carryingPlayerBody) {
            DropPlayerBodyClientRpc();
        }
        StartCoroutine(StunCooldown(null, false));
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
            if (HandleEnemyOrPlayerGrabbingEgg()) {
                return;
            }
            if (wanderCoroutine == null)
            {
                if (isOutside && !isNestInside) {
                    StartWandering(nest.transform.position);
                } else if (!isOutside && isNestInside) {
                    StartWandering(nest.transform.position);    
                } else if (isOutside && isNestInside) {
                    GoThroughEntrance();
                } else if (!isOutside && !isNestInside) {
                    GoThroughEntrance();
                }
            }
        }
    }

    private bool HandleEnemyOrPlayerGrabbingEgg()
    {
        if (goldenEgg.isHeldByEnemy && !holdingEgg) {
            LogIfDebugBuild("An Enemy grabbed the egg");
            isAggro = true;
            ControlStateSpeedAnimation(SPRINTING_SPEED, State.ChasingEnemy, false, true, true);
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
            // maybe honk or some stuff.
            isAggro = true;
            ControlStateSpeedAnimation(SPRINTING_SPEED, State.ChasingPlayer, false, true, true);
            SetTargetClientRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, goldenEgg.playerHeldBy));
            return true;
        }
        return false;
    }

    public void DoGuarding()
    {
        HoversNearNest();
    }

    public void DoChasingPlayer()
    {
        // If the golden egg is held by the player, keep chasing the player until the egg is dropped
        if (goldenEgg == null) {
            LogIfDebugBuild("Golden egg is null");
            isAggro = false;
            ControlStateSpeedAnimation(WALKING_SPEED, State.Wandering, true, false, false);
            SetTargetClientRpc(-1);
            return;
        } // <-- shouldn't happen.

        if (targetPlayer != null && goldenEgg.playerHeldBy == targetPlayer)
        {
            if (this.isOutside && goldenEgg.playerHeldBy.isInsideFactory) {
                // go inside, path to player
                GoThroughEntrance();
            } else if (!this.isOutside && !goldenEgg.playerHeldBy.isInsideFactory) {
                // go outside, path to player
                GoThroughEntrance();
            } else if ((this.isOutside && !goldenEgg.playerHeldBy.isInsideFactory) || (!this.isOutside && goldenEgg.playerHeldBy.isInsideFactory)) {
                SetDestinationToPosition(targetPlayer.transform.position, false);
            }
            return;
        } else if (goldenEgg.playerHeldBy != null && targetPlayer != goldenEgg.playerHeldBy) {
            SetTargetClientRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, goldenEgg.playerHeldBy));
            return;
        } else if (recentlyDamaged && targetPlayer != null) {
            // If recently hit, chase the player
            if (holdingEgg) {
                DropEggClientRpc();
            }
            if (carryingPlayerBody) {
                DropPlayerBodyClientRpc();
            }
            SetDestinationToPosition(targetPlayer.transform.position, false);
            return;
        } else if (goldenEgg.playerHeldBy == null) {
            // If not recently hit and egg is not held, go to the egg
            if (targetPlayer != null) {
                SetTargetClientRpc(-1);
            }
            if (Vector3.Distance(this.transform.position, goldenEgg.transform.position) < 1f)
            {
                GrabEggClientRpc();
                return;
            }
            SetDestinationToPosition(goldenEgg.transform.position, false);
            return;
        }
        if (!recentlyDamaged && goldenEgg.playerHeldBy != null && targetPlayer != goldenEgg.playerHeldBy) {
            SetTargetClientRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, goldenEgg.playerHeldBy));
        }
    }// todo: everywhere i use setdestinationtoposition, i need to make a navmeshpath check to even see if it's possible to go there.

    public void DoChasingEnemy()
    {
        if (goldenEgg == null) {
            LogIfDebugBuild("Golden egg is null");
            isAggro = false;
            ControlStateSpeedAnimation(WALKING_SPEED, State.Wandering, true, false, false);
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
                ControlStateSpeedAnimation(WALKING_SPEED, State.Wandering, true, false, false);
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

    public void AggroOnHit(PlayerControllerB playerWhoStunned)
    {
        isAggro = true;
        SetTargetClientRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerWhoStunned));
        ControlStateSpeedAnimation(SPRINTING_SPEED, State.ChasingPlayer, false, true, false);
        PlayJumpScareSoundClientRpc();
    }

    [ClientRpc]
    public void DropEggClientRpc() {
        DropEgg();
    }

    public void DropEgg() {
        goldenEgg.parentObject = null;
        goldenEgg.transform.SetParent(StartOfRound.Instance.propsContainer, true);
        goldenEgg.EnablePhysics(true);
        goldenEgg.fallTime = 0f;
        goldenEgg.startFallingPosition = goldenEgg.transform.parent.InverseTransformPoint(goldenEgg.transform.position);
        goldenEgg.targetFloorPosition = goldenEgg.transform.parent.InverseTransformPoint(goldenEgg.GetItemFloorPosition(default(Vector3)));
        goldenEgg.floorYRot = -1;
        goldenEgg.DiscardItemFromEnemy();
        goldenEgg.grabbable = true;
        goldenEgg.grabbableToEnemies = true;
        goldenEgg.isHeldByEnemy = false;
        holdingEgg = false;
        goldenEgg.transform.rotation = Quaternion.Euler(goldenEgg.itemProperties.restingRotation);
    }

    public override void KillEnemy(bool destroy = false) {
        base.KillEnemy(destroy);
        if (IsHost) {
            TriggerAnimationClientRpc("Death");
            ControlStateSpeedAnimation(0, State.Death, false, false, false);
            if (holdingEgg) {
                DropEggClientRpc();
            }
            if (carryingPlayerBody) {
                DropPlayerBodyClientRpc();
            }
        }
        // creatureVoice.PlayOneShot(dieSFX);
    }
    public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
        if (isEnemyDead || currentBehaviourStateIndex == (int)State.Death) return;
        
        //  creatureVoice.PlayOneShot(hitSounds[UnityEngine.Random.Range(0, hitSounds.Length)]);
        enemyHP -= force;
        if (recentlyDamagedCoroutine != null)
        {
            StopCoroutine(recentlyDamagedCoroutine);
        }
        recentlyDamagedCoroutine = StartCoroutine(RecentlyDamagedCooldown());
        LogIfDebugBuild($"Enemy HP: {enemyHP}");
        LogIfDebugBuild($"Hit with force {force}");
        
        if (playerWhoHit != null && currentBehaviourStateIndex != (int)State.Stunned) {
            PlayerHitEnemy(force, playerWhoHit);
        }

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
                StartWandering(this.transform.position, new SimpleWanderRoutine());
                return;
            }
            SetDestinationToPosition(outsideEntrancePosition);
            
            if (Vector3.Distance(transform.position, outsideEntrancePosition) < 1f) {
                this.agent.Warp(insideEntrancePosition);
                this.SetEnemyOutside(false);
            }
        } else {
            if (NavMesh.CalculatePath(this.transform.position, insideEntrancePosition, this.agent.areaMask, pathToTeleport)) {
                SetDestinationToPosition(insideEntrancePosition);
            }
            if (Vector3.Distance(transform.position, insideEntrancePosition) < 1f) {
                this.agent.Warp(outsideEntrancePosition);
                this.SetEnemyOutside(true);
            }
        }
    }

    public IEnumerator RecentlyDamagedCooldown() {
        recentlyDamaged = true;
        yield return new WaitForSeconds(currentBehaviourStateIndex == (int)State.Stunned ? 8f : 5f);
        recentlyDamaged = false;
    }
    
    public void PlayerHitEnemy(int force, PlayerControllerB playerWhoStunned = null)
    {
        playerHits += force;
        if (playerHits >= 3 && playerWhoStunned != null && currentBehaviourStateIndex != (int)State.Stunned)
        {
            playerHits = 0;
            if (!IsHost) return; 
            TriggerAnimationClientRpc("Stunned");
            ControlStateSpeedAnimation(0f, State.Stunned, false, false, false, playerWhoStunned);
            if (holdingEgg) {
                DropEggClientRpc();
            }
            if (carryingPlayerBody) {
                DropPlayerBodyClientRpc();
            }
            StartCoroutine(StunCooldown(playerWhoStunned));
        }
        else if (currentBehaviourStateIndex != (int)State.ChasingPlayer)
        {
            AggroOnHit(playerWhoStunned);
        }
    }

    [ClientRpc]
    public void PlayJumpScareSoundClientRpc()
    {
        // creatureVoice.PlayOneShot(jumpScareSounds[UnityEngine.Random.Range(0, jumpScareSounds.Length)]);
    }

    [ClientRpc]
    public void PlayFeatherSoundClientRpc()
    {
        // creatureVoice.PlayOneShot(featherSounds[UnityEngine.Random.Range(0, featherSounds.Length)]);
    }

    public void DragBodiesToNest()
    {
        // Implement logic to drag bodies to nest
        SetDestinationToPosition(nest.transform.position, false);
        if (HandleEnemyOrPlayerGrabbingEgg() && carryingPlayerBody) {
            DropPlayerBodyClientRpc();
        }
        if (Vector3.Distance(this.transform.position, nest.transform.position) > 1f) {
            DropPlayerBodyClientRpc();
        }
    }

    public void SpawnEggInNest()
    {
        CodeRebirthUtils.Instance.SpawnScrapServerRpc("GoldenEgg", nest.transform.position, false, true);
        goldenEgg = FindObjectsOfType<GoldenEgg>().Where(egg => Vector3.Distance(egg.transform.position, this.transform.position) < 2f).FirstOrDefault();
        goldenEgg.transform.SetParent(StartOfRound.Instance.propsContainer, true);
        LogIfDebugBuild($"Found egg in nest: {goldenEgg.itemProperties.itemName}");
        return;
    }

    public void HoversNearNest()
    {
        // Logic for hovering near nest
        if (Vector3.Distance(this.transform.position, nest.transform.position) < 0.75f)
        {
            DropEggClientRpc();
            ControlStateSpeedAnimation(0f, State.Idle, false, false, true);
            StartCoroutine(SpawnTimer());
        } else {
            SetDestinationToPosition(nest.transform.position, false);
        }
    }

    public void KillPlayerWithEgg()
    {
        if (targetPlayer == null) return;
        LogIfDebugBuild("Player killed");
        targetPlayer.KillPlayer(targetPlayer.velocityLastFrame * 5f, true, CauseOfDeath.Bludgeoning);
        SetTargetClientRpc(-1);
        if (Vector3.Distance(goldenEgg.transform.position, nest.transform.position) <= 0.75f) {
            CarryingDeadPlayerClientRpc();
            ControlStateSpeedAnimation(SPRINTING_SPEED, State.DragPlayerBodyToNest, false, false, true);
        } else {
            SetDestinationToPosition(goldenEgg.transform.position, false);
        }
    }

    [ClientRpc]
    private void GrabEggClientRpc()
    {
        holdingEgg = true;
        goldenEgg.isHeldByEnemy = true;
        goldenEgg.grabbable = false;
        goldenEgg.parentObject = this.transform;
        goldenEgg.transform.position = this.transform.position + transform.up * 0.5f;
        isAggro = false;
        ControlStateSpeedAnimation(WALKING_SPEED, State.Guarding, false, false, true);
    }

    [ClientRpc]
    public void CarryingDeadPlayerClientRpc()
    {
        carryingPlayerBody = true;
        bodyBeingCarried = targetPlayer.deadBody;
        targetPlayer = null;
        LogIfDebugBuild($"Clearing target on {this} body and carrying the deadbody back to nest");
    }

    [ClientRpc]
    public void DropPlayerBodyClientRpc() {
        DropPlayerBody();
    }
    private IEnumerator StunCooldown(PlayerControllerB playerWhoStunned, bool delaySpeed = true)
    {
        yield return new WaitUntil(() => this.stunNormalizedTimer <= 0);
        if (isEnemyDead) yield break;

        isAggro = true;
        SetTargetClientRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerWhoStunned));
        ControlStateSpeedAnimation(SPRINTING_SPEED, State.ChasingPlayer, false, true, false, playerWhoStunned, delaySpeed);
        this.HitEnemy(0, playerWhoStunned, false, -1);
    }

    public override void OnCollideWithPlayer(Collider other)
    {
        if (isEnemyDead) return;
        PlayerControllerB playerControllerB = MeetsStandardPlayerCollisionConditions(other);
        if (targetPlayer != null && playerControllerB == targetPlayer && currentBehaviourStateIndex == (int)State.ChasingPlayer)
        {
            KillPlayerWithEgg();
        } else if ((int)State.ChasingPlayer == currentBehaviourStateIndex && timeSinceHittingLocalPlayer >= 1f) {
            timeSinceHittingLocalPlayer = 0;
            playerControllerB.DamagePlayer(20);
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
}