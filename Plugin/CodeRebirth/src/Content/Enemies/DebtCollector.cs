using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.MiscScripts;
using Dawn.Utils;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Enemies;

public class DebtCollector : CodeRebirthEnemyAI
{
    [field: SerializeField]
    public float WanderingSpeed { get; private set; } = 3.5f;
    [field: SerializeField]
    public float ChasingSpeed { get; private set; } = 5f;
    [field: SerializeField]
    public float AttackingSpeed { get; private set; } = 2f;
    [field: SerializeField]
    public Transform SliceArmStart { get; private set; }
    [field: SerializeField]
    public Transform SliceArmEnd { get; private set; }

    [field: SerializeField]
    public Transform GrabHand { get; private set; }

    [field: SerializeField]
    public BoundedRange GrabAttackTimer { get; private set; } = new(10f, 15f);

    [field: SerializeField, Range(0f, 100f)]
    public float ChanceToGoForGrabAttack { get; private set; } = 50f;

    [field: SerializeField]
    public AudioSource TreadSource { get; private set; }

    [field: SerializeField]
    public BoundedRange TeleportIdleTimerRange { get; private set; } = new(5f, 10f);

    private List<Material> _treadMaterials = new();
    private Vector3 _lastPosition = Vector3.zero;
    private float _teleportIdleTimer = 3f;
    private float _checkForPlayersTimer = 1.5f;
    private float _lostPlayerTimer = 2f;
    private float _grabAttackTimer = 10f;
    private bool _playerIsGrabbed = false;
    private static Collider[] _cachedColliders = new Collider[24];

    public enum DebtCollectorState
    {
        Spawning,
        Idle,
        Teleporting,
        ChasingTargetPlayer,
        AttackingPlayer,
        Death,
    }

    private static readonly int RunSpeedAnimationHash = Animator.StringToHash("RunSpeed"); // Float
    private static readonly int SliceAnimationHash = Animator.StringToHash("Slice"); // Trigger
    private static readonly int GrabAnimationHash = Animator.StringToHash("Grab"); // Trigger
    private static readonly int SuccessAnimationHash = Animator.StringToHash("Success"); // Trigger
    private static readonly int PryOpenAnimationHash = Animator.StringToHash("PryOpen"); // Trigger
    private static readonly int TeleportAnimationHash = Animator.StringToHash("Teleport"); // Trigger
    private static readonly int IsDeadAnimationHash = Animator.StringToHash("IsDead"); // Bool

    #region Unity Lifecycles
    public override void Start()
    {
        base.Start();
        agent.speed = WanderingSpeed;
        SwitchToBehaviourStateOnLocalClient((int)DebtCollectorState.Spawning);
        _lastPosition = this.transform.position;
        _treadMaterials.Add(skinnedMeshRenderers[1].materials[2]); // Left Tread
        _treadMaterials.Add(skinnedMeshRenderers[1].materials[3]); // Right Tread

        if (!IsServer)
        {
            return;
        }

        FindRandomPlayerViaAsyncPathfinding();
    }

    public override void Update()
    {
        base.Update();

        float velocity = (_lastPosition - this.transform.position).magnitude;
        _treadMaterials[0].SetVector(PeaceKeeper.ScrollSpeedID, new Vector3(0, -velocity, 0)); // Left Tread
        _treadMaterials[1].SetVector(PeaceKeeper.ScrollSpeedID, new Vector3(0, velocity, 0)); // Right Tread
        if (velocity > 0)
        {
            TreadSource.volume = 1f;
        }
        else
        {
            TreadSource.volume = 0f;
        }
        _lastPosition = this.transform.position;

        if (!_playerIsGrabbed && targetPlayer != null && currentBehaviourStateIndex == (int)DebtCollectorState.AttackingPlayer)
        {
            RotateToPlayer(targetPlayer);
        }
    }

