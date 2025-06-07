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
    private Vector3 _magnetOriginalPosition = Vector3.zero;
    private Vector3 _targetPosition = Vector3.zero;
    private Coroutine _findingTargetRoutine = null!;
    private CraneState _currentState = CraneState.Idle;

    public enum CraneState
    {
        Idle,
        TargetingPlayer,
        DroppingMagnet
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
            case CraneState.TargetingPlayer:
                DoTargetingBehaviour();
                break;
            case CraneState.DroppingMagnet:
                DoDroppingMagnetBehaviour();
                break;
        }
    }

    private void DoIdleBehaviour()
    {
        // Check for players in range and switch to TargetingPlayer state if found
        if (_targetablePlayers.Count > 0)
        {
            _currentState = CraneState.TargetingPlayer;
            foreach (var player in _targetablePlayers.ToArray())
            {
                if (player.isPlayerDead || !player.isPlayerControlled)
                {
                    _targetablePlayers.Remove(player);
                    continue;
                }

                _targetPlayer = player;
                _targetablePlayers.Remove(player);
                break;
            }
            return;
        }

        // Random movement logic here
        MoveCrane();
    }

    private void DoTargetingBehaviour()
    {
        // Logic to target the player
        if (_targetPlayer == null || Vector3.Distance(transform.position, _targetPlayer.transform.position) > 10f)
        {
            _currentState = CraneState.Idle;
            return;
        }

        // Move towards the player and prepare to drop the magnet
        MoveCrane();
    }

    private void DoDroppingMagnetBehaviour()
    {
        // Logic to drop the magnet
        MoveCrane();
    }

    private void MoveCrane()
    {
        if (_findingTargetRoutine != null)
            return;

        if (_targetPosition == Vector3.zero)
        {
            _findingTargetRoutine = StartCoroutine(GetTargetPosition());
        }

        if (!RotateCraneHead())
            return;

        if (!MoveCraneArm())
            return;

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
            targetDirection.z = 0; // Ignore z-axis for rotation
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            _cabinHead.transform.rotation = Quaternion.RotateTowards(_cabinHead.transform.rotation, targetRotation, 45f * Time.deltaTime);
            return false; // Still rotating
        }
        return true;
    }

    private bool MoveCraneArm()
    {
        // Increase/Decrease the arm's x-axis inbetween 20 and 80 degrees, if decreasing arm rotation x-axis, increase magnet's rotation x-axis by the same amount.
        // Set the position of the magnet to be always be 10 units below the arm's downward ray
        
        return true;
    }

    private void MoveMagnet()
    {

    }

    private IEnumerator GetTargetPosition()
    {
        if (_targetPlayer != null)
        {
            _targetPosition = RoundManager.Instance.GetRandomNavMeshPositionInRadius(_targetPlayer.transform.position, 3f, default);
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