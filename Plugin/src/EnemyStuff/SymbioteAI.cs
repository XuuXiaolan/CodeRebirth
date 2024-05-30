using CodeRebirth.Misc;
using CodeRebirth.src.EnemyStuff;

namespace CodeRebirth.EnemyStuff;
public class SymbioteAI : CodeRebirthEnemyAI
{

    public enum State {
    }

    public override void Start() {
        base.Start();
    }

    public override void DoAIInterval() {
        base.DoAIInterval();
        if (isEnemyDead || StartOfRound.Instance.allPlayersDead) return;

        switch(currentBehaviourStateIndex.ToSymbioteState()) {
            default:
                LogIfDebugBuild("This Behavior State doesn't exist!");
                break;
        }
    }
}