using System.Collections;
using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.Content.Enemies;
public class CactusBudling : CodeRebirthEnemyAI, IVisibleThreat
{
    [Header("Animations")]
    [SerializeField]
    private AnimationClip _spawnAnimation = null!;
    [SerializeField]
    private AnimationClip _rootingEndAnimation = null!;
    [SerializeField]
    private AnimationClip _rollingStartAnimation = null!;
    [SerializeField]
    private AnimationClip _rollingEndAnimation = null!;

    [Header("Mechanics")]
    [SerializeField]
    private float _rollingDuration = 20f;

    private Vector3 _targetRootPosition = Vector3.zero;
    private Vector3 _targetRollingPosition = Vector3.zero;
    private float _rollingTimer = 20f;
    private CactusBudlingState _nextState = CactusBudlingState.Spawning;
    private Coroutine? _nextStateRoutine = null;

    private static readonly int RunSpeedFloat = Animator.StringToHash("RunSpeed"); // Float
    private static readonly int RollingAnimation = Animator.StringToHash("isRolling"); // Bool
    private static readonly int RootingAnimation = Animator.StringToHash("isRooting"); // Bool
    private static readonly int DeadAnimation = Animator.StringToHash("isDead"); // Bool

    #region ThreatType
    ThreatType IVisibleThreat.type => ThreatType.ForestGiant;

    int IVisibleThreat.SendSpecialBehaviour(int id)
    {
        return 0;
    }

    int IVisibleThreat.GetThreatLevel(Vector3 seenByPosition)
    {
        return 18;
    }

    int IVisibleThreat.GetInterestLevel()
    {
        return 0;
    }

    Transform IVisibleThreat.GetThreatLookTransform()
    {
        return eye;
    }

    Transform IVisibleThreat.GetThreatTransform()
    {
        return base.transform;
    }

    Vector3 IVisibleThreat.GetThreatVelocity()
    {
        if (base.IsOwner)
        {
            return agent.velocity;
        }
        return Vector3.zero;
    }

    float IVisibleThreat.GetVisibility()
    {
        if (isEnemyDead)
        {
            return 0f;
        }
        if (agent.velocity.sqrMagnitude > 0f)
        {
            return 1f;
        }
        return 0.75f;
    }
    #endregion

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
        _targetRootPosition = RoundManager.Instance.GetRandomNavMeshPositionInRadius(this.transform.position, 40f, default);
        _nextStateRoutine = StartCoroutine(DelayToNextState(_spawnAnimation.length, 3f, CactusBudlingState.SearchingForRoot));
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

        _rollingTimer -= AIIntervalTime;
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

    private void DoSpawning()
    {
        
    }

    private void DoSearchingForRoot()
    {
        if (Vector3.Distance(transform.position, _targetRootPosition) < 2f + agent.stoppingDistance)
        {
            smartAgentNavigator.StopAgent();
            creatureAnimator.SetBool(RootingAnimation, true);
            SwitchToBehaviourServerRpc((int)CactusBudlingState.Rooted);
            return;
        }
        smartAgentNavigator.DoPathingToDestination(_targetRootPosition);
    }

    private void DoRooted()
    {

    }

    private void DoRolling()
    {
        if (_rollingTimer <= 0)
        {
            if (_nextStateRoutine != null)
                return;
            creatureAnimator.SetBool(RollingAnimation, false);
            _targetRootPosition = RoundManager.Instance.GetRandomNavMeshPositionInRadius(this.transform.position, 40f, default);
            _nextStateRoutine = StartCoroutine(DelayToNextState(_rollingEndAnimation.length, 3f, CactusBudlingState.SearchingForRoot));
            return;
        }

        if (Vector3.Distance(transform.position, _targetRollingPosition) < 2f + agent.stoppingDistance)
        {
            _targetRollingPosition = RoundManager.Instance.GetRandomNavMeshPositionInRadius(this.transform.position, 40f, default);
        }
        smartAgentNavigator.DoPathingToDestination(_targetRollingPosition);
    }

    private void DoDead()
    {
        
    }
    #endregion

    #region  Misc Functions
    private IEnumerator DelayToNextState(float delay, float agentSpeed, CactusBudlingState nextState)
    {
        Plugin.ExtendedLogging($"Switching to next state: {nextState}");
        _nextState = nextState;
        agent.speed = 0f;
        smartAgentNavigator.StopAgent();
        yield return new WaitForSeconds(delay);
        SwitchToBehaviourStateOnLocalClient((int)nextState);
        agent.speed = agentSpeed;
        _nextStateRoutine = null;
    }
    #endregion

    #region Animation Events
    #endregion

    #region Call Backs
    public override void OnCollideWithPlayer(Collider other)
    {
        base.OnCollideWithPlayer(other);

        if (currentBehaviourStateIndex != (int)CactusBudlingState.Rolling)
            return;

        // todo
    }

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
        if (currentBehaviourStateIndex == (int)CactusBudlingState.Rolling)
        {
            _rollingTimer += 5f;
            return;
        }

        if (currentBehaviourStateIndex != (int)CactusBudlingState.Rolling)
        {
            if (_nextStateRoutine != null)
            {
                if (_nextState != CactusBudlingState.Rolling)
                {
                    StopCoroutine(_nextStateRoutine);
                }
                else
                {
                    return;
                }
            }
            if (IsServer)
            {
                creatureAnimator.SetBool(RootingAnimation, false);
                creatureAnimator.SetBool(RollingAnimation, true);
            }

            _rollingTimer = _rollingDuration;
            _targetRollingPosition = RoundManager.Instance.GetRandomNavMeshPositionInRadius(this.transform.position, 40f, default);
            float delayTimer = _rollingStartAnimation.length;
            if (currentBehaviourStateIndex == (int)CactusBudlingState.Rooted)
            {
                delayTimer += _rootingEndAnimation.length;
            }
            _nextStateRoutine = StartCoroutine(DelayToNextState(delayTimer, 10f, CactusBudlingState.Rolling));
        }
    }

    public override void KillEnemy(bool destroy = false)
    {
        base.KillEnemy(destroy);
        agent.speed = 0f;

        if (_nextStateRoutine != null)
            StopCoroutine(_nextStateRoutine);

        if (!IsServer)
            return;

        creatureAnimator.SetBool(DeadAnimation, true);
    }
    #endregion
}