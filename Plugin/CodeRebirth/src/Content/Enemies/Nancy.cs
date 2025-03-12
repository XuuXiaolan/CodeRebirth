using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.Content.Enemies;
public class Nancy : CodeRebirthEnemyAI
{
    public enum NancyState
    {
        Wandering,
        HealingTarget,
    }

    public override void Start()
    {
        base.Start();
    }

    #region StateMachine

    public override void Update()
    {
        base.Update();

        if (targetPlayer != null || currentBehaviourStateIndex != (int)NancyState.Wandering) return;

        PlayerControllerB localPlayer = GameNetworkManager.Instance.localPlayerController;
        float distance = Vector3.Distance(transform.position, localPlayer.transform.position);
        if (distance < 30 && smartAgentNavigator.CanPathToPoint(this.transform.position, localPlayer.transform.position) <= 20f)
        {

        }
    }

    public override void DoAIInterval()
    {
        base.DoAIInterval();
        if (StartOfRound.Instance.allPlayersDead || isEnemyDead) return;

        switch (currentBehaviourStateIndex)
        {
            case (int)NancyState.Wandering:
                DoWandering();
                break;
            case (int)NancyState.HealingTarget:
                DoHealingTarget();
                break;
        }
    }

    public void DoWandering()
    {

    }

    public void DoHealingTarget()
    {

    }

    #endregion
}