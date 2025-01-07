using System.Collections;
using System.Collections.Generic;
using System.IO;
using GameNetcodeStuff;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.AI;

namespace CodeRebirth.src.Content.Enemies;
public class Janitor : CodeRebirthEnemyAI
{
    public GameObject sirenLights = null!;
    public Collider[] colliderBlockersFromScrap = [];
    public Transform handTransform = null!;
    public Transform placeToHideScrap = null!;

    private GrabbableObject? targetScrap = null;
    private bool currentlyGrabbingScrap = false;
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
    private static readonly int GrabScrapAnimation = Animator.StringToHash("grabScrap"); // Trigger

    public IEnumerator CheckForScrapNearby()
    {
        while (true)
        {
            yield return new WaitForSeconds(2f);
            yield return new WaitUntil(() => currentBehaviourStateIndex == (int)JanitorStates.Idle);
            Collider[] hitColliders = new Collider[20];  // Size accordingly to expected max items nearby
            int numHits = Physics.OverlapSphereNonAlloc(this.transform.position, 20, hitColliders, LayerMask.GetMask("Props"), QueryTriggerInteraction.Collide);
            for (int i = 0; i < numHits; i++)
            {
                if (!hitColliders[i].gameObject.TryGetComponent(out GrabbableObject grabbableObject) || grabbableObject.isHeld || grabbableObject.isHeldByEnemy || grabbableObject.playerHeldBy != null || storedScrapAndValueDict.ContainsKey(grabbableObject)) continue;
                NavMeshPath path = new NavMeshPath();
                if (!agent.CalculatePath(hitColliders[i].gameObject.transform.position, path) || path.status != NavMeshPathStatus.PathComplete || DoCalculatePathDistance(path) > 20) continue;
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
        smartAgentNavigator.SetAllValues(isOutside);
        SwitchToBehaviourStateOnLocalClient((int)JanitorStates.Idle);

        if (!IsServer) return;
        StartCoroutine(CheckForScrapNearby());
        /*
        // Angry stuff
        agent.speed = 15f;
        sirenLights.SetActive(true);
        creatureAnimator.SetBool(IsAngryAnimation, true);
        */
    }

    public override void Update()
    {
        base.Update();
        if (!IsServer) return;
        if (_isRotating)
        {
            HandleRotation();
        }
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
                // Anim event that switches back to idle prob.
                // Add scrap to relevant lists too.
                // Make the scrap un-grabbable.
            }
            return;
        }
        BeginRotation();
    }

    public void DoFollowingPlayer()
    {

    }

    public void DoZoomingOff()
    {

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
            SwitchToBehaviourStateOnLocalClient((int)JanitorStates.FollowingPlayer);
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

    private void CalculateAndSetNewPath(Vector3 targetPosition)
    {
        // Create a new path object
        NavMeshPath path = new NavMeshPath();
        bool pathFound = agent.CalculatePath(targetPosition, path);
        Plugin.ExtendedLogging($"[Janitor] Path found: {pathFound}");
        Plugin.ExtendedLogging($"[Janitor] Path status: {path.status}");
        if (pathFound && path.status == NavMeshPathStatus.PathComplete)
        {
            SetPathAsDestination(path);
        }
        else
        {
            // repeat
            _isPathValid = false;
            agent.ResetPath();
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

        // Determine if turning right or left
        float signedAngle = Vector3.SignedAngle(transform.forward, direction, Vector3.up);
        bool turningRight = signedAngle > 0f; // For controlling treads animation

        // Rotate
        // Plugin.ExtendedLogging($"[Janitor] Signed angle: {signedAngle} so turning Right: {turningRight}");
        float rotateDelta = 45 * Time.deltaTime * (turningRight ? 1 : -1) * (creatureAnimator.GetBool(IsAngryAnimation) ? 2 : 1);
        transform.Rotate(Vector3.up, rotateDelta);

        // Animate treads
        creatureAnimator.SetFloat(LeftTreadFloat,  1f);
        creatureAnimator.SetFloat(RightTreadFloat, -1f);

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
            creatureAnimator.SetFloat(LeftTreadFloat,  0f);
            creatureAnimator.SetFloat(RightTreadFloat, 0f);
        }
    }

    public override void DetectNoise(Vector3 noisePosition, float noiseLoudness, int timesPlayedInOneSpot = 0, int noiseID = 0)
    {
        base.DetectNoise(noisePosition, noiseLoudness, timesPlayedInOneSpot, noiseID);
    }

    public override void OnCollideWithPlayer(Collider other)
    {
        base.OnCollideWithPlayer(other);
    }

    public override void HitEnemy(int force = 1, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
    }

    public override void KillEnemy(bool destroy = false)
    {
        base.KillEnemy(destroy);
        SwitchToBehaviourStateOnLocalClient((int)JanitorStates.Dead);
        foreach (var item in storedScrapAndValueDict.Keys)
        {
            // do something which is either regurgitating the item/scrap or custom scrap that's worth the combined value of all stored scraps.
        }
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
            SwitchToBehaviourStateOnLocalClient((int)JanitorStates.FollowingPlayer);
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
    #endregion
}