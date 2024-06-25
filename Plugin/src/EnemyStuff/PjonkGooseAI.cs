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

namespace CodeRebirth.EnemyStuff;
public class PjonkGooseAI : CodeRebirthEnemyAI
{
    private SimpleWanderRoutine currentWander;
    private Coroutine wanderCoroutine;
    private const float WALKING_SPEED = 6f;
    private const float SPRINTING_SPEED = 15f;
    private bool isAggro = false;
    private int shovelHits = 0;

    public AudioClip[] jumpScareSounds;
    public AudioClip[] featherSounds;
    public AudioClip[] hitSounds;
    public AudioClip[] deathSounds;

    private GrabbableObject goldenEgg;
    private bool holdingEgg;
    public GameObject nest;

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
        SpawnNestClientRpc();
        SpawnEggInNest();
    }

    [ClientRpc]
    public void SpawnNestClientRpc() {
        nest = Instantiate(nest, this.transform.position, Quaternion.identity, RoundManager.Instance.mapPropsContainer.transform);
    }
    public void ControlStateSpeedAnimation(float speed, State state, bool startSearch, bool running, bool guarding) {
        this.ChangeSpeedClientRpc(speed);
        if (stunNormalizedTimer > 0) this.TriggerAnimationClientRpc("Stunned");
        this.SetFloatAnimationClientRpc("MoveZ", speed);
        this.SetBoolAnimationClientRpc("Running", running);
        this.SetBoolAnimationClientRpc("Guarding", guarding);
        this.SetBoolAnimationClientRpc("Aggro", isAggro);
        SwitchToBehaviourClientRpc((int)state);
        if (startSearch) {
            StartSearch(nest.transform.position);
        } else {
            StopSearch(currentSearch);
        }
    }
    public IEnumerator SpawnTimer()
    {
        yield return new WaitForSeconds(3f);
        isAggro = false;
        ControlStateSpeedAnimation(WALKING_SPEED, State.Wandering, true, false, false);

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
            if (goldenEgg.isHeldByEnemy) {
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

    public void DoGuarding()
    {
        HoversNearNest();
        // todo: sit around near the nest after having retrieved an egg from a player after killing said player
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
        if (!goldenEgg.isHeld) {
            SetDestinationToPosition(goldenEgg.transform.position, false);
            if (Vector3.Distance(this.transform.position, goldenEgg.transform.position) < 1f) {
                goldenEgg.isHeldByEnemy = true;
                goldenEgg.grabbable = false;
                holdingEgg = true;
                goldenEgg.parentObject = this.transform;
                goldenEgg.transform.position = this.transform.position + transform.up * 0.5f;
                isAggro = false;
                ControlStateSpeedAnimation(WALKING_SPEED, State.Guarding, false, false, true);
            }
            return;
        }
        SetDestinationToPosition(targetPlayer.transform.position, false);
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
        if (stunNormalizedTimer <= 0f) return;
        StartCoroutine(StunCooldown());
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
        // Your implementation to set the destination of the agent
        agent.SetDestination(position);
    }

    public void AggroOnHit()
    {
        isAggro = true;
        SetBoolAnimationClientRpc("Aggro", true);
        PlayJumpScareSoundClientRpc();
    }

    public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
        if (playerWhoHit.currentlyHeldObjectServer.TryGetComponent<Shovel>(out Shovel shovel)) {
            HitWithShovel(shovel.shovelHitForce, playerWhoHit);
        }
    }
    public void HitWithShovel(int force, PlayerControllerB playerWhoStunned = null)
    {
        shovelHits += force;
        if (shovelHits >= 3)
        {
            SetEnemyStunned(true, 4f, playerWhoStunned);
            SwitchToBehaviourServerRpc((int)State.Stunned);
            shovelHits = 0;
        }
        else if (currentBehaviourStateIndex != (int)State.ChasingPlayer)
        {
            AggroOnHit();
        }
    }

    [ClientRpc]
    public void PlayJumpScareSoundClientRpc()
    {
        creatureVoice.PlayOneShot(jumpScareSounds[UnityEngine.Random.Range(0, jumpScareSounds.Length)]);
    }

    [ClientRpc]
    public void PlayFeatherSoundClientRpc()
    {
        creatureVoice.PlayOneShot(featherSounds[UnityEngine.Random.Range(0, featherSounds.Length)]);
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
        CodeRebirthUtils.Instance.SpawnScrapServerRpc("GoldenEgg", this.transform.position, false);
        goldenEgg = FindObjectsOfType<GoldenEgg>().Where(egg => Vector3.Distance(egg.transform.position, this.transform.position) < 10f).FirstOrDefault();
        LogIfDebugBuild($"Found egg in nest: {goldenEgg.itemProperties.itemName}");
        return;
    }

    public void HoversNearNest()
    {
        // Logic for hovering near nest
        if (Vector3.Distance(this.transform.position, nest.transform.position) < 2f)
        {
            SwitchToBehaviourClientRpc((int)State.Idle);
            ChangeSpeedClientRpc(0f);
            SetBoolAnimationClientRpc("Running", false);
            SetBoolAnimationClientRpc("Guarding", true);
            SetFloatAnimationClientRpc("MoveZ", 0f);
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
            targetPlayer.KillPlayer(targetPlayer.velocityLastFrame, true, CauseOfDeath.Bludgeoning);
            // Carry egg back to nest
        }
    }

    private IEnumerator StunCooldown()
    {
        yield return new WaitUntil(() => this.stunNormalizedTimer <= 0f);
        this.SwitchToBehaviourStateOnLocalClient(previousBehaviourStateIndex);
    }

    public override void OnCollideWithPlayer(Collider other)
    {
        if (isEnemyDead) return;

        if (other.GetComponent<PlayerControllerB>() == targetPlayer && currentBehaviourStateIndex == (int)State.ChasingPlayer)
        {
            KillPlayerWithEgg();
        }
    }
    
    public override void OnCollideWithEnemy(Collider other, EnemyAI collidedEnemy) {
        if (isEnemyDead) return;
        if (targetEnemy != null && collidedEnemy == targetEnemy && currentBehaviourStateIndex == (int)State.ChasingEnemy) {
            // kill enemy and then take the egg back to the nest
        }
    }
}