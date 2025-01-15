using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public AudioClip[] idleSounds = [];
    public AudioClip[] detectItemDroppedSounds = [];
    public AudioClip[] grabPlayerSounds = [];
    public AudioClip[] throwPlayerSounds = [];
    public Material[] variantMaterials = [];

    private float idleTimer = 60f;
    private System.Random janitorRandom = new();
    
    // Janitor states
    public enum JanitorStates
    {
        Idle,
        StoringScrap,
        FollowingPlayer,
        ZoomingOff,
        Dead
    }

    // For storing references to all Janitors and TrashCans
    public static List<Janitor> janitors = new();
    [HideInInspector] public static List<TrashCan> trashCans = new();
    
    // Navigation and pathing
    private Vector3[] _pathCorners = [];
    private int _currentCornerIndex = 0;
    private bool _isRotating = false;
    private bool _isPathValid;
    private readonly float _cornerThreshold = 0.5f;

    // Scrap & Player
    private TrashCan? targetTrashCan = null;
    private GrabbableObject? targetScrap = null;
    [HideInInspector] public bool currentlyGrabbingScrap = false;
    [HideInInspector] public bool currentlyGrabbingPlayer = false;
    [HideInInspector] public bool currentlyThrowingPlayer = false;
    private readonly Dictionary<GrabbableObject, int> storedScrapAndValueDict = new();

    // Animator Hashes
    private static readonly int RightTreadFloat = Animator.StringToHash("RightTreadFloat");
    private static readonly int LeftTreadFloat  = Animator.StringToHash("LeftTreadFloat");
    private static readonly int IsAngryAnimation = Animator.StringToHash("isAngry");
    private static readonly int HoldingPlayerAnimation = Animator.StringToHash("holdingPlayer");
    private static readonly int IsDeadAnimation = Animator.StringToHash("isDead");
    private static readonly int GrabScrapAnimation = Animator.StringToHash("grabScrap");
    private static readonly int BreakMovementAnimation = Animator.StringToHash("break");
    private static readonly int ThrowPlayerAnimation = Animator.StringToHash("throwPlayer");

    #endregion

    #region Network Spawn & Despawn

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

    #endregion

    #region Unity Lifecycle

    public override void Start()
    {
        base.Start();
        // Random seed for variant material
        janitorRandom = new System.Random(StartOfRound.Instance.randomMapSeed + janitors.Count);

        // Apply material variant
        ApplyMaterialVariant();

        // Audio & initial setup
        creatureVoice.PlayOneShot(spawnSound);
        SwitchToBehaviourStateOnLocalClient((int)JanitorStates.Idle);

        // If we're the server, start coroutines
        if (IsServer)
        {
            StartCoroutine(CheckForScrapNearby());
        }
    }

    public override void Update()
    {
        base.Update();
        
        if (currentBehaviourStateIndex == (int)JanitorStates.Idle)
        {
            HandleIdleSoundTimer();
        }

        // Only the server does path rotation
        if (!IsServer) return;
        if (_isRotating)
        {
            HandleRotation();
        }
    }

    /// <summary>
    /// Handle position adjustments each frame, especially for ZoomingOff state.
    /// </summary>
    public void LateUpdate()
    {
        if (currentBehaviourStateIndex == (int)JanitorStates.ZoomingOff)
        {
            KeepPlayerAttachedDuringZoom();
        }
    }

    #endregion

    #region Coroutines

    /// <summary>
    /// Periodically checks for scrap in range, and if found, tries to collect it.
    /// </summary>
    public IEnumerator CheckForScrapNearby()
    {
        while (true)
        {
            yield return new WaitForSeconds(2f);

            // Only check if we're idle and have no scrap targeted
            yield return new WaitUntil(() => currentBehaviourStateIndex == (int)JanitorStates.Idle && targetScrap == null);

            // Scan for scrap
            TryFindScrapNearby();
        }
    }

    public IEnumerator WaitUntilNotDoingAnythingCurrently(PlayerControllerB playerWhoHit)
    {
        yield return new WaitUntil(() => !currentlyGrabbingScrap && !currentlyGrabbingPlayer && !currentlyThrowingPlayer);
        DetectDroppedScrapServerRpc(playerWhoHit.transform.position, Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerWhoHit));
    }

    #endregion

    #region State Behaviors

    public override void DoAIInterval()
    {
        base.DoAIInterval();

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
                // Currently no default code for Dead
                break;
        }
    }

    /// <summary>
    /// Wanders around when idle.
    /// </summary>
    private void DoIdle()
    {
        if (!_isPathValid || IsPathInvalid())
        {
            CalculateAndSetNewPath(
                RoundManager.Instance.GetRandomNavMeshPositionInRadius(transform.position, 50, default)
            );
            return;
        }

        if (_isRotating) return;
        HandleMovement();

        // If we've reached a corner, rotate or get a new path
        if (ReachedCurrentCorner())
        {
            if (IsAtFinalCorner())
            {
                // Reached the final corner, pick a new path
                CalculateAndSetNewPath(
                    RoundManager.Instance.GetRandomNavMeshPositionInRadius(transform.position, 50, default)
                );
            }
            else
            {
                BeginRotation();
            }
        }
    }

    /// <summary>
    /// Moves to scrap location. If invalid or scrap is missing, go idle again.
    /// </summary>
    private void DoStoringScrap()
    {
        if (!IsScrapStillValid())
        {
            // Something invalid happened; revert to Idle
            ResetToIdle();
            return;
        }

        if (_isRotating) return;
        HandleMovement();

        if (ReachedCurrentCorner())
        {
            if (IsAtFinalCorner())
            {
                // Attempt to pick up the scrap if in reach
                TryGrabScrap();
            }
            else
            {
                BeginRotation();
            }
        }
    }

    /// <summary>
    /// Chases a target player. If close enough, tries to grab them.
    /// </summary>
    private void DoFollowingPlayer()
    {
        if (!IsPlayerStillValid())
        {
            ResetChaseAndRevertToIdle();
            return;
        }

        if (!_isPathValid || IsPathInvalid())
        {
            UpdatePathToTargetPlayer();
            return;
        }

        if (_isRotating) return;
        HandleMovement();

        if (IsPlayerInRange() && !currentlyGrabbingPlayer)
        {
            StartGrabPlayer();
        }
        else if (ReachedCurrentCorner())
        {
            // If the player is too far from our corner, recalc path
            if (!currentlyGrabbingPlayer && IsPlayerTooFarFromCorner())
            {
                _isPathValid = false;
            }
            else
            {
                BeginRotation();
            }
        }
    }

    /// <summary>
    /// Moves the Janitor (with player in tow) to a trash can to throw them in.
    /// </summary>
    private void DoZoomingOff()
    {
        if (!_isPathValid || IsPathInvalid() || targetTrashCan == null)
        {
            // If we can't find a new trash can or path is invalid, throw the player now
            if (currentlyThrowingPlayer) return;
            if (!TryFindAnyValidTrashCan())
            {
                TriggerPlayerThrowAnimation();
            }
            return;
        }

        if (_isRotating) return;
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

    #region Server/Client RPC Calls

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
        NavMesh.SamplePosition(noisePosition, out NavMeshHit hit, 5f, NavMesh.AllAreas);
        NavMeshPath path = new NavMeshPath();
        if (agent.CalculatePath(hit.position, path) &&
            path.status == NavMeshPathStatus.PathComplete &&
            DoCalculatePathDistance(path) <= 20f)
        {
            // Switch to chasing state
            SetTargetClientRpc(playerWhoDroppedIndex);
            targetPlayer = StartOfRound.Instance.allPlayerScripts[playerWhoDroppedIndex];
            UpdateBlendShapeAndSpeedForChase();
            SwitchToBehaviourClientRpc((int)JanitorStates.FollowingPlayer);
        }
        PlayDetectScrapSoundClientRpc();
    }

    [ClientRpc]
    public void PlayDetectScrapSoundClientRpc()
    {
        creatureVoice.PlayOneShot(detectItemDroppedSounds[janitorRandom.Next(detectItemDroppedSounds.Length)]);
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

        if (IsServer)
        {
            creatureAnimator.SetBool(HoldingPlayerAnimation, true);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetTargetScrapUngrabbableServerRpc(NetworkObjectReference netObjRef)
    {
        SetTargetScrapUngrabbableClientRpc(netObjRef);
    }

    [ClientRpc]
    public void SetTargetScrapUngrabbableClientRpc(NetworkObjectReference netObjRef)
    {
        var scrapObj = (netObjRef.TryGet(out NetworkObject netObj) ? netObj : null)?.GetComponent<GrabbableObject>();
        if (scrapObj == null)
        {
            // Fallback
            targetScrap = null;
            SwitchToBehaviourStateOnLocalClient((int)JanitorStates.Idle);
            return;
        }

        // If someone else grabbed it in the meantime, chase that player
        if (scrapObj.isHeld && scrapObj.playerHeldBy != null)
        {
            // Freak out => chase player
            SwitchToChaseState(scrapObj.playerHeldBy);
            return;
        }

        scrapObj.grabbable = false;
        HoarderBugAI.grabbableObjectsInMap.Remove(scrapObj.gameObject);

        if (IsServer) 
        {
            creatureNetworkAnimator.SetTrigger(GrabScrapAnimation);
        }
    }

    #endregion

    #region Movement & Rotation

    /// <summary>
    /// Initiates rotation to next corner (stops agent movement while rotating).
    /// </summary>
    private void BeginRotation()
    {
        _isRotating = true;
        if (agent.velocity.magnitude > 7.5f)
        {
            creatureNetworkAnimator.SetTrigger(BreakMovementAnimation);
        }

        agent.velocity = Vector3.zero;
        agent.isStopped = true;

        // Reset tread animations
        creatureAnimator.SetFloat(LeftTreadFloat, 0f);
        creatureAnimator.SetFloat(RightTreadFloat, 0f);
    }

    /// <summary>
    /// Rotates the Janitor to face the next path corner.
    /// </summary>
    private void HandleRotation()
    {
        if (_pathCorners.Length == 0) return;

        Vector3 direction = GetRotationDirection();
        float signedAngle = Vector3.SignedAngle(transform.forward, direction, Vector3.up);
        bool turningRight = signedAngle > 0f;

        // Apply rotation
        float rotateSpeed = 60f * Time.deltaTime * (turningRight ? 1 : -1) * (sirenLights.activeSelf ? 6 : 1);
        transform.Rotate(Vector3.up, rotateSpeed);

        // Animate treads
        creatureAnimator.SetFloat(LeftTreadFloat,  turningRight ? 1f : -1f);
        creatureAnimator.SetFloat(RightTreadFloat, turningRight ? -1f : 1f);
        creatureSFX.volume = 1f;

        // If mostly aligned, stop rotating
        if (Mathf.Abs(signedAngle) < 5f)
        {
            StopRotating();
        }
    }

    private Vector3 GetRotationDirection()
    {
        Vector3 direction;
        if (_currentCornerIndex < _pathCorners.Length - 1)
        {
            // Next corner direction
            direction = (_pathCorners[_currentCornerIndex + 1] - transform.position).normalized;
        }
        else
        {
            // If there's no next corner, rotate to final corner (or do something else)
            direction = (_pathCorners[_currentCornerIndex] - transform.position).normalized;
        }
        direction.y = 0f;
        return direction;
    }

    /// <summary>
    /// Moves the Janitor along the path, updates the animator.
    /// </summary>
    private void HandleMovement()
    {
        float forwardSpeed = agent.velocity.magnitude;

        creatureAnimator.SetFloat(LeftTreadFloat,  forwardSpeed);
        creatureAnimator.SetFloat(RightTreadFloat, forwardSpeed);
        creatureSFX.volume = (forwardSpeed > 0f) ? 1f : 0f;

        // Ensure the agent is heading to the correct corner
        if (_pathCorners.Length > 0 && _currentCornerIndex < _pathCorners.Length)
        {
            if (!agent.pathPending)
            {
                smartAgentNavigator.DoPathingToDestination(_pathCorners[_currentCornerIndex]);
            }
        }
    }

    /// <summary>
    /// Stops rotating and advances the corner index if not at the final corner.
    /// </summary>
    private void StopRotating()
    {
        _isRotating = false;
        agent.isStopped = false;

        // Move on to the next corner
        if (_currentCornerIndex < _pathCorners.Length - 1)
        {
            _currentCornerIndex++;
            smartAgentNavigator.DoPathingToDestination(_pathCorners[_currentCornerIndex]
            );
        }

        // Reset tread animations
        creatureSFX.volume = 0f;
        creatureAnimator.SetFloat(LeftTreadFloat,  0f);
        creatureAnimator.SetFloat(RightTreadFloat, 0f);
    }

    #endregion

    #region Animation Events

    /// <summary>
    /// Animation event for fully grabbing scrap.
    /// </summary>
    public void GrabScrapAnimEvent()
    {
        if (targetScrap == null || Vector3.Distance(targetScrap.transform.position, transform.position) > 1.25f)
        {
            targetScrap = null;
            ResetToIdle();
            return;
        }

        if (currentBehaviourStateIndex != (int)JanitorStates.StoringScrap)
        {
            targetScrap = null;
            return;
        }

        // If a player snatched it in the meantime => chase
        if (targetScrap.playerHeldBy != null)
        {
            SwitchToChaseState(targetScrap.playerHeldBy);
            return;
        }

        // Otherwise, proceed to store it
        StartCoroutine(PlaceScrapInsideJanitor(targetScrap));
        targetScrap = null;
    }

    /// <summary>
    /// Animation event for fully grabbing a player.
    /// </summary>
    public void GrabPlayerAnimEvent()
    {
        targetScrap = null;
        creatureVoice.PlayOneShot(grabPlayerSounds[janitorRandom.Next(grabPlayerSounds.Length)]);
        SwitchToBehaviourStateOnLocalClient((int)JanitorStates.ZoomingOff);
        currentlyGrabbingPlayer = false;
    }

    /// <summary>
    /// Animation event for throwing the player into a trash can or away.
    /// </summary>
    public void ThrowPlayerAnimEvent()
    {
        creatureVoice.PlayOneShot(throwPlayerSounds[janitorRandom.Next(throwPlayerSounds.Length)]);

        PlayerControllerB previousTargetPlayer = targetPlayer;
        if (previousTargetPlayer == null)
        {
            currentlyThrowingPlayer = false;
            return;
        }

        // Apply force to tossed player
        Vector3 forceDirection = (targetTrashCan != null)
            ? (targetTrashCan.transform.position - previousTargetPlayer.transform.position).normalized
            : transform.forward;

        previousTargetPlayer.externalForceAutoFade = Vector3.up * 5f + forceDirection * 25f;

        // Reset states
        targetTrashCan = null;
        targetPlayer = null;
        previousTargetPlayer.disableMoveInput = false;
        currentlyThrowingPlayer = false;
        previousTargetPlayer.inAnimationWithEnemy = null;
        previousTargetPlayer.DamagePlayer(15, true, false, CauseOfDeath.Gravity, 0, false, default);

        if (IsServer)
        {
            creatureAnimator.SetBool(HoldingPlayerAnimation, false);
        }
    }

    #endregion

    #region Overrides (Take Damage, Kill, etc.)

    /// <summary>
    /// Applies damage to the Janitor and handles transitions to chase or death states.
    /// </summary>
    public override void HitEnemy(int force = 1, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
        if (isEnemyDead) return;

        // If a new player hits us, reduce HP further
        if (playerWhoHit != null && targetPlayer != playerWhoHit && targetPlayer.disableMoveInput)
        {
            enemyHP -= force;
        }

        // If we’re still alive, chase that player if we’re not already
        if (playerWhoHit != null && IsServer && currentBehaviourStateIndex != (int)JanitorStates.FollowingPlayer && currentBehaviourStateIndex != (int)JanitorStates.ZoomingOff)
        {
            if (!currentlyGrabbingPlayer && !currentlyGrabbingScrap && !currentlyThrowingPlayer)
            {
                DetectDroppedScrapServerRpc(playerWhoHit.transform.position, Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerWhoHit));
            }
            else
            {
                StartCoroutine(WaitUntilNotDoingAnythingCurrently(playerWhoHit));
            }
        }

        // If HP is depleted, kill
        if (enemyHP <= 0 && !isEnemyDead)
        {
            if (IsOwner)
            {
                KillEnemyOnOwnerClient();
            }
        }
    }

    /// <summary>
    /// Kills the Janitor, dropping scrap and resetting states.
    /// </summary>
    public override void KillEnemy(bool destroy = false)
    {
        base.KillEnemy(destroy);

        creatureVoice.PlayOneShot(deathSounds[janitorRandom.Next(deathSounds.Length)]);
        currentlyThrowingPlayer = false;
        currentlyGrabbingPlayer = false;
        currentlyGrabbingScrap = false;
        targetScrap = null;
        targetTrashCan = null;

        // Free any player
        if (targetPlayer != null)
        {
            targetPlayer.inAnimationWithEnemy = null;
            targetPlayer.disableMoveInput = false;
        }

        // Switch to Dead state
        SwitchToBehaviourStateOnLocalClient((int)JanitorStates.Dead);

        // Drop stored scrap
        foreach (var item in storedScrapAndValueDict.Keys)
        {
            item.EnableItemMeshes(true);
            item.EnablePhysics(true);
            item.grabbable = true;
            item.grabbableToEnemies = true;
            if (!HoarderBugAI.grabbableObjectsInMap.Contains(item.gameObject))
            {
                HoarderBugAI.grabbableObjectsInMap.Add(item.gameObject);
            }
        }

        storedScrapAndValueDict.Clear();
        creatureSFX.volume = 0f;

        // Reset animator states
        if (IsServer)
        {
            creatureAnimator.SetBool(HoldingPlayerAnimation, false);
            creatureAnimator.SetFloat(LeftTreadFloat, 0);
            creatureAnimator.SetFloat(RightTreadFloat, 0);
            creatureAnimator.SetBool(IsAngryAnimation, false);
            creatureAnimator.SetBool(IsDeadAnimation, true);
        }

        // Turn off lights and blend shape
        sirenLights.SetActive(false);
        skinnedMeshRenderers[0].SetBlendShapeWeight(0, 0);
        foreach (var lights in headLights)
        {
            lights.SetActive(false);
        }
    }

    #endregion

    #region Helper & Utility Methods

    private void ApplyMaterialVariant()
    {
        Material variantMaterial = variantMaterials[janitorRandom.Next(variantMaterials.Length)];
        Material[] currentMaterials = skinnedMeshRenderers[0].sharedMaterials;
        currentMaterials[1] = variantMaterial;
        skinnedMeshRenderers[0].SetMaterials(currentMaterials.ToList());
    }

    private void HandleIdleSoundTimer()
    {
        idleTimer -= Time.deltaTime;
        if (idleTimer < 0)
        {
            idleTimer = janitorRandom.Next(30, 150);
            creatureVoice.PlayOneShot(idleSounds[janitorRandom.Next(idleSounds.Length)]);
        }
    }

    private void KeepPlayerAttachedDuringZoom()
    {
        if (targetPlayer == null || targetPlayer.isPlayerDead)
        {
            // Reset and revert to idle
            targetPlayer = null;
            sirenLights.SetActive(false);
            targetScrap = null;
            SwitchToBehaviourStateOnLocalClient((int)JanitorStates.Idle);
            if (IsServer)
            {
                agent.speed = 7.5f;
                creatureAnimator.SetBool(IsAngryAnimation, false);
            }
            skinnedMeshRenderers[0].SetBlendShapeWeight(0, 0);
        }
        else
        {
            targetPlayer.transform.position = playerBoneTransform.position;
            targetPlayer.transform.rotation = playerBoneTransform.rotation;
        }
    }

    /// <summary>
    /// Calculates and sets a path to the given position, returns true if valid.
    /// </summary>
    private bool CalculateAndSetNewPath(Vector3 targetPosition)
    {
        NavMeshPath path = new NavMeshPath();
        bool pathFound = agent.CalculatePath(targetPosition, path);

        if (pathFound && path.status == NavMeshPathStatus.PathComplete)
        {
            SetPathAsDestination(path);
            return true;
        }
        _isPathValid = false;
        agent.ResetPath();
        return false;
    }

    private void SetPathAsDestination(NavMeshPath navMeshPath)
    {
        _isPathValid = true;
        agent.SetPath(navMeshPath);
        _pathCorners = navMeshPath.corners;
        _currentCornerIndex = 0;

        if (_pathCorners.Length > 0)
        {
            smartAgentNavigator.DoPathingToDestination(_pathCorners[_currentCornerIndex]
            );
        }
    }

    private float DoCalculatePathDistance(NavMeshPath path)
    {
        float length = 0f;
        if (path.corners.Length > 1)
        {
            for (int i = 1; i < path.corners.Length; i++)
            {
                length += Vector3.Distance(path.corners[i - 1], path.corners[i]);
                Plugin.ExtendedLogging($"Distance: {Vector3.Distance(path.corners[i - 1], path.corners[i])}");
            }
        }
        Plugin.ExtendedLogging($"Path distance: {length}");
        return length;
    }

    private bool IsPathInvalid()
    {
        return agent.path.status == NavMeshPathStatus.PathInvalid || agent.path.status == NavMeshPathStatus.PathPartial;
    }

    private bool ReachedCurrentCorner()
    {
        if (_pathCorners.Length == 0 || _currentCornerIndex >= _pathCorners.Length)
            return false;

        float distToCorner = Vector3.Distance(transform.position, _pathCorners[_currentCornerIndex]);
        return distToCorner <= _cornerThreshold;
    }

    private bool IsAtFinalCorner()
    {
        return _currentCornerIndex == _pathCorners.Length - 1;
    }

    private bool IsScrapStillValid()
    {
        return _isPathValid &&
                !IsPathInvalid() &&
                targetScrap != null &&
                !targetScrap.isHeld &&
                targetScrap.playerHeldBy == null &&
                !targetScrap.isHeldByEnemy;
    }

    private void TryGrabScrap()
    {
        // If within reach and not currently grabbing
        if (Vector3.Distance(targetScrap.transform.position, transform.position) <= agent.stoppingDistance + 1.2f && !currentlyGrabbingScrap)
        {
            currentlyGrabbingScrap = true;
            SetTargetScrapUngrabbableServerRpc(new NetworkObjectReference(targetScrap.gameObject));
        }
    }

    private bool IsPlayerStillValid()
    {
        return targetPlayer != null && !targetPlayer.isPlayerDead;
    }

    private void ResetChaseAndRevertToIdle()
    {
        _isPathValid = false;
        targetPlayer = null;
        targetScrap = null;
        SwitchToBehaviourServerRpc((int)JanitorStates.Idle);
        agent.speed = 7.5f;
        creatureAnimator.SetBool(IsAngryAnimation, false);
        SetBlendShapeWeightServerRpc(0);
    }

    private void UpdatePathToTargetPlayer()
    {
        NavMesh.SamplePosition(targetPlayer.transform.position, out NavMeshHit hit, 5, NavMesh.AllAreas);
        CalculateAndSetNewPath(hit.position);
    }

    private bool IsPlayerInRange()
    {
        float distToPlayer = Vector3.Distance(transform.position, targetPlayer.transform.position);

        // Determine if player is in front by comparing the dot product of the forward direction 
        // and the direction to the player. A value > 0 means "in front"; < 0 means "behind".
        Vector3 directionToPlayer = (targetPlayer.transform.position - transform.position).normalized;
        float dotProduct = Vector3.Dot(transform.forward, directionToPlayer);
        Plugin.ExtendedLogging($"Dot product: {dotProduct} with distance: {distToPlayer}");
        // Player must be within distance AND in front
        return distToPlayer <= agent.stoppingDistance + 2f && dotProduct > 0.25f;
    }

    private void StartGrabPlayer()
    {
        currentlyGrabbingPlayer = true;
        int playerIndex = Array.IndexOf(StartOfRound.Instance.allPlayerScripts, targetPlayer);
        SetPlayerImmovableServerRpc(playerIndex);
    }

    private bool IsPlayerTooFarFromCorner()
    {
        float distToLastCorner = Vector3.Distance(
            targetPlayer.transform.position,
            agent.path.corners[^1]
        );
        return distToLastCorner > 3f && _currentCornerIndex != 0;
    }

    private bool TryFindAnyValidTrashCan()
    {
        List<TrashCan> viableTrashCans = new();
        foreach (TrashCan trashCan in trashCans)
        {
            if (trashCan == null) continue;
            NavMesh.SamplePosition(trashCan.transform.position, out NavMeshHit hit, 5, NavMesh.AllAreas);
            if (smartAgentNavigator.CanPathToPoint(this.transform.position, hit.position) >= 0)
            {
                viableTrashCans.Add(trashCan);
            }
        }
        if (viableTrashCans.Count > 0)
        {
            targetTrashCan = viableTrashCans[UnityEngine.Random.Range(0, viableTrashCans.Count)];
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
        yield return new WaitForSeconds(0.2f);
        
        storedScrapAndValueDict.Add(scrap, scrap.scrapValue);
        scrap.parentObject = placeToHideScrap;
        scrap.transform.position = placeToHideScrap.position;
        scrap.EnableItemMeshes(false);
        scrap.EnablePhysics(false);

        ResetToIdle();
        currentlyGrabbingScrap = false;
    }

    private void ResetToIdle()
    {
        _isPathValid = false;
        targetScrap = null;
        SwitchToBehaviourServerRpc((int)JanitorStates.Idle);
    }

    private void SwitchToChaseState(PlayerControllerB player)
    {
        sirenLights.SetActive(true);
        _isPathValid = false;
        targetScrap = null;
        targetPlayer = player;
        SwitchToBehaviourStateOnLocalClient((int)JanitorStates.FollowingPlayer);
        skinnedMeshRenderers[0].SetBlendShapeWeight(0, 100);

        if (!IsServer) return;
        agent.speed = 15f;
        creatureAnimator.SetBool(IsAngryAnimation, true);
    }

    /// <summary>
    /// Finds scrap near the Janitor and attempts to path to it if reachable.
    /// </summary>
    private void TryFindScrapNearby()
    {
        Collider[] hitColliders = new Collider[20];
        int numHits = Physics.OverlapSphereNonAlloc(transform.position, 15f, hitColliders, LayerMask.GetMask("Props"), QueryTriggerInteraction.Collide);

        for (int i = 0; i < numHits; i++)
        {
            if (!hitColliders[i].TryGetComponent(out GrabbableObject grabbable) ||
                grabbable.isHeld ||
                grabbable.isHeldByEnemy ||
                grabbable.playerHeldBy != null ||
                storedScrapAndValueDict.ContainsKey(grabbable))
            {
                continue;
            }

            // Ensure no other Janitor is already targeting this scrap
            if (janitors.Any(j => j.targetScrap == grabbable)) continue;

            NavMeshPath path = new NavMeshPath();
            if (agent.CalculatePath(hitColliders[i].transform.position, path) &&
                path.status == NavMeshPathStatus.PathComplete &&
                DoCalculatePathDistance(path) <= 12.5f)
            {
                targetScrap = grabbable;
                SetPathAsDestination(path);
                SwitchToBehaviourServerRpc((int)JanitorStates.StoringScrap);
                break;
            }
        }
    }

    private void UpdateBlendShapeAndSpeedForChase()
    {
        SetBlendShapeWeightClientRpc(100);
        agent.speed = 15f;
        creatureAnimator.SetBool(IsAngryAnimation, true);
        _isPathValid = false;
        targetScrap = null;
    }

    #endregion
}