using System.Collections;
using System.Collections.Generic;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;

public class AutonomousCrane : NetworkBehaviour
{
    [Header("Crane Parts")]
    [SerializeField]
    private GameObject _cabinHead = null!; // this can turn in a 360 degree circle
    [SerializeField]
    private GameObject _craneArm = null!; // When this moves up or down, magnet needs to rotate to still always be facing downwards and be straight under the arm, can only change it's x rotation to between 80 - 20 degrees at most
    [SerializeField]
    private GameObject _magnet = null!; // this can move up and down, but not rotate, it should always be facing downwards, and be straight under the arm, it can't get closer than 5 units of the arm's downward raycast
    [SerializeField]
    private GameObject _craneArmDownwardRaycast = null!;

    [Header("Disabling Interacts")]
    [SerializeField]
    private Animator _leverAnimator = null!;
    [SerializeField]
    private InteractTrigger _disableInteract = null!;

    [HideInInspector]
    public List<PlayerControllerB> _targetablePlayers = new();
    [HideInInspector]
    public PlayerControllerB? _targetPlayer = null;

    private bool _craneIsActive = false;
    private Vector3 _targetPosition = Vector3.zero;
    private Coroutine _findingTargetRoutine = null!;
    private CraneState _currentState = CraneState.Idle;

    public enum CraneState
    {
        Idle,
        GetTargetPosition,
        MoveCraneHead,
        MoveCraneArm,
        DropMagnet
    }

    /*
        if nobody's in range, it just does random movements, picks a direction, left or right, moves for a bit, stops, then rolls a decent chance to drop the magnet.
        picks it's magnet back up
        if a player comes into range at any point, it stops what it's doing, makes sure it's magnet goes up, then moves left and right trying to target where the player is, then drops when the player is directly under it.
        if the player moves out of range, it goes back to random movements.
        it shouldn't look for any target players unless it's in Idle state.
    */

    public void Update()
    {
        switch (_currentState)
        {
            case CraneState.Idle:
                DoIdleBehaviour();
                break;
            case CraneState.GetTargetPosition:
                DoGetTargetPositionBehaviour();
                break;
            case CraneState.MoveCraneHead:
                DoMovingCraneHeadBehaviour();
                break;
            case CraneState.MoveCraneArm:
                DoMovingCraneArmBehaviour();
                break;
            case CraneState.DropMagnet:
                DoDroppingMagnetBehaviour();
                break;
        }
    }

    private void DoIdleBehaviour()
    {
        // Check for players in range and switch to TargetingPlayer state if found
        if (_targetablePlayers.Count <= 0)
            return;

        foreach (var player in _targetablePlayers.ToArray())
        {
            if (player.isPlayerDead || !player.isPlayerControlled)
            {
                _targetablePlayers.Remove(player);
                continue;
            }

            Plugin.ExtendedLogging($"AutonomousCrane: Player {player} is in range, switching to GetTargetPosition state.");
            _currentState = CraneState.GetTargetPosition;
            _targetPlayer = player;
            _targetPosition = Vector3.zero;
            _targetablePlayers.Remove(player);
            break;
        }
        return;
    }

    private void DoGetTargetPositionBehaviour()
    {
        // Logic to target the player
        if (_targetPlayer == null || Vector3.Distance(transform.position, _targetPlayer.transform.position) <= 10f)
        {
            _targetPlayer = null;
            _currentState = CraneState.Idle;
            return;
        }

        if (_findingTargetRoutine != null)
            return;

        if (_targetPosition == Vector3.zero)
        {
            _findingTargetRoutine = StartCoroutine(GetTargetPosition());
            return;
        }

        Plugin.ExtendedLogging($"Move crane head time");
        _currentState = CraneState.MoveCraneHead;
    }

    private void DoMovingCraneHeadBehaviour()
    {
        if (RotateCraneHead())
        {
            Plugin.ExtendedLogging($"Move crane arm time");
            _currentState = CraneState.MoveCraneArm;
        }
    }

    private void DoMovingCraneArmBehaviour()
    {
        if (MoveCraneArm())
        {
            Plugin.ExtendedLogging($"Move magnet time");
            _currentState = CraneState.DropMagnet;
        }
    }

    private void DoDroppingMagnetBehaviour()
    {
        MoveMagnet();
    }

    private bool RotateCraneHead()
    {
        float dot = Vector2.Dot(
            new Vector2(_targetPosition.x - transform.position.x, _targetPosition.z - transform.position.z).normalized,
            new Vector2(_cabinHead.transform.forward.x, _cabinHead.transform.forward.z).normalized
        );
        if (dot < 0.99f)
        {
            Vector3 targetDirection = (_targetPosition - _cabinHead.transform.position).normalized;
            targetDirection.x = 0; // Ignore x-axis for rotation
            targetDirection.y = 0; // Ignore z-axis for rotation
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            _cabinHead.transform.rotation = Quaternion.RotateTowards(_cabinHead.transform.rotation, targetRotation, 45f * Time.deltaTime);
            return false; // Still rotating
        }
        return true;
    }

