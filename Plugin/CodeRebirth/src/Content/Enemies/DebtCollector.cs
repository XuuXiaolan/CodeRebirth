using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.MiscScripts;
using Dawn.Utils;
using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.Content.Enemies;

public class DebtCollector : CodeRebirthEnemyAI
{
    [field: SerializeField]
    public AudioSource TreadSource { get; private set; }

    [field: SerializeField]
    public BoundedRange TeleportIdleTimerRange { get; private set; } = new(5f, 10f);

    private List<Material> _treadMaterials = new();
    private Vector3 _lastPosition = Vector3.zero;
    private float _teleportIdleTimer = 3f;
    public enum DebtCollectorState
    {
        Spawning,
        Idle,
        Teleporting,
        ChasingTargetPlayer,
        Death,
    }

    private static readonly int RunSpeedAnimationHash = Animator.StringToHash("RunSpeed"); // Float
    private static readonly int SliceAnimationHash = Animator.StringToHash("Slice"); // Trigger
    private static readonly int GrabAnimationHash = Animator.StringToHash("Grab"); // Trigger
    private static readonly int SuccessAnimationHash = Animator.StringToHash("Success"); // Trigger
    private static readonly int FailAnimationHash = Animator.StringToHash("Fail"); // Trigger
    private static readonly int PryOpenAnimationHash = Animator.StringToHash("PryOpen"); // Trigger
    private static readonly int TeleportAnimationHash = Animator.StringToHash("Teleport"); // Trigger
    private static readonly int IsDeadAnimationHash = Animator.StringToHash("IsDead"); // Bool

    #region Unity Lifecycles
    public override void Start()
    {
        base.Start();
        SwitchToBehaviourStateOnLocalClient((int)DebtCollectorState.Spawning);
        _lastPosition = this.transform.position;
        _treadMaterials.Add(skinnedMeshRenderers[0].materials[2]); // Left Tread
        _treadMaterials.Add(skinnedMeshRenderers[0].materials[3]); // Right Tread

        if (!IsServer)
        {
            return;
        }

        FindRandomPlayerViaAsyncPathfinding();
    }

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
            SwitchToBehaviourServerRpc((int)DebtCollectorState.ChasingTargetPlayer);
        }
        else
        {
            SwitchToBehaviourServerRpc((int)DebtCollectorState.Idle);
            smartAgentNavigator.StartSearchRoutine(20);
            Plugin.ExtendedLogging($"DebtCollector: Going idle temporarily");
        }
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
        _teleportIdleTimer -= AIIntervalTime;
        if (_teleportIdleTimer <= 0f)
        {
            smartAgentNavigator.StopSearchRoutine();
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
            SwitchToBehaviourServerRpc((int)DebtCollectorState.Idle);
            return;
        }

        smartAgentNavigator.DoPathingToDestination(targetPlayer.transform.position);
    }

    private void DoDeath()
    {

    }
    #endregion

    #region  Misc Functions
    #endregion

    #region Animation Events

    public void TeleportSomewhereRandom()
    {
        if (!IsServer)
        {
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
        if (currentBehaviourStateIndex == (int)DebtCollectorState.Spawning || currentBehaviourStateIndex == (int)DebtCollectorState.Idle)
        {
            FindRandomPlayerViaAsyncPathfinding();
        }
    }
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
        creatureAnimator.SetBool(IsDeadAnimationHash, true);
        SwitchToBehaviourStateOnLocalClient((int)DebtCollectorState.Death);
    }
    #endregion
}