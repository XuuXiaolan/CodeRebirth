using CodeRebirth.Misc;
using CodeRebirth.src.EnemyStuff;

namespace CodeRebirth.EnemyStuff;
public class SnailCatAI : CodeRebirthEnemyAI
{

    public enum State {
        Wandering,
    }

    public override void Start() {
        base.Start();
        StartSearch(transform.position);
        this.SwitchToBehaviourStateOnLocalClient(State.Wandering);
    }

    public override void DoAIInterval() {
        base.DoAIInterval();
        if (isEnemyDead || StartOfRound.Instance.allPlayersDead) return;

        switch(currentBehaviourStateIndex.ToSnailState()) {
            case State.Wandering:
                agent.speed = 4f;
                break;
            default:
                LogIfDebugBuild("This Behavior State doesn't exist!");
                break;
        }
    }
}