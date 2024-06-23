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

namespace CodeRebirth.EnemyStuff
{
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
        }

        public override void Start()
        {
            base.Start();
            if (!IsHost) return;
            this.SwitchToBehaviourClientRpc((int)State.Spawning);
            this.ChangeSpeedClientRpc(WALKING_SPEED);
            this.SetFloatAnimationClientRpc("MoveZ", agent.speed);
            StartSearch(transform.position);
            StartCoroutine(WaitTimer1());
            SpawnNestClientRpc();
        }

        [ClientRpc]
        public void SpawnNestClientRpc() {
            Object.Instantiate(nest, this.transform.position, Quaternion.identity, RoundManager.Instance.mapPropsContainer.transform);
            if (IsHost) StartCoroutine(WaitTimer2());
        }

        public IEnumerator WaitTimer2() {
            yield return new WaitForSeconds(3f);
            SpawnEggInNest();
        }
        public IEnumerator WaitTimer1()
        {
            yield return new WaitForSeconds(3f);
            this.SwitchToBehaviourClientRpc((int)State.Wandering);
        }

        public void DoSpawning()
        {
            // Handle spawning logic
        }

        public void DoWandering()
        {
            if (!isAggro)
            {
                if (wanderCoroutine == null)
                {
                    StartWandering(nest.transform.position);
                }
            }
        }

        public void DoGuarding()
        {
            // Handle guarding logic
        }

        public void DoChasingPlayer()
        {
            if (targetPlayer == null || !targetPlayer.IsSpawned || targetPlayer.isPlayerDead)
            {
                this.SwitchToBehaviourStateOnLocalClient(State.Wandering);
                return;
            }

            SetDestinationToPosition(targetPlayer.transform.position, false);
        }

        public void DoChasingEnemy()
        {
            // Handle chasing enemy logic
        }

        public void DoDeath()
        {
            // Handle death logic
            // Play the Death sound
        }

        public void DoStunned()
        {
            if (stunNormalizedTimer <= 0f) return;
            StartCoroutine(StunCooldown());
        }

        public override void Update()
        {
            base.Update();
            if (goldenEgg == null) {
                LogIfDebugBuild("Golden Egg is null!");
                return;
            }
            if (goldenEgg.isHeldByEnemy) {
                LogIfDebugBuild("Golden Egg is held by enemy!");
            } else if (goldenEgg.isHeld) {
                LogIfDebugBuild("Golden Egg is held by player!");
            }
        }
        public override void DoAIInterval()
        {
            base.DoAIInterval();
            if (isEnemyDead || StartOfRound.Instance.allPlayersDead) return;

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
                default:
                    LogIfDebugBuild("This Behavior State doesn't exist!");
                    break;
            }
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
            TriggerAnimationOnLocalClient("AggroJumpScare");
            PlayJumpScareSound();
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
            else
            {
                AggroOnHit();
            }
        }

        public void PlayJumpScareSound()
        {
            creatureVoice.PlayOneShot(jumpScareSounds[UnityEngine.Random.Range(0, jumpScareSounds.Length)]);
        }

        public void PlayFeatherSound()
        {
            creatureVoice.PlayOneShot(featherSounds[UnityEngine.Random.Range(0, featherSounds.Length)]);
        }

        public void DragBodiesToNest()
        {
            foreach (var body in FindNearbyBodies())
            {
                body.transform.position = nest.transform.position;
            }
        }

        private IEnumerable<GameObject> FindNearbyBodies()
        {
            // Implement logic to take the dead body it killed and drag back to nest
            return new List<GameObject>();
        }

        public void SpawnEggInNest()
        { // here
            CodeRebirthUtils.Instance.SpawnScrapServerRpc("GoldenEgg", nest.transform.position, false);
            goldenEgg = FindObjectOfType<GoldenEgg>();
            LogIfDebugBuild($"Found egg in nest: {goldenEgg.itemProperties.itemName}");
            return;
        }

        public void HoversNearNest()
        {
            // Logic for hovering near nest
            ChangeSpeedOnLocalClient(0f);
            SetBoolAnimationOnLocalClient("Running", false);
            SetBoolAnimationOnLocalClient("Guarding", false);
            SetFloatAnimationOnLocalClient("MoveZ", 0f);
        }

        public void KillPlayerWithEgg()
        {
            if (targetPlayer == null) return;
            if (targetPlayer.currentlyHeldObjectServer.itemProperties.itemName == "Pjonk's Golden Egg")
            {
                targetPlayer.KillPlayer(targetPlayer.velocityLastFrame, true, CauseOfDeath.Bludgeoning);
                targetPlayer.transform.position = nest.transform.position;
            }
        }

        private IEnumerator StunCooldown()
        {
            yield return new WaitUntil(() => this.stunNormalizedTimer <= 0f);
            this.SwitchToBehaviourStateOnLocalClient(State.Wandering);
        }

        public override void OnCollideWithPlayer(Collider other)
        {
            if (isEnemyDead) return;

            if (other.GetComponent<PlayerControllerB>() == targetPlayer)
            {
                KillPlayerWithEgg();
            }
        }
    }
}