    public void LateUpdate()
    {
        if (targetPlayer == null)
        {
            return;
        }

        if (!_playerIsGrabbed)
        {
            return;
        }

        targetPlayer.transform.position = GrabHand.position + Vector3.down * 2.43f;
        Quaternion baseRotation = GrabHand.rotation * Quaternion.Euler(0, -180f, 0);

        float baseYaw = baseRotation.eulerAngles.y;
        float currentYaw = targetPlayer.transform.eulerAngles.y;

        float clampedOffset = Mathf.Clamp(
            Mathf.DeltaAngle(baseYaw, currentYaw),
            -90f,
            90f
        );

        targetPlayer.transform.rotation = Quaternion.Euler(0, baseYaw + clampedOffset, 0);
    }
    #endregion

    #region StateMachines
    public override void DoAIInterval()
    {
        base.DoAIInterval();
        if (StartOfRound.Instance.allPlayersDead || isEnemyDead || smartAgentNavigator.CheckPathsOngoing())
        {
            return;
        }

        float velocity = agent.velocity.magnitude / 3;
        creatureAnimator.SetFloat(RunSpeedAnimationHash, velocity);

        switch (currentBehaviourStateIndex)
        {
            case (int)DebtCollectorState.Spawning:
                DoSpawning();
                break;
            case (int)DebtCollectorState.Idle:
                DoIdle();
                break;
            case (int)DebtCollectorState.Teleporting:
                DoTeleporting();
                break;
            case (int)DebtCollectorState.ChasingTargetPlayer:
                DoChasingTargetPlayer();
                break;
            case (int)DebtCollectorState.AttackingPlayer:
                DoAttackingPlayer();
                break;
            case (int)DebtCollectorState.Death:
                DoDeath();
                break;
        }
    }

    private void DoSpawning()
    {

    }

    private void DoIdle()
    {
        _checkForPlayersTimer -= AIIntervalTime;
        if (_checkForPlayersTimer <= 0f)
        {
            _checkForPlayersTimer = TeleportIdleTimerRange.GetRandomInRange(new System.Random(UnityEngine.Random.Range(0, 999999))) / 4f;
            FindRandomPlayerViaAsyncPathfinding();
            return;
        }

        _teleportIdleTimer -= AIIntervalTime;
        if (_teleportIdleTimer <= 0f)
        {
            smartAgentNavigator.StopSearchRoutine();
            agent.speed = 0f;
            SwitchToBehaviourServerRpc((int)DebtCollectorState.Teleporting);
            creatureNetworkAnimator.SetTrigger(TeleportAnimationHash);
            _teleportIdleTimer = TeleportIdleTimerRange.GetRandomInRange(new System.Random(UnityEngine.Random.Range(0, 999999)));
        }
    }

    private void DoTeleporting()
    {

    }

    private void DoChasingTargetPlayer()
    {
        if (targetPlayer == null || targetPlayer.isPlayerDead)
        {
            smartAgentNavigator.StartSearchRoutine(20f);
            ClearPlayerTargetServerRpc();
            agent.speed = WanderingSpeed;
            SwitchToBehaviourServerRpc((int)DebtCollectorState.Idle);
            return;
        }

        smartAgentNavigator.TryDoPathingToDestination(targetPlayer.transform.position, out SmartAgentNavigator.GoToDestinationResult result);

        if (result == SmartAgentNavigator.GoToDestinationResult.Failure)
        {
            _lostPlayerTimer -= AIIntervalTime;
            if (_lostPlayerTimer <= 0f)
            {
                _lostPlayerTimer = 2f;
                agent.speed = 0f;
                SwitchToBehaviourServerRpc((int)DebtCollectorState.Teleporting);
                creatureNetworkAnimator.SetTrigger(TeleportAnimationHash);
                return;
            }
        }
        else
        {
            _lostPlayerTimer = Mathf.Min(_lostPlayerTimer + AIIntervalTime, 2f);
        }

        _grabAttackTimer -= AIIntervalTime;
        if (Vector3.Distance(this.transform.position, targetPlayer.transform.position) < agent.stoppingDistance + 0.5f)
        {
            agent.speed = AttackingSpeed;
            SwitchToBehaviourServerRpc((int)DebtCollectorState.AttackingPlayer);
            if (_grabAttackTimer <= 0f && UnityEngine.Random.Range(0f, 100f) < ChanceToGoForGrabAttack)
            {
                _grabAttackTimer = GrabAttackTimer.GetRandomInRange(new System.Random(UnityEngine.Random.Range(0, 999999)));
                agent.speed = AttackingSpeed * 1.5f;
                agent.acceleration = 100f;
                creatureNetworkAnimator.SetTrigger(GrabAnimationHash);
                return;
            }
            else
            {
                creatureNetworkAnimator.SetTrigger(SliceAnimationHash);
                // go for a slice attack
            }
        }
    }

