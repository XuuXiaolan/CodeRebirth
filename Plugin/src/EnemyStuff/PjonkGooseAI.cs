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
using Unity.Netcode.Components;
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
    public Transform TopOfBody;
    public GameObject nest;
    public ParticleSystem featherHitParticles;
    public AnimationClip stunAnimation;

    private GrabbableObject goldenEgg;
    private float timeSinceHittingLocalPlayer;
    private float timeSinceAction;
    private bool holdingEgg = false;
    private bool recentlyDamaged = false;
    private bool nestCreated = false;
    private bool isNestInside = true;
    private Coroutine recentlyDamagedCoroutine;
    private float collisionThresholdVelocity = SPRINTING_SPEED - 1.5f;
    private System.Random enemyRandom;
    private DoorLock[] doors;

    public enum State
    {
        Spawning,
        Wandering,
        Guarding,
        ChasingPlayer,
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
        timeSinceHittingLocalPlayer = 0;
        timeSinceAction = 0;
        creatureVoice.PlayOneShot(SpawnSound);
        if (!IsHost) return;
        ControlStateSpeedAnimationServerRpc(0f, (int)State.Spawning, false, false, false, -1, true, false);
        StartCoroutine(SpawnTimer()); // 559 state is not transferring when client hits on clients end, constantly clearing target because the egg is not held but it should keep target because recently damaged
    }

    public void PlayFootstepSound() {
        creatureSFX.PlayOneShot(FootstepSounds[enemyRandom.Next(0, FootstepSounds.Length)]);
    } // Animation Event

    public void PlayViolentFootstepSound() {
        creatureSFX.PlayOneShot(ViolentFootstepSounds[enemyRandom.Next(0, ViolentFootstepSounds.Length)]);
    } // Animation Event

    [ClientRpc]
    public void SpawnNestClientRpc() {
        if (IsHost) nest = Instantiate(nest, this.transform.position + Vector3.down * 0.0133f, Quaternion.identity, RoundManager.Instance.mapPropsContainer.transform);
        if (IsHost) nest.GetComponent<NetworkObject>().Spawn(true);
        nestCreated = true;
        if (!isOutside) isNestInside = true;
        else isNestInside = false;
    }

    public void ApplyChasingSpeed() { // Animation Event
        if (isEnemyDead) return;
        this.ChangeSpeedOnLocalClient(SPRINTING_SPEED);
    }

    [ClientRpc]
    public void ControlStateSpeedAnimationClientRpc(float speed, int state, bool startSearch, bool running, bool guarding, int playerWhoStunnedIndex, bool delaySpeed, bool _isAggro)
    {
        isAggro = _isAggro;

        if ((state == (int)State.ChasingPlayer) && delaySpeed)
        {
            this.ChangeSpeedOnLocalClient(0);
            this.agent.velocity = Vector3.zero;
        }
        else {
            this.ChangeSpeedOnLocalClient(speed);
        }

        if (state == (int)State.Stunned) {
            this.SetEnemyStunned(true, stunAnimation.length, StartOfRound.Instance.allPlayerScripts[playerWhoStunnedIndex]);
            this.TriggerAnimationOnLocalClient("Stunned");
        }
        if (state == (int)State.Death) {
            this.TriggerAnimationOnLocalClient("Death");
        }
        this.SetBoolAnimationOnLocalClient("Running", running);
        this.SetBoolAnimationOnLocalClient("Guarding", guarding);
        this.SetBoolAnimationOnLocalClient("Aggro", _isAggro);
        this.SetFloatAnimationOnLocalClient("MoveZ", speed);
        this.SwitchToBehaviourStateOnLocalClient((int)state);
        if (!IsHost) return;
        if (startSearch)
        {
            StartWandering(nest.transform.position);
        }
        else
        {
            StopWandering(this.currentWander);
        }
    }

    public IEnumerator SpawnTimer()
    {
        yield return new WaitForSeconds(1f);
        if (!nestCreated) SpawnNestServerRpc();
        if (goldenEgg == null) SpawnEggInNestServerRpc();
        yield return new WaitForSeconds(2f);
        if (currentBehaviourStateIndex == (int)State.Stunned || currentBehaviourStateIndex == (int)State.ChasingPlayer || currentBehaviourStateIndex == (int)State.Death) yield break;
        ControlStateSpeedAnimationServerRpc(WALKING_SPEED, (int)State.Wandering, true, false, false, -1, true, false);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnEggInNestServerRpc()
    {
        SpawnEggInNestClientRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnNestServerRpc()
    {
        SpawnNestClientRpc();
    }

    public override void Update()
    {
        base.Update();
        if (stunNormalizedTimer > 0 && currentBehaviourStateIndex != (int)State.Stunned && IsHost) {
            if (targetPlayer == null) {
                if (playerWhoLastHit == null) {
                    var PlayerToTakeBlame = StartOfRound.Instance.allPlayerScripts.OrderBy(player => Vector3.Distance(player.transform.position, this.transform.position)).First();
                    StunGoose(PlayerToTakeBlame, false);
                } else {
                    StunGoose(playerWhoLastHit, false);
                }
            } else {
                StunGoose(targetPlayer, false);
            }
        }

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
        CheckForCollidingWithDoor();
    }

    private void CheckForCollidingWithDoor() {
        if (targetPlayer == null || currentBehaviourStateIndex != (int)State.ChasingPlayer) return;
        foreach (DoorLock door in doors)
        {
            if (door == null) continue;
            if (door.isDoorOpened) continue;
            if (agent.velocity.magnitude > collisionThresholdVelocity && Vector3.Distance(door.transform.position, transform.position) < 2f && !door.GetComponent<Rigidbody>())
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
    }
    private void CheckWallCollision()
    {
        if (targetPlayer == null) return;
        if (currentBehaviourStateIndex == (int)State.ChasingPlayer)
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

    private void StunGoose(PlayerControllerB playerWhoStunned, bool delaySpeed = true)
    {
        ControlStateSpeedAnimationServerRpc(0f, (int)State.Stunned, false, false, false, Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerWhoStunned), delaySpeed, true);
        if (holdingEgg) {
            DropEggServerRpc();
        }
        if (carryingPlayerBody) {
            DropPlayerBodyServerRpc();
        }
        StartCoroutine(StunCooldown(playerWhoStunned, false));
    }
    public override void DoAIInterval()
    {
        base.DoAIInterval();
        if (isEnemyDead || StartOfRound.Instance.allPlayersDead || !IsHost) return;
        
        switch (currentBehaviourStateIndex)
        {
            case (int)State.Wandering:
                DoWandering();
                break;
            case (int)State.Guarding:
                DoGuarding();
                break;
            case (int)State.ChasingPlayer:
                DoChasingPlayer();
                break;
            case (int)State.DragPlayerBodyToNest:
                DoDragPlayerBodyToNest();
                break;
            default:
                break;
        }
    }

    public void DoWandering()
    {
        if (!isAggro)
        {
            if (timeSinceAction >= enemyRandom.Next(10, 20)) {
                timeSinceAction = 0;
                ControlStateSpeedAnimationServerRpc(0f, (int)State.Idle, false, false, false, -1, true, false);
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
        if (goldenEgg.isHeld && !holdingEgg) {
            LogIfDebugBuild("Someone grabbed the egg");
            SetTargetServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, goldenEgg.playerHeldBy));
            PlayMiscSoundsServerRpc(2);
            ControlStateSpeedAnimationServerRpc(SPRINTING_SPEED, (int)State.ChasingPlayer, false, true, false, -1, true, true);
            return true;
        }
        return false;
    }

    [ClientRpc]
    public void HonkAnimationClientRpc() {
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
            ControlStateSpeedAnimationServerRpc(WALKING_SPEED, (int)State.Wandering, true, false, false, -1, true, false);
            SetTargetServerRpc(-1);
            return;
        }
        if (targetPlayer == null && recentlyDamaged) {
            LogIfDebugBuild("Target player is null"); // playerWhoLastHit is probably being set to null or smthn idk.
            SetTargetServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerWhoLastHit));
        }
        // Prioritize recently damaged logic
        if (recentlyDamaged && targetPlayer != null) {
            LogIfDebugBuild("Chasing player because recently damaged");
            if (holdingEgg) {
                DropEggServerRpc();
            }
            if (carryingPlayerBody) {
                DropPlayerBodyServerRpc();
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
            SetTargetServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, goldenEgg.playerHeldBy));
            return;
        } else if (goldenEgg.playerHeldBy == null) {
            LogIfDebugBuild("Egg is not held by any player");
            if (targetPlayer != null) SetTargetServerRpc(-1);
            // If not recently hit and egg is not held, go to the egg
            if (Vector3.Distance(this.transform.position, goldenEgg.transform.position) < 1f && !holdingEgg) {
                GrabEggServerRpc();
                PlayMiscSoundsServerRpc(0);
                ControlStateSpeedAnimationServerRpc(WALKING_SPEED, (int)State.Guarding, false, false, true, -1, true, false);
                return;
            }
            SetDestinationToPosition(goldenEgg.transform.position, false);
            return;
        }

        if (!recentlyDamaged && goldenEgg.playerHeldBy != null && targetPlayer != goldenEgg.playerHeldBy) {
            LogIfDebugBuild("Switching to chase player holding the egg");
            SetTargetServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, goldenEgg.playerHeldBy));
        }
    } // todo: everywhere i use setdestinationtoposition, i need to make a navmeshpath check to even see if it's possible to go there.

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
        DropEgg();
    }

    public IEnumerator DelayDroppingEgg() {
        yield return new WaitForSeconds(0.25f);
        DropEggClientRpc();
    }

    public void DropEgg() {
        goldenEgg.parentObject = null;
        if (IsServer) goldenEgg.transform.SetParent(StartOfRound.Instance.propsContainer, true);
        goldenEgg.EnablePhysics(true);
        goldenEgg.fallTime = 0f;
        if (IsServer) goldenEgg.startFallingPosition = goldenEgg.transform.parent.InverseTransformPoint(goldenEgg.transform.position);
        if (IsServer) goldenEgg.targetFloorPosition = goldenEgg.transform.parent.InverseTransformPoint(goldenEgg.GetItemFloorPosition(default(Vector3)));
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
            ControlStateSpeedAnimationServerRpc(0, (int)State.Death, false, false, false, -1, true, false);
            if (holdingEgg) {
                DropEggServerRpc();
            }
            if (carryingPlayerBody) {
                DropPlayerBodyServerRpc();
            }
        }
        var FeatherPS = this.transform.Find("GooseRig").Find("Root").Find("Torso").Find("Torso.001").Find("Feather PS").GetComponent<ParticleSystem>().main;
        FeatherPS.loop = false;
        creatureVoice.PlayOneShot(deathSounds[enemyRandom.Next(0, deathSounds.Length)]);
    }

    [ServerRpc(RequireOwnership = false)]
    private void DropPlayerBodyServerRpc()
    {
        DropPlayerBodyClientRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void DropEggServerRpc()
    {
        StartCoroutine(DelayDroppingEgg());
    } // hittingenemyserverrpc and the speed

    public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
        if (force == 0) {
            LogIfDebugBuild("Hit with force 0");
            return;
        }
        featherHitParticles.Play();
        creatureVoice.PlayOneShot(hitSounds[enemyRandom.Next(0, hitSounds.Length)]);
        if (isEnemyDead || currentBehaviourStateIndex == (int)State.Death) return;
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
                ControlStateSpeedAnimationServerRpc(WALKING_SPEED, (int)State.Wandering, true, false, false, -1, false, false);
                return;
            }
            SetDestinationToPosition(outsideEntrancePosition);
            
            if (Vector3.Distance(transform.position, outsideEntrancePosition) < 1f) {
                this.agent.Warp(insideEntrancePosition);
                SetEnemyOutsideServerRpc(false);
            }
        } else {
            if (NavMesh.CalculatePath(this.transform.position, insideEntrancePosition, this.agent.areaMask, pathToTeleport)) {
                SetDestinationToPosition(insideEntrancePosition);
            }
            if (Vector3.Distance(transform.position, insideEntrancePosition) < 1f) {
                this.agent.Warp(outsideEntrancePosition);
                SetEnemyOutsideServerRpc(true);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetEnemyOutsideServerRpc(bool setOutside)
    {
        SetEnemyOutsideClientRpc(setOutside);
    }

    [ClientRpc]
    public void SetEnemyOutsideClientRpc(bool setOutisde) {
        this.SetEnemyOutside(setOutisde);
    }
    public IEnumerator RecentlyDamagedCooldown(PlayerControllerB playerWhoHit) {
        recentlyDamaged = true;
        playerWhoLastHit = playerWhoHit;
        if (IsHost) SetTargetServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerWhoHit));
        yield return new WaitForSeconds(enemyRandom.Next(10, 15));
        if (IsHost && !(currentBehaviourStateIndex != (int)State.Death || currentBehaviourStateIndex != (int)State.Stunned)) PlayMiscSoundsServerRpc(1);
        recentlyDamaged = false;
    }
    
    public void PlayerHitEnemy(PlayerControllerB playerWhoStunned = null)
    {
        playerHits += 1;
        LogIfDebugBuild($"PlayerHitEnemy called. Current hits: {playerHits}, Current State: {currentBehaviourStateIndex}");
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
            if (!IsHost) return;
            AggroOnHit(playerWhoStunned);
        }
    }

    public void AggroOnHit(PlayerControllerB playerWhoStunned)
    {
        SetTargetServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerWhoStunned));
        PlayMiscSoundsServerRpc(2);
        ControlStateSpeedAnimationServerRpc(SPRINTING_SPEED, (int)State.ChasingPlayer, false, true, false, -1, true, true);
    }

    public void PlayFeatherSound() // Animation Event
    {
        creatureVoice.PlayOneShot(featherSounds[enemyRandom.Next(0, featherSounds.Length)]);
    }

    [ServerRpc(RequireOwnership = false)]
    public void PlayMiscSoundsServerRpc(int soundID) {
        PlayMiscSoundsClientRpc(soundID);
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
            DropPlayerBodyServerRpc();
        }
        if (Vector3.Distance(this.transform.position, nest.transform.position) < 1f) {
            DropPlayerBodyServerRpc();
        }
    }

    [ClientRpc]
    public void SpawnEggInNestClientRpc()
    {
        if (IsHost) {
            CodeRebirthUtils.Instance.SpawnScrapServerRpc("GoldenEgg", nest.transform.position, false, true); // todo: for some reason value doesn't sync on clients
        }
        StartCoroutine(DelayFindingGoldenEgg());
    }

    public IEnumerator DelayFindingGoldenEgg()
    {
        yield return new WaitForSeconds(0.5f);
        CodeRebirthUtils.goldenEggs = CodeRebirthUtils.goldenEggs.Where(egg => egg != null).ToList();
        goldenEgg = CodeRebirthUtils.goldenEggs.Where(egg => Vector3.Distance(egg.transform.position, this.transform.position) < 2f).FirstOrDefault();
        if (IsServer) goldenEgg.transform.SetParent(StartOfRound.Instance.propsContainer, true);
        LogIfDebugBuild($"Found egg in nest: {goldenEgg.itemProperties.itemName}");
    }

    public void HoversNearNest()
    {
        // Logic for hovering near nest
        if (Vector3.Distance(this.transform.position, nest.transform.position) < 0.75f)
        {
            DropEggServerRpc();
            ControlStateSpeedAnimationServerRpc(0f, (int)State.Idle, false, false, true, -1, true, false);
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
        LogIfDebugBuild("Player killed");
        targetPlayer.DamagePlayer(200, true, true, CauseOfDeath.Bludgeoning, 0, false, default);
        if (Vector3.Distance(goldenEgg.transform.position, nest.transform.position) <= 0.75f) {
            CarryingDeadPlayerServerRpc();
            ControlStateSpeedAnimationServerRpc(WALKING_SPEED, (int)State.DragPlayerBodyToNest, false, false, true, -1, true, false);
        } else {
            SetDestinationToPosition(goldenEgg.transform.position, false);
        }
        SetTargetServerRpc(-1);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CarryingDeadPlayerServerRpc()
    {
        CarryingDeadPlayerClientRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void ControlStateSpeedAnimationServerRpc(float speed, int state, bool startSearch, bool running, bool guarding, int playerWhoStunnedIndex, bool delaySpeed, bool _isAggro)
    {
        ControlStateSpeedAnimationClientRpc(speed, state, startSearch, running, guarding, playerWhoStunnedIndex, delaySpeed, _isAggro);
    }

    [ServerRpc(RequireOwnership = false)]
    private void GrabEggServerRpc() {
        GrabEggClientRpc();
        goldenEgg.grabbable = false;
        goldenEgg.grabbableToEnemies = false;
    }
    [ClientRpc]
    private void GrabEggClientRpc()
    {
        holdingEgg = true;
        goldenEgg.isHeldByEnemy = true;
        goldenEgg.parentObject = this.transform;
        goldenEgg.transform.position = this.transform.position + transform.up * 4f;
    }

    [ClientRpc]
    public void CarryingDeadPlayerClientRpc()
    {
        carryingPlayerBody = true;
        bodyBeingCarried = targetPlayer.deadBody;
        bodyBeingCarried.attachedTo = this.TopOfBody;
        bodyBeingCarried.attachedLimb = this.targetPlayer.deadBody.bodyParts[0];
        bodyBeingCarried.matchPositionExactly = true;
    }

    [ClientRpc]
    public void DropPlayerBodyClientRpc() {
        DropPlayerBody();
    }
    private IEnumerator StunCooldown(PlayerControllerB playerWhoStunned, bool delaySpeed = true)
    {
        yield return new WaitUntil(() => this.stunNormalizedTimer <= 0);
        if (isEnemyDead) yield break;

        SetTargetServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerWhoStunned));
        ControlStateSpeedAnimationServerRpc(SPRINTING_SPEED, (int)State.ChasingPlayer, false, true, false, Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerWhoStunned), delaySpeed, true);
    }

    public override void OnCollideWithPlayer(Collider other)
    {
        if (timeSinceHittingLocalPlayer < 1f) return;
        PlayerControllerB player = MeetsStandardPlayerCollisionConditions(other, false, true);
        if (player == null || player != GameNetworkManager.Instance.localPlayerController && currentBehaviourStateIndex != (int)State.ChasingPlayer) {
            LogIfDebugBuild("Player does not meet standard player conditions");
            return;
        } else {
            LogIfDebugBuild("Player meets standard player conditions");
        }

        if (targetPlayer == null) return;
        
        if (player.currentlyHeldObjectServer == goldenEgg)
        {
            KillPlayerWithEgg();
        } else {
            timeSinceHittingLocalPlayer = 0;
            LogIfDebugBuild("Hitting player");
            player.DamagePlayer(75, true, true, CauseOfDeath.Bludgeoning, 0, false, default);
            if (player.health <= 0) {
                LogIfDebugBuild("Player is dead");
                HostDecisionAfterDeathServerRpc();
                SetTargetServerRpc(-1);
            }
        }
    }
    

    [ServerRpc(RequireOwnership = false)]
    private void HostDecisionAfterDeathServerRpc()
    {
        if (Vector3.Distance(goldenEgg.transform.position, nest.transform.position) <= 2f) {
            LogIfDebugBuild("Carrying dead player");
            CarryingDeadPlayerClientRpc();
            ControlStateSpeedAnimationClientRpc(WALKING_SPEED, (int)State.DragPlayerBodyToNest, false, false, true, -1, true, false);
        } else {
            SetDestinationToPosition(goldenEgg.transform.position, false);
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
            ControlStateSpeedAnimationServerRpc(0f, (int)State.Wandering, true, false, false, -1, false, false);
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