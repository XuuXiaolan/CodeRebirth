using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.Content.Maps;
using CodeRebirthLib;
using CodeRebirthLib.Utils;



using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace CodeRebirth.src.Content.Enemies;
public class Guardsman : CodeRebirthEnemyAI, IVisibleThreat
{
    [Header("Audio")]
    [SerializeField]
    private AudioClip _slamAudioClip = null!;
    [SerializeField]
    private AudioClip _ripApartAudioClip = null!;
    [SerializeField]
    private AudioClip[] _enemySpottedAudioClips = [];

    [SerializeField]
    private Transform[] _enemyHoldingPoints = [];
    [SerializeField]
    private Transform _spotlightHead = null!;
    [SerializeField]
    private float _enemySizeThreshold = 69;
    [SerializeField]
    private GameObject _dustLandDecal = null!;
    [SerializeField]
    private ParticleSystem _dustParticleSystem = null!;

    private Coroutine? _messWithSizeOverTimeRoutine = null;
    private bool _killingLargeEnemy = false;
    private float _bufferTimer = 0f;
    private List<IHittable> _iHittableList = new();
    private List<EnemyAI> _enemyAIList = new();
    private Collider[] _cachedHits = new Collider[24];
    internal HashSet<string> _internalEnemyBlacklist = new();

    #region IVisibleThreat
    public ThreatType type => ThreatType.RadMech;

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
        return base.transform;
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

    bool IVisibleThreat.IsThreatDead()
    {
        return isEnemyDead;
    }

    GrabbableObject? IVisibleThreat.GetHeldObject()
    {
        return null;
    }
    #endregion

    private static readonly int RunSpeedFloat = Animator.StringToHash("RunSpeed"); // Float
    private static readonly int IsDeadAnimation = Animator.StringToHash("isDead"); // Bool
    private static readonly int KillLargeAnimation = Animator.StringToHash("killLarge"); // Trigger
    public static readonly int KillSmallAnimation = Animator.StringToHash("killSmall"); // Trigger

    public override void Start()
    {
        base.Start();
        var enemyBlacklistArray = MapObjectHandler.Instance.Merchant.GetConfig<string>("Guardsman | Enemy Blacklist").Value.Split(',').Select(s => s.Trim());
        foreach (var nameEntry in enemyBlacklistArray.ToList())
        {
            _internalEnemyBlacklist.UnionWith(LethalContent.Enemies.Values.Where(et => et.EnemyType.enemyName.Equals(nameEntry, StringComparison.OrdinalIgnoreCase)).Select(et => et.EnemyType.enemyName));
        }

        foreach (var nameEntry in _internalEnemyBlacklist)
        {
            Plugin.ExtendedLogging($"Adding {nameEntry} to Guardsman's internal blacklist.");
        }
        StartCoroutine(StartDelay());
        StartCoroutine(ProjectorUnparentDelay());
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
        _idleTimer -= Time.deltaTime;
        if (_idleTimer < 0)
        {
            _idleTimer = enemyRandom.NextFloat(_idleAudioClips.minTime, _idleAudioClips.maxTime);
            creatureVoice.PlayOneShot(_idleAudioClips.audioClips[enemyRandom.Next(_idleAudioClips.audioClips.Length)]);
        }

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
            if (enemy == null || enemy.isEnemyDead || enemy is Guardsman || enemy is SandWormAI)
                continue;

            if (_internalEnemyBlacklist.Contains(enemy.enemyType.enemyName))
                continue;

            if (Vector3.Distance(transform.position, enemy.transform.position) > 45f)
                continue;

            if (EnemyHasLineOfSightToPosition(enemy.transform.position, 60, 45, 5))
                continue;

            SetEnemyTargetServerRpc(new NetworkBehaviourReference(enemy));
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
            _bufferTimer = 7.5f;
            smartAgentNavigator.DisableMovement(true);
            smartAgentNavigator.StopAgent();
            MiscSoundsClientRpc(1);
            creatureNetworkAnimator.SetTrigger(KillLargeAnimation);
        }
        else
        {
            _bufferTimer = 7.5f;
            smartAgentNavigator.DisableMovement(true);
            smartAgentNavigator.StopAgent();
            MiscSoundsClientRpc(0);
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

    public override void EnemySetAsTarget(EnemyAI? enemy)
    {
        base.EnemySetAsTarget(enemy);
        creatureVoice.PlayOneShot(_enemySpottedAudioClips[enemyRandom.Next(_enemySpottedAudioClips.Length)]);
    }
    #endregion
    #region Misc Functions

    [ClientRpc]
    public void MiscSoundsClientRpc(int soundID)
    {
        switch (soundID)
        {
            case 0:
                creatureSFX.PlayOneShot(_slamAudioClip);
                break;
            case 1:
                creatureSFX.PlayOneShot(_ripApartAudioClip);
                break;
        }
    }

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
        smartAgentNavigator.DisableMovement(true);
        smartAgentNavigator.StopAgent();
        yield return new WaitForSeconds(10f);
        skinnedMeshRenderers[0].updateWhenOffscreen = false;
        smartAgentNavigator.DisableMovement(false);
        smartAgentNavigator.StartSearchRoutine(100f);
    }

    private IEnumerator ProjectorUnparentDelay()
    {
        yield return new WaitUntil(() => _dustLandDecal.activeSelf);
        Vector3 previousPosition = _dustLandDecal.transform.position;
        _dustLandDecal.transform.SetParent(RoundManager.Instance.mapPropsContainer.transform);
        _dustLandDecal.transform.position = previousPosition;
        _dustLandDecal.gameObject.SetActive(true);
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
        smartAgentNavigator.DisableMovement(false);
        smartAgentNavigator.StopAgent();

        if (targetEnemy != null)
            yield break;

        smartAgentNavigator.StartSearchRoutine(100f);
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
        _dustParticleSystem.Play();

        Vector3 hitPosition = (_enemyHoldingPoints[0].position + _enemyHoldingPoints[1].position) / 2;
        int numHits = Physics.OverlapSphereNonAlloc(hitPosition, 6f, _cachedHits, MoreLayerMasks.PlayersAndInteractableAndEnemiesAndPropsHazardMask, QueryTriggerInteraction.Collide);

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
    }

    public void RipApartEnemyAnimEvent()
    {
        StartCoroutine(StartSearchRoutineWithDelay());
        _killingLargeEnemy = false;
        if (targetEnemy == null)
            return;

        if (targetEnemy.IsOwner)
            targetEnemy.KillEnemyOnOwnerClient(true);
    }

    #endregion
}