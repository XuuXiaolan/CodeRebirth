using CodeRebirth.Misc;
using CodeRebirth.src.EnemyStuff;
using CodeRebirth.Util.Extensions;

namespace CodeRebirth.EnemyStuff;
public class PjonkGooseAI : CodeRebirthEnemyAI
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

        switch(currentBehaviourStateIndex.ToPjonkGooseState()) {
            case State.Wandering:
                
                break;
            default:
                LogIfDebugBuild("This Behavior State doesn't exist!");
                break;
        }
    }
}