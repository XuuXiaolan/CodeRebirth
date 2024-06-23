using System.Collections;
using System.Linq;
using CodeRebirth.Misc;
using CodeRebirth.src.EnemyStuff;
using CodeRebirth.Util.Extensions;
using UnityEngine;

namespace CodeRebirth.EnemyStuff;
public class PjonkGooseAI : CodeRebirthEnemyAI
{
    private SimpleWanderRoutine currentWander;
    private Coroutine wanderCoroutine;
    private const float WALKING_SPEED = 6f;
    public enum State {
        Spawning,
        Wandering,
        Guarding,
        ChasingPlayer,
        ChasingEnemy,
        Death,
        Stunned,
    }

    public override void Start() {
        base.Start();
        this.SwitchToBehaviourStateOnLocalClient(State.Spawning);
        this.ChangeSpeedOnLocalClient(WALKING_SPEED);
        this.SetFloatAnimationOnLocalClient("MoveZ", agent.speed);
        StartSearch(transform.position);
        StartCoroutine(WaitTimer());
    }

    public IEnumerator WaitTimer() {
        yield return new WaitForSeconds(3f);
        this.SwitchToBehaviourStateOnLocalClient(State.Wandering);
    }
    public void DoSpawning() {

    }

    public void DoWandering() {

    }

    public void DoGuarding() {

    }

    public void DoChasingPlayer() {

    }

    public void DoChasingEnemy() {

    }

    public void DoDeath() {

    }

    public void DoStunned() {

    }
    public override void DoAIInterval() {
        base.DoAIInterval();
        if (isEnemyDead || StartOfRound.Instance.allPlayersDead) return;

        switch(currentBehaviourStateIndex.ToPjonkGooseState()) {
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
                    this.SetWanderDestinationToPosiiton(this.currentWander.currentTargetNode.transform.position, false);
                    this.currentWander.unvisitedNodes.Remove(this.currentWander.currentTargetNode);

                    // Wait until reaching the target
                    yield return new WaitUntil(() => Vector3.Distance(transform.position, this.currentWander.currentTargetNode.transform.position) < this.currentWander.searchPrecision);
                }
            }
        }
        yield break;
    }

    private void SetWanderDestinationToPosiiton(Vector3 position, bool stopCurrentPath)
    {
        // Your implementation to set the destination of the agent
        agent.SetDestination(position);
    }
}