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
    public GameObject sirenLights = null!;
    public Transform handTransform = null!;
    public Transform playerBoneTransform = null!;
    public Transform placeToHideScrap = null!;
    [Header("Audio and Sounds")]
    public AudioClip spawnSound = null!;
    public AudioClip[] deathSounds = [];
    public AudioClip[] idleSounds = [];
    public AudioClip[] detectItemDroppedSounds = [];
    public AudioClip[] grabPlayerSounds = [];
    public AudioClip[] throwPlayerSounds = [];
    public Material[] variantMaterials = [];

    private float idleTimer = 60f;
    private System.Random janitorRandom = new();
    [HideInInspector] public static List<TrashCan> trashCans = new();
    private TrashCan? targetTrashCan = null;
    private GrabbableObject? targetScrap = null;
    private bool currentlyGrabbingScrap = false;
    private bool currentlyGrabbingPlayer = false;
    private bool currentlyThrowingPlayer = false;
    private Dictionary<GrabbableObject, int> storedScrapAndValueDict = new();
    // -- Path Info --
    private Vector3[] _pathCorners = [];
    private int _currentCornerIndex = 0;
    private bool _isRotating = false;
    private float _cornerThreshold = 0.5f; // Distance threshold to consider corner "reached"
    private bool  _isPathValid;
    public enum JanitorStates
    {
        Idle,
        StoringScrap,
        FollowingPlayer,
        ZoomingOff,
        Dead
    }

    private static readonly int RightTreadFloat = Animator.StringToHash("RightTreadFloat"); // Float
    private static readonly int LeftTreadFloat  = Animator.StringToHash("LeftTreadFloat"); // Float
    private static readonly int IsAngryAnimation = Animator.StringToHash("isAngry"); // Bool
    private static readonly int HoldingPlayerAnimation = Animator.StringToHash("holdingPlayer"); // Bool
    private static readonly int IsDeadAnimation = Animator.StringToHash("isDead"); // Bool
    private static readonly int GrabScrapAnimation = Animator.StringToHash("grabScrap"); // Trigger
    private static readonly int BreakMovementAnimation = Animator.StringToHash("break"); // Trigger
    private static readonly int ThrowPlayerAnimation = Animator.StringToHash("throwPlayer"); // Trigger

    public static List<Janitor> janitors = new();

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

    public IEnumerator CheckForScrapNearby()
    {
        while (true)
        {
            yield return new WaitForSeconds(2f);
            yield return new WaitUntil(() => currentBehaviourStateIndex == (int)JanitorStates.Idle);
            Collider[] hitColliders = new Collider[20];  // Size accordingly to expected max items nearby
            int numHits = Physics.OverlapSphereNonAlloc(this.transform.position, 15, hitColliders, LayerMask.GetMask("Props"), QueryTriggerInteraction.Collide);
            for (int i = 0; i < numHits; i++)
            {
                if (!hitColliders[i].gameObject.TryGetComponent(out GrabbableObject grabbableObject) || grabbableObject.isHeld || grabbableObject.isHeldByEnemy || grabbableObject.playerHeldBy != null || storedScrapAndValueDict.ContainsKey(grabbableObject)) continue;
                NavMeshPath path = new NavMeshPath();
                if (!agent.CalculatePath(hitColliders[i].gameObject.transform.position, path) || path.status != NavMeshPathStatus.PathComplete || DoCalculatePathDistance(path) > 12.5f) continue;
                targetScrap = grabbableObject;
                SetPathAsDestination(path);
                SwitchToBehaviourServerRpc((int)JanitorStates.StoringScrap);
                break;
            }
        }
    }

    public float DoCalculatePathDistance(NavMeshPath path)
    {
        float length = 0.0f;
      
        if (path.corners.Length > 1)
        {
            for (int i = 1; i < path.corners.Length; i++)
            {
                length += Vector3.Distance(path.corners[i-1], path.corners[i]);
            }
        }
        Plugin.ExtendedLogging($"Path distance: {length}");
        return length;
    }

    public override void Start()
    {
        base.Start();
        janitorRandom = new System.Random(StartOfRound.Instance.randomMapSeed + janitors.Count);

        Material variantMaterial = variantMaterials[janitorRandom.Next(0, variantMaterials.Length)];
        Material[] currentMaterials = skinnedMeshRenderers[0].sharedMaterials;
        Material[] newMaterials = currentMaterials;
        newMaterials[1] = variantMaterial;
        skinnedMeshRenderers[0].SetMaterials(newMaterials.ToList());

        creatureVoice.PlayOneShot(spawnSound);
        smartAgentNavigator.SetAllValues(isOutside);
        SwitchToBehaviourStateOnLocalClient((int)JanitorStates.Idle);

        if (!IsServer) return;
        StartCoroutine(CheckForScrapNearby());
    }

    public override void Update()
    {
        base.Update();
        if (currentBehaviourStateIndex == (int)JanitorStates.Idle)
        {
            idleTimer -= Time.deltaTime;
        }

        if (idleTimer < 0)
        {
            idleTimer = janitorRandom.Next(30, 150);
            creatureVoice.PlayOneShot(idleSounds[janitorRandom.Next(0, idleSounds.Length)]);
        }
        if (!IsServer) return;
        if (_isRotating)
        {
            HandleRotation();
        }
    }

    public void LateUpdate()
    {
        if (currentBehaviourStateIndex == (int)JanitorStates.ZoomingOff)
        {
            if (targetPlayer == null || targetPlayer.isPlayerDead)
            {
                targetPlayer = null;
                sirenLights.SetActive(false);
                SwitchToBehaviourStateOnLocalClient((int)JanitorStates.Idle);
                if (IsServer)
                {
                    agent.speed = 7.5f;
                    creatureAnimator.SetBool(IsAngryAnimation, false);
                }
                skinnedMeshRenderers[0].SetBlendShapeWeight(0, 0);
                return;
            }

            targetPlayer.transform.position = playerBoneTransform.position;
            targetPlayer.transform.rotation = playerBoneTransform.rotation;
        }
    }

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
                // DoDead();
                break;
        }
    }

    public void DoIdle()
    {
        if (!_isPathValid || IsPathInvalid())
        {
            // Plugin.ExtendedLogging($"[Janitor] Attempting to calculate a new path because path is invalid: {IsPathInvalid()}.");
            CalculateAndSetNewPath(RoundManager.Instance.GetRandomNavMeshPositionInRadius(this.transform.position, 50, default));
            return;
        }

        if (_isRotating) return;
        HandleMovement();
        // Plugin.ExtendedLogging($"[Janitor] Corner Length: {_pathCorners.Length}");
        // Plugin.ExtendedLogging($"[Janitor] Current corner index: {_currentCornerIndex}");
        // If we have corners to traverse...
        if (_pathCorners.Length <= 0 || _currentCornerIndex >= _pathCorners.Length) return;

        float distToCorner = Vector3.Distance(transform.position, _pathCorners[_currentCornerIndex]);
        // Plugin.ExtendedLogging($"[Janitor] Distance to corner: {distToCorner}");
        if (distToCorner > _cornerThreshold) return;
        // Are we at the final corner? 
        if (_currentCornerIndex == _pathCorners.Length - 1)
        {
            // We reached the actual destination!
            CalculateAndSetNewPath(RoundManager.Instance.GetRandomNavMeshPositionInRadius(this.transform.position, 50, default));
            return;
        }

        BeginRotation();
    }

    public void DoStoringScrap()
    {
        if (!_isPathValid || IsPathInvalid() || targetScrap == null || targetScrap.isHeld || targetScrap.playerHeldBy != null || targetScrap.isHeldByEnemy)
        {
            // Plugin.ExtendedLogging($"[Janitor] Attempting to calculate a new path because path is invalid: {IsPathInvalid()}.");
            // go back to idle assuming a player picked it up or something, don't get angry?
            _isPathValid = false;
            targetScrap = null;
            SwitchToBehaviourServerRpc((int)JanitorStates.Idle);
            return;
        }

        if (_isRotating) return;
        HandleMovement();
        // Plugin.ExtendedLogging($"[Janitor] Corner Length: {_pathCorners.Length}");
        // Plugin.ExtendedLogging($"[Janitor] Current corner index: {_currentCornerIndex}");
        // If we have corners to traverse...
        if (_pathCorners.Length <= 0 || _currentCornerIndex >= _pathCorners.Length) return;

        float distToCorner = Vector3.Distance(transform.position, _pathCorners[_currentCornerIndex]);
        // Plugin.ExtendedLogging($"[Janitor] Distance to corner: {distToCorner}");
        if (distToCorner > _cornerThreshold) return;
        // Are we at the final corner? 
        if (_currentCornerIndex == _pathCorners.Length - 1)
        {
            // We reached the actual destination!
            // Check if scrap is here, if not, set a new destination
            if (Vector3.Distance(targetScrap.transform.position, this.transform.position) <= agent.stoppingDistance + 1.2 && !currentlyGrabbingScrap)
            {
                Plugin.ExtendedLogging($"[Janitor] Scrap collected!");
                currentlyGrabbingScrap = true;
                SetTargetScrapUngrabbableServerRpc(new NetworkObjectReference(targetScrap.gameObject));
                // do the animation for collecting scrap.
                // Add scrap to relevant lists too.
                // Make the scrap un-grabbable.
            }
            return;
        }
        BeginRotation();
    }

    public void DoFollowingPlayer()
    {
        if (targetPlayer == null || targetPlayer.isPlayerDead)
        {
            // go back to idle assuming a died or something, don't get angry?
            _isPathValid = false;
            targetPlayer = null;
            SwitchToBehaviourServerRpc((int)JanitorStates.Idle);
            agent.speed = 7.5f;
            creatureAnimator.SetBool(IsAngryAnimation, false);
            SetBlendShapeWeightServerRpc(0);
            return;
        }

        if (!_isPathValid || IsPathInvalid())
        {
            // Plugin.ExtendedLogging($"[Janitor] Attempting to calculate a new path because path is invalid: {IsPathInvalid()}.");
            NavMesh.SamplePosition(targetPlayer.transform.position, out NavMeshHit hit, 5, NavMesh.AllAreas);
            CalculateAndSetNewPath(hit.position);
            return;
        }

        if (_isRotating) return;
        HandleMovement();
        // Plugin.ExtendedLogging($"[Janitor] Corner Length: {_pathCorners.Length}");
        // Plugin.ExtendedLogging($"[Janitor] Current corner index: {_currentCornerIndex}");
        // If we have corners to traverse...
        if (_pathCorners.Length <= 0 || _currentCornerIndex >= _pathCorners.Length) return;

        float distToCorner = Vector3.Distance(transform.position, _pathCorners[_currentCornerIndex]);
        // Plugin.ExtendedLogging($"[Janitor] Distance to corner: {distToCorner}");
        
        float distToPlayer = Vector3.Distance(transform.position, targetPlayer.transform.position);
        if (distToPlayer <= agent.stoppingDistance + 3 && !currentlyGrabbingPlayer)
        {
            // Plugin.ExtendedLogging($"[Janitor] Player collected!");
            currentlyGrabbingPlayer = true;
            SetPlayerImmovableServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, targetPlayer));
            return;
        }
        else if (distToCorner > _cornerThreshold)
        {
            return;
        }
        else if (!currentlyGrabbingPlayer && Vector3.Distance(targetPlayer.transform.position, agent.path.corners[agent.path.corners.Length - 1]) > 3f && _currentCornerIndex != 0)
        {
            // Plugin.ExtendedLogging($"[Janitor] Player too far from corner! {Vector3.Distance(targetPlayer.transform.position, agent.path.corners[agent.path.corners.Length - 1])}");
            _isPathValid = false;
            return;
        }
        BeginRotation();
    }

    public void DoZoomingOff()
    {
        if (!_isPathValid || IsPathInvalid() || targetTrashCan == null)
        {
            if (currentlyThrowingPlayer) return;
            bool foundAtleastOneViablePath = false;
            foreach (TrashCan trashCan in trashCans)
            {
                Plugin.ExtendedLogging($"[Janitor] Attempting to calculate a new path because path is invalid: {IsPathInvalid()}.");
                NavMesh.SamplePosition(trashCan.transform.position, out NavMeshHit hit, 5, NavMesh.AllAreas);
                if (CalculateAndSetNewPath(hit.position))
                {
                    targetTrashCan = trashCan;
                    foundAtleastOneViablePath = true;
                    break;
                }
            }
            if (!foundAtleastOneViablePath)
            {
                currentlyThrowingPlayer = true;
                creatureNetworkAnimator.SetTrigger(ThrowPlayerAnimation);
                // Throw player and go back to being idle.
            }
            return;
        }

        if (_isRotating) return;
        HandleMovement();
        // Plugin.ExtendedLogging($"[Janitor] Corner Length: {_pathCorners.Length}");
        // Plugin.ExtendedLogging($"[Janitor] Current corner index: {_currentCornerIndex}");
        // If we have corners to traverse...
        if (_pathCorners.Length <= 0 || _currentCornerIndex >= _pathCorners.Length) return;

        float distToCorner = Vector3.Distance(transform.position, _pathCorners[_currentCornerIndex]);
        // Plugin.ExtendedLogging($"[Janitor] Distance to corner: {distToCorner}");
        if (distToCorner > _cornerThreshold) return;
        // Are we at the final corner? 
        if (_currentCornerIndex == _pathCorners.Length - 1)
        {
            // We reached the actual destination!
            // Check if scrap is here, if not, set a new destination
            if (Vector3.Distance(targetTrashCan.transform.position, this.transform.position) <= agent.stoppingDistance + 1.2 && !currentlyThrowingPlayer)
            {
                currentlyThrowingPlayer = true;
                Plugin.ExtendedLogging($"[Janitor] Throwing player into trash!");
                creatureNetworkAnimator.SetTrigger(ThrowPlayerAnimation);
                // do the animation for throwing player.
                // Anim event that switches back to idle prob.
            }
            return;
        }
        BeginRotation();
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
        targetPlayer = player; // if they somehow turned null somewhere through this
        player.disableMoveInput = true;
        player.disableLookInput = true;
        if (IsServer) creatureAnimator.SetBool(HoldingPlayerAnimation, true);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetTargetScrapUngrabbableServerRpc(NetworkObjectReference netObjRef)
    {
        SetTargetScrapUngrabbableClientRpc(netObjRef);
    }

    [ClientRpc]
    public void SetTargetScrapUngrabbableClientRpc(NetworkObjectReference netObjRef)
    {
        var _targetScrap = ((GameObject)netObjRef).GetComponent<GrabbableObject>();
        if (targetScrap != _targetScrap)
        {
            Plugin.Logger.LogError("This shouldn't be possible, triggered grabbing an item that wasn't the target item??? report this.");
            SwitchToBehaviourStateOnLocalClient((int)JanitorStates.Idle);
            return;
        }
        if (_targetScrap.isHeld && _targetScrap.playerHeldBy != null)
        {
            targetScrap = null;
            // Freak out
            sirenLights.SetActive(true);
            _isPathValid = false;
            SwitchToBehaviourStateOnLocalClient((int)JanitorStates.FollowingPlayer);
            skinnedMeshRenderers[0].SetBlendShapeWeight(0, 100);
            if (!IsServer) return;
            agent.speed = 15f;
            creatureAnimator.SetBool(IsAngryAnimation, true);
            return;
        }
        _targetScrap.grabbable = false;
        if (HoarderBugAI.grabbableObjectsInMap.Contains(_targetScrap.gameObject)) HoarderBugAI.grabbableObjectsInMap.Remove(_targetScrap.gameObject);
        if (!IsServer) return;
        creatureNetworkAnimator.SetTrigger(GrabScrapAnimation);
    }

    private bool CalculateAndSetNewPath(Vector3 targetPosition)
    {
        // Create a new path object
        NavMeshPath path = new NavMeshPath();
        bool pathFound = agent.CalculatePath(targetPosition, path);
        Plugin.ExtendedLogging($"[Janitor] Path found: {pathFound}");
        Plugin.ExtendedLogging($"[Janitor] Path status: {path.status}");
        if (pathFound && path.status == NavMeshPathStatus.PathComplete)
        {
            SetPathAsDestination(path);
            return true;
        }
        else
        {
            // repeat
            _isPathValid = false;
            agent.ResetPath();
            return false;
        }
    }

    private void SetPathAsDestination(NavMeshPath navMeshPath)
    {
        _isPathValid = true;
        agent.SetPath(navMeshPath);
        // Store corners
        _pathCorners = navMeshPath.corners;
        _currentCornerIndex = 0;

        // Force the agent to start heading to the first corner
        if (_pathCorners.Length > 0)
        {
            smartAgentNavigator.DoPathingToDestination(_pathCorners[_currentCornerIndex], !isOutside, false, null);
        }
    }

    private bool IsPathInvalid()
    {
        if (agent.path.status == NavMeshPathStatus.PathInvalid)
        {
            Plugin.ExtendedLogging($"[Janitor] agent has path: {agent.hasPath}, path status: {agent.path.status}.");
            return true;
        }
        return false;
    }

    private void HandleMovement()
    {
        float forwardSpeed = agent.velocity.magnitude;

        creatureAnimator.SetFloat(LeftTreadFloat,  forwardSpeed);
        creatureAnimator.SetFloat(RightTreadFloat, forwardSpeed);
        if (forwardSpeed > 0)
        {
            creatureSFX.volume = 1f;
        }
        else
        {
            creatureSFX.volume = 0f;
        }
        // Ensure we are always heading towards the current corner
        if (_pathCorners.Length > 0 && _currentCornerIndex < _pathCorners.Length)
        {
            // Double check the agent is heading to the correct corner
            if (!agent.pathPending && Vector3.Distance(agent.destination, _pathCorners[_currentCornerIndex]) > 0.1f)
            {
                smartAgentNavigator.DoPathingToDestination(_pathCorners[_currentCornerIndex], !isOutside, false, null);
            }
        }
    }

    private void BeginRotation()
    {
        _isRotating = true;
        Plugin.ExtendedLogging($"[Janitor] Begin Rotation with speed: {agent.velocity.magnitude}");
        if (agent.velocity.magnitude > 7.5f)
        {
            creatureNetworkAnimator.SetTrigger(BreakMovementAnimation);
        }
        // Stop the agent from moving while rotating
        agent.velocity = Vector3.zero;
        agent.isStopped = true;

        // Treads visually reset (optional)
        creatureAnimator.SetFloat(LeftTreadFloat,  0f);
        creatureAnimator.SetFloat(RightTreadFloat, 0f);
    }

    private void HandleRotation()
    {
        // We want to rotate to face the next corner
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

        // Determine if turning right or left
        float signedAngle = Vector3.SignedAngle(transform.forward, direction, Vector3.up);
        bool turningRight = signedAngle > 0f; // For controlling treads animation

        // Rotate
        // Plugin.ExtendedLogging($"[Janitor] Signed angle: {signedAngle} so turning Right: {turningRight}");
        float rotateDelta = 45 * Time.deltaTime * (turningRight ? 1 : -1) * (sirenLights.activeSelf ? 4 : 1);
        transform.Rotate(Vector3.up, rotateDelta);

        // Animate treads
        creatureAnimator.SetFloat(LeftTreadFloat,  1f * (turningRight ? 1 : -1));
        creatureAnimator.SetFloat(RightTreadFloat, -1f * (turningRight ? 1 : -1));

        creatureSFX.volume = 1f;
        // Once rotation time is up, or angle is small enough, finish rotation
        if (Mathf.Abs(signedAngle) < 5f)
        {
            _isRotating = false;

            // Resume agent movement
            agent.isStopped = false;

            // Move on to the next corner
            if (_currentCornerIndex < _pathCorners.Length - 1)
            {
                _currentCornerIndex++;
                smartAgentNavigator.DoPathingToDestination(_pathCorners[_currentCornerIndex], !isOutside, false, null);
            }

            // Reset tread animations for a brief pause
            creatureSFX.volume = 0f;
            creatureAnimator.SetFloat(LeftTreadFloat,  0f);
            creatureAnimator.SetFloat(RightTreadFloat, 0f);
        }
    }

    public void DetectDroppedScrap(PlayerControllerB playerWhoDropped)
    {
        DetectDroppedScrapServerRpc(playerWhoDropped.transform.position, Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerWhoDropped));
    }

    [ServerRpc(RequireOwnership = false)]
    public void DetectDroppedScrapServerRpc(Vector3 noisePosition, int playerWhoDroppedIndex)
    {
        NavMesh.SamplePosition(noisePosition, out NavMeshHit hit, 5f, NavMesh.AllAreas);
        NavMeshPath path = new NavMeshPath();
        Plugin.ExtendedLogging($"[Janitor] Player {playerWhoDroppedIndex} dropped scrap at {noisePosition}");
        if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete && DoCalculatePathDistance(path) <= 20f)
        {
            SetTargetClientRpc(playerWhoDroppedIndex);
            targetPlayer = StartOfRound.Instance.allPlayerScripts[playerWhoDroppedIndex];
            SetBlendShapeWeightClientRpc(100);
            agent.speed = 15f;
            creatureAnimator.SetBool(IsAngryAnimation, true);
            _isPathValid = false;
            SwitchToBehaviourClientRpc((int)JanitorStates.FollowingPlayer);
        }
        PlayDetectScrapSoundClientRpc();
    }

    [ClientRpc]
    public void PlayDetectScrapSoundClientRpc()
    {
        creatureVoice.PlayOneShot(detectItemDroppedSounds[janitorRandom.Next(0, detectItemDroppedSounds.Length)]);
    }

    public override void HitEnemy(int force = 1, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
        if (isEnemyDead) return;
        enemyHP -= force;

        if (playerWhoHit != null && IsServer && (currentBehaviourStateIndex != (int)JanitorStates.FollowingPlayer || currentBehaviourStateIndex == (int)JanitorStates.ZoomingOff))
        {
            DetectDroppedScrap(playerWhoHit);
        }
        if (enemyHP <= 0 && !isEnemyDead)
        {
            if (IsOwner)
            {
                creatureAnimator.SetBool(IsDeadAnimation, true);
                KillEnemyOnOwnerClient();
            }
        }
    }

    public override void KillEnemy(bool destroy = false)
    {
        base.KillEnemy(destroy);
        creatureVoice.PlayOneShot(deathSounds[janitorRandom.Next(0, deathSounds.Length)]);
        currentlyThrowingPlayer = false;
        currentlyGrabbingPlayer = false;
        currentlyGrabbingScrap = false;
        targetScrap = null;
        targetTrashCan = null;
        if (targetPlayer != null)
        {
            targetPlayer.disableMoveInput = false;
            targetPlayer.disableLookInput = false;
        }
        SwitchToBehaviourStateOnLocalClient((int)JanitorStates.Dead);
        foreach (var item in storedScrapAndValueDict.Keys)
        {
            item.EnableItemMeshes(true);
            item.EnablePhysics(true);
            item.grabbable = true;
            item.grabbableToEnemies = true;
            if (!HoarderBugAI.grabbableObjectsInMap.Contains(item.gameObject)) HoarderBugAI.grabbableObjectsInMap.Add(item.gameObject);
            // do something which is either regurgitating the item/scrap or custom scrap that's worth the combined value of all stored scraps.
        }
        creatureSFX.volume = 0f;
        if (IsServer)
        {
            creatureAnimator.SetBool(HoldingPlayerAnimation, false);
            creatureAnimator.SetFloat(LeftTreadFloat, 0);
            creatureAnimator.SetFloat(RightTreadFloat, 0);
            creatureAnimator.SetBool(IsAngryAnimation, false);
        }
        sirenLights.SetActive(false);
        skinnedMeshRenderers[0].SetBlendShapeWeight(0, 0);
    }

    #region Animation Events
    public void GrabScrapAnimEvent()
    {
        if (targetScrap == null || Vector3.Distance(targetScrap.transform.position, this.transform.position) > 1.25f)
        {
            Plugin.Logger.LogError("Scrap I was reaching for suddenly fucking vanished, please report this.");
            SwitchToBehaviourStateOnLocalClient((int)JanitorStates.Idle);
            return;
        }

        if (currentBehaviourStateIndex != (int)JanitorStates.StoringScrap)
        {
            Plugin.Logger.LogError("I shouldn't be in this animation, report this");
            SwitchToBehaviourStateOnLocalClient((int)JanitorStates.Idle);
            return;
        }

        if (targetScrap.playerHeldBy != null)
        {
            // Freak out
            targetScrap.grabbable = true;
            sirenLights.SetActive(true);
            targetPlayer = targetScrap.playerHeldBy;
            _isPathValid = false;
            SwitchToBehaviourStateOnLocalClient((int)JanitorStates.FollowingPlayer);
            skinnedMeshRenderers[0].SetBlendShapeWeight(0, 100);
            if (!IsServer) return;
            agent.speed = 15f;
            creatureAnimator.SetBool(IsAngryAnimation, true);
            return;
        }

        StartCoroutine(PlaceScrapInsideJanitor(targetScrap));
        targetScrap = null;
    }

    private IEnumerator PlaceScrapInsideJanitor(GrabbableObject _targetScrap)
    {
        _targetScrap.parentObject = handTransform;
        yield return new WaitForSeconds(0.2f);
        storedScrapAndValueDict.Add(_targetScrap, _targetScrap.scrapValue);
        _targetScrap.parentObject = placeToHideScrap;
        _targetScrap.transform.position = placeToHideScrap.position;
        _targetScrap.EnableItemMeshes(false);
        _targetScrap.EnablePhysics(false);
        SwitchToBehaviourStateOnLocalClient((int)JanitorStates.Idle);
        currentlyGrabbingScrap = false;
    }

    public void GrabPlayerAnimEvent()
    {
        creatureVoice.PlayOneShot(grabPlayerSounds[janitorRandom.Next(0, grabPlayerSounds.Length)]);
        SwitchToBehaviourStateOnLocalClient((int)JanitorStates.ZoomingOff);
        currentlyGrabbingPlayer = false;
    }

    public void ThrowPlayerAnimEvent()
    {
        creatureVoice.PlayOneShot(throwPlayerSounds[janitorRandom.Next(0, throwPlayerSounds.Length)]);
        PlayerControllerB previousTargetPlayer = targetPlayer;
        targetPlayer = null;
        if (targetTrashCan != null)
        {
            Vector3 directionToTrash = (targetTrashCan.transform.position - previousTargetPlayer.transform.position).normalized;
            previousTargetPlayer.externalForceAutoFade = directionToTrash * 25f;
        }
        else
        {
            previousTargetPlayer.externalForceAutoFade = this.transform.forward * 25f;
        }
        targetTrashCan = null;
        previousTargetPlayer.disableMoveInput = false;
        previousTargetPlayer.disableLookInput = false;
        currentlyThrowingPlayer = false;
        previousTargetPlayer.inAnimationWithEnemy = null;
        previousTargetPlayer.DamagePlayer(20, true, false, CauseOfDeath.Gravity, 0, false, default);
        if (IsServer) creatureAnimator.SetBool(HoldingPlayerAnimation, false);
    }
    #endregion
}