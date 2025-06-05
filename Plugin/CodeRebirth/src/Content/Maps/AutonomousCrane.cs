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

    }

    public void MoveCrane()
    {
        
    }
}