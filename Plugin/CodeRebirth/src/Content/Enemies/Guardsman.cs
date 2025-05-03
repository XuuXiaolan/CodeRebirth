using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src;
using CodeRebirth.src.Content.Enemies;
using CodeRebirth.src.Content.Maps;
using CodeRebirth.src.MiscScripts.ConfigManager;
using CodeRebirth.src.Util;
using CodeRebirth.src.Util.AssetLoading;
using CodeRebirth.src.Util.Extensions;
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

    [SerializeField]
    private ParticleSystem _dustParticleSystem = null!;

    private Coroutine? _messWithSizeOverTimeRoutine = null;
    private bool _killingLargeEnemy = false;
    private float _bufferTimer = 0f;
    private List<IHittable> _iHittableList = new();
    private List<EnemyAI> _enemyAIList = new();
    private Collider[] _cachedHits = new Collider[24];
    private HashSet<EnemyType> _internalEnemyBlacklist = new();

    private static readonly int RunSpeedFloat = Animator.StringToHash("RunSpeed"); // Float
    private static readonly int IsDeadAnimation = Animator.StringToHash("isDead"); // Bool
    private static readonly int KillLargeAnimation = Animator.StringToHash("killLarge"); // Trigger
    public static readonly int KillSmallAnimation = Animator.StringToHash("killSmall"); // Trigger

    public override void Start()
    {
        base.Start();
        List<CRDynamicConfig> configDefinitions = MapObjectHandler.Instance.Merchant!.EnemyDefinitions.GetCREnemyDefinitionWithEnemyName(enemyType.enemyName)!.ConfigEntries;
        CRDynamicConfig? configSetting = configDefinitions.GetCRDynamicConfigWithSetting("Guardsman", "Enemy Blacklist");
        if (configSetting != null)
        {
            var enemyBlacklistArray = CRConfigManager.GetGeneralConfigEntry<string>(configSetting.settingName, configSetting.settingDesc).Value.Split(',').Select(s => s.Trim());
            foreach (var nameEntry in enemyBlacklistArray)
            {
                _internalEnemyBlacklist.UnionWith(CodeRebirthUtils.EnemyTypes.Where(et => et.enemyName.Equals(nameEntry, StringComparison.OrdinalIgnoreCase)));
            }
        }

        foreach (var nameEntry in _internalEnemyBlacklist)
        {
            Plugin.ExtendedLogging($"Adding {nameEntry} to Guardsman's internal blacklist.");
        }
        StartCoroutine(StartDelay());
        /*foreach (var enemyType in Resources.FindObjectsOfTypeAll<EnemyType>())
        {
            if (enemyType.enemyPrefab == null || enemyType.enemyPrefab.GetComponent<EnemyAI>() == null)
                continue;

            Plugin.ExtendedLogging($"{enemyType.enemyName} has Size: {CalculateEnemySize(enemyType.enemyPrefab.GetComponent<EnemyAI>())}");
        }*/
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

        if (_bufferTimer > 0 && targetEnemy != null)
        {
            Vector3 direction = targetEnemy.transform.position - transform.position;
            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, 5 * Time.deltaTime);
        }
    }

    #region State Machines
    public override void DoAIInterval()
    {
        base.DoAIInterval();
        if (isEnemyDead || StartOfRound.Instance.allPlayersDead)
            return;

        creatureAnimator.SetFloat(RunSpeedFloat, agent.velocity.magnitude / 3f);

        if (targetEnemy != null && targetEnemy.isEnemyDead)
        {
            targetEnemy = null;
        }

        _bufferTimer -= AIIntervalTime;
        if (_bufferTimer > 0)
            return;

        if (targetEnemy == null)
        {
            HandleWandering();
        }
        else
        {
            KillLargeOrSmallEnemy(targetEnemy);
        }
    }

    private void HandleWandering()
    {
        foreach (var enemy in RoundManager.Instance.SpawnedEnemies)
        {
            if (enemy == null || enemy.isEnemyDead || enemy is Guardsman)
                continue;

            if (_internalEnemyBlacklist.Contains(enemy.enemyType))
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

    private void KillLargeOrSmallEnemy(EnemyAI _targetEnemy)
    {
        smartAgentNavigator.DoPathingToDestination(_targetEnemy.transform.position);

        if (Vector3.Distance(_targetEnemy.transform.position, this.transform.position) > 12.5f + agent.stoppingDistance)
            return;

        if (CalculateEnemySize(_targetEnemy) > _enemySizeThreshold)
        {
            // force enemies to stop moving
            Plugin.ExtendedLogging($"Killing Large Enemy: {_targetEnemy.enemyType.enemyName} with Size: {CalculateEnemySize(_targetEnemy)}");
            _bufferTimer = 7.5f;
            smartAgentNavigator.cantMove = true;
            smartAgentNavigator.StopAgent();
            creatureNetworkAnimator.SetTrigger(KillLargeAnimation);
        }
        else
        {
            Plugin.ExtendedLogging($"Killing Small Enemy: {_targetEnemy.enemyType.enemyName} with Size: {CalculateEnemySize(_targetEnemy)}");
            _bufferTimer = 7.5f;
            smartAgentNavigator.cantMove = true;
            smartAgentNavigator.StopAgent();
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
        smartAgentNavigator.StopAgent();
        yield return new WaitForSeconds(10f);
        skinnedMeshRenderers[0].updateWhenOffscreen = false;
        smartAgentNavigator.cantMove = false;
        smartAgentNavigator.StartSearchRoutine(100f);
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
        smartAgentNavigator.StopAgent();

        if (targetEnemy != null)
            yield break;

        smartAgentNavigator.StartSearchRoutine(100f);
    }

    #endregion

    #region Animation Events
    public void ScreenShakeAnimEvent()
    {
        if (Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, transform.position) <= 50f)
            HUDManager.Instance.ShakeCamera(ScreenShakeType.Long);
    }

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
        _dustParticleSystem.Play();

        Vector3 hitPosition = (_enemyHoldingPoints[0].position + _enemyHoldingPoints[1].position) / 2;
        int numHits = Physics.OverlapSphereNonAlloc(hitPosition, 6f, _cachedHits, CodeRebirthUtils.Instance.playersAndInteractableAndEnemiesAndPropsHazardMask, QueryTriggerInteraction.Collide);

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