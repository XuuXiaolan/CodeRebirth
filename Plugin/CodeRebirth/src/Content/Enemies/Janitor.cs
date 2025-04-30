using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.Util;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace CodeRebirth.src.Content.Enemies;
public class Janitor : CodeRebirthEnemyAI
{
    #region Fields & Properties
    [Header("References & Transforms")]
    public GameObject sirenLights = null!;
    public Transform handTransform = null!;
    public Transform playerBoneTransform = null!;
    public Transform placeToHideScrap = null!;
    public GameObject[] headLights = [];

    [Header("Audio & Sounds")]
    public AudioClip spawnSound = null!;
    public AudioClip[] deathSounds = [];
    public AudioClip[] postDeathSounds = [];
    public AudioClip[] idleSounds = [];
    public AudioClip[] detectItemDroppedSounds = [];
    public AudioClip[] grabPlayerSounds = [];
    public AudioClip[] throwPlayerSounds = [];
    public Material[] variantMaterials = [];

    private Collider[] _hitColliders = new Collider[12];
    private float _idleTimer = 60f;

    public enum JanitorStates
    {
        Idle,
        StoringScrap,
        FollowingPlayer,
        ZoomingOff,
        Dead
    }

    public static List<Janitor> janitors = new();

    private Vector3[] _pathCorners = [];
    private int _currentCornerIndex = 0;
    private bool _isRotating = false;
    private TrashCan? _targetTrashCan = null;
    private GrabbableObject? _targetScrap = null;
    private List<GrabbableObject?> _storedScrap = new();

    [HideInInspector]
    public static List<TrashCan> trashCans = new();
    [HideInInspector]
    public bool currentlyGrabbingScrap = false;
    [HideInInspector]
    public bool currentlyGrabbingPlayer = false;
    [HideInInspector]
    public bool currentlyThrowingPlayer = false;

    private static readonly int RightTreadFloat = Animator.StringToHash("RightTreadFloat");
    private static readonly int LeftTreadFloat = Animator.StringToHash("LeftTreadFloat");
    private static readonly int IsAngryAnimation = Animator.StringToHash("isAngry");
    private static readonly int HoldingPlayerAnimation = Animator.StringToHash("holdingPlayer");
    private static readonly int IsDeadAnimation = Animator.StringToHash("isDead");
    private static readonly int GrabScrapAnimation = Animator.StringToHash("grabScrap");
    private static readonly int BreakMovementAnimation = Animator.StringToHash("break");
    private static readonly int ThrowPlayerAnimation = Animator.StringToHash("throwPlayer");
    #endregion