    private void DoAttackingPlayer()
    {
        if (targetPlayer == null || targetPlayer.isPlayerDead)
        {
            return;
        }

        smartAgentNavigator.DoPathingToDestination(targetPlayer.transform.position);
    }

    private void DoDeath()
    {

    }
    #endregion

    #region  Misc Functions

    private void FindRandomPlayerViaAsyncPathfinding()
    {
        IEnumerable<(PlayerControllerB player, Vector3 position)> candidateObjects = StartOfRound.Instance.allPlayerScripts
            .Where(kv => kv != null && !kv.isPlayerDead && kv.isPlayerControlled)
            .Select(kv => (kv, kv.transform.position));

        smartAgentNavigator.CheckPaths(candidateObjects, CheckIfNeedToChangeState);
    }

    public void CheckIfNeedToChangeState(List<GenericPath<PlayerControllerB>> args)
    {
        int totalAmount = args.Count;
        if (totalAmount > 0)
        {
            Plugin.ExtendedLogging($"DebtCollector: Found {totalAmount} targets");
            SetPlayerTargetServerRpc(args[UnityEngine.Random.Range(0, totalAmount)].Generic);
            smartAgentNavigator.StopSearchRoutine();
            agent.speed = ChasingSpeed;
            SwitchToBehaviourServerRpc((int)DebtCollectorState.ChasingTargetPlayer);
        }
        else
        {
            agent.speed = WanderingSpeed;
            SwitchToBehaviourServerRpc((int)DebtCollectorState.Idle);
            smartAgentNavigator.StartSearchRoutine(20);
            Plugin.ExtendedLogging($"DebtCollector: Going idle temporarily");
        }
    }

