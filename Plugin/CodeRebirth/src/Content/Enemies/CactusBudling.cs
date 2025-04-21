using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.Content.Enemies;
public class CactusBudling : CodeRebirthEnemyAI
{
    [Header("Cactus Budling")]
    [SerializeField]
    private AnimationClip _spawnAnimation = null!;

    private static readonly int RunSpeedFloat = Animator.StringToHash("RunSpeed"); // Float
    private static readonly int RollingAnimation = Animator.StringToHash("isRolling"); // Bool
    private static readonly int RootingAnimation = Animator.StringToHash("isRooting"); // Bool
    private static readonly int DeadAnimation = Animator.StringToHash("isDead"); // Bool

    public enum CactusBudlingState
    {
        Spawning,
        SearchingForRoot,
        Rooted,
        Rolling,
        Dead,
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

        creatureAnimator.SetFloat(RunSpeedFloat, agent.velocity.magnitude / 3f);
        switch (currentBehaviourStateIndex)
        {
            case (int)CactusBudlingState.Spawning:
                DoSpawning();
                break;
            case (int)CactusBudlingState.SearchingForRoot:
                DoSearchingForRoot();
                break;
            case (int)CactusBudlingState.Rooted:
                DoRooted();
                break;
            case (int)CactusBudlingState.Rolling:
                DoRolling();
                break;
            case (int)CactusBudlingState.Dead:
                DoDead();
                break;
        }
    }

    public void DoSpawning()
    {
        
    }

    public void DoSearchingForRoot()
    {
        
    }

    public void DoRooted()
    {
        
    }

    public void DoRolling()
    {
        
    }

    public void DoDead()
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
    }

    public override void KillEnemy(bool destroy = false)
    {
        base.KillEnemy(destroy);
    }
    #endregion
}