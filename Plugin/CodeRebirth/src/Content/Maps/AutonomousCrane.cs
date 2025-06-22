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
    private GameObject _craneMagnetResetGOPosition = null!; // TODO use this gameobject to reset where the magnet will go when it comes back up.
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
        if (_targetPlayer == null)
        {
            _currentState = CraneState.Idle;
            return;
        }

        GetTargetPosition(_targetPlayer);
        _currentState = CraneState.MoveCraneHead;
    }

    private void DoMovingCraneHeadBehaviour()
    {
        if (_targetPlayer == null)
        {
            _currentState = CraneState.Idle;
            return;
        }

        GetTargetPosition(_targetPlayer);

        if (!RotateCraneHead())
        {
            return;
        }

        _currentState = CraneState.MoveCraneArm;
    }

    private void DoMovingCraneArmBehaviour()
    {
        if (_targetPlayer == null)
        {
            _currentState = CraneState.Idle;
            return;
        }

        Vector3 newTargetPosition = _targetPosition;
        newTargetPosition.y = _cabinHead.transform.position.y;
        float dot = Vector3.Dot(_cabinHead.transform.forward, (newTargetPosition - _cabinHead.transform.position).normalized);
        if (dot < 0.98f)
        {
            _currentState = CraneState.MoveCraneHead;
            return;
        }

        GetTargetPosition(_targetPlayer);
        if (!MoveCraneArm())
        {
            return;
        }

        _targetPlayer = null;
        _currentState = CraneState.DropMagnet;
    }

    private void DoDroppingMagnetBehaviour() // just drop pretty fast and align self with terrain
    {
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
        Vector3 localTarget = _craneArmStart.transform.parent.InverseTransformPoint(_targetPosition);

        float horiz = new Vector2(localTarget.x, localTarget.z).magnitude;
        float armLen = Vector3.Distance(_craneArmStart.transform.position, _craneArmEnd.transform.position);

        float rawAngle = Mathf.Asin(Mathf.Clamp01(horiz / armLen)) * Mathf.Rad2Deg;
        if (rawAngle < 20f)
            return true;

        float pitch = Mathf.Clamp(rawAngle, 20f, 80f);

        Quaternion want = Quaternion.Euler(pitch, _craneArmStart.transform.localRotation.eulerAngles.y, _craneArmStart.transform.localRotation.eulerAngles.z);
        _craneArmStart.transform.localRotation = Quaternion.RotateTowards(_craneArmStart.transform.localRotation, want, 25f * Time.deltaTime);

        Quaternion targetEndRot = Quaternion.Euler(80f - pitch, 0f, 0f);
        _craneArmEnd.transform.localRotation = Quaternion.RotateTowards(_craneArmEnd.transform.localRotation, targetEndRot, 25f * Time.deltaTime);

        float remaining = Quaternion.Angle(_craneArmStart.transform.localRotation, want);

        return remaining < 0.1f;
    }

    private Vector3 _magnetTargetPosition = Vector3.zero;
    private Vector3 _originalMagnetPosition = Vector3.zero;
    private Vector3 _magnetOriginalNormals = Vector3.zero;
    private Vector3 _magnetTargetNormals = Vector3.zero;
    private float _magnetMovingProgress = 0f;
    private Coroutine? _adjustMagnetRotationRoutine = null;
    private enum MagnetState
    {
        IdleTop,
        MovingDown,
        IdleBottom,
        MovingUp,
    }
    private MagnetState _magnetState = MagnetState.IdleTop;

    private void MoveMagnet()
    {
        if (_magnetState == MagnetState.IdleTop && Physics.Raycast(_targetPosition + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 100f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
        {
            _originalMagnetPosition = _magnet.transform.position;
            _magnetOriginalNormals = _magnet.transform.up;
            _magnetTargetPosition = hit.point;
            _magnetTargetNormals = hit.normal;
            _magnetMovingProgress = 0f;
            _magnetState = MagnetState.MovingDown;
            Plugin.ExtendedLogging($"Moving magnet to {hit.point}");
        }

        if (_magnetState == MagnetState.MovingDown)
        {
            _magnetMovingProgress += Time.deltaTime * 2.5f;
            if (_magnetMovingProgress >= 1f)
            {
                _magnetMovingProgress = 1f;
                _magnetState = MagnetState.IdleBottom;
            }
            else if (_magnetMovingProgress >= 0.5f && _adjustMagnetRotationRoutine == null)
            {
                _adjustMagnetRotationRoutine = StartCoroutine(AdjustMagnetRotation(_magnetState));
            }
            _magnet.transform.position = Vector3.Lerp(_originalMagnetPosition, _magnetTargetPosition, _magnetMovingProgress);
        }
        else if (_magnetState == MagnetState.IdleBottom)
        {
            _magnetMovingProgress -= Time.deltaTime * 0.25f;
            if (_magnetMovingProgress <= 0)
            {
                _magnetMovingProgress = 0f;
                _adjustMagnetRotationRoutine = StartCoroutine(AdjustMagnetRotation(_magnetState));
                _magnetState = MagnetState.MovingUp;
            }
        }
        else if (_magnetState == MagnetState.MovingUp)
        {
            _magnetMovingProgress += Time.deltaTime * 0.5f;
            if (_magnetMovingProgress >= 1f)
            {
                _magnetState = MagnetState.IdleTop;
                _currentState = CraneState.Idle;
            }
            _magnet.transform.position = Vector3.Lerp(_magnetTargetPosition, _originalMagnetPosition, _magnetMovingProgress);
        }
    }

    private IEnumerator AdjustMagnetRotation(MagnetState magnetState)
    {
        float timeElapsed = 0f;

        while (timeElapsed <= 1f)
        {
            timeElapsed += Time.deltaTime * 0.25f;
            if (magnetState == MagnetState.MovingDown)
            {
                _magnet.transform.up = Vector3.Lerp(_magnetOriginalNormals, _magnetTargetNormals, timeElapsed);
            }
            else if (magnetState == MagnetState.MovingUp)
            {
                _magnet.transform.up = Vector3.Lerp(_magnetTargetNormals, _magnetOriginalNormals, timeElapsed);
            }

            yield return null;
        }
        _adjustMagnetRotationRoutine = null;
    }

    private void GetTargetPosition(PlayerControllerB targetPlayer)
    {
        _targetPosition = targetPlayer.transform.position;
    }
}