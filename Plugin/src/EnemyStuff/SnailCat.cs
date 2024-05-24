using System.Diagnostics;
using CodeRebirth.Misc;
using Unity.Netcode;

namespace CodeRebirth.EnemyStuff;
public class SnailCatAI : EnemyAI
{

    public enum State {
        Wandering,
    }

    [Conditional("DEBUG")]
    void LogIfDebugBuild(string text) {
        Plugin.Logger.LogInfo(text);
    }

    public override void Start() {
        base.Start();
        LogIfDebugBuild("SnailCat Spawned.");
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
    [ClientRpc]
    private void DoAnimationClientRpc(string animationName) {
        LogIfDebugBuild(animationName);
        creatureAnimator.SetTrigger(animationName);
    }
}
