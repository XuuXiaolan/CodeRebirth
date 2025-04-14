using CodeRebirth.src;
using CodeRebirth.src.Content.Enemies;
using UnityEngine;

public class Guardsman : CodeRebirthEnemyAI
{
    [SerializeField]
    private Transform _enemyHoldingPoint = null!;

    [SerializeField]
    private float _enemySizeThreshold = 69;

    private bool _killingLargeEnemy = false;
    private float _bufferTimer = 0f;

    private static readonly int RunSpeedFloat = Animator.StringToHash("RunSpeed"); // Float
    private static readonly int IsDeadAnimation = Animator.StringToHash("isDead"); // Bool
    private static readonly int KillLargeAnimation = Animator.StringToHash("killLarge"); // Trigger
    public static readonly int KillSmallAnimation = Animator.StringToHash("killSmall"); // Trigger

    public override void Start()
    {
        base.Start();

        foreach (var enemyType in Resources.FindObjectsOfTypeAll<EnemyType>())
        {
            Plugin.ExtendedLogging($"{enemyType.enemyName} has Size: {CalculateEnemySize(enemyType.enemyPrefab.GetComponent<EnemyAI>())}");
        }
    }

    public override void Update()
    {
        base.Update();

        if (_killingLargeEnemy && targetEnemy != null)
        {
            targetEnemy.transform.position = _enemyHoldingPoint.position;
        }
    }

    #region State Machines
    public override void DoAIInterval()
    {
        base.DoAIInterval();
        if (isEnemyDead || StartOfRound.Instance.allPlayersDead)
            return;

        creatureAnimator.SetFloat(RunSpeedFloat, agent.velocity.magnitude / 3f);
        _bufferTimer -= AIIntervalTime;
        if (_bufferTimer > 0)
            return;

        if (targetEnemy != null && targetEnemy.isEnemyDead)
        {
            targetEnemy = null;
        }

        if (targetEnemy == null)
        {
            HandleWandering();
        }
        else
        {
            _killingLargeEnemy(targetEnemy);
        }
    }

    private void HandleWandering()
    {
        foreach (var enemy in RoundManager.Instance.SpawnedEnemies)
        {
            if (enemy == null || enemy.isEnemyDead || enemy is Guardsman)
                continue;
            
            if (Vector3.Distance(transform.position, enemy.transform.position) > 45f)
                continue;

            if (EnemyHasLineOfSightToPosition(enemy.transform.position, 60, 45, 5))
                continue;

            SetEnemyTargetServerRpc(RoundManager.Instance.SpawnedEnemies.IndexOf(enemy));
            return;
        }
    }

    private void _killingLargeEnemy(EnemyAI _targetEnemy)
    {
        smartAgentNavigator.DoPathingToDestination(_targetEnemy.transform.position);

        if (Vector3.Distance(_targetEnemy.transform.position, this.transform.position) > 5 + agent.stoppingDistance)
            return;

        if (CalculateEnemySize(_targetEnemy) > _enemySizeThreshold)
        {
            creatureNetworkAnimator.SetTrigger(KillLargeAnimation);
        }
        else
        {
            creatureNetworkAnimator.SetTrigger(KillSmallAnimation);
        }
    }

    #endregion

    #region Callbacks

    public override void KillEnemy(bool destroy = false)
    {
        base.KillEnemy(destroy);

        if (!IsServer)
            return;
        
        creatureAnimator.SetBool(IsDeadAnimation, true);
    }

    #endregion
    #region Misc Functions

    private float CalculateEnemySize(EnemyAI enemyAi)
    {
        float agentSize = 3.14159f * enemyAi.agent.radius * enemyAi.agent.radius * enemyAi.agent.height * (enemyAi.transform.localScale.x * enemyAi.transform.localScale.y * enemyAi.transform.localScale.z);
        return agentSize;
    }
    #endregion

    #region Animation Events
    public void StartKillLargeEnemyAnimEvent()
    {
        _killingLargeEnemy = true;
    }

    public void SmashEnemyAnimEvent()
    {
        if (targetEnemy == null)
            return;

        targetEnemy.transform.localScale = new Vector3(targetEnemy.transform.localScale.x, targetEnemy.transform.localScale.y * 0.1f, targetEnemy.transform.localScale.z);

        bool overrideDestroy = !targetEnemy.enemyType.canDie;
        if (targetEnemy.IsOwner)
            targetEnemy.KillEnemyOnOwnerClient(overrideDestroy);

        // idk other stuff
    }

    public void RipApartEnemyAnimEvent()
    {
        _killingLargeEnemy = false;
        if (targetEnemy == null)
            return;

        bool overrideDestroy = !targetEnemy.enemyType.canDie;
        if (targetEnemy.IsOwner)
            targetEnemy.KillEnemyOnOwnerClient(overrideDestroy);

        // idk other stuff like particle effects ig        
    }

    #endregion
}