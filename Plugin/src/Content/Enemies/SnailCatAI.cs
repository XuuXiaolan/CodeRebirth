namespace CodeRebirth.src.Content.Enemies;
public class SnailCatAI : CodeRebirthEnemyAI
{

    public enum State {
        Wandering,
    }

    public override void Start() {
        base.Start();
        StartSearch(transform.position);
        SwitchToBehaviourStateOnLocalClient((int)State.Wandering);
    }

    public override void DoAIInterval() {
        base.DoAIInterval();
        if (isEnemyDead || StartOfRound.Instance.allPlayersDead) return;

        switch(currentBehaviourStateIndex) {
            case (int)State.Wandering:
                agent.speed = 4f;
                break;
            default:
                Plugin.Logger.LogWarning("This Behavior State doesn't exist!");
                break;
        }
    }
}