    private bool MoveCraneArm()
    {
        Vector3 pivotPos = _craneArmDownwardRaycast.transform.position;
        Vector3 toTarget = _targetPosition - pivotPos;
        float flatDist = new Vector2(toTarget.x, toTarget.z).magnitude;
        float heightDiff = toTarget.y;
        float desiredAngle = Mathf.Atan2(heightDiff, flatDist) * Mathf.Rad2Deg;

        Plugin.ExtendedLogging($"{this} flatDist: {flatDist} heightDiff: {heightDiff} desiredAngle: {desiredAngle}");
        desiredAngle = Mathf.Clamp(desiredAngle, 20f, 80f);

        Vector3 euler = _craneArm.transform.localEulerAngles;
        float currentAngle = euler.x > 180f ? euler.x - 360f : euler.x; // unwrap
        float maxStep = 30f * Time.deltaTime;
        float newAngle = Mathf.MoveTowards(currentAngle, desiredAngle, maxStep);
        float deltaAngle = newAngle - currentAngle;

        euler.x = newAngle;
        _craneArm.transform.localEulerAngles = euler;

        Vector3 magEuler = _magnet.transform.localEulerAngles;
        magEuler.x -= deltaAngle;
        _magnet.transform.localEulerAngles = magEuler;

        Plugin.ExtendedLogging($"{this} desiredAngle: {desiredAngle} currentAngle: {currentAngle} newAngle: {newAngle} deltaAngle: {deltaAngle}");
        return Mathf.Approximately(newAngle, desiredAngle);
    }

    [SerializeField]
    [Tooltip("How fast the magnet moves toward the ground")]
    private float _magnetSpeed = 15f;

    private bool _movingMagnet = false;

    private void MoveMagnet()
    {
        if (_movingMagnet)
            return;

        Vector3 targetPos = _magnet.transform.position;
        Vector3 normal = Vector3.up;

        if (Physics.Raycast(_magnet.transform.position, Vector3.down, out RaycastHit hit, 100f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
        {
            targetPos = hit.point;
            normal = hit.normal;
        }
        else
        {
            // if there's nothing downwards then it shouldnt send the magnet down onto the ground
            return;
        }

        _movingMagnet = true;
        Plugin.ExtendedLogging($"Moving magnet from: {_magnet.transform.position} to: {targetPos}");
        StartCoroutine(FinalizeMagnetDrop(normal, targetPos));
    }

    private IEnumerator FinalizeMagnetDrop(Vector3 normal, Vector3 targetPos)
    {
        Vector3 originalPos = _magnet.transform.position;
        while (Vector3.Distance(_magnet.transform.position, targetPos) > 0.1f)
        {
            float t = Time.deltaTime * _magnetSpeed;
            _magnet.transform.position = Vector3.MoveTowards(_magnet.transform.position, targetPos, t);
            yield return null;
        }

        float duration = 0.2f;
        float elapsed = 0f;
        Quaternion startRot = _magnet.transform.rotation;
        Quaternion endRot = Quaternion.FromToRotation(_magnet.transform.up, normal);
        Vector3 startPos = _magnet.transform.position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            _magnet.transform.position = Vector3.Lerp(startPos, targetPos, p);
            _magnet.transform.rotation = Quaternion.Slerp(startRot, endRot, p);
            yield return null;
        }

        // ensure exact end state
        _magnet.transform.position = targetPos;
        _magnet.transform.rotation = endRot;

        yield return new WaitForSeconds(3f);
        // move it up again

        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            _magnet.transform.position = Vector3.Lerp(targetPos, startPos, p);
            _magnet.transform.rotation = Quaternion.Slerp(endRot, startRot, p);
            yield return null;
        }
        _magnet.transform.rotation = startRot;

        while (Vector3.Distance(_magnet.transform.position, originalPos) > 0.1f)
        {
            float t = Time.deltaTime * _magnetSpeed;
            _magnet.transform.position = Vector3.MoveTowards(_magnet.transform.position, originalPos, t);
            yield return null;
        }
        _magnet.transform.position = originalPos;
        _movingMagnet = false;

        // OnMagnetHitGround();
    }

    private IEnumerator GetTargetPosition()
    {
        if (_targetPlayer != null)
        {
            _targetPosition = RoundManager.Instance.GetRandomNavMeshPositionInRadius(_targetPlayer.transform.position, 5f, default);
            Plugin.ExtendedLogging($"AutonomousCrane: Target position set to {_targetPosition} for crane at {transform.position} and target player at {_targetPlayer.transform.position}");
        }
        while (_targetPosition == Vector3.zero)
        {
            _targetPosition = RoundManager.Instance.GetRandomNavMeshPositionInRadius(transform.position, 40f, default);
            if (Vector3.Distance(_targetPosition, transform.position) <= 13f)
            {
                _targetPosition = Vector3.zero;
            }
            yield return null;
        }
        Plugin.ExtendedLogging($"AutonomousCrane: Target position set to {_targetPosition} for crane at {transform.position}");
        _findingTargetRoutine = null!;
    }
}