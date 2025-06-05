using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;

public class AutonomousCrane : NetworkBehaviour
{
    [SerializeField]
    private Animator _leverAnimator = null!;

    [SerializeField]
    private InteractTrigger _disableInteract = null!;

    private bool _craneIsActive = false;
    private PlayerControllerB? _targetPlayer = null;
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
        if (_targetPlayer != null)
        {
            _currentState = CraneState.TargetingPlayer;
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

    public void MoveCrane()
    {

    }
}