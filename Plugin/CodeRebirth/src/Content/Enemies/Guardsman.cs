using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src;
using CodeRebirth.src.Content.Enemies;
using CodeRebirth.src.Util;
using UnityEngine;
using UnityEngine.AI;

public class Guardsman : CodeRebirthEnemyAI
{
    [SerializeField]
    private Transform[] _enemyHoldingPoints = [];

    [SerializeField]
    private Transform _spotlightHead = null!;

    [SerializeField]
    private float _enemySizeThreshold = 69;

    private Coroutine? _messWithSizeOverTimeRoutine = null;
    private bool _killingLargeEnemy = false;
    private float _bufferTimer = 0f;
    private List<IHittable> _iHittableList = new();
    private List<EnemyAI> _enemyAIList = new();
    private Collider[] _cachedHits = new Collider[24];

    private static readonly int RunSpeedFloat = Animator.StringToHash("RunSpeed"); // Float
    private static readonly int IsDeadAnimation = Animator.StringToHash("isDead"); // Bool
    private static readonly int KillLargeAnimation = Animator.StringToHash("killLarge"); // Trigger
    public static readonly int KillSmallAnimation = Animator.StringToHash("killSmall"); // Trigger

    public override void Start()
    {
        base.Start();
        StartCoroutine(StartDelay());
        foreach (var enemyType in Resources.FindObjectsOfTypeAll<EnemyType>())
        {
            if (enemyType.enemyPrefab == null || enemyType.enemyPrefab.GetComponent<EnemyAI>() == null)
                continue;

            Plugin.ExtendedLogging($"{enemyType.enemyName} has Size: {CalculateEnemySize(enemyType.enemyPrefab.GetComponent<EnemyAI>())}");
        }
    }

    public override void Update()
    {
        base.Update();

        if (_killingLargeEnemy && targetEnemy != null)
        {
            Vector3 holdingPoint = (_enemyHoldingPoints[0].position + _enemyHoldingPoints[1].position) / 2;
            targetEnemy.transform.position = holdingPoint;
            targetEnemy.transform.LookAt(_spotlightHead);
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
            KillEnemy(targetEnemy);
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
            smartAgentNavigator.StopSearchRoutine();
            return;
        }
    }

    private void KillEnemy(EnemyAI _targetEnemy)
    {
        smartAgentNavigator.DoPathingToDestination(_targetEnemy.transform.position);

        if (Vector3.Distance(_targetEnemy.transform.position, this.transform.position) > 5 + agent.stoppingDistance)
            return;

        if (CalculateEnemySize(_targetEnemy) > _enemySizeThreshold)
        {
            // force enemies to stop moving
            Plugin.ExtendedLogging($"Killing Large Enemy: {_targetEnemy.enemyType.enemyName} with Size: {CalculateEnemySize(_targetEnemy)}");
            smartAgentNavigator.cantMove = true;
            creatureNetworkAnimator.SetTrigger(KillLargeAnimation);
            _bufferTimer += 10f;
        }
        else
        {
            Plugin.ExtendedLogging($"Killing Small Enemy: {_targetEnemy.enemyType.enemyName} with Size: {CalculateEnemySize(_targetEnemy)}");
            smartAgentNavigator.cantMove = true;
            creatureNetworkAnimator.SetTrigger(KillSmallAnimation);
            _bufferTimer += 10f;
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

    private IEnumerator MessWithSizeOverTime(int sizeMultiplier)
    {
        while (true)
        {
            if (targetEnemy == null)
            {
                StopCoroutine(_messWithSizeOverTimeRoutine);
                yield break;
            }
            targetEnemy.transform.localScale = new Vector3(targetEnemy.transform.localScale.x, targetEnemy.transform.localScale.y, targetEnemy.transform.localScale.z) + new Vector3(sizeMultiplier * Time.deltaTime, 0, 0);
            yield return null;
        }
    }

    private IEnumerator StartDelay()
    {
        smartAgentNavigator.cantMove = true;
        yield return new WaitForSeconds(10f);
        smartAgentNavigator.cantMove = false;
        smartAgentNavigator.StartSearchRoutine(this.transform.position, 100f);
    }

    private float CalculateEnemySize(EnemyAI enemyAi)
    {
        NavMeshAgent agent = enemyAi.gameObject.GetComponent<NavMeshAgent>();
        if (agent == null)
            return 10f;
        float agentSize = 3.14159f * agent.radius * agent.radius * agent.height * (enemyAi.transform.localScale.x * enemyAi.transform.localScale.y * enemyAi.transform.localScale.z);
        return agentSize;
    }

    private IEnumerator StartSearchRoutineWithDelay()
    {
        yield return new WaitForSeconds(3f);
        smartAgentNavigator.cantMove = false;
        if (targetEnemy == null)
            yield break;

        smartAgentNavigator.StartSearchRoutine(this.transform.position, 100f);
    }

    #endregion

    #region Animation Events
    public void MessWithSizeAnimEvent(int sizeMultiplier)
    {
        if (_messWithSizeOverTimeRoutine != null)
            StopCoroutine(_messWithSizeOverTimeRoutine);

        _messWithSizeOverTimeRoutine = StartCoroutine(MessWithSizeOverTime(sizeMultiplier));
    }

    public void StartKillLargeEnemyAnimEvent()
    {
        _killingLargeEnemy = true;
    }

    public void SmashEnemyAnimEvent()
    {
        StartCoroutine(StartSearchRoutineWithDelay());

        Vector3 hitPosition = (_enemyHoldingPoints[0].position + _enemyHoldingPoints[1].position) / 2;
        int numHits = Physics.OverlapSphereNonAlloc(hitPosition, 3f, _cachedHits, CodeRebirthUtils.Instance.playersAndInteractableAndEnemiesAndPropsHazardMask, QueryTriggerInteraction.Collide);

        _iHittableList.Clear();
        _enemyAIList.Clear();

        for (int i = 0; i < numHits; i++)
        {
            if (!_cachedHits[i].TryGetComponent(out IHittable iHittable))
                continue;

            if (iHittable is EnemyAICollisionDetect enemyAICollisionDetect)
            {
                if (enemyAICollisionDetect.mainScript is Guardsman)
                    continue;

                if (_enemyAIList.Contains(enemyAICollisionDetect.mainScript))
                    continue;

                _enemyAIList.Add(enemyAICollisionDetect.mainScript);
            }
            _iHittableList.Add(iHittable);
        }

        foreach (var iHittable in _iHittableList)
        {
            iHittable.Hit(99, hitPosition, null, true, -1);
        }

        foreach (var enemy in _enemyAIList)
        {
            enemy.gameObject.transform.localScale = new Vector3(enemy.transform.localScale.x, enemy.transform.localScale.y * 0.1f, enemy.transform.localScale.z);

            bool overrideDestroy = !enemy.enemyType.canDie;
            if (enemy.IsOwner)
                enemy.KillEnemyOnOwnerClient(overrideDestroy);
        }



        // idk other stuff
    }

    public void RipApartEnemyAnimEvent()
    {
        StartCoroutine(StartSearchRoutineWithDelay());
        _killingLargeEnemy = false;
        if (targetEnemy == null)
            return;

        if (targetEnemy.IsOwner)
            targetEnemy.KillEnemyOnOwnerClient(true);

        // idk other stuff like particle effects ig        
    }

    #endregion
}