    #region Unity Lifecycle
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        janitors.Add(this);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        janitors.Remove(this);
    }

    public override void Start()
    {
        base.Start();
        ApplyMaterialVariant();

        creatureVoice.PlayOneShot(spawnSound);
        SwitchToBehaviourStateOnLocalClient((int)JanitorStates.Idle);

        if (!IsServer)
            return;

        StartCoroutine(CheckForScrapNearby());
    }

    public override void Update()
    {
        base.Update();

        if (currentBehaviourStateIndex == (int)JanitorStates.Idle)
        {
            HandleIdleSoundTimer();
        }

        if (!IsServer)
            return;

        if (!_isRotating)
            return;

        HandleRotation();
    }

    public void LateUpdate()
    {
        if (currentBehaviourStateIndex == (int)JanitorStates.ZoomingOff)
        {
            KeepPlayerAttachedDuringZoom();
        }
    }
    #endregion

    #region Coroutines
    private IEnumerator CheckForScrapNearby()
    {
        while (true)
        {
            yield return new WaitUntil(() => currentBehaviourStateIndex == (int)JanitorStates.Idle && _targetScrap == null);
            yield return null;
            TryFindScrapNearby();
        }
    }

    public IEnumerator WaitUntilNotDoingAnythingCurrently(PlayerControllerB playerWhoHit)
    {
        yield return new WaitUntil(() => !currentlyGrabbingScrap && !currentlyGrabbingPlayer && !currentlyThrowingPlayer);
        yield return null;
        DetectDroppedScrapServerRpc(playerWhoHit.transform.position, Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerWhoHit));
    }
    #endregion

    #region State Behaviors
    public override void DoAIInterval()
    {
        base.DoAIInterval();
        if (isEnemyDead || StartOfRound.Instance.allPlayersDead)
            return;

        switch (currentBehaviourStateIndex)
        {
            case (int)JanitorStates.Idle:
                DoIdle();
                break;
            case (int)JanitorStates.StoringScrap:
                DoStoringScrap();
                break;
            case (int)JanitorStates.FollowingPlayer:
                DoFollowingPlayer();
                break;
            case (int)JanitorStates.ZoomingOff:
                DoZoomingOff();
                break;
            case (int)JanitorStates.Dead:
                break;
        }
    }

    private void DoIdle()
    {
        if (_isRotating)
            return;

        if (!IsPathValid())
        {
            CalculateAndSetNewPath(RoundManager.Instance.GetRandomNavMeshPositionInRadius(transform.position, 50, default));
            return;
        }

        if (ReachedCurrentCorner())
        {
            if (IsAtFinalCorner())
            {
                CalculateAndSetNewPath(RoundManager.Instance.GetRandomNavMeshPositionInRadius(transform.position, 50, default));
            }
            else
            {
                BeginRotation();
            }
            return;
        }

        HandleMovement();
    }

    private void DoStoringScrap()
    {
        if (!IsScrapStillValid())
        {
            ResetToIdle();
            return;
        }

        if (_isRotating)
            return;

        HandleMovement();

        if (ReachedCurrentCorner())
        {
            if (IsAtFinalCorner())
            {
                TryGrabScrap();
            }
            else
            {
                BeginRotation();
            }
        }
    }

    private void DoFollowingPlayer()
    {
        if (targetPlayer == null || targetPlayer.isPlayerDead || !targetPlayer.isPlayerControlled)
        {
            ResetChaseAndRevertToIdle();
            return;
        }

        if (_isRotating)
            return;

        if (!IsPathValid())
        {
            UpdatePathToTargetPlayer();
            return;
        }


        HandleMovement();

        if (IsPlayerInRange() && !currentlyGrabbingPlayer)
        {
            StartGrabPlayer();
        }
        else if (ReachedCurrentCorner())
        {
            if ((IsAtFinalCorner()) || (!currentlyGrabbingPlayer && IsPlayerTooFarFromFinalCorner()))
            {
                UpdatePathToTargetPlayer();
            }
            else
            {
                BeginRotation();
            }
        }
    }

    private void DoZoomingOff()
    {
        if (!IsPathValid() || _targetTrashCan == null)
        {
            if (currentlyThrowingPlayer)
                return;

            if (!TryFindAnyValidTrashCan())
            {
                TriggerPlayerThrowAnimation();
            }
            return;
        }

        if (_isRotating)
            return;

        HandleMovement();

        if (ReachedCurrentCorner())
        {
            if (IsAtFinalCorner() && !currentlyThrowingPlayer)
            {
                TriggerPlayerThrowAnimation();
            }
            else
            {
                BeginRotation();
            }
        }
    }
    #endregion

    #region Rpc Calls
    [ServerRpc(RequireOwnership = false)]
    public void SetBlendShapeWeightServerRpc(int weight)
    {
        SetBlendShapeWeightClientRpc(weight);
    }

    [ClientRpc]
    public void SetBlendShapeWeightClientRpc(int weight)
    {
        sirenLights.SetActive(weight == 100);
        skinnedMeshRenderers[0].SetBlendShapeWeight(0, weight);
    }

    [ServerRpc(RequireOwnership = false)]
    public void DetectDroppedScrapServerRpc(Vector3 noisePosition, int playerWhoDroppedIndex)
    {
        if (!NavMesh.SamplePosition(noisePosition, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            return;

        if (ObjectIsPathable(hit.position, 20f, out NavMeshPath path))
        {
            SetTargetClientRpc(playerWhoDroppedIndex);
            UpdateBlendShapeAndSpeedForChase();
            SwitchToBehaviourClientRpc((int)JanitorStates.FollowingPlayer);
        }
        PlayDetectScrapSoundClientRpc();
    }

    [ClientRpc]
    public void PlayDetectScrapSoundClientRpc()
    {
        creatureVoice.PlayOneShot(detectItemDroppedSounds[enemyRandom.Next(detectItemDroppedSounds.Length)]);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerImmovableServerRpc(int playerIndex)
    {
        SetPlayerImmovableClientRpc(playerIndex);
    }

    [ClientRpc]
    public void SetPlayerImmovableClientRpc(int playerIndex)
    {
        PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[playerIndex];
        player.inAnimationWithEnemy = this;
        targetPlayer = player;
        player.disableMoveInput = true;

        if (!IsServer)
            return;

        creatureAnimator.SetBool(HoldingPlayerAnimation, true);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetTargetScrapUngrabbableServerRpc(NetworkObjectReference netObjRef)
    {
        SetTargetScrapUngrabbableClientRpc(netObjRef);
    }

    [ClientRpc]
    public void SetTargetScrapUngrabbableClientRpc(NetworkObjectReference netObjRef)
    {
        GrabbableObject? scrapObj = (netObjRef.TryGet(out NetworkObject netObj) ? netObj : null)?.GetComponent<GrabbableObject>();
        if (scrapObj == null)
        {
            _targetScrap = null;
            SwitchToBehaviourStateOnLocalClient((int)JanitorStates.Idle);
            return;
        }

        if (scrapObj.isHeld && scrapObj.playerHeldBy != null)
        {
            SwitchToChaseState(scrapObj.playerHeldBy);
            return;
        }

        scrapObj.grabbable = false;
        HoarderBugAI.grabbableObjectsInMap.Remove(scrapObj.gameObject);

        _targetScrap = scrapObj;
        if (!IsServer)
            return;

        creatureNetworkAnimator.SetTrigger(GrabScrapAnimation);
    }
    #endregion

    #region Movement & Rotation
    private void BeginRotation()
    {
        _isRotating = true;
        if (agent.velocity.magnitude > 7.5f)
        {
            creatureNetworkAnimator.SetTrigger(BreakMovementAnimation);
        }

        smartAgentNavigator.cantMove = true;
        agent.velocity = Vector3.zero;
        agent.isStopped = true;

        creatureAnimator.SetFloat(LeftTreadFloat, 0f);
        creatureAnimator.SetFloat(RightTreadFloat, 0f);
    }

    private void HandleRotation()
    {
        if (_pathCorners.Length == 0)
            return;

        Vector3 direction = GetRotationDirection();
        float signedAngle = Vector3.SignedAngle(transform.forward, direction, Vector3.up);
        bool turningRight = signedAngle > 0f;

        float rotateSpeed = 60f * Time.deltaTime * (turningRight ? 1 : -1) * (sirenLights.activeSelf ? 6 : 1);
        transform.Rotate(Vector3.up, rotateSpeed);

        creatureAnimator.SetFloat(LeftTreadFloat, turningRight ? 1f : -1f);
        creatureAnimator.SetFloat(RightTreadFloat, turningRight ? -1f : 1f);
        creatureSFX.volume = 1f;

        if (Mathf.Abs(signedAngle) < 2.5f)
        {
            StopRotating();
        }
    }

    private Vector3 GetRotationDirection()
    {
        Vector3 direction;
        if (_currentCornerIndex < _pathCorners.Length - 1)
        {
            direction = (_pathCorners[_currentCornerIndex + 1] - transform.position).normalized;
        }
        else
        {
            direction = (_pathCorners[_currentCornerIndex] - transform.position).normalized;
        }
        direction.y = 0f;
        return direction;
    }

    private void HandleMovement()
    {
        float forwardSpeed = agent.velocity.magnitude;

        creatureAnimator.SetFloat(LeftTreadFloat, forwardSpeed);
        creatureAnimator.SetFloat(RightTreadFloat, forwardSpeed);
        creatureSFX.volume = (forwardSpeed > 0f) ? 1f : 0f;

        if (agent.pathPending)
            return;

        smartAgentNavigator.DoPathingToDestination(_pathCorners[_currentCornerIndex]);
    }

    private void StopRotating()
    {
        _isRotating = false;
        agent.isStopped = false;
        smartAgentNavigator.cantMove = false;

        if (_currentCornerIndex < _pathCorners.Length - 1)
        {
            _currentCornerIndex++;
        }

        creatureSFX.volume = 0f;
        creatureAnimator.SetFloat(LeftTreadFloat, 0f);
        creatureAnimator.SetFloat(RightTreadFloat, 0f);

        HandleMovement();
    }
    #endregion

    #region Animation Events
    public void GrabScrapAnimEvent()
    {
        if (_targetScrap == null)
        {
            _targetScrap = null;
            ResetToIdle();
            return;
        }

        if (currentBehaviourStateIndex != (int)JanitorStates.StoringScrap)
        {
            _targetScrap = null;
            return;
        }

        if (_targetScrap.playerHeldBy != null)
        {
            SwitchToChaseState(_targetScrap.playerHeldBy);
            return;
        }

        StartCoroutine(PlaceScrapInsideJanitor(_targetScrap));
        _targetScrap = null;
    }

    public void GrabPlayerAnimEvent()
    {
        _targetScrap = null;
        creatureVoice.PlayOneShot(grabPlayerSounds[enemyRandom.Next(grabPlayerSounds.Length)]);
        SwitchToBehaviourStateOnLocalClient((int)JanitorStates.ZoomingOff);
        currentlyGrabbingPlayer = false;
    }

    public void ThrowPlayerAnimEvent()
    {
        creatureVoice.PlayOneShot(throwPlayerSounds[enemyRandom.Next(throwPlayerSounds.Length)]);

        PlayerControllerB previousTargetPlayer = targetPlayer;
        if (previousTargetPlayer == null)
        {
            currentlyThrowingPlayer = false;
            return;
        }

        Vector3 forceDirection = (_targetTrashCan != null) ? (_targetTrashCan.transform.position - previousTargetPlayer.transform.position).normalized : transform.forward;

        previousTargetPlayer.externalForceAutoFade = Vector3.up * 5f + forceDirection * 25f;

        // Reset states
        _targetTrashCan = null;
        targetPlayer = null;
        previousTargetPlayer.disableMoveInput = false;
        currentlyThrowingPlayer = false;
        previousTargetPlayer.inAnimationWithEnemy = null;
        previousTargetPlayer.DamagePlayer(15, true, false, CauseOfDeath.Gravity, 0, false, default);

        if (!IsServer)
            return;

        creatureAnimator.SetBool(HoldingPlayerAnimation, false);
    }
    #endregion

    #region Overrides
    public override void HitEnemy(int force = 1, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
        if (isEnemyDead)
            return;

        if (targetPlayer != null && targetPlayer == playerWhoHit && targetPlayer.disableMoveInput)
        {

        }
        else
        {
            enemyHP -= force;
        }

        if (enemyHP <= 0 && !isEnemyDead)
        {
            if (IsOwner)
            {
                KillEnemyOnOwnerClient();
            }
            return;
        }

        if (playerWhoHit == null || !IsServer || currentBehaviourStateIndex == (int)JanitorStates.FollowingPlayer || currentBehaviourStateIndex == (int)JanitorStates.ZoomingOff)
            return;

        if (!currentlyGrabbingPlayer && !currentlyGrabbingScrap && !currentlyThrowingPlayer)
        {
            DetectDroppedScrapServerRpc(playerWhoHit.transform.position, Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerWhoHit));
        }
        else
        {
            StartCoroutine(WaitUntilNotDoingAnythingCurrently(playerWhoHit));
        }
    }

    public override void KillEnemy(bool destroy = false)
    {
        base.KillEnemy(destroy);
        creatureVoice.PlayOneShot(deathSounds[enemyRandom.Next(deathSounds.Length)]);
        currentlyThrowingPlayer = false;
        currentlyGrabbingPlayer = false;
        currentlyGrabbingScrap = false;
        _targetScrap = null;
        _targetTrashCan = null;

        if (targetPlayer != null)
        {
            targetPlayer.inAnimationWithEnemy = null;
            targetPlayer.disableMoveInput = false;
        }

        SwitchToBehaviourStateOnLocalClient((int)JanitorStates.Dead);

        foreach (var item in _storedScrap)
        {
            if (item == null)
                continue;
            item.EnableItemMeshes(true);
            item.EnablePhysics(true);
            item.grabbable = true;
            item.grabbableToEnemies = true;
            if (!HoarderBugAI.grabbableObjectsInMap.Contains(item.gameObject))
            {
                HoarderBugAI.grabbableObjectsInMap.Add(item.gameObject);
            }
        }

        _storedScrap.Clear();
        creatureSFX.volume = 0f;

        // Turn off lights and blend shape
        sirenLights.SetActive(false);
        skinnedMeshRenderers[0].SetBlendShapeWeight(0, 0);
        foreach (var lights in headLights)
        {
            lights.SetActive(false);
        }

        creatureVoice.pitch = 0.75f;
        if (!IsServer)
            return;

        creatureAnimator.SetBool(HoldingPlayerAnimation, false);
        creatureAnimator.SetFloat(LeftTreadFloat, 0);
        creatureAnimator.SetFloat(RightTreadFloat, 0);
        creatureAnimator.SetBool(IsAngryAnimation, false);
        creatureAnimator.SetBool(IsDeadAnimation, true);
    }
    #endregion

    #region Misc Methods
    private void ApplyMaterialVariant()
    {
        Material variantMaterial = variantMaterials[enemyRandom.Next(variantMaterials.Length)];
        Material[] currentMaterials = skinnedMeshRenderers[0].sharedMaterials;
        currentMaterials[1] = variantMaterial;
        skinnedMeshRenderers[0].SetMaterials(currentMaterials.ToList());
    }

    private void HandleIdleSoundTimer()
    {
        _idleTimer -= Time.deltaTime;
        if (_idleTimer > 0)
            return;

        _idleTimer = enemyRandom.Next(30, 150);
        AudioClip clip = isEnemyDead ? postDeathSounds[enemyRandom.Next(postDeathSounds.Length)] : idleSounds[enemyRandom.Next(idleSounds.Length)];
        creatureVoice.PlayOneShot(clip);
    }

    private void KeepPlayerAttachedDuringZoom()
    {
        if (targetPlayer == null || targetPlayer.isPlayerDead || !targetPlayer.isPlayerControlled)
        {
            targetPlayer = null;
            sirenLights.SetActive(false);
            _targetScrap = null;
            SwitchToBehaviourStateOnLocalClient((int)JanitorStates.Idle);
            skinnedMeshRenderers[0].SetBlendShapeWeight(0, 0);
            if (!IsServer)
                return;

            agent.speed = 7.5f;
            creatureAnimator.SetBool(IsAngryAnimation, false);
        }
        else
        {
            targetPlayer.transform.position = playerBoneTransform.position;
            targetPlayer.transform.rotation = playerBoneTransform.rotation;
        }
    }

    private void CalculateAndSetNewPath(Vector3 targetPosition)
    {
        NavMeshPath path = new();
        agent.CalculatePath(targetPosition, path);
        if (path.corners.Length <= 0)
            return;

        if (path.status != NavMeshPathStatus.PathComplete)
            return;

        SetPathAsDestination(path);
    }

    private void SetPathAsDestination(NavMeshPath navMeshPath)
    {
        agent.ResetPath();
        agent.SetPath(navMeshPath);
        _pathCorners = navMeshPath.corners;
        _currentCornerIndex = 0;

        if (ReachedCurrentCorner())
        {
            BeginRotation();
            return;
        }
        smartAgentNavigator.DoPathingToDestination(_pathCorners[_currentCornerIndex]);
    }

    private float DoCalculatePathDistance(NavMeshPath path)
    {
        float length = 0f;
        if (path.corners.Length > 1)
        {
            for (int i = 1; i < path.corners.Length; i++)
            {
                float dist = Vector3.Distance(path.corners[i - 1], path.corners[i]);
                length += dist;
                // Plugin.ExtendedLogging($"Distance: {dist}");
            }
        }
        // Plugin.ExtendedLogging($"Path distance: {length}");
        return length;
    }

    private bool IsPathValid()
    {
        return _currentCornerIndex < _pathCorners.Length && agent.path != null && agent.path.corners.Length > 0 && agent.path.status == NavMeshPathStatus.PathComplete;
    }

    private bool ReachedCurrentCorner()
    {
        float distToCorner = Vector3.Distance(transform.position, _pathCorners[_currentCornerIndex]);
        return distToCorner <= 0.35f;
    }

    private bool IsAtFinalCorner()
    {
        return _currentCornerIndex == _pathCorners.Length - 1;
    }

    private bool IsScrapStillValid()
    {
        return IsPathValid() &&
                _targetScrap != null &&
                !_targetScrap.isHeld &&
                !_targetScrap.isHeldByEnemy &&
                _targetScrap.playerHeldBy == null;
    }

    private void TryGrabScrap()
    {
        if (_targetScrap == null)
            return;

        if (currentlyGrabbingScrap)
            return;

        currentlyGrabbingScrap = true;
        SetTargetScrapUngrabbableServerRpc(new NetworkObjectReference(_targetScrap.gameObject));
    }

    private void ResetChaseAndRevertToIdle()
    {
        agent.ResetPath();
        targetPlayer = null;
        _targetScrap = null;
        SwitchToBehaviourServerRpc((int)JanitorStates.Idle);
        agent.speed = 7.5f;
        creatureAnimator.SetBool(IsAngryAnimation, false);
        SetBlendShapeWeightServerRpc(0);
    }

    private void UpdatePathToTargetPlayer()
    {
        if (NavMesh.SamplePosition(targetPlayer.transform.position, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
        {
            CalculateAndSetNewPath(hit.position);
        }
    }

    private bool IsPlayerInRange()
    {
        float distToPlayer = Vector3.Distance(transform.position, targetPlayer.transform.position);
        Vector3 directionToPlayer = (targetPlayer.transform.position - transform.position).normalized;
        float dotProduct = Vector3.Dot(transform.forward, directionToPlayer);

        return distToPlayer <= agent.stoppingDistance + 2f && dotProduct > 0.25f;
    }

    private void StartGrabPlayer()
    {
        currentlyGrabbingPlayer = true;
        int playerIndex = Array.IndexOf(StartOfRound.Instance.allPlayerScripts, targetPlayer);
        SetPlayerImmovableServerRpc(playerIndex);
    }

    private bool IsPlayerTooFarFromFinalCorner()
    {
        float distToLastCorner = Vector3.Distance(targetPlayer.transform.position, _pathCorners[^1]);
        return distToLastCorner > 3f && _currentCornerIndex != 0;
    }

    private bool TryFindAnyValidTrashCan()
    {
        List<(TrashCan trashCan, NavMeshPath path)> viableTrashCans = new();
        foreach (TrashCan trashCan in trashCans)
        {
            if (trashCan == null) continue;
            NavMesh.SamplePosition(trashCan.transform.position, out NavMeshHit hit, 5, NavMesh.AllAreas);
            if (ObjectIsPathable(hit.position, 500f, out NavMeshPath path))
            {
                viableTrashCans.Add((trashCan, path));
            }
        }
        Plugin.ExtendedLogging($"Found {viableTrashCans.Count} viable trash cans");
        if (viableTrashCans.Count > 0)
        {
            var (trashCan, path) = viableTrashCans[UnityEngine.Random.Range(0, viableTrashCans.Count)];
            _targetTrashCan = trashCan;
            SetPathAsDestination(path);
            return true;
        }
        return false;
    }

    private void TriggerPlayerThrowAnimation()
    {
        currentlyThrowingPlayer = true;
        creatureNetworkAnimator.SetTrigger(ThrowPlayerAnimation);
    }

    private IEnumerator PlaceScrapInsideJanitor(GrabbableObject scrap)
    {
        scrap.parentObject = handTransform;
        scrap.isHeldByEnemy = true;
        scrap.hasHitGround = false;
        scrap.EnablePhysics(false);
        yield return new WaitForSeconds(0.2f);
        scrap.isInElevator = false;
        scrap.isInShipRoom = false;
        scrap.playerHeldBy?.DiscardHeldObject();
        yield return new WaitForSeconds(0.2f);
        _storedScrap.Add(scrap);
        scrap.parentObject = placeToHideScrap;
        scrap.transform.position = placeToHideScrap.position;
        scrap.EnableItemMeshes(false);

        ResetToIdle();
        currentlyGrabbingScrap = false;
    }

    private void ResetToIdle()
    {
        agent.ResetPath();
        _targetScrap = null;
        SwitchToBehaviourServerRpc((int)JanitorStates.Idle);
    }

    private void SwitchToChaseState(PlayerControllerB player)
    {
        sirenLights.SetActive(true);
        _targetScrap = null;
        targetPlayer = player;
        SwitchToBehaviourStateOnLocalClient((int)JanitorStates.FollowingPlayer);
        skinnedMeshRenderers[0].SetBlendShapeWeight(0, 100);

        if (!IsServer) return;
        agent.ResetPath();
        agent.speed = 15f;
        creatureAnimator.SetBool(IsAngryAnimation, true);
    }

    private void TryFindScrapNearby()
    {
        int numHits = Physics.OverlapSphereNonAlloc(transform.position, 15f, _hitColliders, CodeRebirthUtils.Instance.propsMask, QueryTriggerInteraction.Collide);

        for (int i = 0; i < numHits; i++)
        {
            if (!_hitColliders[i].TryGetComponent(out GrabbableObject grabbable) ||
                grabbable.isHeld ||
                grabbable.isHeldByEnemy ||
                grabbable.playerHeldBy != null ||
                _storedScrap.Contains(grabbable))
            {
                continue;
            }

            bool skipItem = false;
            foreach (var janitor in janitors)
            {
                if (janitor._targetScrap == grabbable)
                {
                    skipItem = true;
                    break;
                }
            }

            if (skipItem)
                continue;

            if (ObjectIsPathable(grabbable.transform.position, 12.5f, out NavMeshPath path))
            {
                _targetScrap = grabbable;
                SetPathAsDestination(path);
                SwitchToBehaviourServerRpc((int)JanitorStates.StoringScrap);
                break;
            }
        }
    }

    private bool ObjectIsPathable(Vector3 position, float maxPathLength, out NavMeshPath path)
    {
        path = new();
        if (!agent.CalculatePath(position, path))
            return false;

        if (path.status != NavMeshPathStatus.PathComplete)
            return false;

        if (DoCalculatePathDistance(path) > maxPathLength)
            return false;

        return true;
    }

    private void UpdateBlendShapeAndSpeedForChase()
    {
        SetBlendShapeWeightClientRpc(100);
        agent.speed = 15f;
        creatureAnimator.SetBool(IsAngryAnimation, true);
        agent.ResetPath();
        _targetScrap = null;
    }
    #endregion
}