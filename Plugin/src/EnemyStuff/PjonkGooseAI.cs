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
    private const float SPRINTING_SPEED = 15f;
    private bool isAggro = false;
    private int playerHits = 0;
    public AudioClip[] jumpScareSounds;
    public AudioClip[] featherSounds;
    public AudioClip[] hitSounds;
    public AudioClip[] deathSounds;

    private GrabbableObject goldenEgg;
    private bool holdingEgg;
    public GameObject nest;
    private bool recentlyDamaged = false;
    private bool nestCreated = false;
    private Coroutine recentlyDamagedCoroutine;

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
    }

    public override void Start()
    {
        base.Start();
        if (!IsHost) return;
        isAggro = false;
        ControlStateSpeedAnimation(0f, State.Spawning, false, false, false);
        StartCoroutine(SpawnTimer());
    }

    [ClientRpc]
    public void SpawnNestClientRpc() {
        nest = Instantiate(nest, this.transform.position + Vector3.down * 0.02f, Quaternion.identity, RoundManager.Instance.mapPropsContainer.transform);
    }
    public void ControlStateSpeedAnimation(float speed, State state, bool startSearch, bool running, bool guarding) {
        this.ChangeSpeedClientRpc(speed);
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
            default:
                LogIfDebugBuild("This Behavior State doesn't exist!");
                break;
        }
    }

    public void DoSpawning()
    {

    }

    public void DoWandering()
    {
        if (!isAggro)
        {
            if (wanderCoroutine == null)
            {
                StartWandering(nest.transform.position);
            }
            if (goldenEgg.isHeldByEnemy && !holdingEgg) {
                LogIfDebugBuild("An Enemy grabbed the egg");
                isAggro = true;
                ControlStateSpeedAnimation(SPRINTING_SPEED, State.ChasingEnemy, false, true, true);
                SetTargetClientRpc(-1);
                var yippeeHoldingEgg = FindYippeeHoldingEgg();
                if (yippeeHoldingEgg == null) 
                {      
                    LogIfDebugBuild("enemy holding egg could not be found");
                    return;
                }
                SetEnemyTargetClientRpc(Array.IndexOf(RoundManager.Instance.SpawnedEnemies.ToArray(), yippeeHoldingEgg));
            } else if (goldenEgg.isHeld) {
                LogIfDebugBuild("Someone grabbed the egg");
                // maybe honk or some stuff.
                isAggro = true;
                ControlStateSpeedAnimation(SPRINTING_SPEED, State.ChasingPlayer, false, true, true);
                SetTargetClientRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, goldenEgg.playerHeldBy));
            }
        }
    }

    public void DoGuarding()
    {
        HoversNearNest();
    }

    public void DoChasingPlayer()
    {
        if (targetPlayer == null || !targetPlayer.IsSpawned || targetPlayer.isPlayerDead)
        {
            isAggro = false;
            ControlStateSpeedAnimation(WALKING_SPEED, State.Wandering, true, false, false);
            SetTargetClientRpc(-1);
            return;
        }

        // If the golden egg is held by the player, keep chasing the player until the egg is dropped
        if (goldenEgg != null && goldenEgg.playerHeldBy == targetPlayer)
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
            SetDestinationToPosition(targetPlayer.transform.position, false);
            if (!goldenEgg.playerHeldBy)
            {
                // Player dropped the egg, pick it up
                if (Vector3.Distance(this.transform.position, goldenEgg.transform.position) < 1f)
                {
                    holdingEgg = true;
                    goldenEgg.isHeldByEnemy = true;
                    goldenEgg.grabbable = false;
                    goldenEgg.parentObject = this.transform;
                    goldenEgg.transform.position = this.transform.position + transform.up * 0.5f;
                    isAggro = false;
                    ControlStateSpeedAnimation(WALKING_SPEED, State.Guarding, false, false, true);
                }
                return;
            }
        } else if (recentlyDamaged) {
            // If recently hit, chase the player
            SetDestinationToPosition(targetPlayer.transform.position, false);
        } else {
            // If not recently hit and egg is not held, go to the egg
            SetDestinationToPosition(goldenEgg.transform.position, false);
            if (Vector3.Distance(this.transform.position, goldenEgg.transform.position) < 1f)
            {
                holdingEgg = true;
                goldenEgg.isHeldByEnemy = true;
                goldenEgg.grabbable = false;
                goldenEgg.parentObject = this.transform;
                goldenEgg.transform.position = this.transform.position + transform.up * 0.5f;
                isAggro = false;
                ControlStateSpeedAnimation(WALKING_SPEED, State.Guarding, false, false, true);
            }
        }
    }

    public void DoChasingEnemy()
    {
        // todo: get a reference to the enemyAI that took the egg and if kill-able, chase them and beat them to death
    }

    public void DoDeath()
    {
        // Handle death logic
    }

    public void DoStunned()
    {
        // Handle stun logic
    }

    public void DoIdle() {
        // do nothing
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
                this.currentWander.currentTargetNode = this.currentWander.unvisitedNodes
                    .Where(node => Vector3.Distance(this.currentWander.NestPosition, node.transform.position) <= this.currentWander.wanderRadius)
                    .OrderBy(node => UnityEngine.Random.value)
                    .FirstOrDefault();

                if (this.currentWander.currentTargetNode != null)
                {
                    this.SetWanderDestinationToPosition(this.currentWander.currentTargetNode.transform.position, false);
                    this.currentWander.unvisitedNodes.Remove(this.currentWander.currentTargetNode);

                    // Wait until reaching the target
                    yield return new WaitUntil(() => Vector3.Distance(transform.position, this.currentWander.currentTargetNode.transform.position) < this.currentWander.searchPrecision);
                }
            }
        }
        yield break;
    }

    private void SetWanderDestinationToPosition(Vector3 position, bool stopCurrentPath)
    {
        if (position == Vector3.zero) {
            Debug.LogWarning("Attempted to set destination to Vector3.zero");
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

    public override void KillEnemy(bool destroy = false) {
        base.KillEnemy(destroy);
        if (IsHost) {
            TriggerAnimationClientRpc("Death");
            ControlStateSpeedAnimation(0, State.Death, false, false, false);
            if (holdingEgg) {
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
            LogIfDebugBuild("outside entrance position: " + outsideEntrancePosition);
            if (NavMesh.CalculatePath(this.transform.position, outsideEntrancePosition, this.agent.areaMask, pathToTeleport)) {
                SetDestinationToPosition(outsideEntrancePosition);
            }
            if (Vector3.Distance(transform.position, outsideEntrancePosition) < 1f) {
                this.agent.Warp(insideEntrancePosition);
                this.SetEnemyOutside(false);
            }
        } else {
            LogIfDebugBuild("inside entrance position: " + insideEntrancePosition);
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
        LogIfDebugBuild("Enemy recently damaged");
        yield return new WaitForSeconds(currentBehaviourStateIndex == (int)State.Stunned ? 8f : 5f);
        LogIfDebugBuild("Enemy not recently damaged");
        recentlyDamaged = false;
    }
    
    public void PlayerHitEnemy(int force, PlayerControllerB playerWhoStunned = null)
    {
        playerHits += force;
        if (playerHits >= 3 && playerWhoStunned != null && currentBehaviourStateIndex != (int)State.Stunned)
        {
            SetEnemyStunned(true, 5.317f, playerWhoStunned);
            playerHits = 0;
            if (!IsHost) return; 
            TriggerAnimationClientRpc("Stunned");
            ControlStateSpeedAnimation(0f, State.Stunned, false, false, false);
            if (holdingEgg) {
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
        foreach (var body in FindNearbyBodies())
        {
            body.transform.position = nest.transform.position;
            // This is just rudimentry, I should make it keep reference of the bodies it killed, and take those back to the nest by having the egg on it's back and the dead body on it's mouth.
        }
    }

    private IEnumerable<GameObject> FindNearbyBodies()
    {
        // Implement logic to take the dead body it killed and drag back to nest
        return new List<GameObject>();
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
            if (targetPlayer != null) {
               this.transform.LookAt(targetPlayer.transform.position);
            }
            ControlStateSpeedAnimation(0f, State.Idle, false, false, true);
            StartCoroutine(SpawnTimer());
        } else {
            SetDestinationToPosition(nest.transform.position, false);
        }
    }

    public void KillPlayerWithEgg()
    {
        if (targetPlayer == null) return;
        if (targetPlayer.currentlyHeldObjectServer == goldenEgg)
        {
            targetPlayer.KillPlayer(targetPlayer.velocityLastFrame * 5f, true, CauseOfDeath.Bludgeoning);
            SetTargetClientRpc(-1);
            // Carry egg back to nest
        } else {
            targetPlayer.DamagePlayer(30, true, true, CauseOfDeath.Mauling, 0, false, this.transform.forward * 3f);
        }
    }

    private IEnumerator StunCooldown(PlayerControllerB playerWhoStunned)
    {
        yield return new WaitUntil(() => this.stunNormalizedTimer <= 0);
        if (isEnemyDead) yield break;

        isAggro = true;
        SetTargetClientRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerWhoStunned));
        ControlStateSpeedAnimation(SPRINTING_SPEED, State.ChasingPlayer, false, true, false);
        this.HitEnemy(0, playerWhoStunned, false, -4);
    }

    public override void OnCollideWithPlayer(Collider other)
    {
        if (isEnemyDead) return;
        // todo: make it so that the rest of this function only gets used if the enemy's collider's gameobject name that the player collided with is Collisions 
        if (targetPlayer != null && other.GetComponent<PlayerControllerB>() == targetPlayer && currentBehaviourStateIndex == (int)State.ChasingPlayer)
        {
            KillPlayerWithEgg();
        } else if ((int)State.ChasingPlayer == currentBehaviourStateIndex) {
            other.GetComponent<PlayerControllerB>().DamagePlayer(20);
        }
    }
    
    public override void OnCollideWithEnemy(Collider other, EnemyAI collidedEnemy) {
        if (isEnemyDead) return;
        if (targetEnemy != null && collidedEnemy == targetEnemy && currentBehaviourStateIndex == (int)State.ChasingEnemy) {
            // kill enemy and then take the egg back to the nest
        }
    }
}