    private void RotateToPlayer(PlayerControllerB player)
    {
        Vector3 direction = player.transform.position - transform.position;
        direction.y = 0;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, 5 * Time.deltaTime);
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false, DeferLocal = true)]
    private void SetPlayerAsGrabbedRpc()
    {
        _playerIsGrabbed = true;
        targetPlayer.disableMoveInput = true;
        targetPlayer.inAnimationWithEnemy = this;
        creatureAnimator.SetTrigger(SuccessAnimationHash);
    }
    #endregion

    #region Animation Events

    public void TeleportSomewhereRandom()
    {
        if (!IsServer)
        {
            return;
        }

        if (targetPlayer != null && !targetPlayer.isPlayerDead)
        {
            CRUtilities.TeleportEnemy(this, RoundManager.Instance.GetRandomNavMeshPositionInRadius(targetPlayer.transform.position, 20f, default));
            agent.speed = ChasingSpeed;
            SwitchToBehaviourServerRpc((int)DebtCollectorState.ChasingTargetPlayer);
            return;
        }

        List<Vector3> randomPositions = new();
        if (RoundManager.Instance.insideAINodes != null && RoundManager.Instance.insideAINodes.Length > 0)
        {
            randomPositions.AddRange(RoundManager.Instance.insideAINodes.Select(node => node.transform.position));
        }
        else if (RoundManager.Instance.outsideAINodes != null && RoundManager.Instance.outsideAINodes.Length > 0)
        {
            randomPositions.AddRange(RoundManager.Instance.outsideAINodes.Select(node => node.transform.position));
        }

        if (randomPositions.Count == 0)
        {
            Plugin.ExtendedLogging($"DebtCollector: No nodes to teleport to");
            return;
        }

        CRUtilities.TeleportEnemy(this, randomPositions[UnityEngine.Random.Range(0, randomPositions.Count)]);
        FindRandomPlayerViaAsyncPathfinding();
    }

    public void TryGrabPlayer()
    {
        if (targetPlayer != null && !targetPlayer.isPlayerDead && targetPlayer.IsLocalPlayer())
        {
            if (Physics.Raycast(GrabHand.position, targetPlayer.transform.position - GrabHand.position, out RaycastHit hit, 5f, StartOfRound.Instance.playersMask, QueryTriggerInteraction.Collide))
            {
                if (Vector3.Distance(hit.collider.ClosestPoint(GrabHand.position), GrabHand.position) < 2f)
                {
                    GameObject.Find("Systems/Rendering/PlayerHUDHelmetModel").SetActive(false);
                    targetPlayer.DamagePlayer(targetPlayer.health - 1, true, true);
                    SetPlayerAsGrabbedRpc();
                }
            }
        }
    }

    public void KillTargetPlayer()
    {
        if (targetPlayer != null && !targetPlayer.isPlayerDead)
        {
            if (targetPlayer.IsLocalPlayer())
            {
                GameObject.Find("Systems/Rendering/PlayerHUDHelmetModel").SetActive(true);
            }
            targetPlayer.disableMoveInput = false;
            targetPlayer.inAnimationWithEnemy = null;
            targetPlayer.KillPlayer(Vector3.zero, true, CauseOfDeath.Snipped, 8, default);
            ClearPlayerTarget();
        }
        _playerIsGrabbed = false;
    }

    public void EndAttackAnimation()
    {
        Plugin.ExtendedLogging($"DebtCollector: EndAttackAnimation");
        _playerIsGrabbed = false;
        agent.speed = ChasingSpeed;
        agent.acceleration = 16f;
        SwitchToBehaviourStateOnLocalClient((int)DebtCollectorState.ChasingTargetPlayer);
    }

    public void DoSlashAttack()
    {
        int numHits = Physics.OverlapCapsuleNonAlloc(SliceArmStart.position, SliceArmEnd.position, 2f, _cachedColliders, MoreLayerMasks.PlayersAndInteractableAndEnemiesAndPropsHazardMask, QueryTriggerInteraction.Collide);
        for (int i = 0; i < numHits; i++)
        {
            Collider collider = _cachedColliders[i];
            Plugin.ExtendedLogging($"Bitch Slap hit {collider.name}");
            if (!collider.TryGetComponent(out IHittable iHittable))
                continue;

            if (iHittable is PlayerControllerB player)
            {
                Vector3 directionVector = (player.transform.position - this.transform.position).normalized * 20f;
                player.DamagePlayer(40, true, true, CauseOfDeath.Bludgeoning, 0, false, directionVector);
                player.externalForceAutoFade += directionVector;
            }
            else if (iHittable is EnemyAICollisionDetect enemyAICollisionDetect)
            {
                if (!IsServer)
                    continue;

                if (enemyAICollisionDetect.mainScript.gameObject == gameObject)
                    continue;

                enemyAICollisionDetect.mainScript.HitEnemyOnLocalClient(2, this.transform.position, null, true, 1921);
            }
            else
            {
                if (!IsServer)
                    continue;

                iHittable.Hit(2, this.transform.position, null, true, 1921);
            }
        }
    }
    #endregion

    #region Call Backs
    public override void HitEnemy(int force = 1, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
        if (isEnemyDead)
        {
            return;
        }

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
        creatureAnimator.SetBool(IsDeadAnimationHash, true);
        agent.speed = 0f;
        SwitchToBehaviourStateOnLocalClient((int)DebtCollectorState.Death);
    }
    #endregion
}