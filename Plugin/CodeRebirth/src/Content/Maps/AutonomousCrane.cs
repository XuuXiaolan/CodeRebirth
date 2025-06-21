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
    private GameObject _craneArmStart = null!;
    [SerializeField]
    private GameObject _craneArmEnd = null!;
    [SerializeField]
    private GameObject _magnet = null!; // this can move up and down, but not rotate, it should always be facing downwards, and be straight under the arm, it can't get closer than 5 units of the arm's downward raycast

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
    private CraneState _currentState = CraneState.Idle;

    public enum CraneState
    {
        Idle,
        GetTargetPosition,
        MoveCraneHead,
        MoveCraneArm,
        DropMagnet
    }

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

        foreach (var player in _targetablePlayers)
        {
            Plugin.ExtendedLogging($"AutonomousCrane: Player {player} is in range, switching to GetTargetPosition state.");
            _targetPosition = Vector3.zero;
            _targetablePlayers.Remove(player);
            _targetPlayer = player;
            _currentState = CraneState.GetTargetPosition;
            break;
        }
    }

    private void DoGetTargetPositionBehaviour()
    {
        // Logic to target the player
        if (_targetPlayer == null)
        {
            _currentState = CraneState.Idle;
            return;
        }

        GetTargetPosition(_targetPlayer);

        Plugin.ExtendedLogging($"Move crane head time");
        _currentState = CraneState.MoveCraneHead;
    }

    private void DoMovingCraneHeadBehaviour()
    {
        if (_targetPlayer == null)
        {
            // reset stuff
            _currentState = CraneState.Idle;
            return;
        }

        GetTargetPosition(_targetPlayer);

        if (RotateCraneHead())
        {
            Plugin.ExtendedLogging($"Move crane arm time");
            _currentState = CraneState.MoveCraneArm;
        }
    }

    private void DoMovingCraneArmBehaviour()
    {
        if (_targetPlayer == null) // || far enough away from the direction, maybe read the Vector3.Dot or something
        {
            // reset stuff
            _currentState = CraneState.Idle;
            return;
        }

        if (MoveCraneArm()) // crane arm's end might be kinda misplaced cuz im noticing the arm adjusts itself a tiny bit off.
        {
            Plugin.ExtendedLogging($"Move magnet time");
            _currentState = CraneState.DropMagnet;
        }
    }

    private void DoDroppingMagnetBehaviour() // just drop pretty fast and align self with terrain
    {
        // not implemented properly yet
        MoveMagnet();
    }

    private bool RotateCraneHead()
    {
        Vector3 directionToTarget = _targetPosition - _cabinHead.transform.position;
        directionToTarget.y = 0f;

        Quaternion targetRot = Quaternion.LookRotation(directionToTarget, Vector3.up);
        _cabinHead.transform.rotation = Quaternion.RotateTowards(_cabinHead.transform.rotation, targetRot, 25f * Time.deltaTime);
        float angleDelta = Quaternion.Angle(_cabinHead.transform.rotation, targetRot);

        return angleDelta < 0.1f;
    }

    private bool MoveCraneArm()
    {
        Vector3 worldDir = _targetPosition - _craneArmStart.transform.position;
        if (worldDir.sqrMagnitude < 0.001f)
            return true; // nothing to do if we’re basically on top of it
 
        Quaternion worldTargetRot = Quaternion.LookRotation(worldDir, Vector3.up);

        float headYaw = _cabinHead.transform.eulerAngles.y;
        Quaternion undoYaw = Quaternion.Euler(0f, headYaw, 0f);
        Quaternion localTargetRot = Quaternion.Inverse(undoYaw) * worldTargetRot;

        Vector3 localEulerAngles = localTargetRot.eulerAngles;
        float pitch = localEulerAngles.x > 180f ? localEulerAngles.x - 360f : localEulerAngles.x;
        pitch = Mathf.Clamp(pitch, 20f, 80f);
        localTargetRot = Quaternion.Euler(pitch, 0f, 0f);

        _craneArmStart.transform.localRotation = Quaternion.RotateTowards(_craneArmStart.transform.localRotation, localTargetRot, 25f * Time.deltaTime);

        float angleLeft = Quaternion.Angle(_craneArmStart.transform.localRotation, localTargetRot);
        Plugin.ExtendedLogging($"Arm pitch remaining: {angleLeft:F2}°");

        return angleLeft < 0.1f;
    }

    [SerializeField]
    [Tooltip("How fast the magnet moves toward the ground")]
    private float _magnetSpeed = 15f;

    private bool _movingMagnet = false;

    private void MoveMagnet()
    {
        if (_movingMagnet)
            return;

        if (!Physics.Raycast(_magnet.transform.position, Vector3.down, out RaycastHit hit, 100f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
        {
            // shouldnt really ever be possible lol
            return;
        }

        Vector3 targetPos = hit.point;
        Vector3 normal = hit.normal;

        _movingMagnet = true;
        Plugin.ExtendedLogging($"Moving magnet from: {_magnet.transform.position} to: {targetPos}");
    }

    private void GetTargetPosition(PlayerControllerB targetPlayer)
    {
        _targetPosition = targetPlayer.transform.position;
        Plugin.ExtendedLogging($"Target position set to: {_targetPosition}");
    }
}