using GameNetcodeStuff;

namespace CodeRebirth.src.Content.Enemies;
public class TemplateAI : CodeRebirthEnemyAI
{

    public enum TemplateState
    {
        Spawning,
        Idle,
        Death,
    }

    #region Unity Lifecycles
    public override void Start()
    {
        base.Start();
    }

    public override void Update()
    {
        base.Update();
    }
    #endregion

    #region StateMachines
    public override void DoAIInterval()
    {
        base.DoAIInterval();
        if (StartOfRound.Instance.allPlayersDead || isEnemyDead)
            return;

        switch (currentBehaviourStateIndex)
        {
            case (int)TemplateState.Spawning:
                DoSpawning();
                break;
            case (int)TemplateState.Idle:
                DoIdle();
                break;
            case (int)TemplateState.Death:
                DoDeath();
                break;
        }
    }

    private void DoSpawning()
    {

    }

    private void DoIdle()
    {

    }

    private void DoDeath()
    {

    }
    #endregion

    #region  Misc Functions
    #endregion

    #region Animation Events
    #endregion

    #region Call Backs
    public override void HitEnemy(int force = 1, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
        if (isEnemyDead)
            return;

        enemyHP -= force;

        if (enemyHP <= 0)
        {
            if (IsOwner)
            {
                KillEnemyOnOwnerClient();
            }
            return;
        }
    }

    public override void KillEnemy(bool destroy = false)
    {
        base.KillEnemy(destroy);
        SwitchToBehaviourStateOnLocalClient((int)TemplateState.Death);
    }
    #endregion
}