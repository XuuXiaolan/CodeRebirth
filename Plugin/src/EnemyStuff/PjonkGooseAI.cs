using System.Collections;
using CodeRebirth.Misc;
using CodeRebirth.src.EnemyStuff;
using CodeRebirth.Util.Extensions;
using UnityEngine;

namespace CodeRebirth.EnemyStuff;
public class PjonkGooseAI : CodeRebirthEnemyAI
{
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
        this.ChangeSpeedOnLocalClient(6f);
        this.SetFloatAnimationOnLocalClient("MoveZ", 6f);
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
}