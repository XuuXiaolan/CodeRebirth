using GameNetcodeStuff;

namespace CodeRebirth.src.Content.Enemies;
public class TemplateAI : CodeRebirthEnemyAI
{

    public enum TemplateState
    {
        
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

        switch (currentBehaviourStateIndex)
        {

        }
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
    }

    public override void KillEnemy(bool destroy = false)
    {
        base.KillEnemy(destroy);
    }
    #endregion
}